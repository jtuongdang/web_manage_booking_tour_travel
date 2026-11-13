using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Payments;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController(
    AppDbContext db,
    IPaymentGateway gateway,
    IFinancialLedgerWriter ledger,
    ISystemSettingService settings,
    ICurrentUserService currentUser,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost("qr-create")]
    public async Task<IActionResult> CreateQr([FromBody] CreateQrRequest request, CancellationToken ct)
    {
        var booking = await FindBooking(request.BookingId, request.BookingCode, ct);
        EnsureBookingAccess(booking);
        var amount = CalculateExpectedAmount(booking, request.Mode);
        if (amount <= 0) throw new BadRequestException("Booking has no remaining amount to pay.");

        var bank = await db.CompanyBankAccounts.AsNoTracking()
            .Where(x => x.IsActive && x.IsDefault)
            .FirstOrDefaultAsync(ct)
            ?? await db.CompanyBankAccounts.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id).FirstOrDefaultAsync(ct)
            ?? throw new ConflictException("No active company bank account is configured.", "bank_account_missing");

        var now = DateTime.UtcNow;
        var code = $"PAY-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant();
        var transfer = $"TOUR {booking.BookingCode}";
        var qr = gateway.GenerateVietQrPayload(bank.BankCode, bank.BankBin, bank.BankName, bank.AccountNumber,
            bank.AccountHolder, amount, transfer);
        var tx = new PaymentTransactions
        {
            TransactionCode = code, BookingId = booking.Id, CompanyBankAccountId = bank.Id,
            Method = "QR_SIMULATED", TransferContent = transfer, QrPayload = qr.QrContent,
            ExpectedAmount = amount, ActualReceivedAmount = 0, Status = "Pending",
            ReconciliationStatus = "Unreconciled", ExpiresAt = now.AddMinutes(15), CreatedAt = now
        };
        db.PaymentTransactions.Add(tx);
        await db.SaveChangesAsync(ct);
        return Ok(new { tx.Id, tx.TransactionCode, tx.ExpectedAmount, tx.ExpiresAt, tx.Status, Qr = qr });
    }

    [HttpGet("{txCode}/status")]
    public async Task<IActionResult> GetStatus(string txCode, CancellationToken ct)
    {
        var tx = await db.PaymentTransactions.FirstOrDefaultAsync(x => x.TransactionCode == txCode, ct)
            ?? throw new NotFoundException("Payment transaction was not found.");
        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tx.BookingId, ct)
            ?? throw new NotFoundException("Booking was not found.");
        EnsureBookingAccess(booking);
        if (tx.Status == "Pending" && tx.ExpiresAt <= DateTime.UtcNow)
        {
            tx.Status = "Expired";
            await db.SaveChangesAsync(ct);
        }
        return Ok(new { tx.TransactionCode, tx.Status, tx.ReconciliationStatus, tx.ExpectedAmount,
            tx.ActualReceivedAmount, tx.ExpiresAt, tx.PaidAt });
    }

    [HttpPost("qr-simulate-webhook")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> SimulateWebhook([FromBody] SimulateWebhookRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new BadRequestException("IdempotencyKey is required.");
        if (request.Scenario is not null && !new[] { "Success", "Partial", "Overpaid", "Failed" }
                .Contains(request.Scenario, StringComparer.OrdinalIgnoreCase))
            throw new BadRequestException("Scenario must be Success, Partial, Overpaid or Failed.");
        if (!gateway.VerifyWebhookSignature(request.TransactionCode, request.PaidAmount, request.MockSignature))
            throw new UnauthorizedException("Mock webhook signature is invalid.", "invalid_mock_signature");

        return await ProcessWebhookCoreAsync(request, ct);
    }

    /// <summary>
    /// Development-only simulator convenience endpoint so Customer/Staff UI demo flows can trigger VietQR callbacks
    /// without exposing the webhook secret to client code.
    /// </summary>
    [HttpPost("qr-simulate-demo")]
    public async Task<IActionResult> SimulateDemoWebhook([FromBody] SimulateDemoWebhookRequest request, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            throw new ForbiddenException("Demo simulator endpoint is only available in Development environment.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new BadRequestException("IdempotencyKey is required.");
        if (request.Scenario is not null && !new[] { "Success", "Partial", "Overpaid", "Failed" }
                .Contains(request.Scenario, StringComparer.OrdinalIgnoreCase))
            throw new BadRequestException("Scenario must be Success, Partial, Overpaid or Failed.");

        var txLookup = await db.PaymentTransactions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TransactionCode == request.TransactionCode, ct)
            ?? throw new NotFoundException("Payment transaction was not found.");
        var booking = await db.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == txLookup.BookingId, ct)
            ?? throw new NotFoundException("Booking was not found.");
        EnsureBookingAccess(booking);

        var paidAmount = request.PaidAmount ?? txLookup.ExpectedAmount;
        var sig = VietQrPaymentGateway.CreateMockSignature(request.TransactionCode, paidAmount);
        return await ProcessWebhookCoreAsync(new SimulateWebhookRequest(
            request.TransactionCode, paidAmount, request.Scenario ?? "Success", request.IdempotencyKey, sig), ct);
    }

    private async Task<IActionResult> ProcessWebhookCoreAsync(SimulateWebhookRequest request, CancellationToken ct)
    {
        var replay = await db.PaymentTransactions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null)
        {
            if (PaymentRules.GetIdempotencyResult(replay, request.TransactionCode) == IdempotencyResult.KeyReused)
                throw new ConflictException("IdempotencyKey was already used for another transaction.", "idempotency_key_reused");
            return Ok(new { replay.TransactionCode, replay.Status, replay.ActualReceivedAmount, IdempotentReplay = true });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var tx = await db.PaymentTransactions.FirstOrDefaultAsync(x => x.TransactionCode == request.TransactionCode, ct)
            ?? throw new NotFoundException("Payment transaction was not found.");
        if (tx.Status != "Pending") throw new ConflictException("Payment transaction is no longer pending.");
        if (tx.ExpiresAt <= DateTime.UtcNow)
        {
            tx.Status = "Expired";
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Conflict(new { message = "Payment transaction has expired." });
        }
        tx.IdempotencyKey = request.IdempotencyKey;
        tx.RawCallbackJson = JsonSerializer.Serialize(new { request.TransactionCode, request.PaidAmount, request.Scenario });
        var scenario = request.Scenario?.Trim();
        var received = scenario?.Equals("Failed", StringComparison.OrdinalIgnoreCase) == true ? 0 : request.PaidAmount;
        if (received < 0) throw new BadRequestException("PaidAmount cannot be negative.");
        tx.ActualReceivedAmount = received;
        tx.PaidAt = received > 0 ? DateTime.UtcNow : null;

        if (scenario?.Equals("Failed", StringComparison.OrdinalIgnoreCase) == true)
        {
            tx.Status = "Failed";
        }
        else if (PaymentRules.GetCallbackStatus(tx.ExpectedAmount, received) == "NeedReconciliation")
        {
            tx.Status = "NeedReconciliation";
            tx.ReconciliationStatus = "Discrepancy";
        }
        else
        {
            tx.Status = "Succeeded";
            tx.ReconciliationStatus = "Matched";
            await ApplyReceivedAmountAsync(tx, received, ct);
        }

        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            await transaction.RollbackAsync(ct);
            throw new ConflictException("IdempotencyKey or bank reference has already been used.", "duplicate_payment_reference");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new ConflictException("Payment or booking changed concurrently; retry with a new idempotency key.", "payment_concurrency_conflict");
        }
        return Ok(new { tx.TransactionCode, tx.Status, tx.ActualReceivedAmount, IdempotentReplay = false });
    }

    [HttpPost("manual-record")]
    [Authorize(Roles = RoleCodes.Sales + "," + RoleCodes.Accountant + "," + RoleCodes.Admin)]
    public async Task<IActionResult> RecordManualPayment([FromBody] ManualPaymentRequest request, CancellationToken ct)
    {
        if (request.Method is not ("CASH" or "BANK_TRANSFER_MANUAL")) throw new BadRequestException("Method must be CASH or BANK_TRANSFER_MANUAL.");
        if (request.Amount <= 0) throw new BadRequestException("Amount must be positive.");
        var booking = await FindBooking(request.BookingId, request.BookingCode, ct);
        EnsureBookingAccess(booking);
        if (request.Method == "BANK_TRANSFER_MANUAL" && (string.IsNullOrWhiteSpace(request.BankCode) || string.IsNullOrWhiteSpace(request.BankRef)))
            throw new BadRequestException("BankCode and BankRef are required for a manual bank transfer.");
        var tx = new PaymentTransactions
        {
            TransactionCode = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant(),
            BookingId = booking.Id, Method = request.Method, ExpectedAmount = request.Amount,
            ActualReceivedAmount = request.Amount, Status = "Succeeded", ReconciliationStatus = "Matched",
            BankCode = request.BankCode, BankRef = request.BankRef, EvidenceUrl = request.EvidenceUrl,
            ReconciliationNote = request.Note, ReconciledByUserId = currentUser.UserId,
            ReconciledAt = DateTime.UtcNow, PaidAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        };
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var matchingRefs = string.IsNullOrWhiteSpace(request.BankRef) ? new List<PaymentTransactions>() : await db.PaymentTransactions.AsNoTracking()
            .Where(x => x.BankCode == request.BankCode && x.BankRef == request.BankRef).ToListAsync(ct);
        if (PaymentRules.HasDuplicateBankReference(matchingRefs, tx.Id, request.BankCode, request.BankRef))
            throw new ConflictException("Bank statement reference already belongs to another payment.", "duplicate_bank_reference");
        db.PaymentTransactions.Add(tx);
        try
        {
            await db.SaveChangesAsync(ct);
            await ApplyReceivedAmountAsync(tx, tx.ActualReceivedAmount, ct, booking);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            await transaction.RollbackAsync(ct);
            throw new ConflictException("Bank statement reference already belongs to another payment.", "duplicate_bank_reference");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new ConflictException("Payment or booking changed concurrently.", "payment_concurrency_conflict");
        }
        return CreatedAtAction(nameof(GetStatus), new { txCode = tx.TransactionCode }, new { tx.Id, tx.TransactionCode, tx.Status });
    }

    [HttpGet("reconciliation-queue")]
    [Authorize(Policy = PolicyNames.RequireAccountantOrAdmin)]
    public async Task<IActionResult> ReconciliationQueue(CancellationToken ct)
    {
        var items = await db.PaymentTransactions.AsNoTracking()
            .Where(x => x.Status == "NeedReconciliation" || x.ReconciliationStatus == "Discrepancy")
            .OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.TransactionCode, x.BookingId, x.ExpectedAmount,
                x.ActualReceivedAmount, x.Status, x.ReconciliationStatus, x.BankCode, x.BankRef, x.CreatedAt }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("{id:long}/reconcile")]
    [Authorize(Policy = PolicyNames.RequireAccountantOrAdmin)]
    public async Task<IActionResult> Reconcile(long id, [FromBody] ReconcilePaymentRequest request, CancellationToken ct)
    {
        if (request.ActualReceivedAmount <= 0) throw new BadRequestException("ActualReceivedAmount must be positive.");
        if (string.IsNullOrWhiteSpace(request.BankCode) || string.IsNullOrWhiteSpace(request.BankRef))
            throw new BadRequestException("BankCode and BankRef are required.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var tx = await db.PaymentTransactions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Payment transaction was not found.");
        if (tx.Status is not ("NeedReconciliation" or "Pending") && tx.ReconciliationStatus != "Discrepancy")
            throw new ConflictException("Payment transaction is not awaiting reconciliation.");
        var matchingRefs = await db.PaymentTransactions.AsNoTracking()
            .Where(x => x.BankCode == request.BankCode && x.BankRef == request.BankRef).ToListAsync(ct);
        if (PaymentRules.HasDuplicateBankReference(matchingRefs, id, request.BankCode, request.BankRef))
            throw new ConflictException("Bank statement reference already belongs to another payment.", "duplicate_bank_reference");
        tx.BankCode = request.BankCode.Trim(); tx.BankRef = request.BankRef.Trim();
        tx.ActualReceivedAmount = request.ActualReceivedAmount;
        tx.EvidenceUrl = request.EvidenceUrl; tx.ReconciliationNote = request.ReconciliationNote;
        tx.ReconciledByUserId = currentUser.UserId; tx.ReconciledAt = DateTime.UtcNow;
        tx.PaidAt ??= DateTime.UtcNow;
        tx.Status = "Succeeded"; tx.ReconciliationStatus = "Resolved";
        await ApplyReceivedAmountAsync(tx, request.ActualReceivedAmount, ct);
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            await transaction.RollbackAsync(ct);
            throw new ConflictException("Bank statement reference already belongs to another payment.", "duplicate_bank_reference");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new ConflictException("Payment or booking changed concurrently.", "payment_concurrency_conflict");
        }
        return Ok(new { tx.Id, tx.Status, tx.ReconciliationStatus, tx.ActualReceivedAmount });
    }

    private async Task ApplyReceivedAmountAsync(PaymentTransactions tx, decimal amount, CancellationToken ct, Bookings? knownBooking = null)
    {
        var booking = knownBooking ?? await db.Bookings.FirstOrDefaultAsync(x => x.Id == tx.BookingId, ct)
            ?? throw new NotFoundException("Booking was not found.");
        if (booking.Status is "Cancelled" or "Expired" or "Refunded") throw new ConflictException("Booking cannot receive payments in its current state.");
        if (amount <= 0) return;
        var departure = await db.TourDepartures.FirstOrDefaultAsync(x => x.Id == booking.DepartureId, ct)
            ?? throw new NotFoundException("Tour departure was not found.");
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == booking.CustomerId, ct)
            ?? throw new NotFoundException("Customer was not found.");
        var appliedToBooking = Math.Min(amount, Math.Max(0, booking.TotalAmount - booking.PaidAmount));
        if (amount > appliedToBooking)
        {
            var excess = amount - appliedToBooking;
            tx.ReconciliationNote = string.IsNullOrWhiteSpace(tx.ReconciliationNote)
                ? $"Overpayment {excess:0.00} requires refund handling."
                : $"{tx.ReconciliationNote} | Overpayment {excess:0.00} requires refund handling.";
        }
        booking.PaidAmount += appliedToBooking;
        booking.UpdatedAt = DateTime.UtcNow;
        var fullyPaid = booking.PaidAmount >= booking.TotalAmount;
        if (booking.Status == "PendingPayment" && booking.PaidAmount >= booking.RequiredDepositAmount)
        {
            if (departure.HeldSlots < booking.SeatsReserved || departure.HeldSlots + departure.BookedSlots > departure.TotalCapacity)
                throw new ConflictException("Held capacity is no longer available for this booking.", "held_capacity_conflict");
            departure.HeldSlots -= booking.SeatsReserved;
            departure.BookedSlots += booking.SeatsReserved;
            if (departure.HeldSlots + departure.BookedSlots > departure.TotalCapacity)
                throw new ConflictException("Departure capacity would be exceeded.", "overbooking_prevented");
            if (departure.Status == "Full" && (departure.TotalCapacity - departure.HeldSlots - departure.BookedSlots) > 0) departure.Status = "Open";
            booking.Status = fullyPaid ? "Confirmed" : booking.PaidAmount >= booking.RequiredDepositAmount ? "DepositPaid" : "PendingPayment";
            var finalDays = await settings.GetIntAsync("FinalPaymentDueDaysBeforeDeparture", 5, ct);
            booking.FinalPaymentDueAt = departure.DepartureDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(-Math.Clamp(finalDays, 0, 365));
        }
        else if (fullyPaid) booking.Status = "Confirmed";

        customer.TotalSpent += amount;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.LoyaltyTier = customer.TotalSpent >= 100_000_000m ? "VIP" : customer.TotalSpent >= 50_000_000m ? "Gold" : customer.TotalSpent >= 20_000_000m ? "Silver" : "Standard";
        await ledger.AppendAsync(new FinancialLedgerAppendRequest("In", "CustomerPayment", amount,
            $"Payment {tx.TransactionCode} for booking {booking.BookingCode}", booking.DepartureId, booking.Id, tx.Id,
            RecordedByUserId: currentUser.UserId), saveChanges: false, cancellationToken: ct);
    }

    private async Task<Bookings> FindBooking(long? id, string? code, CancellationToken ct)
    {
        if (id is null && string.IsNullOrWhiteSpace(code)) throw new BadRequestException("BookingId or BookingCode is required.");
        var query = db.Bookings.AsQueryable();
        var booking = id.HasValue
            ? await query.FirstOrDefaultAsync(x => x.Id == id.Value, ct)
            : await query.FirstOrDefaultAsync(x => x.BookingCode == code, ct);
        return booking ?? throw new NotFoundException("Booking was not found.");
    }

    private void EnsureBookingAccess(Bookings booking)
    {
        if (currentUser.Roles.Any(role => role is RoleCodes.Sales or RoleCodes.Operator or RoleCodes.Accountant or RoleCodes.Admin)) return;
        if (currentUser.CustomerId != booking.CustomerId)
            throw new ForbiddenException("You cannot access this booking.");
    }

    private static decimal CalculateExpectedAmount(Bookings booking, string mode)
    {
        var remainingAmount = Math.Max(0m, booking.TotalAmount - booking.PaidAmount);
        return mode switch
        {
            "Deposit" => Math.Min(booking.RequiredDepositAmount, remainingAmount),
            "Remaining" => remainingAmount,
            "Full" => remainingAmount,
            _ => throw new BadRequestException("Mode must be Deposit, Remaining or Full.")
        };
    }

    private static bool IsUniqueConstraint(DbUpdateException exception) => exception.InnerException?.Message.Contains("2601", StringComparison.Ordinal) == true
        || exception.InnerException?.Message.Contains("2627", StringComparison.Ordinal) == true;
}

