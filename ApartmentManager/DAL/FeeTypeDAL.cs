using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for FeeType operations
/// </summary>
public class FeeTypeDAL
{
    /// <summary>
    /// Get fee type by ID
    /// </summary>
    public static dynamic? GetFeeTypeByID(int feeTypeID)
    {
        try
        {
            const string query = @"
                SELECT FeeTypeID, FeeTypeName, Description, UnitOfMeasurement, Status, CreatedAt, UpdatedAt
                FROM FeeTypes
                WHERE FeeTypeID = @FeeTypeID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FeeTypeID", feeTypeID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapFeeType(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting fee type by ID: {FeeTypeID}", feeTypeID);
            return null;
        }
    }

    /// <summary>
    /// Get all fee types
    /// </summary>
    public static List<dynamic> GetAllFeeTypes()
    {
        var feeTypes = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT FeeTypeID, FeeTypeName, Description, UnitOfMeasurement, Status, CreatedAt, UpdatedAt
                FROM FeeTypes
                ORDER BY FeeTypeName
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            feeTypes.Add(MapFeeType(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all fee types");
        }

        return feeTypes;
    }

    /// <summary>
    /// Get active fee types
    /// </summary>
    public static List<dynamic> GetActiveFeeTypes()
    {
        var feeTypes = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT FeeTypeID, FeeTypeName, Description, UnitOfMeasurement, Status, CreatedAt, UpdatedAt
                FROM FeeTypes
                WHERE Status = 'Active'
                ORDER BY FeeTypeName
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            feeTypes.Add(MapFeeType(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting active fee types");
        }

        return feeTypes;
    }

    /// <summary>
    /// Create fee type
    /// </summary>
    public static int CreateFeeType(string feeTypeName, string description, string unitOfMeasurement)
    {
        try
        {
            const string query = @"
                INSERT INTO FeeTypes (FeeTypeName, Description, UnitOfMeasurement, Status)
                VALUES (@FeeTypeName, @Description, @UnitOfMeasurement, 'Active')
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FeeTypeName", feeTypeName);
                    command.Parameters.AddWithValue("@Description", description);
                    command.Parameters.AddWithValue("@UnitOfMeasurement", unitOfMeasurement);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var feeTypeID = Convert.ToInt32(result);

                    Log.Information("Fee type created: {FeeTypeName} (ID: {FeeTypeID})", feeTypeName, feeTypeID);
                    return feeTypeID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating fee type: {FeeTypeName}", feeTypeName);
            throw;
        }
    }

    /// <summary>
    /// Update fee type
    /// </summary>
    public static bool UpdateFeeType(int feeTypeID, string feeTypeName, string description, string unitOfMeasurement)
    {
        try
        {
            const string query = @"
                UPDATE FeeTypes
                SET FeeTypeName = @FeeTypeName, Description = @Description, 
                    UnitOfMeasurement = @UnitOfMeasurement, UpdatedAt = GETDATE()
                WHERE FeeTypeID = @FeeTypeID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FeeTypeID", feeTypeID);
                    command.Parameters.AddWithValue("@FeeTypeName", feeTypeName);
                    command.Parameters.AddWithValue("@Description", description);
                    command.Parameters.AddWithValue("@UnitOfMeasurement", unitOfMeasurement);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Fee type updated: {FeeTypeID}", feeTypeID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating fee type: {FeeTypeID}", feeTypeID);
            return false;
        }
    }

    /// <summary>
    /// Update fee type status
    /// </summary>
    public static bool UpdateFeeTypeStatus(int feeTypeID, string status)
    {
        try
        {
            const string query = @"
                UPDATE FeeTypes
                SET Status = @Status, UpdatedAt = GETDATE()
                WHERE FeeTypeID = @FeeTypeID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FeeTypeID", feeTypeID);
                    command.Parameters.AddWithValue("@Status", status);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Fee type status updated: {FeeTypeID} to {Status}", feeTypeID, status);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating fee type status: {FeeTypeID}", feeTypeID);
            return false;
        }
    }

    /// <summary>
    /// Delete fee type (only if not used in invoices)
    /// </summary>
    public static bool DeleteFeeType(int feeTypeID)
    {
        try
        {
            const string query = "DELETE FROM FeeTypes WHERE FeeTypeID = @FeeTypeID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FeeTypeID", feeTypeID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Fee type deleted: {FeeTypeID}", feeTypeID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting fee type: {FeeTypeID}", feeTypeID);
            return false;
        }
    }

    /// <summary>
    /// Check if fee type name exists
    /// </summary>
    public static bool FeeTypeNameExists(string feeTypeName, int? excludeFeeTypeID = null)
    {
        try
        {
            string query = "SELECT COUNT(*) FROM FeeTypes WHERE FeeTypeName = @FeeTypeName";
            if (excludeFeeTypeID.HasValue)
                query += " AND FeeTypeID != @ExcludeFeeTypeID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FeeTypeName", feeTypeName);
                    if (excludeFeeTypeID.HasValue)
                        command.Parameters.AddWithValue("@ExcludeFeeTypeID", excludeFeeTypeID.Value);

                    connection.Open();
                    return (int)command.ExecuteScalar()! > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if fee type name exists: {FeeTypeName}", feeTypeName);
            return true;
        }
    }

    /// <summary>
    /// Map SqlDataReader to fee type object
    /// </summary>
    private static dynamic MapFeeType(SqlDataReader reader)
    {
        return new
        {
            FeeTypeID = reader.GetInt32(0),
            FeeTypeName = reader.GetString(1),
            Description = reader.GetString(2),
            UnitOfMeasurement = reader.GetString(3),
            Status = reader.GetString(4),
            CreatedAt = reader.GetDateTime(5),
            UpdatedAt = reader.GetDateTime(6)
        };
    }
}


