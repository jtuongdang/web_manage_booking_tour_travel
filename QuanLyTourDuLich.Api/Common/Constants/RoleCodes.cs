namespace QuanLyTourDuLich.Api.Common.Constants;

public static class RoleCodes
{
    public const string Customer = "CUSTOMER";
    public const string Sales = "SALES";
    public const string Operator = "OPERATOR";
    public const string Guide = "GUIDE";
    public const string Accountant = "ACCOUNTANT";
    public const string Admin = "ADMIN";

    public static readonly string[] All =
    [
        Customer, Sales, Operator, Guide, Accountant, Admin
    ];

    public static readonly string[] InternalStaff =
    [
        Sales, Operator, Guide, Accountant, Admin
    ];
}

public static class PolicyNames
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireSalesOrAdmin = "RequireSalesOrAdmin";
    public const string RequireOperatorOrAdmin = "RequireOperatorOrAdmin";
    public const string RequireGuideOrOps = "RequireGuideOrOps";
    public const string RequireAccountantOrAdmin = "RequireAccountantOrAdmin";
    public const string RequireInternalStaff = "RequireInternalStaff";
}
