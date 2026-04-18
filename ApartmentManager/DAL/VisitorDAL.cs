using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Visitor operations
/// </summary>
public class VisitorDAL
{
    /// <summary>
    /// Get visitor by ID
    /// </summary>
    public static dynamic? GetVisitorByID(int visitorID)
    {
        try
        {
            const string query = @"
                SELECT v.VisitorID, v.ResidentID, r.FullName as ResidentName, v.VisitorName,
                       v.Phone, v.Email, v.IDNumber, v.Purpose, v.ArrivalTime, v.DepartureTime,
                       v.Status, v.ApprovedByUserID, ISNULL(u.Username, '') as ApprovedBy,
                       v.Note, v.CreatedAt, v.UpdatedAt
                FROM Visitors v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                LEFT JOIN Users u ON v.ApprovedByUserID = u.UserID
                WHERE v.VisitorID = @VisitorID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitorID", visitorID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapVisitor(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting visitor by ID: {VisitorID}", visitorID);
            return null;
        }
    }

    /// <summary>
    /// Get all visitors
    /// </summary>
    public static List<dynamic> GetAllVisitors()
    {
        var visitors = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT v.VisitorID, v.ResidentID, r.FullName as ResidentName, v.VisitorName,
                       v.Phone, v.Email, v.IDNumber, v.Purpose, v.ArrivalTime, v.DepartureTime,
                       v.Status, v.ApprovedByUserID, ISNULL(u.Username, '') as ApprovedBy,
                       v.Note, v.CreatedAt, v.UpdatedAt
                FROM Visitors v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                LEFT JOIN Users u ON v.ApprovedByUserID = u.UserID
                ORDER BY v.CreatedAt DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            visitors.Add(MapVisitor(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all visitors");
        }

        return visitors;
    }

    /// <summary>
    /// Get visitors for resident
    /// </summary>
    public static List<dynamic> GetVisitorsByResident(int residentID)
    {
        var visitors = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT v.VisitorID, v.ResidentID, r.FullName as ResidentName, v.VisitorName,
                       v.Phone, v.Email, v.IDNumber, v.Purpose, v.ArrivalTime, v.DepartureTime,
                       v.Status, v.ApprovedByUserID, ISNULL(u.Username, '') as ApprovedBy,
                       v.Note, v.CreatedAt, v.UpdatedAt
                FROM Visitors v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                LEFT JOIN Users u ON v.ApprovedByUserID = u.UserID
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
                            visitors.Add(MapVisitor(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting visitors by resident: {ResidentID}", residentID);
        }

        return visitors;
    }

    /// <summary>
    /// Get pending visitor approvals
    /// </summary>
    public static List<dynamic> GetPendingApprovals()
    {
        var visitors = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT v.VisitorID, v.ResidentID, r.FullName as ResidentName, v.VisitorName,
                       v.Phone, v.Email, v.IDNumber, v.Purpose, v.ArrivalTime, v.DepartureTime,
                       v.Status, v.ApprovedByUserID, ISNULL(u.Username, '') as ApprovedBy,
                       v.Note, v.CreatedAt, v.UpdatedAt
                FROM Visitors v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                LEFT JOIN Users u ON v.ApprovedByUserID = u.UserID
                WHERE v.Status = 'Pending'
                ORDER BY v.CreatedAt ASC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            visitors.Add(MapVisitor(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting pending visitor approvals");
        }

        return visitors;
    }

    /// <summary>
    /// Get visitors by date range
    /// </summary>
    public static List<dynamic> GetVisitorsByDateRange(DateTime startDate, DateTime endDate)
    {
        var visitors = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT v.VisitorID, v.ResidentID, r.FullName as ResidentName, v.VisitorName,
                       v.Phone, v.Email, v.IDNumber, v.Purpose, v.ArrivalTime, v.DepartureTime,
                       v.Status, v.ApprovedByUserID, ISNULL(u.Username, '') as ApprovedBy,
                       v.Note, v.CreatedAt, v.UpdatedAt
                FROM Visitors v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                LEFT JOIN Users u ON v.ApprovedByUserID = u.UserID
                WHERE v.ArrivalTime >= @StartDate AND v.ArrivalTime <= @EndDate
                ORDER BY v.ArrivalTime DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            visitors.Add(MapVisitor(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting visitors by date range");
        }

        return visitors;
    }

    /// <summary>
    /// Register visitor
    /// </summary>
    public static int RegisterVisitor(int residentID, string visitorName, string phone, string email,
                                      string idNumber, string purpose, DateTime arrivalTime, string? note = null)
    {
        try
        {
            const string query = @"
                INSERT INTO Visitors (ResidentID, VisitorName, Phone, Email, IDNumber, Purpose, ArrivalTime, Status, Note)
                VALUES (@ResidentID, @VisitorName, @Phone, @Email, @IDNumber, @Purpose, @ArrivalTime, 'Pending', @Note)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    command.Parameters.AddWithValue("@VisitorName", visitorName);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@IDNumber", idNumber);
                    command.Parameters.AddWithValue("@Purpose", purpose);
                    command.Parameters.AddWithValue("@ArrivalTime", arrivalTime);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var visitorID = Convert.ToInt32(result);

                    Log.Information("Visitor registered: {VisitorName} (ID: {VisitorID})", visitorName, visitorID);
                    return visitorID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error registering visitor: {VisitorName}", visitorName);
            throw;
        }
    }

    /// <summary>
    /// Approve visitor
    /// </summary>
    public static bool ApproveVisitor(int visitorID, int userID)
    {
        try
        {
            const string query = @"
                UPDATE Visitors
                SET Status = 'Approved', ApprovedByUserID = @UserID, UpdatedAt = GETDATE()
                WHERE VisitorID = @VisitorID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitorID", visitorID);
                    command.Parameters.AddWithValue("@UserID", userID);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Visitor approved: {VisitorID}", visitorID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error approving visitor: {VisitorID}", visitorID);
            return false;
        }
    }

    /// <summary>
    /// Reject visitor
    /// </summary>
    public static bool RejectVisitor(int visitorID, int userID)
    {
        try
        {
            const string query = @"
                UPDATE Visitors
                SET Status = 'Rejected', ApprovedByUserID = @UserID, UpdatedAt = GETDATE()
                WHERE VisitorID = @VisitorID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitorID", visitorID);
                    command.Parameters.AddWithValue("@UserID", userID);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Visitor rejected: {VisitorID}", visitorID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error rejecting visitor: {VisitorID}", visitorID);
            return false;
        }
    }

    /// <summary>
    /// Record visitor departure
    /// </summary>
    public static bool RecordDeparture(int visitorID, DateTime departureTime)
    {
        try
        {
            const string query = @"
                UPDATE Visitors
                SET DepartureTime = @DepartureTime, UpdatedAt = GETDATE()
                WHERE VisitorID = @VisitorID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitorID", visitorID);
                    command.Parameters.AddWithValue("@DepartureTime", departureTime);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Visitor departure recorded: {VisitorID}", visitorID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error recording visitor departure: {VisitorID}", visitorID);
            return false;
        }
    }

    /// <summary>
    /// Delete visitor
    /// </summary>
    public static bool DeleteVisitor(int visitorID)
    {
        try
        {
            const string query = "DELETE FROM Visitors WHERE VisitorID = @VisitorID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitorID", visitorID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Visitor deleted: {VisitorID}", visitorID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting visitor: {VisitorID}", visitorID);
            return false;
        }
    }

    /// <summary>
    /// Map SqlDataReader to visitor object
    /// </summary>
    private static dynamic MapVisitor(SqlDataReader reader)
    {
        return new
        {
            VisitorID = reader.GetInt32(0),
            ResidentID = reader.GetInt32(1),
            ResidentName = reader.GetString(2),
            VisitorName = reader.GetString(3),
            Phone = reader.GetString(4),
            Email = reader.GetString(5),
            IDNumber = reader.GetString(6),
            Purpose = reader.GetString(7),
            ArrivalTime = reader.GetDateTime(8),
            DepartureTime = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            Status = reader.GetString(10),
            ApprovedByUserID = reader.IsDBNull(11) ? null : reader.GetInt32(11),
            ApprovedBy = reader.GetString(12),
            Note = reader.IsDBNull(13) ? null : reader.GetString(13),
            CreatedAt = reader.GetDateTime(14),
            UpdatedAt = reader.GetDateTime(15)
        };
    }
}


