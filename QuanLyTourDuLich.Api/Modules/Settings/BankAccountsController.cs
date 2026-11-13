using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Modules.Settings;

[ApiController]
[Authorize(Policy = PolicyNames.RequireAdmin)]
[Route("api/v1/bank-accounts")]
public sealed class BankAccountsController(AppDbContext db, IAuditLogService auditLog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankAccountResponse>>> List(CancellationToken cancellationToken)
    {
        var accounts = await db.CompanyBankAccounts.AsNoTracking()
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Id)
            .Select(ToResponse())
            .ToListAsync(cancellationToken);
        return Ok(accounts);
    }

    [HttpPost]
    public async Task<ActionResult<BankAccountResponse>> Create(CreateBankAccountRequest request, CancellationToken cancellationToken)
    {
        Validate(request.BankCode, request.BankBin, request.BankName, request.AccountNumber, request.AccountHolder, request.BranchName);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (request.IsDefault)
        {
            await db.CompanyBankAccounts.Where(a => a.IsDefault)
                .ExecuteUpdateAsync(update => update.SetProperty(a => a.IsDefault, false), cancellationToken);
        }

        var account = new CompanyBankAccounts
        {
            BankCode = request.BankCode.Trim(),
            BankBin = request.BankBin.Trim(),
            BankName = request.BankName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            AccountHolder = request.AccountHolder.Trim(),
            BranchName = request.BranchName?.Trim(),
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        db.CompanyBankAccounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            "CreateBankAccount", "CompanyBankAccounts", account.Id.ToString(),
            newValue: new { account.BankCode, account.AccountNumber, account.IsDefault, account.IsActive },
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(List), new { id = account.Id }, ToResponse(account));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BankAccountResponse>> Update(
        int id,
        UpdateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request.BankCode, request.BankBin, request.BankName, request.AccountNumber, request.AccountHolder, request.BranchName);
        var account = await db.CompanyBankAccounts.SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Bank account not found.");
        var oldValue = ToResponse(account);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (request.IsDefault)
        {
            await db.CompanyBankAccounts.Where(a => a.IsDefault && a.Id != id)
                .ExecuteUpdateAsync(update => update.SetProperty(a => a.IsDefault, false), cancellationToken);
        }

        account.BankCode = request.BankCode.Trim();
        account.BankBin = request.BankBin.Trim();
        account.BankName = request.BankName.Trim();
        account.AccountNumber = request.AccountNumber.Trim();
        account.AccountHolder = request.AccountHolder.Trim();
        account.BranchName = request.BranchName?.Trim();
        account.IsDefault = request.IsDefault;
        account.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            "UpdateBankAccount", "CompanyBankAccounts", account.Id.ToString(),
            oldValue: oldValue,
            newValue: ToResponse(account),
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(ToResponse(account));
    }

    private static System.Linq.Expressions.Expression<Func<CompanyBankAccounts, BankAccountResponse>> ToResponse() =>
        a => new BankAccountResponse(a.Id, a.BankCode, a.BankBin, a.BankName, a.AccountNumber, a.AccountHolder, a.BranchName, a.IsDefault, a.IsActive);

    private static BankAccountResponse ToResponse(CompanyBankAccounts account) =>
        new(account.Id, account.BankCode, account.BankBin, account.BankName, account.AccountNumber, account.AccountHolder, account.BranchName, account.IsDefault, account.IsActive);

    private static void Validate(string bankCode, string bankBin, string bankName, string accountNumber, string accountHolder, string? branchName)
    {
        if (string.IsNullOrWhiteSpace(bankCode) || bankCode.Length > 20 ||
            string.IsNullOrWhiteSpace(bankBin) || bankBin.Length > 10 ||
            string.IsNullOrWhiteSpace(bankName) || bankName.Length > 120 ||
            string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length > 40 ||
            string.IsNullOrWhiteSpace(accountHolder) || accountHolder.Length > 150 ||
            branchName?.Length > 150)
        {
            throw new BadRequestException("Invalid company bank account fields.");
        }
    }
}

public sealed record CreateBankAccountRequest(string BankCode, string BankBin, string BankName, string AccountNumber, string AccountHolder, string? BranchName, bool IsDefault = false, bool IsActive = true);
public sealed record UpdateBankAccountRequest(string BankCode, string BankBin, string BankName, string AccountNumber, string AccountHolder, string? BranchName, bool IsDefault, bool IsActive);
public sealed record BankAccountResponse(int Id, string BankCode, string BankBin, string BankName, string AccountNumber, string AccountHolder, string? BranchName, bool IsDefault, bool IsActive);
