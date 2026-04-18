using System;

namespace ApartmentManager.DTO;

/// <summary>
/// Data Transfer Object for Permission
/// </summary>
public class PermissionDTO
{
    public int PermissionID { get; set; }
    public string? PermissionName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
