using System;

namespace ApartmentManager.DTO;

/// <summary>
/// Data Transfer Object for Notification
/// </summary>
public class NotificationDTO
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public int ResidentID { get; set; }
    public string? Title { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? Message { get; set; }
    public string? Body { get; set; }
    public string? Type { get; set; }
    public string? NotificationType { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
