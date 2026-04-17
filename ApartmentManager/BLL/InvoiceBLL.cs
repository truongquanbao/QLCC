using ApartmentManager.DAL;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.BLL;

/// <summary>
/// Business Logic Layer for Invoice management
/// </summary>
public class InvoiceBLL
{
    /// <summary>
    /// Get invoice by ID with validation
    /// </summary>
    public static dynamic? GetInvoiceByID(int invoiceID)
    {
        try
        {
            if (invoiceID <= 0)
            {
                Log.Warning("Invalid invoice ID: {InvoiceID}", invoiceID);
                return null;
            }

            return InvoiceDAL.GetInvoiceByID(invoiceID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting invoice: {InvoiceID}", invoiceID);
            return null;
        }
    }

    /// <summary>
    /// Get all unpaid invoices
    /// </summary>
    public static List<dynamic> GetUnpaidInvoices()
    {
        try
        {
            return InvoiceDAL.GetUnpaidInvoices();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting unpaid invoices");
            return new List<dynamic>();
        }
    }

    /// <summary>
    /// Create invoice with validation
    /// </summary>
    public static (bool Success, string Message, int InvoiceID) CreateInvoice(
        int apartmentID, int month, int year, DateTime dueDate, decimal totalAmount, string? note = null)
    {
        try
        {
            // Validation
            if (apartmentID <= 0)
                return (false, "Invalid apartment ID", 0);

            if (month < 1 || month > 12)
                return (false, "Month must be between 1 and 12", 0);

            if (year < 2000 || year > DateTime.Now.Year + 5)
                return (false, "Invalid year", 0);

            if (dueDate < DateTime.Now.Date)
                return (false, "Due date cannot be in the past", 0);

            if (totalAmount <= 0)
                return (false, "Total amount must be greater than 0", 0);

            // Check if invoice already exists for this apartment/month/year
            if (InvoiceDAL.InvoiceExists(apartmentID, month, year))
                return (false, "Invoice already exists for this apartment in the specified month/year", 0);

            int invoiceID = InvoiceDAL.CreateInvoice(apartmentID, month, year, dueDate, totalAmount, note);
            Log.Information("Invoice created via BLL: {InvoiceID}", invoiceID);

            return (true, "Invoice created successfully", invoiceID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error creating invoice");
            return (false, $"Error creating invoice: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Update invoice with validation
    /// </summary>
    public static (bool Success, string Message) UpdateInvoice(
        int invoiceID, DateTime dueDate, decimal totalAmount, string? note = null)
    {
        try
        {
            if (invoiceID <= 0)
                return (false, "Invalid invoice ID");

            if (dueDate < DateTime.Now.Date)
                return (false, "Due date cannot be in the past");

            if (totalAmount <= 0)
                return (false, "Total amount must be greater than 0");

            bool success = InvoiceDAL.UpdateInvoice(invoiceID, dueDate, totalAmount, note);

            if (success)
            {
                Log.Information("Invoice updated via BLL: {InvoiceID}", invoiceID);
                return (true, "Invoice updated successfully");
            }

            return (false, "Failed to update invoice");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error updating invoice: {InvoiceID}", invoiceID);
            return (false, $"Error updating invoice: {ex.Message}");
        }
    }

    /// <summary>
    /// Record payment with automatic status update
    /// </summary>
    public static (bool Success, string Message) RecordPayment(int invoiceID, decimal amountPaid)
    {
        try
        {
            if (invoiceID <= 0)
                return (false, "Invalid invoice ID");

            if (amountPaid <= 0)
                return (false, "Payment amount must be greater than 0");

            var invoice = InvoiceDAL.GetInvoiceByID(invoiceID);
            if (invoice == null)
                return (false, "Invoice not found");

            decimal newPaidAmount = invoice.PaidAmount + amountPaid;

            if (newPaidAmount > invoice.TotalAmount)
                return (false, $"Payment exceeds invoice total. Remaining: {invoice.TotalAmount - invoice.PaidAmount}");

            string newStatus = "Unpaid";
            if (newPaidAmount >= invoice.TotalAmount)
                newStatus = "Paid";
            else if (newPaidAmount > 0)
                newStatus = "Partial";

            bool success = InvoiceDAL.UpdatePaymentStatus(invoiceID, newStatus, newPaidAmount);

            if (success)
            {
                Log.Information("Payment recorded for invoice {InvoiceID}: {Amount}", invoiceID, amountPaid);
                return (true, $"Payment recorded successfully. Status: {newStatus}");
            }

            return (false, "Failed to record payment");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error recording payment for invoice: {InvoiceID}", invoiceID);
            return (false, $"Error recording payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Get invoices by apartment
    /// </summary>
    public static List<dynamic> GetInvoicesByApartment(int apartmentID)
    {
        try
        {
            if (apartmentID <= 0)
                return new List<dynamic>();

            return InvoiceDAL.GetInvoicesByApartment(apartmentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting invoices by apartment: {ApartmentID}", apartmentID);
            return new List<dynamic>();
        }
    }

    /// <summary>
    /// Get invoices by month
    /// </summary>
    public static List<dynamic> GetInvoicesByMonth(int month, int year)
    {
        try
        {
            if (month < 1 || month > 12 || year < 2000)
                return new List<dynamic>();

            return InvoiceDAL.GetInvoicesByMonth(month, year);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting invoices by month: {Month}/{Year}", month, year);
            return new List<dynamic>();
        }
    }

    /// <summary>
    /// Get debt summary for apartment
    /// </summary>
    public static dynamic? GetApartmentDebtSummary(int apartmentID)
    {
        try
        {
            if (apartmentID <= 0)
                return null;

            return InvoiceDAL.GetApartmentDebtSummary(apartmentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting debt summary for apartment: {ApartmentID}", apartmentID);
            return null;
        }
    }

    /// <summary>
    /// Get financial statistics
    /// </summary>
    public static dynamic GetFinancialStatistics()
    {
        try
        {
            var allInvoices = InvoiceDAL.GetUnpaidInvoices();
            var paidInvoices = InvoiceDAL.GetInvoicesByStatus("Paid");

            decimal totalOutstanding = allInvoices.Sum(i => (decimal)(i.TotalAmount - i.PaidAmount));
            decimal totalCollected = paidInvoices.Sum(i => (decimal)i.TotalAmount);

            var stats = new
            {
                TotalInvoices = allInvoices.Count + paidInvoices.Count,
                PaidInvoices = paidInvoices.Count,
                UnpaidInvoices = allInvoices.Count(i => i.PaymentStatus == "Unpaid"),
                PartialPayments = allInvoices.Count(i => i.PaymentStatus == "Partial"),
                TotalOutstanding = totalOutstanding.ToString("F2"),
                TotalCollected = totalCollected.ToString("F2"),
                CollectionRate = (allInvoices.Count + paidInvoices.Count) > 0 
                    ? ((paidInvoices.Count * 100.0) / (allInvoices.Count + paidInvoices.Count)).ToString("F2") + "%"
                    : "0%"
            };

            return stats;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting financial statistics");
            return null;
        }
    }

    /// <summary>
    /// Delete invoice
    /// </summary>
    public static (bool Success, string Message) DeleteInvoice(int invoiceID)
    {
        try
        {
            if (invoiceID <= 0)
                return (false, "Invalid invoice ID");

            var invoice = InvoiceDAL.GetInvoiceByID(invoiceID);
            if (invoice == null)
                return (false, "Invoice not found");

            // Don't allow deletion of paid invoices for audit purposes
            if (invoice.PaymentStatus == "Paid")
                return (false, "Cannot delete paid invoices for audit purposes");

            bool success = InvoiceDAL.DeleteInvoice(invoiceID);

            if (success)
            {
                Log.Information("Invoice deleted via BLL: {InvoiceID}", invoiceID);
                return (true, "Invoice deleted successfully");
            }

            return (false, "Failed to delete invoice");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error deleting invoice: {InvoiceID}", invoiceID);
            return (false, $"Error deleting invoice: {ex.Message}");
        }
    }

    /// <summary>
    /// Get overdue invoices
    /// </summary>
    public static List<dynamic> GetOverdueInvoices()
    {
        try
        {
            var unpaidInvoices = InvoiceDAL.GetUnpaidInvoices();
            return unpaidInvoices.Where(i => i.DueDate < DateTime.Now.Date).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting overdue invoices");
            return new List<dynamic>();
        }
    }
}
