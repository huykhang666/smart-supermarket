-- =======================================================
-- Database Schema: PaymentTransaction (Smart SuperMarket)
-- Hệ Quản Trị CSDL: Microsoft SQL Server (T-SQL)
-- Mô tả: Lưu trữ thông tin chi tiết lịch sử giao dịch thanh toán
-- =======================================================

-- 1. Tạo bảng PaymentTransaction
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransaction]') AND type in (N'U'))
BEGIN
    CREATE TABLE PaymentTransaction (
        TransactionId INT IDENTITY(1,1) PRIMARY KEY,
        TransactionCode VARCHAR(100) NOT NULL UNIQUE, -- Mã GD nội bộ (VD: PAY-20261020-001)
        InvoiceId VARCHAR(20) NOT NULL,                -- Mã hóa đơn (VD: SSM10293)
        PaymentMethod TINYINT NOT NULL,               -- 1: Cash, 2: VietQR, 3: Card
        Amount DECIMAL(18,2) NOT NULL,                -- Số tiền hóa đơn
        GivenAmount DECIMAL(18,2) NULL,               -- Số tiền khách đưa (Dành cho Tiền mặt)
        ChangeAmount DECIMAL(18,2) NULL,              -- Số tiền thừa trả lại khách
        TransferContent VARCHAR(100) NULL,            -- Nội dung CK (Ví dụ: SSM10293)
        BankCode VARCHAR(20) NULL,                    -- Mã ngân hàng nhận (MB, VCB, ICB...)
        Status INT DEFAULT 0,                         -- 0: Pending, 1: Success, 2: Failed, 3: Cancelled
        CreatedDate DATETIME DEFAULT GETDATE(),
        CompletedDate DATETIME NULL,
        CashierId VARCHAR(20) NOT NULL
    );
END
GO

-- 2. Tạo Index tối ưu tốc độ tra cứu
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PaymentTransaction_InvoiceId' AND object_id = OBJECT_ID(N'[dbo].[PaymentTransaction]'))
BEGIN
    CREATE INDEX IX_PaymentTransaction_InvoiceId ON PaymentTransaction(InvoiceId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PaymentTransaction_CreatedDate' AND object_id = OBJECT_ID(N'[dbo].[PaymentTransaction]'))
BEGIN
    CREATE INDEX IX_PaymentTransaction_CreatedDate ON PaymentTransaction(CreatedDate);
END
GO
