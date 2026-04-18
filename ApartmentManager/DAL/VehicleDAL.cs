using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Vehicle operations
/// </summary>
public class VehicleDAL
{
    /// <summary>
    /// Get vehicle by ID
    /// </summary>
    public static dynamic? GetVehicleByID(int vehicleID)
    {
        try
        {
            const string query = @"
                SELECT v.VehicleID, v.ResidentID, r.FullName, v.VehicleType, v.LicensePlate,
                       v.Color, v.Brand, v.Status, v.Note, v.CreatedAt, v.UpdatedAt
                FROM Vehicles v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                WHERE v.VehicleID = @VehicleID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VehicleID", vehicleID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapVehicle(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting vehicle by ID: {VehicleID}", vehicleID);
            return null;
        }
    }

    /// <summary>
    /// Get vehicle by license plate
    /// </summary>
    public static dynamic? GetVehicleByLicensePlate(string licensePlate)
    {
        try
        {
            const string query = @"
                SELECT v.VehicleID, v.ResidentID, r.FullName, v.VehicleType, v.LicensePlate,
                       v.Color, v.Brand, v.Status, v.Note, v.CreatedAt, v.UpdatedAt
                FROM Vehicles v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                WHERE v.LicensePlate = @LicensePlate
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LicensePlate", licensePlate);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapVehicle(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting vehicle by license plate: {LicensePlate}", licensePlate);
            return null;
        }
    }

    /// <summary>
    /// Get vehicles by resident
    /// </summary>
    public static List<dynamic> GetVehiclesByResident(int residentID)
    {
        var vehicles = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT v.VehicleID, v.ResidentID, r.FullName, v.VehicleType, v.LicensePlate,
                       v.Color, v.Brand, v.Status, v.Note, v.CreatedAt, v.UpdatedAt
                FROM Vehicles v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                WHERE v.ResidentID = @ResidentID
                ORDER BY v.CreatedAt DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            vehicles.Add(MapVehicle(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting vehicles by resident: {ResidentID}", residentID);
        }

        return vehicles;
    }

    /// <summary>
    /// Get all vehicles
    /// </summary>
    public static List<dynamic> GetAllVehicles()
    {
        var vehicles = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT v.VehicleID, v.ResidentID, r.FullName, v.VehicleType, v.LicensePlate,
                       v.Color, v.Brand, v.Status, v.Note, v.CreatedAt, v.UpdatedAt
                FROM Vehicles v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                ORDER BY r.FullName, v.LicensePlate
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            vehicles.Add(MapVehicle(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all vehicles");
        }

        return vehicles;
    }

    /// <summary>
    /// Create vehicle
    /// </summary>
    public static int CreateVehicle(int residentID, string vehicleType, string licensePlate, 
                                    string color, string brand, string? note = null)
    {
        try
        {
            const string query = @"
                INSERT INTO Vehicles (ResidentID, VehicleType, LicensePlate, Color, Brand, Status, Note)
                VALUES (@ResidentID, @VehicleType, @LicensePlate, @Color, @Brand, 'Active', @Note)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    command.Parameters.AddWithValue("@VehicleType", vehicleType);
                    command.Parameters.AddWithValue("@LicensePlate", licensePlate);
                    command.Parameters.AddWithValue("@Color", color);
                    command.Parameters.AddWithValue("@Brand", brand);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var vehicleID = Convert.ToInt32(result);

                    Log.Information("Vehicle registered: {LicensePlate} (ID: {VehicleID})", licensePlate, vehicleID);
                    return vehicleID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating vehicle: {LicensePlate}", licensePlate);
            throw;
        }
    }

    /// <summary>
    /// Update vehicle
    /// </summary>
    public static bool UpdateVehicle(int vehicleID, string vehicleType, string color, string brand, string? note = null)
    {
        try
        {
            const string query = @"
                UPDATE Vehicles
                SET VehicleType = @VehicleType, Color = @Color, Brand = @Brand, Note = @Note, UpdatedAt = GETDATE()
                WHERE VehicleID = @VehicleID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VehicleID", vehicleID);
                    command.Parameters.AddWithValue("@VehicleType", vehicleType);
                    command.Parameters.AddWithValue("@Color", color);
                    command.Parameters.AddWithValue("@Brand", brand);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Vehicle updated: {VehicleID}", vehicleID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating vehicle: {VehicleID}", vehicleID);
            return false;
        }
    }

    /// <summary>
    /// Update vehicle status
    /// </summary>
    public static bool UpdateVehicleStatus(int vehicleID, string status)
    {
        try
        {
            const string query = @"
                UPDATE Vehicles
                SET Status = @Status, UpdatedAt = GETDATE()
                WHERE VehicleID = @VehicleID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VehicleID", vehicleID);
                    command.Parameters.AddWithValue("@Status", status);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Vehicle status updated: {VehicleID} to {Status}", vehicleID, status);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating vehicle status: {VehicleID}", vehicleID);
            return false;
        }
    }

    /// <summary>
    /// Delete vehicle
    /// </summary>
    public static bool DeleteVehicle(int vehicleID)
    {
        try
        {
            const string query = "DELETE FROM Vehicles WHERE VehicleID = @VehicleID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VehicleID", vehicleID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Vehicle deleted: {VehicleID}", vehicleID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting vehicle: {VehicleID}", vehicleID);
            return false;
        }
    }

    /// <summary>
    /// Check if license plate exists
    /// </summary>
    public static bool LicensePlateExists(string licensePlate, int? excludeVehicleID = null)
    {
        try
        {
            string query = "SELECT COUNT(*) FROM Vehicles WHERE LicensePlate = @LicensePlate";
            if (excludeVehicleID.HasValue)
                query += " AND VehicleID != @ExcludeVehicleID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LicensePlate", licensePlate);
                    if (excludeVehicleID.HasValue)
                        command.Parameters.AddWithValue("@ExcludeVehicleID", excludeVehicleID.Value);

                    connection.Open();
                    return (int)command.ExecuteScalar()! > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if license plate exists: {LicensePlate}", licensePlate);
            return true;
        }
    }

    /// <summary>
    /// Map SqlDataReader to vehicle object
    /// </summary>
    private static dynamic MapVehicle(SqlDataReader reader)
    {
        return new
        {
            VehicleID = reader.GetInt32(0),
            ResidentID = reader.GetInt32(1),
            FullName = reader.GetString(2),
            VehicleType = reader.GetString(3),
            LicensePlate = reader.GetString(4),
            Color = reader.GetString(5),
            Brand = reader.GetString(6),
            Status = reader.GetString(7),
            Note = reader.IsDBNull(8) ? null : reader.GetString(8),
            CreatedAt = reader.GetDateTime(9),
            UpdatedAt = reader.GetDateTime(10)
        };
    }
}


