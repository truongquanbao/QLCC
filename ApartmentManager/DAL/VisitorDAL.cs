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
    private const string VisitorSelect = @"
                SELECT v.VisitorID, v.ResidentID, r.FullName as ResidentName, v.VisitorName,
                       v.Phone, v.Email, v.IDNumber, v.Purpose, v.ArrivalTime, v.DepartureTime,
                       v.Status, v.ApprovedByUserID, ISNULL(u.Username, '') as ApprovedBy,
                       v.Note, v.CreatedAt, v.UpdatedAt,
                       r.ApartmentID, ISNULL(a.ApartmentCode, '') as ApartmentCode,
                       ISNULL(b.BuildingName, '') as BuildingName,
                       ISNULL(bl.BlockName, '') as BlockName
                FROM Visitors v
                INNER JOIN Residents r ON v.ResidentID = r.ResidentID
                LEFT JOIN Apartments a ON r.ApartmentID = a.ApartmentID
                LEFT JOIN Floors f ON a.FloorID = f.FloorID
                LEFT JOIN Blocks bl ON f.BlockID = bl.BlockID
                LEFT JOIN Buildings b ON bl.BuildingID = b.BuildingID
                LEFT JOIN Users u ON v.ApprovedByUserID = u.UserID
            ";

    /// <summary>
    /// Get visitor by ID
    /// </summary>
    public static dynamic? GetVisitorByID(int visitorID)
    {
        try
        {
            const string query = VisitorSelect + @"
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
            const string query = VisitorSelect + @"
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
            const string query = VisitorSelect + @"
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
            const string query = VisitorSelect + @"
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
            const string query = VisitorSelect + @"
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
    /// Backward-compatible visitor registration overload.
    /// </summary>
    public static int RegisterVisitor(int residentID, string visitorName, string phone, string email,
                                      string visitorType, string purpose)
    {
        return RegisterVisitor(residentID, visitorName, phone, email, "", purpose, DateTime.Now, BuildVisitorNote(visitorType, ""));
    }



    public static int RegisterVisitor(int residentID, string visitorName, string phone, string email,
                                      string idNumber, string visitorType, string purpose, DateTime arrivalTime, string note = null)
    {
        return RegisterVisitor(residentID, visitorName, phone, email, idNumber, purpose, arrivalTime, BuildVisitorNote(visitorType, note));
    }

    public static int RegisterVisitor(
    int residentID,
    string visitorName,
    string phone,
    string email,
    string idNumber,
    string visitorType,
    string purpose,
    DateTime arrivalTime,
    DateTime expectedDepartureTime,
    int guestCount,
    string note = null)
    {
        return RegisterVisitor(
            residentID,
            visitorName,
            phone,
            email,
            idNumber,
            purpose,
            arrivalTime,
            BuildVisitorNote(visitorType, note, expectedDepartureTime, guestCount));
    }

    public static bool UpdateResidentVisitor(
    int visitorID,
    int residentID,
    string visitorName,
    string phone,
    string email,
    string idNumber,
    string visitorType,
    string purpose,
    DateTime arrivalTime,
    DateTime expectedDepartureTime,
    int guestCount,
    string note = null)
    {
        try
        {
            const string query = @"
            UPDATE Visitors
            SET VisitorName = @VisitorName,
                Phone = @Phone,
                Email = @Email,
                IDNumber = @IDNumber,
                Purpose = @Purpose,
                ArrivalTime = @ArrivalTime,
                Note = @Note,
                UpdatedAt = GETDATE()
            WHERE VisitorID = @VisitorID
              AND ResidentID = @ResidentID
              AND Status = 'Pending'
        ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitorID", visitorID);
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    command.Parameters.AddWithValue("@VisitorName", visitorName);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@IDNumber", idNumber);
                    command.Parameters.AddWithValue("@Purpose", purpose);
                    command.Parameters.AddWithValue("@ArrivalTime", arrivalTime);
                    command.Parameters.AddWithValue("@Note", BuildVisitorNote(visitorType, note, expectedDepartureTime, guestCount));

                    connection.Open();
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating resident visitor: {VisitorID}", visitorID);
            return false;
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
                SET Status = 'Approved', ApprovedByUserID = NULLIF(@UserID, 0), UpdatedAt = GETDATE()
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
    /// Backward-compatible approval overload that records the approval time only.
    /// </summary>
    public static bool ApproveVisitor(int visitorID, DateTime approvedAt)
    {
        try
        {
            const string query = @"
                UPDATE Visitors
                SET Status = 'Approved', UpdatedAt = @ApprovedAt
                WHERE VisitorID = @VisitorID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitorID", visitorID);
                    command.Parameters.AddWithValue("@ApprovedAt", approvedAt);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Visitor approved (compatibility overload): {VisitorID}", visitorID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error approving visitor (compatibility overload): {VisitorID}", visitorID);
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
                SET Status = 'Rejected', ApprovedByUserID = NULLIF(@UserID, 0), UpdatedAt = GETDATE()
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
                SET DepartureTime = @DepartureTime, Status = 'CheckedOut', UpdatedAt = GETDATE()
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
        string? idNumber = reader.GetString(6);
        string rawNote = reader.IsDBNull(13) ? string.Empty : reader.GetString(13);
        var metadata = ParseVisitorNote(rawNote);
        string visitorType = ResolveVisitorType(metadata, rawNote, reader.GetString(7));
        string note = metadata.TryGetValue("NOTE", out var noteValue) ? noteValue : (IsVisitorType(rawNote) ? string.Empty : rawNote);
        DateTime? checkOutTime = reader.IsDBNull(9) ? null : reader.GetDateTime(9);

        int guestCount = 1;
        if (metadata.TryGetValue("COUNT", out var countText) && int.TryParse(countText, out int parsedCount))
        {
            guestCount = Math.Max(1, Math.Min(7, parsedCount));
        }

        DateTime? expectedDepartureTime = null;
        if (metadata.TryGetValue("EXPECTED_OUT", out var expectedText) &&
            DateTime.TryParse(expectedText, out DateTime parsedExpected))
        {
            expectedDepartureTime = parsedExpected;
        }

        return new
        {
            VisitorID = reader.GetInt32(0),
            ResidentID = reader.GetInt32(1),
            ResidentName = reader.GetString(2),
            VisitorName = reader.GetString(3),
            Phone = reader.GetString(4),
            Email = reader.GetString(5),
            IDNumber = idNumber,
            VisitorType = visitorType,
            Purpose = reader.GetString(7),
            CheckInTime = reader.GetDateTime(8),
            ArrivalTime = reader.GetDateTime(8),
            CheckOutTime = checkOutTime,
            DepartureTime = checkOutTime ?? DateTime.MinValue,
            ExpectedDepartureTime = expectedDepartureTime,
            GuestCount = guestCount,
            Status = reader.GetString(10),
            ApprovedByUserID = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
            ApprovedBy = reader.GetString(12),
            Note = note,
            CreatedAt = reader.GetDateTime(14),
            UpdatedAt = reader.GetDateTime(15),
            ApartmentID = reader.IsDBNull(16) ? 0 : reader.GetInt32(16),
            ApartmentCode = reader.GetString(17),
            BuildingName = reader.GetString(18),
            BlockName = reader.GetString(19)
        };
    }

    private static string BuildVisitorNote(string visitorType, string note)
    {
        string safeType = string.IsNullOrWhiteSpace(visitorType) ? "Guest" : visitorType.Trim();
        string safeNote = note ?? string.Empty;
        return $"TYPE={safeType};NOTE={safeNote}";
    }

    private static string BuildVisitorNote(string visitorType, string note, DateTime expectedDepartureTime, int guestCount)
    {
        string safeType = string.IsNullOrWhiteSpace(visitorType) ? "Guest" : visitorType.Trim();
        string safeNote = (note ?? string.Empty).Replace(";", ",");
        int safeCount = Math.Max(1, Math.Min(7, guestCount));

        return $"TYPE={safeType};COUNT={safeCount};EXPECTED_OUT={expectedDepartureTime:yyyy-MM-dd HH:mm};NOTE={safeNote}";
    }

    private static Dictionary<string, string> ParseVisitorNote(string rawNote)
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

    private static string ResolveVisitorType(Dictionary<string, string> metadata, string rawNote, string purpose)
    {
        if (metadata.TryGetValue("TYPE", out var type) && IsVisitorType(type))
        {
            return type;
        }

        if (IsVisitorType(rawNote))
        {
            return rawNote;
        }

        string text = $"{rawNote} {purpose}";
        if (text.Contains("giao", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ship", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("delivery", StringComparison.OrdinalIgnoreCase))
        {
            return "Delivery";
        }

        if (text.Contains("sửa", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("lắp", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("dịch vụ", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("service", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("nhà thầu", StringComparison.OrdinalIgnoreCase))
        {
            return "Service";
        }

        if (text.Contains("gia đình", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("người thân", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("family", StringComparison.OrdinalIgnoreCase))
        {
            return "Family";
        }

        return "Guest";
    }

    private static bool IsVisitorType(string value)
    {
        return string.Equals(value, "Guest", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Delivery", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Service", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Family", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "Other", StringComparison.OrdinalIgnoreCase);
    }
}


