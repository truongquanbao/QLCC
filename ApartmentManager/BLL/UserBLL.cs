using System;
using System.Collections.Generic;
using ApartmentManager.DTO;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System.Linq;

namespace ApartmentManager.BLL;

/// <summary>
/// Business Logic Layer for User Management
/// </summary>
public class UserBLL
{
    /// <summary>
    /// Get user by ID
    /// </summary>
    public static UserDTO? GetUserByID(int userID)
    {
        return UserDAL.GetUserByID(userID);
    }

    /// <summary>
    /// Get all users
    /// </summary>
    public static List<UserDTO> GetAllUsers()
    {
        return UserDAL.GetAllUsers();
    }

    /// <summary>
    /// Get all pending approval users (Residents)
    /// </summary>
    public static List<UserDTO> GetPendingApprovalUsers()
    {
        var allUsers = UserDAL.GetAllUsers();
        return allUsers.Where(u => !u.IsApproved && u.Status == "Pending" && u.RoleName == "Resident").ToList();
    }

    /// <summary>
    /// Get pending approval count
    /// </summary>
    public static int GetPendingApprovalCount()
    {
        return GetPendingApprovalUsers().Count;
    }

    /// <summary>
    /// Approve user account (Super Admin / Manager)
    /// </summary>
    public static (bool success, string message) ApproveUser(int userID, int approvedBy)
    {
        try
        {
            var user = UserDAL.GetUserByID(userID);
            if (user == null)
                return (false, "Không tìm thấy người dùng");

            if (user.IsApproved)
                return (false, "Tài khoản đã được duyệt");

            var success = UserDAL.ApproveUser(userID, approvedBy);
            if (success)
            {
                AuditLogDAL.LogAction(approvedBy, "Approve_User", "User", userID, $"Duyệt tài khoản: {user.Username}");
                Log.Information("User approved: {UserID} by {ApprovedBy}", userID, approvedBy);
                return (true, "Tài khoản đã được duyệt thành công");
            }

            return (false, "Lỗi khi duyệt tài khoản");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error approving user: {UserID}", userID);
            return (false, "Lỗi khi duyệt tài khoản");
        }
    }

    /// <summary>
    /// Reject user account
    /// </summary>
    public static (bool success, string message) RejectUser(int userID, int? rejectedBy, string reason = "")
    {
        try
        {
            var user = UserDAL.GetUserByID(userID);
            if (user == null)
                return (false, "Không tìm thấy người dùng");

            var success = UserDAL.RejectUser(userID);
            if (!success)
                return (false, "Không thể từ chối tài khoản");

            AuditLogDAL.LogAction(rejectedBy, "Reject_User", "User", userID, 
                $"Từ chối tài khoản: {user.Username}. Lý do: {reason}");

            Log.Information("User rejected: {UserID} by {RejectedBy}", userID, rejectedBy);
            return (true, "Tài khoản đã bị từ chối");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error rejecting user: {UserID}", userID);
            return (false, "Lỗi khi từ chối tài khoản");
        }
    }

    /// <summary>
    /// Create manager account (Super Admin only)
    /// </summary>
    public static (bool success, string message, int? userID) CreateManagerAccount(
        string username, string fullName, string email, string phone, string tempPassword)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(username) || !ValidationHelper.IsValidUsername(username))
                return (false, "Tên đăng nhập không hợp lệ", null);

            if (UserDAL.UsernameExists(username))
                return (false, "Tên đăng nhập đã được sử dụng", null);

            if (!ValidationHelper.IsValidEmail(email))
                return (false, "Email không hợp lệ", null);

            if (UserDAL.EmailExists(email))
                return (false, "Email đã được sử dụng", null);

            if (!ValidationHelper.IsValidPhone(phone))
                return (false, "Số điện thoại không hợp lệ", null);

            // Create user with Manager role (2), Active status
            var passwordHash = PasswordHasher.HashPassword(tempPassword);
            var userID = UserDAL.CreateUser(username, passwordHash, fullName, email, phone, roleID: 2, status: "Active");

            AuditLogDAL.LogAction(SessionManager.GetCurrentUserID(), "Create_Manager", "User", userID, 
                $"Tạo tài khoản quản lý: {username}");

            Log.Information("Manager account created: {Username} (ID: {UserID})", username, userID);
            return (true, "Tài khoản quản lý đã được tạo thành công", userID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating manager account: {Username}", username);
            return (false, "Lỗi khi tạo tài khoản quản lý", null);
        }
    }

    /// <summary>
    /// Check if user has specific permission
    /// </summary>
    public static bool UserHasPermission(int userID, string permissionName)
    {
        return RolePermissionDAL.UserHasPermission(userID, permissionName);
    }

    /// <summary>
    /// Lock user account
    /// </summary>
    public static (bool success, string message) LockUserAccount(int userID, int? lockedBy, string reason = "")
    {
        try
        {
            var user = UserDAL.GetUserByID(userID);
            if (user == null)
                return (false, "Không tìm thấy người dùng");

            if (string.Equals(user.Status, "Inactive", StringComparison.OrdinalIgnoreCase) &&
                user.LockedUntil.HasValue &&
                user.LockedUntil.Value > DateTime.Now)
            {
                return (false, "Tài khoản đang bị khóa");
            }

            var lockedUntil = DateTime.Now.AddYears(10);
            if (!UserDAL.LockUser(userID, lockedUntil))
                return (false, "Không thể khóa tài khoản");

            AuditLogDAL.LogAction(lockedBy, "Lock_Account", "User", userID, 
                $"Khóa tài khoản: {user.Username}. Lý do: {reason}");

            Log.Information("User account locked: {UserID} by {LockedBy}", userID, lockedBy);
            return (true, "Tài khoản đã bị khóa");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error locking user account: {UserID}", userID);
            return (false, "Lỗi khi khóa tài khoản");
        }
    }

    /// <summary>
    /// Unlock user account
    /// </summary>
    public static (bool success, string message) UnlockUserAccount(int userID, int? unlockedBy, string reason = "")
    {
        try
        {
            var user = UserDAL.GetUserByID(userID);
            if (user == null)
                return (false, "Không tìm thấy người dùng");

            string restoredStatus = user.IsApproved || !string.Equals(user.RoleName, "Resident", StringComparison.OrdinalIgnoreCase)
                ? "Active"
                : "Pending";

            if (!UserDAL.UnlockUser(userID, restoredStatus))
                return (false, "Không thể mở khóa tài khoản");

            AuditLogDAL.LogAction(unlockedBy, "Unlock_Account", "User", userID, 
                $"Mở khóa tài khoản: {user.Username}. Lý do: {reason}");

            Log.Information("User account unlocked: {UserID} by {UnlockedBy}", userID, unlockedBy);
            return (true, "Tài khoản đã được mở khóa");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error unlocking user account: {UserID}", userID);
            return (false, "Lỗi khi mở khóa tài khoản");
        }
    }

    /// <summary>
    /// Get pending approval users for dashboard
    /// </summary>
    public static List<UserDTO> GetDashboardPendingUsers(int limit = 5)
    {
        var pending = GetPendingApprovalUsers();
        return pending.Take(limit).ToList();
    }
}


