using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApartmentManager.BLL
{
    public class FeeTypeBLL
    {
        private const int MIN_NAME_LENGTH = 3;
        private const int MAX_NAME_LENGTH = 50;
        private const int MAX_DESCRIPTION_LENGTH = 500;

        /// <summary>
        /// Create a new fee type
        /// </summary>
        public static (bool Success, string Message, int FeeTypeID) CreateFeeType(
            string feeTypeName,
            string description,
            string unitOfMeasurement)
        {
            try
            {
                // Validate fee type name
                if (string.IsNullOrWhiteSpace(feeTypeName))
                    return (false, "Fee type name is required.", 0);

                if (!ValidationHelper.IsValidLength(feeTypeName, MIN_NAME_LENGTH, MAX_NAME_LENGTH))
                    return (false, $"Fee type name must be between {MIN_NAME_LENGTH} and {MAX_NAME_LENGTH} characters.", 0);

                // Check for duplicate fee type name
                if (FeeTypeDAL.FeeTypeNameExists(feeTypeName))
                    return (false, "A fee type with this name already exists.", 0);

                // Validate description
                if (!string.IsNullOrWhiteSpace(description))
                {
                    if (!ValidationHelper.IsValidLength(description, 0, MAX_DESCRIPTION_LENGTH))
                        return (false, $"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters.", 0);
                }

                // Validate unit of measurement
                if (string.IsNullOrWhiteSpace(unitOfMeasurement))
                    unitOfMeasurement = "VND"; // Default unit

                // Create fee type
                int feeTypeID = FeeTypeDAL.CreateFeeType(
                    feeTypeName,
                    description ?? "",
                    unitOfMeasurement);

                if (feeTypeID > 0)
                {
                    Log.Information($"Fee type created: ID={feeTypeID}, Name={feeTypeName}");
                    return (true, "Fee type created successfully.", feeTypeID);
                }

                return (false, "Failed to create fee type in database.", 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating fee type");
                return (false, $"Error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Update an existing fee type
        /// </summary>
        public static (bool Success, string Message) UpdateFeeType(
            int feeTypeID,
            string feeTypeName,
            string description,
            string unitOfMeasurement)
        {
            try
            {
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);
                if (feeType == null)
                    return (false, "Fee type not found.");

                // Validate fee type name
                if (string.IsNullOrWhiteSpace(feeTypeName))
                    return (false, "Fee type name is required.");

                if (!ValidationHelper.IsValidLength(feeTypeName, MIN_NAME_LENGTH, MAX_NAME_LENGTH))
                    return (false, $"Fee type name must be between {MIN_NAME_LENGTH} and {MAX_NAME_LENGTH} characters.");

                // Check for duplicate fee type name (exclude current fee type)
                if (FeeTypeDAL.FeeTypeNameExists(feeTypeName, feeTypeID))
                    return (false, "A different fee type with this name already exists.");

                // Validate description
                if (!string.IsNullOrWhiteSpace(description))
                {
                    if (!ValidationHelper.IsValidLength(description, 0, MAX_DESCRIPTION_LENGTH))
                        return (false, $"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters.");
                }

                // Validate unit of measurement
                if (string.IsNullOrWhiteSpace(unitOfMeasurement))
                    unitOfMeasurement = "VND"; // Default unit

                bool updated = FeeTypeDAL.UpdateFeeType(
                    feeTypeID,
                    feeTypeName,
                    description ?? "",
                    unitOfMeasurement);

                if (updated)
                {
                    Log.Information($"Fee type updated: ID={feeTypeID}, Name={feeTypeName}");
                    return (true, "Fee type updated successfully.");
                }

                return (false, "Failed to update fee type.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating fee type");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a fee type
        /// </summary>
        public static (bool Success, string Message) DeleteFeeType(int feeTypeID)
        {
            try
            {
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);
                if (feeType == null)
                    return (false, "Fee type not found.");

                bool deleted = FeeTypeDAL.DeleteFeeType(feeTypeID);

                if (deleted)
                {
                    Log.Information($"Fee type deleted: ID={feeTypeID}");
                    return (true, "Fee type deleted successfully.");
                }

                return (false, "Failed to delete fee type.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting fee type");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get fee type statistics
        /// </summary>
        public static dynamic GetFeeTypeStatistics()
        {
            try
            {
                var feeTypes = FeeTypeDAL.GetAllFeeTypes();

                return new
                {
                    TotalFeeTypes = feeTypes.Count,
                    ActiveCount = feeTypes.Count(f => f.Status == "Active"),
                    InactiveCount = feeTypes.Count(f => f.Status == "Inactive")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving fee type statistics");
                return new { TotalFeeTypes = 0 };
            }
        }
    }
}
