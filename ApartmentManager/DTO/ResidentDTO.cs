using System;

namespace ApartmentManager.DTO;

/// <summary>
/// Data Transfer Object for Resident
/// </summary>
public class ResidentDTO
{
    public int ResidentID { get; set; }
    public int UserID { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public DateTime? DOB { get; set; }
    public string? Gender { get; set; }
    public string? CCCD { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressRegistration { get; set; }
    public int ApartmentID { get; set; }
    public string? ApartmentCode { get; set; }
    public string? ResidentStatus { get; set; }
    public string? Status { get; set; }
    public string? RelationshipWithOwner { get; set; }
    public DateTime? MoveInDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? MoveOutDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AvatarPath { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
