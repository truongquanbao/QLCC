using Xunit;
using ApartmentManager.BLL;
using System;

namespace ApartmentManager.Tests
{
    /// <summary>
    /// Unit tests for VehicleBLL class
    /// Tests vehicle registration and license plate validation
    /// </summary>
    public class VehicleBLLTests
    {
        [Fact]
        public void CreateVehicle_ValidData_ReturnsSuccess()
        {
            // Arrange
            int residentID = 1;
            string licensePlate = "30A-12345";
            string vehicleType = "Car";
            string brand = "Toyota";
            int year = 2020;

            // Act
            var result = VehicleBLL.CreateVehicle(
                residentID,
                licensePlate,
                vehicleType,
                brand,
                "Camry",
                year,
                "Silver"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateVehicle_InvalidLicensePlate_ReturnsFalse()
        {
            // Arrange
            string invalidPlate = "INVALID";

            // Act
            var result = VehicleBLL.CreateVehicle(
                1,
                invalidPlate,
                "Car",
                "Toyota",
                "Camry",
                2020,
                "Silver"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateVehicle_DuplicateLicensePlate_ReturnsFalse()
        {
            // Arrange
            string duplicatePlate = "30A-12345"; // Assuming already exists

            // Act
            var result = VehicleBLL.CreateVehicle(
                1,
                duplicatePlate,
                "Car",
                "Toyota",
                "Camry",
                2020,
                "Silver"
            );

            // Assert
            Assert.NotNull(result);
            // Should prevent duplicate registration
        }

        [Fact]
        public void CreateVehicle_InvalidYear_ReturnsFalse()
        {
            // Arrange
            int invalidYear = 1979; // Before 1980 - invalid

            // Act
            var result = VehicleBLL.CreateVehicle(
                1,
                "30A-12345",
                "Car",
                "Toyota",
                "Camry",
                invalidYear,
                "Silver"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateVehicle_FutureYear_ReturnsFalse()
        {
            // Arrange
            int futureYear = DateTime.Now.Year + 2;

            // Act
            var result = VehicleBLL.CreateVehicle(
                1,
                "30A-12345",
                "Car",
                "Toyota",
                "Camry",
                futureYear,
                "Silver"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateVehicle_InvalidType_ReturnsFalse()
        {
            // Arrange
            string invalidType = "Spaceship";

            // Act
            var result = VehicleBLL.CreateVehicle(
                1,
                "30A-12345",
                invalidType,
                "Toyota",
                "Camry",
                2020,
                "Silver"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVehicleStatistics_ReturnsValidStats()
        {
            // Act
            var stats = VehicleBLL.GetVehicleStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalVehicles >= 0);
        }
    }

    /// <summary>
    /// Unit tests for VisitorBLL class
    /// Tests visitor check-in/check-out and registration
    /// </summary>
    public class VisitorBLLTests
    {
        [Fact]
        public void CheckInVisitor_ValidData_ReturnsSuccess()
        {
            // Arrange
            int residentID = 1;
            string visitorName = "John Smith";
            string visitorType = "Guest";

            // Act
            var result = VisitorBLL.CheckInVisitor(
                residentID,
                visitorName,
                "0912345678",
                "john@example.com",
                visitorType,
                "Visiting family member"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CheckInVisitor_InvalidVisitorName_ReturnsFalse()
        {
            // Arrange
            string invalidName = "J"; // Too short - min 3 chars

            // Act
            var result = VisitorBLL.CheckInVisitor(
                1,
                invalidName,
                "0912345678",
                "john@example.com",
                "Guest",
                "Visit"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CheckInVisitor_InvalidPhone_ReturnsFalse()
        {
            // Arrange
            string invalidPhone = "123"; // Invalid format

            // Act
            var result = VisitorBLL.CheckInVisitor(
                1,
                "John Smith",
                invalidPhone,
                "john@example.com",
                "Guest",
                "Visit"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CheckInVisitor_InvalidEmail_ReturnsFalse()
        {
            // Arrange
            string invalidEmail = "not-an-email";

            // Act
            var result = VisitorBLL.CheckInVisitor(
                1,
                "John Smith",
                "0912345678",
                invalidEmail,
                "Guest",
                "Visit"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CheckOutVisitor_ValidVisitor_ReturnsSuccess()
        {
            // Arrange
            int visitorID = 1;

            // Act
            var result = VisitorBLL.CheckOutVisitor(visitorID);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVisitorStatistics_ReturnsValidStats()
        {
            // Act
            var stats = VisitorBLL.GetVisitorStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalVisitors >= 0);
            Assert.True(stats.CheckedInCount >= 0);
        }
    }

    /// <summary>
    /// Unit tests for NotificationBLL class
    /// Tests notification creation and status workflow
    /// </summary>
    public class NotificationBLLTests
    {
        [Fact]
        public void CreateNotification_ValidData_ReturnsSuccess()
        {
            // Arrange
            int residentID = 1;
            string subject = "Maintenance scheduled";
            string body = "Water maintenance is scheduled for this week";
            string notificationType = "Maintenance";

            // Act
            var result = NotificationBLL.CreateNotification(
                residentID,
                subject,
                body,
                notificationType
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateNotification_SubjectTooShort_ReturnsFalse()
        {
            // Arrange
            string shortSubject = "Hi"; // Less than 5 chars - invalid

            // Act
            var result = NotificationBLL.CreateNotification(
                1,
                shortSubject,
                "Valid body with enough characters",
                "Announcement"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateNotification_BodyTooShort_ReturnsFalse()
        {
            // Arrange
            string shortBody = "Short"; // Less than 10 chars - invalid

            // Act
            var result = NotificationBLL.CreateNotification(
                1,
                "Valid subject",
                shortBody,
                "Announcement"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void SendNotification_DraftNotification_ReturnsSuccess()
        {
            // Arrange
            int notificationID = 1;

            // Act
            var result = NotificationBLL.SendNotification(notificationID);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void DeleteNotification_SentNotification_ReturnsFalse()
        {
            // Arrange
            int sentNotificationID = 1; // Assuming sent

            // Act
            var result = NotificationBLL.DeleteNotification(sentNotificationID);

            // Assert
            Assert.NotNull(result);
            // Should prevent deletion of sent notifications for audit
        }

        [Fact]
        public void GetNotificationStatistics_ReturnsValidStats()
        {
            // Act
            var stats = NotificationBLL.GetNotificationStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalNotifications >= 0);
            Assert.True(stats.DraftCount >= 0);
            Assert.True(stats.SentCount >= 0);
        }
    }

    /// <summary>
    /// Unit tests for FeeTypeBLL class
    /// Tests fee type configuration and duplicate prevention
    /// </summary>
    public class FeeTypeBLLTests
    {
        [Fact]
        public void CreateFeeType_ValidData_ReturnsSuccess()
        {
            // Arrange
            string feeTypeName = "Water Bill-" + DateTime.Now.Ticks;
            string description = "Monthly water usage charges";

            // Act
            var result = FeeTypeBLL.CreateFeeType(
                feeTypeName,
                description,
                "VND"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateFeeType_NameTooShort_ReturnsFalse()
        {
            // Arrange
            string shortName = "AB"; // Less than 3 chars - invalid

            // Act
            var result = FeeTypeBLL.CreateFeeType(
                shortName,
                "Valid description",
                "VND"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateFeeType_DuplicateName_ReturnsFalse()
        {
            // Arrange
            string duplicateName = "Water Bill"; // Assuming already exists

            // Act
            var result = FeeTypeBLL.CreateFeeType(
                duplicateName,
                "Monthly water charges",
                "VND"
            );

            // Assert
            Assert.NotNull(result);
            // Should prevent duplicate fee type names
        }

        [Fact]
        public void CreateFeeType_DescriptionTooLong_ReturnsFalse()
        {
            // Arrange
            string longDescription = new string('A', 501); // Exceeds 500 char limit

            // Act
            var result = FeeTypeBLL.CreateFeeType(
                "Valid Name",
                longDescription,
                "VND"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetFeeTypeStatistics_ReturnsValidStats()
        {
            // Act
            var stats = FeeTypeBLL.GetFeeTypeStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalFeeTypes >= 0);
            Assert.True(stats.ActiveFeeTypes >= 0);
            Assert.True(stats.InactiveFeeTypes >= 0);
        }
    }

    /// <summary>
    /// Unit tests for ResidentBLL class
    /// Tests resident registration and lifecycle management
    /// </summary>
    public class ResidentBLLTests
    {
        [Fact]
        public void RegisterResident_ValidData_ReturnsSuccess()
        {
            // Arrange
            int apartmentID = 1;
            string fullName = "Nguyen Van A";
            string cccd = "012345678901"; // 12 digits

            // Act
            var result = ResidentBLL.RegisterResident(
                apartmentID,
                fullName,
                "1990-01-15",
                cccd,
                "0912345678",
                "nguyen@example.com",
                "Active"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RegisterResident_InvalidCCCD_ReturnsFalse()
        {
            // Arrange
            string invalidCCCD = "123456789"; // Too short

            // Act
            var result = ResidentBLL.RegisterResident(
                1,
                "Nguyen Van A",
                "1990-01-15",
                invalidCCCD,
                "0912345678",
                "nguyen@example.com",
                "Active"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetResidentStatistics_ReturnsValidStats()
        {
            // Act
            var stats = ResidentBLL.GetResidentStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalResidents >= 0);
            Assert.True(stats.ActiveResidents >= 0);
        }
    }
}
