namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class CompanyBankAccounts
{
    public int Id { get; set; }
    public string BankCode { get; set; } = null!;
    public string BankBin { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string AccountHolder { get; set; } = null!;
    public string? BranchName { get; set; } = null!;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
