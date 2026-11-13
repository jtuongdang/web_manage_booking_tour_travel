# 01. Tổng Quan Tác Nhân & Ma Trận Phân Quyền (RBAC)

## 1. Các tác nhân sử dụng hệ thống (Actors)

Để vận hành thực tế tại doanh nghiệp lữ hành, hệ thống phân chia rõ 6 nhóm người dùng cốt lõi:

| Mã vai trò | Tên vai trò | Mô tả trách nhiệm chính |
| :--- | :--- | :--- |
| `CUSTOMER` | **Khách hàng (B2C)** | Tìm kiếm tour, xem lịch khởi hành, đặt tour online, nhập thông tin đoàn khách, quét mã QR thanh toán, theo dõi trạng thái booking, gửi yêu cầu hủy/đổi, đánh giá sau tour. |
| `SALES` | **Nhân viên Kinh doanh / CSKH** | Tư vấn, tạo booking hộ khách (qua điện thoại/Zalo/tại quầy), áp dụng mã ưu đãi theo hạn mức, theo dõi công nợ khách, hỗ trợ đổi/hủy tour, xử lý khiếu nại. |
| `OPERATOR` | **Điều hành Tour (Tour Operator)** | Tạo và cập nhật thông tin tour, lịch trình, lịch khởi hành, cấu hình nhà cung cấp (khách sạn, xe, vé), chốt danh sách đoàn, phân công Hướng dẫn viên (HDV), ghi nhận chi phí vận hành. |
| `GUIDE` | **Hướng dẫn viên (Tour Guide)** | Xem lịch tour được phân công, xem danh sách hành khách (tên, SĐT, ghi chú ăn uống/y tế), thực hiện điểm danh (check-in) theo chặng, báo cáo sự cố phát sinh trong tour. |
| `ACCOUNTANT` | **Kế toán** | Theo dõi dòng tiền, đối soát giao dịch QR, xác nhận phiếu thu/chi, xử lý yêu cầu hoàn tiền (Refund), kiểm tra thuế & xuất hóa đơn mô phỏng, xem báo cáo lãi/lỗ theo từng đoàn. |
| `ADMIN` | **Quản trị viên / Ban Giám đốc** | Quản lý tài khoản nhân viên, phân quyền, cấu hình tham số hệ thống (thời gian giữ chỗ, % VAT mặc định, chính sách hủy), xem toàn bộ báo cáo KPI & Audit Log. |

---

## 2. Ma trận phân quyền chi tiết (Permission Matrix)

Ký hiệu: **C** (Tạo), **R** (Xem), **U** (Sửa), **D** (Xóa/Vô hiệu hóa), **A** (Phê duyệt/Xác nhận).

| Chức năng nghiệp vụ | CUSTOMER | SALES | OPERATOR | GUIDE | ACCOUNTANT | ADMIN |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Quản lý tài khoản & Phân quyền** | R/U (Cá nhân) | R/U (Cá nhân) | R/U (Cá nhân) | R/U (Cá nhân) | R/U (Cá nhân) | C/R/U/D |
| **Danh mục Tour & Lịch trình** | R (Published) | R | C/R/U/D | R (Assigned) | R | C/R/U/D |
| **Bảng giá & Chính sách hủy** | R | R | C/R/U | - | R/U | C/R/U/D |
| **Lịch khởi hành & Quota chỗ** | R | R | C/R/U/D | R (Assigned) | R | C/R/U/D |
| **Nhà cung cấp (KS, Xe, Vé)** | - | R | C/R/U/D | R (Assigned) | R | C/R/U/D |
| **Đặt Tour (Booking)** | C/R (Của mình) | C/R/U (Tất cả) | R | - | R | C/R/U |
| **Yêu cầu Hủy / Đổi Tour** | C/R (Của mình) | C/R/A (Theo cấp) | R/A (Đổi lịch) | - | A (Hoàn tiền) | A |
| **Mã khuyến mãi / Voucher** | R (Áp dụng) | R (Áp dụng) | - | - | R | C/R/U/D |
| **Thanh toán QR & Giao dịch** | C/R (Của mình) | C/R | - | - | R/U/A (Đối soát) | R |
| **Hoàn tiền (Refund) & Phiếu chi** | R (Của mình) | C (Đề xuất) | - | - | C/R/A | A |
| **Thuế & Hóa đơn (Mô phỏng)** | R (Tải hóa đơn) | R | - | - | C/R/U | R |
| **Chốt đoàn & Phân công HDV** | - | R | C/R/U/A | R (Của mình) | R | C/R/U/A |
| **Check-in & Sự cố trên Tour** | - | R | R/U | C/R/U (Của mình) | R (Chi phí) | R |
| **Đánh giá (Review) & CSKH** | C/R | R/U (Phản hồi) | R | R | - | R/D (Ẩn spam) |
| **Báo cáo Doanh thu / Lợi nhuận** | - | R (Cá nhân) | R (Vận hành) | - | R (Tài chính) | R (Toàn bộ) |
| **Audit Log hệ thống** | - | - | - | - | R (Tài chính) | R |

---

## 3. Quy định xác thực, bảo mật & Audit Log

1. **Xác thực (Authentication):**
   - Sử dụng **JWT (Access Token ngắn hạn 15–30 phút)** kết hợp **Refresh Token** lưu an toàn (HTTP-only Cookie hoặc bảng Token có hỗ trợ thu hồi/revoke).
   - Mật khẩu băm bằng thuật toán chuẩn hiện đại (ASP.NET Core Identity PasswordHasher / BCrypt / Argon2).
   - Tài khoản khách hàng đăng ký qua Email/SĐT; tài khoản nhân viên chỉ do `ADMIN` tạo và cấp vai trò.
2. **Bảo vệ dữ liệu nhạy cảm:**
   - Thông tin CCCD/Hộ chiếu hành khách chỉ hiển thị đầy đủ cho `SALES`, `OPERATOR`, `GUIDE` (thuộc đoàn được phân công) và chủ booking; không trả về công khai ở các API danh sách.
3. **Nhật ký kiểm toán (Audit Log):**
   - Mọi thao tác làm thay đổi dữ liệu quan trọng đều phải ghi log bất biến (Immutable): Ai làm (`UserId`, `Role`), làm lúc nào (`Timestamp`), thao tác gì (`Action`), dữ liệu trước và sau (`OldValue`, `NewValue`), IP/User-Agent.
   - Các sự kiện bắt buộc ghi Audit Log: Thay đổi giá tour, sửa quota chỗ thủ công, hủy/đổi booking, xác nhận/đối soát thanh toán, duyệt hoàn tiền, xuất hóa đơn, thay đổi phân quyền.
