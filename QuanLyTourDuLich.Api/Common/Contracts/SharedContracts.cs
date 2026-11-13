namespace QuanLyTourDuLich.Api.Common.Contracts;

public sealed record PagedRequest(
    int PageIndex = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false)
{
    public int NormalizedPageIndex => Math.Max(1, PageIndex);
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, 200);
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageIndex,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public interface IAuditLogService
{
    Task WriteAsync(
        string action,
        string entityName,
        string entityId,
        object? oldValue = null,
        object? newValue = null,
        int? actorUserIdOverride = null,
        string? actorRoleOverride = null,
        CancellationToken cancellationToken = default);
}

public interface ISystemSettingService
{
    Task<string> GetStringAsync(string key, string fallback, CancellationToken cancellationToken = default);
    Task<int> GetIntAsync(string key, int fallback, CancellationToken cancellationToken = default);
    Task<decimal> GetDecimalAsync(string key, decimal fallback, CancellationToken cancellationToken = default);
    Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task NotifyUserAsync(int userId, string type, string title, string body, string? linkUrl = null, CancellationToken cancellationToken = default);
    Task NotifyRoleAsync(string targetRole, string type, string title, string body, string? linkUrl = null, CancellationToken cancellationToken = default);
}

public sealed record FinancialLedgerAppendRequest(
    string Direction,
    string Category,
    decimal Amount,
    string Description,
    int? DepartureId = null,
    long? BookingId = null,
    long? PaymentTransactionId = null,
    long? RefundRequestId = null,
    long? SupplierPaymentId = null,
    long? ExtraExpenseId = null,
    int? RecordedByUserId = null,
    DateTime? OccurredAt = null);

public interface IFinancialLedgerWriter
{
    Task AppendAsync(
        FinancialLedgerAppendRequest request,
        bool saveChanges = false,
        CancellationToken cancellationToken = default);
}

public sealed record VietQrPayloadResult(
    string QrContent,
    string QrImageDataUrl,
    string BankCode,
    string BankBin,
    string BankName,
    string AccountNumber,
    string AccountHolder);

public interface IPaymentGateway
{
    VietQrPayloadResult GenerateVietQrPayload(
        string bankCode,
        string bankBin,
        string bankName,
        string accountNumber,
        string accountHolder,
        decimal amount,
        string transferContent);

    bool VerifyWebhookSignature(string transactionCode, decimal paidAmount, string? signature);
}

public sealed record SimulatedInvoiceRenderResult(
    string InvoiceNumber,
    string SerialNumber,
    string LookupCode,
    string HtmlContent);

public interface ITaxInvoiceService
{
    Task<SimulatedInvoiceRenderResult> RenderSimulatedInvoiceHtmlAsync(
        long bookingId,
        string invoiceNumber,
        string serialNumber,
        string lookupCode,
        CancellationToken cancellationToken = default);
}
