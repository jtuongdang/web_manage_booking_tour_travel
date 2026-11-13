# Tài Liệu Phân Tích & Thiết Kế Hệ Thống Quản Lý Đặt Tour Du Lịch (Tour & Travel Booking Management System)

## 1. Mục tiêu & Tầm nhìn sản phẩm

Hệ thống **Manage Booking Tour Travel** được thiết kế để trở thành nền tảng vận hành cốt lõi cho doanh nghiệp lữ hành (Inbound/Outbound/Nội địa), kết nối xuyên suốt từ **kênh bán hàng (B2C Web/App & Sales nội bộ)** đến **điều hành tour**, **hướng dẫn viên**, **kế toán** và **ban lãnh đạo**.

Mục tiêu khi đưa vào vận hành thực tế (Single-Tenant Enterprise — 1 doanh nghiệp vận hành trực tiếp):
- **Chống bán lố chỗ (Zero Overbooking):** Kiểm soát chặt chẽ số chỗ khả dụng theo thời gian thực với cơ chế giữ chỗ tạm (Hold Slot) có thời hạn và khóa lạc quan `RowVersion` trên SQL Server.
- **Minh bạch tài chính, công nợ & đối soát nội bộ:** Theo dõi rõ ràng đặt cọc, thanh toán phần còn lại, hoàn/hủy, chiết khấu, chi phí nhà cung cấp, chi phí phát sinh, đối soát sao kê ngân hàng và lợi nhuận gộp từng đoàn trước khi khóa sổ quyết toán.
- **Chuẩn hóa quy trình vận hành:** Từ lúc tạo tour -> mở lịch khởi hành -> khách đặt (B2C hoặc Sales đặt hộ) -> thanh toán QR/đối soát -> chốt đoàn -> đi tour -> hậu mãi & quyết toán.
- **Dễ mở rộng & tích hợp:** Xây dựng trên kiến trúc **ASP.NET Core 10 Web API** (Modular Monolith) + **SQL Server 2022**, sẵn sàng cắm cổng thanh toán thật (`IPaymentGateway`) và hóa đơn điện tử thật (`ITaxInvoiceService`) ở giai đoạn sau.

---

## 2. Phạm vi Thực tế vs. Mô phỏng (Simulation Scope)

Theo định hướng triển khai thực tế của doanh nghiệp ở giai đoạn hiện tại:

| Phân hệ | Mức độ triển khai | Ghi chú |
| :--- | :--- | :--- |
| **Quản lý Người dùng & Phân quyền (RBAC)** | **Đầy đủ thực tế** | JWT Auth, Refresh Token, phân quyền chi tiết theo vai trò & audit log. |
| **Quản lý Tour, Lịch trình & Giá** | **Đầy đủ thực tế** | Tour ghép/riêng, giá theo độ tuổi/mùa, phụ thu, chính sách hoàn hủy, nhà cung cấp. |
| **Quản lý Lịch khởi hành & Tồn kho chỗ** | **Đầy đủ thực tế** | Quản lý Quota, Hold/Release chỗ tự động, trạng thái đoàn. |
| **Đặt Tour, Hành khách & Hậu mãi** | **Đầy đủ thực tế** | Booking State Machine, đổi/hủy/chuyển nhượng, khuyến mãi, đánh giá. |
| **Điều hành Tour & Báo cáo KPI** | **Đầy đủ thực tế** | Phân công HDV, danh sách đoàn (Manifest), check-in, doanh thu/lợi nhuận. |
| **Thanh toán quét mã QR** | **Mô phỏng (Simulated)** | Sinh mã QR theo chuẩn VietQR/Dynamic QR + API/Webhook giả lập ngân hàng xác nhận giao dịch, xử lý timeout & đối soát. |
| **Thuế & Hóa đơn điện tử** | **Mô phỏng (Simulated)** | Tự động tính VAT (cấu hình % linh hoạt), xuất dữ liệu/phiếu hóa đơn mô phỏng (B2C/B2B MST) phục vụ kế toán nội bộ. |

---

## 3. Danh mục tài liệu trong thư mục `doc/`

