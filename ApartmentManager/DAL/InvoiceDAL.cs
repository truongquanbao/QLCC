using Microsoft.Data.SqlClient;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Invoice operations
/// </summary>
public class InvoiceDAL
{
    /// <summary>
    /// Get invoice by ID
    /// </summary>
    public static InvoiceDTO? GetInvoiceByID(int invoiceID)
    {
        try
        {
            const string query = @"
                SELECT i.InvoiceID, i.ApartmentID, a.ApartmentCode, i.Month, i.Year,
                       i.DueDate, i.PaymentStatus, i.TotalAmount, i.PaidAmount,
                       i.CreatedAt, i.UpdatedAt, i.Note
                FROM Invoices i
                INNER JOIN Apartments a ON i.ApartmentID = a.ApartmentID
                WHERE i.InvoiceID = @InvoiceID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@InvoiceID", invoiceID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapInvoiceDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting invoice by ID: {InvoiceID}", invoiceID);
            return null;
        }
    }

    /// <summary>
    /// Get invoices by apartment
    /// </summary>
    public static List<InvoiceDTO> GetInvoicesByApartment(int apartmentID)
    {
        var invoices = new List<InvoiceDTO>();

        try
        {
            const string query = @"
                SELECT i.InvoiceID, i.ApartmentID, a.ApartmentCode, i.Month, i.Year,
                       i.DueDate, i.PaymentStatus, i.TotalAmount, i.PaidAmount,
                       i.CreatedAt, i.UpdatedAt, i.Note
                FROM Invoices i
                INNER JOIN Apartments a ON i.ApartmentID = a.ApartmentID
                WHERE i.ApartmentID = @ApartmentID
                ORDER BY i.Year DESC, i.Month DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            invoices.Add(MapInvoiceDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting invoices by apartment: {ApartmentID}", apartmentID);
        }

        return invoices;
    }

    /// <summary>
    /// Get invoices by status
    /// </summary>
    public static List<InvoiceDTO> GetInvoicesByStatus(string paymentStatus)
    {
        var invoices = new List<InvoiceDTO>();

        try
        {
            const string query = @"
                SELECT i.InvoiceID, i.ApartmentID, a.ApartmentCode, i.Month, i.Year,
                       i.DueDate, i.PaymentStatus, i.TotalAmount, i.PaidAmount,
                       i.CreatedAt, i.UpdatedAt, i.Note
                FROM Invoices i
                INNER JOIN Apartments a ON i.ApartmentID = a.ApartmentID
                WHERE i.PaymentStatus = @PaymentStatus
                ORDER BY i.DueDate ASC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PaymentStatus", paymentStatus);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            invoices.Add(MapInvoiceDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting invoices by status: {PaymentStatus}", paymentStatus);
        }

        return invoices;
    }

    /// <summary>
    /// Get unpaid invoices (overdue or not yet due)
    /// </summary>
    public static List<InvoiceDTO> GetUnpaidInvoices()
    {
        var invoices = new List<InvoiceDTO>();

        try
        {
            const string query = @"
                SELECT i.InvoiceID, i.ApartmentID, a.ApartmentCode, i.Month, i.Year,
                       i.DueDate, i.PaymentStatus, i.TotalAmount, i.PaidAmount,
                       i.CreatedAt, i.UpdatedAt, i.Note
                FROM Invoices i
                INNER JOIN Apartments a ON i.ApartmentID = a.ApartmentID
                WHERE i.PaymentStatus IN ('Unpaid', 'Partial')
                ORDER BY i.DueDate ASC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            invoices.Add(MapInvoiceDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting unpaid invoices");
        }

        return invoices;
    }

    /// <summary>
    /// Get invoices by month and year
    /// </summary>
    public static List<InvoiceDTO> GetInvoicesByMonth(int month, int year)
    {
        var invoices = new List<InvoiceDTO>();

        try
        {
            const string query = @"
                SELECT i.InvoiceID, i.ApartmentID, a.ApartmentCode, i.Month, i.Year,
                       i.DueDate, i.PaymentStatus, i.TotalAmount, i.PaidAmount,
                       i.CreatedAt, i.UpdatedAt, i.Note
                FROM Invoices i
                INNER JOIN Apartments a ON i.ApartmentID = a.ApartmentID
                WHERE i.Month = @Month AND i.Year = @Year
                ORDER BY a.ApartmentCode
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Month", month);
                    command.Parameters.AddWithValue("@Year", year);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            invoices.Add(MapInvoiceDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting invoices by month: {Month}/{Year}", month, year);
        }

        return invoices;
    }

    /// <summary>
    /// Get invoices by date range
    /// </summary>
    public static List<InvoiceDTO> GetInvoicesByDateRange(DateTime startDate, DateTime endDate)
    {
        var invoices = new List<InvoiceDTO>();

        try
        {
            const string query = @"
                SELECT i.InvoiceID, i.ApartmentID, a.ApartmentCode, i.Month, i.Year,
                       i.DueDate, i.PaymentStatus, i.TotalAmount, i.PaidAmount,
                       i.CreatedAt, i.UpdatedAt, i.Note
                FROM Invoices i
                INNER JOIN Apartments a ON i.ApartmentID = a.ApartmentID
                WHERE i.CreatedAt >= @StartDate AND i.CreatedAt <= @EndDate
                ORDER BY i.CreatedAt DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            invoices.Add(MapInvoiceDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting invoices by date range");
        }

        return invoices;
    }

    /// <summary>
    /// Create invoice
    /// </summary>
    public static int CreateInvoice(int apartmentID, int month, int year, DateTime dueDate, decimal totalAmount, string? note = null)
    {
        try
        {
            const string query = @"
                INSERT INTO Invoices (ApartmentID, Month, Year, DueDate, PaymentStatus, TotalAmount, PaidAmount, Note)
                VALUES (@ApartmentID, @Month, @Year, @DueDate, 'Unpaid', @TotalAmount, 0, @Note)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    command.Parameters.AddWithValue("@Month", month);
                    command.Parameters.AddWithValue("@Year", year);
                    command.Parameters.AddWithValue("@DueDate", dueDate);
                    command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var invoiceID = Convert.ToInt32(result);

                    Log.Information("Invoice created: {Month}/{Year} for apartment {ApartmentID} (ID: {InvoiceID})", month, year, apartmentID, invoiceID);
                    return invoiceID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating invoice for apartment: {ApartmentID}", apartmentID);
            throw;
        }
    }

    /// <summary>
    /// Update invoice
    /// </summary>
    public static bool UpdateInvoice(int invoiceID, DateTime dueDate, decimal totalAmount, string? note = null)
    {
        try
        {
            const string query = @"
                UPDATE Invoices
                SET DueDate = @DueDate, TotalAmount = @TotalAmount, Note = @Note, UpdatedAt = GETDATE()
                WHERE InvoiceID = @InvoiceID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@InvoiceID", invoiceID);
                    command.Parameters.AddWithValue("@DueDate", dueDate);
                    command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Invoice updated: {InvoiceID}", invoiceID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating invoice: {InvoiceID}", invoiceID);
            return false;
        }
    }

    /// <summary>
    /// Update invoice payment status and paid amount
    /// </summary>
    public static bool UpdatePaymentStatus(int invoiceID, string paymentStatus, decimal paidAmount)
    {
        try
        {
            const string query = @"
                UPDATE Invoices
                SET PaymentStatus = @PaymentStatus, PaidAmount = @PaidAmount, UpdatedAt = GETDATE()
                WHERE InvoiceID = @InvoiceID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@InvoiceID", invoiceID);
                    command.Parameters.AddWithValue("@PaymentStatus", paymentStatus);
                    command.Parameters.AddWithValue("@PaidAmount", paidAmount);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Invoice payment status updated: {InvoiceID} to {PaymentStatus}", invoiceID, paymentStatus);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating invoice payment status: {InvoiceID}", invoiceID);
            return false;
        }
    }

    /// <summary>
    /// Delete invoice
    /// </summary>
    public static bool DeleteInvoice(int invoiceID)
    {
        try
        {
            const string query = "DELETE FROM Invoices WHERE InvoiceID = @InvoiceID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@InvoiceID", invoiceID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Invoice deleted: {InvoiceID}", invoiceID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting invoice: {InvoiceID}", invoiceID);
            return false;
        }
    }

    /// <summary>
    /// Check if invoice exists for apartment in month/year
    /// </summary>
    public static bool InvoiceExists(int apartmentID, int month, int year)
    {
        try
        {
            const string query = "SELECT COUNT(*) FROM Invoices WHERE ApartmentID = @ApartmentID AND Month = @Month AND Year = @Year";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    command.Parameters.AddWithValue("@Month", month);
                    command.Parameters.AddWithValue("@Year", year);

                    connection.Open();
                    return (int)command.ExecuteScalar()! > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if invoice exists: {ApartmentID} {Month}/{Year}", apartmentID, month, year);
            return true;
        }
    }

    /// <summary>
    /// Get debt summary for apartment
    /// </summary>
    public static dynamic? GetApartmentDebtSummary(int apartmentID)
    {
        try
        {
            const string query = @"
                SELECT 
                    COUNT(CASE WHEN PaymentStatus IN ('Unpaid', 'Partial') THEN 1 END) as UnpaidInvoiceCount,
                    SUM(CASE WHEN PaymentStatus = 'Unpaid' THEN TotalAmount ELSE 0 END) as TotalUnpaidAmount,
                    SUM(CASE WHEN PaymentStatus = 'Partial' THEN (TotalAmount - PaidAmount) ELSE 0 END) as TotalPartialAmount,
                    SUM(TotalAmount) as TotalInvoiceAmount,
                    SUM(PaidAmount) as TotalPaidAmount
                FROM Invoices
                WHERE ApartmentID = @ApartmentID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                UnpaidInvoiceCount = reader.GetInt32(0),
                                TotalUnpaidAmount = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1),
                                TotalPartialAmount = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                                TotalInvoiceAmount = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3),
                                TotalPaidAmount = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4)
                            };
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting debt summary for apartment: {ApartmentID}", apartmentID);
            return null;
        }
    }

    /// <summary>
    /// Map SqlDataReader to InvoiceDTO
    /// </summary>
    private static InvoiceDTO MapInvoiceDTO(SqlDataReader reader)
    {
        return new InvoiceDTO
        {
            InvoiceID = reader.GetInt32(0),
            ApartmentID = reader.GetInt32(1),
            ApartmentCode = reader.GetString(2),
            Month = reader.GetInt32(3),
            Year = reader.GetInt32(4),
            DueDate = reader.GetDateTime(5),
            PaymentStatus = reader.GetString(6),
            TotalAmount = reader.GetDecimal(7),
            PaidAmount = reader.GetDecimal(8),
            CreatedAt = reader.GetDateTime(9),
            UpdatedAt = reader.GetDateTime(10),
            Note = reader.IsDBNull(11) ? null : reader.GetString(11)
        };
    }
}
