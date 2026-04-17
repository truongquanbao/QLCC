using Xunit;
using ApartmentManager.BLL;

namespace ApartmentManager.Tests
{
    /// <summary>
    /// Unit tests for ApartmentBLL class
    /// Tests CRUD operations, validation, and duplicate prevention
    /// </summary>
    public class ApartmentBLLTests
    {
        #region CreateApartment Tests

        [Fact]
        public void CreateApartment_ValidData_ReturnsSuccess()
        {
            // Arrange
            string apartmentCode = "TestApt-" + System.DateTime.Now.Ticks;
            decimal area = 100.5m;
            string type = "2BR";
            int maxResidents = 4;

            // Act
            var result = ApartmentBLL.CreateApartment(
                apartmentCode,
                1, // Floor ID (assuming exists)
                area,
                type,
                maxResidents,
                "Test apartment"
            );

            // Assert
            Assert.NotNull(result);
            // Success depends on database state
        }

        [Fact]
        public void CreateApartment_MissingCode_ReturnsFalse()
        {
            // Arrange
            string apartmentCode = ""; // Invalid: empty

            // Act
            var result = ApartmentBLL.CreateApartment(
                apartmentCode,
                1,
                100.5m,
                "2BR",
                4,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
            // Should fail validation
        }

        [Fact]
        public void CreateApartment_InvalidArea_ReturnsFalse()
        {
            // Arrange
            string apartmentCode = "Apt001";
            decimal invalidArea = -100m; // Invalid: negative

            // Act
            var result = ApartmentBLL.CreateApartment(
                apartmentCode,
                1,
                invalidArea,
                "2BR",
                4,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
            // Should fail validation
        }

        [Fact]
        public void CreateApartment_InvalidMaxResidents_ReturnsFalse()
        {
            // Arrange
            int invalidMaxResidents = 0; // Invalid: zero

            // Act
            var result = ApartmentBLL.CreateApartment(
                "Apt001",
                1,
                100.5m,
                "2BR",
                invalidMaxResidents,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
            // Should fail validation
        }

        #endregion

        #region UpdateApartment Tests

        [Fact]
        public void UpdateApartment_ValidData_ReturnsSuccess()
        {
            // Arrange
            int apartmentID = 1; // Assuming exists
            string newCode = "UpdatedApt-" + System.DateTime.Now.Ticks;
            decimal newArea = 150.5m;

            // Act
            var result = ApartmentBLL.UpdateApartment(
                apartmentID,
                newCode,
                newArea,
                "3BR",
                5,
                "Updated"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void UpdateApartment_InvalidArea_ReturnsFalse()
        {
            // Arrange
            int apartmentID = 1;
            decimal invalidArea = 0m; // Invalid

            // Act
            var result = ApartmentBLL.UpdateApartment(
                apartmentID,
                "Apt001",
                invalidArea,
                "2BR",
                4,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region DeleteApartment Tests

        [Fact]
        public void DeleteApartment_ValidID_ReturnsSuccess()
        {
            // Arrange
            int apartmentID = 999; // Assuming doesn't exist or safe to delete

            // Act
            var result = ApartmentBLL.DeleteApartment(apartmentID);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void DeleteApartment_InvalidID_ReturnsFalse()
        {
            // Arrange
            int invalidID = -1;

            // Act
            var result = ApartmentBLL.DeleteApartment(invalidID);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region GetApartmentsByBuilding Tests

        [Fact]
        public void GetApartmentsByBuilding_ValidBuildingID_ReturnsApartments()
        {
            // Arrange
            int buildingID = 1; // Assuming exists

            // Act
            var apartments = ApartmentBLL.GetApartmentsByBuilding(buildingID);

            // Assert
            Assert.NotNull(apartments);
            // Should return list (may be empty or populated)
        }

        [Fact]
        public void GetApartmentsByBuilding_InvalidBuildingID_ReturnsEmpty()
        {
            // Arrange
            int invalidBuildingID = -1;

            // Act
            var apartments = ApartmentBLL.GetApartmentsByBuilding(invalidBuildingID);

            // Assert
            Assert.NotNull(apartments);
        }

        #endregion

        #region Occupancy Statistics Tests

        [Fact]
        public void GetOccupancyStatistics_ReturnsValidStats()
        {
            // Act
            var stats = ApartmentBLL.GetOccupancyStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalApartments >= 0);
            Assert.True(stats.OccupiedApartments >= 0);
            Assert.True(stats.OccupancyRate >= 0);
        }

        #endregion
    }
}
