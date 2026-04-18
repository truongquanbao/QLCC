using System;
using System.Collections.Generic;

namespace ApartmentManager.DTO;

/// <summary>
/// Data Transfer Object for Role
/// </summary>
public class RoleDTO
{
    public int RoleID { get; set; }
    public string? RoleName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> PermissionIDs { get; set; } = new();
}

