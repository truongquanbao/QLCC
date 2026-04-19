using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApartmentManager.BLL;

/// <summary>
/// Business Logic Layer for Vehicle management
/// </summary>
public class VehicleBLL
{
    /// <summary>
    /// Create a new vehicle with comprehensive validation
    /// </summary>
    public static (bool Success, string Message, int VehicleID) CreateVehicle(
        int residentID,
        string licensePlate,
        string vehicleType,
        string brand,
        string model,
        int yearMade,
        string? color = null,
        string? note = null)
    {
        try
        {
            // Validate input
            if (residentID <= 0)
                return (false, "Please select a valid resident.", 0);

            if (string.IsNullOrWhiteSpace(licensePlate))
                return (false, "License plate is required.", 0);

            if (!ValidationHelper.IsValidLicensePlate(licensePlate))
                return (false, "Invalid license plate format.", 0);

            if (string.IsNullOrWhiteSpace(vehicleType))
                return (false, "Vehicle type is required.", 0);

            var validTypes = new[] { "Car", "Motorcycle", "Truck", "Bus", "Bicycle", "Scooter", "Other" };
            if (!validTypes.ToList().Contains(vehicleType))
                return (false, "Invalid vehicle type.", 0);

            if (string.IsNullOrWhiteSpace(brand))
                return (false, "Brand is required.", 0);

            if (brand.Length > 50)
                return (false, "Brand must be less than 50 characters.", 0);

            if (string.IsNullOrWhiteSpace(model))
                return (false, "Model is required.", 0);

            if (model.Length > 50)
                return (false, "Model must be less than 50 characters.", 0);

            if (yearMade < 1980 || yearMade > DateTime.Now.Year)
                return (false, $"Year made must be between 1980 and {DateTime.Now.Year}.", 0);

            // Check if resident exists
            var resident = ResidentDAL.GetResidentByID(residentID);
            if (resident == null)
                return (false, "Selected resident does not exist.", 0);

            // Check for duplicate license plate
            var existingVehicle = VehicleDAL.GetVehicleByLicensePlate(licensePlate);
            if (existingVehicle != null)
                return (false, "A vehicle with this license plate already exists.", 0);

            // Create vehicle
            var vehicleID = VehicleDAL.CreateVehicle(
                residentID,
                vehicleType,
                licensePlate,
                color,
                brand,
                note
            );

            if (vehicleID > 0)
            {
                Log.Information($"Vehicle created successfully: {licensePlate} - {brand} {model}");
                return (true, $"Vehicle {licensePlate} created successfully.", vehicleID);
            }

            return (false, "Failed to create vehicle.", 0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating vehicle");
            return (false, $"Error: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Update existing vehicle details
    /// </summary>
    public static (bool Success, string Message) UpdateVehicle(
        int vehicleID,
        string licensePlate,
        string vehicleType,
        string brand,
        string model,
        int yearMade,
        string? color = null,
        string? note = null)
    {
        try
        {
            // Validate input
            if (vehicleID <= 0)
                return (false, "Invalid vehicle ID.");

            if (string.IsNullOrWhiteSpace(licensePlate))
                return (false, "License plate is required.");

            if (!ValidationHelper.IsValidLicensePlate(licensePlate))
                return (false, "Invalid license plate format.");

            if (string.IsNullOrWhiteSpace(brand) || brand.Length > 50)
                return (false, "Invalid brand.");

            if (string.IsNullOrWhiteSpace(model) || model.Length > 50)
                return (false, "Invalid model.");

            if (yearMade < 1980 || yearMade > DateTime.Now.Year)
                return (false, $"Year made must be between 1980 and {DateTime.Now.Year}.");

            var validTypes = new[] { "Car", "Motorcycle", "Truck", "Bus", "Bicycle", "Scooter", "Other" };
            if (!validTypes.ToList().Contains(vehicleType))
                return (false, "Invalid vehicle type.");

            // Get existing vehicle
            var vehicle = VehicleDAL.GetVehicleByID(vehicleID);
            if (vehicle == null)
                return (false, "Vehicle not found.");

            // Check for duplicate license plate (excluding current vehicle)
            var existingVehicle = VehicleDAL.GetVehicleByLicensePlate(licensePlate);
            if (existingVehicle != null && existingVehicle.VehicleID != vehicleID)
                return (false, "This license plate is already used by another vehicle.");

            // Update vehicle
            var success = VehicleDAL.UpdateVehicle(
                vehicleID,
                vehicleType,
                color,
                brand,
                note
            );

            if (success)
            {
                Log.Information($"Vehicle updated: {licensePlate}");
                return (true, $"Vehicle {licensePlate} updated successfully.");
            }

            return (false, "Failed to update vehicle.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating vehicle");
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a vehicle
    /// </summary>
    public static (bool Success, string Message) DeleteVehicle(int vehicleID)
    {
        try
        {
            if (vehicleID <= 0)
                return (false, "Invalid vehicle ID.");

            var vehicle = VehicleDAL.GetVehicleByID(vehicleID);
            if (vehicle == null)
                return (false, "Vehicle not found.");

            var success = VehicleDAL.DeleteVehicle(vehicleID);

            if (success)
            {
                Log.Information($"Vehicle deleted: {vehicle.LicensePlate}");
                return (true, "Vehicle deleted successfully.");
            }

            return (false, "Failed to delete vehicle.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting vehicle");
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get vehicles by resident
    /// </summary>
    public static List<dynamic> GetVehiclesByResident(int residentID)
    {
        try
        {
            if (residentID <= 0)
                return new List<dynamic>();

            return VehicleDAL.GetVehiclesByResident(residentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting vehicles by resident");
            return new List<dynamic>();
        }
    }

    /// <summary>
    /// Get vehicle statistics
    /// </summary>
    public static dynamic? GetVehicleStatistics()
    {
        try
        {
            var vehicles = VehicleDAL.GetAllVehicles();
            if (vehicles.Count == 0)
                return null;

            int carCount = 0;
            int motorcycleCount = 0;
            int truckCount = 0;
            int otherCount = 0;

            foreach (var v in vehicles)
            {
                switch (v.VehicleType)
                {
                    case "Car":
                        carCount++;
                        break;
                    case "Motorcycle":
                        motorcycleCount++;
                        break;
                    case "Truck":
                        truckCount++;
                        break;
                    default:
                        otherCount++;
                        break;
                }
            }

            return new
            {
                TotalVehicles = vehicles.Count,
                CarCount = carCount,
                MotorcycleCount = motorcycleCount,
                TruckCount = truckCount,
                OtherCount = otherCount
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting vehicle statistics");
            return null;
        }
    }
}
