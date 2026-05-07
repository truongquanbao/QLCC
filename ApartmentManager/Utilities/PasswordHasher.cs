using System;
using System.Text.RegularExpressions;

namespace ApartmentManager.Utilities;

/// <summary>
/// Password hashing utility using BCrypt
/// </summary>
public static class PasswordHasher
{
    /// <summary>
    /// Hash a password using BCrypt
    /// </summary>
    public static string HashPassword(string password)
    {
        try
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error hashing password", ex);
        }
    }

    /// <summary>
    /// Verify password against hash
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate password strength
    /// </summary>
    public static (bool isValid, string message) ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Mật khẩu không được để trống");

        if (password.Length < 8)
            return (false, "Mật khẩu phải có ít nhất 8 ký tự");

        if (!Regex.IsMatch(password, "[A-Z]"))
            return (false, "Mật khẩu phải có ít nhất 1 chữ in hoa");

        if (!Regex.IsMatch(password, "[a-z]"))
            return (false, "Mật khẩu phải có ít nhất 1 chữ thường");

        if (!Regex.IsMatch(password, "[0-9]"))
            return (false, "Mật khẩu phải có ít nhất 1 chữ số");

        if (!Regex.IsMatch(password, "[!@#$%^&*]"))
            return (false, "Mật khẩu phải có ít nhất 1 ký tự đặc biệt (!@#$%^&*)");

        return (true, "Mật khẩu hợp lệ");
    }
}
