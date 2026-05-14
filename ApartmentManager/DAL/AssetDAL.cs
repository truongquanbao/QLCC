using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;

namespace ApartmentManager.DAL;

public static class AssetDAL
{
    public sealed class AssetRecord
    {
        public int AssetID { get; init; }
        public string AssetCode { get; init; } = "";
        public string AssetName { get; init; } = "";
        public string AssetType { get; init; } = "";
        public string Location { get; init; } = "";
        public DateTime? PurchaseDate { get; init; }
        public string Condition { get; init; } = "";
        public DateTime? LastMaintenanceDate { get; init; }
        public DateTime? NextMaintenanceDate { get; init; }
        public decimal RepairCost { get; init; }
        public string Note { get; init; } = "";
    }

    public sealed class MaintenanceRecord
    {
        public int MaintenanceID { get; init; }
        public int AssetID { get; init; }
        public string AssetName { get; init; } = "";
        public string Location { get; init; } = "";
        public string Category { get; init; } = "";
        public DateTime ScheduledDate { get; init; }
        public string Status { get; init; } = "";
        public string AssignedTo { get; init; } = "";
        public string Note { get; init; } = "";
    }

    public static AssetRecord? GetAssetByID(int assetID)
    {
        try
        {
            const string query = @"
                SELECT AssetID, AssetCode, AssetName, AssetType, Location, PurchaseDate,
                       Condition, LastMaintenanceDate, NextMaintenanceDate, RepairCost, Note
                FROM Assets
                WHERE AssetID = @AssetID";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@AssetID", assetID);
            connection.Open();
            using var reader = command.ExecuteReader();
            return reader.Read() ? MapAsset(reader) : null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Asset cannot be read: {AssetID}", assetID);
            return null;
        }
    }

    public static List<AssetRecord> GetAllAssets()
    {
        var assets = new List<AssetRecord>();
        try
        {
            const string query = @"
                SELECT AssetID, AssetCode, AssetName, AssetType, Location, PurchaseDate,
                       Condition, LastMaintenanceDate, NextMaintenanceDate, RepairCost, Note
                FROM Assets
                ORDER BY AssetType, Location, AssetName";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                assets.Add(MapAsset(reader));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Assets table is unavailable or cannot be read");
        }

        return assets;
    }

    public static int CreateAsset(
        string assetCode,
        string assetName,
        string assetType,
        string location,
        DateTime? purchaseDate,
        string condition,
        DateTime? lastMaintenanceDate,
        DateTime? nextMaintenanceDate,
        decimal repairCost,
        string note)
    {
        try
        {
            const string query = @"
                INSERT INTO Assets
                    (AssetCode, AssetName, AssetType, Location, PurchaseDate, Condition,
                     LastMaintenanceDate, NextMaintenanceDate, RepairCost, Note)
                VALUES
                    (@AssetCode, @AssetName, @AssetType, @Location, @PurchaseDate, @Condition,
                     @LastMaintenanceDate, @NextMaintenanceDate, @RepairCost, @Note);
                SELECT SCOPE_IDENTITY();";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            AddAssetParameters(command, assetCode, assetName, assetType, location, purchaseDate, condition, lastMaintenanceDate, nextMaintenanceDate, repairCost, note);
            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating asset: {AssetCode}", assetCode);
            return 0;
        }
    }

    public static bool UpdateAsset(
        int assetID,
        string assetCode,
        string assetName,
        string assetType,
        string location,
        DateTime? purchaseDate,
        string condition,
        DateTime? lastMaintenanceDate,
        DateTime? nextMaintenanceDate,
        decimal repairCost,
        string note)
    {
        try
        {
            const string query = @"
                UPDATE Assets
                SET AssetCode = @AssetCode,
                    AssetName = @AssetName,
                    AssetType = @AssetType,
                    Location = @Location,
                    PurchaseDate = @PurchaseDate,
                    Condition = @Condition,
                    LastMaintenanceDate = @LastMaintenanceDate,
                    NextMaintenanceDate = @NextMaintenanceDate,
                    RepairCost = @RepairCost,
                    Note = @Note,
                    UpdatedAt = GETDATE()
                WHERE AssetID = @AssetID";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@AssetID", assetID);
            AddAssetParameters(command, assetCode, assetName, assetType, location, purchaseDate, condition, lastMaintenanceDate, nextMaintenanceDate, repairCost, note);
            connection.Open();
            return command.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating asset: {AssetID}", assetID);
            return false;
        }
    }

    public static bool DeleteAsset(int assetID)
    {
        try
        {
            using var connection = DatabaseHelper.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using (var schedules = new SqlCommand("DELETE FROM MaintenanceSchedules WHERE AssetID = @AssetID", connection, transaction))
            {
                schedules.Parameters.AddWithValue("@AssetID", assetID);
                schedules.ExecuteNonQuery();
            }

            int affected;
            using (var asset = new SqlCommand("DELETE FROM Assets WHERE AssetID = @AssetID", connection, transaction))
            {
                asset.Parameters.AddWithValue("@AssetID", assetID);
                affected = asset.ExecuteNonQuery();
            }

            transaction.Commit();
            return affected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting asset: {AssetID}", assetID);
            return false;
        }
    }

    public static List<MaintenanceRecord> GetMaintenanceSchedules()
    {
        var schedules = new List<MaintenanceRecord>();
        try
        {
            const string query = @"
                SELECT m.MaintenanceID, m.AssetID, a.AssetName, a.Location, m.Category,
                       m.ScheduledDate, m.Status, m.AssignedTo, m.Note
                FROM MaintenanceSchedules m
                INNER JOIN Assets a ON m.AssetID = a.AssetID
                ORDER BY m.ScheduledDate";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                schedules.Add(new MaintenanceRecord
                {
                    MaintenanceID = reader.GetInt32(0),
                    AssetID = reader.GetInt32(1),
                    AssetName = reader.GetString(2),
                    Location = reader.GetString(3),
                    Category = reader.GetString(4),
                    ScheduledDate = reader.GetDateTime(5),
                    Status = reader.GetString(6),
                    AssignedTo = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Note = reader.IsDBNull(8) ? "" : reader.GetString(8)
                });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "MaintenanceSchedules table is unavailable or cannot be read");
        }

        return schedules;
    }

    public static int CreateMaintenanceSchedule(
        int assetID,
        string category,
        DateTime scheduledDate,
        string status,
        string assignedTo,
        string note)
    {
        try
        {
            const string query = @"
                INSERT INTO MaintenanceSchedules (AssetID, Category, ScheduledDate, Status, AssignedTo, Note)
                VALUES (@AssetID, @Category, @ScheduledDate, @Status, @AssignedTo, @Note);
                SELECT SCOPE_IDENTITY();";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@AssetID", assetID);
            command.Parameters.AddWithValue("@Category", category);
            command.Parameters.AddWithValue("@ScheduledDate", scheduledDate.Date);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@AssignedTo", string.IsNullOrWhiteSpace(assignedTo) ? (object)DBNull.Value : assignedTo.Trim());
            command.Parameters.AddWithValue("@Note", string.IsNullOrWhiteSpace(note) ? (object)DBNull.Value : note.Trim());
            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating maintenance schedule for asset: {AssetID}", assetID);
            return 0;
        }
    }

    public static bool UpdateMaintenanceStatus(int maintenanceID, string status)
    {
        try
        {
            const string query = @"
                UPDATE MaintenanceSchedules
                SET Status = @Status, UpdatedAt = GETDATE()
                WHERE MaintenanceID = @MaintenanceID";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MaintenanceID", maintenanceID);
            command.Parameters.AddWithValue("@Status", status);
            connection.Open();
            return command.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating maintenance schedule: {MaintenanceID}", maintenanceID);
            return false;
        }
    }

    private static AssetRecord MapAsset(SqlDataReader reader)
    {
        return new AssetRecord
        {
            AssetID = reader.GetInt32(0),
            AssetCode = reader.GetString(1),
            AssetName = reader.GetString(2),
            AssetType = reader.GetString(3),
            Location = reader.GetString(4),
            PurchaseDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            Condition = reader.GetString(6),
            LastMaintenanceDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
            NextMaintenanceDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            RepairCost = reader.GetDecimal(9),
            Note = reader.IsDBNull(10) ? "" : reader.GetString(10)
        };
    }

    private static void AddAssetParameters(
        SqlCommand command,
        string assetCode,
        string assetName,
        string assetType,
        string location,
        DateTime? purchaseDate,
        string condition,
        DateTime? lastMaintenanceDate,
        DateTime? nextMaintenanceDate,
        decimal repairCost,
        string note)
    {
        command.Parameters.AddWithValue("@AssetCode", assetCode.Trim());
        command.Parameters.AddWithValue("@AssetName", assetName.Trim());
        command.Parameters.AddWithValue("@AssetType", assetType.Trim());
        command.Parameters.AddWithValue("@Location", location.Trim());
        command.Parameters.AddWithValue("@PurchaseDate", purchaseDate.HasValue ? purchaseDate.Value.Date : (object)DBNull.Value);
        command.Parameters.AddWithValue("@Condition", string.IsNullOrWhiteSpace(condition) ? "Tốt" : condition.Trim());
        command.Parameters.AddWithValue("@LastMaintenanceDate", lastMaintenanceDate.HasValue ? lastMaintenanceDate.Value.Date : (object)DBNull.Value);
        command.Parameters.AddWithValue("@NextMaintenanceDate", nextMaintenanceDate.HasValue ? nextMaintenanceDate.Value.Date : (object)DBNull.Value);
        command.Parameters.AddWithValue("@RepairCost", repairCost);
        command.Parameters.AddWithValue("@Note", string.IsNullOrWhiteSpace(note) ? (object)DBNull.Value : note.Trim());
    }
}
