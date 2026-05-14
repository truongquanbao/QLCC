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
    private const string VehicleSelect = @"
                SELECT v.VehicleID, v.ResidentID, r.FullName, v.VehicleType, v.LicensePlate,
                       v.Color, v.Brand, v.Status, v.Note, v.CreatedAt, v.UpdatedAt,
                       r.Phone, r.ApartmentID, a.ApartmentCode, b.BuildingName, bl.BlockName, f.FloorNumber
                FROM Vehicles v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                LEFT JOIN Apartments a ON r.ApartmentID = a.ApartmentID
                LEFT JOIN Floors f ON a.FloorID = f.FloorID
                LEFT JOIN Blocks bl ON f.BlockID = bl.BlockID
                LEFT JOIN Buildings b ON bl.BuildingID = b.BuildingID
            ";

    /// <summary>
    /// Get vehicle by ID
    /// </summary>
    public static dynamic? GetVehicleByID(int vehicleID)
    {
        try
        {
            const string query = VehicleSelect + @"
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
            const string query = VehicleSelect + @"
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
            const string query = VehicleSelect + @"
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
            const string query = VehicleSelect + @"
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
                                    string color, string brand, string note = null)
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
    /// Backward-compatible vehicle creation overload.
    /// </summary>
    public static int CreateVehicle(int residentID, string licensePlate, string vehicleType,
                                    string brand, string model, int year, string color)
    {
        string compatibilityNote = $"MODEL={model};YEAR={year};NOTE=";
        return CreateVehicle(residentID, vehicleType, licensePlate, color, brand, compatibilityNote);
    }

    /// <summary>
    /// Update vehicle
    /// </summary>
    public static bool UpdateVehicle(int vehicleID, string vehicleType, string color, string brand, string note = null)
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
    /// Update vehicle including license plate.
    /// </summary>
    public static bool UpdateVehicle(int vehicleID, string licensePlate, string vehicleType, string color, string brand, string note = null)
    {
        try
        {
            const string query = @"
                UPDATE Vehicles
                SET LicensePlate = @LicensePlate,
                    VehicleType = @VehicleType,
                    Color = @Color,
                    Brand = @Brand,
                    Note = @Note,
                    UpdatedAt = GETDATE()
                WHERE VehicleID = @VehicleID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VehicleID", vehicleID);
                    command.Parameters.AddWithValue("@LicensePlate", licensePlate);
                    command.Parameters.AddWithValue("@VehicleType", vehicleType);
                    command.Parameters.AddWithValue("@Color", color ?? string.Empty);
                    command.Parameters.AddWithValue("@Brand", brand ?? string.Empty);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    int affected = command.ExecuteNonQuery();

                    Log.Information("Vehicle updated: {VehicleID}", vehicleID);
                    return affected > 0;
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
        int vehicleID = reader.GetInt32(0);
        string vehicleType = reader.GetString(3);
        string rawNote = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);
        var metadata = ParseVehicleNote(rawNote);

        string model = metadata.TryGetValue("MODEL", out var modelValue) ? modelValue : string.Empty;
        string note = metadata.TryGetValue("NOTE", out var noteValue) ? noteValue : rawNote;
        string cardNumber = metadata.TryGetValue("CARD", out var cardValue) && !string.IsNullOrWhiteSpace(cardValue)
            ? cardValue
            : $"TH{vehicleID:000000}";
        string parkingArea = metadata.TryGetValue("AREA", out var areaValue) && !string.IsNullOrWhiteSpace(areaValue)
            ? areaValue
            : InferParkingArea(vehicleType, note);

        int yearMade = DateTime.Now.Year;
        if (metadata.TryGetValue("YEAR", out var yearValue) &&
            int.TryParse(yearValue, out var parsedYear))
        {
            yearMade = parsedYear;
        }

        DateTime createdAt = reader.GetDateTime(9);
        DateTime expiredAt = createdAt.AddYears(1);
        if (metadata.TryGetValue("EXPIRES", out var expiresValue) &&
            DateTime.TryParse(expiresValue, out var parsedExpires))
        {
            expiredAt = parsedExpires;
        }

        return new
        {
            VehicleID = vehicleID,
            ResidentID = reader.GetInt32(1),
            FullName = reader.GetString(2),
            ResidentName = reader.GetString(2),
            OwnerName = reader.GetString(2),
            VehicleType = vehicleType,
            LicensePlate = reader.GetString(4),
            PlateNumber = reader.GetString(4),
            Color = reader.GetString(5),
            Brand = reader.GetString(6),
            Status = reader.GetString(7),
            Model = model,
            YearMade = yearMade,
            Note = note,
            RawNote = rawNote,
            CardNumber = cardNumber,
            ParkingArea = parkingArea,
            Area = parkingArea,
            ExpiredAt = expiredAt,
            CreatedAt = createdAt,
            RegisteredAt = createdAt,
            UpdatedAt = reader.GetDateTime(10),
            Phone = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            ApartmentID = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
            ApartmentCode = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
            BuildingName = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
            BlockName = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
            FloorNumber = reader.IsDBNull(16) ? 0 : reader.GetInt32(16)
        };
    }

    private static Dictionary<string, string> ParseVehicleNote(string rawNote)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(rawNote) || !rawNote.Contains('='))
        {
            return values;
        }

        foreach (var part in rawNote.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            string key = part.Substring(0, separator).Trim();
            string value = separator + 1 < part.Length ? part.Substring(separator + 1).Trim() : string.Empty;
            values[key] = value;
        }

        return values;
    }

    private static string InferParkingArea(string vehicleType, string note)
    {
        string combined = $"{vehicleType} {note}";
        if (combined.Contains("B2", StringComparison.OrdinalIgnoreCase))
        {
            return "Hầm B2";
        }

        if (combined.Contains("B1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicleType, "Car", StringComparison.OrdinalIgnoreCase))
        {
            return "Hầm B1";
        }

        if (string.Equals(vehicleType, "Bicycle", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicleType, "ElectricBike", StringComparison.OrdinalIgnoreCase))
        {
            return "Khu xe đạp";
        }

        return "Khu ngoài trời";
    }
}


