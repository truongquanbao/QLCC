using System;

namespace ApartmentManager.DTO;

public class PaymentAccountDTO
{
    public int PaymentAccountID { get; set; }
    public string? AccountName { get; set; }
    public string? AccountType { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountHolder { get; set; }
    public string? QRImagePath { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentDTO
{
    public int PaymentID { get; set; }
    public string? PaymentCode { get; set; }
    public int InvoiceID { get; set; }
    public int ApartmentID { get; set; }
    public int? ResidentID { get; set; }
    public int? PaymentAccountID { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? TransactionCode { get; set; }
    public string? ProofImagePath { get; set; }
    public string? PaymentStatus { get; set; }
    public int? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? RejectedReason { get; set; }
    public string? CancelReason { get; set; }
    public string? Note { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? InvoiceCode { get; set; }
    public string? ApartmentCode { get; set; }
    public string? ResidentName { get; set; }
    public string? AccountName { get; set; }
}

public class ReceiptDTO
{
    public int ReceiptID { get; set; }
    public string? ReceiptCode { get; set; }
    public int PaymentID { get; set; }
    public int InvoiceID { get; set; }
    public int ApartmentID { get; set; }
    public string? PayerName { get; set; }
    public decimal Amount { get; set; }
    public DateTime ReceiptDate { get; set; }
    public int CreatedBy { get; set; }
    public string? Note { get; set; }
}

public class ExpenseCategoryDTO
{
    public int ExpenseCategoryID { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VendorDTO
{
    public int VendorID { get; set; }
    public string? VendorName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpenseDTO
{
    public int ExpenseID { get; set; }
    public string? ExpenseCode { get; set; }
    public int ExpenseCategoryID { get; set; }
    public int? VendorID { get; set; }
    public int? FundID { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiptImagePath { get; set; }
    public string? ExpenseStatus { get; set; }
    public int CreatedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentVoucherDTO
{
    public int VoucherID { get; set; }
    public string? VoucherCode { get; set; }
    public int ExpenseID { get; set; }
    public string? PayeeName { get; set; }
    public decimal Amount { get; set; }
    public DateTime VoucherDate { get; set; }
    public int CreatedBy { get; set; }
    public string? Note { get; set; }
}

public class FundDTO
{
    public int FundID { get; set; }
    public string? FundName { get; set; }
    public string? Description { get; set; }
    public decimal InitialBalance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FundTransactionDTO
{
    public int FundTransactionID { get; set; }
    public int FundID { get; set; }
    public string? TransactionType { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceID { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public int CreatedBy { get; set; }
    public string? Note { get; set; }
}

public class FinancialPeriodDTO
{
    public int PeriodID { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Status { get; set; }
    public int? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
