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
                return (false, "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng");

            if (user.IsApproved)
                return (false, "TÃ i khoáº£n Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t");

            var success = UserDAL.ApproveUser(userID, approvedBy);
            if (success)
            {
                AuditLogDAL.LogAction(approvedBy, "Approve_User", "User", userID, $"Duyá»‡t tÃ i khoáº£n: {user.Username}");
                Log.Information("User approved: {UserID} by {ApprovedBy}", userID, approvedBy);
                return (true, "TÃ i khoáº£n Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t thÃ nh cÃ´ng");
            }

            return (false, "Lá»—i khi duyá»‡t tÃ i khoáº£n");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error approving user: {UserID}", userID);
            return (false, "Lá»—i khi duyá»‡t tÃ i khoáº£n");
        }
    }

    /// <summary>
    /// Reject user account
    /// </summary>
    public static (bool success, string message) RejectUser(int userID, int rejectedBy, string reason = "")
    {
        try
        {
            var user = UserDAL.GetUserByID(userID);
            if (user == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng");

            // TODO: Implement rejection logic in DAL
            AuditLogDAL.LogAction(rejectedBy, "Reject_User", "User", userID, 
                $"Tá»« chá»‘i tÃ i khoáº£n: {user.Username}. LÃ½ do: {reason}");

            Log.Information("User rejected: {UserID} by {RejectedBy}", userID, rejectedBy);
            return (true, "TÃ i khoáº£n Ä‘Ã£ bá»‹ tá»« chá»‘i");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error rejecting user: {UserID}", userID);
            return (false, "Lá»—i khi tá»« chá»‘i tÃ i khoáº£n");
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
                return (false, "TÃªn Ä‘Äƒng nháº­p khÃ´ng há»£p lá»‡", null);

            if (UserDAL.UsernameExists(username))
                return (false, "TÃªn Ä‘Äƒng nháº­p Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng", null);

            if (!ValidationHelper.IsValidEmail(email))
                return (false, "Email khÃ´ng há»£p lá»‡", null);

            if (UserDAL.EmailExists(email))
                return (false, "Email Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng", null);

            if (!ValidationHelper.IsValidPhone(phone))
                return (false, "Sá»‘ Ä‘iá»‡n thoáº¡i khÃ´ng há»£p lá»‡", null);

            // Create user with Manager role (2), Active status
            var passwordHash = PasswordHasher.HashPassword(tempPassword);
            var userID = UserDAL.CreateUser(username, passwordHash, fullName, email, phone, roleID: 2, status: "Active");

            AuditLogDAL.LogAction(SessionManager.GetCurrentUserID(), "Create_Manager", "User", userID, 
                $"Táº¡o tÃ i khoáº£n quáº£n lÃ½: {username}");

            Log.Information("Manager account created: {Username} (ID: {UserID})", username, userID);
            return (true, "TÃ i khoáº£n quáº£n lÃ½ Ä‘Ã£ Ä‘Æ°á»£c táº¡o thÃ nh cÃ´ng", userID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating manager account: {Username}", username);
            return (false, "Lá»—i khi táº¡o tÃ i khoáº£n quáº£n lÃ½", null);
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
    public static (bool success, string message) LockUserAccount(int userID, int lockedBy, string reason = "")
    {
        try
        {
            // TODO: Implement lock logic in DAL
            AuditLogDAL.LogAction(lockedBy, "Lock_Account", "User", userID, 
                $"KhÃ³a tÃ i khoáº£n. LÃ½ do: {reason}");

            Log.Information("User account locked: {UserID} by {LockedBy}", userID, lockedBy);
            return (true, "TÃ i khoáº£n Ä‘Ã£ bá»‹ khÃ³a");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error locking user account: {UserID}", userID);
            return (false, "Lá»—i khi khÃ³a tÃ i khoáº£n");
        }
    }

    /// <summary>
    /// Unlock user account
    /// </summary>
    public static (bool success, string message) UnlockUserAccount(int userID, int unlockedBy, string reason = "")
    {
        try
        {
            // TODO: Implement unlock logic in DAL
            AuditLogDAL.LogAction(unlockedBy, "Unlock_Account", "User", userID, 
                $"Má»Ÿ khÃ³a tÃ i khoáº£n. LÃ½ do: {reason}");

            Log.Information("User account unlocked: {UserID} by {UnlockedBy}", userID, unlockedBy);
            return (true, "TÃ i khoáº£n Ä‘Ã£ Ä‘Æ°á»£c má»Ÿ khÃ³a");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error unlocking user account: {UserID}", userID);
            return (false, "Lá»—i khi má»Ÿ khÃ³a tÃ i khoáº£n");
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


