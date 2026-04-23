using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Notification operations
/// </summary>
public class NotificationDAL
{
    /// <summary>
    /// Get notification by ID
    /// </summary>
    public static NotificationDTO? GetNotificationByID(int notificationID)
    {
        try
        {
            const string query = @"
                SELECT NotificationID, UserID, ResidentID, Title, Subject, Message, Body,
                       NotificationType, Priority, IsRead, ReadAt, Status, SentDate, CreatedAt, UpdatedAt
                FROM Notifications
                WHERE NotificationID = @NotificationID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NotificationID", notificationID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapNotificationDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting notification by ID: {NotificationID}", notificationID);
            return null;
        }
    }

    /// <summary>
    /// Get all notifications for user
    /// </summary>
    public static List<NotificationDTO> GetUserNotifications(int userID)
    {
        var notifications = new List<NotificationDTO>();

        try
        {
            const string query = @"
                SELECT NotificationID, UserID, ResidentID, Title, Subject, Message, Body,
                       NotificationType, Priority, IsRead, ReadAt, Status, SentDate, CreatedAt, UpdatedAt
                FROM Notifications
                WHERE UserID = @UserID
                ORDER BY CreatedAt DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            notifications.Add(MapNotificationDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user notifications: {UserID}", userID);
        }

        return notifications;
    }

    /// <summary>
    /// Get unread notifications for user
    /// </summary>
    public static List<NotificationDTO> GetUnreadNotifications(int userID)
    {
        var notifications = new List<NotificationDTO>();

        try
        {
            const string query = @"
                SELECT NotificationID, UserID, ResidentID, Title, Subject, Message, Body,
                       NotificationType, Priority, IsRead, ReadAt, Status, SentDate, CreatedAt, UpdatedAt
                FROM Notifications
                WHERE UserID = @UserID AND IsRead = 0
                ORDER BY CreatedAt DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            notifications.Add(MapNotificationDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting unread notifications: {UserID}", userID);
        }

        return notifications;
    }

    /// <summary>
    /// Get unread notification count for user
    /// </summary>
    public static int GetUnreadNotificationCount(int userID)
    {
        try
        {
            const string query = "SELECT COUNT(*) FROM Notifications WHERE UserID = @UserID AND IsRead = 0";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    connection.Open();
                    return (int)command.ExecuteScalar()!;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting unread notification count: {UserID}", userID);
            return 0;
        }
    }

    /// <summary>
    /// Create notification
    /// </summary>
    public static int CreateNotification(int userID, string title, string message, string notificationType, string? status = null)
    {
        try
        {
            var resident = ResidentDAL.GetResidentByID(userID);
            int? residentID = resident?.ResidentID;
            int? actualUserID = resident?.UserID;

            if (resident == null)
            {
                actualUserID = userID;
            }

            const string query = @"
                INSERT INTO Notifications (UserID, ResidentID, Title, Subject, Message, Body,
                                           NotificationType, Priority, IsRead, Status, SentDate)
                VALUES (@UserID, @ResidentID, @Title, @Subject, @Message, @Body,
                        @NotificationType, @Priority, 0, @Status, @SentDate);
                SELECT SCOPE_IDENTITY();
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", actualUserID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ResidentID", residentID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Subject", title);
                    command.Parameters.AddWithValue("@Message", message);
                    command.Parameters.AddWithValue("@Body", message);
                    command.Parameters.AddWithValue("@NotificationType", notificationType);
                    command.Parameters.AddWithValue("@Priority", "Medium");
                    command.Parameters.AddWithValue("@Status", status ?? "Draft");
                    command.Parameters.AddWithValue("@SentDate", status == "Sent" ? (object)DateTime.Now : DBNull.Value);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var notificationID = Convert.ToInt32(result);

                    Log.Information("Notification created: {Title} for recipient {UserID} (ID: {NotificationID})", title, userID, notificationID);
                    return notificationID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating notification for user: {UserID}", userID);
            throw;
        }
    }

    /// <summary>
    /// Create bulk notifications for multiple users
    /// </summary>
    public static int CreateBulkNotifications(List<int> userIDs, string title, string message, string notificationType)
    {
        int count = 0;

        try
        {
            const string query = @"
                INSERT INTO Notifications (UserID, ResidentID, Title, Subject, Message, Body,
                                           NotificationType, Priority, IsRead, Status, SentDate)
                VALUES (@UserID, NULL, @Title, @Subject, @Message, @Body,
                        @NotificationType, @Priority, 0, 'Sent', GETDATE())
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                connection.Open();

                foreach (var userID in userIDs)
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@UserID", userID);
                        command.Parameters.AddWithValue("@Title", title);
                        command.Parameters.AddWithValue("@Subject", title);
                        command.Parameters.AddWithValue("@Message", message);
                        command.Parameters.AddWithValue("@Body", message);
                        command.Parameters.AddWithValue("@NotificationType", notificationType);
                        command.Parameters.AddWithValue("@Priority", "Medium");

                        command.ExecuteNonQuery();
                        count++;
                    }
                }
            }

            Log.Information("Bulk notifications created: {Count} notifications", count);
            return count;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating bulk notifications");
            throw;
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public static bool MarkAsRead(int notificationID)
    {
        try
        {
            const string query = @"
                UPDATE Notifications
                SET IsRead = 1, ReadAt = GETDATE()
                WHERE NotificationID = @NotificationID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NotificationID", notificationID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Notification marked as read: {NotificationID}", notificationID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error marking notification as read: {NotificationID}", notificationID);
            return false;
        }
    }

    /// <summary>
    /// Mark all notifications as read for user
    /// </summary>
    public static bool MarkAllAsRead(int userID)
    {
        try
        {
            const string query = @"
                UPDATE Notifications
                SET IsRead = 1, ReadAt = GETDATE()
                WHERE UserID = @UserID AND IsRead = 0
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("All notifications marked as read for user: {UserID}", userID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error marking all notifications as read for user: {UserID}", userID);
            return false;
        }
    }

    /// <summary>
    /// Delete notification
    /// </summary>
    public static bool DeleteNotification(int notificationID)
    {
        try
        {
            const string query = "DELETE FROM Notifications WHERE NotificationID = @NotificationID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NotificationID", notificationID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Notification deleted: {NotificationID}", notificationID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting notification: {NotificationID}", notificationID);
            return false;
        }
    }

    /// <summary>
    /// Delete old notifications (older than specified days)
    /// </summary>
    public static int DeleteOldNotifications(int daysOld)
    {
        try
        {
            const string query = "DELETE FROM Notifications WHERE CreatedAt < DATEADD(day, -@DaysOld, GETDATE())";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DaysOld", daysOld);
                    connection.Open();
                    int deleted = command.ExecuteNonQuery();

                    Log.Information("Old notifications deleted: {Count} records older than {Days} days", deleted, daysOld);
                    return deleted;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting old notifications");
            return 0;
        }
    }

    /// <summary>
    /// Get all notifications
    /// </summary>
    public static List<NotificationDTO> GetAllNotifications()
    {
        try
        {
            const string query = @"
                SELECT NotificationID, UserID, ResidentID, Title, Subject, Message, Body,
                       NotificationType, Priority, IsRead, ReadAt, Status, SentDate, CreatedAt, UpdatedAt
                FROM Notifications
                ORDER BY CreatedAt DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    var notifications = new List<NotificationDTO>();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            notifications.Add(MapNotificationDTO(reader));
                    }

                    return notifications;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all notifications");
            return new List<NotificationDTO>();
        }
    }

    /// <summary>
    /// Update notification status
    /// </summary>
    public static bool UpdateNotificationStatus(int notificationID, string status, DateTime? sentTime = null)
    {
        try
        {
            const string query = @"
                UPDATE Notifications 
                SET IsRead = @IsRead, Status = @Status, SentDate = ISNULL(@SentDate, SentDate), UpdatedAt = GETDATE()
                WHERE NotificationID = @NotificationID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NotificationID", notificationID);
                    command.Parameters.AddWithValue("@IsRead", status == "Read");
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@SentDate", sentTime ?? DateTime.Now);
                    connection.Open();

                    var affected = command.ExecuteNonQuery();
                    if (affected > 0)
                    {
                        Log.Information("Notification {NotificationID} status updated to {Status}", notificationID, status);
                    }

                    return affected > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating notification status: {NotificationID}", notificationID);
            return false;
        }
    }

    /// <summary>
    /// Map SqlDataReader to NotificationDTO
    /// </summary>
    private static NotificationDTO MapNotificationDTO(SqlDataReader reader)
    {
        return new NotificationDTO
        {
            NotificationID = reader.GetInt32(0),
            UserID = reader.GetInt32(1),
            ResidentID = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            Title = reader.GetString(3),
            Subject = reader.IsDBNull(4) ? null : reader.GetString(4),
            Description = reader.IsDBNull(5) ? null : reader.GetString(5),
            Message = reader.GetString(5),
            Body = reader.IsDBNull(6) ? null : reader.GetString(6),
            Type = reader.GetString(7),
            NotificationType = reader.GetString(7),
            Priority = reader.GetString(8),
            IsRead = reader.GetBoolean(9),
            ReadAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
            Status = reader.GetString(11),
            SentDate = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
            CreatedAt = reader.GetDateTime(13),
            UpdatedAt = reader.IsDBNull(14) ? null : reader.GetDateTime(14)
        };
    }
}


