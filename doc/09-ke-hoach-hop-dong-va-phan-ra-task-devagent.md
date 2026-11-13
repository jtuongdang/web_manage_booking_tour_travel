# 09. Kế Hoạch Hợp Đồng Giao Việc & Phân Rã Task trên DevAgent Orchestrator

Tài liệu này là kế hoạch điều phối chính thức để **Tech Lead** tạo hợp đồng (`Contracts`) và phân rã task song song không xung đột phạm vi file (`file_globs`) trên **DevAgent Orchestrator** cho toàn dự án **QuanLyTourDuLich**.

---

## 1. Cấu hình Repository trên DevAgent Orchestrator

| Trường cấu hình | Giá trị khuyến nghị |
| :--- | :--- |
| **Local project path** | `/Users/jtuong/Documents/quanlytuadulich` |
| **Default branch** | `main` |
| **Validation profile** | `custom` (`Custom argv command`) |
| **Validator command arguments** | Dòng 1: `python3` <br> Dòng 2: `scripts/validate.py` |

> **Lưu ý:** Khi các task chạy trong worktree Git cô lập, `frontend/node_modules` không được Git theo dõi nên `scripts/validate.py` sẽ tự động build & chạy test `.NET`, còn `npm run lint && npm run build` sẽ chạy đầy đủ tại bản checkout chính khi nghiệm thu frontend.

---

## 2. Danh sách Hợp đồng (Interface Contracts) & Lộ trình Task

### Hợp đồng 1 — `CONTRACT-01`: Core Infrastructure, EF Core DbContext & Entities (33 bảng)
- **Mục tiêu:** Thiết lập nền tảng ASP.NET Core 10 khớp 100% schema `QuanLyTourDuLichDb` ([08-thiet-ke-csdl-sqlserver.md](./08-thiet-ke-csdl-sqlserver.md)), `AppDbContext`, cấu hình `ProblemDetails` RFC 7807, OpenAPI, JWT plumbing, Audit Log service, SystemSettings helper và project kiểm thử `QuanLyTourDuLich.Tests`.
- **Task 1.1 (`T1-CORE`):** `Khởi tạo Entities, AppDbContext (33 bảng SQL Server), ProblemDetails & Test Project`
  - **Priority:** `urgent` | **Dependencies:** không
  - **File globs:** `QuanLyTourDuLich.slnx`, `QuanLyTourDuLich.Api/QuanLyTourDuLich.Api.csproj`, `QuanLyTourDuLich.Api/Program.cs`, `QuanLyTourDuLich.Api/Common/**`, `QuanLyTourDuLich.Api/Data/**`, `QuanLyTourDuLich.Api/Models/Entities/**`, `QuanLyTourDuLich.Tests/**`
  - **Nghiệm thu:** `dotnet build` & `dotnet test` pass; mapping EF Core khớp đủ 33 bảng, computed columns (`AvailableSlots`, `SeatsReserved`, `RemainingAmount`, `EstimatedCost`), `RowVersion`, filtered unique indexes.

---

### Hợp đồng 2 — `CONTRACT-02`: Identity/RBAC, System Settings, CRM Customers & Promotions
- **Mục tiêu:** Cung cấp `/api/v1/auth/*`, `/api/v1/users/*`, `/api/v1/settings/*`, `/api/v1/bank-accounts/*`, `/api/v1/customers/*`, `/api/v1/promotions/*` kèm kiểm thử phân quyền 6 vai trò và tách `Customers`/`Users`.
- **Task 2.1 (`T2-AUTH`, chạy song song với `T2-CRM`):** `Xây dựng Auth JWT/RefreshToken, RBAC 6 vai trò, Users, SystemSettings & CompanyBankAccounts API`
  - **Priority:** `high` | **Dependencies:** `T1-CORE` | **Contract:** `CONTRACT-02`
  - **File globs:** `QuanLyTourDuLich.Api/Modules/Identity/**`, `QuanLyTourDuLich.Api/Modules/Settings/**`, `QuanLyTourDuLich.Tests/Identity/**`
- **Task 2.2 (`T2-CRM`, chạy song song với `T2-AUTH`):** `Xây dựng CRM Customers (Walk-in/Web) & Promotions Validation/Quota API`
  - **Priority:** `high` | **Dependencies:** `T1-CORE` | **Contract:** `CONTRACT-02`
  - **File globs:** `QuanLyTourDuLich.Api/Modules/Customers/**`, `QuanLyTourDuLich.Api/Modules/Promotions/**`, `QuanLyTourDuLich.Tests/Promotions/**`

---

