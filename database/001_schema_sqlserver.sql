/* ============================================================================
   QuanLyTourDuLichDb — SQL Server 2022 Enterprise Single-Tenant Schema
   Target: Docker mssql/server:2022-latest
   Note: SET ANSI_NULLS ON & QUOTED_IDENTIFIER ON are required for filtered
         unique indexes and persisted computed columns.
   ============================================================================ */

IF DB_ID(N'QuanLyTourDuLichDb') IS NULL
BEGIN
    CREATE DATABASE [QuanLyTourDuLichDb];
END
GO

USE [QuanLyTourDuLichDb];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ============================================================================
   MODULE 1: IDENTITY, RBAC, CONFIG & AUDIT (7 TABLES)
   ============================================================================ */

CREATE TABLE dbo.Roles (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Code        VARCHAR(30)       NOT NULL CONSTRAINT UQ_Roles_Code UNIQUE,
    Name        NVARCHAR(100)     NOT NULL,
    Description NVARCHAR(300)     NULL,
    CreatedAt   DATETIME2(0)      NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE dbo.Users (
    Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username         VARCHAR(60)       NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    Email            NVARCHAR(150)     NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    Phone            VARCHAR(20)       NULL,
    PasswordHash     NVARCHAR(500)     NOT NULL,
    FullName         NVARCHAR(120)     NOT NULL,
    UserType         VARCHAR(20)       NOT NULL CONSTRAINT DF_Users_UserType DEFAULT 'Staff',
    IsActive         BIT               NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    FailedLoginCount INT               NOT NULL CONSTRAINT DF_Users_FailedLogin DEFAULT 0,
    LockoutEndAt     DATETIME2(0)      NULL,
    LastLoginAt      DATETIME2(0)      NULL,
    CreatedAt        DATETIME2(0)      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt        DATETIME2(0)      NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Users_UserType CHECK (UserType IN ('Customer', 'Staff'))
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Users_Phone
    ON dbo.Users(Phone)
    WHERE Phone IS NOT NULL;
GO

CREATE TABLE dbo.UserRoles (
    UserId           INT          NOT NULL,
    RoleId           INT          NOT NULL,
    AssignedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_UserRoles_AssignedAt DEFAULT SYSUTCDATETIME(),
    AssignedByUserId INT          NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id),
    CONSTRAINT FK_UserRoles_AssignedBy FOREIGN KEY (AssignedByUserId) REFERENCES dbo.Users(Id)
);
GO

CREATE TABLE dbo.RefreshTokens (
    Id                  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
    UserId              INT                  NOT NULL,
    TokenHash           VARCHAR(128)         NOT NULL CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE,
    ExpiresAt           DATETIME2(0)         NOT NULL,
    RevokedAt           DATETIME2(0)         NULL,
    ReplacedByTokenHash VARCHAR(128)         NULL,
    CreatedByIp         VARCHAR(45)          NULL,
    UserAgent           NVARCHAR(300)        NULL,
    CreatedAt           DATETIME2(0)         NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserExpires
    ON dbo.RefreshTokens(UserId, ExpiresAt);
GO

CREATE TABLE dbo.SystemSettings (
    SettingKey      VARCHAR(80)   NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
    SettingValue    NVARCHAR(500) NOT NULL,
    ValueType       VARCHAR(20)   NOT NULL CONSTRAINT DF_SystemSettings_ValueType DEFAULT 'String',
    Description     NVARCHAR(300) NULL,
    UpdatedByUserId INT           NULL,
    UpdatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_SystemSettings_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_SystemSettings_ValueType CHECK (ValueType IN ('String', 'Int', 'Decimal', 'Bool', 'Json')),
    CONSTRAINT FK_SystemSettings_Users FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(Id)
);
GO

CREATE TABLE dbo.CompanyBankAccounts (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CompanyBankAccounts PRIMARY KEY,
    BankCode      VARCHAR(20)       NOT NULL,
    BankBin       VARCHAR(10)       NOT NULL,
    BankName      NVARCHAR(120)     NOT NULL,
    AccountNumber VARCHAR(40)       NOT NULL,
    AccountHolder NVARCHAR(150)     NOT NULL,
    BranchName    NVARCHAR(150)     NULL,
    IsDefault     BIT               NOT NULL CONSTRAINT DF_CompanyBankAccounts_IsDefault DEFAULT 0,
    IsActive      BIT               NOT NULL CONSTRAINT DF_CompanyBankAccounts_IsActive DEFAULT 1,
    CreatedAt     DATETIME2(0)      NOT NULL CONSTRAINT DF_CompanyBankAccounts_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_CompanyBankAccounts_BankAcc UNIQUE (BankCode, AccountNumber)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_CompanyBankAccounts_Default
    ON dbo.CompanyBankAccounts(IsDefault)
    WHERE IsDefault = 1;
GO

CREATE TABLE dbo.AuditLogs (
    Id           BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    ActorUserId  INT                  NULL,
    ActorRole    VARCHAR(30)          NULL,
    Action       VARCHAR(80)          NOT NULL,
    EntityName   VARCHAR(80)          NOT NULL,
    EntityId     VARCHAR(64)          NOT NULL,
    OldValueJson NVARCHAR(MAX)        NULL,
    NewValueJson NVARCHAR(MAX)        NULL,
    IpAddress    VARCHAR(45)          NULL,
    UserAgent    NVARCHAR(300)        NULL,
    CreatedAt    DATETIME2(0)         NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(Id)
);
GO

CREATE NONCLUSTERED INDEX IX_AuditLogs_Entity
    ON dbo.AuditLogs(EntityName, EntityId, CreatedAt DESC);
GO

CREATE NONCLUSTERED INDEX IX_AuditLogs_Actor
    ON dbo.AuditLogs(ActorUserId, CreatedAt DESC);
GO

/* ============================================================================
   MODULE 2: CUSTOMER & CRM (1 TABLE)
   ============================================================================ */

CREATE TABLE dbo.Customers (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    CustomerCode  VARCHAR(30)       NOT NULL CONSTRAINT UQ_Customers_Code UNIQUE,
    UserId        INT               NULL,
    FullName      NVARCHAR(120)     NOT NULL,
    Phone         VARCHAR(20)       NOT NULL CONSTRAINT UQ_Customers_Phone UNIQUE,
    Email         NVARCHAR(150)     NULL,
    Address       NVARCHAR(300)     NULL,
    Source        VARCHAR(30)       NOT NULL CONSTRAINT DF_Customers_Source DEFAULT 'Web',
    LoyaltyTier   VARCHAR(20)       NOT NULL CONSTRAINT DF_Customers_Tier DEFAULT 'Standard',
    TotalSpent    DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Customers_TotalSpent DEFAULT 0,
    TotalBookings INT               NOT NULL CONSTRAINT DF_Customers_TotalBookings DEFAULT 0,
    Notes         NVARCHAR(500)     NULL,
    CreatedAt     DATETIME2(0)      NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt     DATETIME2(0)      NOT NULL CONSTRAINT DF_Customers_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Customers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Customers_Source CHECK (Source IN ('Web', 'WalkIn', 'Zalo', 'Facebook', 'Hotline', 'Corporate')),
    CONSTRAINT CK_Customers_Tier CHECK (LoyaltyTier IN ('Standard', 'Silver', 'Gold', 'VIP')),
    CONSTRAINT CK_Customers_Totals CHECK (TotalSpent >= 0 AND TotalBookings >= 0)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Customers_UserId
    ON dbo.Customers(UserId)
    WHERE UserId IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Customers_Email
    ON dbo.Customers(Email)
    WHERE Email IS NOT NULL;
GO

/* ============================================================================
   MODULE 3: TOURS, PRICING & CANCELLATION POLICIES (5 TABLES)
   ============================================================================ */

CREATE TABLE dbo.CancellationPolicies (
    Id                         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CancellationPolicies PRIMARY KEY,
    Code                       VARCHAR(40)       NOT NULL CONSTRAINT UQ_CancellationPolicies_Code UNIQUE,
    Name                       NVARCHAR(150)     NOT NULL,
    Description                NVARCHAR(500)     NULL,
    CompanyCancelRefundPercent DECIMAL(5,2)      NOT NULL CONSTRAINT DF_CancelPol_CompRefund DEFAULT 100.00,
    AllowReschedule            BIT               NOT NULL CONSTRAINT DF_CancelPol_AllowResched DEFAULT 1,
    RescheduleCutoffDays       INT               NOT NULL CONSTRAINT DF_CancelPol_ReschedDays DEFAULT 7,
    RescheduleFeePercent       DECIMAL(5,2)      NOT NULL CONSTRAINT DF_CancelPol_ReschedFee DEFAULT 0.00,
    IsDefault                  BIT               NOT NULL CONSTRAINT DF_CancelPol_IsDefault DEFAULT 0,
    IsActive                   BIT               NOT NULL CONSTRAINT DF_CancelPol_IsActive DEFAULT 1,
    CreatedAt                  DATETIME2(0)      NOT NULL CONSTRAINT DF_CancelPol_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_CancelPol_Percents CHECK (
        CompanyCancelRefundPercent BETWEEN 0 AND 100
        AND RescheduleFeePercent BETWEEN 0 AND 100
        AND RescheduleCutoffDays >= 0
    )
);
GO

CREATE TABLE dbo.CancellationPolicyRules (
    Id                      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CancellationPolicyRules PRIMARY KEY,
    PolicyId                INT               NOT NULL,
    MinDaysBeforeDeparture  INT               NOT NULL,
    MaxDaysBeforeDeparture  INT               NULL,
    PenaltyPercent          DECIMAL(5,2)      NOT NULL,
    Label                   NVARCHAR(150)     NOT NULL,
    SortOrder               INT               NOT NULL CONSTRAINT DF_CancelRule_Sort DEFAULT 0,
    CONSTRAINT FK_CancelRule_Policy FOREIGN KEY (PolicyId) REFERENCES dbo.CancellationPolicies(Id) ON DELETE CASCADE,
    CONSTRAINT CK_CancelRule_Days CHECK (
        MinDaysBeforeDeparture >= 0
        AND (MaxDaysBeforeDeparture IS NULL OR MaxDaysBeforeDeparture >= MinDaysBeforeDeparture)
    ),
    CONSTRAINT CK_CancelRule_Penalty CHECK (PenaltyPercent BETWEEN 0 AND 100)
);
GO

CREATE NONCLUSTERED INDEX IX_CancelRule_Policy
    ON dbo.CancellationPolicyRules(PolicyId, SortOrder);
GO

CREATE TABLE dbo.Tours (
    Id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tours PRIMARY KEY,
    Code                 VARCHAR(40)       NOT NULL CONSTRAINT UQ_Tours_Code UNIQUE,
    Slug                 VARCHAR(160)      NOT NULL CONSTRAINT UQ_Tours_Slug UNIQUE,
    Title                NVARCHAR(250)     NOT NULL,
    TourType             VARCHAR(20)       NOT NULL CONSTRAINT DF_Tours_TourType DEFAULT 'GroupTour',
    ScopeType            VARCHAR(20)       NOT NULL CONSTRAINT DF_Tours_ScopeType DEFAULT 'Domestic',
    Tags                 NVARCHAR(200)     NULL,
    DepartureFrom        NVARCHAR(100)     NOT NULL,
    Destination          NVARCHAR(150)     NOT NULL,
    DurationDays         TINYINT           NOT NULL,
    DurationNights       TINYINT           NOT NULL,
    Transport            NVARCHAR(60)      NOT NULL,
    OriginalPrice        DECIMAL(18,2)     NULL,
    BaseAdultPrice       DECIMAL(18,2)     NOT NULL,
    ChildPricePercent    DECIMAL(5,2)      NOT NULL CONSTRAINT DF_Tours_ChildPct DEFAULT 75.00,
    InfantPrice          DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Tours_InfantPrice DEFAULT 0,
    SingleSupplement     DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Tours_SingleSupp DEFAULT 0,
    CancellationPolicyId INT               NOT NULL,
    Summary              NVARCHAR(600)     NULL,
    HighlightsJson       NVARCHAR(MAX)     NULL,
    IncludesJson         NVARCHAR(MAX)     NULL,
    ExcludesJson         NVARCHAR(MAX)     NULL,
    Badges               NVARCHAR(200)     NULL,
    IsPublic             BIT               NOT NULL CONSTRAINT DF_Tours_IsPublic DEFAULT 1,
    Status               VARCHAR(20)       NOT NULL CONSTRAINT DF_Tours_Status DEFAULT 'Draft',
    AverageRating        DECIMAL(3,2)      NOT NULL CONSTRAINT DF_Tours_AvgRating DEFAULT 0,
    ReviewCount          INT               NOT NULL CONSTRAINT DF_Tours_ReviewCount DEFAULT 0,
    BookedCountTotal     INT               NOT NULL CONSTRAINT DF_Tours_BookedCount DEFAULT 0,
    CreatedByUserId      INT               NULL,
    CreatedAt            DATETIME2(0)      NOT NULL CONSTRAINT DF_Tours_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt            DATETIME2(0)      NOT NULL CONSTRAINT DF_Tours_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Tours_CancelPolicy FOREIGN KEY (CancellationPolicyId) REFERENCES dbo.CancellationPolicies(Id),
    CONSTRAINT FK_Tours_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Tours_Type CHECK (TourType IN ('GroupTour', 'PrivateTour', 'CustomTour')),
    CONSTRAINT CK_Tours_Scope CHECK (ScopeType IN ('Domestic', 'International')),
    CONSTRAINT CK_Tours_Status CHECK (Status IN ('Draft', 'Published', 'Suspended', 'Archived')),
    CONSTRAINT CK_Tours_Values CHECK (
        DurationDays >= 1 AND DurationNights >= 0
        AND BaseAdultPrice >= 0 AND (OriginalPrice IS NULL OR OriginalPrice >= 0)
        AND ChildPricePercent BETWEEN 0 AND 100
        AND InfantPrice >= 0 AND SingleSupplement >= 0
        AND AverageRating BETWEEN 0 AND 5 AND ReviewCount >= 0 AND BookedCountTotal >= 0
    ),
    CONSTRAINT CK_Tours_Json CHECK (
        (HighlightsJson IS NULL OR ISJSON(HighlightsJson) = 1)
        AND (IncludesJson IS NULL OR ISJSON(IncludesJson) = 1)
        AND (ExcludesJson IS NULL OR ISJSON(ExcludesJson) = 1)
    )
);
GO

CREATE NONCLUSTERED INDEX IX_Tours_Catalog
    ON dbo.Tours(Status, IsPublic, ScopeType, DepartureFrom)
    INCLUDE (Code, Slug, Title, Destination, BaseAdultPrice, DurationDays, AverageRating);
GO

CREATE TABLE dbo.TourItineraries (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TourItineraries PRIMARY KEY,
    TourId        INT               NOT NULL,
    DayNumber     TINYINT           NOT NULL,
    Title         NVARCHAR(200)     NOT NULL,
    Meals         NVARCHAR(100)     NULL,
    Accommodation NVARCHAR(200)     NULL,
    Description   NVARCHAR(MAX)     NOT NULL,
    CONSTRAINT FK_TourItineraries_Tours FOREIGN KEY (TourId) REFERENCES dbo.Tours(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_TourItineraries_TourDay UNIQUE (TourId, DayNumber),
    CONSTRAINT CK_TourItineraries_Day CHECK (DayNumber >= 1)
);
GO

CREATE TABLE dbo.TourImages (
    Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TourImages PRIMARY KEY,
    TourId    INT               NOT NULL,
    ImageUrl  NVARCHAR(500)     NOT NULL,
    AltText   NVARCHAR(200)     NULL,
    IsCover   BIT               NOT NULL CONSTRAINT DF_TourImages_IsCover DEFAULT 0,
    SortOrder INT               NOT NULL CONSTRAINT DF_TourImages_Sort DEFAULT 0,
    CONSTRAINT FK_TourImages_Tours FOREIGN KEY (TourId) REFERENCES dbo.Tours(Id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_TourImages_TourSort
    ON dbo.TourImages(TourId, SortOrder);
GO

/* ============================================================================
   MODULE 4: DEPARTURES, OPERATIONS & SUPPLIERS (6 TABLES)
   ============================================================================ */

CREATE TABLE dbo.TourDepartures (
    Id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TourDepartures PRIMARY KEY,
    TourId               INT               NOT NULL,
    DepartureCode        VARCHAR(50)       NOT NULL CONSTRAINT UQ_TourDepartures_Code UNIQUE,
    DepartureDate        DATE              NOT NULL,
    ReturnDate           DATE              NOT NULL,
    CutoffAt             DATETIME2(0)      NOT NULL,
    AdultPrice           DECIMAL(18,2)     NOT NULL,
    ChildPrice           DECIMAL(18,2)     NULL,
    InfantPrice          DECIMAL(18,2)     NULL,
    SingleSupplement     DECIMAL(18,2)     NULL,
    TotalCapacity        INT               NOT NULL,
    HeldSlots            INT               NOT NULL CONSTRAINT DF_Departures_Held DEFAULT 0,
    BookedSlots          INT               NOT NULL CONSTRAINT DF_Departures_Booked DEFAULT 0,
    AvailableSlots       AS (TotalCapacity - HeldSlots - BookedSlots) PERSISTED,
    MinParticipants      INT               NOT NULL CONSTRAINT DF_Departures_MinPax DEFAULT 1,
    MaxInfants           INT               NULL,
    CancellationPolicyId INT               NULL,
    IsPublic             BIT               NOT NULL CONSTRAINT DF_Departures_IsPublic DEFAULT 1,
    ContractCode         VARCHAR(60)       NULL,
    Status               VARCHAR(20)       NOT NULL CONSTRAINT DF_Departures_Status DEFAULT 'Open',
    VehicleType          NVARCHAR(80)      NULL,
    VehiclePlate         VARCHAR(30)       NULL,
    DriverName           NVARCHAR(120)     NULL,
    DriverPhone          VARCHAR(20)       NULL,
    Notes                NVARCHAR(500)     NULL,
    SettledAt            DATETIME2(0)      NULL,
    SettledByUserId      INT               NULL,
    RowVersion           ROWVERSION        NOT NULL,
    CreatedAt            DATETIME2(0)      NOT NULL CONSTRAINT DF_Departures_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt            DATETIME2(0)      NOT NULL CONSTRAINT DF_Departures_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Departures_Tours FOREIGN KEY (TourId) REFERENCES dbo.Tours(Id),
    CONSTRAINT FK_Departures_CancelPolicy FOREIGN KEY (CancellationPolicyId) REFERENCES dbo.CancellationPolicies(Id),
    CONSTRAINT FK_Departures_SettledBy FOREIGN KEY (SettledByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Departures_Dates CHECK (ReturnDate >= DepartureDate),
    CONSTRAINT CK_Departures_Prices CHECK (
        AdultPrice >= 0
        AND (ChildPrice IS NULL OR ChildPrice >= 0)
        AND (InfantPrice IS NULL OR InfantPrice >= 0)
        AND (SingleSupplement IS NULL OR SingleSupplement >= 0)
    ),
    CONSTRAINT CK_TourDepartures_Slots CHECK (
        TotalCapacity >= 1
        AND HeldSlots >= 0
        AND BookedSlots >= 0
        AND HeldSlots + BookedSlots <= TotalCapacity
        AND MinParticipants >= 1
        AND (MaxInfants IS NULL OR MaxInfants >= 0)
    ),
    CONSTRAINT CK_Departures_Status CHECK (
        Status IN ('Open', 'Full', 'Confirmed', 'InProgress', 'Completed', 'Settled', 'Cancelled')
    )
);
GO

CREATE NONCLUSTERED INDEX IX_TourDepartures_Search
    ON dbo.TourDepartures(TourId, Status, IsPublic, DepartureDate)
    INCLUDE (AdultPrice, AvailableSlots, CutoffAt);
GO

CREATE TABLE dbo.DepartureGuides (
    Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DepartureGuides PRIMARY KEY,
    DepartureId      INT               NOT NULL,
    GuideUserId      INT               NOT NULL,
    GuideRole        VARCHAR(20)       NOT NULL CONSTRAINT DF_DepGuides_Role DEFAULT 'Lead',
    AssignedAt       DATETIME2(0)      NOT NULL CONSTRAINT DF_DepGuides_AssignedAt DEFAULT SYSUTCDATETIME(),
    AssignedByUserId INT               NULL,
    Notes            NVARCHAR(300)     NULL,
    CONSTRAINT FK_DepGuides_Departure FOREIGN KEY (DepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_DepGuides_Guide FOREIGN KEY (GuideUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_DepGuides_AssignedBy FOREIGN KEY (AssignedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT UQ_DepartureGuides_DepGuide UNIQUE (DepartureId, GuideUserId),
    CONSTRAINT CK_DepGuides_Role CHECK (GuideRole IN ('Lead', 'Assistant'))
);
GO

CREATE TABLE dbo.DepartureChecklists (
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DepartureChecklists PRIMARY KEY,
    DepartureId       INT               NOT NULL,
    TaskName          NVARCHAR(200)     NOT NULL,
    IsCompleted       BIT               NOT NULL CONSTRAINT DF_DepChecklist_Done DEFAULT 0,
    CompletedByUserId INT               NULL,
    CompletedAt       DATETIME2(0)      NULL,
    Notes             NVARCHAR(300)     NULL,
    SortOrder         INT               NOT NULL CONSTRAINT DF_DepChecklist_Sort DEFAULT 0,
    CONSTRAINT FK_DepChecklist_Departure FOREIGN KEY (DepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_DepChecklist_User FOREIGN KEY (CompletedByUserId) REFERENCES dbo.Users(Id)
);
GO

CREATE NONCLUSTERED INDEX IX_DepartureChecklists_DepSort
    ON dbo.DepartureChecklists(DepartureId, SortOrder);
GO

CREATE TABLE dbo.Suppliers (
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    Code              VARCHAR(40)       NOT NULL CONSTRAINT UQ_Suppliers_Code UNIQUE,
    Name              NVARCHAR(200)     NOT NULL,
    SupplierType      VARCHAR(30)       NOT NULL,
    TaxCode           VARCHAR(20)       NULL,
    ContactName       NVARCHAR(120)     NULL,
    Phone             VARCHAR(20)       NULL,
    Email             NVARCHAR(150)     NULL,
    Address           NVARCHAR(300)     NULL,
    BankName          NVARCHAR(120)     NULL,
    BankAccountNumber VARCHAR(40)       NULL,
    BankAccountHolder NVARCHAR(150)     NULL,
    InternalRating    TINYINT           NULL,
    IsActive          BIT               NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT 1,
    Notes             NVARCHAR(500)     NULL,
    CreatedAt         DATETIME2(0)      NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Suppliers_Type CHECK (SupplierType IN ('Hotel', 'Transport', 'Airline', 'Restaurant', 'Ticket', 'Visa', 'Other')),
    CONSTRAINT CK_Suppliers_Rating CHECK (InternalRating IS NULL OR InternalRating BETWEEN 1 AND 5)
);
GO

CREATE TABLE dbo.DepartureServices (
    Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DepartureServices PRIMARY KEY,
    DepartureId      INT               NOT NULL,
    SupplierId       INT               NOT NULL,
    ServiceName      NVARCHAR(200)     NOT NULL,
    Quantity         DECIMAL(10,2)     NOT NULL,
    UnitPrice        DECIMAL(18,2)     NOT NULL,
    EstimatedCost    AS (CAST(ROUND(Quantity * UnitPrice, 2) AS DECIMAL(18,2))) PERSISTED,
    ActualCost       DECIMAL(18,2)     NOT NULL CONSTRAINT DF_DepSvc_Actual DEFAULT 0,
    PaidAmount       DECIMAL(18,2)     NOT NULL CONSTRAINT DF_DepSvc_Paid DEFAULT 0,
    PaymentStatus    VARCHAR(20)       NOT NULL CONSTRAINT DF_DepSvc_PayStatus DEFAULT 'Unpaid',
    BookingReference VARCHAR(80)       NULL,
    Notes            NVARCHAR(400)     NULL,
    RowVersion       ROWVERSION        NOT NULL,
    CreatedAt        DATETIME2(0)      NOT NULL CONSTRAINT DF_DepSvc_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_DepSvc_Departure FOREIGN KEY (DepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_DepSvc_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id),
    CONSTRAINT CK_DepSvc_Values CHECK (Quantity > 0 AND UnitPrice >= 0 AND ActualCost >= 0 AND PaidAmount >= 0),
    CONSTRAINT CK_DepSvc_PayStatus CHECK (PaymentStatus IN ('Unpaid', 'Partial', 'Paid'))
);
GO

CREATE NONCLUSTERED INDEX IX_DepartureServices_Dep
    ON dbo.DepartureServices(DepartureId, SupplierId);
GO

CREATE TABLE dbo.DepartureExtraExpenses (
    Id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DepartureExtraExpenses PRIMARY KEY,
    DepartureId      INT                  NOT NULL,
    Title            NVARCHAR(200)        NOT NULL,
    Category         VARCHAR(40)          NOT NULL CONSTRAINT DF_ExtraExp_Cat DEFAULT 'Other',
    Amount           DECIMAL(18,2)        NOT NULL,
    ExpenseDate      DATETIME2(0)         NOT NULL CONSTRAINT DF_ExtraExp_Date DEFAULT SYSUTCDATETIME(),
    AttachmentUrl    NVARCHAR(500)        NULL,
    Description      NVARCHAR(500)        NULL,
    CreatedByUserId  INT                  NOT NULL,
    ApprovalStatus   VARCHAR(20)          NOT NULL CONSTRAINT DF_ExtraExp_Status DEFAULT 'Pending',
    ReviewedByUserId INT                  NULL,
    ReviewedAt       DATETIME2(0)         NULL,
    ReviewNote       NVARCHAR(300)        NULL,
    CreatedAt        DATETIME2(0)         NOT NULL CONSTRAINT DF_ExtraExp_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ExtraExp_Departure FOREIGN KEY (DepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_ExtraExp_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_ExtraExp_ReviewedBy FOREIGN KEY (ReviewedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_ExtraExp_Amount CHECK (Amount > 0),
    CONSTRAINT CK_ExtraExp_Cat CHECK (Category IN ('Meal', 'Transport', 'Ticket', 'Medical', 'Tip', 'Other')),
    CONSTRAINT CK_ExtraExp_Status CHECK (ApprovalStatus IN ('Pending', 'Approved', 'Rejected'))
);
GO

CREATE NONCLUSTERED INDEX IX_ExtraExpenses_DepStatus
    ON dbo.DepartureExtraExpenses(DepartureId, ApprovalStatus);
GO

/* ============================================================================
   MODULE 5: PROMOTIONS, BOOKINGS & PASSENGERS (6 TABLES)
   ============================================================================ */

CREATE TABLE dbo.Promotions (
    Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Promotions PRIMARY KEY,
    Code                VARCHAR(40)       NOT NULL CONSTRAINT UQ_Promotions_Code UNIQUE,
    Name                NVARCHAR(150)     NOT NULL,
    Description         NVARCHAR(400)     NULL,
    DiscountType        VARCHAR(20)       NOT NULL,
    DiscountValue       DECIMAL(18,2)     NOT NULL,
    MaxDiscountAmount   DECIMAL(18,2)     NULL,
    MinOrderAmount      DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Promotions_MinOrder DEFAULT 0,
    MinPax              INT               NOT NULL CONSTRAINT DF_Promotions_MinPax DEFAULT 1,
    ApplyAllTours       BIT               NOT NULL CONSTRAINT DF_Promotions_AllTours DEFAULT 1,
    UsageLimit          INT               NULL,
    UsedCount           INT               NOT NULL CONSTRAINT DF_Promotions_UsedCount DEFAULT 0,
    MaxUsagePerCustomer INT               NOT NULL CONSTRAINT DF_Promotions_MaxPerCust DEFAULT 1,
    ValidFrom           DATETIME2(0)      NOT NULL,
    ValidTo             DATETIME2(0)      NOT NULL,
    IsActive            BIT               NOT NULL CONSTRAINT DF_Promotions_IsActive DEFAULT 1,
    RowVersion          ROWVERSION        NOT NULL,
    CreatedAt           DATETIME2(0)      NOT NULL CONSTRAINT DF_Promotions_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Promotions_Type CHECK (DiscountType IN ('Percent', 'FixedBooking', 'FixedPerPax')),
    CONSTRAINT CK_Promotions_Values CHECK (
        DiscountValue > 0
        AND (MaxDiscountAmount IS NULL OR MaxDiscountAmount >= 0)
        AND MinOrderAmount >= 0 AND MinPax >= 1
        AND (UsageLimit IS NULL OR UsageLimit >= 1)
        AND UsedCount >= 0
        AND MaxUsagePerCustomer >= 1
        AND ValidTo > ValidFrom
    )
);
GO

CREATE TABLE dbo.PromotionTours (
    PromotionId INT NOT NULL,
    TourId      INT NOT NULL,
    CONSTRAINT PK_PromotionTours PRIMARY KEY (PromotionId, TourId),
    CONSTRAINT FK_PromotionTours_Promo FOREIGN KEY (PromotionId) REFERENCES dbo.Promotions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PromotionTours_Tour FOREIGN KEY (TourId) REFERENCES dbo.Tours(Id)
);
GO

CREATE TABLE dbo.Bookings (
    Id                             BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY,
    BookingCode                    VARCHAR(32)          NOT NULL CONSTRAINT UQ_Bookings_Code UNIQUE,
    TourId                         INT                  NOT NULL,
    DepartureId                    INT                  NOT NULL,
    CustomerId                     INT                  NOT NULL,
    CreatedByUserId                INT                  NULL,
    SalesUserId                    INT                  NULL,
    BookingChannel                 VARCHAR(20)          NOT NULL CONSTRAINT DF_Bookings_Channel DEFAULT 'WebB2C',
    ContactName                    NVARCHAR(120)        NOT NULL,
    ContactPhone                   VARCHAR(20)          NOT NULL,
    ContactEmail                   NVARCHAR(150)        NOT NULL,
    SpecialNote                    NVARCHAR(500)        NULL,
    AdultCount                     INT                  NOT NULL,
    ChildCount                     INT                  NOT NULL CONSTRAINT DF_Bookings_Child DEFAULT 0,
    InfantCount                    INT                  NOT NULL CONSTRAINT DF_Bookings_Infant DEFAULT 0,
    SingleRoomCount                INT                  NOT NULL CONSTRAINT DF_Bookings_SingleRoom DEFAULT 0,
    SeatsReserved                  AS (AdultCount + ChildCount) PERSISTED,
    SubTotal                       DECIMAL(18,2)        NOT NULL,
    DiscountAmount                 DECIMAL(18,2)        NOT NULL CONSTRAINT DF_Bookings_Discount DEFAULT 0,
    TaxMode                        VARCHAR(20)          NOT NULL CONSTRAINT DF_Bookings_TaxMode DEFAULT 'Included',
    VatRate                        DECIMAL(5,2)         NOT NULL CONSTRAINT DF_Bookings_VatRate DEFAULT 8.00,
    AmountBeforeTax                DECIMAL(18,2)        NOT NULL,
    VatAmount                      DECIMAL(18,2)        NOT NULL,
    TotalAmount                    DECIMAL(18,2)        NOT NULL,
    RequiredDepositAmount          DECIMAL(18,2)        NOT NULL,
    PaidAmount                     DECIMAL(18,2)        NOT NULL CONSTRAINT DF_Bookings_Paid DEFAULT 0,
    CreditAmount                   DECIMAL(18,2)        NOT NULL CONSTRAINT DF_Bookings_Credit DEFAULT 0,
    RemainingAmount                AS (CASE WHEN TotalAmount > PaidAmount THEN TotalAmount - PaidAmount ELSE CAST(0 AS DECIMAL(18,2)) END) PERSISTED,
    PromotionId                    INT                  NULL,
    PromoCodeApplied               VARCHAR(40)          NULL,
    PayOption                      VARCHAR(20)          NOT NULL CONSTRAINT DF_Bookings_PayOption DEFAULT 'Full',
    RequireVatInvoice              BIT                  NOT NULL CONSTRAINT DF_Bookings_RequireVat DEFAULT 0,
    InvoiceCompanyName             NVARCHAR(200)        NULL,
    InvoiceTaxCode                 VARCHAR(20)          NULL,
    InvoiceAddress                 NVARCHAR(300)        NULL,
    InvoiceEmail                   NVARCHAR(150)        NULL,
    PriceSnapshotJson              NVARCHAR(MAX)        NOT NULL,
    CancellationPolicySnapshotJson NVARCHAR(MAX)        NOT NULL,
    Status                         VARCHAR(30)          NOT NULL CONSTRAINT DF_Bookings_Status DEFAULT 'PendingPayment',
    HoldExpiresAt                  DATETIME2(0)         NOT NULL,
    FinalPaymentDueAt              DATETIME2(0)         NULL,
    CancellationReason             NVARCHAR(300)        NULL,
    CancelPenaltyPercent           DECIMAL(5,2)         NULL,
    CancelPenaltyAmount            DECIMAL(18,2)        NULL,
    CancelApprovedByUserId         INT                  NULL,
    CancelledAt                    DATETIME2(0)         NULL,
    RowVersion                     ROWVERSION           NOT NULL,
    CreatedAt                      DATETIME2(0)         NOT NULL CONSTRAINT DF_Bookings_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt                      DATETIME2(0)         NOT NULL CONSTRAINT DF_Bookings_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Bookings_Tour FOREIGN KEY (TourId) REFERENCES dbo.Tours(Id),
    CONSTRAINT FK_Bookings_Departure FOREIGN KEY (DepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_Bookings_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
    CONSTRAINT FK_Bookings_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Bookings_Sales FOREIGN KEY (SalesUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Bookings_Promotion FOREIGN KEY (PromotionId) REFERENCES dbo.Promotions(Id),
    CONSTRAINT FK_Bookings_CancelApprovedBy FOREIGN KEY (CancelApprovedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Bookings_Channel CHECK (BookingChannel IN ('WebB2C', 'WalkIn', 'Zalo', 'Facebook', 'Hotline', 'Corporate')),
    CONSTRAINT CK_Bookings_TaxMode CHECK (TaxMode IN ('Included', 'Excluded')),
    CONSTRAINT CK_Bookings_PayOption CHECK (PayOption IN ('Full', 'Deposit')),
    CONSTRAINT CK_Bookings_Status CHECK (
        Status IN (
            'PendingPayment', 'Expired', 'DepositPaid', 'FinalPaymentOverdue',
            'Confirmed', 'InProgress', 'Completed',
            'CancellationRequested', 'Cancelled', 'Refunded'
        )
    ),
    CONSTRAINT CK_Bookings_Pax CHECK (
        AdultCount >= 1 AND ChildCount >= 0 AND InfantCount >= 0 AND SingleRoomCount >= 0
    ),
    CONSTRAINT CK_Bookings_Amounts CHECK (
        SubTotal >= 0 AND DiscountAmount >= 0 AND VatRate BETWEEN 0 AND 100
        AND AmountBeforeTax >= 0 AND VatAmount >= 0 AND TotalAmount >= 0
        AND RequiredDepositAmount >= 0 AND PaidAmount >= 0 AND CreditAmount >= 0
        AND (CancelPenaltyPercent IS NULL OR CancelPenaltyPercent BETWEEN 0 AND 100)
        AND (CancelPenaltyAmount IS NULL OR CancelPenaltyAmount >= 0)
    ),
    CONSTRAINT CK_Bookings_SnapshotsJson CHECK (
        ISJSON(PriceSnapshotJson) = 1 AND ISJSON(CancellationPolicySnapshotJson) = 1
    )
);
GO

CREATE NONCLUSTERED INDEX IX_Bookings_HoldExpiryWorker
    ON dbo.Bookings(Status, HoldExpiresAt)
    INCLUDE (DepartureId, SeatsReserved, PromotionId);
GO

CREATE NONCLUSTERED INDEX IX_Bookings_FinalDueWorker
    ON dbo.Bookings(Status, FinalPaymentDueAt)
    INCLUDE (CustomerId, SalesUserId, TotalAmount, PaidAmount);
GO

CREATE NONCLUSTERED INDEX IX_Bookings_Departure
    ON dbo.Bookings(DepartureId, Status);
GO

CREATE NONCLUSTERED INDEX IX_Bookings_Customer
    ON dbo.Bookings(CustomerId, CreatedAt DESC);
GO

CREATE TABLE dbo.PromotionUsages (
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PromotionUsages PRIMARY KEY,
    PromotionId INT                  NOT NULL,
    BookingId   BIGINT               NOT NULL CONSTRAINT UQ_PromotionUsages_Booking UNIQUE,
    CustomerId  INT                  NOT NULL,
    Status      VARCHAR(20)          NOT NULL CONSTRAINT DF_PromoUsage_Status DEFAULT 'Reserved',
    ReservedAt  DATETIME2(0)         NOT NULL CONSTRAINT DF_PromoUsage_ReservedAt DEFAULT SYSUTCDATETIME(),
    ReleasedAt  DATETIME2(0)         NULL,
    CONSTRAINT FK_PromoUsage_Promo FOREIGN KEY (PromotionId) REFERENCES dbo.Promotions(Id),
    CONSTRAINT FK_PromoUsage_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_PromoUsage_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
    CONSTRAINT CK_PromoUsage_Status CHECK (Status IN ('Reserved', 'Redeemed', 'Released'))
);
GO

CREATE NONCLUSTERED INDEX IX_PromotionUsages_PromoCust
    ON dbo.PromotionUsages(PromotionId, CustomerId, Status);
GO

CREATE TABLE dbo.BookingPassengers (
    Id                 BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BookingPassengers PRIMARY KEY,
    BookingId          BIGINT               NOT NULL,
    PaxType            VARCHAR(20)          NOT NULL,
    FullName           NVARCHAR(120)        NOT NULL,
    Gender             NVARCHAR(16)         NULL,
    DateOfBirth        DATE                 NULL,
    IdentityNumber     VARCHAR(40)          NULL,
    PassportExpiryDate DATE                 NULL,
    Nationality        NVARCHAR(60)         NOT NULL CONSTRAINT DF_Passengers_Nat DEFAULT N'Việt Nam',
    SpecialRequest     NVARCHAR(300)        NULL,
    CheckInStatus      VARCHAR(20)          NOT NULL CONSTRAINT DF_Passengers_CheckIn DEFAULT 'Pending',
    CheckedInAt        DATETIME2(0)         NULL,
    CheckedInByUserId  INT                  NULL,
    CreatedAt          DATETIME2(0)         NOT NULL CONSTRAINT DF_Passengers_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt          DATETIME2(0)         NOT NULL CONSTRAINT DF_Passengers_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Passengers_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Passengers_CheckedBy FOREIGN KEY (CheckedInByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Passengers_Type CHECK (PaxType IN ('Adult', 'Child', 'Infant')),
    CONSTRAINT CK_Passengers_CheckIn CHECK (CheckInStatus IN ('Pending', 'CheckedIn', 'Late', 'NoShow'))
);
GO

CREATE NONCLUSTERED INDEX IX_BookingPassengers_Booking
    ON dbo.BookingPassengers(BookingId);
GO

CREATE TABLE dbo.BookingReschedules (
    Id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BookingReschedules PRIMARY KEY,
    BookingId        BIGINT               NOT NULL,
    OldDepartureId   INT                  NOT NULL,
    NewDepartureId   INT                  NOT NULL,
    OldTotalAmount   DECIMAL(18,2)        NOT NULL,
    NewTotalAmount   DECIMAL(18,2)        NOT NULL,
    RescheduleFee    DECIMAL(18,2)        NOT NULL CONSTRAINT DF_Reschedule_Fee DEFAULT 0,
    PriceDifference  DECIMAL(18,2)        NOT NULL,
    Reason           NVARCHAR(300)        NULL,
    ApprovedByUserId INT                  NULL,
    CreatedAt        DATETIME2(0)         NOT NULL CONSTRAINT DF_Reschedule_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Reschedule_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Reschedule_OldDep FOREIGN KEY (OldDepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_Reschedule_NewDep FOREIGN KEY (NewDepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_Reschedule_ApprovedBy FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Reschedule_Amounts CHECK (OldTotalAmount >= 0 AND NewTotalAmount >= 0 AND RescheduleFee >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_BookingReschedules_Booking
    ON dbo.BookingReschedules(BookingId, CreatedAt DESC);
GO

/* ============================================================================
   MODULE 6: PAYMENTS, RECONCILIATION, REFUNDS, INVOICES & LEDGER (5 TABLES)
   ============================================================================ */

CREATE TABLE dbo.PaymentTransactions (
    Id                   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentTransactions PRIMARY KEY,
    TransactionCode      VARCHAR(40)          NOT NULL CONSTRAINT UQ_PaymentTransactions_Code UNIQUE,
    BookingId            BIGINT               NOT NULL,
    CompanyBankAccountId INT                  NULL,
    Method               VARCHAR(30)          NOT NULL,
    TransferContent      VARCHAR(100)         NULL,
    QrPayload            NVARCHAR(600)        NULL,
    ExpectedAmount       DECIMAL(18,2)        NOT NULL,
    ActualReceivedAmount DECIMAL(18,2)        NOT NULL CONSTRAINT DF_PayTx_Actual DEFAULT 0,
    Status               VARCHAR(30)          NOT NULL CONSTRAINT DF_PayTx_Status DEFAULT 'Pending',
    ReconciliationStatus VARCHAR(20)          NOT NULL CONSTRAINT DF_PayTx_ReconStatus DEFAULT 'Unreconciled',
    IdempotencyKey       VARCHAR(100)         NULL,
    BankCode             VARCHAR(20)          NULL,
    BankRef              VARCHAR(100)         NULL,
    EvidenceUrl          NVARCHAR(500)        NULL,
    ReconciledByUserId   INT                  NULL,
    ReconciledAt         DATETIME2(0)         NULL,
    ReconciliationNote   NVARCHAR(400)        NULL,
    ExpiresAt            DATETIME2(0)         NULL,
    PaidAt               DATETIME2(0)         NULL,
    RawCallbackJson      NVARCHAR(MAX)        NULL,
    RowVersion           ROWVERSION           NOT NULL,
    CreatedAt            DATETIME2(0)         NOT NULL CONSTRAINT DF_PayTx_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PayTx_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_PayTx_BankAcc FOREIGN KEY (CompanyBankAccountId) REFERENCES dbo.CompanyBankAccounts(Id),
    CONSTRAINT FK_PayTx_ReconciledBy FOREIGN KEY (ReconciledByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_PayTx_Method CHECK (Method IN ('QR_SIMULATED', 'BANK_TRANSFER_MANUAL', 'CASH')),
    CONSTRAINT CK_PayTx_Amounts CHECK (ExpectedAmount > 0 AND ActualReceivedAmount >= 0),
    CONSTRAINT CK_PayTx_Status CHECK (Status IN ('Pending', 'Succeeded', 'Failed', 'NeedReconciliation', 'Expired', 'Cancelled')),
    CONSTRAINT CK_PayTx_ReconStatus CHECK (ReconciliationStatus IN ('Unreconciled', 'Matched', 'Discrepancy', 'Resolved'))
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_PaymentTransactions_IdempotencyKey
    ON dbo.PaymentTransactions(IdempotencyKey)
    WHERE IdempotencyKey IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_PaymentTransactions_BankRef
    ON dbo.PaymentTransactions(BankCode, BankRef)
    WHERE BankRef IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX IX_PaymentTransactions_Booking
    ON dbo.PaymentTransactions(BookingId, CreatedAt DESC);
GO

CREATE NONCLUSTERED INDEX IX_PaymentTransactions_ReconQueue
    ON dbo.PaymentTransactions(Status, ReconciliationStatus, CreatedAt DESC);
GO

CREATE TABLE dbo.RefundRequests (
    Id                    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefundRequests PRIMARY KEY,
    RefundCode            VARCHAR(40)          NOT NULL CONSTRAINT UQ_RefundRequests_Code UNIQUE,
    BookingId             BIGINT               NOT NULL,
    RequestedByUserId     INT                  NULL,
    Reason                NVARCHAR(400)        NULL,
    PaidAmountSnapshot    DECIMAL(18,2)        NOT NULL,
    PenaltyAmountSnapshot DECIMAL(18,2)        NOT NULL,
    RefundAmount          DECIMAL(18,2)        NOT NULL,
    Status                VARCHAR(20)          NOT NULL CONSTRAINT DF_Refunds_Status DEFAULT 'PendingApproval',
    BankName              NVARCHAR(120)        NULL,
    BankAccountNumber     VARCHAR(40)          NULL,
    BankAccountHolder     NVARCHAR(150)        NULL,
    ApprovedByUserId      INT                  NULL,
    ApprovedAt            DATETIME2(0)         NULL,
    CompletedByUserId     INT                  NULL,
    CompletedAt           DATETIME2(0)         NULL,
    TransferRef           VARCHAR(100)         NULL,
    RejectReason          NVARCHAR(300)        NULL,
    CreatedAt             DATETIME2(0)         NOT NULL CONSTRAINT DF_Refunds_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Refunds_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Refunds_RequestedBy FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Refunds_ApprovedBy FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Refunds_CompletedBy FOREIGN KEY (CompletedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Refunds_Amounts CHECK (PaidAmountSnapshot >= 0 AND PenaltyAmountSnapshot >= 0 AND RefundAmount >= 0),
    CONSTRAINT CK_Refunds_Status CHECK (Status IN ('PendingApproval', 'Approved', 'Rejected', 'Completed'))
);
GO

CREATE NONCLUSTERED INDEX IX_RefundRequests_Status
    ON dbo.RefundRequests(Status, CreatedAt DESC);
GO

CREATE TABLE dbo.SimulatedInvoices (
    Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SimulatedInvoices PRIMARY KEY,
    BookingId       BIGINT               NOT NULL CONSTRAINT UQ_SimulatedInvoices_Booking UNIQUE,
    InvoiceNumber   VARCHAR(40)          NOT NULL CONSTRAINT UQ_SimulatedInvoices_Number UNIQUE,
    SerialNumber    VARCHAR(20)          NOT NULL CONSTRAINT DF_Invoices_Serial DEFAULT '1C26TVT',
    LookupCode      VARCHAR(40)          NOT NULL CONSTRAINT UQ_SimulatedInvoices_Lookup UNIQUE,
    BuyerType       VARCHAR(20)          NOT NULL CONSTRAINT DF_Invoices_BuyerType DEFAULT 'B2B',
    BuyerName       NVARCHAR(200)        NOT NULL,
    BuyerTaxCode    VARCHAR(20)          NULL,
    BuyerAddress    NVARCHAR(300)        NULL,
    BuyerEmail      NVARCHAR(150)        NULL,
    AmountBeforeTax DECIMAL(18,2)        NOT NULL,
    VatRate         DECIMAL(5,2)         NOT NULL,
    VatAmount       DECIMAL(18,2)        NOT NULL,
    TotalAmount     DECIMAL(18,2)        NOT NULL,
    Status          VARCHAR(20)          NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT 'Draft',
    IssuedByUserId  INT                  NULL,
    IssuedAt        DATETIME2(0)         NULL,
    CancelledAt     DATETIME2(0)         NULL,
    CancelReason    NVARCHAR(300)        NULL,
    CreatedAt       DATETIME2(0)         NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Invoices_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Invoices_IssuedBy FOREIGN KEY (IssuedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Invoices_BuyerType CHECK (BuyerType IN ('B2C', 'B2B')),
    CONSTRAINT CK_Invoices_Status CHECK (Status IN ('Draft', 'Issued', 'Cancelled')),
    CONSTRAINT CK_Invoices_Amounts CHECK (AmountBeforeTax >= 0 AND VatRate BETWEEN 0 AND 100 AND VatAmount >= 0 AND TotalAmount >= 0)
);
GO

CREATE TABLE dbo.SupplierPayments (
    Id                 BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplierPayments PRIMARY KEY,
    PaymentCode        VARCHAR(40)          NOT NULL CONSTRAINT UQ_SupplierPayments_Code UNIQUE,
    DepartureServiceId INT                  NOT NULL,
    SupplierId         INT                  NOT NULL,
    Amount             DECIMAL(18,2)        NOT NULL,
    Method             VARCHAR(30)          NOT NULL CONSTRAINT DF_SuppPay_Method DEFAULT 'BankTransfer',
    BankRef            VARCHAR(100)         NULL,
    EvidenceUrl        NVARCHAR(500)        NULL,
    PaidAt             DATETIME2(0)         NOT NULL,
    PaidByUserId       INT                  NOT NULL,
    Notes              NVARCHAR(300)        NULL,
    CreatedAt          DATETIME2(0)         NOT NULL CONSTRAINT DF_SuppPay_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SuppPay_Service FOREIGN KEY (DepartureServiceId) REFERENCES dbo.DepartureServices(Id),
    CONSTRAINT FK_SuppPay_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id),
    CONSTRAINT FK_SuppPay_PaidBy FOREIGN KEY (PaidByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_SuppPay_Amount CHECK (Amount > 0),
    CONSTRAINT CK_SuppPay_Method CHECK (Method IN ('BankTransfer', 'Cash'))
);
GO

CREATE NONCLUSTERED INDEX IX_SupplierPayments_Service
    ON dbo.SupplierPayments(DepartureServiceId, PaidAt DESC);
GO

CREATE TABLE dbo.FinancialLedgers (
    Id                   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FinancialLedgers PRIMARY KEY,
    EntryCode            VARCHAR(40)          NOT NULL CONSTRAINT UQ_FinancialLedgers_Code UNIQUE,
    Direction            VARCHAR(10)          NOT NULL,
    Category             VARCHAR(40)          NOT NULL,
    Amount               DECIMAL(18,2)        NOT NULL,
    DepartureId          INT                  NULL,
    BookingId            BIGINT               NULL,
    PaymentTransactionId BIGINT               NULL,
    RefundRequestId      BIGINT               NULL,
    SupplierPaymentId    BIGINT               NULL,
    ExtraExpenseId       BIGINT               NULL,
    Description          NVARCHAR(400)        NOT NULL,
    OccurredAt           DATETIME2(0)         NOT NULL,
    RecordedByUserId     INT                  NULL,
    CreatedAt            DATETIME2(0)         NOT NULL CONSTRAINT DF_Ledgers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Ledgers_Departure FOREIGN KEY (DepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_Ledgers_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Ledgers_PayTx FOREIGN KEY (PaymentTransactionId) REFERENCES dbo.PaymentTransactions(Id),
    CONSTRAINT FK_Ledgers_Refund FOREIGN KEY (RefundRequestId) REFERENCES dbo.RefundRequests(Id),
    CONSTRAINT FK_Ledgers_SuppPay FOREIGN KEY (SupplierPaymentId) REFERENCES dbo.SupplierPayments(Id),
    CONSTRAINT FK_Ledgers_ExtraExp FOREIGN KEY (ExtraExpenseId) REFERENCES dbo.DepartureExtraExpenses(Id),
    CONSTRAINT FK_Ledgers_RecordedBy FOREIGN KEY (RecordedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Ledgers_Direction CHECK (Direction IN ('In', 'Out')),
    CONSTRAINT CK_Ledgers_Category CHECK (
        Category IN ('CustomerPayment', 'CustomerRefund', 'SupplierPayment', 'ExtraExpense', 'OtherAdjustment')
    ),
    CONSTRAINT CK_Ledgers_Amount CHECK (Amount > 0)
);
GO

CREATE NONCLUSTERED INDEX IX_FinancialLedgers_Occurred
    ON dbo.FinancialLedgers(OccurredAt DESC, Direction, Category);
GO

CREATE NONCLUSTERED INDEX IX_FinancialLedgers_Departure
    ON dbo.FinancialLedgers(DepartureId, Direction)
    WHERE DepartureId IS NOT NULL;
GO

/* ============================================================================
   MODULE 7: INCIDENTS, REVIEWS & NOTIFICATIONS (3 TABLES)
   ============================================================================ */

CREATE TABLE dbo.TourIncidents (
    Id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TourIncidents PRIMARY KEY,
    DepartureId      INT                  NOT NULL,
    ReportedByUserId INT                  NOT NULL,
    Title            NVARCHAR(200)        NOT NULL,
    Severity         VARCHAR(20)          NOT NULL CONSTRAINT DF_Incidents_Severity DEFAULT 'Medium',
    Description      NVARCHAR(MAX)        NOT NULL,
    ResolutionNote   NVARCHAR(MAX)        NULL,
    Status           VARCHAR(20)          NOT NULL CONSTRAINT DF_Incidents_Status DEFAULT 'Open',
    OccurredAt       DATETIME2(0)         NOT NULL CONSTRAINT DF_Incidents_OccurredAt DEFAULT SYSUTCDATETIME(),
    ResolvedAt       DATETIME2(0)         NULL,
    CONSTRAINT FK_Incidents_Departure FOREIGN KEY (DepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_Incidents_Reporter FOREIGN KEY (ReportedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Incidents_Severity CHECK (Severity IN ('Low', 'Medium', 'High', 'Critical')),
    CONSTRAINT CK_Incidents_Status CHECK (Status IN ('Open', 'InProgress', 'Resolved'))
);
GO

CREATE NONCLUSTERED INDEX IX_TourIncidents_DepStatus
    ON dbo.TourIncidents(DepartureId, Status);
GO

CREATE TABLE dbo.TourReviews (
    Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TourReviews PRIMARY KEY,
    TourId          INT                  NOT NULL,
    DepartureId     INT                  NOT NULL,
    BookingId       BIGINT               NOT NULL CONSTRAINT UQ_TourReviews_Booking UNIQUE,
    CustomerId      INT                  NOT NULL,
    OverallRating   TINYINT              NOT NULL,
    GuideRating     TINYINT              NULL,
    ItineraryRating TINYINT              NULL,
    HotelFoodRating TINYINT              NULL,
    TransportRating TINYINT              NULL,
    Comment         NVARCHAR(1500)       NOT NULL,
    ImagesJson      NVARCHAR(MAX)        NULL,
    StaffReply      NVARCHAR(1000)       NULL,
    RepliedByUserId INT                  NULL,
    RepliedAt       DATETIME2(0)         NULL,
    IsHidden        BIT                  NOT NULL CONSTRAINT DF_Reviews_IsHidden DEFAULT 0,
    CreatedAt       DATETIME2(0)         NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Reviews_Tour FOREIGN KEY (TourId) REFERENCES dbo.Tours(Id),
    CONSTRAINT FK_Reviews_Departure FOREIGN KEY (DepartureId) REFERENCES dbo.TourDepartures(Id),
    CONSTRAINT FK_Reviews_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Reviews_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
    CONSTRAINT FK_Reviews_RepliedBy FOREIGN KEY (RepliedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Reviews_Ratings CHECK (
        OverallRating BETWEEN 1 AND 5
        AND (GuideRating IS NULL OR GuideRating BETWEEN 1 AND 5)
        AND (ItineraryRating IS NULL OR ItineraryRating BETWEEN 1 AND 5)
        AND (HotelFoodRating IS NULL OR HotelFoodRating BETWEEN 1 AND 5)
        AND (TransportRating IS NULL OR TransportRating BETWEEN 1 AND 5)
    ),
    CONSTRAINT CK_Reviews_ImagesJson CHECK (ImagesJson IS NULL OR ISJSON(ImagesJson) = 1)
);
GO

CREATE NONCLUSTERED INDEX IX_TourReviews_TourVisible
    ON dbo.TourReviews(TourId, IsHidden, CreatedAt DESC);
GO

CREATE TABLE dbo.Notifications (
    Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
    UserId     INT                  NULL,
    TargetRole VARCHAR(30)          NULL,
    Type       VARCHAR(50)          NOT NULL,
    Title      NVARCHAR(200)        NOT NULL,
    Body       NVARCHAR(800)        NOT NULL,
    LinkUrl    NVARCHAR(300)        NULL,
    IsRead     BIT                  NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
    ReadAt     DATETIME2(0)         NULL,
    CreatedAt  DATETIME2(0)         NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO

CREATE NONCLUSTERED INDEX IX_Notifications_UserUnread
    ON dbo.Notifications(UserId, IsRead, CreatedAt DESC)
    WHERE UserId IS NOT NULL;
GO
