using System;

namespace ApartmentManager.DTO;

/// <summary>
/// Data Transfer Object for Complaint
/// </summary>
public class ComplaintDTO
{
    public int ComplaintID { get; set; }
    public int ResidentID { get; set; }
    public string? ComplaintType { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? ImageAttachmentPath { get; set; }
    public int? SatisfactionRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
