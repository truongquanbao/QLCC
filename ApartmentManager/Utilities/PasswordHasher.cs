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
            return (false, "Password cannot be empty");

        if (password.Length < 8)
            return (false, "Password must be at least 8 characters long");

        if (!Regex.IsMatch(password, "[A-Z]"))
            return (false, "Password must contain at least one uppercase letter");

        if (!Regex.IsMatch(password, "[a-z]"))
            return (false, "Password must contain at least one lowercase letter");

        if (!Regex.IsMatch(password, "[0-9]"))
            return (false, "Password must contain at least one digit");

        if (!Regex.IsMatch(password, "[!@#$%^&*]"))
            return (false, "Password must contain at least one special character (!@#$%^&*)");

        return (true, "Password is strong");
    }
}
