using QuanLyTourDuLich.Api.Common.Exceptions;

namespace QuanLyTourDuLich.Api.Modules.Operations;

public static class SettlementLockRules
{
    public static void EnsureEditable(string departureStatus)
    {
        if (departureStatus == "Settled")
            throw new ConflictException("Đoàn đã khóa sổ; không thể thay đổi dữ liệu vận hành/tài chính.");
    }
}
