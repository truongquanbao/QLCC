using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Resident operations
/// </summary>
public class ResidentDAL
{
    /// <summary>
    /// Get resident by ID
    /// </summary>
    public static ResidentDTO? GetResidentByID(int residentID)
    {
        try
        {
            const string query = @"
                SELECT r.ResidentID, r.UserID, u.Username, r.FullName, r.Phone, r.Email,
                       r.CCCD, r.DOB, r.ApartmentID, a.ApartmentCode, r.RelationshipWithOwner,
                       r.Status, r.StartDate, r.EndDate, r.Note, r.CreatedAt, r.UpdatedAt
                FROM Residents r
                LEFT JOIN Users u ON r.UserID = u.UserID
                LEFT JOIN Apartments a ON r.ApartmentID = a.ApartmentID
                WHERE r.ResidentID = @ResidentID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapResidentDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting resident by ID: {ResidentID}", residentID);
            return null;
        }
    }

    /// <summary>
    /// Get resident by UserID
    /// </summary>
    public static ResidentDTO? GetResidentByUserID(int userID)
    {
        try
        {
            const string query = @"
                SELECT r.ResidentID, r.UserID, u.Username, r.FullName, r.Phone, r.Email,
                       r.CCCD, r.DOB, r.ApartmentID, a.ApartmentCode, r.RelationshipWithOwner,
                       r.Status, r.StartDate, r.EndDate, r.Note, r.CreatedAt, r.UpdatedAt
                FROM Residents r
                LEFT JOIN Users u ON r.UserID = u.UserID
                LEFT JOIN Apartments a ON r.ApartmentID = a.ApartmentID
                WHERE r.UserID = @UserID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapResidentDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting resident by UserID: {UserID}", userID);
            return null;
        }
    }