### Hợp đồng 3 — `CONTRACT-03`: Tour Catalog, Cancellation Policies, Departures & Booking Concurrency Engine
- **Mục tiêu:** Cung cấp `/api/v1/tours/*`, `/api/v1/policies/*`, `/api/v1/departures/*`, `/api/v1/bookings/*` + Background Workers nhả chỗ hết hạn & nhắc hạn thanh toán còn lại (`FinalPaymentOverdue`).
- **Task 3.1 (`T3-TOURS`, chạy song song với `T3-BOOKINGS` sau `T1-CORE`):** `Xây dựng Tours (IsPublic/Private/Custom), Itineraries, CancellationPolicies & Departures CRUD API`
  - **Priority:** `high` | **Dependencies:** `T1-CORE`, `T2-AUTH` | **Contract:** `CONTRACT-03`
  - **File globs:** `QuanLyTourDuLich.Api/Modules/Tours/**`, `QuanLyTourDuLich.Api/Modules/Departures/**`, `QuanLyTourDuLich.Tests/Tours/**`
- **Task 3.2 (`T3-BOOKINGS`, chạy song song với `T3-TOURS`):** `Xây dựng Booking Engine (Hold TTL, RowVersion chống Overbooking, Snapshot, Reschedule, AdjustPax, Cancel & Background Workers)`
  - **Priority:** `urgent` | **Dependencies:** `T1-CORE`, `T2-AUTH`, `T2-CRM` | **Contract:** `CONTRACT-03`
  - **File globs:** `QuanLyTourDuLich.Api/Modules/Bookings/**`, `QuanLyTourDuLich.Api/Workers/**`, `QuanLyTourDuLich.Tests/Bookings/**`

---

### Hợp đồng 4 — `CONTRACT-04`: VietQR Simulator, Accounting Reconciliation, Refunds, Simulated VAT Invoices & Cashbook Ledger
- **Mục tiêu:** Cung cấp `IPaymentGateway`, `ITaxInvoiceService`, `/api/v1/payments/*` (VietQR + simulator webhook idempotent + manual reconcile chống trùng `BankRef`), `/api/v1/refunds/*`, `/api/v1/invoices/*`, `/api/v1/ledger/*`.
- **Task 4.1 (`T4-PAY`, chạy song song với `T4-FIN`):** `Xây dựng Payment QR Simulator, Idempotency Webhook & Đối soát sao kê Kế toán`
  - **Priority:** `urgent` | **Dependencies:** `T3-BOOKINGS` | **Contract:** `CONTRACT-04`
  - **File globs:** `QuanLyTourDuLich.Api/Modules/Payments/**`, `QuanLyTourDuLich.Tests/Payments/**`
- **Task 4.2 (`T4-FIN`, chạy song song với `T4-PAY`):** `Xây dựng Refund Workflow, Simulated VAT Invoice & Sổ quỹ FinancialLedgers`
  - **Priority:** `high` | **Dependencies:** `T3-BOOKINGS` | **Contract:** `CONTRACT-04`
  - **File globs:** `QuanLyTourDuLich.Api/Modules/Finance/**`, `QuanLyTourDuLich.Tests/Finance/**`

---

### Hợp đồng 5 — `CONTRACT-05`: Tour Operations, Suppliers AP, Settlement, KPI Reports & Full Frontend (B2C + Portal Nội bộ)
- **Mục tiêu:** Hoàn thiện backend điều hành/báo cáo và nối toàn bộ Frontend B2C + xây mới Portal Nội bộ cho 5 vai trò doanh nghiệp.
- **Task 5.1 (`T5-OPS`, backend):** `Xây dựng Suppliers AP, DepartureGuides, Check-in, ExtraExpenses, Settlement Lock & Reports KPI API`
  - **Priority:** `high` | **Dependencies:** `T3-TOURS`, `T3-BOOKINGS`, `T4-FIN` | **Contract:** `CONTRACT-05`
  - **File globs:** `QuanLyTourDuLich.Api/Modules/Suppliers/**`, `QuanLyTourDuLich.Api/Modules/Operations/**`, `QuanLyTourDuLich.Api/Modules/Reports/**`, `QuanLyTourDuLich.Tests/Operations/**`
- **Task 5.2 (`T5-FE-B2C`, frontend khách hàng — song song với `T5-FE-PORTAL`):** `Nối Web B2C vào ASP.NET Core 10 API thật, Auth Đăng nhập/Đăng ký, Booking/QR/VAT thật & Reschedule`
  - **Priority:** `high` | **Dependencies:** `T2-AUTH`, `T3-TOURS`, `T3-BOOKINGS`, `T4-PAY`, `T4-FIN` | **Contract:** `CONTRACT-05`
  - **File globs:** `frontend/vite.config.js`, `frontend/src/api/**`, `frontend/src/store/**`, `frontend/src/components/**`, `frontend/src/pages/**`
- **Task 5.3 (`T5-FE-PORTAL`, frontend nội bộ — song song với `T5-FE-B2C`):** `Xây dựng Portal Nội bộ (/admin, /sales, /operator, /guide, /accountant) & Routing`
  - **Priority:** `high` | **Dependencies:** `T2-AUTH`, `T4-PAY`, `T4-FIN`, `T5-OPS` | **Contract:** `CONTRACT-05`
  - **File globs:** `frontend/src/portal/**`, `frontend/src/App.jsx`, `frontend/src/index.css`
