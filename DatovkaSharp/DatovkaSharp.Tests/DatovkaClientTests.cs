using NUnit.Framework;
using DatovkaSharp;
using System;
using System.Linq;
using System.Threading.Tasks;
using DatovkaSharp.Services.Access;
using DatovkaSharp.Services.Search;
using NUnit.Framework.Legacy;
using tDbOwnerInfo = DatovkaSharp.Services.Access.tDbOwnerInfo;

namespace DatovkaSharp.Tests
{
    [TestFixture]
    public class DatovkaClientTests
    {
        private DatovkaClient? _client;

        [SetUp]
        public void Setup()
        {
            AppConfig config = TestConfiguration.Config;
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
            DatovkaResult<bool> connected = await _client!.TestConnectionAsync();

            // Assert
            ClassicAssert.IsTrue(connected.IsSuccess, "Connection to test environment should succeed");
        }

        [Test]
        public async Task GetDataBoxInfo_ShouldReturnOwnerInfo()
        {
            // Act
            DatovkaResult<tDbOwnerInfo> result = await _client!.Api.GetDataBoxInfoAsync();

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, "Operation should succeed");
            ClassicAssert.IsNotNull(result.Data, "Data box info should not be null");
            ClassicAssert.IsNotNull(result.Data.dbID, "Data box ID should not be null");
            Console.WriteLine($"Data Box ID: {result.Data.dbID}");
        }

        [Test]
        public async Task GetUserInfo_ShouldReturnUserInfo()
        {
            // Act
            DatovkaResult<tDbUserInfo> result = await _client!.Api.GetUserInfoAsync();

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, "Operation should succeed");
            ClassicAssert.IsNotNull(result.Data, "User info should not be null");
            ClassicAssert.IsNotNull(result.Data.userID, "User ID should not be null");
            Console.WriteLine($"User ID: {result.Data.userID}");
        }

        [Test]
        public async Task GetPasswordExpires_ShouldReturnDateOrNull()
        {
            // Act
            DatovkaResult<DateTime?> result = await _client!.Api.GetPasswordExpiresAsync();

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, "Operation should succeed");
            // Note: Password expiration may be null for certificate-based accounts or accounts without expiration
            if (result.Data.HasValue)
            {
                Console.WriteLine($"Password expires: {result.Data}");
                ClassicAssert.That(result.Data.Value, Is.GreaterThan(DateTime.MinValue));
            }
            else
            {
                Console.WriteLine("Password expiration not set (certificate-based or no expiration)");
                ClassicAssert.Pass("Password expiration is not set for this account");
            }
        }

        [Test]
        public async Task GetStats_ShouldReturnStatistics()
        {
            // Act
            DatovkaResult<string> result = await _client!.Api.GetStatsAsync();

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, "Operation should succeed");
            ClassicAssert.IsNotNull(result.Data, "Statistics should not be null");
            ClassicAssert.IsNotEmpty(result.Data, "Statistics should not be empty");
            Console.WriteLine($"ISDS Statistics: {result.Data}");
        }

        [Test]
        public async Task GetDataBoxCreditInfo_ShouldReturnCreditData()
        {
            // Arrange & Act
            DatovkaResult<tDBCreditInfoOutput> result = await _client!.Api.GetDataBoxCreditInfoAsync();

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, $"Should get credit info. Got: {result.StatusMessage}");
            ClassicAssert.IsNotNull(result.Data, "Credit info data should not be null");
            ClassicAssert.IsNotNull(result.Data!.currentCredit, "Current credit should not be null");
            
            Console.WriteLine($"✓ Current credit: {result.Data.currentCredit}");
            if (!string.IsNullOrEmpty(result.Data.notifEmail))
            {
                Console.WriteLine($"  Notification email: {result.Data.notifEmail}");
            }
            if (result.Data.ciRecords is { Length: > 0 })
            {
                Console.WriteLine($"  Credit history records: {result.Data.ciRecords.Length}");
            }
        }

        [Test]
        public async Task GetDataBoxCreditInfo_WithDateRange_ShouldReturnCreditHistory()
        {
            // Arrange
            DateTime fromDate = DateTime.Now.AddDays(-30);
            DateTime toDate = DateTime.Now;

            // Act
            DatovkaResult<tDBCreditInfoOutput> result = await _client!.Api.GetDataBoxCreditInfoAsync(
                dataBoxId: null,  // Use logged-in user's ID
                fromDate: fromDate,
                toDate: toDate
            );

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, $"Should get credit info with date range. Got: {result.StatusMessage}");
            ClassicAssert.IsNotNull(result.Data, "Credit info data should not be null");
            ClassicAssert.IsNotNull(result.Data.currentCredit, "Current credit should not be null");
            
            Console.WriteLine($"✓ Current credit: {result.Data.currentCredit}");
            Console.WriteLine($"  Date range: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
            if (result.Data.ciRecords is { Length: > 0 })
            {
                Console.WriteLine($"  Credit history records: {result.Data.ciRecords.Length}");
                foreach (tCiRecord? record in result.Data.ciRecords.Take(3))  // Show first 3
                {
                    Console.WriteLine($"    - {record.ciEventTime:yyyy-MM-dd HH:mm:ss}: {record.ciEventType}, Change: {record.ciCreditChange}, After: {record.ciCreditAfter}");
                }
            }
        }
    }
}

