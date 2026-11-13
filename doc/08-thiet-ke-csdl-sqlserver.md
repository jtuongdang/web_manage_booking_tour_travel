# 08. Thiết Kế Cơ Sở Dữ Liệu Vật Lý — SQL Server 2022 (Single-Tenant)

## 1. Thông số kỹ thuật & Quy ước chung

| Hạng mục | Quy chuẩn áp dụng |
| :--- | :--- |
| **Hệ quản trị CSDL** | Microsoft SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest` trong Docker `musing_volhard`, port `1433`) |
| **Tên Database** | `QuanLyTourDuLichDb` (Single-Tenant — 1 doanh nghiệp vận hành trực tiếp) |
| **Schema** | `dbo` |
| **Khóa chính (PK)** | `INT IDENTITY(1,1)` cho bảng danh mục/vận hành; `BIGINT IDENTITY(1,1)` cho bảng giao dịch/log tăng trưởng nhanh (`Bookings`, `PaymentTransactions`, `FinancialLedgers`, `AuditLogs`, `Notifications`...) |
| **Định danh công khai** | Cột mã nghiệp vụ duy nhất (`BookingCode`, `TransactionCode`, `InvoiceNumber`, `Tour.Code`, `CustomerCode`) để không lộ ID tự tăng ra URL/khách hàng |
| **Thời gian** | `DATETIME2(0)` lưu chuẩn **UTC** (`SYSUTCDATETIME()`); các mốc ngày lịch dùng `DATE` |
| **Tiền tệ & Tỷ lệ** | Tiền tệ `DECIMAL(18, 2)` (VNĐ); tỷ lệ `%` dùng `DECIMAL(5, 2)` |
| **Tiếng Việt Unicode** | `NVARCHAR(...)` cho toàn bộ cột văn bản hiển thị |
| **Khóa lạc quan (Concurrency)** | `ROWVERSION` trên `TourDepartures`, `Bookings`, `PaymentTransactions`, `Promotions`, `DepartureServices` |
| **Toàn vẹn tham chiếu** | Tránh lỗi multiple cascade paths của SQL Server: chỉ bật `ON DELETE CASCADE` trên 6 bảng con sở hữu trực tiếp (`UserRoles`, `RefreshTokens`, `CancellationPolicyRules`, `TourItineraries`, `TourImages`, `PromotionTours`); mọi bảng nghiệp vụ còn lại dùng `NO ACTION` |

---

## 2. Tổng quan 33 Bảng theo 7 Phân hệ

### Phân hệ 1 — Hệ thống, Phân quyền & Cấu hình (7 bảng)
| Bảng | Vai trò chính | Ràng buộc & Chỉ mục đáng chú ý |
| :--- | :--- | :--- |
| `Roles` | 6 vai trò chuẩn (`CUSTOMER`, `SALES`, `OPERATOR`, `GUIDE`, `ACCOUNTANT`, `ADMIN`) | `UNIQUE(Code)` |
| `Users` | Tài khoản đăng nhập nhân viên & khách hàng | `UNIQUE(Username)`, `UNIQUE(Email)`, Filtered Unique `Phone WHERE Phone IS NOT NULL` |
| `UserRoles` | Gán N-N vai trò cho tài khoản | `PK(UserId, RoleId)` |
| `RefreshTokens` | Lưu hash Refresh Token hỗ trợ thu hồi (revoke) | `UNIQUE(TokenHash)`, index `(UserId, ExpiresAt)` |
| `SystemSettings` | Tham số động (VAT `8%`, `TaxMode`, `HoldMinutes`, `DepositPercent`, hạn thanh toán còn lại) | `PK(SettingKey)` |
| `CompanyBankAccounts` | Tài khoản ngân hàng nhận VietQR của doanh nghiệp | Filtered Unique `(IsDefault) WHERE IsDefault = 1` đảm bảo tối đa 1 TK mặc định |
| `AuditLogs` | Nhật ký kiểm toán bất biến (ai, vai trò, thao tác, old/new JSON, IP) | Index `(EntityName, EntityId, CreatedAt DESC)` |

### Phân hệ 2 — Khách hàng & CRM (1 bảng)
| Bảng | Vai trò chính | Ràng buộc & Chỉ mục đáng chú ý |
| :--- | :--- | :--- |
| `Customers` | Hồ sơ khách hàng thống nhất cho cả đặt Web B2C lẫn Sales tạo hộ (Walk-in/Zalo/Hotline); lưu `TotalSpent`, `LoyaltyTier` | `UNIQUE(CustomerCode)`, `UNIQUE(Phone)`, Filtered Unique `UserId WHERE UserId IS NOT NULL`, Filtered Unique `Email WHERE Email IS NOT NULL` |

### Phân hệ 3 — Tour, Giá & Chính sách Hủy (5 bảng)
| Bảng | Vai trò chính | Ràng buộc & Chỉ mục đáng chú ý |
| :--- | :--- | :--- |
| `CancellationPolicies` | Bộ chính sách hoàn/hủy/đổi lịch dùng chung hoặc theo tour | `UNIQUE(Code)` |
| `CancellationPolicyRules` | Các bậc phạt theo số ngày trước khởi hành (`MinDaysBeforeDeparture`, `PenaltyPercent`) | `CHECK (PenaltyPercent BETWEEN 0 AND 100)` |
| `Tours` | Danh mục tour (`GroupTour`, `PrivateTour`, `CustomTour`), giá nền, cờ `IsPublic` ẩn tour đoàn riêng khỏi B2C | `UNIQUE(Code)`, `UNIQUE(Slug)`, `ISJSON(...) = 1` cho `HighlightsJson`/`IncludesJson`/`ExcludesJson` |
| `TourItineraries` | Lịch trình chi tiết theo ngày | `UNIQUE(TourId, DayNumber)` |
| `TourImages` | Thư viện ảnh tour và ảnh bìa | Index `(TourId, SortOrder)` |

### Phân hệ 4 — Lịch khởi hành, Điều hành & Nhà cung cấp (6 bảng)
| Bảng | Vai trò chính | Ràng buộc & Chỉ mục đáng chú ý |
| :--- | :--- | :--- |
| `TourDepartures` | Đợt khởi hành, giá ghi đè, kiểm soát chỗ (`TotalCapacity`, `HeldSlots`, `BookedSlots`), thông tin xe/tài xế, khóa sổ `Settled`, `RowVersion` | `AvailableSlots AS (TotalCapacity - HeldSlots - BookedSlots) PERSISTED`; `CHECK (HeldSlots >= 0 AND BookedSlots >= 0 AND HeldSlots + BookedSlots <= TotalCapacity)` |
| `DepartureGuides` | Phân công nhiều HDV (`Lead` / `Assistant`) cho mỗi đoàn | `UNIQUE(DepartureId, GuideUserId)` |
| `DepartureChecklists` | Checklist kiểm tra trước ngày khởi hành | Index `(DepartureId, SortOrder)` |
| `Suppliers` | Danh mục nhà cung cấp (Khách sạn, Nhà xe, Hàng không, Nhà hàng, Vé...) | `UNIQUE(Code)` |
| `DepartureServices` | Dịch vụ đầu vào đặt cho từng đoàn (`EstimatedCost` computed, `ActualCost`, `PaidAmount`, `RowVersion`) | `EstimatedCost AS (CAST(ROUND(Quantity * UnitPrice, 2) AS DECIMAL(18,2))) PERSISTED` |
| `DepartureExtraExpenses` | Chi phí phát sinh thực tế trên tour kèm chứng từ, chờ `ACCOUNTANT` duyệt | Index `(DepartureId, ApprovalStatus)` |

### Phân hệ 5 — Khuyến mãi, Đặt chỗ & Hành khách (6 bảng)
| Bảng | Vai trò chính | Ràng buộc & Chỉ mục đáng chú ý |
| :--- | :--- | :--- |
| `Promotions` | Mã giảm giá (`Percent`, `FixedBooking`, `FixedPerPax`), giới hạn lượt dùng, `RowVersion` | `UNIQUE(Code)`, `CHECK (ValidTo > ValidFrom)` |
| `PromotionTours` | Gán danh sách Tour áp dụng khi `ApplyAllTours = 0` | `PK(PromotionId, TourId)` |
| `PromotionUsages` | Theo dõi giữ/dùng/hoàn trả lượt khuyến mãi theo từng Booking | `UNIQUE(BookingId)`, index `(PromotionId, CustomerId, Status)` |
| `Bookings` | Đơn đặt tour trung tâm: snapshot giá + chính sách hủy, thuế VAT, cọc/đã trả, `HoldExpiresAt`, `FinalPaymentDueAt`, `RowVersion` | `UNIQUE(BookingCode)`, `SeatsReserved` & `RemainingAmount` computed persisted, index phục vụ Background Worker `(Status, HoldExpiresAt)` |
| `BookingPassengers` | Danh sách hành khách (CCCD/Passport, ngày sinh, yêu cầu đặc biệt, trạng thái check-in trên tour) | Index `(BookingId)` |
| `BookingReschedules` | Lịch sử đổi ngày/đổi tour, phí đổi và chênh lệch tiền | Index `(BookingId, CreatedAt DESC)` |

### Phân hệ 6 — Thanh toán QR, Đối soát, Hoàn tiền, Hóa đơn & Sổ quỹ (5 bảng)
| Bảng | Vai trò chính | Ràng buộc & Chỉ mục đáng chú ý |
| :--- | :--- | :--- |
| `PaymentTransactions` | Phiên VietQR mô phỏng, thu tiền mặt/chuyển khoản thủ công, đối soát sai tiền (`ExpectedAmount` vs `ActualReceivedAmount`), `RowVersion` | `UNIQUE(TransactionCode)`; Filtered Unique `IdempotencyKey WHERE IdempotencyKey IS NOT NULL`; Filtered Unique `(BankCode, BankRef) WHERE BankRef IS NOT NULL` |
| `RefundRequests` | Yêu cầu hoàn tiền sau hủy đơn/hủy đoàn, tách bước tạo và bước Kế toán duyệt chi | `UNIQUE(RefundCode)`, index `(Status, CreatedAt)` |
| `SimulatedInvoices` | Hóa đơn VAT mô phỏng (B2C/B2B MST), mẫu số `1C26TVT`, mã tra cứu | `UNIQUE(BookingId)`, `UNIQUE(InvoiceNumber)`, `UNIQUE(LookupCode)` |
| `SupplierPayments` | Phiếu chi thanh toán công nợ nhà cung cấp theo `DepartureService` | `UNIQUE(PaymentCode)`, index `(DepartureServiceId)` |
| `FinancialLedgers` | **Sổ quỹ thu/chi hợp nhất** ghi nhận mọi dòng tiền thực tế (`In`/`Out`) của doanh nghiệp theo đoàn & thời gian | `UNIQUE(EntryCode)`, index `(OccurredAt DESC, Direction)` và `(DepartureId)` |

### Phân hệ 7 — Sự cố, Đánh giá & Thông báo (3 bảng)
| Bảng | Vai trò chính | Ràng buộc & Chỉ mục đáng chú ý |
| :--- | :--- | :--- |
| `TourIncidents` | Sự cố phát sinh trong quá trình vận hành tour | Index `(DepartureId, Status)` |
| `TourReviews` | Đánh giá sau tour của booking đã `Completed`, kèm phản hồi của Sales/CSKH | `UNIQUE(BookingId)`, index `(TourId, IsHidden, CreatedAt DESC)` |
| `Notifications` | Thông báo nội bộ/người dùng (quá hạn cọc, lệch tiền QR, phân công HDV...) | Index `(UserId, IsRead, CreatedAt DESC)` |

---

## 3. Cơ chế toàn vẹn dữ liệu & Chống lỗi sản xuất

1. **Chống Overbooking bằng Optimistic Concurrency + Computed Slot Constraint:**
   - Khi đặt $N$ chỗ (`AdultCount + ChildCount`), giao dịch cập nhật `TourDepartures` kèm `WHERE Id = @Id AND RowVersion = @Rv AND HeldSlots + BookedSlots + @N <= TotalCapacity`.
   - Ràng buộc `CK_TourDepartures_Slots` ở tầng SQL Server là chốt chặn cuối cùng: ngay cả khi có bug ứng dụng, DB vẫn từ chối ghi nếu `HeldSlots + BookedSlots > TotalCapacity`.
2. **Chống cộng tiền trùng từ Webhook & Đối soát tay:**
   - Webhook VietQR mô phỏng bắt buộc truyền `IdempotencyKey` (có Filtered Unique Index).
   - Khi Kế toán đối soát sao kê ngân hàng thực tế, cặp `(BankCode, BankRef)` có Filtered Unique Index nên không thể vô tình gán cùng 1 giao dịch ngân hàng cho 2 Booking khác nhau.
3. **Bất biến báo cáo lịch sử & Khóa sổ đoàn (`Settled`):**
   - `Bookings` lưu đầy đủ `PriceSnapshotJson` và `CancellationPolicySnapshotJson`, tách biệt khỏi thay đổi bảng giá tương lai.
   - Khi `TourDepartures.Status = 'Settled'`, tầng API khóa sửa `DepartureServices`, `DepartureExtraExpenses`, `SupplierPayments` và ghi `AuditLogs` nếu `ADMIN` thực hiện mở khóa.

---

## 4. Tệp thực thi đi kèm

- Script tạo database & toàn bộ 33 bảng: [database/001_schema_sqlserver.sql](../database/001_schema_sqlserver.sql)
- Script nạp dữ liệu mẫu & cấu hình nền (khớp với 6 tour trong `frontend/src/data/tours.js`): [database/002_seed_data.sql](../database/002_seed_data.sql)
