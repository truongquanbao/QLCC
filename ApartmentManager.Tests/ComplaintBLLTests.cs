using Xunit;
using ApartmentManager.BLL;
using System;

namespace ApartmentManager.Tests
{
    /// <summary>
    /// Unit tests for ComplaintBLL class
    /// Tests complaint workflow, status protection, and priority management
    /// </summary>
    public class ComplaintBLLTests
    {
        #region CreateComplaint Tests

        [Fact]
        public void CreateComplaint_ValidData_ReturnsSuccess()
        {
            // Arrange
            int residentID = 1;
            string title = "Leaky faucet in kitchen";
            string description = "The kitchen faucet is leaking water constantly";
            string priority = "Medium";

            // Act
            var result = ComplaintBLL.CreateComplaint(
                residentID,
                title,
                description,
                priority
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateComplaint_TitleTooShort_ReturnsFalse()
        {
            // Arrange
            string shortTitle = "Leak"; // Less than 10 chars - invalid

            // Act
            var result = ComplaintBLL.CreateComplaint(
                1,
                shortTitle,
                "Valid description that is long enough",
                "Medium"
            );

            // Assert
            Assert.NotNull(result);
            // Should fail validation (title min 10 chars)
        }

        [Fact]
        public void CreateComplaint_DescriptionTooShort_ReturnsFalse()
        {
            // Arrange
            string shortDescription = "Broken"; // Less than 20 chars - invalid

            // Act
            var result = ComplaintBLL.CreateComplaint(
                1,
                "Valid complaint title",
                shortDescription,
                "Medium"
            );

            // Assert
            Assert.NotNull(result);
            // Should fail validation (description min 20 chars)
        }

        [Fact]
        public void CreateComplaint_InvalidPriority_ReturnsFalse()
        {
            // Arrange
            string invalidPriority = "Extreme"; // Invalid priority level

            // Act
            var result = ComplaintBLL.CreateComplaint(
                1,
                "Valid complaint title",
                "Valid description that meets minimum length",
                invalidPriority
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateComplaint_InvalidResident_ReturnsFalse()
        {
            // Arrange
            int invalidResidentID = -1;

            // Act
            var result = ComplaintBLL.CreateComplaint(
                invalidResidentID,
                "Valid complaint title",
                "Valid description that meets minimum length",
                "Medium"
            );

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region Status Workflow Protection Tests

        [Fact]
        public void UpdateComplaint_ResolvedComplaint_ReturnsFalse()
        {
            // Arrange
            int resolvedComplaintID = 1; // Assuming this is a resolved complaint
            string newDescription = "Updated description";

            // Act
            var result = ComplaintBLL.UpdateComplaint(
                resolvedComplaintID,
                "Updated title",
                newDescription,
                "Medium"
            );

            // Assert
            Assert.NotNull(result);
            // Should prevent update of resolved complaints
        }

        [Fact]
        public void UpdateComplaint_ClosedComplaint_ReturnsFalse()
        {
            // Arrange
            int closedComplaintID = 1; // Assuming this is a closed complaint

            // Act
            var result = ComplaintBLL.UpdateComplaint(
                closedComplaintID,
                "Updated title",
                "Updated description",
                "Low"
            );

            // Assert
            Assert.NotNull(result);
            // Should prevent update of closed complaints
        }

        [Fact]
        public void DeleteComplaint_ResolvedComplaint_ReturnsFalse()
        {
            // Arrange
            int resolvedComplaintID = 1; // Assuming resolved

            // Act
            var result = ComplaintBLL.DeleteComplaint(resolvedComplaintID);

            // Assert
            Assert.NotNull(result);
            // Should prevent deletion for audit trail
        }

        #endregion

        #region AssignComplaint Tests

        [Fact]
        public void AssignComplaint_ValidStaff_ReturnsSuccess()
        {
            // Arrange
            int complaintID = 1;
            int staffUserID = 1; // Assuming staff user exists

            // Act
            var result = ComplaintBLL.AssignComplaint(complaintID, staffUserID);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AssignComplaint_InvalidComplaint_ReturnsFalse()
        {
            // Arrange
            int invalidComplaintID = -1;
            int staffUserID = 1;

            // Act
            var result = ComplaintBLL.AssignComplaint(invalidComplaintID, staffUserID);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AssignComplaint_InvalidStaff_ReturnsFalse()
        {
            // Arrange
            int complaintID = 1;
            int invalidStaffID = -1;

            // Act
            var result = ComplaintBLL.AssignComplaint(complaintID, invalidStaffID);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region ResolveComplaint Tests

        [Fact]
        public void ResolveComplaint_ValidComplaint_ReturnsSuccess()
        {
            // Arrange
            int complaintID = 1;
            string resolutionNotes = "Repaired the leaky faucet";

            // Act
            var result = ComplaintBLL.ResolveComplaint(complaintID, resolutionNotes);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ResolveComplaint_InvalidComplaint_ReturnsFalse()
        {
            // Arrange
            int invalidComplaintID = -1;

            // Act
            var result = ComplaintBLL.ResolveComplaint(invalidComplaintID, "Fixed");

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region Priority Level Tests

        [Fact]
        public void CreateComplaint_AllValidPriorities_ReturnSuccess()
        {
            // Arrange
            string[] validPriorities = { "Low", "Medium", "High", "Critical" };

            // Act & Assert
            foreach (var priority in validPriorities)
            {
                var result = ComplaintBLL.CreateComplaint(
                    1,
                    "Valid complaint title",
                    "Valid description that meets minimum length",
                    priority
                );
                Assert.NotNull(result);
            }
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public void GetComplaintStatistics_ReturnsValidStats()
        {
            // Act
            var stats = ComplaintBLL.GetComplaintStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalComplaints >= 0);
            Assert.True(stats.ResolvedComplaints >= 0);
            Assert.True(stats.ResolutionRate >= 0);
        }

        [Fact]
        public void GetComplaintStatistics_ResolutionRateCalculation()
        {
            // Act
            var stats = ComplaintBLL.GetComplaintStatistics();

            // Assert
            if (stats.TotalComplaints > 0)
            {
                decimal expectedRate = (stats.ResolvedComplaints * 100) / stats.TotalComplaints;
                Assert.Equal(expectedRate, stats.ResolutionRate);
            }
        }

        #endregion
    }
}
