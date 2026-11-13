/* ============================================================================
   QuanLyTourDuLichDb — Initial Master & Sample Seed Data
   Matches baseline business rules (doc/01..08) and frontend/src/data/tours.js
   Note: Staff account PasswordHash values below are PLACEHOLDERS for schema
         initialization only; replace via ASP.NET Core Identity PasswordHasher
         during application bootstrap before enabling login.
   ============================================================================ */

USE [QuanLyTourDuLichDb];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

/* 1. ROLES */
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Code = 'CUSTOMER')
BEGIN
    INSERT INTO dbo.Roles (Code, Name, Description) VALUES
    ('CUSTOMER',   N'Khách hàng (B2C)',        N'Tìm kiếm tour, đặt chỗ, thanh toán QR mô phỏng, xem đơn và đánh giá sau tour'),
    ('SALES',      N'Kinh doanh / CSKH',       N'Tạo booking hộ khách, tư vấn, áp dụng ưu đãi, xử lý yêu cầu hủy/đổi'),
    ('OPERATOR',   N'Điều hành Tour',          N'Quản lý tour, lịch khởi hành, nhà cung cấp, phân công HDV, chốt đoàn'),
    ('GUIDE',      N'Hướng dẫn viên',          N'Xem lịch đoàn được giao, điểm danh khách, báo cáo sự cố & chi phí phát sinh'),
    ('ACCOUNTANT', N'Kế toán',                 N'Đối soát sao kê ngân hàng, duyệt hoàn tiền, chi NCC, phát hành hóa đơn mô phỏng, khóa sổ đoàn'),
    ('ADMIN',      N'Quản trị / Ban Giám đốc', N'Quản lý nhân sự, phân quyền, cấu hình hệ thống, báo cáo KPI & Audit Log');
END
GO

/* 2. SYSTEM SETTINGS */
MERGE dbo.SystemSettings AS target
USING (VALUES
    ('CompanyName',                        N'CÔNG TY TNHH DU LỊCH VIETTOUR', 'String',  N'Tên pháp nhân hiển thị trên hóa đơn mô phỏng'),
    ('CompanyTaxCode',                     N'0312345678',                     'String',  N'Mã số thuế doanh nghiệp'),
    ('CompanyAddress',                     N'190 Pasteur, Q.3, TP.HCM',       'String',  N'Địa chỉ trụ sở doanh nghiệp'),
    ('VatRatePercent',                     N'8.00',                           'Decimal', N'Thuế suất VAT mô phỏng mặc định (%)'),
    ('TaxMode',                            N'Included',                       'String',  N'Included (giá đã gồm VAT) hoặc Excluded'),
    ('DefaultHoldMinutesB2C',              N'15',                             'Int',     N'Thời gian giữ chỗ mặc định cho khách đặt online (phút)'),
    ('DefaultHoldMinutesSales',            N'120',                            'Int',     N'Thời gian giữ chỗ mặc định cho Sales đặt hộ (phút)'),
    ('DefaultDepositPercent',              N'30.00',                          'Decimal', N'Tỷ lệ đặt cọc tối thiểu (%)'),
    ('FinalPaymentDueDaysBeforeDeparture', N'5',                              'Int',     N'Số ngày trước khởi hành phải thanh toán phần còn lại'),
    ('InvoiceSerialNumber',                N'1C26TVT',                        'String',  N'Ký hiệu mẫu hóa đơn VAT mô phỏng')
) AS src (SettingKey, SettingValue, ValueType, Description)
ON target.SettingKey = src.SettingKey
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingValue, ValueType, Description)
    VALUES (src.SettingKey, src.SettingValue, src.ValueType, src.Description);
GO

