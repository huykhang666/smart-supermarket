using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Desktop.Models;

namespace Desktop.Services;

/// <summary>
/// Service quản lý giao dịch thanh toán sử dụng ADO.NET (SqlConnection, SqlCommand)
/// </summary>
public class PaymentService
{
    private static readonly Random _random = new();

    /// <summary>
    /// Danh sách các chuỗi kết nối dự phòng để thử lần lượt (Localhost, SQLEXPRESS, IP)
    /// </summary>
    private static readonly string[] PossibleConnectionStrings = new[]
    {
        "Server=localhost;Database=AISupermarketDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;",
        "Server=.\\SQLEXPRESS;Database=AISupermarketDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;",
        "Server=127.0.0.1;Database=AISupermarketDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;",
        "Server=localhost,1433;Database=AISupermarketDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;"
    };

    /// <summary>
    /// Chuỗi kết nối CSDL SQL Server hiện tại
    /// </summary>
    public string ConnectionString { get; set; } = PossibleConnectionStrings[0];

    public SqlConnection GetOpenConnection()
    {
        // Thử kết nối với ConnectionString hiện tại trước
        try
        {
            var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }
        catch
        {
            // Nếu thất bại, thử các instance khác (như .\SQLEXPRESS)
            foreach (var connStr in PossibleConnectionStrings)
            {
                if (connStr == ConnectionString) continue;
                try
                {
                    var fallbackConn = new SqlConnection(connStr);
                    fallbackConn.Open();
                    ConnectionString = connStr; // Lưu lại chuỗi kết nối hoạt động thành công
                    return fallbackConn;
                }
                catch
                {
                    // Tiếp tục thử cái kế tiếp
                }
            }
            throw; // Nếu tất cả đều fail, ném exception gốc
        }
    }

    public PaymentService()
    {
    }

