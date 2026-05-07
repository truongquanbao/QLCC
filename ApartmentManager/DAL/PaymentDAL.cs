using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for payment workflow and related financial entities.
/// </summary>
public class PaymentDAL
{
    private const string PaymentSelectQuery = @"
        SELECT
            p.PaymentID,
            p.PaymentCode,
            p.InvoiceID,
            p.ApartmentID,
            p.ResidentID,
            p.PaymentAccountID,
            p.Amount,
            p.PaymentMethod,
            p.PaymentDate,
            p.TransactionCode,
            p.ProofImagePath,
            p.PaymentStatus,
            p.ConfirmedBy,
            p.ConfirmedAt,
            p.RejectedReason,
            p.CancelReason,
            p.Note,
            p.CreatedBy,
            p.CreatedAt,
            CONCAT(a.ApartmentCode, N'-', RIGHT(CONCAT(N'0', CAST(i.[Month] AS NVARCHAR(2))), 2), N'/', CAST(i.[Year] AS NVARCHAR(4))) AS InvoiceCode,
            a.ApartmentCode,
            COALESCE(r.FullName, createdUser.FullName, N'') AS ResidentName,
            pa.AccountName
        FROM dbo.Payments p
        INNER JOIN dbo.Invoices i ON p.InvoiceID = i.InvoiceID
        INNER JOIN dbo.Apartments a ON p.ApartmentID = a.ApartmentID
        LEFT JOIN dbo.Residents r ON p.ResidentID = r.ResidentID
        LEFT JOIN dbo.Users createdUser ON p.CreatedBy = createdUser.UserID
        LEFT JOIN dbo.PaymentAccounts pa ON p.PaymentAccountID = pa.PaymentAccountID";

    public static List<PaymentAccountDTO> GetActivePaymentAccounts()
    {
        var accounts = new List<PaymentAccountDTO>();

        try
        {
            const string query = @"
                SELECT PaymentAccountID, AccountName, AccountType, BankName, AccountNumber,
                       AccountHolder, QRImagePath, IsActive, CreatedAt
                FROM dbo.PaymentAccounts
                WHERE IsActive = 1
                ORDER BY AccountType, AccountName";

            using (var connection = DatabaseHelper.CreateConnection())
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        accounts.Add(MapPaymentAccount(reader));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting active payment accounts");
        }

        return accounts;
    }

