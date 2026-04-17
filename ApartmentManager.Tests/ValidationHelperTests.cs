using Xunit;
using ApartmentManager.Utilities;

namespace ApartmentManager.Tests
{
    /// <summary>
    /// Unit tests for ValidationHelper class covering all 16 validation methods
    /// </summary>
    public class ValidationHelperTests
    {
        #region Email Validation Tests

        [Fact]
        public void IsValidEmail_ValidEmail_ReturnsTrue()
        {
            // Arrange
            string validEmail = "user@example.com";

            // Act
            bool result = ValidationHelper.IsValidEmail(validEmail);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidEmail_InvalidEmail_ReturnsFalse()
        {
            // Arrange & Act & Assert
            Assert.False(ValidationHelper.IsValidEmail("invalid.email"));
            Assert.False(ValidationHelper.IsValidEmail("user@"));
            Assert.False(ValidationHelper.IsValidEmail("@example.com"));
            Assert.False(ValidationHelper.IsValidEmail("user name@example.com"));
        }

        [Fact]
        public void IsValidEmail_EmptyEmail_ReturnsFalse()
        {
            // Assert
            Assert.False(ValidationHelper.IsValidEmail(""));
            Assert.False(ValidationHelper.IsValidEmail(null));
        }

        #endregion

        #region Phone Validation Tests

        [Fact]
        public void IsValidPhone_ValidVietnamesePhone_ReturnsTrue()
        {
            // Arrange & Act & Assert
            Assert.True(ValidationHelper.IsValidPhone("0912345678")); // 10 digits
            Assert.True(ValidationHelper.IsValidPhone("09 1234 5678")); // Format with spaces
            Assert.True(ValidationHelper.IsValidPhone("+84912345678")); // International format
        }

        [Fact]
        public void IsValidPhone_InvalidPhone_ReturnsFalse()
        {
            // Arrange & Act & Assert
            Assert.False(ValidationHelper.IsValidPhone("123456789")); // Too short
            Assert.False(ValidationHelper.IsValidPhone("abc1234567")); // Contains letters
            Assert.False(ValidationHelper.IsValidPhone("")); // Empty
            Assert.False(ValidationHelper.IsValidPhone(null)); // Null
        }

        #endregion

        #region CCCD Validation Tests

        [Fact]
        public void IsValidCCCD_ValidCCCD_ReturnsTrue()
        {
            // Arrange
            string validCCCD = "012345678910"; // 12 digits

            // Act
            bool result = ValidationHelper.IsValidCCCD(validCCCD);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidCCCD_InvalidLength_ReturnsFalse()
        {
            // Arrange & Act & Assert
            Assert.False(ValidationHelper.IsValidCCCD("123456789")); // Too short
            Assert.False(ValidationHelper.IsValidCCCD("12345678901234")); // Too long
            Assert.False(ValidationHelper.IsValidCCCD("")); // Empty
            Assert.False(ValidationHelper.IsValidCCCD(null)); // Null
        }

        [Fact]
        public void IsValidCCCD_NonNumeric_ReturnsFalse()
        {
            // Assert
            Assert.False(ValidationHelper.IsValidCCCD("01234567891A"));
            Assert.False(ValidationHelper.IsValidCCCD("0123456789AB"));
        }

        #endregion

        #region Birth Date Validation Tests

        [Fact]
        public void IsValidBirthDate_ValidBirthDate_ReturnsTrue()
        {
            // Arrange - 25 years old
            var birthDate = System.DateTime.Now.AddYears(-25);

            // Act
            bool result = ValidationHelper.IsValidBirthDate(birthDate);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidBirthDate_TooYoung_ReturnsFalse()
        {
            // Arrange - 17 years old (less than 18)
            var birthDate = System.DateTime.Now.AddYears(-17);

            // Act
            bool result = ValidationHelper.IsValidBirthDate(birthDate);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidBirthDate_TooOld_ReturnsFalse()
        {
            // Arrange - 150 years old
            var birthDate = System.DateTime.Now.AddYears(-150);

            // Act
            bool result = ValidationHelper.IsValidBirthDate(birthDate);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidBirthDate_FutureDate_ReturnsFalse()
        {
            // Arrange
            var birthDate = System.DateTime.Now.AddDays(1);

            // Act
            bool result = ValidationHelper.IsValidBirthDate(birthDate);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region License Plate Validation Tests

        [Fact]
        public void IsValidLicensePlate_ValidPlate_ReturnsTrue()
        {
            // Arrange & Act & Assert
            Assert.True(ValidationHelper.IsValidLicensePlate("30A-123.45")); // Standard format
            Assert.True(ValidationHelper.IsValidLicensePlate("30A12345")); // Alternative format
        }

        [Fact]
        public void IsValidLicensePlate_InvalidPlate_ReturnsFalse()
        {
            // Arrange & Act & Assert
            Assert.False(ValidationHelper.IsValidLicensePlate("ABC-123.45")); // Invalid format
            Assert.False(ValidationHelper.IsValidLicensePlate("30-123456")); // Missing number
            Assert.False(ValidationHelper.IsValidLicensePlate("")); // Empty
            Assert.False(ValidationHelper.IsValidLicensePlate(null)); // Null
        }

        #endregion

        #region Amount Validation Tests

        [Fact]
        public void IsValidAmount_PositiveAmount_ReturnsTrue()
        {
            // Arrange & Act & Assert
            Assert.True(ValidationHelper.IsValidAmount(100.50m));
            Assert.True(ValidationHelper.IsValidAmount(0.01m));
            Assert.True(ValidationHelper.IsValidAmount(999999.99m));
        }

        [Fact]
        public void IsValidAmount_InvalidAmount_ReturnsFalse()
        {
            // Arrange & Act & Assert
            Assert.False(ValidationHelper.IsValidAmount(-100m)); // Negative
            Assert.False(ValidationHelper.IsValidAmount(0m)); // Zero
            Assert.False(ValidationHelper.IsValidAmount(decimal.MaxValue)); // Too large
        }

        #endregion

        #region Age Validation Tests

        [Fact]
        public void IsValidAge_ValidAge_ReturnsTrue()
        {
            // Arrange & Act & Assert
            Assert.True(ValidationHelper.IsValidAge(18)); // Minimum
            Assert.True(ValidationHelper.IsValidAge(50));
            Assert.True(ValidationHelper.IsValidAge(120)); // Maximum
        }

        [Fact]
        public void IsValidAge_InvalidAge_ReturnsFalse()
        {
            // Arrange & Act & Assert
            Assert.False(ValidationHelper.IsValidAge(17)); // Below minimum
            Assert.False(ValidationHelper.IsValidAge(0)); // Zero
            Assert.False(ValidationHelper.IsValidAge(-5)); // Negative
            Assert.False(ValidationHelper.IsValidAge(121)); // Above maximum
        }

        #endregion

        #region String Length Validation Tests

        [Fact]
        public void IsValidLength_ValidLength_ReturnsTrue()
        {
            // Arrange
            string testString = "Hello World";

            // Act
            bool result = ValidationHelper.IsValidLength(testString, 5, 20);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidLength_TooShort_ReturnsFalse()
        {
            // Arrange
            string testString = "Hi";

            // Act
            bool result = ValidationHelper.IsValidLength(testString, 5, 20);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidLength_TooLong_ReturnsFalse()
        {
            // Arrange
            string testString = "This is a very long string that exceeds the maximum length";

            // Act
            bool result = ValidationHelper.IsValidLength(testString, 5, 20);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidLength_EmptyString_ReturnsFalse()
        {
            // Assert
            Assert.False(ValidationHelper.IsValidLength("", 1, 20));
            Assert.False(ValidationHelper.IsValidLength(null, 1, 20));
        }

        #endregion

        #region Date Range Validation Tests

        [Fact]
        public void IsValidDateRange_ValidRange_ReturnsTrue()
        {
            // Arrange
            var startDate = System.DateTime.Now;
            var endDate = System.DateTime.Now.AddDays(10);

            // Act
            bool result = ValidationHelper.IsValidDateRange(startDate, endDate);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidDateRange_InvalidRange_ReturnsFalse()
        {
            // Arrange
            var startDate = System.DateTime.Now.AddDays(10);
            var endDate = System.DateTime.Now;

            // Act
            bool result = ValidationHelper.IsValidDateRange(startDate, endDate);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidDateRange_SameDate_ReturnsFalse()
        {
            // Arrange
            var date = System.DateTime.Now;

            // Act
            bool result = ValidationHelper.IsValidDateRange(date, date);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Year Validation Tests

        [Fact]
        public void IsValidYear_ValidYear_ReturnsTrue()
        {
            // Arrange & Act & Assert
            Assert.True(ValidationHelper.IsValidYear(1980)); // Minimum
            Assert.True(ValidationHelper.IsValidYear(2000));
            Assert.True(ValidationHelper.IsValidYear(System.DateTime.Now.Year)); // Current
        }

        [Fact]
        public void IsValidYear_InvalidYear_ReturnsFalse()
        {
            // Arrange & Act & Assert
            Assert.False(ValidationHelper.IsValidYear(1979)); // Before minimum
            Assert.False(ValidationHelper.IsValidYear(0)); // Invalid
            Assert.False(ValidationHelper.IsValidYear(-100)); // Negative
            Assert.False(ValidationHelper.IsValidYear(System.DateTime.Now.Year + 1)); // Future
        }

        #endregion

        #region Future Date Validation Tests

        [Fact]
        public void IsNotFutureDate_PastDate_ReturnsTrue()
        {
            // Arrange
            var pastDate = System.DateTime.Now.AddDays(-1);

            // Act
            bool result = ValidationHelper.IsNotFutureDate(pastDate);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsNotFutureDate_FutureDate_ReturnsFalse()
        {
            // Arrange
            var futureDate = System.DateTime.Now.AddDays(1);

            // Act
            bool result = ValidationHelper.IsNotFutureDate(futureDate);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Past Date Validation Tests

        [Fact]
        public void IsNotPastDate_FutureDate_ReturnsTrue()
        {
            // Arrange
            var futureDate = System.DateTime.Now.AddDays(1);

            // Act
            bool result = ValidationHelper.IsNotPastDate(futureDate);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsNotPastDate_PastDate_ReturnsFalse()
        {
            // Arrange
            var pastDate = System.DateTime.Now.AddDays(-1);

            // Act
            bool result = ValidationHelper.IsNotPastDate(pastDate);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Range Validation Tests

        [Fact]
        public void IsInRange_ValidValue_ReturnsTrue()
        {
            // Arrange & Act & Assert
            Assert.True(ValidationHelper.IsInRange(50, 1, 100));
            Assert.True(ValidationHelper.IsInRange(1, 1, 100)); // Minimum
            Assert.True(ValidationHelper.IsInRange(100, 1, 100)); // Maximum
        }

        [Fact]
        public void IsInRange_InvalidValue_ReturnsFalse()
        {
            // Arrange & Act & Assert
            Assert.False(ValidationHelper.IsInRange(0, 1, 100)); // Below minimum
            Assert.False(ValidationHelper.IsInRange(101, 1, 100)); // Above maximum
        }

        #endregion
    }
}
