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
   - Khách hoặc Sales chọn số tiền cần thanh toán (Tiền cọc hoặc Thanh toán toàn bộ / Phần còn lại) và phương thức `QR_BANK_TRANSFER`.
   - Hệ thống tạo một bản ghi `PaymentTransaction` với:
     - `TransactionCode`: Mã giao dịch duy nhất (VD: `PAY-20261113-0001`).
     - `TransferContent` (Nội dung chuyển khoản chuẩn hóa): VD `TOUR BK2026111301`.
     - `Amount`: Số tiền chính xác cần thanh toán.
     - `ExpiresAt`: Thời hạn mã QR (VD: 15 phút).
     - `Status`: `Pending`.
   - Hệ thống sinh **chuỗi/ảnh QR mô phỏng** (theo chuẩn VietQR payload hoặc SVG/PNG Base64 kèm thông tin: Tên ngân hàng giả lập, Số tài khoản công ty, Tên chủ tài khoản, Số tiền, Nội dung chuyển khoản).

2. **Bước 2 — Hiển thị & Theo dõi trạng thái (Realtime/Polling):**
   - Giao diện hiển thị mã QR, đồng hồ đếm ngược và nút kiểm tra trạng thái.
   - Frontend polling API kiểm tra trạng thái giao dịch (hoặc nhận tín hiệu qua SignalR/WebSocket).

3. **Bước 3 — Mô phỏng Quét mã & Xác nhận từ Ngân hàng (Simulator):**
   - Do chưa kết nối ngân hàng thật, hệ thống cung cấp **Payment Simulator Panel / API giả lập** (chỉ bật ở môi trường Dev/Staging hoặc quyền Admin/Test):
     - **Kịch bản 1 — Thanh toán thành công (Happy Path):** Giả lập ngân hàng gửi Webhook callback về API với `TransactionCode`, `PaidAmount == Amount`, chữ ký hợp lệ (`MockSignature`).
     - **Kịch bản 2 — Thanh toán thất bại / Hủy:** Giả lập khách hủy giao dịch hoặc lỗi ngân hàng $\rightarrow$ `Status = Failed`.
     - **Kịch bản 3 — Chuyển sai số tiền (Partial / Overpaid):** Giả lập khách chuyển thiếu hoặc thừa tiền $\rightarrow$ Hệ thống đánh dấu `NeedReconciliation` (Cần kế toán đối soát) và cảnh báo Sales/Kế toán thay vì tự động xác nhận sai.
     - **Kịch bản 4 — Hết hạn QR (Timeout):** Quá `ExpiresAt` chưa có callback $\rightarrow$ Giao dịch chuyển `Expired`, yêu cầu tạo mã QR mới.

4. **Bước 4 — Xử lý Idempotency & Cập nhật Booking:**
   - Webhook nhận callback phải kiểm tra **Idempotency** (nếu cùng một `BankTransactionId` gửi lại nhiều lần thì chỉ cộng tiền đúng 1 lần).
   - Cập nhật `PaidAmount` trên Booking, chuyển trạng thái Booking (`DepositPaid` hoặc `Confirmed`), chuyển chỗ từ `Held` sang `Booked`, và tạo tự động **Phiếu thu (Receipt)**.

### 2.2. Đối soát thủ công (Manual Reconciliation)
- Trường hợp khách chuyển khoản ngoài luồng hoặc chuyển sai nội dung/số tiền, `ACCOUNTANT` có chức năng **Xác nhận thanh toán thủ công** (đính kèm ảnh chụp biên lai/mã tham chiếu ngân hàng) để gạch nợ cho Booking.

---

## 3. Nghiệp vụ Hoàn tiền (Refund)

Khi Booking bị hủy hợp lệ hoặc đoàn bị hủy bởi công ty:
1. Hệ thống tính toán tự động:
   - `PenaltyAmount` = `TotalAmount * CancellationFeePercent` (theo chính sách hủy tại ngày gửi yêu cầu).
   - `RefundableAmount = Max(0, PaidAmount - PenaltyAmount)`.
2. Tạo yêu cầu hoàn tiền (`RefundRequest`) với trạng thái `PendingApproval`.
3. `ACCOUNTANT` / `ADMIN` kiểm tra, phê duyệt và thực hiện chi trả (nhập mã tham chiếu chuyển khoản hoàn trả), tạo **Phiếu chi**, chuyển trạng thái Booking sang `Refunded`.

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
