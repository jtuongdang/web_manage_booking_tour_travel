using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Tours;

public sealed class TourCatalogService(AppDbContext db)
{
    public async Task<(IReadOnlyList<TourListItem> Items, int TotalCount)> SearchAsync(TourQuery q, bool canSeeInternal, CancellationToken ct)
    {
        if (q.PageIndex < 1 || q.PageSize is < 1 or > 100) throw new BadRequestException("PageIndex must be positive and PageSize must be between 1 and 100.");
        if (q.MinPrice < 0 || q.MaxPrice < 0 || q.MinPrice > q.MaxPrice) throw new BadRequestException("Price range is invalid.");
        var query = db.Tours.AsNoTracking().AsQueryable();
        var internalView = q.IncludeInternal && canSeeInternal;
        if (!internalView) query = query.Where(t => t.IsPublic && t.Status == "Published");
        else if (!q.IncludeInternal) query = query.Where(t => t.IsPublic && t.Status == "Published");
        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.Trim();
            query = query.Where(t => t.Title.Contains(keyword) || t.Code.Contains(keyword) || t.Destination.Contains(keyword) || (t.Summary != null && t.Summary.Contains(keyword)));
        }
        if (!string.IsNullOrWhiteSpace(q.Category)) query = query.Where(t => t.TourType == q.Category || t.ScopeType == q.Category || (t.Tags != null && t.Tags.Contains(q.Category)));
        if (!string.IsNullOrWhiteSpace(q.Tags))
            foreach (var tag in q.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) query = query.Where(t => t.Tags != null && t.Tags.Contains(tag));
        if (!string.IsNullOrWhiteSpace(q.ScopeType)) query = query.Where(t => t.ScopeType == q.ScopeType);
        if (!string.IsNullOrWhiteSpace(q.DepartureFrom)) query = query.Where(t => t.DepartureFrom.Contains(q.DepartureFrom));
        if (!string.IsNullOrWhiteSpace(q.Destination)) query = query.Where(t => t.Destination.Contains(q.Destination));
        if (q.DurationDays.HasValue) query = query.Where(t => t.DurationDays == q.DurationDays.Value);
        if (q.MinPrice.HasValue || q.MaxPrice.HasValue || q.DepartureDate.HasValue)
        {
            query = query.Where(t => db.TourDepartures.Any(d => d.TourId == t.Id && (internalView || d.IsPublic)
                && (!q.MinPrice.HasValue || d.AdultPrice >= q.MinPrice.Value)
                && (!q.MaxPrice.HasValue || d.AdultPrice <= q.MaxPrice.Value)
                && (!q.DepartureDate.HasValue || d.DepartureDate == q.DepartureDate.Value))
                || (!db.TourDepartures.Any(d => d.TourId == t.Id) && !q.DepartureDate.HasValue
                    && (!q.MinPrice.HasValue || t.BaseAdultPrice >= q.MinPrice.Value)
                    && (!q.MaxPrice.HasValue || t.BaseAdultPrice <= q.MaxPrice.Value)));
        }
        var total = await query.CountAsync(ct);
        var tours = await query.OrderBy(t => t.Title).Skip((q.PageIndex - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        var ids = tours.Select(t => t.Id).ToArray();
        var departures = await db.TourDepartures.AsNoTracking()
            .Where(d => ids.Contains(d.TourId) && d.DepartureDate >= DateOnly.FromDateTime(DateTime.UtcNow) && (internalView || (d.IsPublic && d.Status != "Cancelled")))
            .OrderBy(d => d.DepartureDate).ToListAsync(ct);
        var firstByTour = departures.GroupBy(d => d.TourId).ToDictionary(g => g.Key, g => g.First());
        var images = await db.TourImages.AsNoTracking()
            .Where(i => ids.Contains(i.TourId))
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .ToListAsync(ct);
        var coverByTour = images.GroupBy(i => i.TourId).ToDictionary(g => g.Key, g => g.First().ImageUrl);
        return (tours.Select(t =>
        {
            firstByTour.TryGetValue(t.Id, out var next);
            coverByTour.TryGetValue(t.Id, out var coverUrl);
            int? nextAvailable = next is null ? null : next.TotalCapacity - next.HeldSlots - next.BookedSlots;
            return new TourListItem(t.Id, t.Code, t.Slug, t.Title, t.TourType, t.ScopeType, t.Tags, t.DepartureFrom, t.Destination,
                t.DurationDays, t.BaseAdultPrice, t.IsPublic, t.Status, t.AverageRating, t.ReviewCount, next?.DepartureDate, nextAvailable,
                t.OriginalPrice, t.Transport, t.Badges, coverUrl, next?.Id);
        }).ToList(), total);
    }

    public async Task<TourDetail> GetAsync(string key, bool canSeeInternal, CancellationToken ct)
    {
        var tour = await db.Tours.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == key || t.Code == key || t.Id.ToString() == key, ct)
            ?? throw new NotFoundException("Tour was not found.");
        if (!(canSeeInternal) && (!tour.IsPublic || tour.Status != "Published")) throw new NotFoundException("Tour was not found.");
        var policy = await db.CancellationPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == tour.CancellationPolicyId, ct)
            ?? throw new NotFoundException("Cancellation policy was not found.");
        var rules = await db.CancellationPolicyRules.AsNoTracking().Where(r => r.PolicyId == policy.Id).OrderBy(r => r.SortOrder).ToListAsync(ct);
        var itineraries = await db.TourItineraries.AsNoTracking().Where(x => x.TourId == tour.Id).OrderBy(x => x.DayNumber).ToListAsync(ct);
        var images = await db.TourImages.AsNoTracking().Where(x => x.TourId == tour.Id).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var departures = await db.TourDepartures.AsNoTracking().Where(x => x.TourId == tour.Id && x.DepartureDate >= DateOnly.FromDateTime(DateTime.UtcNow) && (canSeeInternal || (x.IsPublic && x.Status != "Cancelled")))
            .OrderBy(x => x.DepartureDate).Take(20).ToListAsync(ct);
        var totalVisible = await db.TourReviews.AsNoTracking().CountAsync(x => x.TourId == tour.Id && !x.IsHidden, ct);
        var reviewList = await (
            from r in db.TourReviews.AsNoTracking()
            join c in db.Customers.AsNoTracking() on r.CustomerId equals c.Id
            where r.TourId == tour.Id && !r.IsHidden
            orderby r.CreatedAt descending
            select new TourReviewItem(r.Id, c.FullName, r.OverallRating, r.GuideRating, r.ItineraryRating,
                r.HotelFoodRating, r.TransportRating, r.Comment, r.StaffReply, r.RepliedAt, r.CreatedAt)
        ).Take(30).ToListAsync(ct);
        var reviewCount = totalVisible > 0 ? totalVisible : tour.ReviewCount;
        var avg = reviewList.Count > 0 ? Math.Round(reviewList.Average(x => (double)x.OverallRating), 2) : (double)tour.AverageRating;
        static double? Avg(IEnumerable<byte?> vals) { var arr = vals.Where(v => v.HasValue).Select(v => (double)v!.Value).ToArray(); return arr.Length > 0 ? Math.Round(arr.Average(), 1) : null; }
        var summary = new
        {
            reviewCount,
            averageRating = avg,
            guideAverage = Avg(reviewList.Select(x => x.GuideRating)),
            itineraryAverage = Avg(reviewList.Select(x => x.ItineraryRating)),
            hotelFoodAverage = Avg(reviewList.Select(x => x.HotelFoodRating)),
            transportAverage = Avg(reviewList.Select(x => x.TransportRating)),
        };
        return new TourDetail(tour, itineraries, images, new CancellationPolicyDetail(policy, rules), departures, summary, reviewList);
    }
}
