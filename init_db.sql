/* ==========================================================================
   FIX LỖI 1934: Bật các tùy chọn bắt buộc cho Computed Columns
   ========================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ==========================================================================
   PHẦN 1: KHỞI TẠO DATABASE & CÁC BẢNG CƠ BẢN (CORE)
   ========================================================================== */
USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'QuantIQ_DB')
BEGIN
    CREATE DATABASE QuantIQ_DB;
END
GO

USE QuantIQ_DB;
GO

-- Set lại lần nữa trong context của DB mới cho chắc chắn
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Bảng Users (Bắt buộc có trước để các bảng khác tham chiếu)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
CREATE TABLE Users (
    UserID VARCHAR(50) NOT NULL,       -- PK: GUID hoặc String
    Username NVARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(MAX) NOT NULL,
    Email VARCHAR(200),
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_Users PRIMARY KEY (UserID)
);

-- 2. Bảng Symbols (Danh mục mã chứng khoán)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Symbols')
CREATE TABLE Symbols (
    Symbol VARCHAR(10) NOT NULL,       -- PK: FPT, VIC
    CompanyName NVARCHAR(255),
    Exchange VARCHAR(10),              -- HOSE, HNX
    CONSTRAINT PK_Symbols PRIMARY KEY (Symbol)
);

-- 3. Bảng Candles (Dữ liệu giá - Quan trọng nhất)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Candles')
CREATE TABLE Candles (
    Symbol VARCHAR(10) NOT NULL,       
    Timestamp BIGINT NOT NULL,         -- Unix Timestamp
    [Open] DECIMAL(18, 4),
    [High] DECIMAL(18, 4),
    [Low] DECIMAL(18, 4),
    [Close] DECIMAL(18, 4),
    Volume BIGINT,
    
    CONSTRAINT PK_Candles PRIMARY KEY (Symbol, Timestamp),
    CONSTRAINT FK_Candles_Symbols FOREIGN KEY (Symbol) REFERENCES Symbols(Symbol)
);

-- Index tối ưu cho biểu đồ
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Candles_Symbol_Time')
CREATE NONCLUSTERED INDEX IX_Candles_Symbol_Time ON Candles(Symbol, Timestamp);

/* ==========================================================================
   PHẦN 2: MODULE NÂNG CAO (ORDER, WALLET, TRANSACTION - REVISED)
   ========================================================================== */

-- 4. Bảng Orders (Quản lý lệnh)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
CREATE TABLE Orders (
    OrderID VARCHAR(50) PRIMARY KEY,
    UserID VARCHAR(50) NOT NULL,
    Symbol VARCHAR(10) NOT NULL,
    Side VARCHAR(4) NOT NULL, -- 'BUY', 'SELL'
    OrderType VARCHAR(10) NOT NULL, -- 'LO', 'MP'
    Status VARCHAR(20) DEFAULT 'PENDING',
    RequestQty INT NOT NULL,
    Price DECIMAL(18, 4) NOT NULL,
    MatchedQty INT DEFAULT 0,
    AvgMatchedPrice DECIMAL(18, 4) DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Orders_Symbols FOREIGN KEY (Symbol) REFERENCES Symbols(Symbol)
);

