using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Block operations
/// </summary>
public class BlockDAL
{
    /// <summary>
    /// Get block by ID
    /// </summary>
    public static dynamic? GetBlockByID(int blockID)
    {
        try
        {
            const string query = @"
                SELECT BlockID, BlockName, BuildingID, CreatedAt, UpdatedAt
                FROM Blocks
                WHERE BlockID = @BlockID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BlockID", blockID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                BlockID = reader.GetInt32(0),
                                BlockName = reader.GetString(1),
                                BuildingID = reader.GetInt32(2),
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
            Log.Error(ex, "Error getting block by ID: {BlockID}", blockID);
            return null;
        }
    }

    /// <summary>
    /// Get blocks by building
    /// </summary>
    public static List<dynamic> GetBlocksByBuilding(int buildingID)
    {
        var blocks = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT BlockID, BlockName, BuildingID, CreatedAt, UpdatedAt
                FROM Blocks
                WHERE BuildingID = @BuildingID
                ORDER BY BlockName
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
                        {
                            blocks.Add(new
                            {
                                BlockID = reader.GetInt32(0),
                                BlockName = reader.GetString(1),
                                BuildingID = reader.GetInt32(2),
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
            Log.Error(ex, "Error getting blocks by building: {BuildingID}", buildingID);
        }

        return blocks;
    }

    /// <summary>
    /// Create block
    /// </summary>
    public static int CreateBlock(string blockName, int buildingID)
    {
        try
        {
            const string query = @"
                INSERT INTO Blocks (BlockName, BuildingID)
                VALUES (@BlockName, @BuildingID)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BlockName", blockName);
                    command.Parameters.AddWithValue("@BuildingID", buildingID);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var blockID = Convert.ToInt32(result);

                    Log.Information("Block created: {BlockName} in Building {BuildingID} (ID: {BlockID})", blockName, buildingID, blockID);
                    return blockID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating block: {BlockName}", blockName);
            throw;
        }
    }

    /// <summary>
    /// Update block
    /// </summary>
    public static bool UpdateBlock(int blockID, string blockName)
    {
        try
        {
            const string query = @"
                UPDATE Blocks
                SET BlockName = @BlockName, UpdatedAt = GETDATE()
                WHERE BlockID = @BlockID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BlockID", blockID);
                    command.Parameters.AddWithValue("@BlockName", blockName);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Block updated: {BlockID}", blockID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating block: {BlockID}", blockID);
            return false;
        }
    }

    /// <summary>
    /// Delete block
    /// </summary>
    public static bool DeleteBlock(int blockID)
    {
        try
        {
            const string query = "DELETE FROM Blocks WHERE BlockID = @BlockID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BlockID", blockID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Block deleted: {BlockID}", blockID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting block: {BlockID}", blockID);
            return false;
        }
    }
}


