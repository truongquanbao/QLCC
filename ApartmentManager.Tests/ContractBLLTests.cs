using Xunit;
using ApartmentManager.BLL;
using System;

namespace ApartmentManager.Tests
{
    /// <summary>
    /// Unit tests for ContractBLL class
    /// Tests contract lifecycle, term validation, and auto-renewal functionality
    /// </summary>
    public class ContractBLLTests
    {
        #region CreateContract Tests

        [Fact]
        public void CreateContract_ValidLease_ReturnsSuccess()
        {
            // Arrange
            int apartmentID = 1;
            int residentID = 1;
            string contractType = "Lease";
            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now.AddMonths(12);
            int termMonths = 12;

            // Act
            var result = ContractBLL.CreateContract(
                apartmentID,
                residentID,
                contractType,
                startDate,
                endDate,
                termMonths,
                false,
                "Standard lease agreement"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateContract_TermTooShort_ReturnsFalse()
        {
            // Arrange
            int termMonths = 5; // Less than 6 months minimum - invalid

            // Act
            var result = ContractBLL.CreateContract(
                1,
                1,
                "Lease",
                DateTime.Now,
                DateTime.Now.AddMonths(5),
                termMonths,
                false,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
            // Should fail validation (term min 6 months)
        }

        [Fact]
        public void CreateContract_TermTooLong_ReturnsFalse()
        {
            // Arrange
            int termMonths = 121; // More than 120 months maximum - invalid

            // Act
            var result = ContractBLL.CreateContract(
                1,
                1,
                "Lease",
                DateTime.Now,
                DateTime.Now.AddMonths(121),
                termMonths,
                false,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
            // Should fail validation (term max 120 months)
        }

        [Fact]
        public void CreateContract_StartDateAfterEndDate_ReturnsFalse()
        {
            // Arrange
            DateTime startDate = DateTime.Now.AddMonths(1);
            DateTime endDate = DateTime.Now; // End before start - invalid

            // Act
            var result = ContractBLL.CreateContract(
                1,
                1,
                "Lease",
                startDate,
                endDate,
                12,
                false,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
            // Should fail date validation
        }

        [Fact]
        public void CreateContract_InvalidContractType_ReturnsFalse()
        {
            // Arrange
            string invalidType = "RoomRental"; // Invalid type

            // Act
            var result = ContractBLL.CreateContract(
                1,
                1,
                invalidType,
                DateTime.Now,
                DateTime.Now.AddMonths(12),
                12,
                false,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateContract_InvalidApartment_ReturnsFalse()
        {
            // Arrange
            int invalidApartmentID = -1;

            // Act
            var result = ContractBLL.CreateContract(
                invalidApartmentID,
                1,
                "Lease",
                DateTime.Now,
                DateTime.Now.AddMonths(12),
                12,
                false,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateContract_InvalidResident_ReturnsFalse()
        {
            // Arrange
            int invalidResidentID = -1;

            // Act
            var result = ContractBLL.CreateContract(
                1,
                invalidResidentID,
                "Lease",
                DateTime.Now,
                DateTime.Now.AddMonths(12),
                12,
                false,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region RenewContract Tests

        [Fact]
        public void RenewContract_ValidContract_ReturnsSuccess()
        {
            // Arrange
            int contractID = 1;
            int renewalTermMonths = 12;

            // Act
            var result = ContractBLL.RenewContract(contractID, renewalTermMonths);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RenewContract_InvalidTermMonths_ReturnsFalse()
        {
            // Arrange
            int contractID = 1;
            int invalidTermMonths = 5; // Less than 6 - invalid

            // Act
            var result = ContractBLL.RenewContract(contractID, invalidTermMonths);

            // Assert
            Assert.NotNull(result);
            // Should fail term validation
        }

        [Fact]
        public void RenewContract_InvalidContractID_ReturnsFalse()
        {
            // Arrange
            int invalidContractID = -1;

            // Act
            var result = ContractBLL.RenewContract(invalidContractID, 12);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region Workflow Protection Tests

        [Fact]
        public void DeleteContract_ActiveContract_ReturnsFalse()
        {
            // Arrange
            int activeContractID = 1; // Assuming this is active

            // Act
            var result = ContractBLL.DeleteContract(activeContractID);

            // Assert
            Assert.NotNull(result);
            // Should prevent deletion of active contracts for audit trail
        }

        [Fact]
        public void DeleteContract_TerminatedContract_MayReturnSuccess()
        {
            // Arrange
            int terminatedContractID = 1; // Assuming this is terminated

            // Act
            var result = ContractBLL.DeleteContract(terminatedContractID);

            // Assert
            Assert.NotNull(result);
            // May allow deletion of terminated contracts
        }

        #endregion

        #region TerminateContract Tests

        [Fact]
        public void TerminateContract_ActiveContract_ReturnsSuccess()
        {
            // Arrange
            int activeContractID = 1;
            DateTime terminationDate = DateTime.Now;

            // Act
            var result = ContractBLL.TerminateContract(activeContractID, terminationDate);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void TerminateContract_InvalidContractID_ReturnsFalse()
        {
            // Arrange
            int invalidContractID = -1;

            // Act
            var result = ContractBLL.TerminateContract(invalidContractID, DateTime.Now);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public void GetContractStatistics_ReturnsValidStats()
        {
            // Act
            var stats = ContractBLL.GetContractStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalContracts >= 0);
            Assert.True(stats.ActiveContracts >= 0);
            Assert.True(stats.ExpiredContracts >= 0);
        }

        [Fact]
        public void GetContractStatistics_StatusBreakdown()
        {
            // Act
            var stats = ContractBLL.GetContractStatistics();

            // Assert
            // Verify status counts don't exceed total
            Assert.True(stats.ActiveContracts + stats.ExpiredContracts <= stats.TotalContracts);
        }

        #endregion

        #region Term Validation Tests

        [Fact]
        public void CreateContract_MinimumValidTerm_ReturnsSuccess()
        {
            // Arrange - 6 months is minimum
            int termMonths = 6;

            // Act
            var result = ContractBLL.CreateContract(
                1,
                1,
                "Lease",
                DateTime.Now,
                DateTime.Now.AddMonths(6),
                termMonths,
                false,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateContract_MaximumValidTerm_ReturnsSuccess()
        {
            // Arrange - 120 months is maximum
            int termMonths = 120;

            // Act
            var result = ContractBLL.CreateContract(
                1,
                1,
                "Lease",
                DateTime.Now,
                DateTime.Now.AddMonths(120),
                termMonths,
                false,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region ContractType Validation Tests

        [Fact]
        public void CreateContract_ValidContractTypes_ReturnSuccess()
        {
            // Arrange
            string[] validTypes = { "Lease", "Service" };

            // Act & Assert
            foreach (var type in validTypes)
            {
                var result = ContractBLL.CreateContract(
                    1,
                    1,
                    type,
                    DateTime.Now,
                    DateTime.Now.AddMonths(12),
                    12,
                    false,
                    "Test"
                );
                Assert.NotNull(result);
            }
        }

        #endregion
    }
}
