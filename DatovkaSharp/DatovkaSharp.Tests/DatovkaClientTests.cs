using NUnit.Framework;
using DatovkaSharp;
using System;
using System.Threading.Tasks;
using DatovkaSharp.Services.Access;

namespace DatovkaSharp.Tests
{
    [TestFixture]
    public class DatovkaClientTests
    {
        private DatovkaClient? _client;

        [SetUp]
        public void Setup()
        {
            var config = TestConfiguration.Config;
            _client = new DatovkaClient(DataBoxEnvironment.Test);
            _client.LoginWithUsernameAndPassword(config.Account1.Username, config.Account1.Password);
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
        }

        [Test]
        public async Task TestConnection_ShouldSucceed()
        {
            // Act
            bool connected = _client!.TestConnection();

            // Assert
            Assert.IsTrue(connected, "Connection to test environment should succeed");
        }

        [Test]
        public async Task GetDataBoxInfo_ShouldReturnOwnerInfo()
        {
            // Act
            var result = await _client!.Api.GetDataBoxInfoAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess, "Operation should succeed");
            Assert.IsNotNull(result.Data, "Data box info should not be null");
            Assert.IsNotNull(result.Data.dbID, "Data box ID should not be null");
            Console.WriteLine($"Data Box ID: {result.Data.dbID}");
        }

        [Test]
        public async Task GetUserInfo_ShouldReturnUserInfo()
        {
            // Act
            var result = await _client!.Api.GetUserInfoAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess, "Operation should succeed");
            Assert.IsNotNull(result.Data, "User info should not be null");
            Assert.IsNotNull(result.Data.userID, "User ID should not be null");
            Console.WriteLine($"User ID: {result.Data.userID}");
        }

        [Test]
        public async Task GetPasswordExpires_ShouldReturnDateOrNull()
        {
            // Act
            var result = await _client!.Api.GetPasswordExpiresAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess, "Operation should succeed");
            // Note: Password expiration may be null for certificate-based accounts or accounts without expiration
            if (result.Data.HasValue)
            {
                Console.WriteLine($"Password expires: {result.Data}");
                Assert.That(result.Data.Value, Is.GreaterThan(DateTime.MinValue));
            }
            else
            {
                Console.WriteLine("Password expiration not set (certificate-based or no expiration)");
                Assert.Pass("Password expiration is not set for this account");
            }
        }

        [Test]
        public async Task GetStats_ShouldReturnStatistics()
        {
            // Act
            var result = await _client!.Api.GetStatsAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess, "Operation should succeed");
            Assert.IsNotNull(result.Data, "Statistics should not be null");
            Assert.IsNotEmpty(result.Data, "Statistics should not be empty");
            Console.WriteLine($"ISDS Statistics: {result.Data}");
        }
    }
}

