using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Tours;

[ApiController]
[Route("api/v1/policies")]
public sealed class PoliciesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var policies = await db.CancellationPolicies.AsNoTracking().Where(p => p.IsActive).OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name).ToListAsync(ct);
        var ids = policies.Select(p => p.Id).ToArray();
        var rules = await db.CancellationPolicyRules.AsNoTracking().Where(r => ids.Contains(r.PolicyId)).OrderBy(r => r.SortOrder).ToListAsync(ct);
        return Ok(policies.Select(p => new CancellationPolicyDetail(p, rules.Where(r => r.PolicyId == p.Id).ToArray())));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var policy = await db.CancellationPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct)
            ?? throw new NotFoundException("Cancellation policy was not found.");
        var rules = await db.CancellationPolicyRules.AsNoTracking().Where(r => r.PolicyId == id).OrderBy(r => r.SortOrder).ToListAsync(ct);
        return Ok(new CancellationPolicyDetail(policy, rules));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireOperatorOrAdmin)]
    public async Task<IActionResult> Create(PolicyWriteRequest request, CancellationToken ct)
    {
        Validate(request);
        if (await db.CancellationPolicies.AnyAsync(p => p.Code == request.Code, ct)) throw new ConflictException("Policy code already exists.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (request.IsDefault) await db.CancellationPolicies.Where(p => p.IsDefault).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false), ct);
        var policy = Map(request); db.CancellationPolicies.Add(policy); await db.SaveChangesAsync(ct);
        db.CancellationPolicyRules.AddRange(request.Rules.Select(r => MapRule(policy.Id, r)));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = policy.Id }, new CancellationPolicyDetail(policy, await db.CancellationPolicyRules.AsNoTracking().Where(r => r.PolicyId == policy.Id).ToListAsync(ct)));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyNames.RequireOperatorOrAdmin)]
    public async Task<IActionResult> Update(int id, PolicyWriteRequest request, CancellationToken ct)
    {
        Validate(request);
        var policy = await db.CancellationPolicies.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException("Cancellation policy was not found.");
        if (await db.CancellationPolicies.AnyAsync(p => p.Id != id && p.Code == request.Code, ct)) throw new ConflictException("Policy code already exists.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (request.IsDefault) await db.CancellationPolicies.Where(p => p.Id != id && p.IsDefault).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false), ct);
        policy.Code = request.Code; policy.Name = request.Name; policy.Description = request.Description;
        policy.CompanyCancelRefundPercent = request.CompanyCancelRefundPercent; policy.AllowReschedule = request.AllowReschedule;
        policy.RescheduleCutoffDays = request.RescheduleCutoffDays; policy.RescheduleFeePercent = request.RescheduleFeePercent;
        policy.IsDefault = request.IsDefault; policy.IsActive = request.IsActive;
        var oldRules = await db.CancellationPolicyRules.Where(r => r.PolicyId == id).ToListAsync(ct); db.CancellationPolicyRules.RemoveRange(oldRules);
        db.CancellationPolicyRules.AddRange(request.Rules.Select(r => MapRule(id, r)));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Ok(new CancellationPolicyDetail(policy, await db.CancellationPolicyRules.AsNoTracking().Where(r => r.PolicyId == id).OrderBy(r => r.SortOrder).ToListAsync(ct)));
    }

    private static CancellationPolicies Map(PolicyWriteRequest r) => new() { Code = r.Code, Name = r.Name, Description = r.Description,
        CompanyCancelRefundPercent = r.CompanyCancelRefundPercent, AllowReschedule = r.AllowReschedule, RescheduleCutoffDays = r.RescheduleCutoffDays,
        RescheduleFeePercent = r.RescheduleFeePercent, IsDefault = r.IsDefault, IsActive = r.IsActive, CreatedAt = DateTime.UtcNow };
    private static CancellationPolicyRules MapRule(int id, PolicyRuleItem r) => new() { PolicyId = id, MinDaysBeforeDeparture = r.MinDaysBeforeDeparture,
        MaxDaysBeforeDeparture = r.MaxDaysBeforeDeparture, PenaltyPercent = r.PenaltyPercent, Label = r.Label, SortOrder = r.SortOrder };
    private static void Validate(PolicyWriteRequest r)
    {
        if (r.Rules.Any(x => x.MinDaysBeforeDeparture < 0 || (x.MaxDaysBeforeDeparture.HasValue && x.MaxDaysBeforeDeparture < x.MinDaysBeforeDeparture) || x.PenaltyPercent is < 0 or > 100))
            throw new BadRequestException("Cancellation rule ranges or penalties are invalid.");
    }
}
