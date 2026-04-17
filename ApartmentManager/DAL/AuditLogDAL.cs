using Microsoft.Data.SqlClient;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Audit Log operations
/// </summary>
public class AuditLogDAL
{
    /// <summary>
    /// Log user action
    /// </summary>
    public static void LogAction(int? userID, string action, string entityName, int? entityID, 
                                 string? description = null, string? oldValue = null, string? newValue = null)
    {
        try
        {
            const string query = @"
                INSERT INTO AuditLogs (UserID, Action, EntityName, EntityID, OldValue, NewValue, 
                                       Timestamp, Description, IPAddress)
                VALUES (@UserID, @Action, @EntityName, @EntityID, @OldValue, @NewValue, 
                        GETDATE(), @Description, @IPAddress)
            ";

            var ipAddress = GetClientIPAddress();

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Action", action);
                    command.Parameters.AddWithValue("@EntityName", entityName);
                    command.Parameters.AddWithValue("@EntityID", entityID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@OldValue", oldValue ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@NewValue", newValue ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IPAddress", ipAddress ?? (object)DBNull.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Debug("Audit log recorded: {Action} on {EntityName} (ID: {EntityID})", action, entityName, entityID);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error logging audit action");
        }
    }

    /// <summary>
    /// Log login attempt
    /// </summary>
    public static void LogLogin(int userID, bool success, string? reason = null)
    {
        var action = success ? "Login_Success" : "Login_Failed";
        LogAction(userID, action, "User", userID, reason);
    }

    /// <summary>
    /// Log logout
    /// </summary>
    public static void LogLogout(int userID)
    {
        LogAction(userID, "Logout", "User", userID);
    }

    /// <summary>
    /// Get audit logs
    /// </summary>
    public static List<dynamic> GetAuditLogs(int? userID = null, string? action = null, 
                                             string? entityName = null, DateTime? fromDate = null, 
                                             DateTime? toDate = null, int limit = 1000)
    {
        var logs = new List<dynamic>();

        try
        {
            var query = @"
                SELECT TOP (@Limit)
                    l.LogID, l.UserID, u.Username, l.Action, l.EntityName, l.EntityID, 
                    l.OldValue, l.NewValue, l.Timestamp, l.Description, l.IPAddress
                FROM AuditLogs l
                LEFT JOIN Users u ON l.UserID = u.UserID
                WHERE 1=1
            ";

            if (userID.HasValue)
                query += " AND l.UserID = @UserID";
            if (!string.IsNullOrEmpty(action))
                query += " AND l.Action = @Action";
            if (!string.IsNullOrEmpty(entityName))
                query += " AND l.EntityName = @EntityName";
            if (fromDate.HasValue)
                query += " AND l.Timestamp >= @FromDate";
            if (toDate.HasValue)
                query += " AND l.Timestamp <= @ToDate";

            query += " ORDER BY l.Timestamp DESC";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Limit", limit);
                    if (userID.HasValue)
                        command.Parameters.AddWithValue("@UserID", userID.Value);
                    if (!string.IsNullOrEmpty(action))
                        command.Parameters.AddWithValue("@Action", action);
                    if (!string.IsNullOrEmpty(entityName))
                        command.Parameters.AddWithValue("@EntityName", entityName);
                    if (fromDate.HasValue)
                        command.Parameters.AddWithValue("@FromDate", fromDate.Value);
                    if (toDate.HasValue)
                        command.Parameters.AddWithValue("@ToDate", toDate.Value);

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dynamic log = new
                            {
                                LogID = reader.GetInt32(0),
                                UserID = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                Username = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Action = reader.GetString(3),
                                EntityName = reader.GetString(4),
                                EntityID = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                OldValue = reader.IsDBNull(6) ? null : reader.GetString(6),
                                NewValue = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Timestamp = reader.GetDateTime(8),
                                Description = reader.IsDBNull(9) ? null : reader.GetString(9),
                                IPAddress = reader.IsDBNull(10) ? null : reader.GetString(10)
                            };
                            logs.Add(log);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting audit logs");
        }

        return logs;
    }

    /// <summary>
    /// Get client IP address (simplified version)
    /// </summary>
    private static string? GetClientIPAddress()
    {
        try
        {
            // In WinForms, we don't have HttpContext, so return local machine name
            return System.Net.Dns.GetHostName();
        }
        catch
        {
            return null;
        }
    }
}
