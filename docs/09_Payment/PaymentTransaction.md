### 3. `09_Payment/PaymentTransaction.md`

```markdown
# Database Schema - PaymentTransaction (Smart SuperMarket)

Bảng `PaymentTransaction` lưu lịch sử chi tiết tất cả các lượt thanh toán phát sinh tại quầy POS.

## 1. SQL Script Khởi Tạo Bảng (SQL Server)

```sql
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
    CashierId VARCHAR(20) NOT NULL,
    CONSTRAINT FK_Payment_Invoice FOREIGN KEY (InvoiceId) REFERENCES Invoice(InvoiceId)
);

-- Tạo Index để tối ưu tốc độ tra cứu
CREATE INDEX IX_PaymentTransaction_InvoiceId ON PaymentTransaction(InvoiceId);
CREATE INDEX IX_PaymentTransaction_CreatedDate ON PaymentTransaction(CreatedDate);