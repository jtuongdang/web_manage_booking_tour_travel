using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Data;
using Xunit;

namespace QuanLyTourDuLich.Tests;

public sealed class SchemaMappingTests
{
    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(local);Database=SchemaMetadataOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    [Fact]
    public void Maps_all_33_schema_tables()
    {
        using var context = CreateContext();
        Assert.Equal(33, context.Model.GetEntityTypes().Count());
        Assert.Equal(33, context.Model.GetEntityTypes().Select(e => e.GetTableName()).Distinct().Count());
    }

    [Theory]
    [InlineData("TourDepartures", "AvailableSlots", "(TotalCapacity - HeldSlots - BookedSlots)")]
    [InlineData("Bookings", "SeatsReserved", "(AdultCount + ChildCount)")]
    [InlineData("Bookings", "RemainingAmount", "(CASE WHEN TotalAmount > PaidAmount THEN TotalAmount - PaidAmount ELSE CAST(0 AS DECIMAL(18,2)) END)")]
    [InlineData("DepartureServices", "EstimatedCost", "(CAST(ROUND(Quantity * UnitPrice, 2) AS DECIMAL(18,2)))")]
    public void Maps_computed_columns(string table, string property, string? expectedFragment)
    {
        using var context = CreateContext();
        var entity = context.Model.GetEntityTypes().Single(e => e.GetTableName() == table);
        var column = entity.FindProperty(property)!;
        Assert.Equal(expectedFragment, column.GetComputedColumnSql());
        Assert.True(column.ValueGenerated.HasFlag(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate));
    }

    [Theory]
    [InlineData("TourDepartures")]
    [InlineData("Bookings")]
    [InlineData("PaymentTransactions")]
    [InlineData("Promotions")]
    [InlineData("DepartureServices")]
    public void Maps_schema_rowversion_columns(string table)
    {
        using var context = CreateContext();
        var entity = context.Model.GetEntityTypes().Single(e => e.GetTableName() == table);
        var rowVersion = entity.GetProperties().Single(p => p.IsConcurrencyToken);
        Assert.Equal("rowversion", rowVersion.GetColumnType());
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void Maps_filtered_unique_indexes()
    {
        using var context = CreateContext();
        var users = context.Model.GetEntityTypes().Single(e => e.GetTableName() == "Users");
        Assert.Contains(users.GetIndexes(), index => index.IsUnique && index.GetFilter() == "Phone IS NOT NULL");
        var payments = context.Model.GetEntityTypes().Single(e => e.GetTableName() == "PaymentTransactions");
        Assert.Contains(payments.GetIndexes(), index => index.IsUnique && index.GetFilter() == "IdempotencyKey IS NOT NULL");
        Assert.Contains(payments.GetIndexes(), index => index.IsUnique && index.GetFilter() == "BankRef IS NOT NULL");
    }
}