/* 3. COMPANY BANK ACCOUNT (VIETQR SIMULATION) */
IF NOT EXISTS (SELECT 1 FROM dbo.CompanyBankAccounts WHERE BankCode = 'VCB' AND AccountNumber = '1023456789')
BEGIN
    INSERT INTO dbo.CompanyBankAccounts (BankCode, BankBin, BankName, AccountNumber, AccountHolder, BranchName, IsDefault, IsActive)
    VALUES ('VCB', '970436', N'Vietcombank (Mô phỏng)', '1023456789', N'CONG TY TNHH VIETTOUR', N'CN TP.HCM', 1, 1);
END
GO

/* 4. CANCELLATION POLICIES & RULES */
IF NOT EXISTS (SELECT 1 FROM dbo.CancellationPolicies WHERE Code = 'POL-DOMESTIC-15')
BEGIN
    INSERT INTO dbo.CancellationPolicies (Code, Name, Description, CompanyCancelRefundPercent, AllowReschedule, RescheduleCutoffDays, RescheduleFeePercent, IsDefault)
    VALUES
    ('POL-DOMESTIC-15', N'Chính sách Tour Nội địa Tiêu chuẩn',   N'Miễn giảm mức thấp khi báo trước từ 15 ngày',            100.00, 1, 7,  0.00, 1),
    ('POL-FLEX-7',      N'Chính sách Tour Ngắn ngày / Du thuyền', N'Linh hoạt cho tour ngắn ngày 2N1Đ–3N2Đ',                100.00, 1, 5,  0.00, 0),
    ('POL-INTL-21',     N'Chính sách Tour Quốc tế (Visa)',        N'Áp dụng cho tour có thủ tục visa/vé máy bay quốc tế',   100.00, 1, 14, 5.00, 0);

    DECLARE @PolDom INT = (SELECT Id FROM dbo.CancellationPolicies WHERE Code = 'POL-DOMESTIC-15');
    DECLARE @PolFlex INT = (SELECT Id FROM dbo.CancellationPolicies WHERE Code = 'POL-FLEX-7');
    DECLARE @PolIntl INT = (SELECT Id FROM dbo.CancellationPolicies WHERE Code = 'POL-INTL-21');

    INSERT INTO dbo.CancellationPolicyRules (PolicyId, MinDaysBeforeDeparture, MaxDaysBeforeDeparture, PenaltyPercent, Label, SortOrder) VALUES
    (@PolDom, 15, NULL, 10.00,  N'Hủy trước >= 15 ngày: Phí 10%',  1),
    (@PolDom, 7,  14,   30.00,  N'Hủy từ 7 - 14 ngày: Phí 30%',    2),
    (@PolDom, 3,  6,    50.00,  N'Hủy từ 3 - 6 ngày: Phí 50%',     3),
    (@PolDom, 0,  2,    100.00, N'Hủy dưới 3 ngày / No-show: 100%',4),

    (@PolFlex, 7,  NULL, 10.00,  N'Hủy trước >= 7 ngày: Phí 10%',   1),
    (@PolFlex, 3,  6,    40.00,  N'Hủy từ 3 - 6 ngày: Phí 40%',     2),
    (@PolFlex, 0,  2,    100.00, N'Hủy dưới 3 ngày / No-show: 100%',3),

    (@PolIntl, 30, NULL, 15.00,  N'Hủy trước >= 30 ngày: Phí 15%',  1),
    (@PolIntl, 21, 29,   35.00,  N'Hủy từ 21 - 29 ngày: Phí 35%',   2),
    (@PolIntl, 10, 20,   70.00,  N'Hủy từ 10 - 20 ngày: Phí 70%',   3),
    (@PolIntl, 0,  9,    100.00, N'Hủy dưới 10 ngày / Đã nộp visa: 100%', 4);
END
GO

