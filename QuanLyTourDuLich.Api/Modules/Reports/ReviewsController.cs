using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Reports;

[ApiController, Authorize, Route("api/v1/reviews")]
public sealed class ReviewsController(AppDbContext db, ICurrentUserService current) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] int? tourId, [FromQuery] bool includeHidden = false, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var staff = User.Identity?.IsAuthenticated == true && RoleCodes.InternalStaff.Any(User.IsInRole);
        var query = from r in db.TourReviews.AsNoTracking()
                    join c in db.Customers.AsNoTracking() on r.CustomerId equals c.Id
                    join t in db.Tours.AsNoTracking() on r.TourId equals t.Id
                    where (!tourId.HasValue || r.TourId == tourId.Value)
                       && ((staff && includeHidden) || !r.IsHidden)
                    select new
                    {
                        r.Id, r.TourId, TourTitle = t.Title, TourCode = t.Code, r.DepartureId, r.BookingId,
                        r.CustomerId, CustomerName = c.FullName, r.OverallRating, r.GuideRating,
                        r.ItineraryRating, r.HotelFoodRating, r.TransportRating, r.Comment,
                        r.StaffReply, r.RepliedAt, r.IsHidden, r.CreatedAt
                    };
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new { items, pageIndex, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpPost("bookings/{id:long}")]
    [Authorize(Roles=RoleCodes.Customer+","+RoleCodes.Sales+","+RoleCodes.Admin)]
    public async Task<IActionResult> Create(long id,ReviewRequest r,CancellationToken ct)
    {
        var b=await db.Bookings.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new NotFoundException("Không tìm thấy booking.");
        if(current.IsInRole(RoleCodes.Customer)&&b.CustomerId!=current.CustomerId)throw new ForbiddenException("Booking không thuộc tài khoản này.");
        if(b.Status!="Completed")throw new ConflictException("Chỉ booking Completed mới được đánh giá.");
        if(await db.TourReviews.AnyAsync(x=>x.BookingId==id,ct))throw new ConflictException("Booking đã được đánh giá.");
        if(r.OverallRating is <1 or >5||new byte?[]{r.GuideRating,r.ItineraryRating,r.HotelFoodRating,r.TransportRating}.Any(x=>x is <1 or >5)||string.IsNullOrWhiteSpace(r.Comment))throw new BadRequestException("Điểm đánh giá phải từ 1 đến 5 và bình luận không được để trống.");
        var e=new TourReviews{TourId=b.TourId,DepartureId=b.DepartureId,BookingId=b.Id,CustomerId=b.CustomerId,OverallRating=r.OverallRating,GuideRating=r.GuideRating,ItineraryRating=r.ItineraryRating,HotelFoodRating=r.HotelFoodRating,TransportRating=r.TransportRating,Comment=r.Comment.Trim(),ImagesJson=r.Images is null?null:JsonSerializer.Serialize(r.Images),CreatedAt=DateTime.UtcNow};
        db.TourReviews.Add(e);
        await db.SaveChangesAsync(ct);
        var stats = await db.TourReviews.AsNoTracking().Where(x => x.TourId == b.TourId && !x.IsHidden)
            .GroupBy(_ => 1).Select(g => new { Count = g.Count(), Average = g.Average(x => (decimal)x.OverallRating) }).FirstOrDefaultAsync(ct);
        var tour = await db.Tours.SingleOrDefaultAsync(x => x.Id == b.TourId, ct);
        if (tour is not null && stats is not null)
        {
            tour.ReviewCount = stats.Count;
            tour.AverageRating = Math.Round(stats.Average, 2);
            tour.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return Created($"/api/v1/reviews/{e.Id}",e);
    }
    [HttpPost("{id:long}/reply"),Authorize(Roles=RoleCodes.Sales+","+RoleCodes.Admin)]
    public async Task<IActionResult> Reply(long id,ReviewReplyRequest r,CancellationToken ct){if(string.IsNullOrWhiteSpace(r.Reply))throw new BadRequestException("Nội dung phản hồi là bắt buộc.");var e=await db.TourReviews.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new NotFoundException("Không tìm thấy đánh giá.");e.StaffReply=r.Reply.Trim();e.RepliedByUserId=current.UserId;e.RepliedAt=DateTime.UtcNow;await db.SaveChangesAsync(ct);return Ok(e);}
}
public sealed record ReviewRequest(byte OverallRating,byte? GuideRating,byte? ItineraryRating,byte? HotelFoodRating,byte? TransportRating,string Comment,IReadOnlyList<string>? Images=null);
public sealed record ReviewReplyRequest(string Reply);
