using System;
using System.Collections.Generic;
using ApartmentManager.DTO;

namespace ApartmentManager.Utilities;

/// <summary>
/// Represents user session information
/// </summary>
public class UserSession
{
    public int UserID { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int RoleID { get; set; }
    public string? RoleName { get; set; }
    public string? Status { get; set; }
    public string? AvatarPath { get; set; }
    public UserDTO? CurrentUser { get; set; }
    public List<string> Permissions { get; set; } = new();
    public DateTime LoginTime { get; set; } = DateTime.Now;
    public DateTime? LastActivityTime { get; set; }

    /// <summary>
    /// Check if user has a specific permission
    /// </summary>
    public bool HasPermission(string permissionName)
    {
        return Permissions.Contains(permissionName);
    }

    /// <summary>
    /// Check if user is active (not locked or inactive)
    /// </summary>
    public bool IsActive => Status == "Active";
}

