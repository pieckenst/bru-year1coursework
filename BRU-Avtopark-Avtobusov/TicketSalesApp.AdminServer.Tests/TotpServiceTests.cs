using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TicketSalesApp.AdminServer.Services;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using OtpNet;

namespace TicketSalesApp.AdminServer.Tests
{
    public class TotpServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<TotpService>> _mockLogger;
        private readonly TotpService _totpService;

        public TotpServiceTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options, "SQLite");
            _mockLogger = new Mock<ILogger<TotpService>>();
            _totpService = new TotpService(_context, _mockLogger.Object);

            // Seed test data
            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            var testUser = new User
            {
                UserId = 1,
                GuidId = Guid.NewGuid(),
                Login = "testuser",
                PasswordHash = "hashedpassword",
                Role = 0,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GenerateSetupAsync_ShouldCreateTotpSetup_ForValidUser()
        {
            // Arrange
            var userId = 1L;

            // Act
            var result = await _totpService.GenerateSetupAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.SecretKey);
            Assert.NotEmpty(result.QrCodeUri);
            Assert.NotEmpty(result.QrCodeDataUrl);
            Assert.NotEmpty(result.ManualEntryKey);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("TicketSalesApp", result.Issuer);

            // Verify secret was stored in database
            var user = await _context.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.NotNull(user.TotpSecret);
            Assert.False(user.IsTotpEnabled); // Should not be enabled until verification
        }

        [Fact]
        public async Task GenerateSetupAsync_ShouldThrowException_ForInvalidUser()
        {
            // Arrange
            var invalidUserId = 999L;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _totpService.GenerateSetupAsync(invalidUserId));
        }

        [Fact]
        public async Task EnableTotpAsync_ShouldEnableTotp_WithValidCode()
        {
            // Arrange
            var userId = 1L;
            
            // First generate setup
            var setup = await _totpService.GenerateSetupAsync(userId);
            
            // Generate a valid TOTP code
            var secretBytes = Base32Encoding.ToBytes(setup.SecretKey);
            var totp = new Totp(secretBytes);
            var validCode = totp.ComputeTotp();

            // Act
            var result = await _totpService.EnableTotpAsync(userId, validCode);

            // Assert
            Assert.True(result);

            // Verify TOTP is enabled in database
            var user = await _context.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.True(user.IsTotpEnabled);
            Assert.NotNull(user.TotpEnabledAt);
            Assert.NotNull(user.TotpRecoveryCodes);
        }

        [Fact]
        public async Task EnableTotpAsync_ShouldFail_WithInvalidCode()
        {
            // Arrange
            var userId = 1L;
            
            // First generate setup
            await _totpService.GenerateSetupAsync(userId);
            
            var invalidCode = "123456"; // Invalid code

            // Act
            var result = await _totpService.EnableTotpAsync(userId, invalidCode);

            // Assert
            Assert.False(result);

            // Verify TOTP is not enabled in database
            var user = await _context.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.False(user.IsTotpEnabled);
        }

        [Fact]
        public async Task ValidateCodeAsync_ShouldReturnTrue_ForValidCode()
        {
            // Arrange
            var userId = 1L;
            
            // Setup and enable TOTP
            var setup = await _totpService.GenerateSetupAsync(userId);
            var secretBytes = Base32Encoding.ToBytes(setup.SecretKey);
            var totp = new Totp(secretBytes);
            var validCode = totp.ComputeTotp();
            await _totpService.EnableTotpAsync(userId, validCode);

            // Generate a new valid code
            var newValidCode = totp.ComputeTotp();

            // Act
            var result = await _totpService.ValidateCodeAsync(userId, newValidCode);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateCodeAsync_ShouldReturnFalse_ForInvalidCode()
        {
            // Arrange
            var userId = 1L;
            
            // Setup and enable TOTP
            var setup = await _totpService.GenerateSetupAsync(userId);
            var secretBytes = Base32Encoding.ToBytes(setup.SecretKey);
            var totp = new Totp(secretBytes);
            var validCode = totp.ComputeTotp();
            await _totpService.EnableTotpAsync(userId, validCode);

            var invalidCode = "000000";

            // Act
            var result = await _totpService.ValidateCodeAsync(userId, invalidCode);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateCodeAsync_ShouldReturnFalse_ForUserWithoutTotp()
        {
            // Arrange
            var userId = 1L;
            var anyCode = "123456";

            // Act
            var result = await _totpService.ValidateCodeAsync(userId, anyCode);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsTotpEnabledAsync_ShouldReturnFalse_ForUserWithoutTotp()
        {
            // Arrange
            var userId = 1L;

            // Act
            var result = await _totpService.IsTotpEnabledAsync(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsTotpEnabledAsync_ShouldReturnTrue_ForUserWithEnabledTotp()
        {
            // Arrange
            var userId = 1L;
            
            // Setup and enable TOTP
            var setup = await _totpService.GenerateSetupAsync(userId);
            var secretBytes = Base32Encoding.ToBytes(setup.SecretKey);
            var totp = new Totp(secretBytes);
            var validCode = totp.ComputeTotp();
            await _totpService.EnableTotpAsync(userId, validCode);

            // Act
            var result = await _totpService.IsTotpEnabledAsync(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GenerateRecoveryCodesAsync_ShouldGenerateRecoveryCodes_ForEnabledUser()
        {
            // Arrange
            var userId = 1L;
            
            // Setup and enable TOTP
            var setup = await _totpService.GenerateSetupAsync(userId);
            var secretBytes = Base32Encoding.ToBytes(setup.SecretKey);
            var totp = new Totp(secretBytes);
            var validCode = totp.ComputeTotp();
            await _totpService.EnableTotpAsync(userId, validCode);

            // Act
            var recoveryCodes = await _totpService.GenerateRecoveryCodesAsync(userId);

            // Assert
            Assert.NotNull(recoveryCodes);
            Assert.Equal(10, recoveryCodes.Count()); // Should generate 10 recovery codes
            
            // All codes should be 8 characters long
            foreach (var code in recoveryCodes)
            {
                Assert.Equal(8, code.Length);
            }
        }

        [Fact]
        public async Task DisableTotpAsync_ShouldDisableTotp_WithValidCode()
        {
            // Arrange
            var userId = 1L;
            
            // Setup and enable TOTP
            var setup = await _totpService.GenerateSetupAsync(userId);
            var secretBytes = Base32Encoding.ToBytes(setup.SecretKey);
            var totp = new Totp(secretBytes);
            var validCode = totp.ComputeTotp();
            await _totpService.EnableTotpAsync(userId, validCode);

            // Generate a new valid code for disabling
            var disableCode = totp.ComputeTotp();

            // Act
            var result = await _totpService.DisableTotpAsync(userId, disableCode);

            // Assert
            Assert.True(result);

            // Verify TOTP is disabled in database
            var user = await _context.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.False(user.IsTotpEnabled);
            Assert.Null(user.TotpSecret);
            Assert.Null(user.TotpEnabledAt);
            Assert.Null(user.TotpRecoveryCodes);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}