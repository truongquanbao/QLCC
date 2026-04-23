using System;
using System.Text.RegularExpressions;

namespace ApartmentManager.Utilities;

/// <summary>
/// Helper class for input validation
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validate username format
    /// </summary>
    public static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        if (username.Length < 3 || username.Length > 50)
            return false;

        // Only alphanumeric and underscore allowed
        return Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
    }

    /// <summary>
    /// Validate email format
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate phone number format (Vietnamese)
    /// </summary>
    public static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Remove common separators
        var cleaned = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        // Vietnamese phone: 10 digits starting with 0, or +84
        return Regex.IsMatch(cleaned, @"^(\+84|0)\d{9}$");
    }

    /// <summary>
    /// Validate CCCD (National ID) format
    /// </summary>
    public static bool IsValidCCCD(string cccd)
    {
        if (string.IsNullOrWhiteSpace(cccd))
            return false;

        var cleaned = cccd.Replace(" ", "").Replace("-", "");

        // CCCD: 9 or 12 digits
        return Regex.IsMatch(cleaned, @"^\d{9}$|^\d{12}$");
    }

    /// <summary>
    /// Validate birth date (age >= 18)
    /// </summary>
    public static bool IsValidBirthDate(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;

        if (birthDate.Date > today.AddYears(-age))
            age--;

        return age >= 18 && birthDate < today;
    }

    /// <summary>
    /// Validate license plate format
    /// </summary>
    public static bool IsValidLicensePlate(string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
            return false;

        var cleaned = licensePlate.Replace(" ", "").ToUpper();

        // Vietnamese format: 2 digits - 4 digits - 2 letters (e.g., 30-12345-AB)
        // Also accepts: XX-0000-XX format
        return Regex.IsMatch(cleaned, @"^\d{2}-\d{4,5}-[A-Z]{2}$") ||
               Regex.IsMatch(cleaned, @"^[A-Z]{2}\d{1,3}[A-Z]{2}\d{3,4}$"); // Alternative format
    }

    /// <summary>
    /// Validate positive decimal number
    /// </summary>
    public static bool IsValidAmount(decimal amount)
    {
        return amount > 0 && amount <= decimal.MaxValue;
    }

    /// <summary>
    /// Validate age >= 18
    /// </summary>
    public static bool IsValidAge(DateTime dateOfBirth)
    {
        return IsValidBirthDate(dateOfBirth);
    }

    /// <summary>
    /// Validate age value directly.
    /// </summary>
    public static bool IsValidAge(int age)
    {
        return age >= 18 && age <= 120;
    }

    /// <summary>
    /// Validate string length
    /// </summary>
    public static bool IsValidLength(string value, int minLength, int maxLength)
    {
        if (value == null)
            return minLength == 0;

        return value.Length >= minLength && value.Length <= maxLength;
    }

    /// <summary>
    /// Validate that date is not in future
    /// </summary>
    public static bool IsNotFutureDate(DateTime date)
    {
        return date <= DateTime.Now;
    }

    /// <summary>
    /// Validate that date is not in past
    /// </summary>
    public static bool IsNotPastDate(DateTime date)
    {
        return date >= DateTime.Now;
    }

    /// <summary>
    /// Validate end date is after start date
    /// </summary>
    public static bool IsValidDateRange(DateTime startDate, DateTime endDate)
    {
        return endDate > startDate;
    }

    /// <summary>
    /// Validate numeric value within range
    /// </summary>
    public static bool IsInRange(int value, int min, int max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// Validate year is valid
    /// </summary>
    public static bool IsValidYear(int year)
    {
        return year >= 1900 && year <= DateTime.Now.Year + 100;
    }
}
