using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Replace all permissions assigned to a role.
    /// </summary>
    public static bool UpdateRolePermissions(int roleID, IEnumerable<int> permissionIDs)
    {
        try
        {
            var normalizedPermissionIds = (permissionIDs ?? Enumerable.Empty<int>())
                .Distinct()
                .ToList();

            using (var connection = DatabaseHelper.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using (var deleteCommand = new SqlCommand("DELETE FROM RolePermissions WHERE RoleID = @RoleID", connection, transaction))
                    {
                        deleteCommand.Parameters.AddWithValue("@RoleID", roleID);
                        deleteCommand.ExecuteNonQuery();
                    }

                    if (normalizedPermissionIds.Count > 0)
                    {
                        const string insertQuery = @"
                            INSERT INTO RolePermissions (RoleID, PermissionID)
                            VALUES (@RoleID, @PermissionID)
                        ";

                        foreach (var permissionID in normalizedPermissionIds)
                        {
                            using var insertCommand = new SqlCommand(insertQuery, connection, transaction);
                            insertCommand.Parameters.AddWithValue("@RoleID", roleID);
                            insertCommand.Parameters.AddWithValue("@PermissionID", permissionID);
                            insertCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating permissions for role: {RoleID}", roleID);
            return false;
        }
    }

    /// <summary>
    /// Ensure the application has the standard RBAC roles, permissions and demo accounts.
    /// Existing custom permission changes are preserved after the current RBAC policy version is applied once.
    /// </summary>
    public static void EnsureRbacDefaults()
    {
        const string policyVersion = "2026-05-26-rbac-manager-v2";
        try
        {
            var roles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Super Admin"] = "Toàn quyền hệ thống",
                ["Admin"] = "Quản lý hệ thống",
                ["Manager"] = "Quản lý vận hành chung cư",
                ["Kế toán"] = "Quản lý hóa đơn, phí và xuất dữ liệu tài chính",
                ["Lễ tân"] = "Quản lý khách ra vào",
                ["Kỹ thuật"] = "Xử lý phản ánh và tài sản",
                ["Bảo vệ"] = "Quản lý khách ra vào và kiểm soát cổng",
                ["Resident"] = "Cư dân chung cư"
            };

            var permissions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ViewDashboard"] = "Xem dashboard",
                ["UserManagement"] = "Quản lý tài khoản người dùng",
                ["ManageRoles"] = "Quản lý vai trò và phân quyền",
                ["ChangeUserRole"] = "Đổi vai trò người dùng",
                ["LockUsers"] = "Khóa hoặc mở khóa tài khoản",
                ["ResetPassword"] = "Reset mật khẩu tài khoản",
                ["DeleteUsers"] = "Xóa tài khoản",
                ["ManageApartments"] = "Quản lý tòa nhà / căn hộ",
                ["ManageResidents"] = "Quản lý cư dân",
                ["ManageContracts"] = "Quản lý hợp đồng",
                ["ManageInvoices"] = "Quản lý hóa đơn / phí",
                ["ManageFeeTypes"] = "Quản lý loại phí",
                ["ManageComplaints"] = "Quản lý phản ánh",
                ["ManageAssets"] = "Quản lý tài sản / bảo trì",
                ["ManageNotifications"] = "Quản lý thông báo",
                ["ManageVehicles"] = "Quản lý phương tiện",
                ["ManageVisitors"] = "Quản lý khách ra vào",
                ["ViewReports"] = "Xem báo cáo",
                ["ReportGeneration"] = "Tạo báo cáo",
                ["ExportData"] = "Xuất Excel / PDF",
                ["ViewLogs"] = "Xem log hệ thống",
                ["SystemConfiguration"] = "Cấu hình hệ thống",
                ["ResetData"] = "Reset dữ liệu hệ thống"
            };

            var rolePermissions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Super Admin"] = permissions.Keys.ToArray(),
                ["Admin"] = new[]
                {
                    "ViewDashboard", "UserManagement", "ManageRoles", "ChangeUserRole", "LockUsers",
                    "ResetPassword", "DeleteUsers", "ManageApartments", "ManageResidents",
                    "ManageContracts", "ManageInvoices", "ManageFeeTypes", "ManageComplaints",
                    "ManageAssets", "ManageNotifications", "ManageVehicles", "ManageVisitors",
                    "ViewReports", "ReportGeneration", "ExportData", "ViewLogs",
                    "SystemConfiguration", "ResetData"
                },

                ["Manager"] = new[]
                {
                    "ViewDashboard",

                    "ManageApartments",
                    "ManageResidents",
                    "ManageInvoices",
                    "ManageFeeTypes",
                    "ManageComplaints",
                    "ManageNotifications",
                    "ManageVehicles",
                    "ManageVisitors",
                    "ManageAssets",

                    "ViewReports",
                    "ReportGeneration",
                    "ExportData"
                },

                ["Kế toán"] = new[] { "ViewDashboard", "ManageInvoices", "ManageFeeTypes", "ViewReports", "ReportGeneration", "ExportData" },
                ["Lễ tân"] = new[] { "ViewDashboard", "ManageVisitors", "ManageNotifications" },
                ["Kỹ thuật"] = new[] { "ViewDashboard", "ManageComplaints", "ManageAssets", "ManageNotifications" },
                ["Bảo vệ"] = new[] { "ViewDashboard", "ManageVisitors" },
                ["Resident"] = new[] { "ViewDashboard", "ManageInvoices", "ManageComplaints", "ManageNotifications", "ManageVehicles", "ManageVisitors" }
            };

            var demoUsers = new[]
            {
                (Username: "superadmin", FullName: "Super Administrator", Email: "superadmin@system.local", Phone: "0900000001", Role: "Super Admin"),
                (Username: "admin1", FullName: "Admin Demo", Email: "admin1@system.local", Phone: "0900000005", Role: "Admin"),
                (Username: "manager1", FullName: "Manager Demo", Email: "manager1@system.local", Phone: "0900000002", Role: "Manager"),
                (Username: "ketoan01", FullName: "Kế toán Demo", Email: "ketoan01@system.local", Phone: "0900000006", Role: "Kế toán"),
                (Username: "letan01", FullName: "Lễ tân Demo", Email: "letan01@system.local", Phone: "0900000007", Role: "Lễ tân"),
                (Username: "kythuat01", FullName: "Kỹ thuật Demo", Email: "kythuat01@system.local", Phone: "0900000008", Role: "Kỹ thuật"),
                (Username: "baove01", FullName: "Bảo vệ Demo", Email: "baove01@system.local", Phone: "0900000009", Role: "Bảo vệ")
            };

            using var connection = DatabaseHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            foreach (var role in roles)
            {
                UpsertRole(connection, transaction, role.Key, role.Value);
            }

            foreach (var permission in permissions)
            {
                UpsertPermission(connection, transaction, permission.Key, permission.Value);
            }

            var roleIds = LoadRoleIds(connection, transaction);
            var permissionIds = LoadPermissionIds(connection, transaction);
            string currentVersion = GetSystemConfig(connection, transaction, "RbacPolicyVersion");
            if (!string.Equals(currentVersion, policyVersion, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var role in rolePermissions)
                {
                    if (!roleIds.TryGetValue(role.Key, out int roleId))
                    {
                        continue;
                    }

                    ReplaceRolePermissions(connection, transaction, roleId, role.Value, permissionIds);
                }

                UpsertSystemConfig(connection, transaction, "RbacPolicyVersion", policyVersion, "Phiên bản chính sách phân quyền RBAC mặc định");
            }

            foreach (var user in demoUsers)
            {
                if (roleIds.TryGetValue(user.Role, out int roleId))
                {
                    EnsureDemoUser(connection, transaction, user.Username, user.FullName, user.Email, user.Phone, roleId);
                }
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error ensuring RBAC defaults");
        }

        static void UpsertRole(SqlConnection connection, SqlTransaction transaction, string roleName, string description)
        {
            const string query = @"
                IF EXISTS (SELECT 1 FROM Roles WHERE RoleName = @RoleName)
                    UPDATE Roles SET Description = @Description WHERE RoleName = @RoleName;
                ELSE
                    INSERT INTO Roles (RoleName, Description) VALUES (@RoleName, @Description);";
            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@RoleName", roleName);
            command.Parameters.AddWithValue("@Description", description);
            command.ExecuteNonQuery();
        }

        static void UpsertPermission(SqlConnection connection, SqlTransaction transaction, string permissionName, string description)
        {
            const string query = @"
                IF EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = @PermissionName)
                    UPDATE Permissions SET Description = @Description WHERE PermissionName = @PermissionName;
                ELSE
                    INSERT INTO Permissions (PermissionName, Description) VALUES (@PermissionName, @Description);";
            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@PermissionName", permissionName);
            command.Parameters.AddWithValue("@Description", description);
            command.ExecuteNonQuery();
        }

        static Dictionary<string, int> LoadRoleIds(SqlConnection connection, SqlTransaction transaction)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using var command = new SqlCommand("SELECT RoleID, RoleName FROM Roles", connection, transaction);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result[reader.GetString(1)] = reader.GetInt32(0);
            }

            return result;
        }

        static Dictionary<string, int> LoadPermissionIds(SqlConnection connection, SqlTransaction transaction)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using var command = new SqlCommand("SELECT PermissionID, PermissionName FROM Permissions", connection, transaction);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result[reader.GetString(1)] = reader.GetInt32(0);
            }

            return result;
        }

        static string GetSystemConfig(SqlConnection connection, SqlTransaction transaction, string key)
        {
            using var command = new SqlCommand("SELECT ConfigValue FROM SystemConfig WHERE ConfigKey = @ConfigKey", connection, transaction);
            command.Parameters.AddWithValue("@ConfigKey", key);
            return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
        }

        static void UpsertSystemConfig(SqlConnection connection, SqlTransaction transaction, string key, string value, string description)
        {
            const string query = @"
                IF EXISTS (SELECT 1 FROM SystemConfig WHERE ConfigKey = @ConfigKey)
                    UPDATE SystemConfig
                    SET ConfigValue = @ConfigValue, Description = @Description, UpdatedAt = GETDATE()
                    WHERE ConfigKey = @ConfigKey;
                ELSE
                    INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt)
                    VALUES (@ConfigKey, @ConfigValue, @Description, GETDATE());";
            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@ConfigKey", key);
            command.Parameters.AddWithValue("@ConfigValue", value);
            command.Parameters.AddWithValue("@Description", description);
            command.ExecuteNonQuery();
        }

        static void ReplaceRolePermissions(SqlConnection connection, SqlTransaction transaction, int roleId, IEnumerable<string> permissionNames, Dictionary<string, int> permissionIds)
        {
            using (var delete = new SqlCommand("DELETE FROM RolePermissions WHERE RoleID = @RoleID", connection, transaction))
            {
                delete.Parameters.AddWithValue("@RoleID", roleId);
                delete.ExecuteNonQuery();
            }

            const string insertQuery = "INSERT INTO RolePermissions (RoleID, PermissionID) VALUES (@RoleID, @PermissionID)";
            foreach (string permissionName in permissionNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!permissionIds.TryGetValue(permissionName, out int permissionId))
                {
                    continue;
                }

                using var insert = new SqlCommand(insertQuery, connection, transaction);
                insert.Parameters.AddWithValue("@RoleID", roleId);
                insert.Parameters.AddWithValue("@PermissionID", permissionId);
                insert.ExecuteNonQuery();
            }
        }

        static void EnsureDemoUser(SqlConnection connection, SqlTransaction transaction, string username, string fullName, string email, string phone, int roleId)
        {
            using (var exists = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @Username", connection, transaction))
            {
                exists.Parameters.AddWithValue("@Username", username);
                if ((int)exists.ExecuteScalar()! > 0)
                {
                    return;
                }
            }

            const string insertQuery = @"
                INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved, ApprovedAt, CreatedAt, UpdatedAt)
                VALUES (@Username, @PasswordHash, @FullName, @Email, @Phone, @RoleID, N'Active', 1, GETDATE(), GETDATE(), GETDATE());";
            using var insert = new SqlCommand(insertQuery, connection, transaction);
            insert.Parameters.AddWithValue("@Username", username);
            insert.Parameters.AddWithValue("@PasswordHash", PasswordHasher.HashPassword("Demo@123"));
            insert.Parameters.AddWithValue("@FullName", fullName);
            insert.Parameters.AddWithValue("@Email", email);
            insert.Parameters.AddWithValue("@Phone", phone);
            insert.Parameters.AddWithValue("@RoleID", roleId);
            insert.ExecuteNonQuery();
        }
    }
}


