#!/usr/bin/env python3
"""Autonomous Tech Lead Supervisor for QuanLyTourDuLich (1-hour bounded / early-completion).

Manages DevAgent Orchestrator across 3 AI employees:
- codex-local
- github-copilot-executive-909723
- antigravity-cli

Capabilities:
1. Enforces strict 1-hour wall-clock deadline (or completes early when 10/10 tasks finish).
2. Prevents premature macOS idle sleep via `caffeinate -i` DURING the active build window.
3. Auto-approves contracts & validated PR branches (`complete-local`), merges branches into `main`,
   and runs `python3 scripts/validate.py` after each merge.
4. Auto-recovers failed/blocked tasks by capturing validation errors and reassigning retries
   to healthy fallback agents.
5. On completion OR 1-hour timeout: cleanly cancels remaining tasks/processes, verifies build/test,
   writes `doc/BIEN_BAN_BAN_GIAO_DU_AN.md`, commits & pushes to Git (`origin/main`), releases
   `caffeinate`, quits DevAgent Orchestrator cleanly, and triggers `pmset sleepnow`.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import signal
import sqlite3
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

REPO_PATH = Path("/Users/jtuong/Documents/quanlytuadulich")
ORCH_DB_PATH = Path.home() / "Library/Application Support/com.jtuongdang.devagent-orchestrator/orchestrator.sqlite3"
LOG_FILE = REPO_PATH / "doc" / "SUPERVISOR_LIVE.log"
HANDOFF_FILE = REPO_PATH / "doc" / "BIEN_BAN_BAN_GIAO_DU_AN.md"
STATE_FILE = REPO_PATH / "doc" / ".supervisor_state.json"

SHARED_GUIDANCE = """
QUY TẮC KIẾN TRÚC BẮT BUỘC TỪ TECH LEAD:
1. Tầng nền tảng dùng chung đã sẵn sàng trên `main`:
   - `Data/AppDbContext.cs` & 33 Entities trong `Models/Entities/*.cs`.
   - `Common/Constants/RoleCodes.cs` (`RoleCodes.Customer`, `Sales`, `Operator`, `Guide`, `Accountant`, `Admin`, `PolicyNames.*`).
   - `Common/Modules/IModuleInstaller.cs` (`IModuleInstaller`, `IStartupInitializer`). `Program.cs` TỰ ĐỘNG quét assembly tìm mọi class implement `IModuleInstaller` / `IStartupInitializer`.
   - `Common/Contracts/SharedContracts.cs` (`PagedRequest`, `PagedResult<T>`, `IAuditLogService`, `ISystemSettingService`, `INotificationService`, `IFinancialLedgerWriter`, `IPaymentGateway`, `ITaxInvoiceService`).
   - `Common/Exceptions/AppExceptions.cs` (`BadRequestException`, `UnauthorizedException`, `ForbiddenException`, `NotFoundException`, `ConflictException`) — tự động map sang RFC 7807 `ProblemDetails`.
   - `Common/Security/JwtSettings.cs` (`JwtOptions`, `IJwtSigningKeyProvider`, `ICurrentUserService`).
