using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Complaint operations
/// </summary>
public class ComplaintDAL
{
    /// <summary>
    /// Get complaint by ID
    /// </summary>
    public static dynamic? GetComplaintByID(int complaintID)
    {
        try
        {
            const string query = @"
                SELECT c.ComplaintID, c.ResidentID, r.FullName, c.ApartmentID, a.ApartmentCode,
                       c.Title, c.Description, c.Category, c.Priority, c.Status, c.AssignedToUserID,
                       ISNULL(u.Username, 'Unassigned') as AssignedTo, c.CreatedAt, c.UpdatedAt
                FROM Complaints c
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                LEFT JOIN Users u ON c.AssignedToUserID = u.UserID
                WHERE c.ComplaintID = @ComplaintID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ComplaintID", complaintID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapComplaint(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting complaint by ID: {ComplaintID}", complaintID);
            return null;
        }
    }

    /// <summary>
    /// Get all complaints
    /// </summary>
    public static List<dynamic> GetAllComplaints()
    {
        var complaints = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT c.ComplaintID, c.ResidentID, r.FullName, c.ApartmentID, a.ApartmentCode,
                       c.Title, c.Description, c.Category, c.Priority, c.Status, c.AssignedToUserID,
                       ISNULL(u.Username, 'Unassigned') as AssignedTo, c.CreatedAt, c.UpdatedAt
                FROM Complaints c
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                LEFT JOIN Users u ON c.AssignedToUserID = u.UserID
                ORDER BY c.Priority DESC, c.CreatedAt DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            complaints.Add(MapComplaint(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all complaints");
        }

        return complaints;
    }

    /// <summary>
    /// Get complaints by status
    /// </summary>
    public static List<dynamic> GetComplaintsByStatus(string status)
    {
        var complaints = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT c.ComplaintID, c.ResidentID, r.FullName, c.ApartmentID, a.ApartmentCode,
                       c.Title, c.Description, c.Category, c.Priority, c.Status, c.AssignedToUserID,
                       ISNULL(u.Username, 'Unassigned') as AssignedTo, c.CreatedAt, c.UpdatedAt
                FROM Complaints c
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                LEFT JOIN Users u ON c.AssignedToUserID = u.UserID
                WHERE c.Status = @Status
                ORDER BY c.Priority DESC, c.CreatedAt DESC
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
                            complaints.Add(MapComplaint(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting complaints by status: {Status}", status);
        }

        return complaints;
    }

    /// <summary>
    /// Get complaints by priority
    /// </summary>
    public static List<dynamic> GetComplaintsByPriority(string priority)
    {
        var complaints = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT c.ComplaintID, c.ResidentID, r.FullName, c.ApartmentID, a.ApartmentCode,
                       c.Title, c.Description, c.Category, c.Priority, c.Status, c.AssignedToUserID,
                       ISNULL(u.Username, 'Unassigned') as AssignedTo, c.CreatedAt, c.UpdatedAt
                FROM Complaints c
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                LEFT JOIN Users u ON c.AssignedToUserID = u.UserID
                WHERE c.Priority = @Priority
                ORDER BY c.CreatedAt DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Priority", priority);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            complaints.Add(MapComplaint(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting complaints by priority: {Priority}", priority);
        }

        return complaints;
    }

    /// <summary>
    /// Get complaints by resident
    /// </summary>
    public static List<dynamic> GetComplaintsByResident(int residentID)
    {
        var complaints = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT c.ComplaintID, c.ResidentID, r.FullName, c.ApartmentID, a.ApartmentCode,
                       c.Title, c.Description, c.Category, c.Priority, c.Status, c.AssignedToUserID,
                       ISNULL(u.Username, 'Unassigned') as AssignedTo, c.CreatedAt, c.UpdatedAt
                FROM Complaints c
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                LEFT JOIN Users u ON c.AssignedToUserID = u.UserID
                WHERE c.ResidentID = @ResidentID
                ORDER BY c.CreatedAt DESC
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
                            complaints.Add(MapComplaint(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting complaints by resident: {ResidentID}", residentID);
        }

        return complaints;
    }

    /// <summary>
    /// Create complaint
    /// </summary>
    public static int CreateComplaint(int residentID, int apartmentID, string title, string description,
                                      string category, string priority)
    {
        try
        {
            const string query = @"
                INSERT INTO Complaints (ResidentID, ApartmentID, Title, Description, Category, Priority, Status)
                VALUES (@ResidentID, @ApartmentID, @Title, @Description, @Category, @Priority, 'Open')
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Description", description);
                    command.Parameters.AddWithValue("@Category", category);
                    command.Parameters.AddWithValue("@Priority", priority);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var complaintID = Convert.ToInt32(result);

                    Log.Information("Complaint created: {Title} (ID: {ComplaintID})", title, complaintID);
                    return complaintID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating complaint: {Title}", title);
            throw;
        }
    }

    /// <summary>
    /// Update complaint
    /// </summary>
    public static bool UpdateComplaint(int complaintID, string title, string description, string category, string priority)
    {
        try
        {
            const string query = @"
                UPDATE Complaints
                SET Title = @Title, Description = @Description, Category = @Category, Priority = @Priority, UpdatedAt = GETDATE()
                WHERE ComplaintID = @ComplaintID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ComplaintID", complaintID);
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Description", description);
                    command.Parameters.AddWithValue("@Category", category);
                    command.Parameters.AddWithValue("@Priority", priority);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Complaint updated: {ComplaintID}", complaintID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating complaint: {ComplaintID}", complaintID);
            return false;
        }
    }

    /// <summary>
    /// Update complaint status
    /// </summary>
    public static bool UpdateComplaintStatus(int complaintID, string status)
    {
        try
        {
            const string query = @"
                UPDATE Complaints
                SET Status = @Status, UpdatedAt = GETDATE()
                WHERE ComplaintID = @ComplaintID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ComplaintID", complaintID);
                    command.Parameters.AddWithValue("@Status", status);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Complaint status updated: {ComplaintID} to {Status}", complaintID, status);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating complaint status: {ComplaintID}", complaintID);
            return false;
        }
    }

    /// <summary>
    /// Assign complaint to user
    /// </summary>
    public static bool AssignComplaint(int complaintID, int userID)
    {
        try
        {
            const string query = @"
                UPDATE Complaints
                SET AssignedToUserID = @UserID, UpdatedAt = GETDATE()
                WHERE ComplaintID = @ComplaintID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ComplaintID", complaintID);
                    command.Parameters.AddWithValue("@UserID", userID);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Complaint assigned: {ComplaintID} to user {UserID}", complaintID, userID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error assigning complaint: {ComplaintID}", complaintID);
            return false;
        }
    }

    /// <summary>
    /// Delete complaint
    /// </summary>
    public static bool DeleteComplaint(int complaintID)
    {
        try
        {
            const string query = "DELETE FROM Complaints WHERE ComplaintID = @ComplaintID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ComplaintID", complaintID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Complaint deleted: {ComplaintID}", complaintID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting complaint: {ComplaintID}", complaintID);
            return false;
        }
    }

    /// <summary>
    /// Resolve complaint (mark as resolved)
    /// </summary>
    public static bool ResolveComplaint(int complaintID, string resolutionNotes)
    {
        try
        {
            const string query = @"
                UPDATE Complaints 
                SET Status = 'Resolved', ResolutionNotes = @ResolutionNotes, UpdatedAt = GETDATE()
                WHERE ComplaintID = @ComplaintID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ComplaintID", complaintID);
                    command.Parameters.AddWithValue("@ResolutionNotes", resolutionNotes ?? "");
                    connection.Open();

                    var affected = command.ExecuteNonQuery();
                    if (affected > 0)
                    {
                        Log.Information("Complaint {ComplaintID} resolved", complaintID);
                    }

                    return affected > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resolving complaint: {ComplaintID}", complaintID);
            return false;
        }
    }

    /// <summary>
    /// Map SqlDataReader to complaint object
    /// </summary>
    private static dynamic MapComplaint(SqlDataReader reader)
    {
        return new
        {
            ComplaintID = reader.GetInt32(0),
            ResidentID = reader.GetInt32(1),
            ResidentName = reader.GetString(2),
            ApartmentID = reader.GetInt32(3),
            ApartmentCode = reader.GetString(4),
            Title = reader.GetString(5),
            Description = reader.GetString(6),
            Category = reader.GetString(7),
            Priority = reader.GetString(8),
            Status = reader.GetString(9),
            AssignedToUserID = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            AssignedTo = reader.GetString(11),
            CreatedAt = reader.GetDateTime(12),
            UpdatedAt = reader.GetDateTime(13)
        };
    }
}


