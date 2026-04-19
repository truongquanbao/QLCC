using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApartmentManager.BLL
{
    public class ContractBLL
    {
        // Contract term validation constants
        private const int MIN_TERM_MONTHS = 6;
        private const int MAX_TERM_MONTHS = 120; // 10 years

        /// <summary>
        /// Create a new contract with validation
        /// </summary>
        public static (bool Success, string Message, int ContractID) CreateContract(
            int apartmentID,
            int residentID,
            DateTime startDate,
            DateTime endDate,
            string contractType,
            string termsAndConditions,
            bool autoRenewal,
            string renewalNotes)
        {
            try
            {
                // Validate apartment
                var apartment = ApartmentDAL.GetApartmentByID(apartmentID);
                if (apartment == null)
                    return (false, "Selected apartment does not exist.", 0);

                // Validate resident
                var resident = ResidentDAL.GetResidentByID(residentID);
                if (resident == null)
                    return (false, "Selected resident does not exist.", 0);

                // Validate dates
                if (startDate >= endDate)
                    return (false, "Start date must be before end date.", 0);

                if (startDate.Date < DateTime.Now.Date)
                    return (false, "Start date cannot be in the past.", 0);

                // Validate contract term
                var termMonths = (endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month);
                if (termMonths < MIN_TERM_MONTHS)
                    return (false, $"Contract term must be at least {MIN_TERM_MONTHS} months.", 0);

                if (termMonths > MAX_TERM_MONTHS)
                    return (false, $"Contract term cannot exceed {MAX_TERM_MONTHS} months (10 years).", 0);

                // Validate contract type
                if (string.IsNullOrWhiteSpace(contractType) || (!contractType.Equals("Lease") && !contractType.Equals("Service")))
                    return (false, "Invalid contract type. Must be 'Lease' or 'Service'.", 0);

                // Check for existing active contracts for this resident in the same apartment
                var existingContracts = ContractDAL.GetContractsByApartment(apartmentID);
                if (existingContracts.Any(c => c.ResidentID == residentID && (c.Status == "Active" || c.Status == "Pending")))
                    return (false, "Resident already has an active or pending contract in this apartment.", 0);

                // Create contract
                int contractID = ContractDAL.CreateContract(
                    apartmentID,
                    residentID,
                    startDate,
                    endDate,
                    0m,
                    0m,
                    termsAndConditions);

                if (contractID > 0)
                {
                    Log.Information($"Contract created: ID={contractID}, Apartment={apartmentID}, Resident={residentID}");
                    return (true, "Contract created successfully.", contractID);
                }

                return (false, "Failed to create contract in database.", 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating contract");
                return (false, $"Error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Update an existing contract
        /// </summary>
        public static (bool Success, string Message) UpdateContract(
            int contractID,
            DateTime startDate,
            DateTime endDate,
            string contractType,
            string termsAndConditions,
            bool autoRenewal,
            string renewalNotes)
        {
            try
            {
                var contract = ContractDAL.GetContractByID(contractID);
                if (contract == null)
                    return (false, "Contract not found.");

                // Prevent updating expired or terminated contracts
                if (contract.Status == "Expired" || contract.Status == "Terminated")
                    return (false, $"Cannot update {contract.Status.ToLower()} contracts.");

                // Validate dates
                if (startDate >= endDate)
                    return (false, "Start date must be before end date.");

                // Validate contract term
                var termMonths = (endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month);
                if (termMonths < MIN_TERM_MONTHS)
                    return (false, $"Contract term must be at least {MIN_TERM_MONTHS} months.");

                if (termMonths > MAX_TERM_MONTHS)
                    return (false, $"Contract term cannot exceed {MAX_TERM_MONTHS} months.");

                // Validate contract type
                if (string.IsNullOrWhiteSpace(contractType) || (!contractType.Equals("Lease") && !contractType.Equals("Service")))
                    return (false, "Invalid contract type. Must be 'Lease' or 'Service'.");

                bool updated = ContractDAL.UpdateContract(
                    contractID,
                    startDate,
                    endDate,
                    0m,
                    0m,
                    termsAndConditions);

                if (updated)
                {
                    Log.Information($"Contract updated: ID={contractID}");
                    return (true, "Contract updated successfully.");
                }

                return (false, "Failed to update contract.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating contract");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Renew a contract by extending the end date
        /// </summary>
        public static (bool Success, string Message) RenewContract(
            int contractID,
            DateTime currentEndDate,
            DateTime newEndDate)
        {
            try
            {
                var contract = ContractDAL.GetContractByID(contractID);
                if (contract == null)
                    return (false, "Contract not found.");

                // Calculate renewal term
                var renewalTermMonths = (newEndDate.Year - currentEndDate.Year) * 12 + 
                                       (newEndDate.Month - currentEndDate.Month);

                if (renewalTermMonths <= 0)
                    return (false, "Renewal term must be greater than zero.");

                if (renewalTermMonths > MAX_TERM_MONTHS)
                    return (false, $"Renewal term cannot exceed {MAX_TERM_MONTHS} months.");

                // Update contract with new end date
                bool updated = ContractDAL.UpdateContract(
                    contractID,
                    contract.StartDate,
                    newEndDate,
                    0m,
                    0m,
                    $"Renewed on {DateTime.Now:yyyy-MM-dd}");

                if (updated)
                {
                    Log.Information($"Contract renewed: ID={contractID}, New End Date={newEndDate:yyyy-MM-dd}");
                    return (true, "Contract renewed successfully.");
                }

                return (false, "Failed to renew contract.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error renewing contract");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Terminate a contract
        /// </summary>
        public static (bool Success, string Message) TerminateContract(int contractID, DateTime terminationDate)
        {
            try
            {
                var contract = ContractDAL.GetContractByID(contractID);
                if (contract == null)
                    return (false, "Contract not found.");

                if (contract.Status == "Terminated")
                    return (false, "Contract is already terminated.");

                bool updated = ContractDAL.UpdateContractStatus(contractID, "Terminated");

                if (updated)
                {
                    Log.Information($"Contract terminated: ID={contractID}, Date={terminationDate:yyyy-MM-dd}");
                    return (true, "Contract terminated successfully.");
                }

                return (false, "Failed to terminate contract.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error terminating contract");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a contract (only if not active)
        /// </summary>
        public static (bool Success, string Message) DeleteContract(int contractID)
        {
            try
            {
                var contract = ContractDAL.GetContractByID(contractID);
                if (contract == null)
                    return (false, "Contract not found.");

                // Prevent deletion of active contracts for audit trail
                if (contract.Status == "Active")
                    return (false, "Cannot delete active contracts. Terminate the contract first.");

                bool deleted = ContractDAL.DeleteContract(contractID);

                if (deleted)
                {
                    Log.Information($"Contract deleted: ID={contractID}");
                    return (true, "Contract deleted successfully.");
                }

                return (false, "Failed to delete contract.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting contract");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get contract statistics
        /// </summary>
        public static dynamic GetContractStatistics()
        {
            try
            {
                var contracts = ContractDAL.GetAllContracts();
                var expiringContracts = ContractDAL.GetExpiringContracts(30);

                return new
                {
                    TotalContracts = contracts.Count,
                    ActiveCount = contracts.Count(c => c.Status == "Active"),
                    ExpiredCount = contracts.Count(c => c.Status == "Expired"),
                    TerminatedCount = contracts.Count(c => c.Status == "Terminated"),
                    PendingCount = contracts.Count(c => c.Status == "Pending"),
                    ExpiringCount = expiringContracts.Count,
                    AutoRenewalCount = contracts.Count(c => c.AutoRenewal)
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving contract statistics");
                return new { TotalContracts = 0 };
            }
        }
    }
}