    public PaymentService(string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            ConnectionString = connectionString;
        }
    }

    /// <summary>
    /// Sinh mã giao dịch theo định dạng: PAY-YYYYMMDD-HHMMSS-XXX
    /// </summary>
    public static string GenerateTransactionCode()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        int randomSuffix = _random.Next(100, 1000); // 3 chữ số ngẫu nhiên
        return $"PAY-{timestamp}-{randomSuffix}";
    }

    /// <summary>
    /// Lưu giao dịch ban đầu với trạng thái PENDING vào CSDL
    /// </summary>
    public bool CreateTransaction(PaymentTransactionModel transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));

        if (string.IsNullOrWhiteSpace(transaction.TransactionCode))
        {
            transaction.TransactionCode = GenerateTransactionCode();
        }

        const string sql = @"
            INSERT INTO PaymentTransaction (
                TransactionCode, InvoiceId, PaymentMethod, Amount,
                GivenAmount, ChangeAmount, TransferContent, BankCode,
                Status, CreatedDate, CompletedDate, CashierId
            )
            VALUES (
                @TransactionCode, @InvoiceId, @PaymentMethod, @Amount,
                @GivenAmount, @ChangeAmount, @TransferContent, @BankCode,
                @Status, @CreatedDate, @CompletedDate, @CashierId
            );
            SELECT SCOPE_IDENTITY();";

        try
        {
            using (var conn = GetOpenConnection())
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@TransactionCode", SqlDbType.VarChar, 100).Value = transaction.TransactionCode;
                    cmd.Parameters.Add("@InvoiceId", SqlDbType.VarChar, 20).Value = transaction.InvoiceId;
                    cmd.Parameters.Add("@PaymentMethod", SqlDbType.TinyInt).Value = (byte)transaction.PaymentMethod;
                    cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = transaction.Amount;
                    cmd.Parameters.Add("@GivenAmount", SqlDbType.Decimal).Value = (object?)transaction.GivenAmount ?? DBNull.Value;
                    cmd.Parameters.Add("@ChangeAmount", SqlDbType.Decimal).Value = (object?)transaction.ChangeAmount ?? DBNull.Value;
                    cmd.Parameters.Add("@TransferContent", SqlDbType.VarChar, 100).Value = (object?)transaction.TransferContent ?? DBNull.Value;
                    cmd.Parameters.Add("@BankCode", SqlDbType.VarChar, 20).Value = (object?)transaction.BankCode ?? DBNull.Value;
                    cmd.Parameters.Add("@Status", SqlDbType.Int).Value = (int)transaction.Status;
                    cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = transaction.CreatedDate;
                    cmd.Parameters.Add("@CompletedDate", SqlDbType.DateTime).Value = (object?)transaction.CompletedDate ?? DBNull.Value;
                    cmd.Parameters.Add("@CashierId", SqlDbType.VarChar, 20).Value = transaction.CashierId;

                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        transaction.TransactionId = Convert.ToInt32(result);
                    }
                }
                conn.Close();
            }
            return true;
        }
        catch (Exception ex)
        {
            string errorDetails = $"[PaymentService] CreateTransaction Error: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorDetails += $" | InnerException: {ex.InnerException.Message}";
            }
            Console.WriteLine(errorDetails);
            System.Diagnostics.Debug.WriteLine(errorDetails);
            return false;
        }
    }

    /// <summary>
    /// Cập nhật trạng thái giao dịch (SUCCESS hoặc CANCELLED) theo TransactionCode
    /// </summary>
    public bool UpdateStatus(string transactionCode, PaymentStatus status)
    {
        if (string.IsNullOrWhiteSpace(transactionCode)) return false;

        const string sql = @"
            UPDATE PaymentTransaction
            SET Status = @Status,
                CompletedDate = CASE WHEN @Status = 1 THEN GETDATE() ELSE CompletedDate END
            WHERE TransactionCode = @TransactionCode;";

        try
        {
            using (var conn = GetOpenConnection())
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@Status", SqlDbType.Int).Value = (int)status;
                    cmd.Parameters.Add("@TransactionCode", SqlDbType.VarChar, 100).Value = transactionCode;

                    int rows = cmd.ExecuteNonQuery();
                    conn.Close();
                    return rows > 0;
                }
            }
        }
        catch (Exception ex)
        {
            string errorDetails = $"[PaymentService] UpdateStatus Error: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorDetails += $" | InnerException: {ex.InnerException.Message}";
            }
            Console.WriteLine(errorDetails);
            System.Diagnostics.Debug.WriteLine(errorDetails);
            return false;
        }
    }

    /// <summary>
    /// Cập nhật trạng thái giao dịch theo TransactionId
    /// </summary>
    public bool UpdateStatusById(int transactionId, PaymentStatus status)
    {
        if (transactionId <= 0) return false;

        const string sql = @"
            UPDATE PaymentTransaction
            SET Status = @Status,
                CompletedDate = CASE WHEN @Status = 1 THEN GETDATE() ELSE CompletedDate END
            WHERE TransactionId = @TransactionId;";

        try
        {
            using (var conn = GetOpenConnection())
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@Status", SqlDbType.Int).Value = (int)status;
                    cmd.Parameters.Add("@TransactionId", SqlDbType.Int).Value = transactionId;

                    int rows = cmd.ExecuteNonQuery();
                    conn.Close();
                    return rows > 0;
                }
            }
        }
        catch (Exception ex)
        {
            string errorDetails = $"[PaymentService] UpdateStatusById Error: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorDetails += $" | InnerException: {ex.InnerException.Message}";
            }
            Console.WriteLine(errorDetails);
            System.Diagnostics.Debug.WriteLine(errorDetails);
            return false;
        }
    }

    /// <summary>
    /// Xử lý thanh toán tiền mặt (tính tiền thừa, lưu giao dịch SUCCESS)
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn</param>
    /// <param name="amount">Số tiền hóa đơn cần thanh toán</param>
    /// <param name="givenAmount">Số tiền khách đưa</param>
    /// <param name="cashierId">Mã thu ngân phụ trách</param>
    /// <returns>PaymentResponseDto chứa kết quả, tiền thừa và mã giao dịch</returns>
    public PaymentResponseDto ProcessCashPayment(string invoiceId, decimal amount, decimal givenAmount, string cashierId)
    {
        var response = new PaymentResponseDto();

        if (givenAmount < amount)
        {
            response.IsSuccess = false;
            response.Message = $"Số tiền khách đưa ({givenAmount:N0} đ) chưa đủ để thanh toán ({amount:N0} đ). Còn thiếu: {(amount - givenAmount):N0} đ.";
            return response;
        }

        decimal changeAmount = givenAmount - amount;
        string transactionCode = GenerateTransactionCode();

        var transaction = new PaymentTransactionModel
        {
            TransactionCode = transactionCode,
            InvoiceId = invoiceId,
            PaymentMethod = PaymentMethod.Cash,
            Amount = amount,
            GivenAmount = givenAmount,
            ChangeAmount = changeAmount,
            TransferContent = $"Thanh toan tien mat {invoiceId}",
            BankCode = null,
            Status = PaymentStatus.Success,
            CreatedDate = DateTime.Now,
            CompletedDate = DateTime.Now,
            CashierId = cashierId
        };

        bool saved = CreateTransaction(transaction);

        response.IsSuccess = saved;
        response.TransactionCode = transactionCode;
        response.ChangeAmount = changeAmount;
        response.Message = saved
            ? $"Thanh toán tiền mặt thành công. Tiền thừa trả khách: {changeAmount:N0} đ."
            : "Lỗi khi lưu giao dịch tiền mặt vào CSDL.";

        return response;
    }
}
