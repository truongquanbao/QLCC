using System;
using System.Configuration;

namespace ApartmentManager.Utilities;

/// <summary>
/// Helper class for configuration management
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Get connection string from configuration
    /// </summary>
    public static string GetConnectionString(string name = "DefaultConnection")
    {
        return ConfigurationManager.ConnectionStrings[name]?.ConnectionString ?? "";
    }

    /// <summary>
    /// Get app setting value
    /// </summary>
    public static string GetAppSetting(string key)
    {
        return ConfigurationManager.AppSettings[key] ?? "";
    }

    /// <summary>
    /// Get app setting as integer
    /// </summary>
    public static int GetAppSettingAsInt(string key, int defaultValue = 0)
    {
        var value = ConfigurationManager.AppSettings[key];
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Get app setting as boolean
    /// </summary>
    public static bool GetAppSettingAsBool(string key, bool defaultValue = false)
    {
        var value = ConfigurationManager.AppSettings[key];
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
}
