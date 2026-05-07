using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ApartmentManager.Utilities;

/// <summary>
/// Helper class for database connections
/// </summary>
public static class DatabaseHelper
{
    private const string ConnectionName = "ApartmentManagerDB";
    private const string RegistryPath = @"Software\ApartmentManager\Database";
    private const string RegistryValueName = "ConnectionString";
    private static string? _runtimeConnectionString;

    /// <summary>
    /// Get the effective connection string used by the application.
    /// </summary>
    public static string GetConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(_runtimeConnectionString))
        {
            return _runtimeConnectionString!;
        }

        var savedConnectionString = GetSavedConnectionString();
        if (!string.IsNullOrWhiteSpace(savedConnectionString))
        {
            return savedConnectionString!;
        }

        var config = ConfigurationManager.ConnectionStrings[ConnectionName];
        if (!string.IsNullOrWhiteSpace(config?.ConnectionString))
        {
            return config.ConnectionString;
        }

        return BuildTrustedConnectionString(@".\SQLEXPRESS");
    }

    /// <summary>
    /// Create and return a new database connection.
    /// </summary>
    public static SqlConnection CreateConnection()
    {
        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string is not configured.");
        }

        return new SqlConnection(connectionString);
    }

    /// <summary>
    /// Set the current connection string in memory and optionally persist it for future runs.
    /// </summary>
    public static void SetConnectionString(string connectionString, bool persist = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));
        }

        _runtimeConnectionString = connectionString.Trim();

        if (persist)
        {
            _ = SaveConnectionString(_runtimeConnectionString, out _);
        }
    }

    /// <summary>
    /// Persist the current connection string for the current Windows user.
    /// </summary>
    public static bool SaveConnectionString(string connectionString, out string message)
    {
        try
        {
            using var registryKey = Registry.CurrentUser.CreateSubKey(RegistryPath);
            registryKey?.SetValue(RegistryValueName, connectionString);
            _runtimeConnectionString = connectionString;
            message = "Đã lưu cấu hình kết nối.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Không thể lưu cấu hình kết nối: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Test database connection.
    /// </summary>
    public static (bool success, string message) TestConnection(string? connectionString = null)
    {
        try
        {
            using var connection = new SqlConnection(NormalizeConnectionString(connectionString ?? GetConnectionString(), testMode: true));
            connection.Open();
            return (true, "Kết nối cơ sở dữ liệu thành công.");
        }
        catch (Exception ex)
        {
            return (false, BuildFriendlyErrorMessage(ex));
        }
    }

    /// <summary>
    /// Ensure the application has a working connection string. If the configured one fails,
    /// fall back to common local SQL Server instances when no user override was saved.
    /// </summary>
    public static (bool success, string message, string? connectionString) EnsureActiveConnection()
    {
        var currentConnectionString = GetConnectionString();
        var currentResult = TestConnection(currentConnectionString);
        if (currentResult.success)
        {
            return (true, currentResult.message, currentConnectionString);
        }

        if (HasSavedConnectionString())
        {
            return (false, currentResult.message, null);
        }

        foreach (var candidate in GetFallbackConnectionStrings(currentConnectionString))
        {
            var result = TestConnection(candidate);
            if (!result.success)
            {
                continue;
            }

            SetConnectionString(candidate, persist: true);
            var builder = new SqlConnectionStringBuilder(candidate);
            return (true, $"Đã tự động kết nối tới SQL Server `{builder.DataSource}`.", candidate);
        }

        return (false, currentResult.message, null);
    }

    private static bool HasSavedConnectionString()
    {
        return !string.IsNullOrWhiteSpace(GetSavedConnectionString());
    }

    private static string? GetSavedConnectionString()
    {
        try
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey(RegistryPath);
            return registryKey?.GetValue(RegistryValueName) as string;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> GetFallbackConnectionStrings(string currentConnectionString)
    {
        var builders = new List<SqlConnectionStringBuilder>();

        TryAddBuilder(builders, currentConnectionString);
        TryAddBuilder(builders, BuildTrustedConnectionString(@".\SQLEXPRESS"));
        TryAddBuilder(builders, BuildTrustedConnectionString(@".\SQLEXPRESS01"));
        TryAddBuilder(builders, BuildTrustedConnectionString(@"(localdb)\MSSQLLocalDB"));
        TryAddBuilder(builders, BuildTrustedConnectionString("localhost"));

        return builders
            .GroupBy(builder => $"{builder.DataSource}|{builder.InitialCatalog}|{builder.IntegratedSecurity}|{builder.UserID}")
            .Select(group => group.First().ConnectionString);
    }

    private static void TryAddBuilder(ICollection<SqlConnectionStringBuilder> builders, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = string.IsNullOrWhiteSpace(new SqlConnectionStringBuilder(connectionString).InitialCatalog)
                    ? "ApartmentManagerDB"
                    : new SqlConnectionStringBuilder(connectionString).InitialCatalog,
                Encrypt = false,
                TrustServerCertificate = true
            };

            builders.Add(builder);
        }
        catch
        {
        }
    }

    private static string NormalizeConnectionString(string connectionString, bool testMode)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            Encrypt = false,
            TrustServerCertificate = true
        };

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            builder.InitialCatalog = "ApartmentManagerDB";
        }

        if (testMode)
        {
            builder.ConnectTimeout = Math.Min(builder.ConnectTimeout <= 0 ? 15 : builder.ConnectTimeout, 5);
        }

        return builder.ConnectionString;
    }

    private static string BuildTrustedConnectionString(string dataSource)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = "ApartmentManagerDB",
            IntegratedSecurity = true,
            Encrypt = false,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }

    private static string BuildFriendlyErrorMessage(Exception exception)
    {
        if (exception is SqlException sqlException)
        {
            return sqlException.Number switch
            {
                4060 => "Không mở được database ApartmentManagerDB. Kiểm tra lại SQL Server instance hoặc quyền truy cập database.",
                18456 => "Không đăng nhập được vào SQL Server với thông tin hiện tại.",
                53 => "Không tìm thấy SQL Server. Kiểm tra lại tên server hoặc instance.",
                -1 => "Không thể kết nối tới SQL Server. Kiểm tra dịch vụ SQL Server đang chạy.",
                2 => "Kết nối SQL Server bị timeout.",
                _ => $"Kết nối cơ sở dữ liệu thất bại: {sqlException.Message}"
            };
        }

        return $"Kết nối cơ sở dữ liệu thất bại: {exception.Message}";
    }
}
