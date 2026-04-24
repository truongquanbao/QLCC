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
                assets.Add(new AssetRecord
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
                });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Assets table is unavailable or cannot be read");
        }

        return assets;
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
}
