using System;
using Microsoft.Data.SqlClient;

using System.Configuration;
namespace ApartmentManager.Utilities;

/// <summary>
/// Helper class for database connections
/// </summary>
public static class DatabaseHelper
{
    private static readonly string? ConnectionString = GetConnectionString();

    /// <summary>
    /// Get connection string from app.config
    /// </summary>
    private static string? GetConnectionString()
    {
        try
        {
            // Try to read from app.config
            var config = System.Configuration.ConfigurationManager.ConnectionStrings["ApartmentManagerDB"];
            if (config != null)
                return config.ConnectionString;

            // Fallback to default LocalDB
            return "Server=(localdb)\\mssqllocaldb;Database=ApartmentManagerDB;Trusted_Connection=true;";
        }
        catch
        {
            return "Server=(localdb)\\mssqllocaldb;Database=ApartmentManagerDB;Trusted_Connection=true;";
        }
    }

    /// <summary>
    /// Create and return a new database connection
    /// </summary>
    public static SqlConnection CreateConnection()
    {
        if (string.IsNullOrEmpty(ConnectionString))
            throw new InvalidOperationException("Connection string is not configured");

        return new SqlConnection(ConnectionString);
    }

    /// <summary>
    /// Test database connection
    /// </summary>
    public static (bool success, string message) TestConnection()
    {
        try
        {
            using (var connection = CreateConnection())
            {
                connection.Open();
                return (true, "Database connection successful");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Database connection failed: {ex.Message}");
        }
    }
}