public sealed record CreateQrRequest(long? BookingId, string? BookingCode, string Mode);
public sealed record SimulateWebhookRequest(string TransactionCode, decimal PaidAmount, string? Scenario, string? IdempotencyKey, string? MockSignature);
public sealed record SimulateDemoWebhookRequest(string TransactionCode, decimal? PaidAmount = null, string? Scenario = "Success", string IdempotencyKey = "");
public sealed record ManualPaymentRequest(long? BookingId, string? BookingCode, string Method, decimal Amount,
    string? BankCode, string? BankRef, string? EvidenceUrl, string? Note);
public sealed record ReconcilePaymentRequest(string BankCode, string BankRef, decimal ActualReceivedAmount,
    string? EvidenceUrl, string? ReconciliationNote);

public enum IdempotencyResult { NewRequest, Replay, KeyReused }

public static class PaymentRules
{
    public static IdempotencyResult GetIdempotencyResult(PaymentTransactions payment, string transactionCode) =>
        string.Equals(payment.TransactionCode, transactionCode, StringComparison.Ordinal)
            ? IdempotencyResult.Replay
            : IdempotencyResult.KeyReused;

    public static bool HasDuplicateBankReference(IEnumerable<PaymentTransactions> payments, long currentId, string? bankCode, string? bankRef) =>
        !string.IsNullOrWhiteSpace(bankRef) && payments.Any(x => x.Id != currentId
            && string.Equals(x.BankCode, bankCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.BankRef, bankRef, StringComparison.OrdinalIgnoreCase));

    public static string GetCallbackStatus(decimal expectedAmount, decimal receivedAmount) =>
        receivedAmount == expectedAmount ? "Succeeded" : "NeedReconciliation";
}
