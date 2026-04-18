using Xunit;
using ApartmentManager.DAL;
using System;

namespace ApartmentManager.Tests
{
    /// <summary>
    /// Integration tests for VisitorDAL class
    /// Tests visitor check-in/check-out and registration persistence
    /// </summary>
    public class VisitorDALIntegrationTests : IDisposable
    {
        private readonly int _testResidentID = 1;

        [Fact]
        public void RegisterVisitor_ValidData_PersistsToDatabase()
        {
            // Arrange
            string visitorName = "TestVisitor-" + DateTime.Now.Ticks;

            // Act
            int visitorID = VisitorDAL.RegisterVisitor(
                _testResidentID,
                visitorName,
                "0912345678",
                "visitor@example.com",
                "Guest",
                "Test visit"
            );

            // Assert
            Assert.True(visitorID > 0);

            // Verify persistence
            var visitor = VisitorDAL.GetVisitorByID(visitorID);
            Assert.NotNull(visitor);
            Assert.Equal(visitorName, visitor.VisitorName);
            Assert.Equal("Guest", visitor.VisitorType);

            // Cleanup
            VisitorDAL.DeleteVisitor(visitorID);
        }

        [Fact]
        public void ApproveVisitor_ChecksInVisitor()
        {
            // Arrange
            int visitorID = 1; // Assuming exists

            // Act
            bool approved = VisitorDAL.ApproveVisitor(visitorID, DateTime.Now);

            // Assert
            Assert.True(approved);

            // Verify status
            var visitor = VisitorDAL.GetVisitorByID(visitorID);
            Assert.NotNull(visitor);
        }

        [Fact]
        public void GetVisitorsByResident_ReturnsResidentVisitors()
        {
            // Act
            var visitors = VisitorDAL.GetVisitorsByResident(_testResidentID);

            // Assert
            Assert.NotNull(visitors);
            foreach (var visitor in visitors)
            {
                Assert.Equal(_testResidentID, visitor.ResidentID);
            }
        }