2. TUYỆT ĐỐI KHÔNG sửa `Program.cs`, `appsettings.json`, `QuanLyTourDuLich.Api.csproj`, `Data/AppDbContext.cs`, `Models/Entities/**` hoặc file ngoài `file_globs` được giao! Hãy tạo class `public sealed class <Module>ModuleInstaller : IModuleInstaller` ngay trong thư mục module của bạn để đăng ký DI/BackgroundService.
3. Không chạy thêm NuGet package bên ngoài nào chưa có trong `.csproj`. Luôn giữ build sạch 0 error và viết unit/integration tests rõ ràng trong `QuanLyTourDuLich.Tests/<Module>/`.
4. Kiểm tra lại tên property chính xác trong `Models/Entities/*.cs` và CHECK constraints trong `database/001_schema_sqlserver.sql` trước khi code!
"""

CONTRACTS_SPEC = [
    {
        "code": "CONTRACT-01",
        "title": "CONTRACT-01: Core Infrastructure, EF Core DbContext & Entities (33 bảng)",
        "body": "Nền tảng ASP.NET Core 10 khớp 100% schema QuanLyTourDuLichDb (33 bảng SQL Server).",
        "tasks": [
            {
                "code": "T1-CORE",
                "title": "T1-CORE: Khởi tạo Entities, AppDbContext (33 bảng SQL Server), ProblemDetails & Test Project",
                "objective": "Hoàn thiện Core DbContext & 33 Entities.",
                "acceptance_criteria": ["33 DbSet & RowVersion & Computed columns"],
                "priority": "urgent",
                "file_globs": [
                    "QuanLyTourDuLich.slnx",
                    "QuanLyTourDuLich.Api/QuanLyTourDuLich.Api.csproj",
                    "QuanLyTourDuLich.Api/Program.cs",
                    "QuanLyTourDuLich.Api/Common/**",
                    "QuanLyTourDuLich.Api/Data/**",
                    "QuanLyTourDuLich.Api/Models/Entities/**",
                    "QuanLyTourDuLich.Tests/**"
                ],
                "dependencies": [],
                "preferred_agent_id": "codex-local",
            }
        ],
    },
    {
        "code": "CONTRACT-02",
        "title": "CONTRACT-02: Identity/RBAC, System Settings, CRM Customers & Promotions",
        "body": "Cung cấp /api/v1/auth/*, /api/v1/users/*, /api/v1/settings/*, /api/v1/bank-accounts/*, /api/v1/customers/*, /api/v1/promotions/*.",
        "tasks": [
            {
                "code": "T2-AUTH",
                "title": "T2-AUTH: Xây dựng Auth JWT/RefreshToken, RBAC 6 vai trò, Users, SystemSettings & CompanyBankAccounts API",
                "objective": (
                    "Đọc `doc/01-tong-quan-va-phan-quyen.md`, `doc/06-thiet-ke-du-lieu-va-api-contracts.md`, `database/001_schema_sqlserver.sql`, `database/002_seed_data.sql`.\n"
                    "Triển khai trong `QuanLyTourDuLich.Api/Modules/Identity/` & `Modules/Settings/` & `QuanLyTourDuLich.Tests/Identity/`:\n"
                    "1. `IdentityModuleInstaller : IModuleInstaller` + `IdentitySeedInitializer : IStartupInitializer`:\n"
                    "   - Seed idempotent 6 tài khoản mẫu với `PasswordHasher<Users>`: `admin` (`ADMIN`), `sales` (`SALES`), `operator` (`OPERATOR`), `guide` (`GUIDE`), `accountant` (`ACCOUNTANT`), `customer` (`CUSTOMER`, kèm liên kết `Customers`) với mật khẩu mẫu dev đọc từ config hoặc mặc định `VietTour@2026!`.\n"
                    "2. `AuthController` (`/api/v1/auth`): `POST /register` (tạo User `Customer`, gán role `CUSTOMER`, liên kết hoặc tạo `Customers` theo Email/Phone), `POST /login` (nhận username/email + password, kiểm tra IsActive & Lockout, cấp Access JWT dùng `IJwtSigningKeyProvider` + `JwtOptions` và lưu SHA256 hash vào `RefreshTokens`), `POST /refresh-token` (rotation + revoke token cũ), `POST /logout`, `GET /me`.\n"
                    "3. `UsersController` (`/api/v1/users`): Admin (`PolicyNames.RequireAdmin`) xem danh sách, `POST /staff` (tạo nhân viên & gán role trong `UserRoles`, ghi AuditLog), bật/khóa tài khoản, gán lại vai trò.\n"
                    "4. `SettingsController` (`/api/v1/settings`), `BankAccountsController` (`/api/v1/bank-accounts`), `AuditLogsController` (`/api/v1/audit-logs`), `NotificationsController` (`/api/v1/notifications`).\n"
                    "5. Viết unit tests trong `QuanLyTourDuLich.Tests/Identity/` kiểm tra JWT generation/validation, password hashing, linking Customer & RBAC roles.\n"
                    + SHARED_GUIDANCE
                ),
                "acceptance_criteria": [
                    "AuthController hỗ trợ register, login, refresh-token, logout, me chuẩn JWT",
                    "IdentitySeedInitializer seed đủ 6 vai trò CUSTOMER/SALES/OPERATOR/GUIDE/ACCOUNTANT/ADMIN",
                    "UsersController, SettingsController, BankAccountsController phân quyền chặt bằng PolicyNames",
                    "dotnet test pass 100% trong QuanLyTourDuLich.Tests/Identity"
                ],
                "priority": "urgent",
                "file_globs": [
                    "QuanLyTourDuLich.Api/Modules/Identity/**",
                    "QuanLyTourDuLich.Api/Modules/Settings/**",
                    "QuanLyTourDuLich.Tests/Identity/**"
                ],
                "dependencies": ["T1-CORE"],
                "preferred_agent_id": "github-copilot-executive-909723",
            },
            {
                "code": "T2-CRM",
                "title": "T2-CRM: Xây dựng CRM Customers (Walk-in/Web) & Promotions Validation/Quota API",
                "objective": (
                    "Đọc `doc/01-tong-quan-va-phan-quyen.md`, `doc/03-nghiep-vu-dat-tour-va-giu-cho.md`, `doc/06-thiet-ke-du-lieu-va-api-contracts.md`, `database/001_schema_sqlserver.sql`.\n"
                    "Quan sát kỹ các cột trong `Models/Entities/Customers.cs`, `Promotions.cs`, `PromotionTours.cs`, `PromotionUsages.cs` trước khi code!\n"
                    "Triển khai trong `QuanLyTourDuLich.Api/Modules/Customers/`, `Modules/Promotions/`, `QuanLyTourDuLich.Tests/Promotions/`:\n"
                    "1. `CrmModuleInstaller : IModuleInstaller` đăng ký services.\n"
                    "2. `CustomersController` (`/api/v1/customers`): `GET` phân trang/tìm kiếm (theo SĐT/Email/Họ tên/LoyaltyTier), `GET /{id}`, `POST` (Sales/Admin tạo hồ sơ khách Walk-in/Zalo/Hotline không cần UserId), `PUT /{id}`, `GET /{id}/bookings` (lịch sử booking).\n"
                    "3. `PromotionsController` (`/api/v1/promotions`) & `PromotionValidationService`: `GET` danh sách mã công khai/quản trị, `POST/PUT` (Admin), `POST /validate` (tính giảm giá `Percent` có trần `MaxDiscountAmount` hoặc `FixedBooking` / `FixedPerPax`, kiểm tra ValidFrom/ValidTo, MinOrderAmount, MinPax, TourId áp dụng qua `PromotionTours` hoặc `ApplyAllTours`, tổng lượt `UsedCount < UsageLimit` và `MaxUsagePerCustomer`).\n"
                    "4. Viết unit tests tính toán voucher `WELCOME10`, `GROUP300`, kiểm tra giới hạn quota & hết hạn.\n"
                    + SHARED_GUIDANCE
                ),
                "acceptance_criteria": [
                    "CustomersController hỗ trợ Walk-in & linked User CRM search/pagination",
                    "PromotionsController & ValidationService tính giảm giá Percent/FixedBooking/FixedPerPax chính xác",
                    "Unit tests trong QuanLyTourDuLich.Tests/Promotions pass 100%"
                ],
                "priority": "high",
                "file_globs": [
                    "QuanLyTourDuLich.Api/Modules/Customers/**",
                    "QuanLyTourDuLich.Api/Modules/Promotions/**",
                    "QuanLyTourDuLich.Tests/Promotions/**"
                ],
                "dependencies": ["T1-CORE"],
                "preferred_agent_id": "antigravity-cli",
            },
        ],
    },
    {
        "code": "CONTRACT-03",
        "title": "CONTRACT-03: Tour Catalog, Cancellation Policies, Departures & Booking Concurrency Engine",
        "body": "Cung cấp /api/v1/tours/*, /api/v1/policies/*, /api/v1/departures/*, /api/v1/bookings/* + Background Workers.",
        "tasks": [
            {
                "code": "T3-TOURS",
                "title": "T3-TOURS: Xây dựng Tours (IsPublic/Private/Custom), Itineraries, CancellationPolicies & Departures CRUD API",
                "objective": (
                    "Đọc `doc/02-nghiep-vu-quan-ly-tour-va-lich-khoi-hanh.md`, `doc/06-thiet-ke-du-lieu-va-api-contracts.md`, `database/001_schema_sqlserver.sql`, `database/002_seed_data.sql`.\n"
                    "Triển khai trong `QuanLyTourDuLich.Api/Modules/Tours/`, `Modules/Departures/`, `QuanLyTourDuLich.Tests/Tours/`:\n"
                    "1. `CatalogModuleInstaller : IModuleInstaller`.\n"
                    "2. `ToursController` (`/api/v1/tours`): `GET` danh sách B2C lọc keyword, category/tags, scopeType, departureFrom, destination, minPrice, maxPrice, durationDays, departureDate (chỉ trả `IsPublic && Status == Published` cho public; nhân viên nội bộ thấy cả Private/Custom/Draft nếu truyền `includeInternal=true`), `GET /{slugOrId}` trả đầy đủ `TourItineraries`, `TourImages`, `CancellationPolicy` + rules, upcoming `TourDepartures` (`IsPublic` nếu public) và reviews summary.\n"
                    "   - `POST /api/v1/tours`, `PUT /api/v1/tours/{id}`, `PUT /api/v1/tours/{id}/publish` (Operator/Admin), `PUT /api/v1/tours/{id}/itineraries`, `PUT /api/v1/tours/{id}/images`.\n"
                    "3. `PoliciesController` (`/api/v1/policies`): `GET / POST / PUT` `CancellationPolicies` kèm `CancellationPolicyRules`.\n"
                    "4. `DeparturesController` (`/api/v1/departures`): `GET /api/v1/tours/{tourId}/departures`, `GET /api/v1/departures`, `GET /api/v1/departures/{id}`, `POST /api/v1/departures`, `PUT /api/v1/departures/{id}`, `PATCH /api/v1/departures/{id}/status`, `GET /api/v1/departures/{id}/manifest` (lấy danh sách hành khách theo booking đang giữ/đã cọc/xác nhận, che CCCD/Passport nếu không đủ quyền).\n"
                    "5. Viết tests trong `QuanLyTourDuLich.Tests/Tours/`.\n"
                    + SHARED_GUIDANCE
                ),
                "acceptance_criteria": [
                    "ToursController lọc tìm kiếm đa tiêu chí, IsPublic filtering, trả chi tiết itineraries/images/departures",
                    "PoliciesController & DeparturesController phân quyền Operator/Admin đúng RoleCodes",
                    "dotnet test pass 100%"
                ],
                "priority": "high",
                "file_globs": [
                    "QuanLyTourDuLich.Api/Modules/Tours/**",
                    "QuanLyTourDuLich.Api/Modules/Departures/**",
                    "QuanLyTourDuLich.Tests/Tours/**"
                ],
                "dependencies": ["T1-CORE"],
                "preferred_agent_id": "codex-local",
            },
            {
                "code": "T3-BOOKINGS",
                "title": "T3-BOOKINGS: Xây dựng Booking Engine (Hold TTL, RowVersion chống Overbooking, Snapshot, Reschedule, AdjustPax & Background Workers)",
                "objective": (
                    "Đọc `doc/03-nghiep-vu-dat-tour-va-giu-cho.md`, `doc/04-nghiep-vu-thanh-toan-qr-va-thue-mo-phong.md`, `doc/06-thiet-ke-du-lieu-va-api-contracts.md`, `database/001_schema_sqlserver.sql`.\n"
                    "Triển khai trong `QuanLyTourDuLich.Api/Modules/Bookings/`, `QuanLyTourDuLich.Api/Workers/`, `QuanLyTourDuLich.Tests/Bookings/`:\n"
                    "1. `BookingModuleInstaller : IModuleInstaller` đăng ký services + `BookingExpirationWorker` & `BookingFinalPaymentMonitorWorker` (`BackgroundService`).\n"
                    "2. `BookingsController` (`/api/v1/bookings`):\n"
                    "   - `POST /api/v1/bookings`: Hỗ trợ cả khách vãng lai/B2C & `SALES` tạo booking hộ (`WalkIn`, `Zalo`, `Hotline`, `Facebook`, `Web`). Tự tạo/liên kết `Customers` theo Email/Phone nếu chưa có. Bắt buộc Transaction kiểm tra `TourDeparture.Status == Open`, chưa quá `CutoffAt`, `AvailableSlots >= adult + child` (`Infant <= MaxInfants ?? adult`). Cập nhật `HeldSlots += seats`, nếu `AvailableSlots == 0` chuyển Departure `Full`. Tính lại giá trên server theo `AdultPrice`, `ChildPrice`, `InfantPrice`, `SingleSupplement`, Promotion, VAT (`VatRatePercent`, `TaxMode` từ `ISystemSettingService`), lưu `PriceSnapshotJson` & `CancellationPolicySnapshotJson`, gán `HoldExpiresAt` (15 phút B2C / 120 phút Sales từ `ISystemSettingService`). Dùng `RowVersion` bắt `DbUpdateConcurrencyException` -> 409 Conflict!\n"
                    "   - `GET /api/v1/bookings/my`, `GET /api/v1/bookings` (Sales/Operator/Accountant/Admin filter), `GET /api/v1/bookings/{codeOrId}` (cho phép tra cứu theo bookingCode + phone/email xác thực hoặc chủ sở hữu/nhân viên).\n"
                    "   - `PUT /api/v1/bookings/{id}/passengers`: Cập nhật manifest hành khách trước CutoffAt.\n"
                    "   - `PATCH /api/v1/bookings/{id}/adjust-pax`: Sales/Operator điều chỉnh số khách, kiểm tra chỗ trống/nhả chỗ atomically bằng `RowVersion`, tính lại giá, ghi AuditLog.\n"
                    "   - `POST /api/v1/bookings/{id}/reschedule`: Đổi sang `TourDeparture` khác, kiểm tra cutoff & số chỗ departure mới, nhả chỗ departure cũ, tạo `BookingReschedules`, xử lý chênh lệch/CreditAmount.\n"
                    "   - `POST /api/v1/bookings/{id}/cancel-request` & `POST /api/v1/bookings/{id}/approve-cancel`: Tính % phạt dựa trên `CancellationPolicySnapshotJson` và số ngày còn lại trước DepartureDate, chuyển `CancellationRequested` -> `Cancelled`, nhả `HeldSlots`/`BookedSlots` và mở lại `Open` nếu còn chỗ.\n"
                    "3. Workers: `BookingExpirationWorker` (mỗi 45s nhả `PendingPayment` có `HoldExpiresAt < UtcNow` -> `Expired`, hoàn `HeldSlots` & `PromotionUsages`); `BookingFinalPaymentMonitorWorker` (đánh dấu `FinalPaymentOverdue` khi `DepositPaid` quá `FinalPaymentDueAt` và gửi notification qua `INotificationService`).\n"
                    "4. Tests trong `QuanLyTourDuLich.Tests/Bookings/` kiểm tra tính toán giá/thuế, overbooking concurrency & state machine.\n"
                    + SHARED_GUIDANCE
                ),
                "acceptance_criteria": [
                    "POST /api/v1/bookings chống overbooking với RowVersion, HoldExpiresAt TTL, snapshot giá & chính sách hủy",
                    "Reschedule, AdjustPax, CancelRequest, ApproveCancel hoạt động đầy đủ và ghi AuditLog",
                    "Background workers nhả chỗ hết hạn & cảnh báo quá hạn FinalPaymentOverdue",
                    "dotnet test pass 100%"
                ],
                "priority": "urgent",
                "file_globs": [
                    "QuanLyTourDuLich.Api/Modules/Bookings/**",
                    "QuanLyTourDuLich.Api/Workers/**",
                    "QuanLyTourDuLich.Tests/Bookings/**"
                ],
                "dependencies": ["T1-CORE", "T2-CRM", "T3-TOURS"],
                "preferred_agent_id": "github-copilot-executive-909723",
            },
        ],
    },
    {
        "code": "CONTRACT-04",
        "title": "CONTRACT-04: VietQR Simulator, Accounting Reconciliation, Refunds, Simulated VAT Invoices & Cashbook Ledger",
        "body": "Cung cấp IPaymentGateway, ITaxInvoiceService, /api/v1/payments/*, /api/v1/refunds/*, /api/v1/invoices/*, /api/v1/ledger/*.",
        "tasks": [
            {
                "code": "T4-PAY",
                "title": "T4-PAY: Xây dựng Payment QR Simulator, Idempotency Webhook & Đối soát sao kê Kế toán",
                "objective": (
                    "Đọc `doc/04-nghiep-vu-thanh-toan-qr-va-thue-mo-phong.md`, `doc/06-thiet-ke-du-lieu-va-api-contracts.md`, `database/001_schema_sqlserver.sql`.\n"
                    "Triển khai trong `QuanLyTourDuLich.Api/Modules/Payments/` & `QuanLyTourDuLich.Tests/Payments/`:\n"
                    "1. `PaymentsModuleInstaller : IModuleInstaller` đăng ký `IPaymentGateway -> VietQrPaymentGateway` và service.\n"
                    "2. `VietQrPaymentGateway : IPaymentGateway`: Sinh chuỗi EMVCo/VietQR payload + SVG/PNG DataUrl QR mô phỏng chứa BankBin, AccountNumber, Amount, TransferContent (`TOUR {BookingCode}`), kiểm tra MockSignature.\n"
                    "3. `PaymentsController` (`/api/v1/payments`):\n"
                    "   - `POST /qr-create`: Nhận BookingId/BookingCode & amount/mode (`Deposit`/`Remaining`/`Full`), lấy tài khoản ngân hàng mặc định từ `CompanyBankAccounts`, tạo `PaymentTransactions` (`Status='Pending'`, `ReconciliationStatus='Unreconciled'`, `ExpiresAt = UtcNow + 15m`), trả QR payload + thông tin chuyển khoản.\n"
                    "   - `GET /{txCode}/status`: Trả trạng thái giao dịch realtime (tự động chuyển `Expired` nếu quá `ExpiresAt` mà còn Pending).\n"
                    "   - `POST /qr-simulate-webhook`: Mô phỏng callback ngân hàng (cho phép kịch bản `Success`, `Partial`/`Overpaid` -> `NeedReconciliation` + `Discrepancy`, `Failed`). BẮT BUỘC check `IdempotencyKey` chống cộng trùng! Khi thành công hoặc xác nhận thu đủ: dùng Transaction cập nhật `Bookings.PaidAmount`, chuyển `PendingPayment` -> `DepositPaid`/`Confirmed`, chuyển `TourDepartures.HeldSlots -> BookedSlots`, tính `FinalPaymentDueAt`, cập nhật `Customers.TotalSpent`/`LoyaltyTier`, và gọi `IFinancialLedgerWriter` ghi dòng tiền `In / CustomerPayment`.\n"
                    "   - `POST /manual-record`: Sales/Accountant ghi nhận thu tiền mặt/chuyển khoản quầy (`CASH`, `BANK_TRANSFER_MANUAL`).\n"
                    "   - `GET /reconciliation-queue` & `POST /{id}/reconcile`: Accountant/Admin đối soát sao kê nhập `(BankCode, BankRef, ActualReceivedAmount, EvidenceUrl, ReconciliationNote)`, kiểm tra không trùng `(BankCode, BankRef)`, cập nhật Booking & ghi `FinancialLedgers`.\n"
                    "4. Tests trong `QuanLyTourDuLich.Tests/Payments/` test VietQR generation, idempotency webhook, duplicate BankRef guard & partial payment.\n"
                    + SHARED_GUIDANCE
                ),
                "acceptance_criteria": [
                    "VietQrPaymentGateway implement IPaymentGateway từ Common/Contracts",
                    "IdempotencyKey & unique (BankCode, BankRef) guard hoạt động an toàn",
                    "Cập nhật Booking PaidAmount + HeldSlots -> BookedSlots + FinancialLedgers atomically",
                    "dotnet test pass 100%"
                ],
                "priority": "urgent",
                "file_globs": [
                    "QuanLyTourDuLich.Api/Modules/Payments/**",
                    "QuanLyTourDuLich.Tests/Payments/**"
                ],
                "dependencies": ["T1-CORE", "T3-TOURS"],
                "preferred_agent_id": "codex-local",
            },
            {
                "code": "T4-FIN",
                "title": "T4-FIN: Xây dựng Refund Workflow, Simulated VAT Invoice & Sổ quỹ FinancialLedgers",
                "objective": (
                    "Đọc `doc/04-nghiep-vu-thanh-toan-qr-va-thue-mo-phong.md`, `doc/06-thiet-ke-du-lieu-va-api-contracts.md`, `database/001_schema_sqlserver.sql`.\n"
                    "Triển khai trong `QuanLyTourDuLich.Api/Modules/Finance/` & `QuanLyTourDuLich.Tests/Finance/`:\n"
                    "1. `FinanceModuleInstaller : IModuleInstaller` đăng ký `ITaxInvoiceService -> SimulatedTaxInvoiceService` & finance services.\n"
                    "2. `RefundsController` (`/api/v1/refunds`): `GET` danh sách yêu cầu hoàn tiền (filter Status), `POST /api/v1/refunds` (đề xuất hoàn tiền từ Booking đã hủy hoặc công ty hủy đoàn: tự tính PenaltyAmount theo chính sách hủy & RefundableAmount = Max(0, PaidAmount - PenaltyAmount)), `POST /{id}/approve` (Accountant/Admin), `POST /{id}/reject`, `POST /{id}/complete` (nhập BankCode/TransferRef, cập nhật `Refunded`, gọi `IFinancialLedgerWriter` ghi `Out / CustomerRefund`, giảm `Customers.TotalSpent`, hủy `SimulatedInvoices` nếu có).\n"
                    "3. `InvoicesController` (`/api/v1/invoices`) & `SimulatedTaxInvoiceService : ITaxInvoiceService`: `POST /api/v1/bookings/{bookingId}/invoice-issue` (sinh `InvoiceNumber` dạng `INV-2026-XXXXXX`, `SerialNumber` từ `SystemSettings`, `LookupCode`, lưu `SimulatedInvoices` với `Status='Issued'`), `GET /api/v1/invoices`, `GET /api/v1/invoices/{id}`, `GET /api/v1/invoices/{id}/preview` (trả HTML hóa đơn GTGT mô phỏng có đóng dấu 'HÓA ĐƠN MÔ PHỎNG - KHÔNG CÓ GIÁ TRỊ PHÁP LÝ').\n"
                    "4. `LedgerController` (`/api/v1/ledger`): `GET` danh sách bút toán sổ quỹ `FinancialLedgers` lọc từ ngày-đến ngày, `Direction` (`In`/`Out`), `Category`, `DepartureId`, kèm tổng hợp Thu/Chi/NetCashflow.\n"
                    "5. Tests trong `QuanLyTourDuLich.Tests/Finance/`.\n"
                    + SHARED_GUIDANCE
                ),
                "acceptance_criteria": [
                    "RefundsController tính & duyệt/chi hoàn tiền chuẩn quy trình tách quyền + ghi sổ quỹ Out",
                    "SimulatedTaxInvoiceService xuất hóa đơn GTGT mô phỏng & render HTML preview",
                    "LedgerController tổng hợp dòng tiền In/Out chính xác",
                    "dotnet test pass 100%"
                ],
                "priority": "high",
                "file_globs": [
                    "QuanLyTourDuLich.Api/Modules/Finance/**",
                    "QuanLyTourDuLich.Tests/Finance/**"
                ],
                "dependencies": ["T1-CORE", "T2-AUTH"],
                "preferred_agent_id": "antigravity-cli",
            },
        ],
    },
    {
        "code": "CONTRACT-05",
        "title": "CONTRACT-05: Tour Operations, Suppliers AP, Settlement, KPI Reports & Full Frontend (B2C + Portal Nội bộ)",
        "body": "Hoàn thiện backend điều hành/báo cáo và nối toàn bộ Frontend B2C + xây mới Portal Nội bộ.",
        "tasks": [
            {
                "code": "T5-OPS",
                "title": "T5-OPS: Xây dựng Suppliers AP, DepartureGuides, Check-in, ExtraExpenses, Settlement Lock & Reports KPI API",
                "objective": (
                    "Đọc `doc/02-nghiep-vu-quan-ly-tour-va-lich-khoi-hanh.md`, `doc/05-nghiep-vu-dieu-hanh-tour-va-hau-mai.md`, `doc/06-thiet-ke-du-lieu-va-api-contracts.md`, `database/001_schema_sqlserver.sql`.\n"
                    "Triển khai trong `QuanLyTourDuLich.Api/Modules/Suppliers/`, `Modules/Operations/`, `Modules/Reports/`, `QuanLyTourDuLich.Tests/Operations/`:\n"
                    "1. `OperationsModuleInstaller : IModuleInstaller`.\n"
                    "2. `SuppliersController` (`/api/v1/suppliers`): CRUD `Suppliers`; `GET/POST/PUT /api/v1/departures/{id}/services` (gán dịch vụ NCC vào đoàn — CHẶN nếu `TourDeparture.Status == 'Settled'` -> 409 Conflict!); `POST /api/v1/departure-services/{id}/payments` (Kế toán thanh toán công nợ NCC `SupplierPayments`, cập nhật `PaidAmount`/`PaymentStatus`, gọi `IFinancialLedgerWriter` ghi `Out / SupplierPayment`).\n"
                    "3. `OperationsController` (`/api/v1/operations`): `PUT /api/v1/operations/departures/{id}/guides` (phân công nhiều HDV `Lead`/`Assistant`), `GET/POST/PATCH /api/v1/operations/departures/{id}/checklists`, `PATCH /api/v1/operations/passengers/{id}/checkin` (HDV điểm danh `Pending`/`CheckedIn`/`Late`/`NoShow`), `GET/POST /api/v1/operations/departures/{id}/incidents`, `GET/POST /api/v1/operations/departures/{id}/extra-expenses`, `POST /api/v1/operations/extra-expenses/{id}/review` (Accountant duyệt/từ chối, khi Approved ghi `FinancialLedgers` `Out / ExtraExpense`), `POST /api/v1/departures/{id}/settle` (Accountant khóa sổ đoàn `Settled` nếu không còn chi phí Pending, ghi AuditLog) & `POST /api/v1/departures/{id}/unlock-settle` (chỉ Admin mở khóa có lý do + AuditLog).\n"
                    "4. `ReviewsController` (`/api/v1/reviews`) & `ReportsController` (`/api/v1/reports`): `POST /api/v1/reviews/bookings/{id}` (chỉ Booking Completed), `POST /api/v1/reviews/{id}/reply`, `GET /api/v1/reports/revenue`, `GET /api/v1/reports/departure-profit` (Doanh thu - ActualCost dịch vụ NCC - ExtraExpenses Approved = Lợi nhuận gộp), `GET /api/v1/reports/occupancy`, `GET /api/v1/reports/sales-performance`.\n"
                    "5. Tests trong `QuanLyTourDuLich.Tests/Operations/` kiểm tra Settlement Lock 409 guard & profit calculation.\n"
                    + SHARED_GUIDANCE
                ),
                "acceptance_criteria": [
                    "Chặn tuyệt đối sửa chi phí/dịch vụ NCC khi đoàn ở trạng thái Settled (409)",
                    "Phân công HDV, check-in, incidents, extra-expenses & Settlement Lock/Unlock đầy đủ AuditLog",
                    "ReportsController trả đúng KPIs doanh thu, lãi gộp đoàn, occupancy",
                    "dotnet test pass 100%"
                ],
                "priority": "high",
                "file_globs": [
                    "QuanLyTourDuLich.Api/Modules/Suppliers/**",
                    "QuanLyTourDuLich.Api/Modules/Operations/**",
                    "QuanLyTourDuLich.Api/Modules/Reports/**",
                    "QuanLyTourDuLich.Tests/Operations/**"
                ],
                "dependencies": ["T1-CORE", "T3-TOURS", "T3-BOOKINGS", "T4-FIN"],
                "preferred_agent_id": "github-copilot-executive-909723",
            },
            {
                "code": "T5-FE-B2C",
                "title": "T5-FE-B2C: Nối Web B2C vào ASP.NET Core 10 API thật, Auth Đăng nhập/Đăng ký, Booking/QR/VAT thật & Reschedule",
                "objective": (
                    "Kiểm tra kỹ các controllers thực tế vừa merge trong `QuanLyTourDuLich.Api/Modules/**` và `doc/07-tham-khao-giao-dien.md`, `frontend/src/`.\n"
                    "Thực hiện trong `frontend/vite.config.js`, `frontend/src/api/**`, `frontend/src/store/**`, `frontend/src/components/**`, `frontend/src/pages/**`:\n"
                    "1. Cấu hình `frontend/vite.config.js` proxy `/api` tới `http://localhost:5141` (`changeOrigin: true`).\n"
                    "2. Tạo `frontend/src/api/client.js` (tự đính JWT Access Token, auto refresh token khi 401 qua `/api/v1/auth/refresh-token`, parse RFC 7807 `ProblemDetails`) và các API modules (`authApi.js`, `toursApi.js`, `bookingsApi.js`, `paymentsApi.js`, `portalApi.js` cho dùng chung).\n"
                    "3. Tạo `frontend/src/store/AuthContext.jsx` quản lý phiên đăng nhập/đăng ký (hiển thị modal hoặc trang `/login` + `/register` từ `Header.jsx`, hiển thị nút vào Cổng Nội Bộ `/portal` nếu user có role Staff!).\n"
                    "4. Cập nhật `Home.jsx`, `Tours.jsx`, `TourDetail.jsx`, `Checkout.jsx`, `Payment.jsx`, `BookingDetail.jsx`, `MyBookings.jsx`, `SearchBar.jsx`, `TourCard.jsx`, `Header.jsx` để gọi API `/api/v1/*` thật từ ASP.NET Core 10 (có fallback thông báo lỗi rõ ràng, giữ nguyên giao diện đẹp Traveloka 1:1).\n"
                    "   - `Checkout.jsx` gọi `POST /api/v1/promotions/validate` & `POST /api/v1/bookings`.\n"
                    "   - `Payment.jsx` gọi `POST /api/v1/payments/qr-create`, polling `GET /api/v1/payments/{txCode}/status`, và nút Simulator gọi `POST /api/v1/payments/qr-simulate-webhook`.\n"
                    "   - `BookingDetail.jsx` hỗ trợ hủy tour thật (`POST /cancel-request`), đổi ngày khởi hành (`POST /reschedule`), xem/tải hóa đơn VAT mô phỏng (`/api/v1/invoices/*`), đánh giá tour (`POST /api/v1/reviews/bookings/{id}`).\n"
                    "5. KHÔNG sửa `frontend/src/App.jsx`, `frontend/src/index.css` hoặc `frontend/src/portal/**` (thuộc task `T5-FE-PORTAL`)! Xuất sẵn `AuthContext` và `api/client.js` để `T5-FE-PORTAL` tái sử dụng.\n"
                    "6. Giữ `oxlint` 0 error và `vite build` pass 100%.\n"
                ),
                "acceptance_criteria": [
                    "vite.config.js proxy /api tới http://localhost:5141",
                    "Web B2C kết nối hoàn chỉnh Auth JWT, Tours, Promotions, Bookings, VietQR Simulator, Hóa đơn VAT",
                    "npm run lint (0 error) và npm run build pass"
                ],
                "priority": "high",
                "file_globs": [
                    "frontend/vite.config.js",
                    "frontend/src/api/**",
                    "frontend/src/store/**",
                    "frontend/src/components/**",
                    "frontend/src/pages/**"
                ],
                "dependencies": ["T2-AUTH", "T2-CRM", "T3-TOURS", "T3-BOOKINGS", "T4-PAY", "T4-FIN"],
                "preferred_agent_id": "codex-local",
            },
            {
                "code": "T5-FE-PORTAL",
                "title": "T5-FE-PORTAL: Xây dựng Portal Nội bộ (/admin, /sales, /operator, /guide, /accountant) & Routing",
                "objective": (
                    "Kiểm tra các API controllers trong `QuanLyTourDuLich.Api/Modules/**`, `frontend/src/api/client.js` và `frontend/src/store/AuthContext.jsx`.\n"
                    "Triển khai trong `frontend/src/portal/**`, `frontend/src/App.jsx`, `frontend/src/index.css`:\n"
                    "1. Xây dựng Portal Layout chuyên nghiệp (`frontend/src/portal/PortalLayout.jsx`) với sidebar điều hướng thông minh theo vai trò JWT (`ADMIN`, `SALES`, `OPERATOR`, `GUIDE`, `ACCOUNTANT`) + Route guard.\n"
                    "2. Trang `/login` (hoặc `/portal/login`) có nút đăng nhập nhanh 1-click tài khoản demo từng vai trò (ghi rõ tài khoản dev).\n"
                    "3. Xây dựng đầy đủ các màn hình Portal:\n"
                    "   - `/admin` (`AdminDashboard.jsx`): KPI tổng quan từ `/api/v1/reports/*`, quản lý nhân sự `/api/v1/users`, cấu hình hệ thống `/api/v1/settings`, ngân hàng `/api/v1/bank-accounts`, khuyến mãi `/api/v1/promotions`, Audit Log.\n"
                    "   - `/sales` (`SalesPortal.jsx`): Bán tour tại quầy / Sales POS (tạo booking hộ Walk-in/Zalo/Hotline, chọn khách CRM hoặc nhập nhanh, giữ chỗ 120 phút, tạo QR thu hộ), tra cứu đơn, xử lý duyệt hủy/đổi lịch.\n"
                    "   - `/operator` (`OperatorPortal.jsx`): Quản lý Tour (`Published`/`Draft`/Custom), lịch khởi hành (`TotalCapacity`/`HeldSlots`/`BookedSlots`), phân công nhiều HDV, gắn dịch vụ Nhà cung cấp (`Suppliers`/`DepartureServices`), checklist đoàn.\n"
                    "   - `/guide` (`GuidePortal.jsx`): Lịch đoàn được phân công, danh sách hành khách + điểm danh check-in realtime (`CheckedIn`/`Late`/`NoShow`), báo cáo sự cố (`TourIncidents`), đề xuất chi phí phát sinh.\n"
                    "   - `/accountant` (`AccountantPortal.jsx`): Hàng đợi đối soát sao kê VietQR (`BankCode`, `BankRef`, khớp thiếu/thừa), duyệt hoàn tiền (`RefundRequests`), phát hành & xem hóa đơn VAT mô phỏng, thanh toán công nợ NCC, duyệt chi phí phát sinh, Khóa sổ đoàn `Settled`, Sổ quỹ thu chi `FinancialLedgers`.\n"
                    "4. Cập nhật `frontend/src/App.jsx` định tuyến đủ các route B2C + `/login`, `/register`, `/portal`, `/admin`, `/sales`, `/operator`, `/guide`, `/accountant`.\n"
                    "5. Đảm bảo `npm run lint` 0 error và `npm run build` pass.\n"
                ),
                "acceptance_criteria": [
                    "Đủ 5 phân hệ Portal theo vai trò: /admin, /sales, /operator, /guide, /accountant",
                    "Routing hoàn chỉnh trong App.jsx giữa B2C và Portal",
                    "npm run lint & npm run build pass"
                ],
                "priority": "high",
                "file_globs": [
                    "frontend/src/portal/**",
                    "frontend/src/App.jsx",
                    "frontend/src/index.css"
                ],
                "dependencies": ["T2-AUTH", "T4-PAY", "T4-FIN", "T5-OPS", "T5-FE-B2C"],
                "preferred_agent_id": "github-copilot-executive-909723",
            },
        ],
    },
]


def log(msg: str):
    timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{timestamp}] {msg}"
    print(line, flush=True)
    LOG_FILE.parent.mkdir(parents=True, exist_ok=True)
    with open(LOG_FILE, "a", encoding="utf-8") as f:
        f.write(line + "\n")


def get_orchestrator_port() -> int:
    try:
        res = subprocess.run(["lsof", "-iTCP", "-sTCP:LISTEN", "-n", "-P"],
                             capture_output=True, text=True, check=False)
        for line in res.stdout.splitlines():
            if "devagent" in line.lower() and "TCP 127.0.0.1:" in line:
                m = re.search(r"127\.0\.0\.1:(\d+)", line)
                if m:
                    return int(m.group(1))
    except Exception as e:
        log(f"Port lookup warning: {e}")
    return 54722


def get_session_token(port: int) -> str:
    req = urllib.request.Request(f"http://127.0.0.1:{port}/api/session",
                                 headers={"Origin": "http://localhost:5173"})
    with urllib.request.urlopen(req, timeout=8) as resp:
        return json.loads(resp.read().decode())["access_token"]


def api_request(port: int, token: str, path: str, method: str = "GET", data: dict | None = None):
    headers = {"Authorization": f"Bearer {token}", "Origin": "http://localhost:5173"}
    body = None
    if data is not None:
        headers["Content-Type"] = "application/json"
        body = json.dumps(data).encode("utf-8")
    req = urllib.request.Request(f"http://127.0.0.1:{port}{path}", data=body, headers=headers, method=method)
    with urllib.request.urlopen(req, timeout=20) as resp:
        raw = resp.read().decode()
        return json.loads(raw) if raw.strip() else {}


def run_validation() -> tuple[bool, str]:
    res = subprocess.run(["python3", "scripts/validate.py"], cwd=REPO_PATH, capture_output=True, text=True)
    out = (res.stdout or "") + "\n" + (res.stderr or "")
    return res.returncode == 0, out


def merge_branch_into_main(branch: str) -> bool:
    try:
        log(f"Merging branch {branch} into main...")
        res = subprocess.run(
            ["git", "merge", "--no-ff", "-m", f"feat(merge): merge {branch} into main", branch],
            cwd=REPO_PATH,
            capture_output=True,
            text=True,
        )
        if res.returncode == 0:
            log(f"Merged {branch} cleanly.")
            ok, out = run_validation()
            if ok:
                log(f"Post-merge validation on main PASSED.")
            else:
                log(f"Post-merge validation warning after merging {branch}:\n{out[-1200:]}")
            return True
        else:
            log(f"Merge conflict on {branch}: {res.stderr or res.stdout}. Aborting merge.")
            subprocess.run(["git", "merge", "--abort"], cwd=REPO_PATH, capture_output=True)
            return False
    except Exception as e:
        log(f"Error merging {branch}: {e}")
        return False


def all_task_specs() -> dict[str, dict]:
    mapping = {}
    for c in CONTRACTS_SPEC:
        for t in c["tasks"]:
            mapping[t["code"]] = t
    return mapping


def ensure_contracts_and_tasks(port: int, token: str, repo_id: str) -> dict[str, str]:
    log("Synchronizing Contracts & Tasks with DevAgent Orchestrator...")
    existing_contracts = api_request(port, token, f"/api/contracts?repository_id={repo_id}")
    contract_map = {c["title"]: c["id"] for c in existing_contracts}

    existing_tasks = api_request(port, token, "/api/tasks")
    code_to_task_id: dict[str, str] = {}
    for t in existing_tasks:
        if t.get("repository_id") == repo_id and t.get("status") != "cancelled":
            for cspec in CONTRACTS_SPEC:
                for tspec in cspec["tasks"]:
                    if t["title"].startswith(tspec["code"] + ":"):
                        code_to_task_id[tspec["code"]] = t["id"]

    for cspec in CONTRACTS_SPEC:
        cid = contract_map.get(cspec["title"])
        if not cid:
            res = api_request(port, token, "/api/contracts", method="POST", data={
                "title": cspec["title"],
                "body": cspec["body"],
                "repository_id": repo_id,
            })
            cid = res["id"]
            contract_map[cspec["title"]] = cid
            api_request(port, token, f"/api/contracts/{cid}/approve", method="POST")
            log(f"Created & approved contract {cspec['code']} -> {cid}")
        else:
            for c in existing_contracts:
                if c["id"] == cid and c.get("state") == "proposed":
                    api_request(port, token, f"/api/contracts/{cid}/approve", method="POST")
                    log(f"Approved existing contract {cspec['code']} -> {cid}")

        for tspec in cspec["tasks"]:
            code = tspec["code"]
            if code not in code_to_task_id:
                dep_ids = [code_to_task_id[dep] for dep in tspec["dependencies"] if dep in code_to_task_id]
                res = api_request(port, token, "/api/tasks", method="POST", data={
                    "title": tspec["title"],
                    "objective": tspec["objective"],
                    "acceptance_criteria": tspec["acceptance_criteria"],
                    "priority": tspec["priority"],
                    "repository_id": repo_id,
                    "base_branch": "main",
                    "required_capabilities": ["implementation"],
                    "file_globs": tspec["file_globs"],
                    "dependencies": dep_ids,
                    "contract_id": cid,
                    "preferred_agent_id": tspec["preferred_agent_id"],
                    "provider_data_consent": True,
                    "max_retries": 2,
                })
                code_to_task_id[code] = res["id"]
                log(f"Queued {code} (preferred={tspec['preferred_agent_id']}, deps={tspec['dependencies']}) -> {res['id']}")

    return code_to_task_id


def retry_failed_task(port: int, token: str, repo_id: str, task: dict, retry_counts: dict[str, int],
                      code_to_task_id: dict[str, str]):
    specs = all_task_specs()
    code = next((c for c in specs if task["title"].startswith(c + ":")), None)
    if not code:
        return
    attempts = retry_counts.get(code, 0)
    if attempts >= 2:
        log(f"Task {code} reached max auto-retries ({attempts}); will not auto-requeue again.")
        return

    retry_counts[code] = attempts + 1
    tspec = specs[code]
    old_agent = task.get("assigned_agent_id") or tspec["preferred_agent_id"]
    fallback_order = ["codex-local", "github-copilot-executive-909723", "antigravity-cli"]
    next_agent = next((a for a in fallback_order if a != old_agent), "codex-local")

    err_summary = task.get("last_error") or task.get("error") or "Validation or execution failure"
    log(f"Auto-healing {code} (attempt #{retry_counts[code]}): previous agent {old_agent} failed ({str(err_summary)[:160]}). Reassigning to {next_agent}...")

    # Archive old failed row cleanly in SQLite so dependencies/title remain unique and unblocked
    try:
        with sqlite3.connect(ORCH_DB_PATH) as con:
            con.execute(
                "UPDATE tasks SET status='cancelled', title=?, updated_at=CURRENT_TIMESTAMP WHERE id=?",
                (f"[RETRY-{retry_counts[code]}] {task['title']}", task["id"]),
            )
            con.execute("DELETE FROM task_writer_leases WHERE task_id=?", (task["id"],))
            con.commit()
    except Exception as e:
        log(f"Warning archiving old failed task row {task['id']}: {e}")

    contracts = api_request(port, token, f"/api/contracts?repository_id={repo_id}")
    cid = task.get("contract_id") or next((c["id"] for c in contracts), None)
    dep_ids = [code_to_task_id[d] for d in tspec["dependencies"] if d in code_to_task_id]

    augmented_objective = (
        tspec["objective"]
        + f"\n\n[LƯU Ý KHẮC PHỤC TỪ TECH LEAD - LẦN CHẠY LẠI #{retry_counts[code]}]:\n"
        + f"Lần chạy trước ({old_agent}) gặp lỗi:\n{str(err_summary)[:1000]}\n"
        + "Hãy kiểm tra kỹ lỗi trên, giữ nguyên phạm vi file_globs và đảm bảo `dotnet build && dotnet test` / `npm run lint && npm run build` pass hoàn toàn!"
    )
    res = api_request(port, token, "/api/tasks", method="POST", data={
        "title": tspec["title"],
        "objective": augmented_objective[:11900],
        "acceptance_criteria": tspec["acceptance_criteria"],
        "priority": "urgent",
        "repository_id": repo_id,
        "base_branch": "main",
        "required_capabilities": ["implementation"],
        "file_globs": tspec["file_globs"],
        "dependencies": dep_ids,
        "contract_id": cid or task.get("contract_id"),
        "preferred_agent_id": next_agent,
        "provider_data_consent": True,
        "max_retries": 2,
    })
    new_tid = res["id"]
    code_to_task_id[code] = new_tid
    # Point any downstream waiting tasks to the new retry task ID
    try:
        with sqlite3.connect(ORCH_DB_PATH) as con:
            rows = con.execute("SELECT id, dependencies FROM tasks WHERE repository_id=? AND status IN ('queued','waiting_for_scope','waiting_for_contract')", (repo_id,)).fetchall()
            for r_id, deps_json in rows:
                deps = json.loads(deps_json or "[]")
                if task["id"] in deps:
                    new_deps = [new_tid if d == task["id"] else d for d in deps]
                    con.execute("UPDATE tasks SET dependencies=?, updated_at=CURRENT_TIMESTAMP WHERE id=?", (json.dumps(new_deps), r_id))
            con.commit()
    except Exception as e:
        log(f"Warning updating downstream dependency IDs: {e}")

    log(f"Re-queued {code} -> {new_tid} on {next_agent}.")


def cancel_active_tasks_and_workers(port: int, token: str, repo_id: str):
    log("Stopping any active/queued tasks on DevAgent Orchestrator...")
    try:
        tasks = api_request(port, token, "/api/tasks")
        for t in tasks:
            if t.get("repository_id") == repo_id and t.get("status") in (
                "running", "assigned", "validating", "queued", "waiting_for_scope", "waiting_for_contract"
            ):
                try:
                    api_request(port, token, f"/api/tasks/{t['id']}/cancel", method="POST")
                    log(f"Cancelled task {t['title'][:35]} ({t['id']})")
                except Exception as exc:
                    log(f"Could not cancel {t['id']} cleanly: {exc}")
    except Exception as e:
        log(f"Warning fetching tasks during shutdown: {e}")

    # Clean up any leftover agent CLI child processes belonging to this repo's worktrees
    worktree_marker = f"/worktrees/{repo_id}/"
    try:
        ps = subprocess.run(["ps", "-eo", "pid,command"], capture_output=True, text=True, check=False)
        for line in ps.stdout.splitlines():
            if worktree_marker in line:
                parts = line.strip().split(None, 1)
                if parts and parts[0].isdigit():
                    pid = int(parts[0])
                    try:
                        os.kill(pid, signal.SIGTERM)
                        log(f"Terminated worker process PID {pid}")
                    except ProcessLookupError:
                        pass
    except Exception as e:
        log(f"Warning cleaning up worker processes: {e}")


def verify_api_smoke() -> str:
    """Runs a quick live API boot & /api/v1/health check against SQL Server."""
    port = 5199
    proc = None
    try:
        env = os.environ.copy()
        env["ASPNETCORE_URLS"] = f"http://127.0.0.1:{port}"
        env["ASPNETCORE_ENVIRONMENT"] = "Development"
        proc = subprocess.Popen(
            ["dotnet", "run", "--no-build", "--project", "QuanLyTourDuLich.Api/QuanLyTourDuLich.Api.csproj"],
            cwd=REPO_PATH,
            env=env,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        )
        deadline = time.time() + 15
        while time.time() < deadline:
            if proc.poll() is not None:
                out = proc.stdout.read() if proc.stdout else ""
                return f"API exited early ({proc.returncode}): {out[-300:]}"
            try:
                with urllib.request.urlopen(f"http://127.0.0.1:{port}/api/v1/health", timeout=2) as resp:
                    body = resp.read().decode()
                    if resp.status == 200:
                        return f"HTTP 200 OK ({body.strip()})"
            except Exception:
                time.sleep(0.6)
        return "API boot timeout (15s)"
    except Exception as e:
        return f"API smoke error: {e}"
    finally:
        if proc and proc.poll() is None:
            proc.terminate()
            try:
                proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                proc.kill()


def write_handoff_report(tasks: list[dict], reason: str, elapsed_min: float, val_ok: bool, val_out: str, api_smoke: str):
    log("Generating Biên bản bàn giao dự án (doc/BIEN_BAN_BAN_GIAO_DU_AN.md)...")
    active_tasks = [t for t in tasks if not t.get("title", "").startswith("[RETRY-")]
    completed = [t for t in active_tasks if t.get("status") == "completed"]

    git_sha = subprocess.run(["git", "rev-parse", "--short", "HEAD"], cwd=REPO_PATH, capture_output=True, text=True).stdout.strip()
    test_summary = next((line.strip() for line in val_out.splitlines() if "Passed!" in line or "Failed!" in line), "Xem output validate.py")

    rows_md = []
    for t in sorted(active_tasks, key=lambda x: x.get("title", "")):
        title = t.get("title", "")
        code = title.split(":")[0] if ":" in title else title[:10]
        name = title.split(":", 1)[1].strip() if ":" in title else title
        agent = t.get("assigned_agent_id") or t.get("preferred_agent_id") or "AI Agent"
        st = t.get("status", "unknown").upper()
        rows_md.append(f"| **{code}** | {name} | `{agent}` | **{st}** |")

    report = f"""# BIÊN BẢN BÀN GIAO DỰ ÁN PHẦN MỀM
