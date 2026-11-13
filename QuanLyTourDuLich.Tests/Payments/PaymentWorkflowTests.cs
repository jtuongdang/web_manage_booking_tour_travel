using Microsoft.EntityFrameworkCore;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Data;
using QuanLyTourDuLich.Api.Models.Entities;
using QuanLyTourDuLich.Api.Modules.Payments;
using Xunit;

namespace QuanLyTourDuLich.Tests.Payments;

public sealed class PaymentWorkflowTests
{
    [Fact]
    public void VietQrPayload_ContainsAmountAndTransferContent_AndSignatureVerifies()
    {
        var gateway = new VietQrPaymentGateway();
        var qr = gateway.GenerateVietQrPayload("VCB", "970436", "Vietcombank", "0123456789", "TOUR COMPANY",
            1_250_000m, "TOUR BK2026111301");

        Assert.Contains("9704360123456789", qr.QrContent);
        Assert.Contains("1250000.00", qr.QrContent);
        Assert.Contains("TOUR BK2026111301", qr.QrContent);
        Assert.StartsWith("data:image/svg+xml;base64,", qr.QrImageDataUrl);
        var signature = VietQrPaymentGateway.CreateMockSignature("PAY-1", 1_250_000m);
        Assert.True(gateway.VerifyWebhookSignature("PAY-1", 1_250_000m, signature));
        Assert.False(gateway.VerifyWebhookSignature("PAY-1", 1_250_001m, signature));
    }

    [Fact]
    public void Idempotency_ReplaysSameTransaction_AndRejectsKeyReuse()
    {
        var payment = new PaymentTransactions { Id = 10, TransactionCode = "PAY-A", IdempotencyKey = "webhook-1" };
        Assert.Equal(IdempotencyResult.Replay, PaymentRules.GetIdempotencyResult(payment, "PAY-A"));
        Assert.Equal(IdempotencyResult.KeyReused, PaymentRules.GetIdempotencyResult(payment, "PAY-B"));
    }

    [Fact]
    public void BankReferenceGuard_RejectsDuplicatePairButAllowsOtherBankOrCurrentPayment()
    {
        var existing = new[] { new PaymentTransactions { Id = 4, BankCode = "VCB", BankRef = "STMT-100" } };
        Assert.True(PaymentRules.HasDuplicateBankReference(existing, 5, "VCB", "STMT-100"));
        Assert.False(PaymentRules.HasDuplicateBankReference(existing, 5, "ACB", "STMT-100"));
        Assert.False(PaymentRules.HasDuplicateBankReference(existing, 4, "VCB", "STMT-100"));
        Assert.False(PaymentRules.HasDuplicateBankReference(existing, 5, "VCB", null));
    }

    [Theory]
    [InlineData(500, 500, "Succeeded")]
    [InlineData(500, 300, "NeedReconciliation")]
    [InlineData(500, 700, "NeedReconciliation")]
    public void CallbackAmountMismatchIsSentToReconciliation(decimal expected, decimal actual, string status)
    {
        Assert.Equal(status, PaymentRules.GetCallbackStatus(expected, actual));
    }

    [Fact]
    public void PaymentTransactions_KeepFilteredUniqueIndexesForIdempotencyAndBankReference()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=Unused;User Id=unused;Password=unused;TrustServerCertificate=True")
            .Options;
        using var db = new AppDbContext(options);
        var indexes = db.Model.FindEntityType(typeof(PaymentTransactions))!.GetIndexes();
        Assert.Contains(indexes, x => x.IsUnique && x.GetFilter() == "IdempotencyKey IS NOT NULL");
        Assert.Contains(indexes, x => x.IsUnique && x.GetFilter() == "BankRef IS NOT NULL");
    }
}
