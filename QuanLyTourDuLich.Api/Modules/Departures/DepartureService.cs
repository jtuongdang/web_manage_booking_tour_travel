using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Departures;

public sealed class DepartureService(AppDbContext db)
{
    public async Task<TourDepartures> GetAsync(int id, bool internalView, CancellationToken ct)
    {
        var query = db.TourDepartures.AsNoTracking().Where(x => x.Id == id);
        if (!internalView) query = query.Where(x => x.IsPublic && x.Status != "Cancelled" && db.Tours.Any(t => t.Id == x.TourId && t.IsPublic && t.Status == "Published"));
        return await query.FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Departure was not found.");
    }

    public async Task EnsureReferencesAsync(DepartureWriteRequest r, CancellationToken ct)
    {
        if (r.ReturnDate < r.DepartureDate) throw new BadRequestException("ReturnDate must be on or after DepartureDate.");
        if (!await db.Tours.AnyAsync(t => t.Id == r.TourId, ct)) throw new BadRequestException("Tour does not exist.");
        if (r.CancellationPolicyId.HasValue && !await db.CancellationPolicies.AnyAsync(p => p.Id == r.CancellationPolicyId && p.IsActive, ct)) throw new BadRequestException("Cancellation policy is inactive or missing.");
    }
}