    /// <summary>
    /// Get resident by CCCD
    /// </summary>
    public static ResidentDTO? GetResidentByCCCD(string cccd)
    {
        try
        {
            const string query = @"
                SELECT r.ResidentID, r.UserID, u.Username, r.FullName, r.Phone, r.Email,
                       r.CCCD, r.DOB, r.ApartmentID, a.ApartmentCode, r.RelationshipWithOwner,
                       r.Status, r.StartDate, r.EndDate, r.Note, r.CreatedAt, r.UpdatedAt
                FROM Residents r
                LEFT JOIN Users u ON r.UserID = u.UserID
                LEFT JOIN Apartments a ON r.ApartmentID = a.ApartmentID
                WHERE r.CCCD = @CCCD
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CCCD", cccd);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapResidentDTO(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting resident by CCCD: {CCCD}", cccd);
            return null;
        }
    }

    /// <summary>
    /// Get residents by apartment
    /// </summary>
    public static List<ResidentDTO> GetResidentsByApartment(int apartmentID)
    {
        var residents = new List<ResidentDTO>();

        try
        {
            const string query = @"
                SELECT r.ResidentID, r.UserID, u.Username, r.FullName, r.Phone, r.Email,
                       r.CCCD, r.DOB, r.ApartmentID, a.ApartmentCode, r.RelationshipWithOwner,
                       r.Status, r.StartDate, r.EndDate, r.Note, r.CreatedAt, r.UpdatedAt
                FROM Residents r
                LEFT JOIN Users u ON r.UserID = u.UserID
                LEFT JOIN Apartments a ON r.ApartmentID = a.ApartmentID
                WHERE r.ApartmentID = @ApartmentID
                ORDER BY r.RelationshipWithOwner DESC, r.StartDate DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            residents.Add(MapResidentDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting residents by apartment: {ApartmentID}", apartmentID);
        }

        return residents;
    }

    /// <summary>
    /// Get residents by building
    /// </summary>
    public static List<ResidentDTO> GetResidentsByBuilding(int buildingID)
    {
        var residents = new List<ResidentDTO>();

        try
        {
            const string query = @"
                SELECT r.ResidentID, r.UserID, u.Username, r.FullName, r.Phone, r.Email,
                       r.CCCD, r.DOB, r.ApartmentID, a.ApartmentCode, r.RelationshipWithOwner,
                       r.Status, r.StartDate, r.EndDate, r.Note, r.CreatedAt, r.UpdatedAt
                FROM Residents r
                LEFT JOIN Users u ON r.UserID = u.UserID
                LEFT JOIN Apartments a ON r.ApartmentID = a.ApartmentID
                INNER JOIN Floors f ON a.FloorID = f.FloorID
                INNER JOIN Blocks b ON f.BlockID = b.BlockID
                WHERE b.BuildingID = @BuildingID
                ORDER BY a.ApartmentCode, r.RelationshipWithOwner DESC
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
                            residents.Add(MapResidentDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting residents by building: {BuildingID}", buildingID);
        }

        return residents;
    }

    /// <summary>
    /// Get residents by status
    /// </summary>
    public static List<ResidentDTO> GetResidentsByStatus(string status)
    {
        var residents = new List<ResidentDTO>();

        try
        {
            const string query = @"
                SELECT r.ResidentID, r.UserID, u.Username, r.FullName, r.Phone, r.Email,
                       r.CCCD, r.DOB, r.ApartmentID, a.ApartmentCode, r.RelationshipWithOwner,
                       r.Status, r.StartDate, r.EndDate, r.Note, r.CreatedAt, r.UpdatedAt
                FROM Residents r
                LEFT JOIN Users u ON r.UserID = u.UserID
                LEFT JOIN Apartments a ON r.ApartmentID = a.ApartmentID
                WHERE r.Status = @Status
                ORDER BY a.ApartmentCode, r.FullName
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
                            residents.Add(MapResidentDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting residents by status: {Status}", status);
        }

        return residents;
    }

    /// <summary>
    /// Get all residents
    /// </summary>
    public static List<ResidentDTO> GetAllResidents()
    {
        var residents = new List<ResidentDTO>();

        try
        {
            const string query = @"
                SELECT r.ResidentID, r.UserID, u.Username, r.FullName, r.Phone, r.Email,
                       r.CCCD, r.DOB, r.ApartmentID, a.ApartmentCode, r.RelationshipWithOwner,
                       r.Status, r.StartDate, r.EndDate, r.Note, r.CreatedAt, r.UpdatedAt
                FROM Residents r
                LEFT JOIN Users u ON r.UserID = u.UserID
                LEFT JOIN Apartments a ON r.ApartmentID = a.ApartmentID
                ORDER BY a.ApartmentCode, r.FullName
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            residents.Add(MapResidentDTO(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all residents");
        }

        return residents;
    }

    /// <summary>
    /// Create resident
    /// </summary>
    public static int CreateResident(int? userID, string fullName, string phone, string email,
                                     string cccd, DateTime dob, int apartmentID, string relationshipWithOwner,
                                     DateTime startDate, string? note = null)
    {
        try
        {
            const string query = @"
                INSERT INTO Residents (UserID, FullName, Phone, Email, CCCD, DOB, ApartmentID,
                                      RelationshipWithOwner, Status, StartDate, Note)
                VALUES (@UserID, @FullName, @Phone, @Email, @CCCD, @DOB, @ApartmentID,
                        @RelationshipWithOwner, 'Active', @StartDate, @Note)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FullName", fullName);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@CCCD", cccd);
                    command.Parameters.AddWithValue("@DOB", dob);
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    command.Parameters.AddWithValue("@RelationshipWithOwner", relationshipWithOwner);
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var residentID = Convert.ToInt32(result);

                    Log.Information("Resident created: {FullName} (ID: {ResidentID})", fullName, residentID);
                    return residentID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating resident: {FullName}", fullName);
            throw;
        }
    }

    /// <summary>
    /// Update resident
    /// </summary>
    public static bool UpdateResident(int residentID, string fullName, string phone, string email,
                                      string relationshipWithOwner, string? note = null)
    {
        try
        {
            const string query = @"
                UPDATE Residents
                SET FullName = @FullName, Phone = @Phone, Email = @Email,
                    RelationshipWithOwner = @RelationshipWithOwner, Note = @Note, UpdatedAt = GETDATE()
                WHERE ResidentID = @ResidentID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    command.Parameters.AddWithValue("@FullName", fullName);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@RelationshipWithOwner", relationshipWithOwner);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Resident updated: {ResidentID}", residentID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating resident: {ResidentID}", residentID);
            return false;
        }
    }

    /// <summary>
    /// Update resident status
    /// </summary>
    public static bool UpdateResidentStatus(int residentID, string status, DateTime? endDate = null)
    {
        try
        {
            string query = @"
                UPDATE Residents
                SET Status = @Status, UpdatedAt = GETDATE()";

            if (endDate.HasValue)
                query += ", EndDate = @EndDate";

            query += " WHERE ResidentID = @ResidentID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    command.Parameters.AddWithValue("@Status", status);

                    if (endDate.HasValue)
                        command.Parameters.AddWithValue("@EndDate", endDate.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Resident status updated: {ResidentID} to {Status}", residentID, status);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating resident status: {ResidentID}", residentID);
            return false;
        }
    }

    /// <summary>
    /// Delete resident
    /// </summary>
    public static bool DeleteResident(int residentID)
    {
        try
        {
            const string query = "DELETE FROM Residents WHERE ResidentID = @ResidentID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Resident deleted: {ResidentID}", residentID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting resident: {ResidentID}", residentID);
            return false;
        }
    }

    /// <summary>
    /// Check if CCCD exists
    /// </summary>
    public static bool CCCDExists(string cccd, int? excludeResidentID = null)
    {
        try
        {
            string query = "SELECT COUNT(*) FROM Residents WHERE CCCD = @CCCD";
            if (excludeResidentID.HasValue)
                query += " AND ResidentID != @ExcludeResidentID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CCCD", cccd);
                    if (excludeResidentID.HasValue)
                        command.Parameters.AddWithValue("@ExcludeResidentID", excludeResidentID.Value);

                    connection.Open();
                    return (int)command.ExecuteScalar()! > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if CCCD exists: {CCCD}", cccd);
            return true;
        }
    }

    /// <summary>
    /// Get active residents count
    /// </summary>
    public static int GetActiveResidentsCount()
    {
        try
        {
            const string query = "SELECT COUNT(*) FROM Residents WHERE Status = 'Active'";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    return (int)command.ExecuteScalar()!;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting active residents count");
            return 0;
        }
    }

    /// <summary>
    /// Map SqlDataReader to ResidentDTO
    /// </summary>
    private static ResidentDTO MapResidentDTO(SqlDataReader reader)
    {
        return new ResidentDTO
        {
            ResidentID = reader.GetInt32(0),
            UserID = reader.IsDBNull(1) ? null : reader.GetInt32(1),
            Username = reader.IsDBNull(2) ? null : reader.GetString(2),
            FullName = reader.GetString(3),
            Phone = reader.GetString(4),
            Email = reader.GetString(5),
            CCCD = reader.GetString(6),
            DOB = reader.GetDateTime(7),
            ApartmentID = reader.GetInt32(8),
            ApartmentCode = reader.IsDBNull(9) ? null : reader.GetString(9),
            RelationshipWithOwner = reader.GetString(10),
            Status = reader.GetString(11),
            StartDate = reader.GetDateTime(12),
            EndDate = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
            Note = reader.IsDBNull(14) ? null : reader.GetString(14),
            CreatedAt = reader.GetDateTime(15),
            UpdatedAt = reader.GetDateTime(16)
        };
    }
}


