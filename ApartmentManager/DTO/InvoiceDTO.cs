using System;
using System.Collections.Generic;

namespace ApartmentManager.DTO;

/// <summary>
/// Data Transfer Object for Invoice
/// </summary>
public class InvoiceDTO
{
    public int InvoiceID { get; set; }
    public int ApartmentID { get; set; }
    public int? ResidentID { get; set; }
    public string? ApartmentCode { get; set; }
    public int InvoiceMonth { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedDate => CreatedAt;
    public string? Note { get; set; }
    public int? ConfirmedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<InvoiceDetailDTO> InvoiceDetails { get; set; } = new();
}

/// <summary>
/// Data Transfer Object for Invoice Detail
/// </summary>
public class InvoiceDetailDTO
{
    public int InvoiceDetailID { get; set; }
    public int InvoiceID { get; set; }
    public int FeeTypeID { get; set; }
    public string? FeeTypeName { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