-- 5. Bảng CashWallets (Ví tiền & Concurrency)
-- Lỗi 1934 thường xảy ra ở đây do cột AvailableBalance
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CashWallets')
CREATE TABLE CashWallets (
    WalletID INT PRIMARY KEY IDENTITY(1,1),
    UserID VARCHAR(50) NOT NULL UNIQUE,
    Balance DECIMAL(18, 4) DEFAULT 0,
    LockedAmount DECIMAL(18, 4) DEFAULT 0,
    AvailableBalance AS (Balance - LockedAmount) PERSISTED, 
    LastUpdated DATETIME DEFAULT GETDATE(),
    RowVersion TIMESTAMP, 
    
    CONSTRAINT FK_CashWallets_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- 6. Bảng Transactions (Audit Trail)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Transactions')
CREATE TABLE Transactions (
    TransID BIGINT PRIMARY KEY IDENTITY(1,1),
    RefID VARCHAR(50) NOT NULL, 
    UserID VARCHAR(50) NOT NULL,
    TransType VARCHAR(20) NOT NULL, 
    Amount DECIMAL(18, 4) NOT NULL,
    BalanceBefore DECIMAL(18, 4) NOT NULL,
    BalanceAfter DECIMAL(18, 4) NOT NULL,
    Description NVARCHAR(500),
    TransTime DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Transactions_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- 7. Bảng Portfolios (Danh mục đầu tư)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Portfolios')
CREATE TABLE Portfolios (
    UserID VARCHAR(50) NOT NULL,
    Symbol VARCHAR(10) NOT NULL,
    TotalQuantity INT DEFAULT 0,
    LockedQuantity INT DEFAULT 0,
    AvailableQuantity AS (TotalQuantity - LockedQuantity) PERSISTED,
    AvgCostPrice DECIMAL(18, 4) DEFAULT 0,
    RowVersion TIMESTAMP,

    CONSTRAINT PK_Portfolios PRIMARY KEY (UserID, Symbol),
    CONSTRAINT FK_Portfolios_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Portfolios_Symbols FOREIGN KEY (Symbol) REFERENCES Symbols(Symbol)
);

-- 8. Bảng MarginRatios (Cấu hình Margin)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MarginRatios')
CREATE TABLE MarginRatios (
    RatioID INT PRIMARY KEY IDENTITY(1,1),
    Symbol VARCHAR(10) NOT NULL,
    InitialRate DECIMAL(5, 2) NOT NULL,
    MaintenanceRate DECIMAL(5, 2) NOT NULL,
    EffectiveDate DATETIME NOT NULL,
    ExpiredDate DATETIME NULL,

    CONSTRAINT FK_Margin_Symbols FOREIGN KEY (Symbol) REFERENCES Symbols(Symbol)
);

/* ==========================================================================
   PHẦN 3: MODULE CORPORATE ACTIONS (QUYỀN & CỔ TỨC)
   ========================================================================== */

-- 9. Bảng CorporateActions (Lịch sự kiện Quyền & Cổ tức)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CorporateActions')
CREATE TABLE CorporateActions (
    ActionID INT PRIMARY KEY IDENTITY(1,1),
    Symbol VARCHAR(10) NOT NULL,

    -- Loại sự kiện: 'CASH_DIVIDEND' | 'STOCK_DIVIDEND' | 'BONUS_SHARE'
    ActionType VARCHAR(20) NOT NULL,

    -- Ngày đăng ký cuối cùng (Record Date / GDKHQ):
    -- Ai giữ cổ phiếu TRƯỚC ngày này sẽ được nhận quyền.
    RecordDate DATE NOT NULL,

    -- Ngày thực trả cổ tức (Payment Date):
    -- Ngày Worker chạy và ghi có vào tài khoản.
    PaymentDate DATE NOT NULL,

    -- Tỷ lệ chi trả:
    -- CASH_DIVIDEND : số VNĐ trên 1 cổ phiếu (vd: 1000 = 1.000đ/cp)
    -- STOCK_DIVIDEND / BONUS_SHARE : tỷ lệ cổ phiếu thưởng (vd: 0.10 = 10%)
    Ratio DECIMAL(18, 4) NOT NULL,

    -- Trạng thái: 'PENDING' | 'PROCESSED' | 'CANCELLED'
    Status VARCHAR(20) NOT NULL DEFAULT 'PENDING',

    ProcessedAt DATETIME NULL,
    Note NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_CorporateActions_Symbols FOREIGN KEY (Symbol) REFERENCES Symbols(Symbol)
);

-- Index để Worker tra cứu nhanh theo PaymentDate + Status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CorporateActions_PaymentDate_Status')
CREATE NONCLUSTERED INDEX IX_CorporateActions_PaymentDate_Status
    ON CorporateActions(PaymentDate, Status);
GO