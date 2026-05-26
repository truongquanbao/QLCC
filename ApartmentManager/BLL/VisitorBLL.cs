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
        private const int MAX_GUEST_COUNT = 7;
        private const int MAX_STAY_DAYS = 5;

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
                    visitorType,
                    purpose,
                    DateTime.Now,
                    "");

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
        /// Backward-compatible check-in overload that resolves apartment from resident.
        /// </summary>
        public static (bool Success, string Message, int VisitorID) CheckInVisitor(
            int residentID,
            string visitorName,
            string phone,
            string email,
            string visitorType,
            string purpose)
        {
            try
            {
                var resident = ResidentDAL.GetResidentByID(residentID);
                if (resident == null)
                    return (false, "Selected resident does not exist.", 0);

                return CheckInVisitor(resident.ApartmentID, residentID, visitorName, phone, email, visitorType, purpose);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking in visitor (compatibility overload)");
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

                bool updated = VisitorDAL.RecordDeparture(visitorID, checkOutTime);

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
        /// Backward-compatible checkout overload that uses the current time.
        /// </summary>
        public static (bool Success, string Message) CheckOutVisitor(int visitorID)
        {
            return CheckOutVisitor(visitorID, DateTime.Now);
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
            => RegisterVisitor(residentID, visitorName, phone, email, "", visitorType, "Đăng ký khách ra vào", DateTime.Now, "");

        public static (bool Success, string Message, int VisitorID) RegisterVisitor(
            int residentID,
            string visitorName,
            string phone,
            string email,
            string idNumber,
            string visitorType,
            string purpose,
            DateTime arrivalTime,
            string note = "")
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

                // Validate ID number: CMND 9 digits or CCCD 12 digits
                string safeIdNumber = (idNumber ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(safeIdNumber))
                    return (false, "Vui lòng nhập CCCD / giấy tờ của khách.", 0);

                if (!safeIdNumber.All(char.IsDigit) || (safeIdNumber.Length != 9 && safeIdNumber.Length != 12))
                    return (false, "CCCD / giấy tờ không hợp lệ. Vui lòng nhập 9 hoặc 12 chữ số.", 0);

                // Validate visitor type
                var validTypes = new[] { "Guest", "Delivery", "Service", "Family", "Other" };
                if (!validTypes.ToList().Contains(visitorType))
                    return (false, "Invalid visitor type.", 0);

                int visitorID = VisitorDAL.RegisterVisitor(
                    residentID,
                    visitorName?.Trim() ?? "",
                    phone?.Trim() ?? "",
                    email?.Trim() ?? "",
                    safeIdNumber,
                    visitorType,
                    purpose?.Trim() ?? "",
                    arrivalTime,
                    note?.Trim() ?? "");

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



        public static (bool Success, string Message, int VisitorID) RegisterVisitor(
    int residentID,
    string visitorName,
    string phone,
    string email,
    string idNumber,
    string visitorType,
    string purpose,
    DateTime arrivalTime,
    DateTime expectedDepartureTime,
    int guestCount,
    string note = "")
        {
            try
            {
                var resident = ResidentDAL.GetResidentByID(residentID);
                if (resident == null)
                    return (false, "Không tìm thấy hồ sơ cư dân.", 0);

                if (string.IsNullOrWhiteSpace(visitorName))
                    return (false, "Vui lòng nhập tên khách.", 0);

                if (!ValidationHelper.IsValidLength(visitorName, MIN_NAME_LENGTH, MAX_NAME_LENGTH))
                    return (false, $"Tên khách phải từ {MIN_NAME_LENGTH} đến {MAX_NAME_LENGTH} ký tự.", 0);

                if (string.IsNullOrWhiteSpace(phone))
                    return (false, "Vui lòng nhập số điện thoại khách.", 0);

                if (!ValidationHelper.IsValidPhone(phone))
                    return (false, "Số điện thoại không hợp lệ.", 0);

                if (!string.IsNullOrWhiteSpace(email) && !ValidationHelper.IsValidEmail(email))
                    return (false, "Email không hợp lệ.", 0);

                string safeIdNumber = (idNumber ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(safeIdNumber))
                    return (false, "Vui lòng nhập CCCD / giấy tờ của khách.", 0);

                if (!safeIdNumber.All(char.IsDigit) || (safeIdNumber.Length != 9 && safeIdNumber.Length != 12))
                    return (false, "CCCD / giấy tờ không hợp lệ. Vui lòng nhập 9 hoặc 12 chữ số.", 0);

                var validTypes = new[] { "Guest", "Delivery", "Service", "Family", "Other" };
                if (!validTypes.Contains(visitorType))
                    return (false, "Loại khách không hợp lệ.", 0);

                if (string.IsNullOrWhiteSpace(purpose))
                    return (false, "Vui lòng nhập mục đích đến.", 0);

                if (!ValidationHelper.IsValidLength(purpose, MIN_PURPOSE_LENGTH, MAX_PURPOSE_LENGTH))
                    return (false, $"Mục đích phải từ {MIN_PURPOSE_LENGTH} đến {MAX_PURPOSE_LENGTH} ký tự.", 0);

                if (arrivalTime < DateTime.Now.AddMinutes(-5))
                    return (false, "Thời gian vào không được nhỏ hơn thời gian hiện tại.", 0);

                if (expectedDepartureTime <= arrivalTime)
                    return (false, "Thời gian ra dự kiến phải lớn hơn thời gian vào.", 0);

                if (expectedDepartureTime > arrivalTime.AddDays(MAX_STAY_DAYS))
                    return (false, $"Khách chỉ được đăng ký ở lại tối đa {MAX_STAY_DAYS} ngày.", 0);

                if (guestCount < 1 || guestCount > MAX_GUEST_COUNT)
                    return (false, $"Số lượng khách phải từ 1 đến {MAX_GUEST_COUNT} người.", 0);

                int visitorID = VisitorDAL.RegisterVisitor(
                    residentID,
                    visitorName?.Trim() ?? "",
                    phone?.Trim() ?? "",
                    email?.Trim() ?? "",
                    safeIdNumber,
                    visitorType,
                    purpose?.Trim() ?? "",
                    arrivalTime,
                    expectedDepartureTime,
                    guestCount,
                    note?.Trim() ?? "");

                if (visitorID > 0)
                {
                    Log.Information($"Resident visitor registered: ID={visitorID}, Name={visitorName}, Count={guestCount}");
                    return (true, "Đăng ký khách thành công.", visitorID);
                }

                return (false, "Không thể đăng ký khách.", 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error registering resident visitor");
                return (false, $"Lỗi: {ex.Message}", 0);
            }
        }

        public static (bool Success, string Message) UpdateResidentVisitor(
    int visitorID,
    int residentID,
    string visitorName,
    string phone,
    string email,
    string idNumber,
    string visitorType,
    string purpose,
    DateTime arrivalTime,
    DateTime expectedDepartureTime,
    int guestCount,
    string note = "")
        {
            try
            {
                if (visitorID <= 0)
                    return (false, "Phiếu khách không hợp lệ.");

                var visitor = VisitorDAL.GetVisitorByID(visitorID);
                if (visitor == null)
                    return (false, "Không tìm thấy phiếu khách.");

                if (visitor.ResidentID != residentID)
                    return (false, "Bạn không có quyền sửa phiếu khách này.");

                string currentStatus = visitor.Status?.ToString() ?? "";
                if (currentStatus != "Pending")
                    return (false, "Chỉ có thể sửa phiếu khách đang chờ duyệt.");

                var resident = ResidentDAL.GetResidentByID(residentID);
                if (resident == null)
                    return (false, "Không tìm thấy hồ sơ cư dân.");

                if (string.IsNullOrWhiteSpace(visitorName))
                    return (false, "Vui lòng nhập tên khách.");

                if (!ValidationHelper.IsValidLength(visitorName, MIN_NAME_LENGTH, MAX_NAME_LENGTH))
                    return (false, $"Tên khách phải từ {MIN_NAME_LENGTH} đến {MAX_NAME_LENGTH} ký tự.");

                if (string.IsNullOrWhiteSpace(phone))
                    return (false, "Vui lòng nhập số điện thoại khách.");

                if (!ValidationHelper.IsValidPhone(phone))
                    return (false, "Số điện thoại không hợp lệ.");

                if (!string.IsNullOrWhiteSpace(email) && !ValidationHelper.IsValidEmail(email))
                    return (false, "Email không hợp lệ.");

                string safeIdNumber = (idNumber ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(safeIdNumber))
                    return (false, "Vui lòng nhập CCCD / giấy tờ của khách.");

                if (!safeIdNumber.All(char.IsDigit) || (safeIdNumber.Length != 9 && safeIdNumber.Length != 12))
                    return (false, "CCCD / giấy tờ không hợp lệ. Vui lòng nhập 9 hoặc 12 chữ số.");

                var validTypes = new[] { "Guest", "Delivery", "Service", "Family", "Other" };
                if (!validTypes.Contains(visitorType))
                    return (false, "Loại khách không hợp lệ.");

                if (string.IsNullOrWhiteSpace(purpose))
                    return (false, "Vui lòng nhập mục đích đến.");

                if (!ValidationHelper.IsValidLength(purpose, MIN_PURPOSE_LENGTH, MAX_PURPOSE_LENGTH))
                    return (false, $"Mục đích phải từ {MIN_PURPOSE_LENGTH} đến {MAX_PURPOSE_LENGTH} ký tự.");

                if (arrivalTime < DateTime.Now.AddMinutes(-5))
                    return (false, "Thời gian vào không được nhỏ hơn thời gian hiện tại.");

                if (expectedDepartureTime <= arrivalTime)
                    return (false, "Thời gian ra dự kiến phải lớn hơn thời gian vào.");

                if (expectedDepartureTime > arrivalTime.AddDays(MAX_STAY_DAYS))
                    return (false, $"Khách chỉ được đăng ký ở lại tối đa {MAX_STAY_DAYS} ngày.");

                if (guestCount < 1 || guestCount > MAX_GUEST_COUNT)
                    return (false, $"Số lượng khách phải từ 1 đến {MAX_GUEST_COUNT} người.");

                bool updated = VisitorDAL.UpdateResidentVisitor(
                    visitorID,
                    residentID,
                    visitorName.Trim(),
                    phone.Trim(),
                    email?.Trim() ?? "",
                    safeIdNumber,
                    visitorType,
                    purpose.Trim(),
                    arrivalTime,
                    expectedDepartureTime,
                    guestCount,
                    note?.Trim() ?? "");

                return updated
                    ? (true, "Đã cập nhật phiếu đăng ký khách.")
                    : (false, "Không thể cập nhật phiếu khách. Phiếu có thể đã được duyệt hoặc không còn ở trạng thái chờ duyệt.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating resident visitor");
                return (false, $"Lỗi: {ex.Message}");
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

        public static (bool Success, string Message) ApproveVisitor(int visitorID, int userID)
        {
            try
            {
                if (visitorID <= 0)
                    return (false, "Invalid visitor ID.");

                var visitor = VisitorDAL.GetVisitorByID(visitorID);
                if (visitor == null)
                    return (false, "Visitor not found.");

                bool approved = VisitorDAL.ApproveVisitor(visitorID, userID);
                return approved
                    ? (true, "Visitor approved successfully.")
                    : (false, "Failed to approve visitor.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error approving visitor");
                return (false, $"Error: {ex.Message}");
            }
        }

        public static (bool Success, string Message) RejectVisitor(int visitorID, int userID)
        {
            try
            {
                if (visitorID <= 0)
                    return (false, "Invalid visitor ID.");

                var visitor = VisitorDAL.GetVisitorByID(visitorID);
                if (visitor == null)
                    return (false, "Visitor not found.");

                bool rejected = VisitorDAL.RejectVisitor(visitorID, userID);
                return rejected
                    ? (true, "Visitor rejected successfully.")
                    : (false, "Failed to reject visitor.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error rejecting visitor");
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
