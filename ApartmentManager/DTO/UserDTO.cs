using System;

namespace ApartmentManager.DTO;

/// <summary>
/// Data Transfer Object for User
/// </summary>
public class UserDTO
{
    public int UserID { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int RoleID { get; set; }
    public string? RoleName { get; set; }
    public string? Status { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginCount { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive => Status == "Active";
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
