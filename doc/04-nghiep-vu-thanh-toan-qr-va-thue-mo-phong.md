# 04. Nghiệp Vụ Thanh Toán (Mô Phỏng QR) & Thuế / Hóa Đơn (Mô Phỏng)

> **Lưu ý phạm vi:** Theo yêu cầu sản phẩm, phân hệ **Thanh toán quét mã QR** và **Thuế / Hóa đơn** được thiết kế ở mức **mô phỏng hoàn chỉnh luồng nghiệp vụ** để vận hành kiểm thử, demo và sử dụng nội bộ, đồng thời cấu trúc sẵn interface (`IPaymentGateway`, `ITaxInvoiceService`) để cắm cổng thật (VNPay, MoMo, SePay, VietQR API, MISA/VNPT e-Invoice) mà không phải sửa code nghiệp vụ cốt lõi.

---

## 1. Quản lý Công nợ & Lịch thanh toán (Payment Schedule)

Một Booking có thể thanh toán 1 lần (100%) hoặc chia 2 đợt:
1. **Đặt cọc (Deposit):** VD tối thiểu 30%–50% tổng giá trị booking ngay khi đặt để xác nhận giữ chỗ chính thức (`DepositPaid`).
2. **Thanh toán phần còn lại (Final Payment):** Phải hoàn tất trước ngày khởi hành $X$ ngày (VD: trước 5 ngày). Hệ thống tự động gửi thông báo nhắc nợ cho khách và Sales khi sắp đến hạn.
3. **Công thức tài chính trên Booking:**
   - `SubTotal`: Tổng tiền gốc (theo số lượng khách + phụ thu).
   - `DiscountAmount`: Tiền giảm giá từ Voucher/Khuyến mãi.
   - `TaxAmount`: Tiền thuế VAT (tính theo cấu hình mô phỏng).
   - `TotalAmount`: Tổng tiền khách phải trả (`SubTotal - DiscountAmount + TaxAmount` nếu giá chưa gồm VAT, hoặc tách ngược VAT nếu giá đã gồm VAT).
   - `PaidAmount`: Tổng tiền đã thanh toán thành công.
   - `RemainingAmount = TotalAmount - PaidAmount`.

---

## 2. Mô phỏng Quá trình Thanh toán Quét mã QR (QR Payment Simulation)

### 2.1. Luồng nghiệp vụ chi tiết
1. **Bước 1 — Khởi tạo phiên thanh toán QR (`Create QR Payment Intent`):**
   - Khách hoặc Sales chọn số tiền cần thanh toán (Tiền cọc hoặc Thanh toán toàn bộ / Phần còn lại) và phương thức `QR_SIMULATED` (hoặc `BANK_TRANSFER_MANUAL` / `CASH` tại quầy khi `SALES`/`ACCOUNTANT` thu hộ).
   - Hệ thống lấy thông tin tài khoản thụ hưởng từ `CompanyBankAccounts` (mặc định tài khoản đang kích hoạt trong `SystemSettings`) và tạo bản ghi `PaymentTransactions` với:
     - `TransactionCode`: Mã giao dịch duy nhất (VD: `PAY-20261113-0001`).
     - `TransferContent` (Nội dung chuyển khoản chuẩn hóa): VD `TOUR BK2026111301`.
     - `ExpectedAmount`: Số tiền cần thu của phiên thanh toán.
     - `ActualReceivedAmount`: Số tiền thực nhận (ban đầu `0`, cập nhật khi callback simulator hoặc khi Kế toán đối soát).
     - `ExpiresAt`: Thời hạn mã QR (VD: 15 phút).
     - `Status`: `Pending` (`Pending` | `Succeeded` | `Failed` | `NeedReconciliation` | `Expired` | `Cancelled`).
     - `ReconciliationStatus`: `Unreconciled` | `Matched` | `Discrepancy` | `Resolved`.
   - Hệ thống sinh **chuỗi/ảnh QR mô phỏng** (theo chuẩn VietQR payload kèm thông tin: Tên ngân hàng, BIN, Số tài khoản công ty, Tên chủ tài khoản, Số tiền, Nội dung chuyển khoản).

2. **Bước 2 — Hiển thị & Theo dõi trạng thái (Realtime/Polling):**
   - Giao diện hiển thị mã QR, đồng hồ đếm ngược và nút kiểm tra trạng thái.
   - Frontend polling API kiểm tra trạng thái giao dịch (hoặc nhận tín hiệu qua SignalR/WebSocket).

3. **Bước 3 — Mô phỏng Quét mã & Xác nhận từ Ngân hàng (Simulator):**
   - Do chưa kết nối ngân hàng thật, hệ thống cung cấp **Payment Simulator Panel / API giả lập** (chỉ bật ở môi trường Dev/Staging hoặc quyền Admin/Test):
     - **Kịch bản 1 — Thanh toán thành công (Happy Path):** Giả lập ngân hàng gửi Webhook callback về API với `TransactionCode`, `PaidAmount == ExpectedAmount`, chữ ký hợp lệ (`MockSignature`).
     - **Kịch bản 2 — Thanh toán thất bại / Hủy:** Giả lập khách hủy giao dịch hoặc lỗi ngân hàng $\rightarrow$ `Status = Failed`.
     - **Kịch bản 3 — Chuyển sai số tiền (Partial / Overpaid):** Giả lập khách chuyển thiếu hoặc thừa tiền $\rightarrow$ Hệ thống ghi nhận `ActualReceivedAmount`, chuyển `Status = NeedReconciliation`, `ReconciliationStatus = Discrepancy` và cảnh báo Sales/Kế toán thay vì tự động gạch nợ sai.
     - **Kịch bản 4 — Hết hạn QR (Timeout):** Quá `ExpiresAt` chưa có callback $\rightarrow$ Giao dịch chuyển `Expired`, yêu cầu tạo mã QR mới.