## HỆ THỐNG QUẢN LÝ & ĐẶT TOUR DU LỊCH DOANH NGHIỆP (QUANLYTOURDULICH)

- **Thời điểm xuất biên bản:** `{time.strftime('%Y-%m-%d %H:%M:%S')}`
- **Lý do kết thúc phiên điều phối:** {reason}
- **Tổng thời gian vận hành phiên:** `{elapsed_min:.1f} phút`
- **Commit mới nhất trên nhánh `main`:** `{git_sha}`
- **Kết quả kiểm tra `scripts/validate.py`:** `{'PASS (0 lỗi)' if val_ok else 'CẦN KIỂM TRA CHI TIẾT'}`
- **Kết quả `dotnet test`:** `{test_summary}`
- **Kiểm tra khởi chạy Live API (`/api/v1/health`):** `{api_smoke}`

---

### 1. TỔNG QUAN TIẾN ĐỘ HOÀN THÀNH ({len(completed)}/{len(active_tasks)} Tasks)

| Mã Task | Phân hệ Nghiệp vụ | Nhân viên AI thực hiện | Trạng thái |
| :--- | :--- | :--- | :--- |
{chr(10).join(rows_md)}

---

### 2. KIẾN TRÚC & TÀI KHOẢN HỆ THỐNG

