using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Apartment operations
/// </summary>
public class ApartmentDAL
{
    /// <summary>
    /// Get apartment by ID
    /// </summary>
    public static ApartmentDTO? GetApartmentByID(int apartmentID)
    {
        try
        {
            const string query = @"
                SELECT a.ApartmentID, a.ApartmentCode, a.FloorID, f.FloorNumber,
                       bl.BlockID, bl.BlockName, b.BuildingID, b.BuildingName,
                       a.Area, a.ApartmentType, a.Status, a.MaxResidents, a.Note,
                       a.CreatedAt, a.UpdatedAt
                FROM Apartments a
                INNER JOIN Floors f ON a.FloorID = f.FloorID
                INNER JOIN Blocks bl ON f.BlockID = bl.BlockID
                INNER JOIN Buildings b ON bl.BuildingID = b.BuildingID
                WHERE a.ApartmentID = @ApartmentID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapApartmentDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting apartment by ID: {ApartmentID}", apartmentID);
            return null;
        }
    }

    /// <summary>
    /// Get apartment by code
    /// </summary>
    public static ApartmentDTO? GetApartmentByCode(string apartmentCode)
    {
        try
        {
            const string query = @"
                SELECT a.ApartmentID, a.ApartmentCode, a.FloorID, f.FloorNumber,
                       bl.BlockID, bl.BlockName, b.BuildingID, b.BuildingName,
                       a.Area, a.ApartmentType, a.Status, a.MaxResidents, a.Note,
                       a.CreatedAt, a.UpdatedAt
                FROM Apartments a
                INNER JOIN Floors f ON a.FloorID = f.FloorID
                INNER JOIN Blocks bl ON f.BlockID = bl.BlockID
                INNER JOIN Buildings b ON bl.BuildingID = b.BuildingID
                WHERE a.ApartmentCode = @ApartmentCode
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentCode", apartmentCode);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapApartmentDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting apartment by code: {ApartmentCode}", apartmentCode);
            return null;
        }
    }

    /// <summary>
    /// Get all apartments
    /// </summary>
    public static List<ApartmentDTO> GetAllApartments()
    {
        var apartments = new List<ApartmentDTO>();

        try
        {
            const string query = @"
                SELECT a.ApartmentID, a.ApartmentCode, a.FloorID, f.FloorNumber,
                       bl.BlockID, bl.BlockName, b.BuildingID, b.BuildingName,
                       a.Area, a.ApartmentType, a.Status, a.MaxResidents, a.Note,
                       a.CreatedAt, a.UpdatedAt
                FROM Apartments a
                INNER JOIN Floors f ON a.FloorID = f.FloorID
                INNER JOIN Blocks bl ON f.BlockID = bl.BlockID
                INNER JOIN Buildings b ON bl.BuildingID = b.BuildingID
                ORDER BY b.BuildingName, bl.BlockName, f.FloorNumber, a.ApartmentCode
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            apartments.Add(MapApartmentDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all apartments");
        }

        return apartments;
    }

    /// <summary>
    /// Get apartments by floor
    /// </summary>
    public static List<ApartmentDTO> GetApartmentsByFloor(int floorID)
    {
        var apartments = new List<ApartmentDTO>();

        try
        {
            const string query = @"
                SELECT a.ApartmentID, a.ApartmentCode, a.FloorID, f.FloorNumber,
                       bl.BlockID, bl.BlockName, b.BuildingID, b.BuildingName,
                       a.Area, a.ApartmentType, a.Status, a.MaxResidents, a.Note,
                       a.CreatedAt, a.UpdatedAt
                FROM Apartments a
                INNER JOIN Floors f ON a.FloorID = f.FloorID
                INNER JOIN Blocks bl ON f.BlockID = bl.BlockID
                INNER JOIN Buildings b ON bl.BuildingID = b.BuildingID
                WHERE a.FloorID = @FloorID
                ORDER BY a.ApartmentCode
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FloorID", floorID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            apartments.Add(MapApartmentDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting apartments by floor: {FloorID}", floorID);
        }

        return apartments;
    }

    /// <summary>
    /// Get apartments by building
    /// </summary>
    public static List<ApartmentDTO> GetApartmentsByBuilding(int buildingID)
    {
        var apartments = new List<ApartmentDTO>();

        try
        {
            const string query = @"
                SELECT a.ApartmentID, a.ApartmentCode, a.FloorID, f.FloorNumber,
                       bl.BlockID, bl.BlockName, b.BuildingID, b.BuildingName,
                       a.Area, a.ApartmentType, a.Status, a.MaxResidents, a.Note,
                       a.CreatedAt, a.UpdatedAt
                FROM Apartments a
                INNER JOIN Floors f ON a.FloorID = f.FloorID
                INNER JOIN Blocks bl ON f.BlockID = bl.BlockID
                INNER JOIN Buildings b ON bl.BuildingID = b.BuildingID
                WHERE b.BuildingID = @BuildingID
                ORDER BY bl.BlockName, f.FloorNumber, a.ApartmentCode
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BuildingID", buildingID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            apartments.Add(MapApartmentDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting apartments by building: {BuildingID}", buildingID);
        }

        return apartments;
    }

    /// <summary>
    /// Get apartments by status
    /// </summary>
    public static List<ApartmentDTO> GetApartmentsByStatus(string status)
    {
        var apartments = new List<ApartmentDTO>();

        try
        {
            const string query = @"
                SELECT a.ApartmentID, a.ApartmentCode, a.FloorID, f.FloorNumber,
                       bl.BlockID, bl.BlockName, b.BuildingID, b.BuildingName,
                       a.Area, a.ApartmentType, a.Status, a.MaxResidents, a.Note,
                       a.CreatedAt, a.UpdatedAt
                FROM Apartments a
                INNER JOIN Floors f ON a.FloorID = f.FloorID
                INNER JOIN Blocks bl ON f.BlockID = bl.BlockID
                INNER JOIN Buildings b ON bl.BuildingID = b.BuildingID
                WHERE a.Status = @Status
                ORDER BY b.BuildingName, bl.BlockName, f.FloorNumber
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Status", status);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            apartments.Add(MapApartmentDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting apartments by status: {Status}", status);
        }

        return apartments;
    }

    /// <summary>
    /// Create apartment
    /// </summary>
    public static int CreateApartment(string apartmentCode, int floorID, decimal area, 
                                      string apartmentType, int maxResidents, string? note = null)
    {
        try
        {
            const string query = @"
                INSERT INTO Apartments (ApartmentCode, FloorID, Area, ApartmentType, Status, MaxResidents, Note)
                VALUES (@ApartmentCode, @FloorID, @Area, @ApartmentType, 'Empty', @MaxResidents, @Note)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentCode", apartmentCode);
                    command.Parameters.AddWithValue("@FloorID", floorID);
                    command.Parameters.AddWithValue("@Area", area);
                    command.Parameters.AddWithValue("@ApartmentType", apartmentType);
                    command.Parameters.AddWithValue("@MaxResidents", maxResidents);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var apartmentID = Convert.ToInt32(result);

                    Log.Information("Apartment created: {ApartmentCode} (ID: {ApartmentID})", apartmentCode, apartmentID);
                    return apartmentID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating apartment: {ApartmentCode}", apartmentCode);
            throw;
        }
    }

    /// <summary>
    /// Backward-compatible apartment creation overload.
    /// </summary>
    public static int CreateApartment(int floorID, string apartmentCode, decimal area,
                                      string apartmentType, int maxResidents, string? note = null)
    {
        return CreateApartment(apartmentCode, floorID, area, apartmentType, maxResidents, note);
    }

    /// <summary>
    /// Update apartment
    /// </summary>
    public static bool UpdateApartment(int apartmentID, string apartmentCode, decimal area, 
                                       string apartmentType, int maxResidents, string? note = null)
    {
        try
        {
            const string query = @"
                UPDATE Apartments
                SET ApartmentCode = @ApartmentCode, Area = @Area, ApartmentType = @ApartmentType,
                    MaxResidents = @MaxResidents, Note = @Note, UpdatedAt = GETDATE()
                WHERE ApartmentID = @ApartmentID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    command.Parameters.AddWithValue("@ApartmentCode", apartmentCode);
                    command.Parameters.AddWithValue("@Area", area);
                    command.Parameters.AddWithValue("@ApartmentType", apartmentType);
                    command.Parameters.AddWithValue("@MaxResidents", maxResidents);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Apartment updated: {ApartmentID}", apartmentID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating apartment: {ApartmentID}", apartmentID);
            return false;
        }
    }

    /// <summary>
    /// Update apartment including floor assignment.
    /// </summary>
    public static bool UpdateApartment(int apartmentID, int floorID, string apartmentCode, decimal area,
                                       string apartmentType, int maxResidents, string? note = null)
    {
        try
        {
            const string query = @"
                UPDATE Apartments
                SET FloorID = @FloorID, ApartmentCode = @ApartmentCode, Area = @Area, ApartmentType = @ApartmentType,
                    MaxResidents = @MaxResidents, Note = @Note, UpdatedAt = GETDATE()
                WHERE ApartmentID = @ApartmentID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    command.Parameters.AddWithValue("@FloorID", floorID);
                    command.Parameters.AddWithValue("@ApartmentCode", apartmentCode);
                    command.Parameters.AddWithValue("@Area", area);
                    command.Parameters.AddWithValue("@ApartmentType", apartmentType);
                    command.Parameters.AddWithValue("@MaxResidents", maxResidents);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Apartment updated with floor: {ApartmentID} -> {FloorID}", apartmentID, floorID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating apartment floor assignment: {ApartmentID}", apartmentID);
            return false;
        }
    }

    /// <summary>
    /// Update apartment status
    /// </summary>
    public static bool UpdateApartmentStatus(int apartmentID, string status)
    {
        try
        {
            const string query = @"
                UPDATE Apartments
                SET Status = @Status, UpdatedAt = GETDATE()
                WHERE ApartmentID = @ApartmentID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    command.Parameters.AddWithValue("@Status", status);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Apartment status updated: {ApartmentID} to {Status}", apartmentID, status);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating apartment status: {ApartmentID}", apartmentID);
            return false;
        }
    }

    /// <summary>
    /// Delete apartment
    /// </summary>
    public static bool DeleteApartment(int apartmentID)
    {
        try
        {
            const string query = "DELETE FROM Apartments WHERE ApartmentID = @ApartmentID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Apartment deleted: {ApartmentID}", apartmentID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting apartment: {ApartmentID}", apartmentID);
            return false;
        }
    }

    /// <summary>
    /// Check if apartment code exists
    /// </summary>
    public static bool ApartmentCodeExists(string apartmentCode, int? excludeApartmentID = null)
    {
        try
        {
            string query = "SELECT COUNT(*) FROM Apartments WHERE ApartmentCode = @ApartmentCode";
            if (excludeApartmentID.HasValue)
                query += " AND ApartmentID != @ExcludeApartmentID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentCode", apartmentCode);
                    if (excludeApartmentID.HasValue)
                        command.Parameters.AddWithValue("@ExcludeApartmentID", excludeApartmentID.Value);

                    connection.Open();
                    return (int)command.ExecuteScalar()! > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if apartment code exists: {ApartmentCode}", apartmentCode);
            return true;
        }
    }

    /// <summary>
    /// Get apartment with resident count
    /// </summary>
    public static dynamic? GetApartmentWithResidentCount(int apartmentID)
    {
        try
        {
            const string query = @"
                SELECT a.ApartmentID, a.ApartmentCode, a.Status,
                       COUNT(r.ResidentID) as ResidentCount,
                       a.MaxResidents
                FROM Apartments a
                LEFT JOIN Residents r ON a.ApartmentID = r.ApartmentID AND r.Status = 'Active'
                WHERE a.ApartmentID = @ApartmentID
                GROUP BY a.ApartmentID, a.ApartmentCode, a.Status, a.MaxResidents
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                ApartmentID = reader.GetInt32(0),
                                ApartmentCode = reader.GetString(1),
                                Status = reader.GetString(2),
                                ResidentCount = reader.GetInt32(3),
                                MaxResidents = reader.GetInt32(4)
                            };
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting apartment with resident count: {ApartmentID}", apartmentID);
            return null;
        }
    }

    /// <summary>
    /// Map SqlDataReader to ApartmentDTO
    /// </summary>
    private static ApartmentDTO MapApartmentDTO(SqlDataReader reader)
    {
        return new ApartmentDTO
        {
            ApartmentID = reader.GetInt32(0),
            ApartmentCode = reader.GetString(1),
            FloorID = reader.GetInt32(2),
            FloorNumber = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            BlockID = reader.IsDBNull(4) ? null : reader.GetInt32(4),
            BlockName = reader.GetString(5),
            BuildingID = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            BuildingName = reader.GetString(7),
            Area = reader.GetDecimal(8),
            ApartmentType = reader.GetString(9),
            Status = reader.GetString(10),
            MaxResidents = reader.GetInt32(11),
            Note = reader.IsDBNull(12) ? null : reader.GetString(12),
            CreatedAt = reader.GetDateTime(13),
            UpdatedAt = reader.GetDateTime(14)
        };
    }
}


