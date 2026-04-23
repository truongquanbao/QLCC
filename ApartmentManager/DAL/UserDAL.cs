using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for User operations
/// </summary>
public class UserDAL
{
    /// <summary>
    /// Get user by username
    /// </summary>
    public static UserDTO? GetUserByUsername(string username)
    {
        try
        {
            const string query = @"
                SELECT u.UserID, u.Username, u.PasswordHash, u.FullName, u.Email, u.Phone, 
                       u.RoleID, r.RoleName, u.Status, u.AvatarPath, u.LastLoginAt, 
                       u.FailedLoginCount, u.LockedUntil, u.IsApproved, u.ApprovedAt, 
                       u.CreatedAt, u.UpdatedAt
                FROM Users u
                INNER JOIN Roles r ON u.RoleID = r.RoleID
                WHERE u.Username = @Username
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapUserDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user by username: {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    public static UserDTO? GetUserByEmail(string email)
    {
        try
        {
            const string query = @"
                SELECT u.UserID, u.Username, u.PasswordHash, u.FullName, u.Email, u.Phone, 
                       u.RoleID, r.RoleName, u.Status, u.AvatarPath, u.LastLoginAt, 
                       u.FailedLoginCount, u.LockedUntil, u.IsApproved, u.ApprovedAt, 
                       u.CreatedAt, u.UpdatedAt
                FROM Users u
                INNER JOIN Roles r ON u.RoleID = r.RoleID
                WHERE u.Email = @Email
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapUserDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user by email: {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    public static UserDTO? GetUserByID(int userID)
    {
        try
        {
            const string query = @"
                SELECT u.UserID, u.Username, u.PasswordHash, u.FullName, u.Email, u.Phone, 
                       u.RoleID, r.RoleName, u.Status, u.AvatarPath, u.LastLoginAt, 
                       u.FailedLoginCount, u.LockedUntil, u.IsApproved, u.ApprovedAt, 
                       u.CreatedAt, u.UpdatedAt
                FROM Users u
                INNER JOIN Roles r ON u.RoleID = r.RoleID
                WHERE u.UserID = @UserID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapUserDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user by ID: {UserID}", userID);
            return null;
        }
    }

    /// <summary>
    /// Get all users
    /// </summary>
    public static List<UserDTO> GetAllUsers()
    {
        var users = new List<UserDTO>();

        try
        {
            const string query = @"
                SELECT u.UserID, u.Username, u.PasswordHash, u.FullName, u.Email, u.Phone, 
                       u.RoleID, r.RoleName, u.Status, u.AvatarPath, u.LastLoginAt, 
                       u.FailedLoginCount, u.LockedUntil, u.IsApproved, u.ApprovedAt, 
                       u.CreatedAt, u.UpdatedAt
                FROM Users u
                INNER JOIN Roles r ON u.RoleID = r.RoleID
                ORDER BY u.CreatedAt DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            users.Add(MapUserDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all users");
        }

        return users;
    }

    /// <summary>
    /// Create new user
    /// </summary>
    public static int CreateUser(string username, string passwordHash, string fullName, 
                                 string email, string phone, int roleID, string status = "Pending")
    {
        try
        {
            const string query = @"
                INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, RoleID, Status, IsApproved)
                VALUES (@Username, @PasswordHash, @FullName, @Email, @Phone, @RoleID, @Status, 0)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@FullName", fullName);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@RoleID", roleID);
                    command.Parameters.AddWithValue("@Status", status);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var userID = Convert.ToInt32(result);

                    Log.Information("User created: {Username} (ID: {UserID})", username, userID);
                    return userID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating user: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// Update user login attempt
    /// </summary>
    public static void UpdateLoginAttempt(int userID, bool success, int? lockDurationMinutes = null)
    {
        try
        {
            string query;
            if (success)
            {
                // Clear failed attempts on successful login
                query = @"
                    UPDATE Users 
                    SET LastLoginAt = GETDATE(), FailedLoginCount = 0, LockedUntil = NULL
                    WHERE UserID = @UserID
                ";
            }
            else
            {
                // Increment failed attempts
                query = @"
                    UPDATE Users 
                    SET FailedLoginCount = FailedLoginCount + 1,
                        LockedUntil = CASE 
                            WHEN FailedLoginCount + 1 >= (SELECT CAST(ConfigValue AS INT) FROM SystemConfig WHERE ConfigKey = 'MaxLoginAttempts')
                            THEN DATEADD(MINUTE, ISNULL(@LockDuration, 15), GETDATE())
                            ELSE NULL
                        END
                    WHERE UserID = @UserID
                ";
            }

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    if (lockDurationMinutes.HasValue)
                        command.Parameters.AddWithValue("@LockDuration", lockDurationMinutes.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Login attempt recorded for user: {UserID}, Success: {Success}", userID, success);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating login attempt for user: {UserID}", userID);
        }
    }

    /// <summary>
    /// Check if username exists
    /// </summary>
    public static bool UsernameExists(string username, int? excludeUserID = null)
    {
        try
        {
            string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            if (excludeUserID.HasValue)
                query += " AND UserID != @ExcludeUserID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    if (excludeUserID.HasValue)
                        command.Parameters.AddWithValue("@ExcludeUserID", excludeUserID.Value);

                    connection.Open();
                    return (int)command.ExecuteScalar()! > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if username exists: {Username}", username);
            return true; // Assume exists to be safe
        }
    }

    /// <summary>
    /// Check if email exists
    /// </summary>
    public static bool EmailExists(string email, int? excludeUserID = null)
    {
        try
        {
            string query = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
            if (excludeUserID.HasValue)
                query += " AND UserID != @ExcludeUserID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    if (excludeUserID.HasValue)
                        command.Parameters.AddWithValue("@ExcludeUserID", excludeUserID.Value);

                    connection.Open();
                    return (int)command.ExecuteScalar()! > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if email exists: {Email}", email);
            return true; // Assume exists to be safe
        }
    }

    /// <summary>
    /// Update user password hash
    /// </summary>
    public static bool UpdatePasswordHash(int userID, string newPasswordHash)
    {
        try
        {
            const string query = @"
                UPDATE Users 
                SET PasswordHash = @PasswordHash, UpdatedAt = GETDATE()
                WHERE UserID = @UserID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@PasswordHash", newPasswordHash);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Password updated for user: {UserID}", userID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating password for user: {UserID}", userID);
            return false;
        }
    }

    /// <summary>
    /// Approve user account
    /// </summary>
    public static bool ApproveUser(int userID, int approvedBy)
    {
        try
        {
            const string query = @"
                UPDATE Users 
                SET IsApproved = 1, Status = 'Active', ApprovedBy = @ApprovedBy, ApprovedAt = GETDATE(), UpdatedAt = GETDATE()
                WHERE UserID = @UserID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@ApprovedBy", approvedBy);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("User approved: {UserID} by {ApprovedBy}", userID, approvedBy);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error approving user: {UserID}", userID);
            return false;
        }
    }

    /// <summary>
    /// Map SqlDataReader to UserDTO
    /// </summary>
    private static UserDTO MapUserDTO(SqlDataReader reader)
    {
        return new UserDTO
        {
            UserID = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.IsDBNull(2) ? null : reader.GetString(2),
            FullName = reader.GetString(3),
            Email = reader.GetString(4),
            Phone = reader.GetString(5),
            RoleID = reader.GetInt32(6),
            RoleName = reader.GetString(7),
            Status = reader.GetString(8),
            AvatarPath = reader.IsDBNull(9) ? null : reader.GetString(9),
            LastLoginAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
            FailedLoginCount = reader.GetInt32(11),
            LockedUntil = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
            IsApproved = reader.GetBoolean(13),
            ApprovedAt = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
            CreatedAt = reader.GetDateTime(15),
            UpdatedAt = reader.GetDateTime(16)
        };
    }
}