    public static PaymentDTO? GetPaymentByID(int paymentID)
    {
        try
        {
            string query = PaymentSelectQuery + " WHERE p.PaymentID = @PaymentID";

            using (var connection = DatabaseHelper.CreateConnection())
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PaymentID", paymentID);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                        return MapPayment(reader);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting payment by ID: {PaymentID}", paymentID);
            return null;
        }
    }

    public static List<PaymentDTO> GetPaymentsByResident(int residentID)
    {
        var payments = new List<PaymentDTO>();

        try
        {
            string query = PaymentSelectQuery + " WHERE p.ResidentID = @ResidentID ORDER BY p.CreatedAt DESC";

            using (var connection = DatabaseHelper.CreateConnection())
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ResidentID", residentID);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        payments.Add(MapPayment(reader));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting payments by resident: {ResidentID}", residentID);
        }

        return payments;
    }

    public static List<PaymentDTO> GetPendingPayments()
    {
        var payments = new List<PaymentDTO>();

        try
        {
            string query = PaymentSelectQuery + " WHERE p.PaymentStatus = N'Pending' ORDER BY p.CreatedAt DESC";

            using (var connection = DatabaseHelper.CreateConnection())
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        payments.Add(MapPayment(reader));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting pending payments");
        }

        return payments;
    }

    public static int CreatePendingPayment(PaymentDTO payment)
    {
        try
        {
            const string query = @"
                INSERT INTO dbo.Payments
                (
                    PaymentCode, InvoiceID, ApartmentID, ResidentID, PaymentAccountID, Amount,
                    PaymentMethod, PaymentDate, TransactionCode, ProofImagePath, PaymentStatus,
                    Note, CreatedBy, CreatedAt
                )
                VALUES
                (
                    @PaymentCode, @InvoiceID, @ApartmentID, @ResidentID, @PaymentAccountID, @Amount,
                    @PaymentMethod, @PaymentDate, @TransactionCode, @ProofImagePath, N'Pending',
                    @Note, @CreatedBy, GETDATE()
                );
                SELECT SCOPE_IDENTITY();";

            using (var connection = DatabaseHelper.CreateConnection())
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PaymentCode", payment.PaymentCode ?? GeneratePaymentCode());
                command.Parameters.AddWithValue("@InvoiceID", payment.InvoiceID);
                command.Parameters.AddWithValue("@ApartmentID", payment.ApartmentID);
                command.Parameters.AddWithValue("@ResidentID", payment.ResidentID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PaymentAccountID", payment.PaymentAccountID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Amount", payment.Amount);
                command.Parameters.AddWithValue("@PaymentMethod", payment.PaymentMethod ?? "Other");
                command.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate == default ? DateTime.Now : payment.PaymentDate);
                command.Parameters.AddWithValue("@TransactionCode", string.IsNullOrWhiteSpace(payment.TransactionCode) ? (object)DBNull.Value : payment.TransactionCode);
                command.Parameters.AddWithValue("@ProofImagePath", string.IsNullOrWhiteSpace(payment.ProofImagePath) ? (object)DBNull.Value : payment.ProofImagePath);
                command.Parameters.AddWithValue("@Note", string.IsNullOrWhiteSpace(payment.Note) ? (object)DBNull.Value : payment.Note);
                command.Parameters.AddWithValue("@CreatedBy", payment.CreatedBy ?? (object)DBNull.Value);

                connection.Open();
                var result = command.ExecuteScalar();
                var paymentID = Convert.ToInt32(result);

                Log.Information("Pending payment created: {PaymentID} for invoice {InvoiceID}", paymentID, payment.InvoiceID);
                return paymentID;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating pending payment for invoice: {InvoiceID}", payment.InvoiceID);
            throw;
        }
    }

    public static bool ConfirmPayment(int paymentID, int confirmedBy, string? confirmationNote = null)
    {
        try
        {
            using (var connection = DatabaseHelper.CreateConnection())
            using (var command = new SqlCommand("dbo.usp_ConfirmPayment", connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@PaymentID", paymentID);
                command.Parameters.AddWithValue("@ConfirmedBy", confirmedBy);
                command.Parameters.AddWithValue("@ConfirmationNote", string.IsNullOrWhiteSpace(confirmationNote) ? (object)DBNull.Value : confirmationNote);

                connection.Open();
                command.ExecuteNonQuery();
                Log.Information("Payment confirmed via stored procedure: {PaymentID}", paymentID);
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error confirming payment: {PaymentID}", paymentID);
            return false;
        }
    }

    public static bool RejectPayment(int paymentID, int rejectedBy, string reason)
    {
        try
        {
            const string query = @"
                UPDATE dbo.Payments
                SET PaymentStatus = N'Rejected',
                    ConfirmedBy = @RejectedBy,
                    ConfirmedAt = GETDATE(),
                    RejectedReason = @RejectedReason
                WHERE PaymentID = @PaymentID
                  AND PaymentStatus = N'Pending'";

            using (var connection = DatabaseHelper.CreateConnection())
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PaymentID", paymentID);
                command.Parameters.AddWithValue("@RejectedBy", rejectedBy);
                command.Parameters.AddWithValue("@RejectedReason", reason);

                connection.Open();
                var affected = command.ExecuteNonQuery();
                return affected > 0;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error rejecting payment: {PaymentID}", paymentID);
            return false;
        }
    }

    public static bool CancelPayment(int paymentID, int cancelledBy, string reason)
    {
        try
        {
            const string query = @"
                UPDATE dbo.Payments
                SET PaymentStatus = N'Cancelled',
                    ConfirmedBy = @CancelledBy,
                    ConfirmedAt = GETDATE(),
                    CancelReason = @CancelReason
                WHERE PaymentID = @PaymentID
                  AND PaymentStatus = N'Pending'";

            using (var connection = DatabaseHelper.CreateConnection())
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PaymentID", paymentID);
                command.Parameters.AddWithValue("@CancelledBy", cancelledBy);
                command.Parameters.AddWithValue("@CancelReason", reason);

                connection.Open();
                var affected = command.ExecuteNonQuery();
                return affected > 0;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error cancelling payment: {PaymentID}", paymentID);
            return false;
        }
    }

    public static ReceiptDTO? GetReceiptByPaymentID(int paymentID)
    {
        try
        {
            const string query = @"
                SELECT ReceiptID, ReceiptCode, PaymentID, InvoiceID, ApartmentID,
                       PayerName, Amount, ReceiptDate, CreatedBy, Note
                FROM dbo.Receipts
                WHERE PaymentID = @PaymentID";

            using (var connection = DatabaseHelper.CreateConnection())
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PaymentID", paymentID);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ReceiptDTO
                        {
                            ReceiptID = reader.GetInt32(0),
                            ReceiptCode = reader.GetString(1),
                            PaymentID = reader.GetInt32(2),
                            InvoiceID = reader.GetInt32(3),
                            ApartmentID = reader.GetInt32(4),
                            PayerName = reader.GetString(5),
                            Amount = reader.GetDecimal(6),
                            ReceiptDate = reader.GetDateTime(7),
                            CreatedBy = reader.GetInt32(8),
                            Note = reader.IsDBNull(9) ? null : reader.GetString(9)
                        };
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting receipt by payment ID: {PaymentID}", paymentID);
            return null;
        }
    }

    private static PaymentAccountDTO MapPaymentAccount(SqlDataReader reader)
    {
        return new PaymentAccountDTO
        {
            PaymentAccountID = reader.GetInt32(0),
            AccountName = reader.GetString(1),
            AccountType = reader.GetString(2),
            BankName = reader.IsDBNull(3) ? null : reader.GetString(3),
            AccountNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
            AccountHolder = reader.IsDBNull(5) ? null : reader.GetString(5),
            QRImagePath = reader.IsDBNull(6) ? null : reader.GetString(6),
            IsActive = reader.GetBoolean(7),
            CreatedAt = reader.GetDateTime(8)
        };
    }

    private static PaymentDTO MapPayment(SqlDataReader reader)
    {
        return new PaymentDTO
        {
            PaymentID = reader.GetInt32(0),
            PaymentCode = reader.GetString(1),
            InvoiceID = reader.GetInt32(2),
            ApartmentID = reader.GetInt32(3),
            ResidentID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
            PaymentAccountID = reader.IsDBNull(5) ? null : reader.GetInt32(5),
            Amount = reader.GetDecimal(6),
            PaymentMethod = reader.GetString(7),
            PaymentDate = reader.GetDateTime(8),
            TransactionCode = reader.IsDBNull(9) ? null : reader.GetString(9),
            ProofImagePath = reader.IsDBNull(10) ? null : reader.GetString(10),
            PaymentStatus = reader.GetString(11),
            ConfirmedBy = reader.IsDBNull(12) ? null : reader.GetInt32(12),
            ConfirmedAt = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
            RejectedReason = reader.IsDBNull(14) ? null : reader.GetString(14),
            CancelReason = reader.IsDBNull(15) ? null : reader.GetString(15),
            Note = reader.IsDBNull(16) ? null : reader.GetString(16),
            CreatedBy = reader.IsDBNull(17) ? null : reader.GetInt32(17),
            CreatedAt = reader.GetDateTime(18),
            InvoiceCode = reader.IsDBNull(19) ? null : reader.GetString(19),
            ApartmentCode = reader.IsDBNull(20) ? null : reader.GetString(20),
            ResidentName = reader.IsDBNull(21) ? null : reader.GetString(21),
            AccountName = reader.IsDBNull(22) ? null : reader.GetString(22)
        };
    }

    private static string GeneratePaymentCode()
    {
        return $"PAY-{DateTime.Now:yyyyMMddHHmmssfff}";
    }
}
