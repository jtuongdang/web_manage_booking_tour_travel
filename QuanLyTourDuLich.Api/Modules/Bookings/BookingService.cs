using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;
using QuanLyTourDuLich.Api.Modules.Identity;
using QuanLyTourDuLich.Api.Modules.Promotions;
using PromotionEntity = QuanLyTourDuLich.Api.Models.Entities.Promotions;

namespace QuanLyTourDuLich.Api.Modules.BookingEngine;

public sealed class BookingService(
    AppDbContext db,
    ICurrentUserService current,
    ISystemSettingService settings,
    IAuditLogService audit,
    IPromotionValidationService promoValidator)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private DateTime UtcNow => DateTime.UtcNow;

    public async Task<Bookings> CreateAsync(CreateBookingRequest request, CancellationToken ct)
    {
        ValidatePax(request.AdultCount, request.ChildCount, request.InfantCount, request.SingleRoomCount);
        if (request.Passengers is { Count: > 0 })
            ValidateManifest(request.Passengers, request.AdultCount, request.ChildCount, request.InfantCount);

        var staff = IsStaff;
        var channel = staff ? NormalizeChannel(request.BookingChannel) : "WebB2C";
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var departure = await db.TourDepartures.SingleOrDefaultAsync(x => x.Id == request.DepartureId, ct) ?? throw new NotFoundException("Departure not found.");
        var tour = await db.Tours.SingleOrDefaultAsync(x => x.Id == departure.TourId, ct) ?? throw new NotFoundException("Tour not found.");
        var now = UtcNow;
        var seatsNeeded = BookingStateRules.SeatsCount(request.AdultCount, request.ChildCount);
        if (departure.Status != "Open" || departure.CutoffAt < now ||
            !BookingStateRules.CanReserve(departure.TotalCapacity, departure.HeldSlots, departure.BookedSlots, seatsNeeded) ||
            request.InfantCount > (departure.MaxInfants ?? request.AdultCount) || tour.Status != "Published" || (!staff && (!departure.IsPublic || !tour.IsPublic)))
            throw new ConflictException("Departure is unavailable for the requested passengers.");

        var customer = await FindOrCreateCustomer(request, channel, staff, ct);
        var discount = 0m;
        PromotionEntity? promotion = null;
        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var basis = BookingPricing.CalculateSubtotal(departure, request.AdultCount, request.ChildCount, request.InfantCount, request.SingleRoomCount);
            var totalPax = request.AdultCount + request.ChildCount + request.InfantCount;
            var promoResult = await promoValidator.ValidateAsync(
                new ValidatePromotionRequest(request.PromoCode, tour.Id, customer.Id, basis, totalPax, now), ct);
            if (!promoResult.IsValid || !promoResult.PromotionId.HasValue)
                throw new BadRequestException(promoResult.Reason ?? "Promotion is invalid for this booking.");
            promotion = await db.Promotions.SingleAsync(x => x.Id == promoResult.PromotionId.Value, ct);
            discount = promoResult.DiscountAmount;
        }
        var vatRate = await settings.GetDecimalAsync("VatRatePercent", 8m, ct);
        var taxMode = await settings.GetStringAsync("TaxMode", "Included", ct);
        var depositPercent = await settings.GetDecimalAsync("DefaultDepositPercent", 30m, ct);
        var price = BookingPricing.Calculate(departure, request.AdultCount, request.ChildCount, request.InfantCount, request.SingleRoomCount, discount, vatRate, taxMode, depositPercent);
        var policyId = departure.CancellationPolicyId ?? tour.CancellationPolicyId;
        var policySnapshot = await BuildPolicySnapshotAsync(policyId, ct);
        var holdMinutes = await settings.GetIntAsync(staff ? "DefaultHoldMinutesSales" : "DefaultHoldMinutesB2C", staff ? 120 : 15, ct);
        departure.HeldSlots += seatsNeeded;
        if (BookingStateRules.RemainingCapacity(departure.TotalCapacity, departure.HeldSlots, departure.BookedSlots) == 0)
            departure.Status = "Full";
        departure.UpdatedAt = now;
        var booking = new Bookings
        {
            BookingCode = $"BK{now:yyMMddHHmm}{Guid.NewGuid():N}"[..18].ToUpperInvariant(), TourId = tour.Id, DepartureId = departure.Id,
            CustomerId = customer.Id, CreatedByUserId = current.UserId, SalesUserId = staff ? current.UserId : null,
            BookingChannel = channel, ContactName = request.ContactName.Trim(), ContactPhone = request.ContactPhone.Trim(), ContactEmail = request.ContactEmail?.Trim() ?? "",
            SpecialNote = request.SpecialNote, AdultCount = request.AdultCount, ChildCount = request.ChildCount, InfantCount = request.InfantCount,
            SingleRoomCount = request.SingleRoomCount, SubTotal = price.SubTotal, DiscountAmount = price.Discount, TaxMode = price.TaxMode,
            VatRate = price.VatRate, AmountBeforeTax = price.BeforeTax, VatAmount = price.Vat, TotalAmount = price.Total,
            RequiredDepositAmount = price.Deposit, PaidAmount = 0m, CreditAmount = 0m, PromotionId = promotion?.Id,
            PromoCodeApplied = promotion?.Code, PayOption = request.PayOption == "Deposit" ? "Deposit" : "Full", RequireVatInvoice = request.RequireVatInvoice,
            InvoiceCompanyName = request.RequireVatInvoice ? TrimOrNull(request.InvoiceCompanyName) : null,
            InvoiceTaxCode = request.RequireVatInvoice ? TrimOrNull(request.InvoiceTaxCode) : null,
            InvoiceAddress = request.RequireVatInvoice ? TrimOrNull(request.InvoiceAddress) : null,
            InvoiceEmail = request.RequireVatInvoice ? TrimOrNull(request.InvoiceEmail) : null,
            PriceSnapshotJson = BookingPricing.PriceSnapshot(departure, price, request.AdultCount, request.ChildCount, request.InfantCount, request.SingleRoomCount),
            CancellationPolicySnapshotJson = policySnapshot, Status = "PendingPayment", HoldExpiresAt = now.AddMinutes(Math.Clamp(holdMinutes, 1, 1440)), CreatedAt = now, UpdatedAt = now
        };
        db.Bookings.Add(booking);
        if (promotion is not null) promotion.UsedCount++;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Departure capacity changed; retry the booking."); }
        if (promotion is not null)
            db.PromotionUsages.Add(new PromotionUsages { PromotionId = promotion.Id, BookingId = booking.Id, CustomerId = customer.Id, Status = "Reserved", ReservedAt = now });
        if (request.Passengers is { Count: > 0 })
            db.BookingPassengers.AddRange(ToPassengerEntities(booking.Id, request.Passengers, now));
        if (promotion is not null || request.Passengers is { Count: > 0 })
            await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return booking;
    }

    public async Task<Bookings> GetAsync(long id, CancellationToken ct) => await db.Bookings.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Booking not found.");

    public async Task<BookingDetailsDto> GetDetailsAsync(long id, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Booking not found.");
        return await BuildDetailsDtoAsync(booking, ct);
    }

    public async Task<BookingDetailsDto> BuildDetailsDtoAsync(Bookings booking, CancellationToken ct)
    {
        var meta = await (
            from d in db.TourDepartures.AsNoTracking()
            join t in db.Tours.AsNoTracking() on d.TourId equals t.Id
            where d.Id == booking.DepartureId
            select new { t.Title, t.Code, d.DepartureCode, d.DepartureDate }
        ).SingleOrDefaultAsync(ct);

        var passengers = await db.BookingPassengers.AsNoTracking()
            .Where(x => x.BookingId == booking.Id)
            .OrderBy(x => x.Id)
            .Select(p => new PassengerDto(p.Id, p.BookingId, p.PaxType, p.FullName, p.Gender, p.DateOfBirth,
                p.IdentityNumber, p.PassportExpiryDate, p.Nationality, p.SpecialRequest, p.CheckInStatus, p.CheckedInAt))
            .ToListAsync(ct);

        var payments = await db.PaymentTransactions.AsNoTracking()
            .Where(x => x.BookingId == booking.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(p => new BookingPaymentDto(p.Id, p.TransactionCode, p.Method, p.ExpectedAmount,
                p.ActualReceivedAmount, p.Status, p.ReconciliationStatus, p.PaidAt, p.CreatedAt))
            .ToListAsync(ct);

        var seats = BookingStateRules.SeatsCount(booking.AdultCount, booking.ChildCount);
        var remaining = Math.Max(0m, booking.TotalAmount - booking.PaidAmount);
        return new BookingDetailsDto(
            booking.Id, booking.BookingCode, booking.TourId, booking.DepartureId, booking.CustomerId,
            booking.CreatedByUserId, booking.SalesUserId, booking.BookingChannel, booking.ContactName,
            booking.ContactPhone, booking.ContactEmail, booking.SpecialNote, booking.AdultCount,
            booking.ChildCount, booking.InfantCount, booking.SingleRoomCount, seats, booking.SubTotal,
            booking.DiscountAmount, booking.TaxMode, booking.VatRate, booking.AmountBeforeTax,
            booking.VatAmount, booking.TotalAmount, booking.RequiredDepositAmount, booking.PaidAmount,
            booking.CreditAmount, remaining, booking.PromotionId, booking.PromoCodeApplied,
            booking.PayOption, booking.RequireVatInvoice, booking.InvoiceCompanyName,
            booking.InvoiceTaxCode, booking.InvoiceAddress, booking.InvoiceEmail,
            booking.PriceSnapshotJson, booking.CancellationPolicySnapshotJson, booking.Status,
            booking.HoldExpiresAt, booking.FinalPaymentDueAt, booking.CancellationReason,
            booking.CancelPenaltyPercent, booking.CancelPenaltyAmount, booking.CancelledAt,
            booking.CreatedAt, booking.UpdatedAt, meta?.Title ?? "", meta?.Code ?? "",
            meta?.DepartureCode ?? "", meta?.DepartureDate ?? default, passengers, payments);
    }

    public async Task AdjustAsync(long id, AdjustPaxRequest r, CancellationToken ct)
    {
        ValidatePax(r.AdultCount, r.ChildCount, r.InfantCount, r.SingleRoomCount);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var b = await GetAsync(id, ct); EnsureEditable(b);
        var d = await db.TourDepartures.SingleAsync(x => x.Id == b.DepartureId, ct);
        if (d.CutoffAt < UtcNow) throw new ConflictException("Departure cutoff has passed.");
        if (r.InfantCount > (d.MaxInfants ?? r.AdultCount)) throw new BadRequestException("Infant count exceeds the departure limit.");
        var old = new { b.AdultCount, b.ChildCount, b.InfantCount, b.SingleRoomCount };
        var delta = BookingStateRules.SeatsCount(r.AdultCount, r.ChildCount) - BookingStateRules.SeatsCount(b.AdultCount, b.ChildCount);
        var heldBooking = b.Status == "PendingPayment";
        if (!BookingStateRules.CanAdjust(d.TotalCapacity, d.HeldSlots, d.BookedSlots, delta)) throw new ConflictException("Not enough departure capacity.");
        if ((heldBooking ? d.HeldSlots : d.BookedSlots) + delta < 0) throw new ConflictException("Booking seat allocation is inconsistent with the departure.");
        if (heldBooking) d.HeldSlots += delta; else d.BookedSlots += delta;
        var remaining = BookingStateRules.RemainingCapacity(d.TotalCapacity, d.HeldSlots, d.BookedSlots);
        if (remaining > 0 && d.Status == "Full") d.Status = "Open"; else if (remaining == 0) d.Status = "Full";
        d.UpdatedAt = UtcNow;
        ApplyCounts(b, r.AdultCount, r.ChildCount, r.InfantCount, r.SingleRoomCount);
        Reprice(b, d, await settings.GetDecimalAsync("VatRatePercent", 8m, ct), await settings.GetStringAsync("TaxMode", "Included", ct), await settings.GetDecimalAsync("DefaultDepositPercent", 30m, ct));
        b.UpdatedAt = UtcNow;
        await SaveConcurrency(ct); await audit.WriteAsync("AdjustPax", "Bookings", b.Id.ToString(), old, r, current.UserId, cancellationToken: ct); await tx.CommitAsync(ct);
    }

    public async Task RescheduleAsync(long id, RescheduleRequest r, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var b = await GetAsync(id, ct); EnsureEditable(b);
        if (r.NewDepartureId == b.DepartureId) throw new BadRequestException("New departure must differ from the current departure.");
        var oldDeparture = await db.TourDepartures.SingleAsync(x => x.Id == b.DepartureId, ct);
        var next = await db.TourDepartures.SingleOrDefaultAsync(x => x.Id == r.NewDepartureId, ct) ?? throw new NotFoundException("New departure not found.");
        var newTour = await db.Tours.AsNoTracking().SingleAsync(x => x.Id == next.TourId, ct);
        using var policyDoc = JsonDocument.Parse(b.CancellationPolicySnapshotJson);
        var policy = policyDoc.RootElement;
        if (policy.TryGetProperty("allowReschedule", out var allow) && !allow.GetBoolean()) throw new ConflictException("Rescheduling is not allowed by the booking policy.");
        var daysBefore = oldDeparture.DepartureDate.DayNumber - DateOnly.FromDateTime(UtcNow).DayNumber;
        if (policy.TryGetProperty("rescheduleCutoffDays", out var cutoff) && daysBefore < cutoff.GetInt32()) throw new ConflictException("Reschedule cutoff has passed.");
        var seats = BookingStateRules.SeatsCount(b.AdultCount, b.ChildCount);
        if (oldDeparture.CutoffAt < UtcNow || next.CutoffAt < UtcNow || next.Status != "Open" ||
            !BookingStateRules.CanReserve(next.TotalCapacity, next.HeldSlots, next.BookedSlots, seats)) throw new ConflictException("Departure cannot be rescheduled.");
        if (b.InfantCount > (next.MaxInfants ?? b.AdultCount)) throw new ConflictException("The new departure cannot accommodate this infant count.");
        var prior = b.TotalAmount;
        var heldBooking = b.Status == "PendingPayment";
        if (heldBooking) oldDeparture.HeldSlots = Math.Max(0, oldDeparture.HeldSlots - seats); else oldDeparture.BookedSlots = Math.Max(0, oldDeparture.BookedSlots - seats);
        if (oldDeparture.Status == "Full" && BookingStateRules.RemainingCapacity(oldDeparture.TotalCapacity, oldDeparture.HeldSlots, oldDeparture.BookedSlots) > 0) oldDeparture.Status = "Open";
        oldDeparture.UpdatedAt = UtcNow;
        if (heldBooking) next.HeldSlots += seats; else next.BookedSlots += seats;
        if (BookingStateRules.RemainingCapacity(next.TotalCapacity, next.HeldSlots, next.BookedSlots) == 0) next.Status = "Full";
        next.UpdatedAt = UtcNow;
        var oldId = b.DepartureId; b.DepartureId = next.Id; b.TourId = next.TourId;
        var depositPercent = await settings.GetDecimalAsync("DefaultDepositPercent", 30m, ct);
        Reprice(b, next, b.VatRate, b.TaxMode, depositPercent);
        var feePercent = policy.TryGetProperty("rescheduleFeePercent", out var feeElement) ? feeElement.GetDecimal() : 0m;
        var fee = Math.Round(prior * feePercent / 100m, 2, MidpointRounding.AwayFromZero);
        if (fee > 0m)
        {
            b.SubTotal += fee;
            var net = b.SubTotal - b.DiscountAmount;
            if (b.TaxMode == "Included")
            {
                b.AmountBeforeTax = b.VatRate == 0 ? net : Math.Round(net / (1m + b.VatRate / 100m), 2, MidpointRounding.AwayFromZero);
                b.VatAmount = net - b.AmountBeforeTax; b.TotalAmount = net;
            }
            else
            {
                b.AmountBeforeTax = net; b.VatAmount = Math.Round(net * b.VatRate / 100m, 2, MidpointRounding.AwayFromZero); b.TotalAmount = net + b.VatAmount;
            }
            b.RequiredDepositAmount = Math.Round(b.TotalAmount * depositPercent / 100m, 2, MidpointRounding.AwayFromZero);
            using var snapshot = JsonDocument.Parse(b.PriceSnapshotJson);
            b.PriceSnapshotJson = JsonSerializer.Serialize(new { price = snapshot.RootElement.Clone(), rescheduleFee = fee, newTotal = b.TotalAmount }, JsonOptions);
        }
        b.CancellationPolicySnapshotJson = await BuildPolicySnapshotAsync(next.CancellationPolicyId ?? newTour.CancellationPolicyId, ct);
        if (b.FinalPaymentDueAt.HasValue || b.PayOption == "Deposit")
        {
            var finalDays = await settings.GetIntAsync("FinalPaymentDueDaysBeforeDeparture", 5, ct);
            b.FinalPaymentDueAt = next.DepartureDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(-Math.Clamp(finalDays, 0, 365));
        }
        var diff = b.TotalAmount - prior;
        if (diff < 0) b.CreditAmount += -diff;
        var rec = new BookingReschedules { BookingId = b.Id, OldDepartureId = oldId, NewDepartureId = next.Id, OldTotalAmount = prior, NewTotalAmount = b.TotalAmount, RescheduleFee = fee, PriceDifference = diff, Reason = r.Reason, ApprovedByUserId = current.UserId, CreatedAt = UtcNow };
        db.BookingReschedules.Add(rec); b.UpdatedAt = UtcNow;
        await SaveConcurrency(ct); await audit.WriteAsync("Reschedule", "Bookings", b.Id.ToString(), new { DepartureId = oldId, Total = prior }, new { DepartureId = next.Id, Total = b.TotalAmount, diff }, current.UserId, cancellationToken: ct); await tx.CommitAsync(ct);
    }

    public async Task RequestCancelAsync(long id, string reason, CancellationToken ct)
    {
        var b = await GetAsync(id, ct); EnsureOwnerOrStaff(b);
        if (!BookingStateRules.CanRequestCancellation(b.Status)) throw new ConflictException("Booking cannot be cancelled in its current state.");
        var d = await db.TourDepartures.SingleAsync(x => x.Id == b.DepartureId, ct);
        var penalty = Modules.Finance.FinanceRules.CalculatePenaltyFromSnapshot(b, d.DepartureDate, UtcNow);
        b.CancelPenaltyAmount = penalty; b.CancelPenaltyPercent = b.TotalAmount == 0 ? 0 : Math.Round(penalty * 100m / b.TotalAmount, 2);
        b.CancellationReason = reason; b.Status = "CancellationRequested"; b.UpdatedAt = UtcNow;
        await db.SaveChangesAsync(ct); await audit.WriteAsync("CancelRequest", "Bookings", b.Id.ToString(), null, new { reason, penalty }, current.UserId, cancellationToken: ct);
    }

    public async Task ApproveCancelAsync(long id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var b = await GetAsync(id, ct); if (b.Status != "CancellationRequested") throw new ConflictException("Cancellation was not requested.");
        var d = await db.TourDepartures.SingleAsync(x => x.Id == b.DepartureId, ct);
        var seats = BookingStateRules.SeatsCount(b.AdultCount, b.ChildCount);
        var inBookedSlots = BookingStateRules.ShouldReleaseBookedSlots(b.PaidAmount, b.RequiredDepositAmount);
        if (inBookedSlots) d.BookedSlots = Math.Max(0, d.BookedSlots - seats); else d.HeldSlots = Math.Max(0, d.HeldSlots - seats);
        if (d.Status == "Full" && BookingStateRules.RemainingCapacity(d.TotalCapacity, d.HeldSlots, d.BookedSlots) > 0) d.Status = "Open";
        d.UpdatedAt = UtcNow;
        b.Status = "Cancelled"; b.CancelledAt = UtcNow; b.CancelApprovedByUserId = current.UserId; b.UpdatedAt = UtcNow;
        await ReleasePromotion(b.Id, ct); await SaveConcurrency(ct);
        await audit.WriteAsync("ApproveCancel", "Bookings", b.Id.ToString(), null, new { b.CancelPenaltyAmount }, current.UserId, cancellationToken: ct);
        await tx.CommitAsync(ct);
    }

    public static void ValidateManifest(IReadOnlyList<PassengerRequest> passengers, int adults, int children, int infants)
    {
        var expected = adults + children + infants;
        if (passengers.Count != expected || passengers.Count(x => x.PaxType == "Adult") != adults ||
            passengers.Count(x => x.PaxType == "Child") != children || passengers.Count(x => x.PaxType == "Infant") != infants)
            throw new BadRequestException("Passenger manifest must match the booking counts.");
        if (passengers.Any(x => string.IsNullOrWhiteSpace(x.FullName)))
            throw new BadRequestException("Each passenger must have a full name.");
    }

    internal static IEnumerable<BookingPassengers> ToPassengerEntities(long bookingId, IReadOnlyList<PassengerRequest> passengers, DateTime now) =>
        passengers.Select(p => new BookingPassengers
        {
            BookingId = bookingId, PaxType = p.PaxType, FullName = p.FullName.Trim(),
            Nationality = string.IsNullOrWhiteSpace(p.Nationality) ? "VN" : p.Nationality.Trim(),
            Gender = p.Gender, DateOfBirth = p.DateOfBirth, IdentityNumber = p.IdentityNumber,
            PassportExpiryDate = p.PassportExpiryDate, SpecialRequest = p.SpecialRequest,
            CheckInStatus = "Pending", CreatedAt = now, UpdatedAt = now
        });

    private async Task<string> BuildPolicySnapshotAsync(int policyId, CancellationToken ct)
    {
        var policy = await db.CancellationPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == policyId, ct);
        var rules = await db.CancellationPolicyRules.AsNoTracking().Where(x => x.PolicyId == policyId).OrderBy(x => x.SortOrder).ToListAsync(ct);
        return JsonSerializer.Serialize(new
        {
            policyId, policyName = policy?.Name, policy?.AllowReschedule, policy?.RescheduleCutoffDays, policy?.RescheduleFeePercent,
            rules = rules.Select(x => new { x.MinDaysBeforeDeparture, x.MaxDaysBeforeDeparture, x.PenaltyPercent, x.Label })
        }, JsonOptions);
    }

    private bool IsStaff => current.Roles.Any(x => x is "SALES" or "OPERATOR" or "ADMIN" or "ACCOUNTANT");
    private void EnsureOwnerOrStaff(Bookings b) { if (!IsStaff && current.CustomerId != b.CustomerId) throw new ForbiddenException("Booking does not belong to this customer."); }
    private void EnsureEditable(Bookings b) { if (!BookingStateRules.CanEdit(b.Status)) throw new ConflictException("Booking cannot be changed in its current state."); }
    private static void ValidatePax(int a, int c, int i, int s) { if (a < 1 || c < 0 || i < 0 || s < 0) throw new BadRequestException("Passenger counts are invalid."); }
    private static string NormalizeChannel(string? channel) => string.IsNullOrWhiteSpace(channel) ? "WebB2C" : channel switch { "WalkIn" or "Zalo" or "Hotline" or "Facebook" or "Corporate" => channel, "Web" or "WebB2C" => "WebB2C", _ => throw new BadRequestException("Unsupported booking channel.") };
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<Customers> FindOrCreateCustomer(CreateBookingRequest r, string channel, bool staff, CancellationToken ct)
    {
        if (!staff && current.CustomerId is int authenticatedCustomerId)
        {
            var claimed = await db.Customers.SingleOrDefaultAsync(x => x.Id == authenticatedCustomerId, ct);
            if (claimed is not null)
            {
                if (claimed.UserId is null && current.UserId is int uid) claimed.UserId = uid;
                return claimed;
            }
        }
        var email = string.IsNullOrWhiteSpace(r.ContactEmail) ? null : r.ContactEmail.Trim(); var phone = r.ContactPhone.Trim();
        var matches = await db.Customers.Where(x => x.Phone == phone || (email != null && x.Email == email)).ToListAsync(ct);
        var customer = CustomerProfileMatcher.FindMatch(matches, email, phone);
        if (customer is null)
        {
            customer = new Customers
            {
                CustomerCode = "CUS" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
                UserId = !staff ? current.UserId : null,
                FullName = r.ContactName.Trim(), Phone = phone, Email = email,
                Source = channel == "WebB2C" ? "Web" : channel, LoyaltyTier = "Standard",
                CreatedAt = UtcNow, UpdatedAt = UtcNow
            };
            db.Customers.Add(customer); await db.SaveChangesAsync(ct);
        }
        else if (!staff && customer.UserId is null && current.UserId is int uid)
        {
            customer.UserId = uid; customer.UpdatedAt = UtcNow;
        }
        return customer;
    }
    private static void ApplyCounts(Bookings b, int a, int c, int i, int s) { b.AdultCount = a; b.ChildCount = c; b.InfantCount = i; b.SingleRoomCount = s; }
    private static void Reprice(Bookings b, TourDepartures d, decimal vat, string mode, decimal depositPercent)
    {
        var p = BookingPricing.Calculate(d, b.AdultCount, b.ChildCount, b.InfantCount, b.SingleRoomCount, b.DiscountAmount, vat, mode, depositPercent);
        b.SubTotal = p.SubTotal; b.DiscountAmount = p.Discount; b.AmountBeforeTax = p.BeforeTax; b.VatAmount = p.Vat; b.TotalAmount = p.Total; b.RequiredDepositAmount = p.Deposit;
        b.PriceSnapshotJson = BookingPricing.PriceSnapshot(d, p, b.AdultCount, b.ChildCount, b.InfantCount, b.SingleRoomCount);
    }
    private async Task SaveConcurrency(CancellationToken ct) { try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ConflictException("Departure capacity changed; retry the operation."); } }
    private async Task ReleasePromotion(long bookingId, CancellationToken ct)
    {
        var usage = await db.PromotionUsages.SingleOrDefaultAsync(x => x.BookingId == bookingId && x.Status == "Reserved", ct);
        if (usage is null) return; usage.Status = "Released"; usage.ReleasedAt = UtcNow;
        var promo = await db.Promotions.SingleAsync(x => x.Id == usage.PromotionId, ct); promo.UsedCount = Math.Max(0, promo.UsedCount - 1);
    }
}
