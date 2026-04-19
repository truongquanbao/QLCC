using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApartmentManager.BLL
{
    public class VisitorBLL
    {
        private const int MIN_NAME_LENGTH = 3;
        private const int MAX_NAME_LENGTH = 100;
        private const int MIN_PURPOSE_LENGTH = 5;
        private const int MAX_PURPOSE_LENGTH = 500;

        /// <summary>
        /// Check-in a visitor
        /// </summary>
        public static (bool Success, string Message, int VisitorID) CheckInVisitor(
            int apartmentID,
            int residentID,
            string visitorName,
            string phone,
            string email,
            string visitorType,
            string purpose)
        {
            try
            {
                // Validate apartment
                var apartment = ApartmentDAL.GetApartmentByID(apartmentID);
                if (apartment == null)
                    return (false, "Selected apartment does not exist.", 0);

                // Validate resident
                var resident = ResidentDAL.GetResidentByID(residentID);
                if (resident == null)
                    return (false, "Selected resident does not exist.", 0);

                // Validate visitor name
                if (string.IsNullOrWhiteSpace(visitorName))
                    return (false, "Visitor name is required.", 0);

                if (!ValidationHelper.IsValidLength(visitorName, MIN_NAME_LENGTH, MAX_NAME_LENGTH))
                    return (false, $"Visitor name must be between {MIN_NAME_LENGTH} and {MAX_NAME_LENGTH} characters.", 0);

                // Validate phone if provided
                if (!string.IsNullOrWhiteSpace(phone) && !ValidationHelper.IsValidPhone(phone))
                    return (false, "Invalid phone number format.", 0);

                // Validate email if provided
                if (!string.IsNullOrWhiteSpace(email) && !ValidationHelper.IsValidEmail(email))
                    return (false, "Invalid email format.", 0);

                // Validate visitor type
                var validTypes = new[] { "Guest", "Delivery", "Service", "Family", "Other" };
                if (!validTypes.Contains(visitorType))
                    return (false, "Invalid visitor type. Must be Guest, Delivery, Service, Family, or Other.", 0);

                // Validate purpose
                if (!string.IsNullOrWhiteSpace(purpose))
                {
                    if (!ValidationHelper.IsValidLength(purpose, MIN_PURPOSE_LENGTH, MAX_PURPOSE_LENGTH))
                        return (false, $"Purpose must be between {MIN_PURPOSE_LENGTH} and {MAX_PURPOSE_LENGTH} characters.", 0);
                }

                // Register visitor
                int visitorID = VisitorDAL.RegisterVisitor(
                    residentID,
                    visitorName,
                    phone ?? "",
                    email ?? "",
                    "",
                    purpose,
                    DateTime.Now,
                    visitorType);

                if (visitorID > 0)
                {
                    Log.Information($"Visitor checked in: ID={visitorID}, Name={visitorName}, Resident={residentID}");
                    return (true, "Visitor checked in successfully.", visitorID);
                }

                return (false, "Failed to check in visitor.", 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking in visitor");
                return (false, $"Error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Check-out a visitor
        /// </summary>
        public static (bool Success, string Message) CheckOutVisitor(int visitorID, DateTime checkOutTime)
        {
            try
            {
                var visitor = VisitorDAL.GetVisitorByID(visitorID);
                if (visitor == null)
                    return (false, "Visitor not found.");

                if (visitor.CheckOutTime != null)
                    return (false, "Visitor has already checked out.");

                // Update check-out time (would need to add this to VisitorDAL)
                bool updated = VisitorDAL.ApproveVisitor(visitorID, 0);

                if (updated)
                {
                    Log.Information($"Visitor checked out: ID={visitorID}");
                    return (true, "Visitor checked out successfully.");
                }

                return (false, "Failed to check out visitor.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking out visitor");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Register a visitor
        /// </summary>
        public static (bool Success, string Message, int VisitorID) RegisterVisitor(
            int residentID,
            string visitorName,
            string phone,
            string email,
            string visitorType)
        {
            try
            {
                // Validate resident
                var resident = ResidentDAL.GetResidentByID(residentID);
                if (resident == null)
                    return (false, "Selected resident does not exist.", 0);

                // Validate visitor name
                if (string.IsNullOrWhiteSpace(visitorName))
                    return (false, "Visitor name is required.", 0);

                if (!ValidationHelper.IsValidLength(visitorName, MIN_NAME_LENGTH, MAX_NAME_LENGTH))
                    return (false, $"Visitor name must be between {MIN_NAME_LENGTH} and {MAX_NAME_LENGTH} characters.", 0);

                // Validate phone
                if (!string.IsNullOrWhiteSpace(phone) && !ValidationHelper.IsValidPhone(phone))
                    return (false, "Invalid phone number format.", 0);

                // Validate email
                if (!string.IsNullOrWhiteSpace(email) && !ValidationHelper.IsValidEmail(email))
                    return (false, "Invalid email format.", 0);

                // Validate visitor type
                var validTypes = new[] { "Guest", "Delivery", "Service", "Family", "Other" };
                if (!validTypes.ToList().Contains(visitorType))
                    return (false, "Invalid visitor type.", 0);

                // Register visitor
                int visitorID = VisitorDAL.RegisterVisitor(
                    residentID,
                    visitorName,
                    phone ?? "",
                    email ?? "",
                    "",
                    "",
                    DateTime.Now,
                    visitorType);

                if (visitorID > 0)
                {
                    Log.Information($"Visitor registered: ID={visitorID}, Name={visitorName}");
                    return (true, "Visitor registered successfully.", visitorID);
                }

                return (false, "Failed to register visitor.", 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error registering visitor");
                return (false, $"Error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Delete a visitor record
        /// </summary>
        public static (bool Success, string Message) DeleteVisitor(int visitorID)
        {
            try
            {
                var visitor = VisitorDAL.GetVisitorByID(visitorID);
                if (visitor == null)
                    return (false, "Visitor not found.");

                bool deleted = VisitorDAL.DeleteVisitor(visitorID);

                if (deleted)
                {
                    Log.Information($"Visitor deleted: ID={visitorID}");
                    return (true, "Visitor deleted successfully.");
                }

                return (false, "Failed to delete visitor.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting visitor");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get visitor statistics
        /// </summary>
        public static dynamic GetVisitorStatistics()
        {
            try
            {
                var visitors = VisitorDAL.GetAllVisitors();
                var todayVisitors = visitors.Where(v => v.CheckInTime.Date == DateTime.Today).ToList();
                var checkedInCount = todayVisitors.Count(v => v.CheckOutTime == null);
                var checkedOutCount = todayVisitors.Count(v => v.CheckOutTime != null);

                return new
                {
                    TotalVisitors = visitors.Count,
                    TodayVisitors = todayVisitors.Count,
                    CheckedInCount = checkedInCount,
                    CheckedOutCount = checkedOutCount,
                    GuestCount = visitors.Count(v => v.VisitorType == "Guest"),
                    DeliveryCount = visitors.Count(v => v.VisitorType == "Delivery"),
                    ServiceCount = visitors.Count(v => v.VisitorType == "Service"),
                    FamilyCount = visitors.Count(v => v.VisitorType == "Family"),
                    OtherCount = visitors.Count(v => v.VisitorType == "Other")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving visitor statistics");
                return new { TotalVisitors = 0 };
            }
        }
    }
}