- **Backend:** ASP.NET Core 10 Web API (`QuanLyTourDuLich.Api/`), EF Core 10 + SQL Server 2022 (`musing_volhard` -> `QuanLyTourDuLichDb`, 33 bảng), JWT Bearer + RBAC 6 vai trò (`CUSTOMER`, `SALES`, `OPERATOR`, `GUIDE`, `ACCOUNTANT`, `ADMIN`), RFC 7807 `ProblemDetails`, OpenAPI + Scalar.
- **Frontend:** React 19 + Vite 8 + React Router 7 (`frontend/`).
- **Tài khoản mẫu (nếu `IdentitySeedInitializer` đã hoàn thành & chạy API ở Development):**
  - `admin` / `sales` / `operator` / `guide` / `accountant` / `customer` (mật khẩu mặc định seed: `VietTour@2026!`, kiểm tra trong `Modules/Identity/`).

---

### 3. HƯỚNG DẪN KHỞI CHẠY SAU KHI MỞ LẠI MÁY

1. **Khởi động SQL Server Docker:**
   ```bash
   docker start musing_volhard
   ```
2. **Chạy ASP.NET Core 10 Backend:**
   ```bash
   cd /Users/jtuong/Documents/quanlytuadulich
   dotnet run --project QuanLyTourDuLich.Api/QuanLyTourDuLich.Api.csproj
   ```
   - Health check: `http://localhost:5141/api/v1/health`
   - Scalar API Docs: `http://localhost:5141/scalar/v1`
