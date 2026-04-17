using Microsoft.Data.SqlClient;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Role and Permission operations
/// </summary>
public class RolePermissionDAL
{
    /// <summary>
    /// Get role by ID
    /// </summary>
    public static RoleDTO? GetRoleByID(int roleID)
    {
        try
        {
            const string query = @"
                SELECT RoleID, RoleName, Description, CreatedAt
                FROM Roles
                WHERE RoleID = @RoleID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RoleID", roleID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var role = new RoleDTO
                            {
                                RoleID = reader.GetInt32(0),
                                RoleName = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3)
                            };

                            reader.Close();

                            // Get permissions for this role
                            role.PermissionIDs = GetPermissionIDsForRole(roleID);
                            return role;
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting role by ID: {RoleID}", roleID);
            return null;
        }
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    public static List<RoleDTO> GetAllRoles()
    {
        var roles = new List<RoleDTO>();

        try
        {
            const string query = @"
                SELECT RoleID, RoleName, Description, CreatedAt
                FROM Roles
                ORDER BY RoleID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var role = new RoleDTO
                            {
                                RoleID = reader.GetInt32(0),
                                RoleName = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3)
                            };
                            role.PermissionIDs = GetPermissionIDsForRole(role.RoleID);
                            roles.Add(role);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all roles");
        }

        return roles;
    }

    /// <summary>
    /// Get permission IDs for a role
    /// </summary>
    public static List<int> GetPermissionIDsForRole(int roleID)
    {
        var permissionIDs = new List<int>();

        try
        {
            const string query = @"
                SELECT PermissionID
                FROM RolePermissions
                WHERE RoleID = @RoleID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RoleID", roleID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            permissionIDs.Add(reader.GetInt32(0));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting permissions for role: {RoleID}", roleID);
        }

        return permissionIDs;
    }

    /// <summary>
    /// Get permission names for a role
    /// </summary>
    public static List<string> GetPermissionNamesForRole(int roleID)
    {
        var permissions = new List<string>();

        try
        {
            const string query = @"
                SELECT p.PermissionName
                FROM RolePermissions rp
                INNER JOIN Permissions p ON rp.PermissionID = p.PermissionID
                WHERE rp.RoleID = @RoleID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RoleID", roleID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            permissions.Add(reader.GetString(0));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting permission names for role: {RoleID}", roleID);
        }

        return permissions;
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    public static List<PermissionDTO> GetAllPermissions()
    {
        var permissions = new List<PermissionDTO>();

        try
        {
            const string query = @"
                SELECT PermissionID, PermissionName, Description, CreatedAt
                FROM Permissions
                ORDER BY PermissionName
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            permissions.Add(new PermissionDTO
                            {
                                PermissionID = reader.GetInt32(0),
                                PermissionName = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all permissions");
        }

        return permissions;
    }

    /// <summary>
    /// Check if user has permission
    /// </summary>
    public static bool UserHasPermission(int userID, string permissionName)
    {
        try
        {
            const string query = @"
                SELECT COUNT(*)
                FROM Users u
                INNER JOIN RolePermissions rp ON u.RoleID = rp.RoleID
                INNER JOIN Permissions p ON rp.PermissionID = p.PermissionID
                WHERE u.UserID = @UserID AND p.PermissionName = @PermissionName
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@PermissionName", permissionName);

                    connection.Open();
                    var result = (int)command.ExecuteScalar()!;
                    return result > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking user permission: {UserID}, {PermissionName}", userID, permissionName);
            return false;
        }
    }

    /// <summary>
    /// Check if role has permission
    /// </summary>
    public static bool RoleHasPermission(int roleID, string permissionName)
    {
        try
        {
            const string query = @"
                SELECT COUNT(*)
                FROM RolePermissions rp
                INNER JOIN Permissions p ON rp.PermissionID = p.PermissionID
                WHERE rp.RoleID = @RoleID AND p.PermissionName = @PermissionName
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RoleID", roleID);
                    command.Parameters.AddWithValue("@PermissionName", permissionName);

                    connection.Open();
                    var result = (int)command.ExecuteScalar()!;
                    return result > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking role permission: {RoleID}, {PermissionName}", roleID, permissionName);
            return false;
        }
    }
}
