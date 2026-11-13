namespace QuanLyTourDuLich.Api.Models.Entities;

public sealed class Suppliers
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string SupplierType { get; set; } = null!;
    public string? TaxCode { get; set; } = null!;
    public string? ContactName { get; set; } = null!;
    public string? Phone { get; set; } = null!;
    public string? Email { get; set; } = null!;
    public string? Address { get; set; } = null!;
    public string? BankName { get; set; } = null!;
    public string? BankAccountNumber { get; set; } = null!;
    public string? BankAccountHolder { get; set; } = null!;
    public byte? InternalRating { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? Notes { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
