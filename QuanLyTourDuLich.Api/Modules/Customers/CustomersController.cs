using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;
using CustomerEntity = QuanLyTourDuLich.Api.Models.Entities.Customers;

namespace QuanLyTourDuLich.Api.Modules.Crm;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PolicyNames.RequireSalesOrAdmin)]
    public async Task<ActionResult<PagedResult<CustomerListItem>>> List(
        [FromQuery] PagedRequest request,
        [FromQuery] string? loyaltyTier,
        CancellationToken cancellationToken)
    {
        var query = db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(c => c.Phone.Contains(search) ||
                (c.Email != null && c.Email.Contains(search)) || c.FullName.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(loyaltyTier))
        {
            var tier = loyaltyTier.Trim();
            query = query.Where(c => c.LoyaltyTier == tier);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(c => c.Id)
            .Skip((request.NormalizedPageIndex - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .Select(c => new CustomerListItem(c.Id, c.CustomerCode, c.UserId, c.FullName, c.Phone,
                c.Email, c.Source, c.LoyaltyTier, c.TotalSpent, c.TotalBookings))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResult<CustomerListItem>(items, request.NormalizedPageIndex, request.NormalizedPageSize, total));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireSalesOrAdmin)]
    public async Task<ActionResult<CustomerDetails>> Get(int id, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking().Where(c => c.Id == id)
            .Select(c => new CustomerDetails(c.Id, c.CustomerCode, c.UserId, c.FullName, c.Phone,
                c.Email, c.Address, c.Source, c.LoyaltyTier, c.TotalSpent, c.TotalBookings, c.Notes,
                c.CreatedAt, c.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Customer not found.");
        return Ok(customer);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireSalesOrAdmin)]
    public async Task<ActionResult<CustomerDetails>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var fullName = request.FullName.Trim();
        var phone = request.Phone.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var source = request.Source.Trim();
        if (fullName.Length is < 1 or > 120 || phone.Length is < 1 or > 20 || email?.Length > 150 ||
            request.Address?.Length > 300 || request.Notes?.Length > 1000 ||
            !CustomerSources.Contains(source, StringComparer.Ordinal))
            throw new BadRequestException("Invalid customer fields or source.");
        if (request.UserId.HasValue && !await db.Users.AnyAsync(u => u.Id == request.UserId.Value, cancellationToken))
            throw new BadRequestException("The linked user does not exist.");
        if (await db.Customers.AnyAsync(c => c.Phone == phone ||
                (email != null && c.Email == email) || (request.UserId != null && c.UserId == request.UserId), cancellationToken))
            throw new ConflictException("A customer already exists for the phone, email, or linked user.");

        var now = DateTime.UtcNow;
        var customer = new CustomerEntity
        {
            CustomerCode = $"CUS-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            UserId = request.UserId,
            FullName = fullName,
            Phone = phone,
            Email = email,
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Source = source,
            LoyaltyTier = "Standard",
            TotalSpent = 0,
            TotalBookings = 0,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Customers.Add(customer);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new ConflictException("A customer already exists for the phone, email, or linked user."); }
        var result = ToDetails(customer);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireSalesOrAdmin)]
    public async Task<ActionResult<CustomerDetails>> Update(int id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Customer not found.");
        var fullName = request.FullName.Trim();
        var phone = request.Phone.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var source = request.Source.Trim();
        var tier = request.LoyaltyTier.Trim();
        if (fullName.Length is < 1 or > 120 || phone.Length is < 1 or > 20 || email?.Length > 150 ||
            request.Address?.Length > 300 || request.Notes?.Length > 1000 ||
            !CustomerSources.Contains(source, StringComparer.Ordinal) || !LoyaltyTiers.Contains(tier, StringComparer.Ordinal))
            throw new BadRequestException("Invalid customer fields, source, or loyalty tier.");
        if (await db.Customers.AnyAsync(c => c.Id != id && (c.Phone == phone || (email != null && c.Email == email)), cancellationToken))
            throw new ConflictException("Another customer already uses the phone or email.");
        customer.FullName = fullName;
        customer.Phone = phone;
        customer.Email = email;
        customer.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        customer.Source = source;
        customer.LoyaltyTier = tier;
        customer.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        customer.UpdatedAt = DateTime.UtcNow;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new ConflictException("Another customer already uses the phone or email."); }
        return Ok(ToDetails(customer));
    }

    [HttpGet("{id:int}/bookings")]
    [Authorize(Policy = PolicyNames.RequireSalesOrAdmin)]
    public async Task<ActionResult<PagedResult<CustomerBookingItem>>> Bookings(
        int id, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == id, cancellationToken))
            throw new NotFoundException("Customer not found.");
        var query = db.Bookings.AsNoTracking().Where(b => b.CustomerId == id);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(b => b.CreatedAt)
            .Skip((request.NormalizedPageIndex - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .Select(b => new CustomerBookingItem(b.Id, b.BookingCode, b.TourId, b.DepartureId,
                b.Status, b.TotalAmount, b.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResult<CustomerBookingItem>(items, request.NormalizedPageIndex, request.NormalizedPageSize, total));
    }

    private static CustomerDetails ToDetails(CustomerEntity c) => new(c.Id, c.CustomerCode, c.UserId, c.FullName,
        c.Phone, c.Email, c.Address, c.Source, c.LoyaltyTier, c.TotalSpent, c.TotalBookings, c.Notes, c.CreatedAt, c.UpdatedAt);

    private static readonly string[] CustomerSources = ["Web", "WalkIn", "Zalo", "Facebook", "Hotline", "Corporate"];
    private static readonly string[] LoyaltyTiers = ["Standard", "Silver", "Gold", "VIP"];
}

public sealed record CreateCustomerRequest(string FullName, string Phone, string? Email, string? Address, string Source, int? UserId = null, string? Notes = null);
public sealed record UpdateCustomerRequest(string FullName, string Phone, string? Email, string? Address, string Source, string LoyaltyTier, string? Notes = null);
public sealed record CustomerListItem(int Id, string CustomerCode, int? UserId, string FullName, string Phone, string? Email, string Source, string LoyaltyTier, decimal TotalSpent, int TotalBookings);
public sealed record CustomerDetails(int Id, string CustomerCode, int? UserId, string FullName, string Phone, string? Email, string? Address, string Source, string LoyaltyTier, decimal TotalSpent, int TotalBookings, string? Notes, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record CustomerBookingItem(long Id, string BookingCode, int TourId, int DepartureId, string Status, decimal TotalAmount, DateTime CreatedAt);