1. [01-tong-quan-va-phan-quyen.md](./01-tong-quan-va-phan-quyen.md) — Tác nhân hệ thống (Actors), Ma trận phân quyền RBAC, Bảo mật & Audit Log.
2. [02-nghiep-vu-quan-ly-tour-va-lich-khoi-hanh.md](./02-nghiep-vu-quan-ly-tour-va-lich-khoi-hanh.md) — Danh mục Tour, Lịch trình, Bảng giá đa tầng, Lịch khởi hành, Kiểm soát chỗ & Nhà cung cấp.
3. [03-nghiep-vu-dat-tour-va-giu-cho.md](./03-nghiep-vu-dat-tour-va-giu-cho.md) — Quy trình Booking, Giữ chỗ (TTL), Danh sách hành khách, Khuyến mãi, Hủy/Đổi tour & State Machine.
4. [04-nghiep-vu-thanh-toan-qr-va-thue-mo-phong.md](./04-nghiep-vu-thanh-toan-qr-va-thue-mo-phong.md) — Quản lý công nợ, Đặt cọc/Hoàn tiền, **Mô phỏng quét mã QR**, **Mô phỏng Thuế & Hóa đơn**.
5. [05-nghiep-vu-dieu-hanh-tour-va-hau-mai.md](./05-nghiep-vu-dieu-hanh-tour-va-hau-mai.md) — Điều hành đoàn, Phân công HDV, Check-in, Chi phí phát sinh, CSKH, Review & Báo cáo KPI.
6. [06-thiet-ke-du-lieu-va-api-contracts.md](./06-thiet-ke-du-lieu-va-api-contracts.md) — Mô hình ERD cốt lõi, Thiết kế nhóm API cho ASP.NET 10 Web API & Yêu cầu phi chức năng.
7. [07-tham-khao-giao-dien.md](./07-tham-khao-giao-dien.md) — Nguồn tham khảo giao diện (Klook, GetYourGuide, Traveloka, Vietravel) & danh sách trang khách hàng đã dựng.
8. [08-thiet-ke-csdl-sqlserver.md](./08-thiet-ke-csdl-sqlserver.md) — Thiết kế CSDL vật lý SQL Server 2022 (33 bảng, khóa ngoại, ràng buộc toàn vẹn, chỉ mục và kịch bản chạy trên Docker).
9. [09-ke-hoach-hop-dong-va-phan-ra-task-devagent.md](./09-ke-hoach-hop-dong-va-phan-ra-task-devagent.md) — Kế hoạch 5 Hợp đồng (`CONTRACT-01`..`CONTRACT-05`) & lộ trình phân task song song trên DevAgent Orchestrator.

---

## 4. Kiến trúc hệ thống đề xuất (ASP.NET 10 Web API + SQL Server 2022)

```
┌──────────────────────────────────────────────────────────────────────┐
│                        CLIENT APPLICATIONS                           │
│   Web Khách hàng (B2C)  │  Portal Nội bộ (Admin/Sales/Điều hành/KT) │
└────────────┬────────────┴───────────────────────┬────────────────────┘
             │ HTTPS / REST JSON                  │
             ▼                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│                   ASP.NET Core 10 Web API Gateway                    │
│   JWT Auth & RBAC  │  Rate Limiting  │  Global Exception  │ OpenAPI  │
├──────────────────────────────────────────────────────────────────────┤
│                        MODULAR BUSINESS CORE                         │
│  [Identity & CRM]  [Tour & Pricing]  [Booking & Hold]  [Operations]  │
│  [Payment QR Sim & Reconciliation] [Tax/Invoice Sim] [Ledger & KPI]  │
├──────────────────────────────────────────────────────────────────────┤
│                     INFRASTRUCTURE & DATA LAYER                      │
│  EF Core 10 (+ Dapper Reporting)  │  SQL Server 2022 (RowVersion)   │
│  Background Worker (Nhả chỗ hết hạn, Cảnh báo quá hạn cọc/khởi hành) │
└──────────────────────────────────────────────────────────────────────┘
```
