using System;
using ApartmentManager.DTO;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.BLL;

/// <summary>
/// Business Logic Layer for Authentication
/// </summary>
public class AuthenticationBLL
{
    /// <summary>
    /// Login user
    /// </summary>
    public static (bool success, string message, UserSession? session) Login(string username, string password)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "TÃªn Ä‘Äƒng nháº­p hoáº·c máº­t kháº©u khÃ´ng Ä‘Æ°á»£c bá» trá»‘ng", null);

            // Get user from database
            var user = UserDAL.GetUserByUsername(username);
            if (user == null)
            {
                Log.Warning("Login failed: user not found - {Username}", username);
                return (false, "TÃªn Ä‘Äƒng nháº­p hoáº·c máº´t kháº©u khÃ´ng chÃ­nh xÃ¡c", null);
            }

            // Check if account is locked
            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.Now)
            {
                var timeLeft = user.LockedUntil.Value - DateTime.Now;
                var message = $"TÃ i khoáº£n bá»‹ khÃ³a. Vui lÃ²ng thá»­ láº¡i sau {(int)timeLeft.TotalMinutes} phÃºt";
                Log.Warning("Login failed: account locked - {Username}", username);
                return (false, message, null);
            }

            // Check if account is not approved (for residents)
            if (!user.IsApproved && user.RoleName == "Resident")
            {
                Log.Warning("Login failed: account not approved - {Username}", username);
                return (false, "TÃ i khoáº£n chÆ°a Ä‘Æ°á»£c duyá»‡t. Vui lÃ²ng chá» xÃ¡c minh tá»« quáº£n lÃ½", null);
            }

            // Check if account is rejected
            if (user.Status == "Rejected")
            {
                Log.Warning("Login failed: account rejected - {Username}", username);
                return (false, "TÃ i khoáº£n bá»‹ tá»« chá»‘i. Vui lÃ²ng liÃªn há»‡ quáº£n lÃ½ Ä‘á»ƒ biáº¿t thÃªm chi tiáº¿t", null);
            }

            // Check if account is inactive
            if (user.Status == "Inactive")
            {
                Log.Warning("Login failed: account inactive - {Username}", username);
                return (false, "TÃ i khoáº£n Ä‘Ã£ bá»‹ vÃ´ hiá»‡u hÃ³a", null);
            }

            // Verify password
            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                UserDAL.UpdateLoginAttempt(user.UserID, false);
                AuditLogDAL.LogLogin(user.UserID, false, "Sai máº­t kháº©u");
                Log.Warning("Login failed: wrong password - {Username}", username);
                return (false, "TÃªn Ä‘Äƒng nháº­p hoáº·c máº´t kháº©u khÃ´ng chÃ­nh xÃ¡c", null);
            }

            // Update login success
            UserDAL.UpdateLoginAttempt(user.UserID, true);
            AuditLogDAL.LogLogin(user.UserID, true);

            // Get user permissions
            var permissions = RolePermissionDAL.GetPermissionNamesForRole(user.RoleID);

            // Create session
            var session = new UserSession
            {
                UserID = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                RoleID = user.RoleID,
                RoleName = user.RoleName,
                Email = user.Email,
                AvatarPath = user.AvatarPath,
                Permissions = permissions,
                LoginTime = DateTime.Now
            };

            SessionManager.SetSession(session);

            Log.Information("User logged in successfully: {Username} ({RoleName})", username, user.RoleName);
            return (true, "ÄÄƒng nháº­p thÃ nh cÃ´ng", session);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during login: {Username}", username);
            return (false, "Lá»—i khi Ä‘Äƒng nháº­p. Vui lÃ²ng thá»­ láº¡i", null);
        }
    }

    /// <summary>
    /// Register new resident account
    /// </summary>
    public static (bool success, string message, int? userID) RegisterResident(
        string username, string password, string passwordConfirm,
        string fullName, string email, string phone, string cccd)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(username) || !ValidationHelper.IsValidUsername(username))
                return (false, "TÃªn Ä‘Äƒng nháº­p khÃ´ng há»£p lá»‡ (3-50 kÃ½ tá»±, chá»‰ chá»©a chá»¯, sá»‘, dáº¥u gáº¡ch dÆ°á»›i)", null);

            if (UserDAL.UsernameExists(username))
                return (false, "TÃªn Ä‘Äƒng nháº­p Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng", null);

            if (!ValidationHelper.IsValidEmail(email))
                return (false, "Email khÃ´ng há»£p lá»‡", null);

            if (UserDAL.EmailExists(email))
                return (false, "Email Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng", null);

            if (!ValidationHelper.IsValidPhone(phone))
                return (false, "Sá»‘ Ä‘iá»‡n thoáº¡i khÃ´ng há»£p lá»‡ (Ä‘á»‹nh dáº¡ng: 09xxxxxxxxx)", null);

            if (!ValidationHelper.IsValidCCCD(cccd))
                return (false, "CCCD/CMND khÃ´ng há»£p lá»‡", null);

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordConfirm))
                return (false, "Máº­t kháº©u khÃ´ng Ä‘Æ°á»£c bá» trá»‘ng", null);

            if (password != passwordConfirm)
                return (false, "XÃ¡c nháº­n máº­t kháº©u khÃ´ng khá»›p", null);

            var passwordValidation = PasswordHasher.ValidatePasswordStrength(password);
            if (!passwordValidation.isValid)
                return (false, passwordValidation.message, null);

            // Hash password
            var passwordHash = PasswordHasher.HashPassword(password);

            // Create user account (Resident role = 3, status = Pending)
            var userID = UserDAL.CreateUser(username, passwordHash, fullName, email, phone, roleID: 3, status: "Pending");

            AuditLogDAL.LogAction(userID, "Register", "User", userID, "ÄÄƒng kÃ½ tÃ i khoáº£n cÆ° dÃ¢n");

            Log.Information("New resident registered: {Username} (ID: {UserID})", username, userID);
            return (true, "ÄÄƒng kÃ½ thÃ nh cÃ´ng! TÃ i khoáº£n sáº½ Ä‘Æ°á»£c xÃ¡c minh bá»Ÿi quáº£n lÃ½", userID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during registration: {Username}", username);
            return (false, "Lá»—i khi Ä‘Äƒng kÃ½. Vui lÃ²ng thá»­ láº¡i", null);
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    public static void Logout()
    {
        try
        {
            if (SessionManager.IsLoggedIn())
            {
                var userID = SessionManager.GetCurrentUserID() ?? 0;
                AuditLogDAL.LogLogout(userID);
                Log.Information("User logged out: {UserID}", userID);
            }

            SessionManager.ClearSession();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during logout");
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    public static (bool success, string message) ChangePassword(int userID, string currentPassword, 
                                                                 string newPassword, string confirmPassword)
    {
        try
        {
            // Get current user
            var user = UserDAL.GetUserByID(userID);
            if (user == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng");

            // Verify current password
            if (!PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash))
                return (false, "Máº­t kháº©u hiá»‡n táº¡i khÃ´ng chÃ­nh xÃ¡c");

            // Validate new password
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                return (false, "Máº­t kháº©u má»›i khÃ´ng Ä‘Æ°á»£c bá» trá»‘ng");

            if (newPassword != confirmPassword)
                return (false, "XÃ¡c nháº­n máº­t kháº©u má»›i khÃ´ng khá»›p");

            var passwordValidation = PasswordHasher.ValidatePasswordStrength(newPassword);
            if (!passwordValidation.isValid)
                return (false, passwordValidation.message);

            // Update password
            var passwordHash = PasswordHasher.HashPassword(newPassword);
            var success = UserDAL.UpdatePasswordHash(userID, passwordHash);

            if (success)
            {
                AuditLogDAL.LogAction(userID, "Change_Password", "User", userID);
                Log.Information("Password changed for user: {UserID}", userID);
                return (true, "Máº­t kháº©u Ä‘Ã£ Ä‘Æ°á»£c thay Ä‘á»•i thÃ nh cÃ´ng");
            }
            else
            {
                return (false, "Lá»—i khi thay Ä‘á»•i máº­t kháº©u");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error changing password for user: {UserID}", userID);
            return (false, "Lá»—i khi thay Ä‘á»•i máº´t kháº©u. Vui lÃ²ng thá»­ láº¡i");
        }
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    public static (bool success, string message) RequestPasswordReset(string emailOrPhone)
    {
        try
        {
            // Find user by email or phone
            UserDTO? user = null;

            if (ValidationHelper.IsValidEmail(emailOrPhone))
                user = UserDAL.GetUserByEmail(emailOrPhone);
            else if (ValidationHelper.IsValidPhone(emailOrPhone))
            {
                // For now, we don't have a GetUserByPhone method, but can add it later
                // Return generic message for security
            }

            if (user == null)
                return (false, "KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n vá»›i email hoáº·c sá»‘ Ä‘iá»‡n thoáº¡i nÃ y");

            // TODO: Send reset email/SMS with reset token
            // For now, just log the request
            AuditLogDAL.LogAction(user.UserID, "Request_Password_Reset", "User", user.UserID);

            Log.Information("Password reset requested for user: {UserID}", user.UserID);
            return (true, "HÆ°á»›ng dáº«n Ä‘áº·t láº¡i máº­t kháº©u Ä‘Ã£ Ä‘Æ°á»£c gá»­i Ä‘áº¿n email/SMS cá»§a báº¡n");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error requesting password reset");
            return (false, "Lá»—i khi yÃªu cáº§u Ä‘áº·t láº¡i máº´t kháº©u");
        }
    }

    /// <summary>
    /// Reset password with token (simplified)
    /// </summary>
    public static (bool success, string message) ResetPassword(int userID, string newPassword, string confirmPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                return (false, "Máº´t kháº©u khÃ´ng Ä‘Æ°á»£c bá» trá»‘ng");

            if (newPassword != confirmPassword)
                return (false, "XÃ¡c nháº­n máº´t kháº©u khÃ´ng khá»›p");

            var passwordValidation = PasswordHasher.ValidatePasswordStrength(newPassword);
            if (!passwordValidation.isValid)
                return (false, passwordValidation.message);

            var passwordHash = PasswordHasher.HashPassword(newPassword);
            var success = UserDAL.UpdatePasswordHash(userID, passwordHash);

            if (success)
            {
                AuditLogDAL.LogAction(userID, "Reset_Password", "User", userID);
                Log.Information("Password reset for user: {UserID}", userID);
                return (true, "Máº´t kháº©u Ä‘Ã£ Ä‘Æ°á»£c Ä‘áº·t láº¡i thÃ nh cÃ´ng");
            }

            return (false, "Lá»—i khi Ä‘áº·t láº¡i máº´t kháº©u");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resetting password for user: {UserID}", userID);
            return (false, "Lá»—i khi Ä‘áº·t láº¡i máº´t kháº©u");
        }
    }
}


