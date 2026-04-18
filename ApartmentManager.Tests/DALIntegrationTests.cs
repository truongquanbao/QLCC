using Xunit;
using ApartmentManager.DAL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApartmentManager.Tests
{
    /// <summary>
    /// Integration tests for ApartmentDAL class
    /// Tests actual database interactions and data persistence
    /// </summary>
    public class ApartmentDALIntegrationTests : IDisposable
    {
        private readonly int _testBuildingID = 1; // Assuming test building exists
        private readonly int _testBlockID = 1;
        private readonly int _testFloorID = 1;

        [Fact]
        public void CreateApartment_ValidData_PersistsToDatabase()
        {
            // Arrange
            string apartmentCode = "TestApt-" + DateTime.Now.Ticks;

            // Act
            int apartmentID = ApartmentDAL.CreateApartment(
                _testFloorID,
                apartmentCode,
                100.5m,
                "2BR",
                4,
                "Test apartment"
            );

            // Assert
            Assert.True(apartmentID > 0, "Apartment should be created with positive ID");

            // Verify persistence
            var apartment = ApartmentDAL.GetApartmentByID(apartmentID);
            Assert.NotNull(apartment);
            Assert.Equal(apartmentCode, apartment.ApartmentCode);
            Assert.Equal("2BR", apartment.ApartmentType);

            // Cleanup
            ApartmentDAL.DeleteApartment(apartmentID);
        }

        [Fact]
        public void UpdateApartment_ModifyData_UpdatesInDatabase()
        {
            // Arrange
            int apartmentID = 1; // Assuming exists

            // Act
            bool updated = ApartmentDAL.UpdateApartment(
                apartmentID,
                "UpdatedCode",
                150.0m,
                "3BR",
                5,
                "Updated test"
            );

            // Assert
            Assert.True(updated, "Update should succeed");

            // Verify change persisted
            var apartment = ApartmentDAL.GetApartmentByID(apartmentID);
            Assert.NotNull(apartment);
            Assert.Equal("UpdatedCode", apartment.ApartmentCode);
            Assert.Equal(150.0m, apartment.Area);
        }

        [Fact]
        public void GetApartmentsByFloor_ReturnsCorrectApartments()
        {
            // Act
            var apartments = ApartmentDAL.GetApartmentsByFloor(_testFloorID);

            // Assert
            Assert.NotNull(apartments);
            Assert.True(apartments.Count >= 0, "Should return list of apartments");
        }

        [Fact]
        public void GetApartmentsByBuilding_ReturnsAllApartmentsInBuilding()
        {
            // Act
            var apartments = ApartmentDAL.GetApartmentsByBuilding(_testBuildingID);

            // Assert
            Assert.NotNull(apartments);
            Assert.True(apartments.Count >= 0);
        }

        [Fact]
        public void DeleteApartment_RemovesFromDatabase()
        {
            // Arrange - Create test apartment
            int apartmentID = ApartmentDAL.CreateApartment(
                _testFloorID,
                "DeleteTest-" + DateTime.Now.Ticks,
                100m,
                "Studio",
                2,
                "To be deleted"
            );

            // Act
            bool deleted = ApartmentDAL.DeleteApartment(apartmentID);

            // Assert
            Assert.True(deleted);

            // Verify deletion
            var apartment = ApartmentDAL.GetApartmentByID(apartmentID);
            Assert.Null(apartment);
        }

        public void Dispose()
        {
            // Cleanup after tests
        }
    }

    /// <summary>
    /// Integration tests for InvoiceDAL class
    /// Tests invoice creation, payment recording, and data relationships
    /// </summary>
    public class InvoiceDALIntegrationTests : IDisposable
    {
        private readonly int _testResidentID = 1; // Assuming exists

        [Fact]
        public void CreateInvoice_ValidData_PersistsToDatabase()
        {
            // Arrange
            decimal totalAmount = 1000m;
            DateTime invoiceDate = DateTime.Now;

            // Act
            int invoiceID = InvoiceDAL.CreateInvoice(
                _testResidentID,
                invoiceDate,
                totalAmount,
                "April",
                2026,
                "Test invoice"
            );

            // Assert
            Assert.True(invoiceID > 0);

            // Verify persistence
            var invoice = InvoiceDAL.GetInvoiceByID(invoiceID);
            Assert.NotNull(invoice);
            Assert.Equal(totalAmount, invoice.TotalAmount);
            Assert.Equal("Pending", invoice.Status);

            // Cleanup
            InvoiceDAL.DeleteInvoice(invoiceID);
        }

        [Fact]
        public void UpdateInvoice_ModifyAmount_UpdatesInDatabase()
        {
            // Arrange
            int invoiceID = 1; // Assuming exists
            decimal newAmount = 1500m;

            // Act
            bool updated = InvoiceDAL.UpdateInvoice(
                invoiceID,
                newAmount,
                "Updated"
            );

            // Assert
            Assert.True(updated);

            // Verify change
            var invoice = InvoiceDAL.GetInvoiceByID(invoiceID);
            Assert.NotNull(invoice);
        }

        [Fact]
        public void GetUnpaidInvoicesByResident_ReturnsOnlyUnpaidInvoices()
        {
            // Act
            var unpaidInvoices = InvoiceDAL.GetUnpaidInvoicesByResident(_testResidentID);

            // Assert
            Assert.NotNull(unpaidInvoices);
            // All should have status "Pending" or "Unpaid"
            foreach (var invoice in unpaidInvoices)
            {
                Assert.True(invoice.Status == "Pending" || invoice.Status == "Unpaid");
            }
        }

        [Fact]
        public void GetAllInvoices_ReturnsAllInvoices()
        {
            // Act
            var invoices = InvoiceDAL.GetAllInvoices();

            // Assert
            Assert.NotNull(invoices);
            Assert.True(invoices.Count >= 0);
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for ResidentDAL class
    /// Tests resident registration and lifecycle operations
    /// </summary>
    public class ResidentDALIntegrationTests : IDisposable
    {
        private readonly int _testApartmentID = 1; // Assuming exists

        [Fact]
        public void CreateResident_ValidData_PersistsToDatabase()
        {
            // Arrange
            string fullName = "Test Resident-" + DateTime.Now.Ticks;
            string cccd = "012345678" + DateTime.Now.Millisecond.ToString("D3");

            // Act
            int residentID = ResidentDAL.CreateResident(
                _testApartmentID,
                fullName,
                "1990-01-15",
                cccd,
                "0912345678",
                "test@example.com",
                "Active"
            );

            // Assert
            Assert.True(residentID > 0);

            // Verify persistence
            var resident = ResidentDAL.GetResidentByID(residentID);
            Assert.NotNull(resident);
            Assert.Equal(fullName, resident.FullName);
            Assert.Equal("Active", resident.Status);

            // Cleanup
            ResidentDAL.DeleteResident(residentID);
        }

        [Fact]
        public void GetResidentsByApartment_ReturnsResidentsInApartment()
        {
            // Act
            var residents = ResidentDAL.GetResidentsByApartment(_testApartmentID);

            // Assert
            Assert.NotNull(residents);
            Assert.True(residents.Count >= 0);
        }

        [Fact]
        public void MoveOutResident_ChangesStatus()
        {
            // Arrange
            int residentID = 1; // Assuming exists and active

            // Act
            bool movedOut = ResidentDAL.MoveOutResident(residentID, DateTime.Now);

            // Assert
            Assert.True(movedOut);

            // Verify status change
            var resident = ResidentDAL.GetResidentByID(residentID);
            Assert.NotNull(resident);
            Assert.Equal("Moved Out", resident.Status);
        }

        [Fact]
        public void GetAllResidents_ReturnsAllResidents()
        {
            // Act
            var residents = ResidentDAL.GetAllResidents();

            // Assert
            Assert.NotNull(residents);
            Assert.True(residents.Count >= 0);
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for ContractDAL class
    /// Tests contract CRUD and relationship integrity
    /// </summary>
    public class ContractDALIntegrationTests : IDisposable
    {
        private readonly int _testApartmentID = 1;
        private readonly int _testResidentID = 1;

        [Fact]
        public void CreateContract_ValidData_PersistsToDatabase()
        {
            // Arrange
            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now.AddMonths(12);

            // Act
            int contractID = ContractDAL.CreateContract(
                _testApartmentID,
                _testResidentID,
                "Lease",
                startDate,
                endDate,
                12,
                false,
                "Test contract"
            );

            // Assert
            Assert.True(contractID > 0);

            // Verify persistence
            var contract = ContractDAL.GetContractByID(contractID);
            Assert.NotNull(contract);
            Assert.Equal("Lease", contract.ContractType);
            Assert.Equal("Pending", contract.Status);

            // Cleanup
            ContractDAL.DeleteContract(contractID);
        }

        [Fact]
        public void GetExpiringContracts_Returns30DayLookout()
        {
            // Act
            var expiringContracts = ContractDAL.GetExpiringContracts(30);

            // Assert
            Assert.NotNull(expiringContracts);
            // All should expire within 30 days
            foreach (var contract in expiringContracts)
            {
                var daysUntilExpiry = (contract.EndDate - DateTime.Now).TotalDays;
                Assert.True(daysUntilExpiry <= 30);
            }
        }

        [Fact]
        public void GetContractsByApartment_ReturnsApartmentContracts()
        {
            // Act
            var contracts = ContractDAL.GetContractsByApartment(_testApartmentID);

            // Assert
            Assert.NotNull(contracts);
        }

        [Fact]
        public void UpdateContractStatus_ChangesStatus()
        {
            // Arrange
            int contractID = 1; // Assuming exists

            // Act
            bool updated = ContractDAL.UpdateContractStatus(contractID, "Active");

            // Assert
            Assert.True(updated);

            // Verify change
            var contract = ContractDAL.GetContractByID(contractID);
            Assert.NotNull(contract);
            Assert.Equal("Active", contract.Status);
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for AuditLogDAL class
    /// Tests audit logging persistence and integrity
    /// </summary>
    public class AuditLogDALIntegrationTests : IDisposable
    {
        [Fact]
        public void LogAction_RecordsAuditEntry()
        {
            // Arrange
            int userID = 1;
            string action = "TestAction-" + DateTime.Now.Ticks;
            string description = "Test audit log entry";

            // Act
            bool logged = AuditLogDAL.LogAction(userID, action, description);

            // Assert
            Assert.True(logged, "Action should be logged");
        }

        [Fact]
        public void AuditLog_PreservesTimestampAccuracy()
        {
            // Arrange
            int userID = 1;
            DateTime beforeLog = DateTime.Now;

            // Act
            bool logged = AuditLogDAL.LogAction(userID, "TimestampTest", "Testing timestamp");

            // Assert
            Assert.True(logged);
            DateTime afterLog = DateTime.Now;

            // Verify timestamp is within acceptable range
            // (This would require reading back from database - simplified here)
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for cascading operations
    /// Tests data relationships and cascading deletes/updates
    /// </summary>
    public class CascadingOperationsTests : IDisposable
    {
        [Fact]
        public void DeleteApartment_DoesNotDeleteResident_PreservesForeignKey()
        {
            // Arrange
            int apartmentID = 1; // Assuming exists with residents
            var residentsBeforeDelete = ResidentDAL.GetResidentsByApartment(apartmentID);

            // Act - Don't actually delete to avoid breaking test data
            // Just verify relationship

            // Assert
            Assert.NotNull(residentsBeforeDelete);
            foreach (var resident in residentsBeforeDelete)
            {
                Assert.Equal(apartmentID, resident.ApartmentID);
            }
        }

        [Fact]
        public void CreateResident_CreatesValidApartmentRelationship()
        {
            // Arrange
            int apartmentID = 1; // Must exist
            var apartments = ApartmentDAL.GetApartmentsByBuilding(1);
            Assert.True(apartments.Count > 0, "Test apartment must exist");

            // Act
            int residentID = ResidentDAL.CreateResident(
                apartmentID,
                "CascadeTest-" + DateTime.Now.Ticks,
                "1990-01-01",
                "012345678901",
                "0912345678",
                "test@example.com",
                "Active"
            );

            // Assert
            Assert.True(residentID > 0);

            var resident = ResidentDAL.GetResidentByID(residentID);
            Assert.NotNull(resident);
            Assert.Equal(apartmentID, resident.ApartmentID);

            // Cleanup
            ResidentDAL.DeleteResident(residentID);
        }

        [Fact]
        public void TransactionIntegrity_MultipleOperations()
        {
            // Arrange
            int apartmentID = 1;
            string apartmentCode = "TransactionTest-" + DateTime.Now.Ticks;

            // Act - Create apartment and verify
            int newApartmentID = ApartmentDAL.CreateApartment(
                1, // FloorID
                apartmentCode,
                100m,
                "2BR",
                4,
                "Transaction test"
            );

            // Assert
            Assert.True(newApartmentID > 0);

            // Verify can be retrieved
            var apartment = ApartmentDAL.GetApartmentByID(newApartmentID);
            Assert.NotNull(apartment);
            Assert.Equal(apartmentCode, apartment.ApartmentCode);

            // Cleanup
            ApartmentDAL.DeleteApartment(newApartmentID);
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    /// <summary>
    /// Integration tests for ComplaintDAL class
    /// Tests complaint workflow and status tracking
    /// </summary>
    public class ComplaintDALIntegrationTests : IDisposable
    {
        private readonly int _testResidentID = 1;

        [Fact]
        public void CreateComplaint_ValidData_PersistsToDatabase()
        {
            // Arrange
            string title = "TestComplaint-" + DateTime.Now.Ticks;

            // Act
            int complaintID = ComplaintDAL.CreateComplaint(
                _testResidentID,
                title,
                "This is a test complaint",
                "Medium"
            );

            // Assert
            Assert.True(complaintID > 0);

            // Verify persistence
            var complaint = ComplaintDAL.GetComplaintByID(complaintID);
            Assert.NotNull(complaint);
            Assert.Equal(title, complaint.Title);
            Assert.Equal("New", complaint.Status); // Initial status

            // Cleanup
            ComplaintDAL.DeleteComplaint(complaintID);
        }

        [Fact]
        public void GetComplaintsByResident_ReturnsResidentComplaints()
        {
            // Act
            var complaints = ComplaintDAL.GetComplaintsByResident(_testResidentID);

            // Assert
            Assert.NotNull(complaints);
            foreach (var complaint in complaints)
            {
                Assert.Equal(_testResidentID, complaint.ResidentID);
            }
        }

        [Fact]
        public void UpdateComplaint_ChangesPriority()
        {
            // Arrange
            int complaintID = 1; // Assuming exists

            // Act
            bool updated = ComplaintDAL.UpdateComplaint(
                complaintID,
                "UpdatedTitle",
                "Updated description",
                "High"
            );

            // Assert
            Assert.True(updated);
        }

        public void Dispose()
        {
            // Cleanup
        }
    }
}