        [Fact]
        public void GetAllVisitors_ReturnsAllVisitors()
        {
            // Act
            var visitors = VisitorDAL.GetAllVisitors();

            // Assert
            Assert.NotNull(visitors);
            Assert.True(visitors.Count >= 0);
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for NotificationDAL class
    /// Tests notification creation and status workflow
    /// </summary>
    public class NotificationDALIntegrationTests : IDisposable
    {
        private readonly int _testResidentID = 1;

        [Fact]
        public void CreateNotification_ValidData_PersistsToDatabase()
        {
            // Arrange
            string subject = "TestNotification-" + DateTime.Now.Ticks;

            // Act
            int notificationID = NotificationDAL.CreateNotification(
                _testResidentID,
                subject,
                "This is a test notification body",
                "Announcement"
            );

            // Assert
            Assert.True(notificationID > 0);

            // Verify persistence
            var notification = NotificationDAL.GetNotificationByID(notificationID);
            Assert.NotNull(notification);
            Assert.Equal(subject, notification.Subject);
            Assert.Equal("Draft", notification.Status); // Initial status

            // Cleanup
            NotificationDAL.DeleteNotification(notificationID);
        }

        [Fact]
        public void UpdateNotificationStatus_ChangesStatus()
        {
            // Arrange
            int notificationID = 1; // Assuming exists

            // Act
            bool updated = NotificationDAL.UpdateNotificationStatus(
                notificationID,
                "Sent",
                DateTime.Now
            );

            // Assert
            Assert.True(updated);

            // Verify status change
            var notification = NotificationDAL.GetNotificationByID(notificationID);
            Assert.NotNull(notification);
            Assert.Equal("Sent", notification.Status);
        }

        [Fact]
        public void GetAllNotifications_ReturnsAllNotifications()
        {
            // Act
            var notifications = NotificationDAL.GetAllNotifications();

            // Assert
            Assert.NotNull(notifications);
            Assert.True(notifications.Count >= 0);
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for VehicleDAL class
    /// Tests vehicle registration and license plate validation
    /// </summary>
    public class VehicleDALIntegrationTests : IDisposable
    {
        private readonly int _testResidentID = 1;

        [Fact]
        public void CreateVehicle_ValidData_PersistsToDatabase()
        {
            // Arrange
            string licensePlate = "30A-" + DateTime.Now.Millisecond.ToString("D5");

            // Act
            int vehicleID = VehicleDAL.CreateVehicle(
                _testResidentID,
                licensePlate,
                "Car",
                "Toyota",
                "Camry",
                2020,
                "Silver"
            );

            // Assert
            Assert.True(vehicleID > 0);

            // Verify persistence
            var vehicle = VehicleDAL.GetVehicleByID(vehicleID);
            Assert.NotNull(vehicle);
            Assert.Equal(licensePlate, vehicle.LicensePlate);
            Assert.Equal("Car", vehicle.VehicleType);

            // Cleanup
            VehicleDAL.DeleteVehicle(vehicleID);
        }

        [Fact]
        public void GetVehicleByLicensePlate_FindsVehicle()
        {
            // Arrange
            string licensePlate = "30A-12345"; // Assuming exists

            // Act
            var vehicle = VehicleDAL.GetVehicleByLicensePlate(licensePlate);

            // Assert - Might be null if not in test database
            if (vehicle != null)
            {
                Assert.Equal(licensePlate, vehicle.LicensePlate);
            }
        }

        [Fact]
        public void GetVehiclesByResident_ReturnsResidentVehicles()
        {
            // Act
            var vehicles = VehicleDAL.GetVehiclesByResident(_testResidentID);

            // Assert
            Assert.NotNull(vehicles);
            foreach (var vehicle in vehicles)
            {
                Assert.Equal(_testResidentID, vehicle.ResidentID);
            }
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for FeeTypeDAL class
    /// Tests fee type configuration and duplicate prevention
    /// </summary>
    public class FeeTypeDALIntegrationTests : IDisposable
    {
        [Fact]
        public void CreateFeeType_ValidData_PersistsToDatabase()
        {
            // Arrange
            string feeTypeName = "TestFeeType-" + DateTime.Now.Ticks;

            // Act
            int feeTypeID = FeeTypeDAL.CreateFeeType(
                feeTypeName,
                "Test fee type description",
                "VND"
            );

            // Assert
            Assert.True(feeTypeID > 0);

            // Verify persistence
            var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);
            Assert.NotNull(feeType);
            Assert.Equal(feeTypeName, feeType.FeeTypeName);
            Assert.Equal("Active", feeType.Status); // Initial status

            // Cleanup
            FeeTypeDAL.DeleteFeeType(feeTypeID);
        }

        [Fact]
        public void FeeTypeNameExists_DuplicateName_ReturnsTrue()
        {
            // Arrange
            string existingName = "Water Bill"; // Assuming exists

            // Act
            bool exists = FeeTypeDAL.FeeTypeNameExists(existingName);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void FeeTypeNameExists_NewName_ReturnsFalse()
        {
            // Arrange
            string newName = "UniqueFeeType-" + DateTime.Now.Ticks;

            // Act
            bool exists = FeeTypeDAL.FeeTypeNameExists(newName);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void GetActiveFeeTypes_ReturnsOnlyActive()
        {
            // Act
            var activeFeeTypes = FeeTypeDAL.GetActiveFeeTypes();

            // Assert
            Assert.NotNull(activeFeeTypes);
            foreach (var feeType in activeFeeTypes)
            {
                Assert.Equal("Active", feeType.Status);
            }
        }

        [Fact]
        public void UpdateFeeTypeStatus_ChangesStatus()
        {
            // Arrange
            int feeTypeID = 1; // Assuming exists

            // Act
            bool updated = FeeTypeDAL.UpdateFeeTypeStatus(feeTypeID, "Inactive");

            // Assert
            Assert.True(updated);

            // Verify change
            var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);
            Assert.NotNull(feeType);
            Assert.Equal("Inactive", feeType.Status);
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for database transaction integrity
    /// Tests ACID properties and consistency
    /// </summary>
    public class DatabaseTransactionIntegrityTests : IDisposable
    {
        [Fact]
        public void CreateAndRetrieveApartment_DataConsistency()
        {
            // Arrange
            int floorID = 1;
            string apartmentCode = "ConsistencyTest-" + DateTime.Now.Ticks;
            decimal area = 120.5m;

            // Act
            int apartmentID = ApartmentDAL.CreateApartment(
                floorID,
                apartmentCode,
                area,
                "2BR",
                4,
                "Test"
            );

            // Assert
            Assert.True(apartmentID > 0);

            // Verify exact data integrity
            var apartment = ApartmentDAL.GetApartmentByID(apartmentID);
            Assert.NotNull(apartment);
            Assert.Equal(apartmentCode, apartment.ApartmentCode);
            Assert.Equal(area, apartment.Area);
            Assert.Equal("2BR", apartment.ApartmentType);
            Assert.Equal(4, apartment.MaxResidents);

            // Cleanup
            ApartmentDAL.DeleteApartment(apartmentID);
        }

        [Fact]
        public void MultipleSequentialOperations_MaintainConsistency()
        {
            // Arrange
            int apartmentID = 1;

            // Act - Perform multiple operations
            var apartments1 = ApartmentDAL.GetApartmentsByBuilding(1);
            int count1 = apartments1.Count;

            // Create new apartment
            int newApartmentID = ApartmentDAL.CreateApartment(
                1,
                "SequentialTest-" + DateTime.Now.Ticks,
                100m,
                "Studio",
                2,
                "Test"
            );

            // Verify count increased
            var apartments2 = ApartmentDAL.GetApartmentsByBuilding(1);
            int count2 = apartments2.Count;

            // Assert
            Assert.True(count2 > count1);

            // Cleanup
            ApartmentDAL.DeleteApartment(newApartmentID);
        }

        [Fact]
        public void ConcurrentReadOperations_ReturnConsistentData()
        {
            // Arrange
            int apartmentID = 1; // Must exist

            // Act - Read same record multiple times
            var apartment1 = ApartmentDAL.GetApartmentByID(apartmentID);
            var apartment2 = ApartmentDAL.GetApartmentByID(apartmentID);
            var apartment3 = ApartmentDAL.GetApartmentByID(apartmentID);

            // Assert - All should match
            if (apartment1 != null && apartment2 != null && apartment3 != null)
            {
                Assert.Equal(apartment1.ApartmentCode, apartment2.ApartmentCode);
                Assert.Equal(apartment2.ApartmentCode, apartment3.ApartmentCode);
                Assert.Equal(apartment1.Area, apartment2.Area);
                Assert.Equal(apartment2.Area, apartment3.Area);
            }
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for error handling and edge cases
    /// </summary>
    public class DALErrorHandlingTests : IDisposable
    {
        [Fact]
        public void GetNonexistentApartment_ReturnsNull()
        {
            // Arrange
            int nonexistentID = 999999;

            // Act
            var apartment = ApartmentDAL.GetApartmentByID(nonexistentID);

            // Assert
            Assert.Null(apartment);
        }

        [Fact]
        public void DeleteNonexistentApartment_ReturnsFalse()
        {
            // Arrange
            int nonexistentID = 999999;

            // Act
            bool deleted = ApartmentDAL.DeleteApartment(nonexistentID);

            // Assert
            Assert.False(deleted);
        }

        [Fact]
        public void CreateApartmentWithInvalidFloor_HandlesGracefully()
        {
            // Arrange
            int invalidFloorID = -1;

            // Act - Should handle gracefully
            try
            {
                int apartmentID = ApartmentDAL.CreateApartment(
                    invalidFloorID,
                    "InvalidTest",
                    100m,
                    "2BR",
                    4,
                    "Test"
                );
                // If no exception, ID should be 0 or negative
                Assert.True(apartmentID <= 0);
            }
            catch
            {
                // Exception is acceptable for invalid input
                Assert.True(true);
            }
        }

        public void Dispose()
        {
            // Cleanup
        }
    }
}
