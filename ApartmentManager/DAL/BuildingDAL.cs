using Microsoft.Data.SqlClient;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.DAL;

/// <summary>
/// Data Access Layer for Building operations
/// </summary>
public class BuildingDAL
{
    /// <summary>
    /// Get building by ID
    /// </summary>
    public static dynamic? GetBuildingByID(int buildingID)
    {
        try
        {
            const string query = @"
                SELECT BuildingID, BuildingName, Address, Description, CreatedAt, UpdatedAt
                FROM Buildings
                WHERE BuildingID = @BuildingID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BuildingID", buildingID);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                BuildingID = reader.GetInt32(0),
                                BuildingName = reader.GetString(1),
                                Address = reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                CreatedAt = reader.GetDateTime(4),
                                UpdatedAt = reader.GetDateTime(5)
                            };
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting building by ID: {BuildingID}", buildingID);
            return null;
        }
    }

    /// <summary>
    /// Get all buildings
    /// </summary>
    public static List<dynamic> GetAllBuildings()
    {
        var buildings = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT BuildingID, BuildingName, Address, Description, CreatedAt, UpdatedAt
                FROM Buildings
                ORDER BY BuildingName
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            buildings.Add(new
                            {
                                BuildingID = reader.GetInt32(0),
                                BuildingName = reader.GetString(1),
                                Address = reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                CreatedAt = reader.GetDateTime(4),
                                UpdatedAt = reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all buildings");
        }

        return buildings;
    }

    /// <summary>
    /// Create building
    /// </summary>
    public static int CreateBuilding(string buildingName, string address, string? description = null)
    {
        try
        {
            const string query = @"
                INSERT INTO Buildings (BuildingName, Address, Description)
                VALUES (@BuildingName, @Address, @Description)
                SELECT SCOPE_IDENTITY()
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BuildingName", buildingName);
                    command.Parameters.AddWithValue("@Address", address);
                    command.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    var buildingID = Convert.ToInt32(result);

                    Log.Information("Building created: {BuildingName} (ID: {BuildingID})", buildingName, buildingID);
                    return buildingID;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating building: {BuildingName}", buildingName);
            throw;
        }
    }

    /// <summary>
    /// Update building
    /// </summary>
    public static bool UpdateBuilding(int buildingID, string buildingName, string address, string? description = null)
    {
        try
        {
            const string query = @"
                UPDATE Buildings
                SET BuildingName = @BuildingName, Address = @Address, Description = @Description, UpdatedAt = GETDATE()
                WHERE BuildingID = @BuildingID
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BuildingID", buildingID);
                    command.Parameters.AddWithValue("@BuildingName", buildingName);
                    command.Parameters.AddWithValue("@Address", address);
                    command.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Building updated: {BuildingID}", buildingID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating building: {BuildingID}", buildingID);
            return false;
        }
    }

    /// <summary>
    /// Delete building
    /// </summary>
    public static bool DeleteBuilding(int buildingID)
    {
        try
        {
            const string query = "DELETE FROM Buildings WHERE BuildingID = @BuildingID";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BuildingID", buildingID);
                    connection.Open();
                    command.ExecuteNonQuery();

                    Log.Information("Building deleted: {BuildingID}", buildingID);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting building: {BuildingID}", buildingID);
            return false;
        }
    }

    /// <summary>
    /// Get buildings with block count
    /// </summary>
    public static List<dynamic> GetBuildingsWithBlockCount()
    {
        var buildings = new List<dynamic>();

        try
        {
            const string query = @"
                SELECT 
                    b.BuildingID,
                    b.BuildingName,
                    b.Address,
                    COUNT(bl.BlockID) as BlockCount,
                    (SELECT COUNT(*) FROM Apartments a 
                     INNER JOIN Floors f ON a.FloorID = f.FloorID
                     INNER JOIN Blocks bl2 ON f.BlockID = bl2.BlockID
                     WHERE bl2.BuildingID = b.BuildingID) as ApartmentCount
                FROM Buildings b
                LEFT JOIN Blocks bl ON b.BuildingID = bl.BuildingID
                GROUP BY b.BuildingID, b.BuildingName, b.Address
                ORDER BY b.BuildingName
            ";

            using (var connection = DatabaseHelper.CreateConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            buildings.Add(new
                            {
                                BuildingID = reader.GetInt32(0),
                                BuildingName = reader.GetString(1),
                                Address = reader.GetString(2),
                                BlockCount = reader.GetInt32(3),
                                ApartmentCount = reader.GetInt32(4)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting buildings with block count");
        }

        return buildings;
    }
}
