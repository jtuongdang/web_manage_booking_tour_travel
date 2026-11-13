# 05. Nghiệp Vụ Điều Hành Tour, Chăm Sóc Khách Hàng & Báo Cáo KPI

## 1. Nghiệp vụ Điều hành Tour (Tour Operations)

Khi lịch khởi hành sắp đến ngày đi, `OPERATOR` thực hiện các nghiệp vụ sau:

1. **Chốt đoàn (Tour Manifest Lock):**
   - Trước ngày khởi hành $1$–$2$ ngày, Điều hành kiểm tra tổng số khách đã xác nhận (`BookedSlots`).
   - Nếu đạt $\ge$ `MinParticipants`: Chuyển trạng thái lịch khởi hành sang `Confirmed`, khóa bán thêm và xuất **Danh sách đoàn (Passenger Manifest)** gồm: Họ tên, SĐT, CCCD/Passport, Nhóm tuổi, Yêu cầu đặc biệt (ăn chay, bệnh lý, phòng đơn).
   - Nếu không đủ `MinParticipants`: Điều hành liên hệ Sales để đề xuất khách **chuyển ngày khởi hành** hoặc **hủy đoàn hoàn tiền 100%**.
2. **Phân công Hướng dẫn viên & Phương tiện (`DepartureGuides`):**
   - Gán **1 hoặc nhiều `GUIDE`** (`Lead` = HDV chính / `Assistant` = HDV phụ) vào `TourDeparture` thông qua bảng `DepartureGuides` kèm thông tin tài xế/biển số xe/loại xe trên đoàn.
   - HDV nhận được thông báo và xem được lịch trình + danh sách đoàn của mình trên hệ thống.
3. **Danh sách kiểm tra vận hành (Operational Checklist — `DepartureChecklists`):**
   - Checklist chuẩn trước khi khởi hành: Đã xác nhận phòng khách sạn, Đã xác nhận xe/vé máy bay, Đã mua bảo hiểm du lịch cho toàn đoàn, Đã phát thông báo giờ tập trung cho khách.

---

## 2. Vận hành trên Tour & Quyết toán đoàn (In-Tour Execution & Settlement)

1. **Điểm danh hành khách (Check-in):**
   - Tại điểm đón hoặc từng chặng tham quan, HDV mở danh sách đoàn để đánh dấu trạng thái từng khách: `Pending`, `CheckedIn` (Đã có mặt), `Late` (Đến trễ), `NoShow` (Vắng mặt).
   - Trạng thái này cập nhật theo thời gian thực để Điều hành nắm bắt.
2. **Ghi nhận Sự cố & Chi phí phát sinh (`TourIncidents` & `DepartureExtraExpenses`):**
   - HDV hoặc Điều hành ghi nhận các sự cố trên đường đi (trễ chuyến, thời tiết, khách ốm, đổi nhà hàng) kèm mức độ nghiêm trọng (`Low`, `Medium`, `High`, `Critical`).
   - Ghi nhận chi phí phát sinh thực tế (VD: tiền bồi dưỡng, vé phát sinh, thuốc men) kèm ảnh chứng từ/hóa đơn (`AttachmentUrl`). `ACCOUNTANT` kiểm tra và duyệt (`Pending` $\rightarrow$ `Approved` / `Rejected`) rồi tự động ghi sổ quỹ `FinancialLedgers` trước khi tính lợi nhuận đoàn.
3. **Kết thúc tour & Khóa sổ quyết toán (`Completed` $\rightarrow$ `Settled`):**
   - Sau ngày về (`ReturnDate`), Điều hành xác nhận hoàn thành đoàn (`Completed`) $\rightarrow$ các Booking tham gia chuyển sang `Completed`.
   - Sau khi `ACCOUNTANT` đối soát xong công nợ NCC và chi phí phát sinh, Kế toán bấm **Quyết toán & Khóa sổ đoàn (`Settled`)**: khóa toàn bộ thay đổi tài chính trên đoàn; muốn điều chỉnh lại phải có `ADMIN` mở khóa và ghi Audit Log.

---

## 3. Hậu mãi, Đánh giá & Chăm sóc khách hàng (Post-Tour & CRM)

1. **Đánh giá & Nhận xét (Reviews & Ratings):**
   - Chỉ những khách hàng có Booking ở trạng thái `Completed` mới được gửi đánh giá (1–5 sao theo các tiêu chí: Hướng dẫn viên, Lịch trình, Khách sạn/Ăn uống, Phương tiện) + bình luận & hình ảnh.
   - Sales/CSKH có quyền phản hồi đánh giá công khai; Admin có quyền ẩn đánh giá vi phạm quy chuẩn.
2. **Hồ sơ khách hàng & Tích điểm (`Customers` — CRM & Loyalty):**
   - Lưu lịch sử các tour khách đã đi (cả đặt online lẫn đặt qua `SALES`), tổng chi tiêu tích lũy (`TotalSpent`), tổng số đơn (`TotalBookings`), phân hạng thành viên (`Standard`, `Silver`, `Gold`, `VIP`).
   - Tự động gửi email/thông báo cảm ơn sau khi tour kết thúc 1 ngày và nhắc lịch sử dụng ưu đãi cho lần đặt tiếp theo.

---

## 4. Hệ thống Báo cáo & Thống kê (Reporting & Dashboard KPIs)

Phân hệ báo cáo phục vụ trực tiếp Ban Giám Đốc, Kế toán và Điều hành với bộ lọc theo **Khoảng thời gian**, **Tour**, **Kênh bán**, **Nhân viên Sales**:

| Nhóm báo cáo | Chỉ số (KPIs) cốt lõi | Đối tượng xem |
| :--- | :--- | :--- |
| **Doanh thu & Dòng tiền** | Tổng doanh thu ghi nhận, Tiền thực thu (Paid), Công nợ phải thu (Remaining), Tổng tiền đã hoàn (Refund), Tổng thuế VAT mô phỏng. | `ADMIN`, `ACCOUNTANT` |
| **Lợi nhuận gộp theo Đoàn** | `Doanh thu đoàn - Tổng chi phí NCC - Chi phí phát sinh = Lợi nhuận gộp` và `Tỷ suất lợi nhuận (%)`. | `ADMIN`, `ACCOUNTANT`, `OPERATOR` |
| **Hiệu suất lấp đầy (Occupancy)** | Tỷ lệ lấp đầy chỗ (`BookedSlots / TotalCapacity %`), Top Tour bán chạy nhất, Tỷ lệ hủy/hết hạn giữ chỗ. | `ADMIN`, `OPERATOR`, `SALES` |
| **Hiệu suất Kinh doanh (Sales)** | Doanh số theo từng nhân viên Sales, Tỷ lệ chuyển đổi từ Booking giữ chỗ sang thanh toán thành công. | `ADMIN`, `SALES` |
| **Chất lượng dịch vụ** | Điểm đánh giá trung bình theo từng Tour và theo từng Hướng dẫn viên. | `ADMIN`, `OPERATOR` |