4. **Bước 4 — Xử lý Idempotency, Chống trùng BankRef & Cập nhật Booking:**
   - Webhook nhận callback phải kiểm tra **IdempotencyKey** (`UNIQUE` filtered index) để chỉ xử lý tối đa 1 lần.
   - Ràng buộc **chống đối soát trùng mã sao kê ngân hàng**: cặp `(BankCode, BankRef)` phải là duy nhất khi `BankRef IS NOT NULL`, tránh 1 giao dịch ngân hàng bị gán cho 2 đơn hàng khác nhau.
   - Khi giao dịch thành công: cập nhật `PaidAmount` trên `Bookings`, chuyển trạng thái Booking (`DepositPaid` hoặc `Confirmed`), chuyển chỗ từ `HeldSlots` sang `BookedSlots`, và tự động ghi bút toán thu vào sổ quỹ `FinancialLedgers` (`Direction = In`, `Category = CustomerPayment`).

### 2.2. Đối soát kế toán nội bộ (Manual Reconciliation)
- Trường hợp khách chuyển khoản ngoài luồng, chuyển sai số tiền (`NeedReconciliation`), hoặc nộp tiền mặt tại quầy: `ACCOUNTANT` sử dụng màn hình **Đối soát thu hộ / sao kê**:
  - Nhập `BankCode`, `BankRef` (số bút toán sao kê ngân hàng), `ActualReceivedAmount`, thời điểm nhận tiền, ảnh chứng từ biên lai (`EvidenceUrl`) và ghi chú đối soát.
  - Nếu khách chuyển thiếu (VD: thiếu 200.000đ): Kế toán có thể **xác nhận ghi nhận đúng số tiền thực nhận (`ActualReceivedAmount`)**, chuyển `ReconciliationStatus = Resolved`; `Bookings.PaidAmount` cộng đúng số thực nhận và phần còn thiếu vẫn nằm ở `RemainingAmount` để khách thanh toán tiếp (hoặc chuyển `Confirmed` nếu đủ điều kiện cọc/thanh toán).
  - Nếu khách chuyển thừa: ghi nhận số thực nhận, tạo ghi chú/đề xuất hoàn trả phần dư cho khách.

---

## 3. Nghiệp vụ Hoàn tiền (Refund)

Khi Booking được `SALES` / `OPERATOR` / `ADMIN` duyệt hủy hợp lệ (đã chốt mức phí phạt theo `CancellationPolicySnapshotJson`) hoặc đoàn bị hủy bởi công ty:
1. Hệ thống tính toán tự động:
   - `PenaltyAmount` = `TotalAmount * CancellationFeePercent` (hoặc `0` nếu công ty hủy đoàn).
   - `RefundableAmount = Max(0, PaidAmount - PenaltyAmount)`.
2. Nếu `RefundableAmount > 0`, hệ thống tạo yêu cầu hoàn tiền (`RefundRequests`) với trạng thái `PendingApproval`.
3. `ACCOUNTANT` / `ADMIN` kiểm tra, phê duyệt và thực hiện chi trả (nhập mã tham chiếu chuyển khoản hoàn trả `TransferRef`), tự động ghi sổ quỹ `FinancialLedgers` (`Direction = Out`, `Category = CustomerRefund`), và chuyển trạng thái Booking sang `Refunded`.

---

## 4. Mô phỏng Thuế & Hóa đơn điện tử (Simulated Tax & E-Invoice)

Không cần tích hợp trực tiếp với Tổng cục Thuế hay nhà cung cấp hóa đơn điện tử thật, nhưng hệ thống phải phản ánh đúng bản chất kế toán:

1. **Cấu hình Thuế VAT mô phỏng:**
   - Cấu hình mức thuế suất VAT mặc định trong hệ thống (VD: `8%` hoặc `10%`, có thể bật/tắt chế độ **Giá đã bao gồm VAT** hay **Giá chưa bao gồm VAT**).
   - Lưu rõ trên mỗi Booking: `AmountBeforeTax` (Tiền trước thuế), `VatRate` (% thuế), `VatAmount` (Tiền thuế), `TotalAmountAfterTax` (Tổng thanh toán).

2. **Xuất Hóa đơn điện tử mô phỏng (`SimulatedInvoice`):**
   - Cho phép khách hàng chọn **"Yêu cầu xuất hóa đơn VAT"** khi đặt tour (nhập Tên công ty, Mã số thuế, Địa chỉ, Email nhận hóa đơn) hoặc xuất hóa đơn bán lẻ cho cá nhân.
   - Khi Booking đã thanh toán đủ (`Confirmed` / `Completed`), Kế toán (hoặc tự động) bấm **"Phát hành hóa đơn mô phỏng"**:
     - Hệ thống sinh số hóa đơn theo quy tắc (VD: `INV-2026-000123`), Ký hiệu mẫu số mô phỏng (`1C26TAA`), Ngày phát hành, Mã tra cứu giả lập.
     - Trạng thái hóa đơn: `Draft` $\rightarrow$ `Issued` (Đã phát hành) $\rightarrow$ `Cancelled` (Hủy hóa đơn nếu hoàn tour).
     - Cho phép tải xuống bản xem trước hóa đơn (dạng JSON/HTML/PDF mô phỏng có đóng dấu *"HÓA ĐƠN MÔ PHỎNG - KHÔNG CÓ GIÁ TRỊ PHÁP LÝ"*).