3. **Chạy Frontend React Vite:**
   ```bash
   cd /Users/jtuong/Documents/quanlytuadulich/frontend
   npm run dev
   ```
   - Web B2C: `http://localhost:5173`
   - Cổng Nội bộ: `http://localhost:5173/portal` (hoặc `/admin`, `/sales`, `/operator`, `/guide`, `/accountant`)

---

### 4. LOG KẾT QUẢ `scripts/validate.py` GẦN NHẤT
```text
{val_out[-2500:].strip()}
```

*Biên bản được xuất tự động bởi Tech Lead Supervisor — DevAgent Orchestrator.*
"""
    HANDOFF_FILE.write_text(report, encoding="utf-8")
    log(f"Written handoff report to {HANDOFF_FILE}")


def git_commit_and_push(reason: str):
    log("Committing & pushing final code to Git remote origin/main...")
    subprocess.run(["git", "add", "-A"], cwd=REPO_PATH, check=False)
    st = subprocess.run(["git", "status", "--porcelain"], cwd=REPO_PATH, capture_output=True, text=True).stdout.strip()
    if st:
        subprocess.run(
            ["git", "commit", "-m", f"chore(handoff): finalize run & handover report ({reason})"],
            cwd=REPO_PATH,
            check=False,
        )
    push = subprocess.run(["git", "push", "origin", "main"], cwd=REPO_PATH, capture_output=True, text=True)
    if push.returncode == 0:
        log("Git push origin main SUCCEEDED.")
    else:
        log(f"Git push warning: {push.stderr or push.stdout}")


def shutdown_and_sleep(caffeinate_proc: subprocess.Popen | None, do_sleep: bool):
    if caffeinate_proc and caffeinate_proc.poll() is None:
        log("Releasing caffeinate sleep assertion...")
        caffeinate_proc.terminate()
        try:
            caffeinate_proc.wait(timeout=3)
        except subprocess.TimeoutExpired:
            caffeinate_proc.kill()

    log("Closing DevAgent Orchestrator cleanly...")
    subprocess.run(["osascript", "-e", 'tell application "DevAgent Orchestrator" to quit'], check=False)
    time.sleep(2)

    if do_sleep:
        log("Putting macOS to sleep via `pmset sleepnow`. Goodbye!")
        subprocess.run(["pmset", "sleepnow"], check=False)
    else:
        log("Sleep skipped (--no-sleep).")


def main():
    parser = argparse.ArgumentParser(description="1-hour autonomous Tech Lead supervisor for QuanLyTourDuLich")
    parser.add_argument("--max-minutes", type=float, default=60.0, help="Maximum runtime in minutes before graceful shutdown")
    parser.add_argument("--no-sleep", action="store_true", help="Do not put macOS to sleep after completion")
    args = parser.parse_args()

    start_time = time.time()
    deadline = start_time + args.max_minutes * 60.0

    # Keep macOS awake during active orchestration up to max_minutes
    caff = subprocess.Popen(["caffeinate", "-i", "-s", "-w", str(os.getpid())])
    log(f"=== TECH LEAD SUPERVISOR STARTED (Budget: {args.max_minutes:.0f} min, PID: {os.getpid()}, caffeinate PID: {caff.pid}) ===")

    port = get_orchestrator_port()
    token = get_session_token(port)
    repos = api_request(port, token, "/api/repositories")
    repo = next((r for r in repos if r["path"] == str(REPO_PATH)), None)
    if not repo:
        repo = api_request(port, token, "/api/repositories", method="POST", data={
            "name": "QuanLyTourDuLich",
            "path": str(REPO_PATH),
            "default_branch": "main",
            "validation_profile": "custom",
            "validation_command": ["python3", "scripts/validate.py"],
        })
    repo_id = repo["id"]
    log(f"Orchestrator port={port}, Repository ID={repo_id}")

    code_to_task_id = ensure_contracts_and_tasks(port, token, repo_id)
    retry_counts: dict[str, int] = {}
    target_total = sum(len(c["tasks"]) for c in CONTRACTS_SPEC)
    completion_reason = "Đã hết thời lượng giới hạn 60 phút"

    try:
        while True:
            remaining = deadline - time.time()
            try:
                tasks = api_request(port, token, "/api/tasks")
                tour_tasks = [t for t in tasks if t.get("repository_id") == repo_id and not t.get("title", "").startswith("[RETRY-")]

                # 1. Approve & merge validated tasks
                for t in tour_tasks:
                    if t.get("status") == "awaiting_pr_approval":
                        log(f"Auto-approving validated task: {t['title'][:55]}...")
                        try:
                            api_request(port, token, f"/api/tasks/{t['id']}/complete-local", method="POST")
                            if t.get("branch"):
                                merge_branch_into_main(t["branch"])
                        except Exception as exc:
                            log(f"Error completing/merging task {t['id']}: {exc}")

                # 2. Auto-recover failed or blocked tasks if time allows (> 4 mins left)
                if remaining > 240:
                    for t in tour_tasks:
                        if t.get("status") in ("failed", "blocked"):
                            retry_failed_task(port, token, repo_id, t, retry_counts, code_to_task_id)

                # Refresh snapshot for reporting
                tasks = api_request(port, token, "/api/tasks")
                tour_tasks = [t for t in tasks if t.get("repository_id") == repo_id and not t.get("title", "").startswith("[RETRY-")]
                completed = [t for t in tour_tasks if t.get("status") == "completed"]
                running = [t for t in tour_tasks if t.get("status") in ("running", "assigned", "validating")]
                queued = [t for t in tour_tasks if t.get("status") in ("queued", "waiting_for_scope", "waiting_for_contract")]

                STATE_FILE.write_text(json.dumps({
                    "updatedAt": time.strftime("%Y-%m-%d %H:%M:%S"),
                    "completed": len(completed),
                    "total": target_total,
                    "running": [r["title"][:25] for r in running],
                    "queued": len(queued),
                    "remainingMinutes": round(max(0, remaining) / 60.0, 1),
                }, ensure_ascii=False, indent=2), encoding="utf-8")

                log(f"Tiến độ: {len(completed)}/{target_total} xong | Đang chạy: {len(running)} | Chờ: {len(queued)} | Còn: {max(0, remaining)/60.0:.1f} phút")

                if len(completed) >= target_total:
                    completion_reason = f"Hoàn thành xuất sắc 100% ({len(completed)}/{target_total} tasks) trước hạn 1 tiếng"
                    log(completion_reason)
                    break

                if remaining <= 0:
                    completion_reason = f"Hoàn tất chu kỳ chạy 1 tiếng ({len(completed)}/{target_total} tasks hoàn thành)"
                    log(completion_reason)
                    break

                time.sleep(min(10, max(1, remaining)))

            except Exception as exc:
                log(f"Monitor loop warning: {exc}")
                try:
                    port = get_orchestrator_port()
                    token = get_session_token(port)
                except Exception:
                    pass
                if deadline - time.time() <= 0:
                    break
                time.sleep(8)

        # Finalize clean state before handoff
        cancel_active_tasks_and_workers(port, token, repo_id)
        final_tasks = api_request(port, token, "/api/tasks")
        tour_final = [t for t in final_tasks if t.get("repository_id") == repo_id]

        # Approve any task that finished right during shutdown
        for t in tour_final:
            if t.get("status") == "awaiting_pr_approval":
                try:
                    api_request(port, token, f"/api/tasks/{t['id']}/complete-local", method="POST")
                    if t.get("branch"):
                        merge_branch_into_main(t["branch"])
                except Exception:
                    pass

        tour_final = [t for t in api_request(port, token, "/api/tasks") if t.get("repository_id") == repo_id]
        val_ok, val_out = run_validation()
        api_smoke = verify_api_smoke() if val_ok else "Skipped (validation build failed)"
        elapsed_min = (time.time() - start_time) / 60.0

        write_handoff_report(tour_final, completion_reason, elapsed_min, val_ok, val_out, api_smoke)
        git_commit_and_push(completion_reason)
    finally:
        shutdown_and_sleep(caff, not args.no_sleep)


if __name__ == "__main__":
    main()
