using System;
using System.Collections.Generic;
using System.Linq;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
using Serilog;

namespace ApartmentManager.BLL;

/// <summary>
/// Business Logic Layer for resident payment submission and manager confirmation.
/// </summary>
public class PaymentBLL
{
    public static List<PaymentAccountDTO> GetActivePaymentAccounts()
    {
        return PaymentDAL.GetActivePaymentAccounts();
    }

    public static List<PaymentDTO> GetMyPaymentHistory(int residentUserID)
    {
        try
        {
            var resident = ResidentDAL.GetResidentByUserID(residentUserID);
            if (resident == null)
                return new List<PaymentDTO>();

            return PaymentDAL.GetPaymentsByResident(resident.ResidentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting payment history for resident user: {ResidentUserID}", residentUserID);
            return new List<PaymentDTO>();
        }
    }

    public static List<PaymentDTO> GetPendingPayments()
    {
        return PaymentDAL.GetPendingPayments();
    }

    public static PaymentDTO? GetPaymentByID(int paymentID)
    {
        if (paymentID <= 0)
            return null;

        return PaymentDAL.GetPaymentByID(paymentID);
    }

    public static (bool Success, string Message, int PaymentID) SubmitPayment(
        int residentUserID,
        int invoiceID,
        decimal amount,
        string paymentMethod,
        int? paymentAccountID,
        string? transactionCode,
        string? proofImagePath,
        string? note = null)
    {
        try
        {
            if (invoiceID <= 0)
                return (false, "Invalid invoice ID.", 0);

            if (amount <= 0)
                return (false, "Payment amount must be greater than 0.", 0);

            if (string.IsNullOrWhiteSpace(paymentMethod))
                return (false, "Payment method is required.", 0);

            var resident = ResidentDAL.GetResidentByUserID(residentUserID);
            if (resident == null)
                return (false, "Resident profile was not found.", 0);

            var invoice = InvoiceDAL.GetInvoiceByID(invoiceID);
            if (invoice == null)
                return (false, "Invoice was not found.", 0);

            if (resident.ApartmentID != invoice.ApartmentID)
                return (false, "Residents can only pay invoices belonging to their own apartment.", 0);

            if (string.Equals(invoice.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(invoice.PaymentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "This invoice is no longer available for payment.", 0);
            }

            var remainingAmount = invoice.RemainingAmount > 0
                ? invoice.RemainingAmount
                : Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount);

            if (remainingAmount <= 0)
                return (false, "This invoice has no remaining balance.", 0);

            if (amount > remainingAmount)
                return (false, $"Payment amount exceeds remaining balance: {remainingAmount:N0} VND.", 0);

            if (RequiresTransferEvidence(paymentMethod) &&
                string.IsNullOrWhiteSpace(transactionCode) &&
                string.IsNullOrWhiteSpace(proofImagePath))
            {
                return (false, "Bank transfer, QR, and e-wallet payments require a transaction code or proof image.", 0);
            }

            var payment = new PaymentDTO
            {
                InvoiceID = invoice.InvoiceID,
                ApartmentID = invoice.ApartmentID,
                ResidentID = resident.ResidentID,
                PaymentAccountID = paymentAccountID,
                Amount = amount,
                PaymentMethod = paymentMethod,
                PaymentDate = DateTime.Now,
                TransactionCode = transactionCode,
                ProofImagePath = proofImagePath,
                Note = note,
                CreatedBy = residentUserID
            };

            int paymentID = PaymentDAL.CreatePendingPayment(payment);

            NotifyFinanceUsers(
                "Có thanh toán mới chờ xác nhận",
                $"Thanh toán mới cho hóa đơn {invoice.InvoiceID} từ căn hộ {invoice.ApartmentCode}, số tiền {amount:N0} VND đang chờ xác nhận.",
                "Payment");

            AuditLogDAL.LogAction(
                residentUserID,
                "Payment_Submit",
                "Payment",
                paymentID,
                $"Resident submitted payment for invoice {invoice.InvoiceID}.");

            return (true, "Payment submitted successfully and is pending confirmation.", paymentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error submitting payment for invoice: {InvoiceID}", invoiceID);
            return (false, $"Error submitting payment: {ex.Message}", 0);
        }
    }

    public static (bool Success, string Message) ConfirmPayment(int paymentID, int confirmedByUserID, string? note = null)
    {
        try
        {
            var payment = PaymentDAL.GetPaymentByID(paymentID);
            if (payment == null)
                return (false, "Payment not found.");

            if (!string.Equals(payment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                return (false, "Only pending payments can be confirmed.");

            var invoice = InvoiceDAL.GetInvoiceByID(payment.InvoiceID);
            if (invoice == null)
                return (false, "Invoice not found.");

            var remainingAmount = invoice.RemainingAmount > 0
                ? invoice.RemainingAmount
                : Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount);

            if (payment.Amount > remainingAmount)
                return (false, "Payment amount exceeds the invoice remaining amount.");

            bool success = PaymentDAL.ConfirmPayment(paymentID, confirmedByUserID, note);
            if (!success)
                return (false, "Failed to confirm payment.");

            NotifyResident(payment.ResidentID, "Thanh toán đã được xác nhận",
                $"Khoản thanh toán {payment.PaymentCode} đã được xác nhận thành công.", "Payment");

            AuditLogDAL.LogAction(
                confirmedByUserID,
                "Payment_Confirm",
                "Payment",
                paymentID,
                $"Confirmed payment {payment.PaymentCode} for invoice {payment.InvoiceID}.");

            return (true, "Payment confirmed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error confirming payment: {PaymentID}", paymentID);
            return (false, $"Error confirming payment: {ex.Message}");
        }
    }

    public static (bool Success, string Message) RejectPayment(int paymentID, int rejectedByUserID, string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Rejected reason is required.");

            var payment = PaymentDAL.GetPaymentByID(paymentID);
            if (payment == null)
                return (false, "Payment not found.");

            if (!string.Equals(payment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                return (false, "Only pending payments can be rejected.");

            bool success = PaymentDAL.RejectPayment(paymentID, rejectedByUserID, reason);
            if (!success)
                return (false, "Failed to reject payment.");

            NotifyResident(payment.ResidentID, "Thanh toán bị từ chối",
                $"Khoản thanh toán {payment.PaymentCode} đã bị từ chối. Lý do: {reason}", "Payment");

            AuditLogDAL.LogAction(
                rejectedByUserID,
                "Payment_Reject",
                "Payment",
                paymentID,
                $"Rejected payment {payment.PaymentCode}. Reason: {reason}");

            return (true, "Payment rejected successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error rejecting payment: {PaymentID}", paymentID);
            return (false, $"Error rejecting payment: {ex.Message}");
        }
    }

    public static (bool Success, string Message) CancelPayment(int paymentID, int cancelledByUserID, string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Cancel reason is required.");

            var payment = PaymentDAL.GetPaymentByID(paymentID);
            if (payment == null)
                return (false, "Payment not found.");

            if (!string.Equals(payment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                return (false, "Only pending payments can be cancelled.");

            bool success = PaymentDAL.CancelPayment(paymentID, cancelledByUserID, reason);
            if (!success)
                return (false, "Failed to cancel payment.");

            NotifyResident(payment.ResidentID, "Thanh toán đã bị hủy",
                $"Khoản thanh toán {payment.PaymentCode} đã bị hủy. Lý do: {reason}", "Payment");

            AuditLogDAL.LogAction(
                cancelledByUserID,
                "Payment_Cancel",
                "Payment",
                paymentID,
                $"Cancelled payment {payment.PaymentCode}. Reason: {reason}");

            return (true, "Payment cancelled successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error cancelling payment: {PaymentID}", paymentID);
            return (false, $"Error cancelling payment: {ex.Message}");
        }
    }

    private static bool RequiresTransferEvidence(string paymentMethod)
    {
        return string.Equals(paymentMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(paymentMethod, "QR", StringComparison.OrdinalIgnoreCase)
            || string.Equals(paymentMethod, "EWallet", StringComparison.OrdinalIgnoreCase);
    }

    private static void NotifyFinanceUsers(string title, string message, string notificationType)
    {
        try
        {
            var recipients = UserDAL.GetAllUsers()
                .Where(u => string.Equals(u.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .Where(u =>
                    string.Equals(u.RoleName, "Super Admin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.RoleName, "Manager", StringComparison.OrdinalIgnoreCase))
                .Select(u => u.UserID)
                .Distinct()
                .ToList();

            if (recipients.Count > 0)
                NotificationDAL.CreateBulkNotifications(recipients, title, message, notificationType);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error notifying finance users");
        }
    }

    private static void NotifyResident(int? residentID, string title, string message, string notificationType)
    {
        try
        {
            if (!residentID.HasValue)
                return;

            NotificationDAL.CreateNotification(residentID.Value, title, message, notificationType, "Sent");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error notifying resident: {ResidentID}", residentID);
        }
    }
}
