using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.BLL;

/// <summary>
/// Business Logic Layer for Apartment management
/// </summary>
public class ApartmentBLL
{
    /// <summary>
    /// Get apartment by ID with validation
    /// </summary>
    public static dynamic? GetApartmentByID(int apartmentID)
    {
        try
        {
            if (apartmentID <= 0)
            {
                Log.Warning("Invalid apartment ID: {ApartmentID}", apartmentID);
                return null;
            }

            return ApartmentDAL.GetApartmentByID(apartmentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting apartment: {ApartmentID}", apartmentID);
            return null;
        }
    }

    /// <summary>
    /// Get all apartments
    /// </summary>
    public static List<dynamic> GetAllApartments()
    {
        try
        {
            return ApartmentDAL.GetAllApartments();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting all apartments");
            return new List<dynamic>();
        }
    }

    /// <summary>
    /// Get apartments by status with validation
    /// </summary>
    public static List<dynamic> GetApartmentsByStatus(string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                Log.Warning("Invalid apartment status");
                return new List<dynamic>();
            }

            var validStatuses = new[] { "Empty", "Occupied", "Renting", "Maintenance", "Locked" };
            if (!validStatuses.Contains(status))
            {
                Log.Warning("Unknown apartment status: {Status}", status);
                return new List<dynamic>();
            }

            return ApartmentDAL.GetApartmentsByStatus(status);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting apartments by status: {Status}", status);
            return new List<dynamic>();
        }
    }

    /// <summary>
    /// Create apartment with validation
    /// </summary>
    public static (bool Success, string Message, int ApartmentID) CreateApartment(
        string apartmentCode, int floorID, decimal area, string apartmentType, int maxResidents, string? note = null)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(apartmentCode))
                return (false, "Apartment code is required", 0);

            if (apartmentCode.Length > 20)
                return (false, "Apartment code must be less than 20 characters", 0);

            if (floorID <= 0)
                return (false, "Invalid floor ID", 0);

            if (area <= 0)
                return (false, "Area must be greater than 0", 0);

            if (area > 1000)
                return (false, "Area cannot exceed 1000 m²", 0);

            if (string.IsNullOrWhiteSpace(apartmentType))
                return (false, "Apartment type is required", 0);

            if (maxResidents <= 0 || maxResidents > 20)
                return (false, "Max residents must be between 1 and 20", 0);

            if (ApartmentDAL.ApartmentCodeExists(apartmentCode))
                return (false, "Apartment code already exists", 0);

            int apartmentID = ApartmentDAL.CreateApartment(apartmentCode, floorID, area, apartmentType, maxResidents, note);
            Log.Information("Apartment created via BLL: {ApartmentCode} (ID: {ApartmentID})", apartmentCode, apartmentID);

            return (true, "Apartment created successfully", apartmentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error creating apartment: {ApartmentCode}", apartmentCode);
            return (false, $"Error creating apartment: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Update apartment with validation
    /// </summary>
    public static (bool Success, string Message) UpdateApartment(
        int apartmentID, string apartmentCode, decimal area, string apartmentType, int maxResidents, string? note = null)
    {
        try
        {
            // Validation
            if (apartmentID <= 0)
                return (false, "Invalid apartment ID");

            if (string.IsNullOrWhiteSpace(apartmentCode))
                return (false, "Apartment code is required");

            if (apartmentCode.Length > 20)
                return (false, "Apartment code must be less than 20 characters");

            if (area <= 0 || area > 1000)
                return (false, "Area must be between 0 and 1000 m²");

            if (string.IsNullOrWhiteSpace(apartmentType))
                return (false, "Apartment type is required");

            if (maxResidents <= 0 || maxResidents > 20)
                return (false, "Max residents must be between 1 and 20");

            if (ApartmentDAL.ApartmentCodeExists(apartmentCode, apartmentID))
                return (false, "Apartment code already exists");

            bool success = ApartmentDAL.UpdateApartment(apartmentID, apartmentCode, area, apartmentType, maxResidents, note);

            if (success)
            {
                Log.Information("Apartment updated via BLL: {ApartmentID}", apartmentID);
                return (true, "Apartment updated successfully");
            }

            return (false, "Failed to update apartment");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error updating apartment: {ApartmentID}", apartmentID);
            return (false, $"Error updating apartment: {ex.Message}");
        }
    }

    /// <summary>
    /// Update apartment status with validation
    /// </summary>
    public static (bool Success, string Message) UpdateApartmentStatus(int apartmentID, string status)
    {
        try
        {
            if (apartmentID <= 0)
                return (false, "Invalid apartment ID");

            if (string.IsNullOrWhiteSpace(status))
                return (false, "Status is required");

            var validStatuses = new[] { "Empty", "Occupied", "Renting", "Maintenance", "Locked" };
            if (!validStatuses.Contains(status))
                return (false, $"Invalid status. Valid statuses: {string.Join(", ", validStatuses)}");

            bool success = ApartmentDAL.UpdateApartmentStatus(apartmentID, status);

            if (success)
            {
                Log.Information("Apartment status updated via BLL: {ApartmentID} to {Status}", apartmentID, status);
                return (true, $"Apartment status updated to {status}");
            }

            return (false, "Failed to update apartment status");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error updating apartment status: {ApartmentID}", apartmentID);
            return (false, $"Error updating apartment status: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete apartment
    /// </summary>
    public static (bool Success, string Message) DeleteApartment(int apartmentID)
    {
        try
        {
            if (apartmentID <= 0)
                return (false, "Invalid apartment ID");

            // Check if apartment has residents
            var residents = ResidentDAL.GetResidentsByStatus("Active");
            if (residents.Any(r => r.ApartmentID == apartmentID))
                return (false, "Cannot delete apartment with active residents");

            bool success = ApartmentDAL.DeleteApartment(apartmentID);

            if (success)
            {
                Log.Information("Apartment deleted via BLL: {ApartmentID}", apartmentID);
                return (true, "Apartment deleted successfully");
            }

            return (false, "Failed to delete apartment");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error deleting apartment: {ApartmentID}", apartmentID);
            return (false, $"Error deleting apartment: {ex.Message}");
        }
    }

    /// <summary>
    /// Get apartment with resident count
    /// </summary>
    public static dynamic? GetApartmentStatus(int apartmentID)
    {
        try
        {
            if (apartmentID <= 0)
                return null;

            return ApartmentDAL.GetApartmentWithResidentCount(apartmentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting apartment status: {ApartmentID}", apartmentID);
            return null;
        }
    }

    /// <summary>
    /// Get occupancy statistics
    /// </summary>
    public static dynamic GetOccupancyStatistics()
    {
        try
        {
            var apartments = ApartmentDAL.GetAllApartments();

            var stats = new
            {
                TotalApartments = apartments.Count,
                EmptyApartments = apartments.Count(a => a.Status == "Empty"),
                OccupiedApartments = apartments.Count(a => a.Status == "Occupied"),
                RentingApartments = apartments.Count(a => a.Status == "Renting"),
                MaintenanceApartments = apartments.Count(a => a.Status == "Maintenance"),
                LockedApartments = apartments.Count(a => a.Status == "Locked"),
                OccupancyRate = apartments.Count > 0 
                    ? ((apartments.Count(a => a.Status == "Occupied" || a.Status == "Renting") * 100.0) / apartments.Count).ToString("F2") + "%"
                    : "0%"
            };

            return stats;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting occupancy statistics");
            return null;
        }
    }
}
