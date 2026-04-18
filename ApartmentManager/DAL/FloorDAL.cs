using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Floor operations
/// </summary>
public class FloorDAL
{
    /// <summary>
    /// Get floor by ID
    /// </summary>
    public static dynamic? GetFloorByID(int floorID)
    {
        try
        {
            const string query = @"
                SELECT FloorID, FloorNumber, BlockID, CreatedAt, UpdatedAt
                FROM Floors
                WHERE FloorID = @FloorID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FloorID", floorID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                FloorID = reader.GetInt32(0),
                                FloorNumber = reader.GetInt32(1),
                                BlockID = reader.GetInt32(2),
                                CreatedAt = reader.GetDateTime(3),
                                UpdatedAt = reader.GetDateTime(4)
                            };
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting floor by ID: {FloorID}", floorID);
            return null;
        }
    }

    /// <summary>
    /// Get floors by block
    /// </summary>
    public static List<dynamic> GetFloorsByBlock(int blockID)
    {
        var floors = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT FloorID, FloorNumber, BlockID, CreatedAt, UpdatedAt
                FROM Floors
                WHERE BlockID = @BlockID
                ORDER BY FloorNumber
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BlockID", blockID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            floors.Add(new
                            {
                                FloorID = reader.GetInt32(0),
                                FloorNumber = reader.GetInt32(1),
                                BlockID = reader.GetInt32(2),
                                CreatedAt = reader.GetDateTime(3),
                                UpdatedAt = reader.GetDateTime(4)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting floors by block: {BlockID}", blockID);
        }

        return floors;
    }

    /// <summary>
    /// Create floor
    /// </summary>
    public static int CreateFloor(int floorNumber, int blockID)
    {
        try
        {
            const string query = @"
                INSERT INTO Floors (FloorNumber, BlockID)
                VALUES (@FloorNumber, @BlockID)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FloorNumber", floorNumber);
                    command.Parameters.AddWithValue("@BlockID", blockID);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var floorID = Convert.ToInt32(result);

                    Log.Information("Floor created: Floor {FloorNumber} in Block {BlockID} (ID: {FloorID})", floorNumber, blockID, floorID);
                    return floorID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating floor: {FloorNumber}", floorNumber);
            throw;
        }
    }

    /// <summary>
    /// Update floor
    /// </summary>
    public static bool UpdateFloor(int floorID, int floorNumber)
    {
        try
        {
            const string query = @"
                UPDATE Floors
                SET FloorNumber = @FloorNumber, UpdatedAt = GETDATE()
                WHERE FloorID = @FloorID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FloorID", floorID);
                    command.Parameters.AddWithValue("@FloorNumber", floorNumber);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Floor updated: {FloorID}", floorID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating floor: {FloorID}", floorID);
            return false;
        }
    }

    /// <summary>
    /// Delete floor
    /// </summary>
    public static bool DeleteFloor(int floorID)
    {
        try
        {
            const string query = "DELETE FROM Floors WHERE FloorID = @FloorID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FloorID", floorID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Floor deleted: {FloorID}", floorID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting floor: {FloorID}", floorID);
            return false;
        }
    }
}


