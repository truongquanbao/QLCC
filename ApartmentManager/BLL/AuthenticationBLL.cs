using System;
using System.Linq;
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
                return (false, "Tên đăng nhập hoặc mật khẩu không được để trống", null);

            RolePermissionDAL.EnsureRbacDefaults();

            // Allow login by username or email to match the UI label
            var user = UserDAL.GetUserByUsername(username);
            if (user == null && ValidationHelper.IsValidEmail(username))
            {
                user = UserDAL.GetUserByEmail(username);
            }
            if (user == null)
            {
                Log.Warning("Login failed: user not found - {Username}", username);
                return (false, "Tên đăng nhập hoặc mật khẩu không chính xác", null);
            }

            // Check if account is locked
            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.Now)
            {
                var timeLeft = user.LockedUntil.Value - DateTime.Now;
                var message = $"Tài khoản bị khóa. Vui lòng thử lại sau {(int)timeLeft.TotalMinutes} phút";
                Log.Warning("Login failed: account locked - {Username}", username);
                return (false, message, null);
            }

            // Check if account is not approved (for residents)
            if (!user.IsApproved && user.RoleName == "Resident")
            {
                Log.Warning("Login failed: account not approved - {Username}", username);
                return (false, "Tài khoản chưa được duyệt. Vui lòng chờ xác minh từ quản lý", null);
            }

            // Check if account is rejected
            if (user.Status == "Rejected")
            {
                Log.Warning("Login failed: account rejected - {Username}", username);
                return (false, "Tài khoản bị từ chối. Vui lòng liên hệ quản lý để biết thêm chi tiết", null);
            }

            // Check if account is inactive
            if (user.Status == "Inactive")
            {
                Log.Warning("Login failed: account inactive - {Username}", username);
                return (false, "Tài khoản đã bị vô hiệu hóa", null);
            }

            // Verify password
            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                UserDAL.UpdateLoginAttempt(user.UserID, false);
                AuditLogDAL.LogLogin(user.UserID, false, "Sai mật khẩu");
                Log.Warning("Login failed: wrong password - {Username}", username);
                return (false, "Tên đăng nhập hoặc mật khẩu không chính xác", null);
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
            return (true, "Đăng nhập thành công", session);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during login: {Username}", username);
            return (false, "Lỗi khi đăng nhập. Vui lòng thử lại", null);
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
                return (false, "Tên đăng nhập không hợp lệ (3-50 ký tự, chỉ chứa chữ, số, dấu gạch dưới)", null);

            if (UserDAL.UsernameExists(username))
                return (false, "Tên đăng nhập đã được sử dụng", null);

            if (!ValidationHelper.IsValidEmail(email))
                return (false, "Email không hợp lệ", null);

            if (UserDAL.EmailExists(email))
                return (false, "Email đã được sử dụng", null);

            if (!ValidationHelper.IsValidPhone(phone))
                return (false, "Số điện thoại không hợp lệ (định dạng: 09xxxxxxxxx)", null);

            if (!ValidationHelper.IsValidCCCD(cccd))
                return (false, "CCCD/CMND không hợp lệ", null);

            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordConfirm))
                return (false, "Mật khẩu không được bỏ trống", null);

            if (password != passwordConfirm)
                return (false, "Xác nhận mật khẩu không khớp", null);

            var passwordValidation = PasswordHasher.ValidatePasswordStrength(password);
            if (!passwordValidation.isValid)
                return (false, passwordValidation.message, null);

            // Hash password
            var passwordHash = PasswordHasher.HashPassword(password);

            RolePermissionDAL.EnsureRbacDefaults();
            var residentRole = RolePermissionDAL.GetAllRoles()
                .FirstOrDefault(r => string.Equals(r.RoleName, "Resident", StringComparison.OrdinalIgnoreCase));
            if (residentRole == null)
                return (false, "Không tìm thấy vai trò cư dân", null);

            // Create user account (Resident role, status = Pending)
            var userID = UserDAL.CreateUser(username, passwordHash, fullName, email, phone, residentRole.RoleID, status: "Pending");

            AuditLogDAL.LogAction(userID, "Register", "User", userID, "Đăng ký tài khoản cư dân");

            Log.Information("New resident registered: {Username} (ID: {UserID})", username, userID);
            return (true, "Đăng ký thành công! Tài khoản sẽ được xác minh bởi quản lý", userID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during registration: {Username}", username);
            return (false, "Lỗi khi đăng ký. Vui lòng thử lại", null);
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
                return (false, "Không tìm thấy người dùng");

            // Verify current password
            if (!PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash))
                return (false, "Mật khẩu hiện tại không chính xác");

            // Validate new password
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                return (false, "Mật khẩu mới không được để trống");

            if (newPassword != confirmPassword)
                return (false, "Xác nhận mật khẩu mới không khớp");

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
                return (true, "Mật khẩu đã được thay đổi thành công");
            }
            else
            {
                return (false, "Lỗi khi thay đổi mật khẩu");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error changing password for user: {UserID}", userID);
            return (false, "Lỗi khi thay đổi mật khẩu. Vui lòng thử lại");
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
                user = UserDAL.GetUserByPhone(emailOrPhone);
            }

            if (user == null)
                return (false, "Không tìm thấy tài khoản với email hoặc số điện thoại này");

            // TODO: Send reset email/SMS with reset token
            // For now, just log the request
            AuditLogDAL.LogAction(user.UserID, "Request_Password_Reset", "User", user.UserID);

            Log.Information("Password reset requested for user: {UserID}", user.UserID);
            return (true, "Yêu cầu đặt lại mật khẩu đã được ghi nhận");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error requesting password reset");
            return (false, "Lỗi khi yêu cầu đặt lại mật khẩu");
        }
    }

    /// <summary>
    /// Reset password from login screen after verifying account identity.
    /// </summary>
    public static (bool success, string message) ResetPasswordByIdentity(
        string usernameOrEmail,
        string recoveryContact,
        string newPassword,
        string confirmPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail))
                return (false, "Vui lòng nhập tên đăng nhập hoặc email");

            if (string.IsNullOrWhiteSpace(recoveryContact))
                return (false, "Vui lòng nhập email hoặc số điện thoại đã đăng ký");

            var normalizedIdentity = usernameOrEmail.Trim();
            var normalizedContact = recoveryContact.Trim();

            var user = ValidationHelper.IsValidEmail(normalizedIdentity)
                ? UserDAL.GetUserByEmail(normalizedIdentity)
                : UserDAL.GetUserByUsername(normalizedIdentity);

            if (user == null)
                return (false, "Không tìm thấy tài khoản phù hợp");

            if (!user.IsActive)
                return (false, "Tài khoản hiện không hoạt động");

            bool emailMatched = string.Equals(user.Email?.Trim(), normalizedContact, StringComparison.OrdinalIgnoreCase);
            bool phoneMatched = NormalizePhone(user.Phone) == NormalizePhone(normalizedContact);

            if (!emailMatched && !phoneMatched)
                return (false, "Thông tin xác minh không khớp với tài khoản");

            return ResetPassword(user.UserID, newPassword, confirmPassword);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resetting password by identity: {Identity}", usernameOrEmail);
            return (false, "Lỗi khi đặt lại mật khẩu");
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
                return (false, "Mật khẩu không được để trống");

            if (newPassword != confirmPassword)
                return (false, "Xác nhận mật khẩu không khớp");

            var passwordValidation = PasswordHasher.ValidatePasswordStrength(newPassword);
            if (!passwordValidation.isValid)
                return (false, passwordValidation.message);

            var passwordHash = PasswordHasher.HashPassword(newPassword);
            var success = UserDAL.UpdatePasswordHash(userID, passwordHash);

            if (success)
            {
                AuditLogDAL.LogAction(userID, "Reset_Password", "User", userID);
                Log.Information("Password reset for user: {UserID}", userID);
                return (true, "Mật khẩu đã được đặt lại thành công");
            }

            return (false, "Lỗi khi đặt lại mật khẩu");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resetting password for user: {UserID}", userID);
            return (false, "Lỗi khi đặt lại mật khẩu");
        }
    }

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        return phone.Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .Trim();
    }
}


