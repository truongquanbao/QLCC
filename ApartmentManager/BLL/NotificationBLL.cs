using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApartmentManager.BLL
{
    public class NotificationBLL
    {
        private const int MIN_SUBJECT_LENGTH = 5;
        private const int MAX_SUBJECT_LENGTH = 200;
        private const int MIN_BODY_LENGTH = 10;
        private const int MAX_BODY_LENGTH = 5000;

        /// <summary>
        /// Create a new notification in draft status
        /// </summary>
        public static (bool Success, string Message, int NotificationID) CreateNotification(
            int residentID,
            string subject,
            string body,
            string notificationType)
        {
            try
            {
                // Validate resident
                var resident = ResidentDAL.GetResidentByID(residentID);
                if (resident == null)
                    return (false, "Selected resident does not exist.", 0);

                // Validate subject
                if (string.IsNullOrWhiteSpace(subject))
                    return (false, "Subject is required.", 0);

                if (!ValidationHelper.IsValidLength(subject, MIN_SUBJECT_LENGTH, MAX_SUBJECT_LENGTH))
                    return (false, $"Subject must be between {MIN_SUBJECT_LENGTH} and {MAX_SUBJECT_LENGTH} characters.", 0);

                // Validate body
                if (string.IsNullOrWhiteSpace(body))
                    return (false, "Message body is required.", 0);

                if (!ValidationHelper.IsValidLength(body, MIN_BODY_LENGTH, MAX_BODY_LENGTH))
                    return (false, $"Message must be between {MIN_BODY_LENGTH} and {MAX_BODY_LENGTH} characters.", 0);

                // Validate notification type
                var validTypes = new[] { "Announcement", "Maintenance", "Payment", "Warning", "Other" };
                if (!validTypes.Contains(notificationType))
                    return (false, "Invalid notification type.", 0);

                // Create notification
                int notificationID = NotificationDAL.CreateNotification(
                    residentID,
                    subject,
                    body,
                    notificationType,
                    "Draft");

                if (notificationID > 0)
                {
                    Log.Information($"Notification created: ID={notificationID}, Type={notificationType}, Resident={residentID}");
                    return (true, "Notification created successfully in draft status.", notificationID);
                }

                return (false, "Failed to create notification.", 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating notification");
                return (false, $"Error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Send a notification
        /// </summary>
        public static (bool Success, string Message) SendNotification(int notificationID)
        {
            try
            {
                var notification = NotificationDAL.GetNotificationByID(notificationID);
                if (notification == null)
                    return (false, "Notification not found.");

                // Update status to sent
                bool updated = NotificationDAL.UpdateNotificationStatus(notificationID, "Sent", DateTime.Now);

                if (updated)
                {
                    Log.Information($"Notification sent: ID={notificationID}");
                    return (true, "Notification sent successfully.");
                }

                return (false, "Failed to send notification.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending notification");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        public static (bool Success, string Message) DeleteNotification(int notificationID)
        {
            try
            {
                var notification = NotificationDAL.GetNotificationByID(notificationID);
                if (notification == null)
                    return (false, "Notification not found.");

                // Prevent deletion of sent notifications for audit trail
                if (notification.Status == "Sent")
                    return (false, "Cannot delete sent notifications.");

                bool deleted = NotificationDAL.DeleteNotification(notificationID);

                if (deleted)
                {
                    Log.Information($"Notification deleted: ID={notificationID}");
                    return (true, "Notification deleted successfully.");
                }

                return (false, "Failed to delete notification.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting notification");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get notification statistics
        /// </summary>
        public static dynamic GetNotificationStatistics()
        {
            try
            {
                var notifications = NotificationDAL.GetAllNotifications();

                return new
                {
                    TotalNotifications = notifications.Count,
                    DraftCount = notifications.Count(n => n.Status == "Draft"),
                    SentCount = notifications.Count(n => n.Status == "Sent"),
                    FailedCount = notifications.Count(n => n.Status == "Failed"),
                    AnnouncementCount = notifications.Count(n => n.NotificationType == "Announcement"),
                    MaintenanceCount = notifications.Count(n => n.NotificationType == "Maintenance"),
                    PaymentCount = notifications.Count(n => n.NotificationType == "Payment"),
                    WarningCount = notifications.Count(n => n.NotificationType == "Warning")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving notification statistics");
                return new { TotalNotifications = 0 };
            }
        }
    }
}
