# AGENTS.md — Hướng dẫn bắt buộc cho AI Agent trên dự án QuanLyTourDuLich

## 1. Tổng quan & Mục tiêu sản phẩm
- **Sản phẩm:** Hệ thống Quản lý & Đặt Tour Du Lịch **Single-Tenant Enterprise** cho 1 doanh nghiệp lữ hành vận hành trực tiếp (Web B2C khách hàng + Portal Nội bộ cho Sales, Điều hành, HDV, Kế toán, Admin).
- **Stack kỹ thuật:**
  - **Backend:** ASP.NET Core 10 Web API (`QuanLyTourDuLich.Api/`, `.NET 10.0`), EF Core 10 + SQL Server 2022 (`QuanLyTourDuLichDb`), RFC 7807 `ProblemDetails`, OpenAPI.
  - **Database:** SQL Server 2022 (Docker `musing_volhard`, port `1433`, DB `QuanLyTourDuLichDb`, schema chuẩn 33 bảng ở `database/001_schema_sqlserver.sql`). Connection string lưu qua `dotnet user-secrets` (`ConnectionStrings:DefaultConnection`).
  - **Frontend:** React 19 + Vite 8 + React Router 7 (`frontend/`).
- **Tài liệu nghiệp vụ & thiết kế chuẩn (`doc/`):**
  - `doc/01-tong-quan-va-phan-quyen.md` — 6 vai trò RBAC (`CUSTOMER`, `SALES`, `OPERATOR`, `GUIDE`, `ACCOUNTANT`, `ADMIN`), tách `Customers` khỏi `Users`.
  - `doc/02-nghiep-vu-quan-ly-tour-va-lich-khoi-hanh.md` — Tour (`IsPublic`), Giá, Chính sách hủy snapshot, `TourDepartures` & công nợ NCC.
  - `doc/03-nghiep-vu-dat-tour-va-giu-cho.md` — Giữ chỗ TTL, `FinalPaymentOverdue`, State Machine, Reschedule, điều chỉnh khách.
  - `doc/04-nghiep-vu-thanh-toan-qr-va-thue-mo-phong.md` — VietQR mô phỏng (`IPaymentGateway`), đối soát sao kê `(BankCode, BankRef)`, hoàn tiền, VAT & hóa đơn mô phỏng (`ITaxInvoiceService`), `FinancialLedgers`.
  - `doc/05-nghiep-vu-dieu-hanh-tour-va-hau-mai.md` — Đa HDV (`DepartureGuides`), check-in, chi phí phát sinh, khóa sổ đoàn `Settled`, CRM Loyalty.
  - `doc/06-thiet-ke-du-lieu-va-api-contracts.md` — API `/api/v1/*` & yêu cầu phi chức năng.
  - `doc/08-thiet-ke-csdl-sqlserver.md` — Đặc tả 33 bảng SQL Server 2022.
  - `doc/09-ke-hoach-hop-dong-va-phan-ra-task-devagent.md` — Lộ trình hợp đồng & phân rã task.

## 2. Quy tắc kỹ thuật & bảo mật bắt buộc
1. **Không bao giờ ghi mật khẩu, JWT key, connection string thật vào mã nguồn.** Dùng `IConfiguration` / `user-secrets` / biến môi trường.
2. **Chống Overbooking tuyệt đối:** Mọi thao tác giữ chỗ, thanh toán chuyển `HeldSlots -> BookedSlots`, nhả chỗ hết hạn, đổi ngày, hoặc thêm/bớt khách phải nằm trong DB transaction và kiểm tra `RowVersion` + điều kiện `HeldSlots + BookedSlots <= TotalCapacity`.
3. **Không tin dữ liệu giá/thuế từ client:** Backend tự tính lại toàn bộ giá, giảm giá, VAT, tiền cọc và phí phạt hủy dựa trên DB và lưu snapshot (`PriceSnapshotJson`, `CancellationPolicySnapshotJson`).
4. **Thanh toán & Đối soát an toàn:** Webhook QR mô phỏng bắt buộc kiểm tra `IdempotencyKey`; đối soát tay bắt buộc không trùng `(BankCode, BankRef)`; mọi dòng tiền thực thu/chi ghi nhận vào `FinancialLedgers`.
5. **Khóa sổ đoàn (`Settled`):** Khi `TourDepartures.Status == 'Settled'`, chặn sửa dịch vụ NCC, chi phí phát sinh và thanh toán NCC liên quan đoàn đó (trừ luồng mở khóa có audit log bởi `ADMIN`).
6. **Phạm vi file (`file_globs`):** Chỉ sửa/tạo file đúng trong phạm vi task được giao; giữ code sạch, có unit/integration test cho mọi nghiệp vụ lõi.

## 3. Lệnh kiểm tra (Validation)
- Chạy `python3 scripts/validate.py` từ thư mục gốc repository (sẽ build solution `.NET`, chạy `dotnet test` nếu có test project, và chạy `oxlint` + `vite build` nếu `frontend/node_modules` có mặt trong worktree).
