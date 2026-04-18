using System;
using System.Collections.Generic;
using System.Linq;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.BLL;

/// <summary>
/// Business Logic Layer for Resident management
/// </summary>
public class ResidentBLL
{
    /// <summary>
    /// Get resident by ID with validation
    /// </summary>
    public static dynamic? GetResidentByID(int residentID)
    {
        try
        {
            if (residentID <= 0)
            {
                Log.Warning("Invalid resident ID: {ResidentID}", residentID);
                return null;
            }

            return ResidentDAL.GetResidentByID(residentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting resident: {ResidentID}", residentID);
            return null;
        }
    }

    /// <summary>
    /// Get all residents
    /// </summary>
    public static List<dynamic> GetAllResidents()
    {
        try
        {
            return ResidentDAL.GetAllResidents().Cast<dynamic>().ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting all residents");
            return new List<dynamic>();
        }
    }

    /// <summary>
    /// Get active residents
    /// </summary>
    public static List<dynamic> GetActiveResidents()
    {
        try
        {
            return ResidentDAL.GetResidentsByStatus("Active").Cast<dynamic>().ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting active residents");
            return new List<dynamic>();
        }
    }

    /// <summary>
    /// Create resident with comprehensive validation
    /// </summary>
    public static (bool Success, string Message, int ResidentID) CreateResident(
        int? userID, string fullName, string phone, string email, string cccd, DateTime dob,
        int apartmentID, string relationshipWithOwner, DateTime startDate, string? note = null)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Full name is required", 0);

            if (fullName.Length > 100)
                return (false, "Full name must be less than 100 characters", 0);

            if (!ValidationHelper.IsValidPhone(phone))
                return (false, "Invalid phone number format", 0);

            if (!ValidationHelper.IsValidEmail(email))
                return (false, "Invalid email format", 0);

            if (!ValidationHelper.IsValidCCCD(cccd))
                return (false, "CCCD must be 9 or 12 digits", 0);

            if (!ValidationHelper.IsValidBirthDate(dob))
                return (false, "Resident must be at least 18 years old", 0);

            if (apartmentID <= 0)
                return (false, "Invalid apartment ID", 0);

            if (string.IsNullOrWhiteSpace(relationshipWithOwner))
                return (false, "Relationship with owner is required", 0);

            var validRelationships = new[] { "Owner", "Family", "Friend", "Tenant", "Other" };
            if (!validRelationships.Contains(relationshipWithOwner))
                return (false, "Invalid relationship type", 0);

            if (startDate > DateTime.Now)
                return (false, "Start date cannot be in the future", 0);

            // Check CCCD duplication
            if (ResidentDAL.CCCDExists(cccd))
                return (false, "A resident with this CCCD already exists", 0);

            int residentID = ResidentDAL.CreateResident(userID, fullName, phone, email, cccd, dob, 
                                                         apartmentID, relationshipWithOwner, startDate, note);

            Log.Information("Resident created via BLL: {FullName} (ID: {ResidentID})", fullName, residentID);
            return (true, "Resident created successfully", residentID);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error creating resident: {FullName}", fullName);
            return (false, $"Error creating resident: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Update resident with validation
    /// </summary>
    public static (bool Success, string Message) UpdateResident(
        int residentID, string fullName, string phone, string email, string relationshipWithOwner, string? note = null)
    {
        try
        {
            if (residentID <= 0)
                return (false, "Invalid resident ID");

            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100)
                return (false, "Invalid full name");

            if (!ValidationHelper.IsValidPhone(phone))
                return (false, "Invalid phone number format");

            if (!ValidationHelper.IsValidEmail(email))
                return (false, "Invalid email format");

            var validRelationships = new[] { "Owner", "Family", "Friend", "Tenant", "Other" };
            if (!validRelationships.Contains(relationshipWithOwner))
                return (false, "Invalid relationship type");

            bool success = ResidentDAL.UpdateResident(residentID, fullName, phone, email, relationshipWithOwner, note);

            if (success)
            {
                Log.Information("Resident updated via BLL: {ResidentID}", residentID);
                return (true, "Resident updated successfully");
            }

            return (false, "Failed to update resident");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error updating resident: {ResidentID}", residentID);
            return (false, $"Error updating resident: {ex.Message}");
        }
    }

    /// <summary>
    /// Move resident out (change status to Inactive with end date)
    /// </summary>
    public static (bool Success, string Message) MoveResidentOut(int residentID, DateTime moveOutDate)
    {
        try
        {
            if (residentID <= 0)
                return (false, "Invalid resident ID");

            if (moveOutDate > DateTime.Now)
                return (false, "Move out date cannot be in the future");

            bool success = ResidentDAL.UpdateResidentStatus(residentID, "Inactive", moveOutDate);

            if (success)
            {
                Log.Information("Resident moved out via BLL: {ResidentID}", residentID);
                return (true, "Resident moved out successfully");
            }

            return (false, "Failed to move resident out");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error moving resident out: {ResidentID}", residentID);
            return (false, $"Error moving resident out: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete resident
    /// </summary>
    public static (bool Success, string Message) DeleteResident(int residentID)
    {
        try
        {
            if (residentID <= 0)
                return (false, "Invalid resident ID");

            bool success = ResidentDAL.DeleteResident(residentID);

            if (success)
            {
                Log.Information("Resident deleted via BLL: {ResidentID}", residentID);
                return (true, "Resident deleted successfully");
            }

            return (false, "Failed to delete resident");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error deleting resident: {ResidentID}", residentID);
            return (false, $"Error deleting resident: {ex.Message}");
        }
    }

    /// <summary>
    /// Get residents by apartment
    /// </summary>
    public static List<dynamic> GetResidentsByApartment(int apartmentID)
    {
        try
        {
            if (apartmentID <= 0)
                return new List<dynamic>();

            return ResidentDAL.GetResidentsByApartment(apartmentID).Cast<dynamic>().ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting residents by apartment: {ApartmentID}", apartmentID);
            return new List<dynamic>();
        }
    }

    /// <summary>
    /// Get resident statistics
    /// </summary>
    public static dynamic GetResidentStatistics()
    {
        try
        {
            var allResidents = ResidentDAL.GetAllResidents();
            var activeResidents = ResidentDAL.GetResidentsByStatus("Active");

            var stats = new
            {
                TotalResidents = allResidents.Count,
                ActiveResidents = activeResidents.Count,
                InactiveResidents = allResidents.Count - activeResidents.Count,
                AveragePerApartment = allResidents.Count > 0 ? 
                    (activeResidents.Count / (decimal)ApartmentDAL.GetAllApartments().Count).ToString("F2") : "0"
            };

            return stats;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BLL Error getting resident statistics");
            return null;
        }
    }
}


