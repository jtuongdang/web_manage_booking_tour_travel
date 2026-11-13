using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using QuanLyTourDuLich.Api.Common.Contracts;

namespace QuanLyTourDuLich.Api.Modules.Payments;

public sealed class VietQrPaymentGateway : IPaymentGateway
{
    public VietQrPayloadResult GenerateVietQrPayload(
        string bankCode, string bankBin, string bankName, string accountNumber,
        string accountHolder, decimal amount, string transferContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bankCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankBin);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        var transfer = NormalizeTransferContent(transferContent);
        var merchantInfo = EmvField("00", "A000000727") + EmvField("01", bankBin + accountNumber);
        var additional = EmvField("62", EmvField("08", transfer));
        var payload = EmvField("00", "01") + EmvField("01", "12") +
                      EmvField("38", merchantInfo) + EmvField("53", "704") +
                      EmvField("54", amount.ToString("0.00", CultureInfo.InvariantCulture)) +
                      EmvField("58", "VN") + EmvField("59", TrimUtf8(accountHolder, 25)) +
                      EmvField("60", TrimUtf8(bankName, 15)) + additional + "6304";
        payload += Crc16Ccitt(payload).ToString("X4", CultureInfo.InvariantCulture);

        // This is a self-contained visual simulator. It embeds a deterministic matrix-like SVG;
        // replacing it with a bank-grade QR encoder does not change the gateway contract.
        var svg = BuildSimulatorSvg(payload);
        var dataUrl = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
        return new VietQrPayloadResult(payload, dataUrl, bankCode, bankBin, bankName, accountNumber, accountHolder);
    }

    public bool VerifyWebhookSignature(string transactionCode, decimal paidAmount, string? signature)
    {
        if (string.IsNullOrWhiteSpace(transactionCode) || string.IsNullOrWhiteSpace(signature)) return false;
        var expected = CreateMockSignature(transactionCode, paidAmount);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }

    public static string CreateMockSignature(string transactionCode, decimal paidAmount)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{transactionCode}|{paidAmount.ToString("0.00", CultureInfo.InvariantCulture)}|VietQR-Simulator"));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizeTransferContent(string value)
    {
        var normalized = new string((value ?? string.Empty).ToUpperInvariant()
            .Where(c => char.IsAsciiLetterOrDigit(c) || c == ' ')
            .ToArray());
        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return TrimUtf8(normalized, 25);
    }

    private static string EmvField(string id, string value)
    {
        var length = Encoding.UTF8.GetByteCount(value);
        if (length > 99) throw new ArgumentOutOfRangeException(nameof(value), "EMVCo field exceeds 99 bytes.");
        return id + length.ToString("D2", CultureInfo.InvariantCulture) + value;
    }

    private static string TrimUtf8(string value, int maxBytes)
    {
        var builder = new StringBuilder();
        foreach (var rune in value.EnumerateRunes())
        {
            if (Encoding.UTF8.GetByteCount(builder.ToString()) + rune.Utf8SequenceLength > maxBytes) break;
            builder.Append(rune.ToString());
        }
        return builder.ToString();
    }

    private static ushort Crc16Ccitt(string value)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.ASCII.GetBytes(value))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++) crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc;
    }

    private static string BuildSimulatorSvg(string payload)
    {
        const int cells = 29;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        var rects = new StringBuilder();
        for (var y = 0; y < cells; y++)
        for (var x = 0; x < cells; x++)
        {
            var finder = false;
            var finderPixel = false;
            foreach (var (left, top) in new[] { (0, 0), (cells - 7, 0), (0, cells - 7) })
            {
                if (!IsFinder(x, y, left, top)) continue;
                finder = true;
                finderPixel = FinderPixel(x - left, y - top);
                break;
            }
            var bit = (bytes[(x * 7 + y * 13) % bytes.Length] >> ((x + y) % 8) & 1) != 0;
            if (finder ? finderPixel : bit)
                rects.Append("<rect x=\"").Append(x + 2).Append("\" y=\"").Append(y + 2).Append("\" width=\"1\" height=\"1\"/>");
        }
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 33 33\"><rect width=\"33\" height=\"33\" fill=\"white\"/><g fill=\"#111\">{rects}</g></svg>";
    }

    private static bool IsFinder(int x, int y, int left, int top) => x >= left && x < left + 7 && y >= top && y < top + 7;
    private static bool FinderPixel(int x, int y)
    {
        return x is 0 or 6 || y is 0 or 6 || (x is >= 2 and <= 4 && y is >= 2 and <= 4);
    }
}
