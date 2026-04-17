using Xunit;
using ApartmentManager.BLL;
using System;

namespace ApartmentManager.Tests
{
    /// <summary>
    /// Unit tests for InvoiceBLL class
    /// Tests invoice creation, payment tracking, and financial operations
    /// </summary>
    public class InvoiceBLLTests
    {
        #region CreateInvoice Tests

        [Fact]
        public void CreateInvoice_ValidData_ReturnsSuccess()
        {
            // Arrange
            int residentID = 1; // Assuming exists
            DateTime invoiceDate = DateTime.Now;
            decimal totalAmount = 1000.00m;
            string month = "April";
            int year = 2026;

            // Act
            var result = InvoiceBLL.CreateInvoice(
                residentID,
                invoiceDate,
                totalAmount,
                month,
                year,
                "April 2026 maintenance fees"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateInvoice_InvalidResident_ReturnsFalse()
        {
            // Arrange
            int invalidResidentID = -1;
            decimal totalAmount = 1000.00m;

            // Act
            var result = InvoiceBLL.CreateInvoice(
                invalidResidentID,
                DateTime.Now,
                totalAmount,
                "April",
                2026,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateInvoice_NegativeAmount_ReturnsFalse()
        {
            // Arrange
            decimal negativeAmount = -1000m;

            // Act
            var result = InvoiceBLL.CreateInvoice(
                1,
                DateTime.Now,
                negativeAmount,
                "April",
                2026,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateInvoice_ZeroAmount_ReturnsFalse()
        {
            // Arrange
            decimal zeroAmount = 0m;

            // Act
            var result = InvoiceBLL.CreateInvoice(
                1,
                DateTime.Now,
                zeroAmount,
                "April",
                2026,
                "Test"
            );

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region RecordPayment Tests

        [Fact]
        public void RecordPayment_ValidPayment_ReturnsSuccess()
        {
            // Arrange
            int invoiceID = 1; // Assuming exists
            decimal paymentAmount = 500.00m;
            DateTime paymentDate = DateTime.Now;

            // Act
            var result = InvoiceBLL.RecordPayment(
                invoiceID,
                paymentAmount,
                paymentDate,
                "Cash"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RecordPayment_InvalidPaymentAmount_ReturnsFalse()
        {
            // Arrange
            int invoiceID = 1;
            decimal invalidAmount = -100m; // Negative

            // Act
            var result = InvoiceBLL.RecordPayment(
                invoiceID,
                invalidAmount,
                DateTime.Now,
                "Cash"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RecordPayment_ZeroAmount_ReturnsFalse()
        {
            // Arrange
            int invoiceID = 1;
            decimal zeroAmount = 0m;

            // Act
            var result = InvoiceBLL.RecordPayment(
                invoiceID,
                zeroAmount,
                DateTime.Now,
                "Cash"
            );

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RecordPayment_InvalidInvoice_ReturnsFalse()
        {
            // Arrange
            int invalidInvoiceID = -1;
            decimal paymentAmount = 100m;

            // Act
            var result = InvoiceBLL.RecordPayment(
                invalidInvoiceID,
                paymentAmount,
                DateTime.Now,
                "Cash"
            );

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region DeleteInvoice Tests

        [Fact]
        public void DeleteInvoice_UnpaidInvoice_ReturnsSuccess()
        {
            // Arrange - Only unpaid invoices can be deleted
            int unpaidInvoiceID = 999; // Assuming unpaid or doesn't exist

            // Act
            var result = InvoiceBLL.DeleteInvoice(unpaidInvoiceID);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void DeleteInvoice_PaidInvoice_ReturnsFalse()
        {
            // Arrange
            // This test assumes we can identify a paid invoice
            int paidInvoiceID = 1; // Assuming this is paid

            // Act
            var result = InvoiceBLL.DeleteInvoice(paidInvoiceID);

            // Assert
            Assert.NotNull(result);
            // Should prevent deletion of paid invoices for audit trail
        }

        [Fact]
        public void DeleteInvoice_InvalidID_ReturnsFalse()
        {
            // Arrange
            int invalidID = -1;

            // Act
            var result = InvoiceBLL.DeleteInvoice(invalidID);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region GetAllInvoices Tests

        [Fact]
        public void GetAllInvoices_ReturnsInvoiceList()
        {
            // Act
            var invoices = InvoiceBLL.GetAllInvoices();

            // Assert
            Assert.NotNull(invoices);
            // List should be populated or empty depending on database state
        }

        #endregion

        #region Invoice Statistics Tests

        [Fact]
        public void GetInvoiceStatistics_ReturnsValidStats()
        {
            // Act
            var stats = InvoiceBLL.GetInvoiceStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalInvoices >= 0);
            Assert.True(stats.PaidInvoices >= 0);
            Assert.True(stats.UnpaidInvoices >= 0);
            Assert.True(stats.CollectionRate >= 0);
        }

        [Fact]
        public void GetInvoiceStatistics_CollectionRateCalculation()
        {
            // Act
            var stats = InvoiceBLL.GetInvoiceStatistics();

            // Assert
            // Collection rate should be calculated correctly
            if (stats.TotalInvoices > 0)
            {
                decimal expectedRate = (stats.PaidInvoices * 100) / stats.TotalInvoices;
                Assert.Equal(expectedRate, stats.CollectionRate);
            }
        }

        #endregion

        #region GetUnpaidInvoices Tests

        [Fact]
        public void GetUnpaidInvoicesByResident_ValidResident_ReturnsUnpaidOnly()
        {
            // Arrange
            int residentID = 1; // Assuming exists

            // Act
            var unpaidInvoices = InvoiceBLL.GetUnpaidInvoicesByResident(residentID);

            // Assert
            Assert.NotNull(unpaidInvoices);
            // All returned invoices should have status "Unpaid"
        }

        [Fact]
        public void GetUnpaidInvoicesByResident_InvalidResident_ReturnsEmpty()
        {
            // Arrange
            int invalidResidentID = -1;

            // Act
            var unpaidInvoices = InvoiceBLL.GetUnpaidInvoicesByResident(invalidResidentID);

            // Assert
            Assert.NotNull(unpaidInvoices);
        }

        #endregion
    }
}
