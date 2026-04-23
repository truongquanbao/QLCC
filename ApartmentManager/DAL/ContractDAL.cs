using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Contract operations
/// </summary>
public class ContractDAL
{
    /// <summary>
    /// Get contract by ID
    /// </summary>
    public static dynamic? GetContractByID(int contractID)
    {
        try
        {
            const string query = @"
                SELECT c.ContractID, c.ApartmentID, a.ApartmentCode, c.ResidentID, r.FullName,
                       c.StartDate, c.EndDate, c.RentAmount, c.DepositAmount, c.Status,
                       c.CreatedAt, c.UpdatedAt, c.Note
                FROM Contracts c
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                WHERE c.ContractID = @ContractID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ContractID", contractID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapContract(reader);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting contract by ID: {ContractID}", contractID);
            return null;
        }
    }

    /// <summary>
    /// Get all contracts
    /// </summary>
    public static List<dynamic> GetAllContracts()
    {
        var contracts = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT c.ContractID, c.ApartmentID, a.ApartmentCode, c.ResidentID, r.FullName,
                       c.StartDate, c.EndDate, c.RentAmount, c.DepositAmount, c.Status,
                       c.CreatedAt, c.UpdatedAt, c.Note
                FROM Contracts c
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                ORDER BY c.StartDate DESC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            contracts.Add(MapContract(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all contracts");
        }

        return contracts;
    }

    /// <summary>
    /// Get contracts by apartment
    /// </summary>
    public static List<dynamic> GetContractsByApartment(int apartmentID)
    {
        var contracts = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT c.ContractID, c.ApartmentID, a.ApartmentCode, c.ResidentID, r.FullName,
                       c.StartDate, c.EndDate, c.RentAmount, c.DepositAmount, c.Status,
                       c.CreatedAt, c.UpdatedAt, c.Note
                FROM Contracts c
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                WHERE c.ApartmentID = @ApartmentID
                ORDER BY c.StartDate DESC
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
                            contracts.Add(MapContract(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting contracts by apartment: {ApartmentID}", apartmentID);
        }

        return contracts;
    }

    /// <summary>
    /// Get active contracts
    /// </summary>
    public static List<dynamic> GetActiveContracts()
    {
        var contracts = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT c.ContractID, c.ApartmentID, a.ApartmentCode, c.ResidentID, r.FullName,
                       c.StartDate, c.EndDate, c.RentAmount, c.DepositAmount, c.Status,
                       c.CreatedAt, c.UpdatedAt, c.Note
                FROM Contracts c
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                WHERE c.Status = 'Active' AND c.StartDate <= GETDATE() AND c.EndDate >= GETDATE()
                ORDER BY c.EndDate ASC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            contracts.Add(MapContract(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting active contracts");
        }

        return contracts;
    }

    /// <summary>
    /// Get expiring contracts (within specified days)
    /// </summary>
    public static List<dynamic> GetExpiringContracts(int withinDays = 30)
    {
        var contracts = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT c.ContractID, c.ApartmentID, a.ApartmentCode, c.ResidentID, r.FullName,
                       c.StartDate, c.EndDate, c.RentAmount, c.DepositAmount, c.Status,
                       c.CreatedAt, c.UpdatedAt, c.Note
                FROM Contracts c
                INNER JOIN Apartments a ON c.ApartmentID = a.ApartmentID
                INNER JOIN Residents r ON c.ResidentID = r.ResidentID
                WHERE c.Status = 'Active' AND c.EndDate BETWEEN GETDATE() AND DATEADD(day, @WithinDays, GETDATE())
                ORDER BY c.EndDate ASC
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@WithinDays", withinDays);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            contracts.Add(MapContract(reader));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting expiring contracts");
        }

        return contracts;
    }

    /// <summary>
    /// Create contract
    /// </summary>
    public static int CreateContract(int apartmentID, int residentID, DateTime startDate, DateTime endDate,
                                     decimal rentAmount, decimal depositAmount, string? note = null)
    {
        try
        {
            const string query = @"
                INSERT INTO Contracts (ApartmentID, ResidentID, StartDate, EndDate, RentAmount, DepositAmount, Status, Note)
                VALUES (@ApartmentID, @ResidentID, @StartDate, @EndDate, @RentAmount, @DepositAmount, 'Active', @Note)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ApartmentID", apartmentID);
                    command.Parameters.AddWithValue("@ResidentID", residentID);
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    command.Parameters.AddWithValue("@RentAmount", rentAmount);
                    command.Parameters.AddWithValue("@DepositAmount", depositAmount);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var contractID = Convert.ToInt32(result);

                    Log.Information("Contract created: Apartment {ApartmentID} (ID: {ContractID})", apartmentID, contractID);
                    return contractID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating contract");
            throw;
        }
    }

    /// <summary>
    /// Backward-compatible contract creation overload.
    /// </summary>
    public static int CreateContract(int apartmentID, int residentID, string contractType, DateTime startDate,
                                     DateTime endDate, int termMonths, bool autoRenewal, string? note = null)
    {
        string compatibilityNote = string.IsNullOrWhiteSpace(note)
            ? $"ContractType:{contractType};AutoRenewal:{autoRenewal}"
            : $"ContractType:{contractType};AutoRenewal:{autoRenewal}|{note}";

        int contractID = CreateContract(apartmentID, residentID, startDate, endDate, 0m, 0m, compatibilityNote);

        if (contractID > 0)
            UpdateContractStatus(contractID, "Pending");

        return contractID;
    }

    /// <summary>
    /// Update contract
    /// </summary>
    public static bool UpdateContract(int contractID, DateTime startDate, DateTime endDate,
                                      decimal rentAmount, decimal depositAmount, string? note = null)
    {
        try
        {
            const string query = @"
                UPDATE Contracts
                SET StartDate = @StartDate, EndDate = @EndDate, RentAmount = @RentAmount,
                    DepositAmount = @DepositAmount, Note = @Note, UpdatedAt = GETDATE()
                WHERE ContractID = @ContractID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ContractID", contractID);
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    command.Parameters.AddWithValue("@RentAmount", rentAmount);
                    command.Parameters.AddWithValue("@DepositAmount", depositAmount);
                    command.Parameters.AddWithValue("@Note", note ?? (object)DBNull.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Contract updated: {ContractID}", contractID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating contract: {ContractID}", contractID);
            return false;
        }
    }

    /// <summary>
    /// Update contract status
    /// </summary>
    public static bool UpdateContractStatus(int contractID, string status)
    {
        try
        {
            const string query = @"
                UPDATE Contracts
                SET Status = @Status, UpdatedAt = GETDATE()
                WHERE ContractID = @ContractID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ContractID", contractID);
                    command.Parameters.AddWithValue("@Status", status);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Contract status updated: {ContractID} to {Status}", contractID, status);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating contract status: {ContractID}", contractID);
            return false;
        }
    }

    /// <summary>
    /// Delete contract
    /// </summary>
    public static bool DeleteContract(int contractID)
    {
        try
        {
            const string query = "DELETE FROM Contracts WHERE ContractID = @ContractID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ContractID", contractID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Contract deleted: {ContractID}", contractID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting contract: {ContractID}", contractID);
            return false;
        }
    }

    /// <summary>
    /// Map SqlDataReader to contract object
    /// </summary>
    private static dynamic MapContract(SqlDataReader reader)
    {
        string? rawNote = reader.IsDBNull(12) ? null : reader.GetString(12);
        string? contractType = null;
        string? note = rawNote;
        bool autoRenewal = false;

        if (!string.IsNullOrWhiteSpace(rawNote) && rawNote.StartsWith("ContractType:", StringComparison.OrdinalIgnoreCase))
        {
            int separatorIndex = rawNote.IndexOf('|');
            string metadata = separatorIndex >= 0 ? rawNote.Substring(0, separatorIndex) : rawNote;

            foreach (var part in metadata.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("ContractType:", StringComparison.OrdinalIgnoreCase))
                {
                    contractType = part.Substring("ContractType:".Length);
                }
                else if (part.StartsWith("AutoRenewal:", StringComparison.OrdinalIgnoreCase) &&
                         bool.TryParse(part.Substring("AutoRenewal:".Length), out var parsedAutoRenewal))
                {
                    autoRenewal = parsedAutoRenewal;
                }
            }

            note = separatorIndex + 1 < rawNote.Length ? rawNote.Substring(separatorIndex + 1) : null;
        }

        return new
        {
            ContractID = reader.GetInt32(0),
            ApartmentID = reader.GetInt32(1),
            ApartmentCode = reader.GetString(2),
            ResidentID = reader.GetInt32(3),
            ResidentName = reader.GetString(4),
            StartDate = reader.GetDateTime(5),
            EndDate = reader.GetDateTime(6),
            RentAmount = reader.GetDecimal(7),
            DepositAmount = reader.GetDecimal(8),
            Status = reader.GetString(9),
            ContractType = contractType,
            AutoRenewal = autoRenewal,
            CreatedAt = reader.GetDateTime(10),
            UpdatedAt = reader.GetDateTime(11),
            Note = note
        };
    }
}


