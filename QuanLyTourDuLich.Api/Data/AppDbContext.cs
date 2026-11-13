using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Models.Entities;

namespace QuanLyTourDuLich.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Roles> Roles => Set<Roles>();
    public DbSet<Users> Users => Set<Users>();
    public DbSet<UserRoles> UserRoles => Set<UserRoles>();
    public DbSet<RefreshTokens> RefreshTokens => Set<RefreshTokens>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<CompanyBankAccounts> CompanyBankAccounts => Set<CompanyBankAccounts>();
    public DbSet<AuditLogs> AuditLogs => Set<AuditLogs>();
    public DbSet<Customers> Customers => Set<Customers>();
    public DbSet<CancellationPolicies> CancellationPolicies => Set<CancellationPolicies>();
    public DbSet<CancellationPolicyRules> CancellationPolicyRules => Set<CancellationPolicyRules>();
    public DbSet<Tours> Tours => Set<Tours>();
    public DbSet<TourItineraries> TourItineraries => Set<TourItineraries>();
    public DbSet<TourImages> TourImages => Set<TourImages>();
    public DbSet<TourDepartures> TourDepartures => Set<TourDepartures>();
    public DbSet<DepartureGuides> DepartureGuides => Set<DepartureGuides>();
    public DbSet<DepartureChecklists> DepartureChecklists => Set<DepartureChecklists>();
    public DbSet<Suppliers> Suppliers => Set<Suppliers>();
    public DbSet<DepartureServices> DepartureServices => Set<DepartureServices>();
    public DbSet<DepartureExtraExpenses> DepartureExtraExpenses => Set<DepartureExtraExpenses>();
    public DbSet<Promotions> Promotions => Set<Promotions>();
    public DbSet<PromotionTours> PromotionTours => Set<PromotionTours>();
    public DbSet<Bookings> Bookings => Set<Bookings>();
    public DbSet<PromotionUsages> PromotionUsages => Set<PromotionUsages>();
    public DbSet<BookingPassengers> BookingPassengers => Set<BookingPassengers>();
    public DbSet<BookingReschedules> BookingReschedules => Set<BookingReschedules>();
    public DbSet<PaymentTransactions> PaymentTransactions => Set<PaymentTransactions>();
    public DbSet<RefundRequests> RefundRequests => Set<RefundRequests>();
    public DbSet<SimulatedInvoices> SimulatedInvoices => Set<SimulatedInvoices>();
    public DbSet<SupplierPayments> SupplierPayments => Set<SupplierPayments>();
    public DbSet<FinancialLedgers> FinancialLedgers => Set<FinancialLedgers>();
    public DbSet<TourIncidents> TourIncidents => Set<TourIncidents>();
    public DbSet<TourReviews> TourReviews => Set<TourReviews>();
    public DbSet<Notifications> Notifications => Set<Notifications>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.ToTable("Roles", "dbo", table =>
            {
            });
            entity.HasKey(e => e.Id).HasName("PK_Roles");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Code).HasColumnName("Code").HasColumnType("varchar(30)");
            entity.Property(e => e.Code).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.Name).HasColumnName("Name").HasColumnType("nvarchar(100)");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("Description").HasColumnType("nvarchar(300)");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UQ_Roles_Code");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.ToTable("Users", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Users_UserType", @"UserType IN ('Customer', 'Staff')");
            });
            entity.HasKey(e => e.Id).HasName("PK_Users");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Username).HasColumnName("Username").HasColumnType("varchar(60)");
            entity.Property(e => e.Username).IsUnicode(false).HasMaxLength(60);
            entity.Property(e => e.Email).HasColumnName("Email").HasColumnType("nvarchar(150)");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Phone).HasColumnName("Phone").HasColumnType("varchar(20)");
            entity.Property(e => e.Phone).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash").HasColumnType("nvarchar(500)");
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.FullName).HasColumnName("FullName").HasColumnType("nvarchar(120)");
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.UserType).HasColumnName("UserType").HasColumnType("varchar(20)");
            entity.Property(e => e.UserType).HasDefaultValueSql(@"'Staff'");
            entity.Property(e => e.UserType).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasColumnType("bit");
            entity.Property(e => e.IsActive).HasDefaultValueSql(@"1");
            entity.Property(e => e.FailedLoginCount).HasColumnName("FailedLoginCount").HasColumnType("int");
            entity.Property(e => e.FailedLoginCount).HasDefaultValueSql(@"0");
            entity.Property(e => e.LockoutEndAt).HasColumnName("LockoutEndAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.LastLoginAt).HasColumnName("LastLoginAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("UQ_Users_Username");
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
            entity.HasIndex(e => e.Phone).IsUnique().HasDatabaseName("UX_Users_Phone").HasFilter(@"Phone IS NOT NULL");
        });

        modelBuilder.Entity<UserRoles>(entity =>
        {
            entity.ToTable("UserRoles", "dbo", table =>
            {
            });
            entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("PK_UserRoles");
            entity.Property(e => e.UserId).HasColumnName("UserId").HasColumnType("int");
            entity.Property(e => e.RoleId).HasColumnName("RoleId").HasColumnType("int");
            entity.Property(e => e.AssignedAt).HasColumnName("AssignedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.AssignedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.AssignedByUserId).HasColumnName("AssignedByUserId").HasColumnType("int");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.UserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_UserRoles_Users");
            entity.HasOne<Roles>().WithMany().HasForeignKey(e => e.RoleId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_UserRoles_Roles");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.AssignedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_UserRoles_AssignedBy");
        });

        modelBuilder.Entity<RefreshTokens>(entity =>
        {
            entity.ToTable("RefreshTokens", "dbo", table =>
            {
            });
            entity.HasKey(e => e.Id).HasName("PK_RefreshTokens");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.UserId).HasColumnName("UserId").HasColumnType("int");
            entity.Property(e => e.TokenHash).HasColumnName("TokenHash").HasColumnType("varchar(128)");
            entity.Property(e => e.TokenHash).IsUnicode(false).HasMaxLength(128);
            entity.Property(e => e.ExpiresAt).HasColumnName("ExpiresAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.RevokedAt).HasColumnName("RevokedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.ReplacedByTokenHash).HasColumnName("ReplacedByTokenHash").HasColumnType("varchar(128)");
            entity.Property(e => e.ReplacedByTokenHash).IsUnicode(false).HasMaxLength(128);
            entity.Property(e => e.CreatedByIp).HasColumnName("CreatedByIp").HasColumnType("varchar(45)");
            entity.Property(e => e.CreatedByIp).IsUnicode(false).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasColumnName("UserAgent").HasColumnType("nvarchar(300)");
            entity.Property(e => e.UserAgent).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("UQ_RefreshTokens_TokenHash");
            entity.HasIndex(e => new { e.UserId, e.ExpiresAt }).HasDatabaseName("IX_RefreshTokens_UserExpires");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.UserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<SystemSettings>(entity =>
        {
            entity.ToTable("SystemSettings", "dbo", table =>
            {
                table.HasCheckConstraint("CK_SystemSettings_ValueType", @"ValueType IN ('String', 'Int', 'Decimal', 'Bool', 'Json')");
            });
            entity.HasKey(e => e.SettingKey).HasName("PK_SystemSettings");
            entity.Property(e => e.SettingKey).HasColumnName("SettingKey").HasColumnType("varchar(80)");
            entity.Property(e => e.SettingKey).IsUnicode(false).HasMaxLength(80);
            entity.Property(e => e.SettingValue).HasColumnName("SettingValue").HasColumnType("nvarchar(500)");
            entity.Property(e => e.SettingValue).HasMaxLength(500);
            entity.Property(e => e.ValueType).HasColumnName("ValueType").HasColumnType("varchar(20)");
            entity.Property(e => e.ValueType).HasDefaultValueSql(@"'String'");
            entity.Property(e => e.ValueType).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.Description).HasColumnName("Description").HasColumnType("nvarchar(300)");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.UpdatedByUserId).HasColumnName("UpdatedByUserId").HasColumnType("int");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.UpdatedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_SystemSettings_Users");
        });

        modelBuilder.Entity<CompanyBankAccounts>(entity =>
        {
            entity.ToTable("CompanyBankAccounts", "dbo", table =>
            {
            });
            entity.HasKey(e => e.Id).HasName("PK_CompanyBankAccounts");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.BankCode).HasColumnName("BankCode").HasColumnType("varchar(20)");
            entity.Property(e => e.BankCode).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.BankBin).HasColumnName("BankBin").HasColumnType("varchar(10)");
            entity.Property(e => e.BankBin).IsUnicode(false).HasMaxLength(10);
            entity.Property(e => e.BankName).HasColumnName("BankName").HasColumnType("nvarchar(120)");
            entity.Property(e => e.BankName).HasMaxLength(120);
            entity.Property(e => e.AccountNumber).HasColumnName("AccountNumber").HasColumnType("varchar(40)");
            entity.Property(e => e.AccountNumber).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.AccountHolder).HasColumnName("AccountHolder").HasColumnType("nvarchar(150)");
            entity.Property(e => e.AccountHolder).HasMaxLength(150);
            entity.Property(e => e.BranchName).HasColumnName("BranchName").HasColumnType("nvarchar(150)");
            entity.Property(e => e.BranchName).HasMaxLength(150);
            entity.Property(e => e.IsDefault).HasColumnName("IsDefault").HasColumnType("bit");
            entity.Property(e => e.IsDefault).HasDefaultValueSql(@"0");
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasColumnType("bit");
            entity.Property(e => e.IsActive).HasDefaultValueSql(@"1");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => new { e.BankCode, e.AccountNumber }).IsUnique().HasDatabaseName("UQ_CompanyBankAccounts_BankAcc");
            entity.HasIndex(e => e.IsDefault).IsUnique().HasDatabaseName("UX_CompanyBankAccounts_Default").HasFilter(@"IsDefault = 1");
        });

        modelBuilder.Entity<AuditLogs>(entity =>
        {
            entity.ToTable("AuditLogs", "dbo", table =>
            {
            });
            entity.HasKey(e => e.Id).HasName("PK_AuditLogs");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.ActorUserId).HasColumnName("ActorUserId").HasColumnType("int");
            entity.Property(e => e.ActorRole).HasColumnName("ActorRole").HasColumnType("varchar(30)");
            entity.Property(e => e.ActorRole).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.Action).HasColumnName("Action").HasColumnType("varchar(80)");
            entity.Property(e => e.Action).IsUnicode(false).HasMaxLength(80);
            entity.Property(e => e.EntityName).HasColumnName("EntityName").HasColumnType("varchar(80)");
            entity.Property(e => e.EntityName).IsUnicode(false).HasMaxLength(80);
            entity.Property(e => e.EntityId).HasColumnName("EntityId").HasColumnType("varchar(64)");
            entity.Property(e => e.EntityId).IsUnicode(false).HasMaxLength(64);
            entity.Property(e => e.OldValueJson).HasColumnName("OldValueJson").HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValueJson).HasColumnName("NewValueJson").HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).HasColumnName("IpAddress").HasColumnType("varchar(45)");
            entity.Property(e => e.IpAddress).IsUnicode(false).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasColumnName("UserAgent").HasColumnType("nvarchar(300)");
            entity.Property(e => e.UserAgent).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => new { e.EntityName, e.EntityId, e.CreatedAt }).HasDatabaseName("IX_AuditLogs_Entity").IsDescending(false, false, true);
            entity.HasIndex(e => new { e.ActorUserId, e.CreatedAt }).HasDatabaseName("IX_AuditLogs_Actor").IsDescending(false, true);
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.ActorUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<Customers>(entity =>
        {
            entity.ToTable("Customers", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Customers_Source", @"Source IN ('Web', 'WalkIn', 'Zalo', 'Facebook', 'Hotline', 'Corporate')");
                table.HasCheckConstraint("CK_Customers_Tier", @"LoyaltyTier IN ('Standard', 'Silver', 'Gold', 'VIP')");
                table.HasCheckConstraint("CK_Customers_Totals", @"TotalSpent >= 0 AND TotalBookings >= 0");
            });
            entity.HasKey(e => e.Id).HasName("PK_Customers");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.CustomerCode).HasColumnName("CustomerCode").HasColumnType("varchar(30)");
            entity.Property(e => e.CustomerCode).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.UserId).HasColumnName("UserId").HasColumnType("int");
            entity.Property(e => e.FullName).HasColumnName("FullName").HasColumnType("nvarchar(120)");
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.Phone).HasColumnName("Phone").HasColumnType("varchar(20)");
            entity.Property(e => e.Phone).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.Email).HasColumnName("Email").HasColumnType("nvarchar(150)");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Address).HasColumnName("Address").HasColumnType("nvarchar(300)");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.Source).HasColumnName("Source").HasColumnType("varchar(30)");
            entity.Property(e => e.Source).HasDefaultValueSql(@"'Web'");
            entity.Property(e => e.Source).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.LoyaltyTier).HasColumnName("LoyaltyTier").HasColumnType("varchar(20)");
            entity.Property(e => e.LoyaltyTier).HasDefaultValueSql(@"'Standard'");
            entity.Property(e => e.LoyaltyTier).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.TotalSpent).HasColumnName("TotalSpent").HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalSpent).HasDefaultValueSql(@"0");
            entity.Property(e => e.TotalSpent).HasPrecision(18, 2);
            entity.Property(e => e.TotalBookings).HasColumnName("TotalBookings").HasColumnType("int");
            entity.Property(e => e.TotalBookings).HasDefaultValueSql(@"0");
            entity.Property(e => e.Notes).HasColumnName("Notes").HasColumnType("nvarchar(500)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.CustomerCode).IsUnique().HasDatabaseName("UQ_Customers_Code");
            entity.HasIndex(e => e.Phone).IsUnique().HasDatabaseName("UQ_Customers_Phone");
            entity.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("UX_Customers_UserId").HasFilter(@"UserId IS NOT NULL");
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UX_Customers_Email").HasFilter(@"Email IS NOT NULL");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.UserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Customers_Users");
        });

        modelBuilder.Entity<CancellationPolicies>(entity =>
        {
            entity.ToTable("CancellationPolicies", "dbo", table =>
            {
                table.HasCheckConstraint("CK_CancelPol_Percents", @"
        CompanyCancelRefundPercent BETWEEN 0 AND 100
        AND RescheduleFeePercent BETWEEN 0 AND 100
        AND RescheduleCutoffDays >= 0
    ");
            });
            entity.HasKey(e => e.Id).HasName("PK_CancellationPolicies");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Code).HasColumnName("Code").HasColumnType("varchar(40)");
            entity.Property(e => e.Code).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.Name).HasColumnName("Name").HasColumnType("nvarchar(150)");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Description).HasColumnName("Description").HasColumnType("nvarchar(500)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CompanyCancelRefundPercent).HasColumnName("CompanyCancelRefundPercent").HasColumnType("decimal(5,2)");
            entity.Property(e => e.CompanyCancelRefundPercent).HasDefaultValueSql(@"100.00");
            entity.Property(e => e.CompanyCancelRefundPercent).HasPrecision(5, 2);
            entity.Property(e => e.AllowReschedule).HasColumnName("AllowReschedule").HasColumnType("bit");
            entity.Property(e => e.AllowReschedule).HasDefaultValueSql(@"1");
            entity.Property(e => e.RescheduleCutoffDays).HasColumnName("RescheduleCutoffDays").HasColumnType("int");
            entity.Property(e => e.RescheduleCutoffDays).HasDefaultValueSql(@"7");
            entity.Property(e => e.RescheduleFeePercent).HasColumnName("RescheduleFeePercent").HasColumnType("decimal(5,2)");
            entity.Property(e => e.RescheduleFeePercent).HasDefaultValueSql(@"0.00");
            entity.Property(e => e.RescheduleFeePercent).HasPrecision(5, 2);
            entity.Property(e => e.IsDefault).HasColumnName("IsDefault").HasColumnType("bit");
            entity.Property(e => e.IsDefault).HasDefaultValueSql(@"0");
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasColumnType("bit");
            entity.Property(e => e.IsActive).HasDefaultValueSql(@"1");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UQ_CancellationPolicies_Code");
        });

        modelBuilder.Entity<CancellationPolicyRules>(entity =>
        {
            entity.ToTable("CancellationPolicyRules", "dbo", table =>
            {
                table.HasCheckConstraint("CK_CancelRule_Days", @"
        MinDaysBeforeDeparture >= 0
        AND (MaxDaysBeforeDeparture IS NULL OR MaxDaysBeforeDeparture >= MinDaysBeforeDeparture)
    ");
                table.HasCheckConstraint("CK_CancelRule_Penalty", @"PenaltyPercent BETWEEN 0 AND 100");
            });
            entity.HasKey(e => e.Id).HasName("PK_CancellationPolicyRules");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.PolicyId).HasColumnName("PolicyId").HasColumnType("int");
            entity.Property(e => e.MinDaysBeforeDeparture).HasColumnName("MinDaysBeforeDeparture").HasColumnType("int");
            entity.Property(e => e.MaxDaysBeforeDeparture).HasColumnName("MaxDaysBeforeDeparture").HasColumnType("int");
            entity.Property(e => e.PenaltyPercent).HasColumnName("PenaltyPercent").HasColumnType("decimal(5,2)");
            entity.Property(e => e.PenaltyPercent).HasPrecision(5, 2);
            entity.Property(e => e.Label).HasColumnName("Label").HasColumnType("nvarchar(150)");
            entity.Property(e => e.Label).HasMaxLength(150);
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder").HasColumnType("int");
            entity.Property(e => e.SortOrder).HasDefaultValueSql(@"0");
            entity.HasIndex(e => new { e.PolicyId, e.SortOrder }).HasDatabaseName("IX_CancelRule_Policy");
            entity.HasOne<CancellationPolicies>().WithMany().HasForeignKey(e => e.PolicyId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_CancelRule_Policy");
        });

        modelBuilder.Entity<Tours>(entity =>
        {
            entity.ToTable("Tours", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Tours_Type", @"TourType IN ('GroupTour', 'PrivateTour', 'CustomTour')");
                table.HasCheckConstraint("CK_Tours_Scope", @"ScopeType IN ('Domestic', 'International')");
                table.HasCheckConstraint("CK_Tours_Status", @"Status IN ('Draft', 'Published', 'Suspended', 'Archived')");
                table.HasCheckConstraint("CK_Tours_Values", @"
        DurationDays >= 1 AND DurationNights >= 0
        AND BaseAdultPrice >= 0 AND (OriginalPrice IS NULL OR OriginalPrice >= 0)
        AND ChildPricePercent BETWEEN 0 AND 100
        AND InfantPrice >= 0 AND SingleSupplement >= 0
        AND AverageRating BETWEEN 0 AND 5 AND ReviewCount >= 0 AND BookedCountTotal >= 0
    ");
                table.HasCheckConstraint("CK_Tours_Json", @"
        (HighlightsJson IS NULL OR ISJSON(HighlightsJson) = 1)
        AND (IncludesJson IS NULL OR ISJSON(IncludesJson) = 1)
        AND (ExcludesJson IS NULL OR ISJSON(ExcludesJson) = 1)
    ");
            });
            entity.HasKey(e => e.Id).HasName("PK_Tours");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Code).HasColumnName("Code").HasColumnType("varchar(40)");
            entity.Property(e => e.Code).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.Slug).HasColumnName("Slug").HasColumnType("varchar(160)");
            entity.Property(e => e.Slug).IsUnicode(false).HasMaxLength(160);
            entity.Property(e => e.Title).HasColumnName("Title").HasColumnType("nvarchar(250)");
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.TourType).HasColumnName("TourType").HasColumnType("varchar(20)");
            entity.Property(e => e.TourType).HasDefaultValueSql(@"'GroupTour'");
            entity.Property(e => e.TourType).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.ScopeType).HasColumnName("ScopeType").HasColumnType("varchar(20)");
            entity.Property(e => e.ScopeType).HasDefaultValueSql(@"'Domestic'");
            entity.Property(e => e.ScopeType).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.Tags).HasColumnName("Tags").HasColumnType("nvarchar(200)");
            entity.Property(e => e.Tags).HasMaxLength(200);
            entity.Property(e => e.DepartureFrom).HasColumnName("DepartureFrom").HasColumnType("nvarchar(100)");
            entity.Property(e => e.DepartureFrom).HasMaxLength(100);
            entity.Property(e => e.Destination).HasColumnName("Destination").HasColumnType("nvarchar(150)");
            entity.Property(e => e.Destination).HasMaxLength(150);
            entity.Property(e => e.DurationDays).HasColumnName("DurationDays").HasColumnType("tinyint");
            entity.Property(e => e.DurationNights).HasColumnName("DurationNights").HasColumnType("tinyint");
            entity.Property(e => e.Transport).HasColumnName("Transport").HasColumnType("nvarchar(60)");
            entity.Property(e => e.Transport).HasMaxLength(60);
            entity.Property(e => e.OriginalPrice).HasColumnName("OriginalPrice").HasColumnType("decimal(18,2)");
            entity.Property(e => e.OriginalPrice).HasPrecision(18, 2);
            entity.Property(e => e.BaseAdultPrice).HasColumnName("BaseAdultPrice").HasColumnType("decimal(18,2)");
            entity.Property(e => e.BaseAdultPrice).HasPrecision(18, 2);
            entity.Property(e => e.ChildPricePercent).HasColumnName("ChildPricePercent").HasColumnType("decimal(5,2)");
            entity.Property(e => e.ChildPricePercent).HasDefaultValueSql(@"75.00");
            entity.Property(e => e.ChildPricePercent).HasPrecision(5, 2);
            entity.Property(e => e.InfantPrice).HasColumnName("InfantPrice").HasColumnType("decimal(18,2)");
            entity.Property(e => e.InfantPrice).HasDefaultValueSql(@"0");
            entity.Property(e => e.InfantPrice).HasPrecision(18, 2);
            entity.Property(e => e.SingleSupplement).HasColumnName("SingleSupplement").HasColumnType("decimal(18,2)");
            entity.Property(e => e.SingleSupplement).HasDefaultValueSql(@"0");
            entity.Property(e => e.SingleSupplement).HasPrecision(18, 2);
            entity.Property(e => e.CancellationPolicyId).HasColumnName("CancellationPolicyId").HasColumnType("int");
            entity.Property(e => e.Summary).HasColumnName("Summary").HasColumnType("nvarchar(600)");
            entity.Property(e => e.Summary).HasMaxLength(600);
            entity.Property(e => e.HighlightsJson).HasColumnName("HighlightsJson").HasColumnType("nvarchar(max)");
            entity.Property(e => e.IncludesJson).HasColumnName("IncludesJson").HasColumnType("nvarchar(max)");
            entity.Property(e => e.ExcludesJson).HasColumnName("ExcludesJson").HasColumnType("nvarchar(max)");
            entity.Property(e => e.Badges).HasColumnName("Badges").HasColumnType("nvarchar(200)");
            entity.Property(e => e.Badges).HasMaxLength(200);
            entity.Property(e => e.IsPublic).HasColumnName("IsPublic").HasColumnType("bit");
            entity.Property(e => e.IsPublic).HasDefaultValueSql(@"1");
            entity.Property(e => e.Status).HasColumnName("Status").HasColumnType("varchar(20)");
            entity.Property(e => e.Status).HasDefaultValueSql(@"'Draft'");
            entity.Property(e => e.Status).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.AverageRating).HasColumnName("AverageRating").HasColumnType("decimal(3,2)");
            entity.Property(e => e.AverageRating).HasDefaultValueSql(@"0");
            entity.Property(e => e.AverageRating).HasPrecision(3, 2);
            entity.Property(e => e.ReviewCount).HasColumnName("ReviewCount").HasColumnType("int");
            entity.Property(e => e.ReviewCount).HasDefaultValueSql(@"0");
            entity.Property(e => e.BookedCountTotal).HasColumnName("BookedCountTotal").HasColumnType("int");
            entity.Property(e => e.BookedCountTotal).HasDefaultValueSql(@"0");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserId").HasColumnType("int");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UQ_Tours_Code");
            entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("UQ_Tours_Slug");
            entity.HasIndex(e => new { e.Status, e.IsPublic, e.ScopeType, e.DepartureFrom }).HasDatabaseName("IX_Tours_Catalog").IncludeProperties(e => new { e.Code, e.Slug, e.Title, e.Destination, e.BaseAdultPrice, e.DurationDays, e.AverageRating });
            entity.HasOne<CancellationPolicies>().WithMany().HasForeignKey(e => e.CancellationPolicyId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Tours_CancelPolicy");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.CreatedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Tours_CreatedBy");
        });

        modelBuilder.Entity<TourItineraries>(entity =>
        {
            entity.ToTable("TourItineraries", "dbo", table =>
            {
                table.HasCheckConstraint("CK_TourItineraries_Day", @"DayNumber >= 1");
            });
            entity.HasKey(e => e.Id).HasName("PK_TourItineraries");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.TourId).HasColumnName("TourId").HasColumnType("int");
            entity.Property(e => e.DayNumber).HasColumnName("DayNumber").HasColumnType("tinyint");
            entity.Property(e => e.Title).HasColumnName("Title").HasColumnType("nvarchar(200)");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Meals).HasColumnName("Meals").HasColumnType("nvarchar(100)");
            entity.Property(e => e.Meals).HasMaxLength(100);
            entity.Property(e => e.Accommodation).HasColumnName("Accommodation").HasColumnType("nvarchar(200)");
            entity.Property(e => e.Accommodation).HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("Description").HasColumnType("nvarchar(max)");
            entity.HasIndex(e => new { e.TourId, e.DayNumber }).IsUnique().HasDatabaseName("UQ_TourItineraries_TourDay");
            entity.HasOne<Tours>().WithMany().HasForeignKey(e => e.TourId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_TourItineraries_Tours");
        });

        modelBuilder.Entity<TourImages>(entity =>
        {
            entity.ToTable("TourImages", "dbo", table =>
            {
            });
            entity.HasKey(e => e.Id).HasName("PK_TourImages");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.TourId).HasColumnName("TourId").HasColumnType("int");
            entity.Property(e => e.ImageUrl).HasColumnName("ImageUrl").HasColumnType("nvarchar(500)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.AltText).HasColumnName("AltText").HasColumnType("nvarchar(200)");
            entity.Property(e => e.AltText).HasMaxLength(200);
            entity.Property(e => e.IsCover).HasColumnName("IsCover").HasColumnType("bit");
            entity.Property(e => e.IsCover).HasDefaultValueSql(@"0");
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder").HasColumnType("int");
            entity.Property(e => e.SortOrder).HasDefaultValueSql(@"0");
            entity.HasIndex(e => new { e.TourId, e.SortOrder }).HasDatabaseName("IX_TourImages_TourSort");
            entity.HasOne<Tours>().WithMany().HasForeignKey(e => e.TourId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_TourImages_Tours");
        });

        modelBuilder.Entity<TourDepartures>(entity =>
        {
            entity.ToTable("TourDepartures", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Departures_Dates", @"ReturnDate >= DepartureDate");
                table.HasCheckConstraint("CK_Departures_Prices", @"
        AdultPrice >= 0
        AND (ChildPrice IS NULL OR ChildPrice >= 0)
        AND (InfantPrice IS NULL OR InfantPrice >= 0)
        AND (SingleSupplement IS NULL OR SingleSupplement >= 0)
    ");
                table.HasCheckConstraint("CK_TourDepartures_Slots", @"
        TotalCapacity >= 1
        AND HeldSlots >= 0
        AND BookedSlots >= 0
        AND HeldSlots + BookedSlots <= TotalCapacity
        AND MinParticipants >= 1
        AND (MaxInfants IS NULL OR MaxInfants >= 0)
    ");
                table.HasCheckConstraint("CK_Departures_Status", @"
        Status IN ('Open', 'Full', 'Confirmed', 'InProgress', 'Completed', 'Settled', 'Cancelled')
    ");
            });
            entity.HasKey(e => e.Id).HasName("PK_TourDepartures");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.TourId).HasColumnName("TourId").HasColumnType("int");
            entity.Property(e => e.DepartureCode).HasColumnName("DepartureCode").HasColumnType("varchar(50)");
            entity.Property(e => e.DepartureCode).IsUnicode(false).HasMaxLength(50);
            entity.Property(e => e.DepartureDate).HasColumnName("DepartureDate").HasColumnType("date");
            entity.Property(e => e.ReturnDate).HasColumnName("ReturnDate").HasColumnType("date");
            entity.Property(e => e.CutoffAt).HasColumnName("CutoffAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.AdultPrice).HasColumnName("AdultPrice").HasColumnType("decimal(18,2)");
            entity.Property(e => e.AdultPrice).HasPrecision(18, 2);
            entity.Property(e => e.ChildPrice).HasColumnName("ChildPrice").HasColumnType("decimal(18,2)");
            entity.Property(e => e.ChildPrice).HasPrecision(18, 2);
            entity.Property(e => e.InfantPrice).HasColumnName("InfantPrice").HasColumnType("decimal(18,2)");
            entity.Property(e => e.InfantPrice).HasPrecision(18, 2);
            entity.Property(e => e.SingleSupplement).HasColumnName("SingleSupplement").HasColumnType("decimal(18,2)");
            entity.Property(e => e.SingleSupplement).HasPrecision(18, 2);
            entity.Property(e => e.TotalCapacity).HasColumnName("TotalCapacity").HasColumnType("int");
            entity.Property(e => e.HeldSlots).HasColumnName("HeldSlots").HasColumnType("int");
            entity.Property(e => e.HeldSlots).HasDefaultValueSql(@"0");
            entity.Property(e => e.BookedSlots).HasColumnName("BookedSlots").HasColumnType("int");
            entity.Property(e => e.BookedSlots).HasDefaultValueSql(@"0");
            entity.Property(e => e.AvailableSlots).HasColumnName("AvailableSlots").HasColumnType("int").IsRequired();
            entity.Property(e => e.AvailableSlots).HasComputedColumnSql(@"(TotalCapacity - HeldSlots - BookedSlots)", stored: true);
            entity.Property(e => e.MinParticipants).HasColumnName("MinParticipants").HasColumnType("int");
            entity.Property(e => e.MinParticipants).HasDefaultValueSql(@"1");
            entity.Property(e => e.MaxInfants).HasColumnName("MaxInfants").HasColumnType("int");
            entity.Property(e => e.CancellationPolicyId).HasColumnName("CancellationPolicyId").HasColumnType("int");
            entity.Property(e => e.IsPublic).HasColumnName("IsPublic").HasColumnType("bit");
            entity.Property(e => e.IsPublic).HasDefaultValueSql(@"1");
            entity.Property(e => e.ContractCode).HasColumnName("ContractCode").HasColumnType("varchar(60)");
            entity.Property(e => e.ContractCode).IsUnicode(false).HasMaxLength(60);
            entity.Property(e => e.Status).HasColumnName("Status").HasColumnType("varchar(20)");
            entity.Property(e => e.Status).HasDefaultValueSql(@"'Open'");
            entity.Property(e => e.Status).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.VehicleType).HasColumnName("VehicleType").HasColumnType("nvarchar(80)");
            entity.Property(e => e.VehicleType).HasMaxLength(80);
            entity.Property(e => e.VehiclePlate).HasColumnName("VehiclePlate").HasColumnType("varchar(30)");
            entity.Property(e => e.VehiclePlate).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.DriverName).HasColumnName("DriverName").HasColumnType("nvarchar(120)");
            entity.Property(e => e.DriverName).HasMaxLength(120);
            entity.Property(e => e.DriverPhone).HasColumnName("DriverPhone").HasColumnType("varchar(20)");
            entity.Property(e => e.DriverPhone).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.Notes).HasColumnName("Notes").HasColumnType("nvarchar(500)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.SettledAt).HasColumnName("SettledAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.SettledByUserId).HasColumnName("SettledByUserId").HasColumnType("int");
            entity.Property(e => e.RowVersion).HasColumnName("RowVersion").HasColumnType("rowversion");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.DepartureCode).IsUnique().HasDatabaseName("UQ_TourDepartures_Code");
            entity.HasIndex(e => new { e.TourId, e.Status, e.IsPublic, e.DepartureDate }).HasDatabaseName("IX_TourDepartures_Search").IncludeProperties(e => new { e.AdultPrice, e.AvailableSlots, e.CutoffAt });
            entity.HasOne<Tours>().WithMany().HasForeignKey(e => e.TourId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Departures_Tours");
            entity.HasOne<CancellationPolicies>().WithMany().HasForeignKey(e => e.CancellationPolicyId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Departures_CancelPolicy");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.SettledByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Departures_SettledBy");
        });

        modelBuilder.Entity<DepartureGuides>(entity =>
        {
            entity.ToTable("DepartureGuides", "dbo", table =>
            {
                table.HasCheckConstraint("CK_DepGuides_Role", @"GuideRole IN ('Lead', 'Assistant')");
            });
            entity.HasKey(e => e.Id).HasName("PK_DepartureGuides");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.DepartureId).HasColumnName("DepartureId").HasColumnType("int");
            entity.Property(e => e.GuideUserId).HasColumnName("GuideUserId").HasColumnType("int");
            entity.Property(e => e.GuideRole).HasColumnName("GuideRole").HasColumnType("varchar(20)");
            entity.Property(e => e.GuideRole).HasDefaultValueSql(@"'Lead'");
            entity.Property(e => e.GuideRole).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.AssignedAt).HasColumnName("AssignedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.AssignedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.AssignedByUserId).HasColumnName("AssignedByUserId").HasColumnType("int");
            entity.Property(e => e.Notes).HasColumnName("Notes").HasColumnType("nvarchar(300)");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.HasIndex(e => new { e.DepartureId, e.GuideUserId }).IsUnique().HasDatabaseName("UQ_DepartureGuides_DepGuide");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.DepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_DepGuides_Departure");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.GuideUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_DepGuides_Guide");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.AssignedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_DepGuides_AssignedBy");
        });

        modelBuilder.Entity<DepartureChecklists>(entity =>
        {
            entity.ToTable("DepartureChecklists", "dbo", table =>
            {
            });
            entity.HasKey(e => e.Id).HasName("PK_DepartureChecklists");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.DepartureId).HasColumnName("DepartureId").HasColumnType("int");
            entity.Property(e => e.TaskName).HasColumnName("TaskName").HasColumnType("nvarchar(200)");
            entity.Property(e => e.TaskName).HasMaxLength(200);
            entity.Property(e => e.IsCompleted).HasColumnName("IsCompleted").HasColumnType("bit");
            entity.Property(e => e.IsCompleted).HasDefaultValueSql(@"0");
            entity.Property(e => e.CompletedByUserId).HasColumnName("CompletedByUserId").HasColumnType("int");
            entity.Property(e => e.CompletedAt).HasColumnName("CompletedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.Notes).HasColumnName("Notes").HasColumnType("nvarchar(300)");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.SortOrder).HasColumnName("SortOrder").HasColumnType("int");
            entity.Property(e => e.SortOrder).HasDefaultValueSql(@"0");
            entity.HasIndex(e => new { e.DepartureId, e.SortOrder }).HasDatabaseName("IX_DepartureChecklists_DepSort");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.DepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_DepChecklist_Departure");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.CompletedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_DepChecklist_User");
        });

        modelBuilder.Entity<Suppliers>(entity =>
        {
            entity.ToTable("Suppliers", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Suppliers_Type", @"SupplierType IN ('Hotel', 'Transport', 'Airline', 'Restaurant', 'Ticket', 'Visa', 'Other')");
                table.HasCheckConstraint("CK_Suppliers_Rating", @"InternalRating IS NULL OR InternalRating BETWEEN 1 AND 5");
            });
            entity.HasKey(e => e.Id).HasName("PK_Suppliers");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Code).HasColumnName("Code").HasColumnType("varchar(40)");
            entity.Property(e => e.Code).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.Name).HasColumnName("Name").HasColumnType("nvarchar(200)");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.SupplierType).HasColumnName("SupplierType").HasColumnType("varchar(30)");
            entity.Property(e => e.SupplierType).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.TaxCode).HasColumnName("TaxCode").HasColumnType("varchar(20)");
            entity.Property(e => e.TaxCode).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.ContactName).HasColumnName("ContactName").HasColumnType("nvarchar(120)");
            entity.Property(e => e.ContactName).HasMaxLength(120);
            entity.Property(e => e.Phone).HasColumnName("Phone").HasColumnType("varchar(20)");
            entity.Property(e => e.Phone).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.Email).HasColumnName("Email").HasColumnType("nvarchar(150)");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Address).HasColumnName("Address").HasColumnType("nvarchar(300)");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.BankName).HasColumnName("BankName").HasColumnType("nvarchar(120)");
            entity.Property(e => e.BankName).HasMaxLength(120);
            entity.Property(e => e.BankAccountNumber).HasColumnName("BankAccountNumber").HasColumnType("varchar(40)");
            entity.Property(e => e.BankAccountNumber).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.BankAccountHolder).HasColumnName("BankAccountHolder").HasColumnType("nvarchar(150)");
            entity.Property(e => e.BankAccountHolder).HasMaxLength(150);
            entity.Property(e => e.InternalRating).HasColumnName("InternalRating").HasColumnType("tinyint");
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasColumnType("bit");
            entity.Property(e => e.IsActive).HasDefaultValueSql(@"1");
            entity.Property(e => e.Notes).HasColumnName("Notes").HasColumnType("nvarchar(500)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UQ_Suppliers_Code");
        });

        modelBuilder.Entity<DepartureServices>(entity =>
        {
            entity.ToTable("DepartureServices", "dbo", table =>
            {
                table.HasCheckConstraint("CK_DepSvc_Values", @"Quantity > 0 AND UnitPrice >= 0 AND ActualCost >= 0 AND PaidAmount >= 0");
                table.HasCheckConstraint("CK_DepSvc_PayStatus", @"PaymentStatus IN ('Unpaid', 'Partial', 'Paid')");
            });
            entity.HasKey(e => e.Id).HasName("PK_DepartureServices");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.DepartureId).HasColumnName("DepartureId").HasColumnType("int");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierId").HasColumnType("int");
            entity.Property(e => e.ServiceName).HasColumnName("ServiceName").HasColumnType("nvarchar(200)");
            entity.Property(e => e.ServiceName).HasMaxLength(200);
            entity.Property(e => e.Quantity).HasColumnName("Quantity").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Quantity).HasPrecision(10, 2);
            entity.Property(e => e.UnitPrice).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)");
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.EstimatedCost).HasColumnName("EstimatedCost").HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.EstimatedCost).HasComputedColumnSql(@"(CAST(ROUND(Quantity * UnitPrice, 2) AS DECIMAL(18,2)))", stored: true);
            entity.Property(e => e.EstimatedCost).HasPrecision(18, 2);
            entity.Property(e => e.ActualCost).HasColumnName("ActualCost").HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualCost).HasDefaultValueSql(@"0");
            entity.Property(e => e.ActualCost).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasColumnName("PaidAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaidAmount).HasDefaultValueSql(@"0");
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaymentStatus).HasColumnName("PaymentStatus").HasColumnType("varchar(20)");
            entity.Property(e => e.PaymentStatus).HasDefaultValueSql(@"'Unpaid'");
            entity.Property(e => e.PaymentStatus).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.BookingReference).HasColumnName("BookingReference").HasColumnType("varchar(80)");
            entity.Property(e => e.BookingReference).IsUnicode(false).HasMaxLength(80);
            entity.Property(e => e.Notes).HasColumnName("Notes").HasColumnType("nvarchar(400)");
            entity.Property(e => e.Notes).HasMaxLength(400);
            entity.Property(e => e.RowVersion).HasColumnName("RowVersion").HasColumnType("rowversion");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => new { e.DepartureId, e.SupplierId }).HasDatabaseName("IX_DepartureServices_Dep");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.DepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_DepSvc_Departure");
            entity.HasOne<Suppliers>().WithMany().HasForeignKey(e => e.SupplierId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_DepSvc_Supplier");
        });

        modelBuilder.Entity<DepartureExtraExpenses>(entity =>
        {
            entity.ToTable("DepartureExtraExpenses", "dbo", table =>
            {
                table.HasCheckConstraint("CK_ExtraExp_Amount", @"Amount > 0");
                table.HasCheckConstraint("CK_ExtraExp_Cat", @"Category IN ('Meal', 'Transport', 'Ticket', 'Medical', 'Tip', 'Other')");
                table.HasCheckConstraint("CK_ExtraExp_Status", @"ApprovalStatus IN ('Pending', 'Approved', 'Rejected')");
            });
            entity.HasKey(e => e.Id).HasName("PK_DepartureExtraExpenses");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.DepartureId).HasColumnName("DepartureId").HasColumnType("int");
            entity.Property(e => e.Title).HasColumnName("Title").HasColumnType("nvarchar(200)");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Category).HasColumnName("Category").HasColumnType("varchar(40)");
            entity.Property(e => e.Category).HasDefaultValueSql(@"'Other'");
            entity.Property(e => e.Category).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.ExpenseDate).HasColumnName("ExpenseDate").HasColumnType("datetime2(0)");
            entity.Property(e => e.ExpenseDate).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.AttachmentUrl).HasColumnName("AttachmentUrl").HasColumnType("nvarchar(500)");
            entity.Property(e => e.AttachmentUrl).HasMaxLength(500);
            entity.Property(e => e.Description).HasColumnName("Description").HasColumnType("nvarchar(500)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserId").HasColumnType("int");
            entity.Property(e => e.ApprovalStatus).HasColumnName("ApprovalStatus").HasColumnType("varchar(20)");
            entity.Property(e => e.ApprovalStatus).HasDefaultValueSql(@"'Pending'");
            entity.Property(e => e.ApprovalStatus).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.ReviewedByUserId).HasColumnName("ReviewedByUserId").HasColumnType("int");
            entity.Property(e => e.ReviewedAt).HasColumnName("ReviewedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.ReviewNote).HasColumnName("ReviewNote").HasColumnType("nvarchar(300)");
            entity.Property(e => e.ReviewNote).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => new { e.DepartureId, e.ApprovalStatus }).HasDatabaseName("IX_ExtraExpenses_DepStatus");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.DepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_ExtraExp_Departure");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.CreatedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_ExtraExp_CreatedBy");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.ReviewedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_ExtraExp_ReviewedBy");
        });

        modelBuilder.Entity<Promotions>(entity =>
        {
            entity.ToTable("Promotions", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Promotions_Type", @"DiscountType IN ('Percent', 'FixedBooking', 'FixedPerPax')");
                table.HasCheckConstraint("CK_Promotions_Values", @"
        DiscountValue > 0
        AND (MaxDiscountAmount IS NULL OR MaxDiscountAmount >= 0)
        AND MinOrderAmount >= 0 AND MinPax >= 1
        AND (UsageLimit IS NULL OR UsageLimit >= 1)
        AND UsedCount >= 0
        AND MaxUsagePerCustomer >= 1
        AND ValidTo > ValidFrom
    ");
            });
            entity.HasKey(e => e.Id).HasName("PK_Promotions");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("int");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Code).HasColumnName("Code").HasColumnType("varchar(40)");
            entity.Property(e => e.Code).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.Name).HasColumnName("Name").HasColumnType("nvarchar(150)");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Description).HasColumnName("Description").HasColumnType("nvarchar(400)");
            entity.Property(e => e.Description).HasMaxLength(400);
            entity.Property(e => e.DiscountType).HasColumnName("DiscountType").HasColumnType("varchar(20)");
            entity.Property(e => e.DiscountType).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.DiscountValue).HasColumnName("DiscountValue").HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
            entity.Property(e => e.MaxDiscountAmount).HasColumnName("MaxDiscountAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxDiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.MinOrderAmount).HasColumnName("MinOrderAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.MinOrderAmount).HasDefaultValueSql(@"0");
            entity.Property(e => e.MinOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.MinPax).HasColumnName("MinPax").HasColumnType("int");
            entity.Property(e => e.MinPax).HasDefaultValueSql(@"1");
            entity.Property(e => e.ApplyAllTours).HasColumnName("ApplyAllTours").HasColumnType("bit");
            entity.Property(e => e.ApplyAllTours).HasDefaultValueSql(@"1");
            entity.Property(e => e.UsageLimit).HasColumnName("UsageLimit").HasColumnType("int");
            entity.Property(e => e.UsedCount).HasColumnName("UsedCount").HasColumnType("int");
            entity.Property(e => e.UsedCount).HasDefaultValueSql(@"0");
            entity.Property(e => e.MaxUsagePerCustomer).HasColumnName("MaxUsagePerCustomer").HasColumnType("int");
            entity.Property(e => e.MaxUsagePerCustomer).HasDefaultValueSql(@"1");
            entity.Property(e => e.ValidFrom).HasColumnName("ValidFrom").HasColumnType("datetime2(0)");
            entity.Property(e => e.ValidTo).HasColumnName("ValidTo").HasColumnType("datetime2(0)");
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasColumnType("bit");
            entity.Property(e => e.IsActive).HasDefaultValueSql(@"1");
            entity.Property(e => e.RowVersion).HasColumnName("RowVersion").HasColumnType("rowversion");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UQ_Promotions_Code");
        });

        modelBuilder.Entity<PromotionTours>(entity =>
        {
            entity.ToTable("PromotionTours", "dbo", table =>
            {
            });
            entity.HasKey(e => new { e.PromotionId, e.TourId }).HasName("PK_PromotionTours");
            entity.Property(e => e.PromotionId).HasColumnName("PromotionId").HasColumnType("int");
            entity.Property(e => e.TourId).HasColumnName("TourId").HasColumnType("int");
            entity.HasOne<Promotions>().WithMany().HasForeignKey(e => e.PromotionId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_PromotionTours_Promo");
            entity.HasOne<Tours>().WithMany().HasForeignKey(e => e.TourId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_PromotionTours_Tour");
        });

        modelBuilder.Entity<Bookings>(entity =>
        {
            entity.ToTable("Bookings", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Bookings_Channel", @"BookingChannel IN ('WebB2C', 'WalkIn', 'Zalo', 'Facebook', 'Hotline', 'Corporate')");
                table.HasCheckConstraint("CK_Bookings_TaxMode", @"TaxMode IN ('Included', 'Excluded')");
                table.HasCheckConstraint("CK_Bookings_PayOption", @"PayOption IN ('Full', 'Deposit')");
                table.HasCheckConstraint("CK_Bookings_Status", @"
        Status IN (
            'PendingPayment', 'Expired', 'DepositPaid', 'FinalPaymentOverdue',
            'Confirmed', 'InProgress', 'Completed',
            'CancellationRequested', 'Cancelled', 'Refunded'
        )
    ");
                table.HasCheckConstraint("CK_Bookings_Pax", @"
        AdultCount >= 1 AND ChildCount >= 0 AND InfantCount >= 0 AND SingleRoomCount >= 0
    ");
                table.HasCheckConstraint("CK_Bookings_Amounts", @"
        SubTotal >= 0 AND DiscountAmount >= 0 AND VatRate BETWEEN 0 AND 100
        AND AmountBeforeTax >= 0 AND VatAmount >= 0 AND TotalAmount >= 0
        AND RequiredDepositAmount >= 0 AND PaidAmount >= 0 AND CreditAmount >= 0
        AND (CancelPenaltyPercent IS NULL OR CancelPenaltyPercent BETWEEN 0 AND 100)
        AND (CancelPenaltyAmount IS NULL OR CancelPenaltyAmount >= 0)
    ");
                table.HasCheckConstraint("CK_Bookings_SnapshotsJson", @"
        ISJSON(PriceSnapshotJson) = 1 AND ISJSON(CancellationPolicySnapshotJson) = 1
    ");
            });
            entity.HasKey(e => e.Id).HasName("PK_Bookings");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.BookingCode).HasColumnName("BookingCode").HasColumnType("varchar(32)");
            entity.Property(e => e.BookingCode).IsUnicode(false).HasMaxLength(32);
            entity.Property(e => e.TourId).HasColumnName("TourId").HasColumnType("int");
            entity.Property(e => e.DepartureId).HasColumnName("DepartureId").HasColumnType("int");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerId").HasColumnType("int");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserId").HasColumnType("int");
            entity.Property(e => e.SalesUserId).HasColumnName("SalesUserId").HasColumnType("int");
            entity.Property(e => e.BookingChannel).HasColumnName("BookingChannel").HasColumnType("varchar(20)");
            entity.Property(e => e.BookingChannel).HasDefaultValueSql(@"'WebB2C'");
            entity.Property(e => e.BookingChannel).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.ContactName).HasColumnName("ContactName").HasColumnType("nvarchar(120)");
            entity.Property(e => e.ContactName).HasMaxLength(120);
            entity.Property(e => e.ContactPhone).HasColumnName("ContactPhone").HasColumnType("varchar(20)");
            entity.Property(e => e.ContactPhone).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.ContactEmail).HasColumnName("ContactEmail").HasColumnType("nvarchar(150)");
            entity.Property(e => e.ContactEmail).HasMaxLength(150);
            entity.Property(e => e.SpecialNote).HasColumnName("SpecialNote").HasColumnType("nvarchar(500)");
            entity.Property(e => e.SpecialNote).HasMaxLength(500);
            entity.Property(e => e.AdultCount).HasColumnName("AdultCount").HasColumnType("int");
            entity.Property(e => e.ChildCount).HasColumnName("ChildCount").HasColumnType("int");
            entity.Property(e => e.ChildCount).HasDefaultValueSql(@"0");
            entity.Property(e => e.InfantCount).HasColumnName("InfantCount").HasColumnType("int");
            entity.Property(e => e.InfantCount).HasDefaultValueSql(@"0");
            entity.Property(e => e.SingleRoomCount).HasColumnName("SingleRoomCount").HasColumnType("int");
            entity.Property(e => e.SingleRoomCount).HasDefaultValueSql(@"0");
            entity.Property(e => e.SeatsReserved).HasColumnName("SeatsReserved").HasColumnType("int").IsRequired();
            entity.Property(e => e.SeatsReserved).HasComputedColumnSql(@"(AdultCount + ChildCount)", stored: true);
            entity.Property(e => e.SubTotal).HasColumnName("SubTotal").HasColumnType("decimal(18,2)");
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasColumnName("DiscountAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasDefaultValueSql(@"0");
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.TaxMode).HasColumnName("TaxMode").HasColumnType("varchar(20)");
            entity.Property(e => e.TaxMode).HasDefaultValueSql(@"'Included'");
            entity.Property(e => e.TaxMode).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.VatRate).HasColumnName("VatRate").HasColumnType("decimal(5,2)");
            entity.Property(e => e.VatRate).HasDefaultValueSql(@"8.00");
            entity.Property(e => e.VatRate).HasPrecision(5, 2);
            entity.Property(e => e.AmountBeforeTax).HasColumnName("AmountBeforeTax").HasColumnType("decimal(18,2)");
            entity.Property(e => e.AmountBeforeTax).HasPrecision(18, 2);
            entity.Property(e => e.VatAmount).HasColumnName("VatAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.VatAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasColumnName("TotalAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.RequiredDepositAmount).HasColumnName("RequiredDepositAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.RequiredDepositAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasColumnName("PaidAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaidAmount).HasDefaultValueSql(@"0");
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.Property(e => e.CreditAmount).HasColumnName("CreditAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreditAmount).HasDefaultValueSql(@"0");
            entity.Property(e => e.CreditAmount).HasPrecision(18, 2);
            entity.Property(e => e.RemainingAmount).HasColumnName("RemainingAmount").HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.RemainingAmount).HasComputedColumnSql(@"(CASE WHEN TotalAmount > PaidAmount THEN TotalAmount - PaidAmount ELSE CAST(0 AS DECIMAL(18,2)) END)", stored: true);
            entity.Property(e => e.RemainingAmount).HasPrecision(18, 2);
            entity.Property(e => e.PromotionId).HasColumnName("PromotionId").HasColumnType("int");
            entity.Property(e => e.PromoCodeApplied).HasColumnName("PromoCodeApplied").HasColumnType("varchar(40)");
            entity.Property(e => e.PromoCodeApplied).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.PayOption).HasColumnName("PayOption").HasColumnType("varchar(20)");
            entity.Property(e => e.PayOption).HasDefaultValueSql(@"'Full'");
            entity.Property(e => e.PayOption).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.RequireVatInvoice).HasColumnName("RequireVatInvoice").HasColumnType("bit");
            entity.Property(e => e.RequireVatInvoice).HasDefaultValueSql(@"0");
            entity.Property(e => e.InvoiceCompanyName).HasColumnName("InvoiceCompanyName").HasColumnType("nvarchar(200)");
            entity.Property(e => e.InvoiceCompanyName).HasMaxLength(200);
            entity.Property(e => e.InvoiceTaxCode).HasColumnName("InvoiceTaxCode").HasColumnType("varchar(20)");
            entity.Property(e => e.InvoiceTaxCode).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.InvoiceAddress).HasColumnName("InvoiceAddress").HasColumnType("nvarchar(300)");
            entity.Property(e => e.InvoiceAddress).HasMaxLength(300);
            entity.Property(e => e.InvoiceEmail).HasColumnName("InvoiceEmail").HasColumnType("nvarchar(150)");
            entity.Property(e => e.InvoiceEmail).HasMaxLength(150);
            entity.Property(e => e.PriceSnapshotJson).HasColumnName("PriceSnapshotJson").HasColumnType("nvarchar(max)");
            entity.Property(e => e.CancellationPolicySnapshotJson).HasColumnName("CancellationPolicySnapshotJson").HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status).HasColumnName("Status").HasColumnType("varchar(30)");
            entity.Property(e => e.Status).HasDefaultValueSql(@"'PendingPayment'");
            entity.Property(e => e.Status).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.HoldExpiresAt).HasColumnName("HoldExpiresAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.FinalPaymentDueAt).HasColumnName("FinalPaymentDueAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CancellationReason).HasColumnName("CancellationReason").HasColumnType("nvarchar(300)");
            entity.Property(e => e.CancellationReason).HasMaxLength(300);
            entity.Property(e => e.CancelPenaltyPercent).HasColumnName("CancelPenaltyPercent").HasColumnType("decimal(5,2)");
            entity.Property(e => e.CancelPenaltyPercent).HasPrecision(5, 2);
            entity.Property(e => e.CancelPenaltyAmount).HasColumnName("CancelPenaltyAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.CancelPenaltyAmount).HasPrecision(18, 2);
            entity.Property(e => e.CancelApprovedByUserId).HasColumnName("CancelApprovedByUserId").HasColumnType("int");
            entity.Property(e => e.CancelledAt).HasColumnName("CancelledAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.RowVersion).HasColumnName("RowVersion").HasColumnType("rowversion");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.BookingCode).IsUnique().HasDatabaseName("UQ_Bookings_Code");
            entity.HasIndex(e => new { e.Status, e.HoldExpiresAt }).HasDatabaseName("IX_Bookings_HoldExpiryWorker").IncludeProperties(e => new { e.DepartureId, e.SeatsReserved, e.PromotionId });
            entity.HasIndex(e => new { e.Status, e.FinalPaymentDueAt }).HasDatabaseName("IX_Bookings_FinalDueWorker").IncludeProperties(e => new { e.CustomerId, e.SalesUserId, e.TotalAmount, e.PaidAmount });
            entity.HasIndex(e => new { e.DepartureId, e.Status }).HasDatabaseName("IX_Bookings_Departure");
            entity.HasIndex(e => new { e.CustomerId, e.CreatedAt }).HasDatabaseName("IX_Bookings_Customer").IsDescending(false, true);
            entity.HasOne<Tours>().WithMany().HasForeignKey(e => e.TourId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Bookings_Tour");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.DepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Bookings_Departure");
            entity.HasOne<Customers>().WithMany().HasForeignKey(e => e.CustomerId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Bookings_Customer");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.CreatedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Bookings_CreatedBy");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.SalesUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Bookings_Sales");
            entity.HasOne<Promotions>().WithMany().HasForeignKey(e => e.PromotionId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Bookings_Promotion");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.CancelApprovedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Bookings_CancelApprovedBy");
        });

        modelBuilder.Entity<PromotionUsages>(entity =>
        {
            entity.ToTable("PromotionUsages", "dbo", table =>
            {
                table.HasCheckConstraint("CK_PromoUsage_Status", @"Status IN ('Reserved', 'Redeemed', 'Released')");
            });
            entity.HasKey(e => e.Id).HasName("PK_PromotionUsages");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.PromotionId).HasColumnName("PromotionId").HasColumnType("int");
            entity.Property(e => e.BookingId).HasColumnName("BookingId").HasColumnType("bigint");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerId").HasColumnType("int");
            entity.Property(e => e.Status).HasColumnName("Status").HasColumnType("varchar(20)");
            entity.Property(e => e.Status).HasDefaultValueSql(@"'Reserved'");
            entity.Property(e => e.Status).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.ReservedAt).HasColumnName("ReservedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.ReservedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.ReleasedAt).HasColumnName("ReleasedAt").HasColumnType("datetime2(0)");
            entity.HasIndex(e => e.BookingId).IsUnique().HasDatabaseName("UQ_PromotionUsages_Booking");
            entity.HasIndex(e => new { e.PromotionId, e.CustomerId, e.Status }).HasDatabaseName("IX_PromotionUsages_PromoCust");
            entity.HasOne<Promotions>().WithMany().HasForeignKey(e => e.PromotionId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_PromoUsage_Promo");
            entity.HasOne<Bookings>().WithMany().HasForeignKey(e => e.BookingId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_PromoUsage_Booking");
            entity.HasOne<Customers>().WithMany().HasForeignKey(e => e.CustomerId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_PromoUsage_Customer");
        });

        modelBuilder.Entity<BookingPassengers>(entity =>
        {
            entity.ToTable("BookingPassengers", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Passengers_Type", @"PaxType IN ('Adult', 'Child', 'Infant')");
                table.HasCheckConstraint("CK_Passengers_CheckIn", @"CheckInStatus IN ('Pending', 'CheckedIn', 'Late', 'NoShow')");
            });
            entity.HasKey(e => e.Id).HasName("PK_BookingPassengers");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.BookingId).HasColumnName("BookingId").HasColumnType("bigint");
            entity.Property(e => e.PaxType).HasColumnName("PaxType").HasColumnType("varchar(20)");
            entity.Property(e => e.PaxType).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.FullName).HasColumnName("FullName").HasColumnType("nvarchar(120)");
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.Gender).HasColumnName("Gender").HasColumnType("nvarchar(16)");
            entity.Property(e => e.Gender).HasMaxLength(16);
            entity.Property(e => e.DateOfBirth).HasColumnName("DateOfBirth").HasColumnType("date");
            entity.Property(e => e.IdentityNumber).HasColumnName("IdentityNumber").HasColumnType("varchar(40)");
            entity.Property(e => e.IdentityNumber).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.PassportExpiryDate).HasColumnName("PassportExpiryDate").HasColumnType("date");
            entity.Property(e => e.Nationality).HasColumnName("Nationality").HasColumnType("nvarchar(60)");
            entity.Property(e => e.Nationality).HasDefaultValueSql(@"N'Việt Nam'");
            entity.Property(e => e.Nationality).HasMaxLength(60);
            entity.Property(e => e.SpecialRequest).HasColumnName("SpecialRequest").HasColumnType("nvarchar(300)");
            entity.Property(e => e.SpecialRequest).HasMaxLength(300);
            entity.Property(e => e.CheckInStatus).HasColumnName("CheckInStatus").HasColumnType("varchar(20)");
            entity.Property(e => e.CheckInStatus).HasDefaultValueSql(@"'Pending'");
            entity.Property(e => e.CheckInStatus).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.CheckedInAt).HasColumnName("CheckedInAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CheckedInByUserId).HasColumnName("CheckedInByUserId").HasColumnType("int");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.BookingId).HasDatabaseName("IX_BookingPassengers_Booking");
            entity.HasOne<Bookings>().WithMany().HasForeignKey(e => e.BookingId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Passengers_Booking");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.CheckedInByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Passengers_CheckedBy");
        });

        modelBuilder.Entity<BookingReschedules>(entity =>
        {
            entity.ToTable("BookingReschedules", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Reschedule_Amounts", @"OldTotalAmount >= 0 AND NewTotalAmount >= 0 AND RescheduleFee >= 0");
            });
            entity.HasKey(e => e.Id).HasName("PK_BookingReschedules");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.BookingId).HasColumnName("BookingId").HasColumnType("bigint");
            entity.Property(e => e.OldDepartureId).HasColumnName("OldDepartureId").HasColumnType("int");
            entity.Property(e => e.NewDepartureId).HasColumnName("NewDepartureId").HasColumnType("int");
            entity.Property(e => e.OldTotalAmount).HasColumnName("OldTotalAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.OldTotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.NewTotalAmount).HasColumnName("NewTotalAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.NewTotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.RescheduleFee).HasColumnName("RescheduleFee").HasColumnType("decimal(18,2)");
            entity.Property(e => e.RescheduleFee).HasDefaultValueSql(@"0");
            entity.Property(e => e.RescheduleFee).HasPrecision(18, 2);
            entity.Property(e => e.PriceDifference).HasColumnName("PriceDifference").HasColumnType("decimal(18,2)");
            entity.Property(e => e.PriceDifference).HasPrecision(18, 2);
            entity.Property(e => e.Reason).HasColumnName("Reason").HasColumnType("nvarchar(300)");
            entity.Property(e => e.Reason).HasMaxLength(300);
            entity.Property(e => e.ApprovedByUserId).HasColumnName("ApprovedByUserId").HasColumnType("int");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => new { e.BookingId, e.CreatedAt }).HasDatabaseName("IX_BookingReschedules_Booking").IsDescending(false, true);
            entity.HasOne<Bookings>().WithMany().HasForeignKey(e => e.BookingId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reschedule_Booking");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.OldDepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reschedule_OldDep");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.NewDepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reschedule_NewDep");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.ApprovedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reschedule_ApprovedBy");
        });

        modelBuilder.Entity<PaymentTransactions>(entity =>
        {
            entity.ToTable("PaymentTransactions", "dbo", table =>
            {
                table.HasCheckConstraint("CK_PayTx_Method", @"Method IN ('QR_SIMULATED', 'BANK_TRANSFER_MANUAL', 'CASH')");
                table.HasCheckConstraint("CK_PayTx_Amounts", @"ExpectedAmount > 0 AND ActualReceivedAmount >= 0");
                table.HasCheckConstraint("CK_PayTx_Status", @"Status IN ('Pending', 'Succeeded', 'Failed', 'NeedReconciliation', 'Expired', 'Cancelled')");
                table.HasCheckConstraint("CK_PayTx_ReconStatus", @"ReconciliationStatus IN ('Unreconciled', 'Matched', 'Discrepancy', 'Resolved')");
            });
            entity.HasKey(e => e.Id).HasName("PK_PaymentTransactions");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.TransactionCode).HasColumnName("TransactionCode").HasColumnType("varchar(40)");
            entity.Property(e => e.TransactionCode).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.BookingId).HasColumnName("BookingId").HasColumnType("bigint");
            entity.Property(e => e.CompanyBankAccountId).HasColumnName("CompanyBankAccountId").HasColumnType("int");
            entity.Property(e => e.Method).HasColumnName("Method").HasColumnType("varchar(30)");
            entity.Property(e => e.Method).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.TransferContent).HasColumnName("TransferContent").HasColumnType("varchar(100)");
            entity.Property(e => e.TransferContent).IsUnicode(false).HasMaxLength(100);
            entity.Property(e => e.QrPayload).HasColumnName("QrPayload").HasColumnType("nvarchar(600)");
            entity.Property(e => e.QrPayload).HasMaxLength(600);
            entity.Property(e => e.ExpectedAmount).HasColumnName("ExpectedAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.ExpectedAmount).HasPrecision(18, 2);
            entity.Property(e => e.ActualReceivedAmount).HasColumnName("ActualReceivedAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualReceivedAmount).HasDefaultValueSql(@"0");
            entity.Property(e => e.ActualReceivedAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasColumnName("Status").HasColumnType("varchar(30)");
            entity.Property(e => e.Status).HasDefaultValueSql(@"'Pending'");
            entity.Property(e => e.Status).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.ReconciliationStatus).HasColumnName("ReconciliationStatus").HasColumnType("varchar(20)");
            entity.Property(e => e.ReconciliationStatus).HasDefaultValueSql(@"'Unreconciled'");
            entity.Property(e => e.ReconciliationStatus).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.IdempotencyKey).HasColumnName("IdempotencyKey").HasColumnType("varchar(100)");
            entity.Property(e => e.IdempotencyKey).IsUnicode(false).HasMaxLength(100);
            entity.Property(e => e.BankCode).HasColumnName("BankCode").HasColumnType("varchar(20)");
            entity.Property(e => e.BankCode).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.BankRef).HasColumnName("BankRef").HasColumnType("varchar(100)");
            entity.Property(e => e.BankRef).IsUnicode(false).HasMaxLength(100);
            entity.Property(e => e.EvidenceUrl).HasColumnName("EvidenceUrl").HasColumnType("nvarchar(500)");
            entity.Property(e => e.EvidenceUrl).HasMaxLength(500);
            entity.Property(e => e.ReconciledByUserId).HasColumnName("ReconciledByUserId").HasColumnType("int");
            entity.Property(e => e.ReconciledAt).HasColumnName("ReconciledAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.ReconciliationNote).HasColumnName("ReconciliationNote").HasColumnType("nvarchar(400)");
            entity.Property(e => e.ReconciliationNote).HasMaxLength(400);
            entity.Property(e => e.ExpiresAt).HasColumnName("ExpiresAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.PaidAt).HasColumnName("PaidAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.RawCallbackJson).HasColumnName("RawCallbackJson").HasColumnType("nvarchar(max)");
            entity.Property(e => e.RowVersion).HasColumnName("RowVersion").HasColumnType("rowversion");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.TransactionCode).IsUnique().HasDatabaseName("UQ_PaymentTransactions_Code");
            entity.HasIndex(e => e.IdempotencyKey).IsUnique().HasDatabaseName("UX_PaymentTransactions_IdempotencyKey").HasFilter(@"IdempotencyKey IS NOT NULL");
            entity.HasIndex(e => new { e.BankCode, e.BankRef }).IsUnique().HasDatabaseName("UX_PaymentTransactions_BankRef").HasFilter(@"BankRef IS NOT NULL");
            entity.HasIndex(e => new { e.BookingId, e.CreatedAt }).HasDatabaseName("IX_PaymentTransactions_Booking").IsDescending(false, true);
            entity.HasIndex(e => new { e.Status, e.ReconciliationStatus, e.CreatedAt }).HasDatabaseName("IX_PaymentTransactions_ReconQueue").IsDescending(false, false, true);
            entity.HasOne<Bookings>().WithMany().HasForeignKey(e => e.BookingId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_PayTx_Booking");
            entity.HasOne<CompanyBankAccounts>().WithMany().HasForeignKey(e => e.CompanyBankAccountId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_PayTx_BankAcc");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.ReconciledByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_PayTx_ReconciledBy");
        });

        modelBuilder.Entity<RefundRequests>(entity =>
        {
            entity.ToTable("RefundRequests", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Refunds_Amounts", @"PaidAmountSnapshot >= 0 AND PenaltyAmountSnapshot >= 0 AND RefundAmount >= 0");
                table.HasCheckConstraint("CK_Refunds_Status", @"Status IN ('PendingApproval', 'Approved', 'Rejected', 'Completed')");
            });
            entity.HasKey(e => e.Id).HasName("PK_RefundRequests");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.RefundCode).HasColumnName("RefundCode").HasColumnType("varchar(40)");
            entity.Property(e => e.RefundCode).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.BookingId).HasColumnName("BookingId").HasColumnType("bigint");
            entity.Property(e => e.RequestedByUserId).HasColumnName("RequestedByUserId").HasColumnType("int");
            entity.Property(e => e.Reason).HasColumnName("Reason").HasColumnType("nvarchar(400)");
            entity.Property(e => e.Reason).HasMaxLength(400);
            entity.Property(e => e.PaidAmountSnapshot).HasColumnName("PaidAmountSnapshot").HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaidAmountSnapshot).HasPrecision(18, 2);
            entity.Property(e => e.PenaltyAmountSnapshot).HasColumnName("PenaltyAmountSnapshot").HasColumnType("decimal(18,2)");
            entity.Property(e => e.PenaltyAmountSnapshot).HasPrecision(18, 2);
            entity.Property(e => e.RefundAmount).HasColumnName("RefundAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.RefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasColumnName("Status").HasColumnType("varchar(20)");
            entity.Property(e => e.Status).HasDefaultValueSql(@"'PendingApproval'");
            entity.Property(e => e.Status).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.BankName).HasColumnName("BankName").HasColumnType("nvarchar(120)");
            entity.Property(e => e.BankName).HasMaxLength(120);
            entity.Property(e => e.BankAccountNumber).HasColumnName("BankAccountNumber").HasColumnType("varchar(40)");
            entity.Property(e => e.BankAccountNumber).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.BankAccountHolder).HasColumnName("BankAccountHolder").HasColumnType("nvarchar(150)");
            entity.Property(e => e.BankAccountHolder).HasMaxLength(150);
            entity.Property(e => e.ApprovedByUserId).HasColumnName("ApprovedByUserId").HasColumnType("int");
            entity.Property(e => e.ApprovedAt).HasColumnName("ApprovedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CompletedByUserId).HasColumnName("CompletedByUserId").HasColumnType("int");
            entity.Property(e => e.CompletedAt).HasColumnName("CompletedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.TransferRef).HasColumnName("TransferRef").HasColumnType("varchar(100)");
            entity.Property(e => e.TransferRef).IsUnicode(false).HasMaxLength(100);
            entity.Property(e => e.RejectReason).HasColumnName("RejectReason").HasColumnType("nvarchar(300)");
            entity.Property(e => e.RejectReason).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.RefundCode).IsUnique().HasDatabaseName("UQ_RefundRequests_Code");
            entity.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("IX_RefundRequests_Status").IsDescending(false, true);
            entity.HasOne<Bookings>().WithMany().HasForeignKey(e => e.BookingId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Refunds_Booking");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.RequestedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Refunds_RequestedBy");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.ApprovedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Refunds_ApprovedBy");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.CompletedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Refunds_CompletedBy");
        });

        modelBuilder.Entity<SimulatedInvoices>(entity =>
        {
            entity.ToTable("SimulatedInvoices", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Invoices_BuyerType", @"BuyerType IN ('B2C', 'B2B')");
                table.HasCheckConstraint("CK_Invoices_Status", @"Status IN ('Draft', 'Issued', 'Cancelled')");
                table.HasCheckConstraint("CK_Invoices_Amounts", @"AmountBeforeTax >= 0 AND VatRate BETWEEN 0 AND 100 AND VatAmount >= 0 AND TotalAmount >= 0");
            });
            entity.HasKey(e => e.Id).HasName("PK_SimulatedInvoices");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.BookingId).HasColumnName("BookingId").HasColumnType("bigint");
            entity.Property(e => e.InvoiceNumber).HasColumnName("InvoiceNumber").HasColumnType("varchar(40)");
            entity.Property(e => e.InvoiceNumber).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.SerialNumber).HasColumnName("SerialNumber").HasColumnType("varchar(20)");
            entity.Property(e => e.SerialNumber).HasDefaultValueSql(@"'1C26TVT'");
            entity.Property(e => e.SerialNumber).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.LookupCode).HasColumnName("LookupCode").HasColumnType("varchar(40)");
            entity.Property(e => e.LookupCode).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.BuyerType).HasColumnName("BuyerType").HasColumnType("varchar(20)");
            entity.Property(e => e.BuyerType).HasDefaultValueSql(@"'B2B'");
            entity.Property(e => e.BuyerType).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.BuyerName).HasColumnName("BuyerName").HasColumnType("nvarchar(200)");
            entity.Property(e => e.BuyerName).HasMaxLength(200);
            entity.Property(e => e.BuyerTaxCode).HasColumnName("BuyerTaxCode").HasColumnType("varchar(20)");
            entity.Property(e => e.BuyerTaxCode).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.BuyerAddress).HasColumnName("BuyerAddress").HasColumnType("nvarchar(300)");
            entity.Property(e => e.BuyerAddress).HasMaxLength(300);
            entity.Property(e => e.BuyerEmail).HasColumnName("BuyerEmail").HasColumnType("nvarchar(150)");
            entity.Property(e => e.BuyerEmail).HasMaxLength(150);
            entity.Property(e => e.AmountBeforeTax).HasColumnName("AmountBeforeTax").HasColumnType("decimal(18,2)");
            entity.Property(e => e.AmountBeforeTax).HasPrecision(18, 2);
            entity.Property(e => e.VatRate).HasColumnName("VatRate").HasColumnType("decimal(5,2)");
            entity.Property(e => e.VatRate).HasPrecision(5, 2);
            entity.Property(e => e.VatAmount).HasColumnName("VatAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.VatAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasColumnName("TotalAmount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasColumnName("Status").HasColumnType("varchar(20)");
            entity.Property(e => e.Status).HasDefaultValueSql(@"'Draft'");
            entity.Property(e => e.Status).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.IssuedByUserId).HasColumnName("IssuedByUserId").HasColumnType("int");
            entity.Property(e => e.IssuedAt).HasColumnName("IssuedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CancelledAt).HasColumnName("CancelledAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CancelReason).HasColumnName("CancelReason").HasColumnType("nvarchar(300)");
            entity.Property(e => e.CancelReason).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.BookingId).IsUnique().HasDatabaseName("UQ_SimulatedInvoices_Booking");
            entity.HasIndex(e => e.InvoiceNumber).IsUnique().HasDatabaseName("UQ_SimulatedInvoices_Number");
            entity.HasIndex(e => e.LookupCode).IsUnique().HasDatabaseName("UQ_SimulatedInvoices_Lookup");
            entity.HasOne<Bookings>().WithMany().HasForeignKey(e => e.BookingId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Invoices_Booking");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.IssuedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Invoices_IssuedBy");
        });

        modelBuilder.Entity<SupplierPayments>(entity =>
        {
            entity.ToTable("SupplierPayments", "dbo", table =>
            {
                table.HasCheckConstraint("CK_SuppPay_Amount", @"Amount > 0");
                table.HasCheckConstraint("CK_SuppPay_Method", @"Method IN ('BankTransfer', 'Cash')");
            });
            entity.HasKey(e => e.Id).HasName("PK_SupplierPayments");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.PaymentCode).HasColumnName("PaymentCode").HasColumnType("varchar(40)");
            entity.Property(e => e.PaymentCode).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.DepartureServiceId).HasColumnName("DepartureServiceId").HasColumnType("int");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierId").HasColumnType("int");
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Method).HasColumnName("Method").HasColumnType("varchar(30)");
            entity.Property(e => e.Method).HasDefaultValueSql(@"'BankTransfer'");
            entity.Property(e => e.Method).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.BankRef).HasColumnName("BankRef").HasColumnType("varchar(100)");
            entity.Property(e => e.BankRef).IsUnicode(false).HasMaxLength(100);
            entity.Property(e => e.EvidenceUrl).HasColumnName("EvidenceUrl").HasColumnType("nvarchar(500)");
            entity.Property(e => e.EvidenceUrl).HasMaxLength(500);
            entity.Property(e => e.PaidAt).HasColumnName("PaidAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.PaidByUserId).HasColumnName("PaidByUserId").HasColumnType("int");
            entity.Property(e => e.Notes).HasColumnName("Notes").HasColumnType("nvarchar(300)");
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.PaymentCode).IsUnique().HasDatabaseName("UQ_SupplierPayments_Code");
            entity.HasIndex(e => new { e.DepartureServiceId, e.PaidAt }).HasDatabaseName("IX_SupplierPayments_Service").IsDescending(false, true);
            entity.HasOne<DepartureServices>().WithMany().HasForeignKey(e => e.DepartureServiceId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_SuppPay_Service");
            entity.HasOne<Suppliers>().WithMany().HasForeignKey(e => e.SupplierId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_SuppPay_Supplier");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.PaidByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_SuppPay_PaidBy");
        });

        modelBuilder.Entity<FinancialLedgers>(entity =>
        {
            entity.ToTable("FinancialLedgers", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Ledgers_Direction", @"Direction IN ('In', 'Out')");
                table.HasCheckConstraint("CK_Ledgers_Category", @"
        Category IN ('CustomerPayment', 'CustomerRefund', 'SupplierPayment', 'ExtraExpense', 'OtherAdjustment')
    ");
                table.HasCheckConstraint("CK_Ledgers_Amount", @"Amount > 0");
            });
            entity.HasKey(e => e.Id).HasName("PK_FinancialLedgers");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.EntryCode).HasColumnName("EntryCode").HasColumnType("varchar(40)");
            entity.Property(e => e.EntryCode).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.Direction).HasColumnName("Direction").HasColumnType("varchar(10)");
            entity.Property(e => e.Direction).IsUnicode(false).HasMaxLength(10);
            entity.Property(e => e.Category).HasColumnName("Category").HasColumnType("varchar(40)");
            entity.Property(e => e.Category).IsUnicode(false).HasMaxLength(40);
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.DepartureId).HasColumnName("DepartureId").HasColumnType("int");
            entity.Property(e => e.BookingId).HasColumnName("BookingId").HasColumnType("bigint");
            entity.Property(e => e.PaymentTransactionId).HasColumnName("PaymentTransactionId").HasColumnType("bigint");
            entity.Property(e => e.RefundRequestId).HasColumnName("RefundRequestId").HasColumnType("bigint");
            entity.Property(e => e.SupplierPaymentId).HasColumnName("SupplierPaymentId").HasColumnType("bigint");
            entity.Property(e => e.ExtraExpenseId).HasColumnName("ExtraExpenseId").HasColumnType("bigint");
            entity.Property(e => e.Description).HasColumnName("Description").HasColumnType("nvarchar(400)");
            entity.Property(e => e.Description).HasMaxLength(400);
            entity.Property(e => e.OccurredAt).HasColumnName("OccurredAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.RecordedByUserId).HasColumnName("RecordedByUserId").HasColumnType("int");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.EntryCode).IsUnique().HasDatabaseName("UQ_FinancialLedgers_Code");
            entity.HasIndex(e => new { e.OccurredAt, e.Direction, e.Category }).HasDatabaseName("IX_FinancialLedgers_Occurred").IsDescending(true, false, false);
            entity.HasIndex(e => new { e.DepartureId, e.Direction }).HasDatabaseName("IX_FinancialLedgers_Departure").HasFilter(@"DepartureId IS NOT NULL");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.DepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Ledgers_Departure");
            entity.HasOne<Bookings>().WithMany().HasForeignKey(e => e.BookingId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Ledgers_Booking");
            entity.HasOne<PaymentTransactions>().WithMany().HasForeignKey(e => e.PaymentTransactionId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Ledgers_PayTx");
            entity.HasOne<RefundRequests>().WithMany().HasForeignKey(e => e.RefundRequestId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Ledgers_Refund");
            entity.HasOne<SupplierPayments>().WithMany().HasForeignKey(e => e.SupplierPaymentId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Ledgers_SuppPay");
            entity.HasOne<DepartureExtraExpenses>().WithMany().HasForeignKey(e => e.ExtraExpenseId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Ledgers_ExtraExp");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.RecordedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Ledgers_RecordedBy");
        });

        modelBuilder.Entity<TourIncidents>(entity =>
        {
            entity.ToTable("TourIncidents", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Incidents_Severity", @"Severity IN ('Low', 'Medium', 'High', 'Critical')");
                table.HasCheckConstraint("CK_Incidents_Status", @"Status IN ('Open', 'InProgress', 'Resolved')");
            });
            entity.HasKey(e => e.Id).HasName("PK_TourIncidents");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.DepartureId).HasColumnName("DepartureId").HasColumnType("int");
            entity.Property(e => e.ReportedByUserId).HasColumnName("ReportedByUserId").HasColumnType("int");
            entity.Property(e => e.Title).HasColumnName("Title").HasColumnType("nvarchar(200)");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Severity).HasColumnName("Severity").HasColumnType("varchar(20)");
            entity.Property(e => e.Severity).HasDefaultValueSql(@"'Medium'");
            entity.Property(e => e.Severity).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.Description).HasColumnName("Description").HasColumnType("nvarchar(max)");
            entity.Property(e => e.ResolutionNote).HasColumnName("ResolutionNote").HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status).HasColumnName("Status").HasColumnType("varchar(20)");
            entity.Property(e => e.Status).HasDefaultValueSql(@"'Open'");
            entity.Property(e => e.Status).IsUnicode(false).HasMaxLength(20);
            entity.Property(e => e.OccurredAt).HasColumnName("OccurredAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.OccurredAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.Property(e => e.ResolvedAt).HasColumnName("ResolvedAt").HasColumnType("datetime2(0)");
            entity.HasIndex(e => new { e.DepartureId, e.Status }).HasDatabaseName("IX_TourIncidents_DepStatus");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.DepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Incidents_Departure");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.ReportedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Incidents_Reporter");
        });

        modelBuilder.Entity<TourReviews>(entity =>
        {
            entity.ToTable("TourReviews", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Reviews_Ratings", @"
        OverallRating BETWEEN 1 AND 5
        AND (GuideRating IS NULL OR GuideRating BETWEEN 1 AND 5)
        AND (ItineraryRating IS NULL OR ItineraryRating BETWEEN 1 AND 5)
        AND (HotelFoodRating IS NULL OR HotelFoodRating BETWEEN 1 AND 5)
        AND (TransportRating IS NULL OR TransportRating BETWEEN 1 AND 5)
    ");
                table.HasCheckConstraint("CK_Reviews_ImagesJson", @"ImagesJson IS NULL OR ISJSON(ImagesJson) = 1");
            });
            entity.HasKey(e => e.Id).HasName("PK_TourReviews");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.TourId).HasColumnName("TourId").HasColumnType("int");
            entity.Property(e => e.DepartureId).HasColumnName("DepartureId").HasColumnType("int");
            entity.Property(e => e.BookingId).HasColumnName("BookingId").HasColumnType("bigint");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerId").HasColumnType("int");
            entity.Property(e => e.OverallRating).HasColumnName("OverallRating").HasColumnType("tinyint");
            entity.Property(e => e.GuideRating).HasColumnName("GuideRating").HasColumnType("tinyint");
            entity.Property(e => e.ItineraryRating).HasColumnName("ItineraryRating").HasColumnType("tinyint");
            entity.Property(e => e.HotelFoodRating).HasColumnName("HotelFoodRating").HasColumnType("tinyint");
            entity.Property(e => e.TransportRating).HasColumnName("TransportRating").HasColumnType("tinyint");
            entity.Property(e => e.Comment).HasColumnName("Comment").HasColumnType("nvarchar(1500)");
            entity.Property(e => e.Comment).HasMaxLength(1500);
            entity.Property(e => e.ImagesJson).HasColumnName("ImagesJson").HasColumnType("nvarchar(max)");
            entity.Property(e => e.StaffReply).HasColumnName("StaffReply").HasColumnType("nvarchar(1000)");
            entity.Property(e => e.StaffReply).HasMaxLength(1000);
            entity.Property(e => e.RepliedByUserId).HasColumnName("RepliedByUserId").HasColumnType("int");
            entity.Property(e => e.RepliedAt).HasColumnName("RepliedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.IsHidden).HasColumnName("IsHidden").HasColumnType("bit");
            entity.Property(e => e.IsHidden).HasDefaultValueSql(@"0");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => e.BookingId).IsUnique().HasDatabaseName("UQ_TourReviews_Booking");
            entity.HasIndex(e => new { e.TourId, e.IsHidden, e.CreatedAt }).HasDatabaseName("IX_TourReviews_TourVisible").IsDescending(false, false, true);
            entity.HasOne<Tours>().WithMany().HasForeignKey(e => e.TourId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reviews_Tour");
            entity.HasOne<TourDepartures>().WithMany().HasForeignKey(e => e.DepartureId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reviews_Departure");
            entity.HasOne<Bookings>().WithMany().HasForeignKey(e => e.BookingId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reviews_Booking");
            entity.HasOne<Customers>().WithMany().HasForeignKey(e => e.CustomerId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reviews_Customer");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.RepliedByUserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reviews_RepliedBy");
        });

        modelBuilder.Entity<Notifications>(entity =>
        {
            entity.ToTable("Notifications", "dbo", table =>
            {
            });
            entity.HasKey(e => e.Id).HasName("PK_Notifications");
            entity.Property(e => e.Id).HasColumnName("Id").HasColumnType("bigint");
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.UserId).HasColumnName("UserId").HasColumnType("int");
            entity.Property(e => e.TargetRole).HasColumnName("TargetRole").HasColumnType("varchar(30)");
            entity.Property(e => e.TargetRole).IsUnicode(false).HasMaxLength(30);
            entity.Property(e => e.Type).HasColumnName("Type").HasColumnType("varchar(50)");
            entity.Property(e => e.Type).IsUnicode(false).HasMaxLength(50);
            entity.Property(e => e.Title).HasColumnName("Title").HasColumnType("nvarchar(200)");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Body).HasColumnName("Body").HasColumnType("nvarchar(800)");
            entity.Property(e => e.Body).HasMaxLength(800);
            entity.Property(e => e.LinkUrl).HasColumnName("LinkUrl").HasColumnType("nvarchar(300)");
            entity.Property(e => e.LinkUrl).HasMaxLength(300);
            entity.Property(e => e.IsRead).HasColumnName("IsRead").HasColumnType("bit");
            entity.Property(e => e.IsRead).HasDefaultValueSql(@"0");
            entity.Property(e => e.ReadAt).HasColumnName("ReadAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(@"SYSUTCDATETIME()");
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt }).HasDatabaseName("IX_Notifications_UserUnread").IsDescending(false, false, true).HasFilter(@"UserId IS NOT NULL");
            entity.HasOne<Users>().WithMany().HasForeignKey(e => e.UserId).HasPrincipalKey(e => e.Id).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Notifications_User");
        });

    }
}