/* 5. SAMPLE PROMOTIONS (MATCHING frontend/src/data/tours.js) */
IF NOT EXISTS (SELECT 1 FROM dbo.Promotions WHERE Code = 'WELCOME10')
BEGIN
    INSERT INTO dbo.Promotions (Code, Name, Description, DiscountType, DiscountValue, MaxDiscountAmount, MinOrderAmount, MinPax, ApplyAllTours, UsageLimit, MaxUsagePerCustomer, ValidFrom, ValidTo)
    VALUES
    ('WELCOME10', N'Ưu đãi khách hàng mới 10%', N'Giảm 10% tối đa 500.000đ cho đơn đầu tiên', 'Percent',      10.00,     500000.00, 0, 1, 1, 1000, 1, '2026-01-01T00:00:00', '2027-12-31T23:59:59'),
    ('GROUP300',  N'Ưu đãi nhóm từ 4 khách',    N'Giảm ngay 300.000đ cho nhóm >= 4 khách',    'FixedBooking', 300000.00, NULL,      0, 4, 1, 500,  5, '2026-01-01T00:00:00', '2027-12-31T23:59:59');
END
GO

/* 6. SAMPLE TOURS, ITINERARIES, IMAGES & DEPARTURES */
IF NOT EXISTS (SELECT 1 FROM dbo.Tours WHERE Code = 'PQ-3N2D')
BEGIN
    DECLARE @PolDomId INT  = (SELECT Id FROM dbo.CancellationPolicies WHERE Code = 'POL-DOMESTIC-15');
    DECLARE @PolFlexId INT = (SELECT Id FROM dbo.CancellationPolicies WHERE Code = 'POL-FLEX-7');
    DECLARE @PolIntlId INT = (SELECT Id FROM dbo.CancellationPolicies WHERE Code = 'POL-INTL-21');

    INSERT INTO dbo.Tours (Code, Slug, Title, TourType, ScopeType, Tags, DepartureFrom, Destination, DurationDays, DurationNights, Transport, OriginalPrice, BaseAdultPrice, ChildPricePercent, InfantPrice, SingleSupplement, CancellationPolicyId, HighlightsJson, IncludesJson, ExcludesJson, Badges, IsPublic, Status, AverageRating, ReviewCount, BookedCountTotal)
    VALUES
    ('PQ-3N2D', 'phu-quoc-3n2d-cap-treo-hon-thom', N'Phú Quốc 3N2Đ: Cáp treo Hòn Thơm - Grand World - Lặn ngắm san hô', 'GroupTour', 'Domestic', N'domestic,beach', N'TP. Hồ Chí Minh', N'Phú Quốc', 3, 2, N'Máy bay', 5990000, 4990000, 75, 500000, 1200000, @PolDomId,
     N'["Đi cáp treo vượt biển dài nhất thế giới đến Hòn Thơm","Khám phá Grand World - thành phố không ngủ","Lặn ngắm san hô tại quần đảo An Thới","Khách sạn 4 sao gần biển, ăn sáng buffet"]',
     N'["Vé máy bay khứ hồi + 7kg hành lý","Khách sạn 4 sao (2 khách/phòng)","Các bữa ăn theo chương trình","Vé tham quan, cáp treo","HDV suốt tuyến","Bảo hiểm du lịch 120 triệu/vụ"]',
     N'["Chi phí cá nhân, đồ uống ngoài","Phụ thu phòng đơn","Tiền tip HDV & tài xế"]',
     N'Bán chạy,Xác nhận ngay', 1, 'Published', 4.80, 1243, 5200),

    ('DN-4N3D', 'da-nang-hoi-an-ba-na-4n3d', N'Đà Nẵng - Hội An - Bà Nà Hills - Cầu Vàng 4N3Đ', 'GroupTour', 'Domestic', N'domestic,beach,culture', N'Hà Nội', N'Đà Nẵng - Hội An', 4, 3, N'Máy bay', 7290000, 6190000, 75, 600000, 1800000, @PolDomId,
     N'["Check-in Cầu Vàng trên đỉnh Bà Nà","Dạo phố cổ Hội An về đêm, thả hoa đăng","Tắm biển Mỹ Khê","Bán đảo Sơn Trà - chùa Linh Ứng"]',
     N'["Vé máy bay khứ hồi","Khách sạn 4 sao","Vé cáp treo Bà Nà + buffet trưa","Xe đời mới đưa đón","HDV, bảo hiểm"]',
     N'["VAT xuất hóa đơn công ty (nếu có yêu cầu riêng)","Chi phí cá nhân"]',
     N'Ưu đãi -15%', 1, 'Published', 4.70, 892, 3100),

    ('HL-2N1D', 'ha-long-du-thuyen-5-sao-2n1d', N'Hạ Long 2N1Đ: Ngủ đêm du thuyền 5 sao - Hang Sửng Sốt - Chèo Kayak', 'GroupTour', 'Domestic', N'domestic,beach', N'Hà Nội', N'Hạ Long', 2, 1, N'Xe Limousine', 4500000, 3890000, 70, 0, 1500000, @PolFlexId,
     N'["Ngủ đêm trên du thuyền 5 sao giữa vịnh","Chèo kayak hang Luồn","Lớp học Thái Cực Quyền buổi sáng","Tiệc BBQ hải sản hoàng hôn"]',
     N'["Xe Limousine Hà Nội - Hạ Long khứ hồi","Cabin du thuyền 5 sao","Toàn bộ bữa ăn trên tàu","Vé thắng cảnh, kayak"]',
     N'["Đồ uống trên tàu","Spa, massage"]',
     N'Cao cấp', 1, 'Published', 4.90, 2107, 8700),

    ('SP-3N2D', 'sapa-fansipan-3n2d', N'Sapa 3N2Đ: Chinh phục Fansipan - Bản Cát Cát - Đèo Ô Quy Hồ', 'GroupTour', 'Domestic', N'domestic,mountain,culture', N'Hà Nội', N'Sapa', 3, 2, N'Xe giường nằm', 3290000, 2790000, 70, 0, 800000, @PolFlexId,
     N'["Cáp treo lên nóc nhà Đông Dương Fansipan","Trekking bản Cát Cát của người H Mông","Săn mây đèo Ô Quy Hồ"]',
     N'["Xe giường nằm cabin khứ hồi","Khách sạn 3 sao trung tâm","Bữa ăn theo chương trình","HDV, bảo hiểm"]',
     N'["Vé cáp treo Fansipan (tự chọn)","Chi phí cá nhân"]',
     N'Giá tốt', 1, 'Published', 4.60, 534, 1900),

    ('JP-6N5D', 'nhat-ban-cung-duong-vang-6n5d', N'Nhật Bản 6N5Đ: Tokyo - Núi Phú Sĩ - Kyoto - Osaka (Cung đường vàng)', 'GroupTour', 'International', N'international,culture,mountain', N'TP. Hồ Chí Minh', N'Nhật Bản', 6, 5, N'Máy bay', 32990000, 28990000, 85, 6000000, 6500000, @PolIntlId,
     N'["Ngắm núi Phú Sĩ từ hồ Kawaguchi","Chùa Vàng Kinkakuji & rừng trúc Arashiyama","Lâu đài Osaka, phố Dotonbori","Tắm Onsen truyền thống"]',
     N'["Vé máy bay quốc tế","Visa Nhật Bản","Khách sạn 3-4 sao","Tàu cao tốc Shinkansen 1 chặng","HDV tiếng Việt"]',
     N'["Hộ chiếu","Tiền tip (khoảng 7 USD/ngày)"]',
     N'Bán chạy,Bao visa', 1, 'Published', 4.90, 678, 2400),

    ('KR-5N4D', 'han-quoc-seoul-nami-everland-5n4d', N'Hàn Quốc 5N4Đ: Seoul - Đảo Nami - Everland - Mặc Hanbok', 'GroupTour', 'International', N'international,culture', N'Hà Nội', N'Hàn Quốc', 5, 4, N'Máy bay', 17990000, 15990000, 80, 3000000, 4000000, @PolIntlId,
     N'["Đảo Nami lãng mạn","Công viên Everland","Mặc Hanbok dạo cung Gyeongbokgung","Tháp Namsan"]',
     N'["Vé máy bay","Visa Hàn Quốc","Khách sạn 3 sao","Vé Everland"]',
     N'["Hộ chiếu","Tiền tip"]',
     N'Ưu đãi -10%', 1, 'Published', 4.70, 455, 1600);

    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
    DECLARE @T1 INT = (SELECT Id FROM dbo.Tours WHERE Code = 'PQ-3N2D');
    DECLARE @T2 INT = (SELECT Id FROM dbo.Tours WHERE Code = 'DN-4N3D');
    DECLARE @T3 INT = (SELECT Id FROM dbo.Tours WHERE Code = 'HL-2N1D');
    DECLARE @T4 INT = (SELECT Id FROM dbo.Tours WHERE Code = 'SP-3N2D');
    DECLARE @T5 INT = (SELECT Id FROM dbo.Tours WHERE Code = 'JP-6N5D');
    DECLARE @T6 INT = (SELECT Id FROM dbo.Tours WHERE Code = 'KR-5N4D');

    INSERT INTO dbo.TourImages (TourId, ImageUrl, IsCover, SortOrder) VALUES
    (@T1, '/images/phu-quoc.jpg', 1, 1), (@T1, '/images/coral-diving.jpg', 0, 2), (@T1, '/images/luxury-resort.jpg', 0, 3),
    (@T2, '/images/da-nang.jpg',  1, 1), (@T2, '/images/hoi-an.jpg',       0, 2), (@T2, '/images/luxury-resort.jpg', 0, 3),
    (@T3, '/images/ha-long.jpg',  1, 1), (@T3, '/images/luxury-resort.jpg',0, 2), (@T3, '/images/coral-diving.jpg',  0, 3),
    (@T4, '/images/sapa.jpg',     1, 1), (@T4, '/images/da-nang.jpg',      0, 2), (@T4, '/images/hoi-an.jpg',        0, 3),
    (@T5, '/images/nhat-ban.jpg', 1, 1), (@T5, '/images/han-quoc.jpg',     0, 2), (@T5, '/images/luxury-resort.jpg', 0, 3),
    (@T6, '/images/han-quoc.jpg', 1, 1), (@T6, '/images/nhat-ban.jpg',     0, 2), (@T6, '/images/luxury-resort.jpg', 0, 3);

    INSERT INTO dbo.TourItineraries (TourId, DayNumber, Title, Meals, Description) VALUES
    (@T1, 1, N'TP.HCM - Phú Quốc - Bắc Đảo',               N'Trưa, Tối',       N'Đón khách tại sân bay Phú Quốc, tham quan Grand World buổi tối.'),
    (@T1, 2, N'Nam Đảo - Cáp treo Hòn Thơm - Lặn san hô', N'Sáng, Trưa, Tối', N'Đi tàu tham quan 3 đảo An Thới, trải nghiệm cáp treo Hòn Thơm.'),
    (@T1, 3, N'Chợ Dương Đông - Mua sắm - TP.HCM',         N'Sáng',            N'Tham quan cơ sở nước mắm, vườn tiêu, ra sân bay về TP.HCM.');

    INSERT INTO dbo.TourDepartures (TourId, DepartureCode, DepartureDate, ReturnDate, CutoffAt, AdultPrice, TotalCapacity, BookedSlots, MinParticipants, Status)
    VALUES
    (@T1, 'PQ-3N2D-D1', DATEADD(DAY, 5,  @Today), DATEADD(DAY, 7,  @Today), DATEADD(DAY, 4,  @Today), 4990000, 30, 30, 10, 'Full'),
    (@T1, 'PQ-3N2D-D2', DATEADD(DAY, 9,  @Today), DATEADD(DAY, 11, @Today), DATEADD(DAY, 8,  @Today), 4990000, 30, 22, 10, 'Open'),
    (@T1, 'PQ-3N2D-D3', DATEADD(DAY, 16, @Today), DATEADD(DAY, 18, @Today), DATEADD(DAY, 15, @Today), 5290000, 30, 12, 10, 'Open'),
    (@T2, 'DN-4N3D-D1', DATEADD(DAY, 7,  @Today), DATEADD(DAY, 10, @Today), DATEADD(DAY, 6,  @Today), 6190000, 25, 18, 10, 'Open'),
    (@T2, 'DN-4N3D-D2', DATEADD(DAY, 14, @Today), DATEADD(DAY, 17, @Today), DATEADD(DAY, 13, @Today), 6190000, 25, 9,  10, 'Open'),
    (@T3, 'HL-2N1D-D1', DATEADD(DAY, 3,  @Today), DATEADD(DAY, 4,  @Today), DATEADD(DAY, 2,  @Today), 3890000, 20, 14, 8,  'Open'),
    (@T3, 'HL-2N1D-D2', DATEADD(DAY, 10, @Today), DATEADD(DAY, 11, @Today), DATEADD(DAY, 9,  @Today), 3890000, 20, 5,  8,  'Open'),
    (@T4, 'SP-3N2D-D1', DATEADD(DAY, 4,  @Today), DATEADD(DAY, 6,  @Today), DATEADD(DAY, 3,  @Today), 2790000, 40, 31, 15, 'Open'),
    (@T4, 'SP-3N2D-D2', DATEADD(DAY, 11, @Today), DATEADD(DAY, 13, @Today), DATEADD(DAY, 10, @Today), 2790000, 40, 12, 15, 'Open'),
    (@T5, 'JP-6N5D-D1', DATEADD(DAY, 25, @Today), DATEADD(DAY, 30, @Today), DATEADD(DAY, 20, @Today), 28990000,25, 20, 12, 'Open'),
    (@T5, 'JP-6N5D-D2', DATEADD(DAY, 40, @Today), DATEADD(DAY, 45, @Today), DATEADD(DAY, 35, @Today), 28990000,25, 8,  12, 'Open'),
    (@T6, 'KR-5N4D-D1', DATEADD(DAY, 20, @Today), DATEADD(DAY, 24, @Today), DATEADD(DAY, 15, @Today), 15990000,30, 26, 12, 'Open'),
    (@T6, 'KR-5N4D-D2', DATEADD(DAY, 34, @Today), DATEADD(DAY, 38, @Today), DATEADD(DAY, 29, @Today), 15990000,30, 10, 12, 'Open');
END
GO

/* 7. SAMPLE SUPPLIERS */
IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE Code = 'SUP-HTL-PQ01')
BEGIN
    INSERT INTO dbo.Suppliers (Code, Name, SupplierType, TaxCode, ContactName, Phone, Email, BankName, BankAccountNumber, BankAccountHolder, InternalRating)
    VALUES
    ('SUP-HTL-PQ01', N'Seashells Phú Quốc Hotel & Spa',   'Hotel',     '1702001234', N'Lê Hoàng Nam',   '02973999888', 'sales@seashellspq.vn', N'Vietcombank', '0091000123456', N'CONG TY SEASHELLS PQ', 5),
    ('SUP-BUS-HN01', N'Nhà xe Limousine Sao Việt',        'Transport', '0106004567', N'Nguyễn Văn Hùng', '0904111222', 'booking@saovietbus.vn',N'Techcombank', '1903000112233', N'CONG TY VAN TAI SAO VIET', 4),
    ('SUP-CRU-HL01', N'Du thuyền Ambassador Hạ Long',     'Hotel',     '5701998877', N'Trần Thu Trang', '02033888999', 'ops@ambassadorcruise.vn',N'BIDV',       '4401000998877', N'CONG TY AMBASSADOR HL', 5);
END
GO
