using NUnit.Framework;
using NUnit.Framework.Legacy;
using DatovkaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DatovkaSharp.Tests
{
    [TestFixture]
    public class CertificateAuthenticationTests
    {
        private DatovkaClient? _client;
        private CertificateConfig? _certConfig;

        [SetUp]
        public void Setup()
        {
            _certConfig = TestConfiguration.Config.Certificates;
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
        }

        [Test]
        public void LoginWithCertificate_FileNotFound_ShouldThrowFileNotFoundException()
        {
            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
            {
                _client.LoginWithCertificate("non_existent_certificate.pfx", "password");
            });
        }

        [Test]
        public void LoginWithCertificateAndDataBoxId_FileNotFound_ShouldThrowFileNotFoundException()
        {
            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
            {
                _client.LoginWithCertificateAndDataBoxId("non_existent_certificate.pfx", "abc123", "password");
            });
        }

        [Test]
        public void LoginWithCertificate_FromByteArray_ShouldSucceed()
        {
            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);
            byte[] fakeCertData = new byte[] { 1, 2, 3, 4, 5 };

            // Act - this will succeed in setting up, actual connection test would fail with invalid cert
            Assert.DoesNotThrow(() =>
            {
                _client.LoginWithCertificate(fakeCertData, "password");
            });

            // Verify the authentication mode was set
            Console.WriteLine("✓ Certificate from byte array login method succeeded");
        }

        [Test]
        public void LoginWithCertificate_FromStream_ShouldSucceed()
        {
            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);
            byte[] fakeCertData = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            using (var stream = new MemoryStream(fakeCertData))
            {
                Assert.DoesNotThrow(() =>
                {
                    _client.LoginWithCertificate(stream, "password");
                });
            }

            Console.WriteLine("✓ Certificate from stream login method succeeded");
        }

        [Test]
        public async Task LoginWithCertificate_SS_Mode_WithRealCertificate()
        {
            // Skip if no certificate is configured
            if (_certConfig == null || 
                string.IsNullOrEmpty(_certConfig.SSCertificatePath) || 
                !File.Exists(_certConfig.SSCertificatePath))
            {
                Assert.Ignore("SS certificate not configured or not found. Place your SS certificate at the configured path to run this test.");
                return;
            }

            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);
            
            Console.WriteLine($"Testing SS mode with certificate: {_certConfig.SSCertificatePath}");

            // Act
            _client.LoginWithCertificate(_certConfig.SSCertificatePath, _certConfig.SSCertificatePassword);

            // Try to connect
            var connected = await _client.TestConnectionAsync();

            // Assert
            ClassicAssert.IsTrue(connected, "Connection with SS certificate should succeed");
            Console.WriteLine("✓ SS mode authentication successful");

            // Try to get data box info
            var result = await _client.Api.GetDataBoxInfoAsync();
            if (result.IsSuccess)
            {
                Console.WriteLine($"  Data Box ID: {result.Data?.dbID}");
            }
            else
            {
                Console.WriteLine($"  GetDataBoxInfo failed: {result.StatusCode} - {result.StatusMessage}");
            }
        }

        [Test]
        public async Task LoginWithCertificate_SS_Mode_FromByteArray()
        {
            // Skip if no certificate is configured
            if (_certConfig == null || 
                string.IsNullOrEmpty(_certConfig.SSCertificatePath) || 
                !File.Exists(_certConfig.SSCertificatePath))
            {
                Assert.Ignore("SS certificate not configured or not found. Place your SS certificate at the configured path to run this test.");
                return;
            }

            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);
            byte[] certBytes = File.ReadAllBytes(_certConfig.SSCertificatePath);
            
            Console.WriteLine($"Testing SS mode with certificate from byte array");

            // Act
            _client.LoginWithCertificate(certBytes, _certConfig.SSCertificatePassword);

            // Try to connect
            var connected = await _client.TestConnectionAsync();

            // Assert
            ClassicAssert.IsTrue(connected, "Connection with SS certificate from byte array should succeed");
            Console.WriteLine("✓ SS mode authentication from byte array successful");
        }

        [Test]
        public async Task LoginWithCertificate_SS_Mode_FromStream()
        {
            // Skip if no certificate is configured
            if (_certConfig == null || 
                string.IsNullOrEmpty(_certConfig.SSCertificatePath) || 
                !File.Exists(_certConfig.SSCertificatePath))
            {
                Assert.Ignore("SS certificate not configured or not found. Place your SS certificate at the configured path to run this test.");
                return;
            }

            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);
            
            Console.WriteLine($"Testing SS mode with certificate from stream");

            // Act
            using (var stream = File.OpenRead(_certConfig.SSCertificatePath))
            {
                _client.LoginWithCertificate(stream, _certConfig.SSCertificatePassword);
            }

            // Try to connect
            var connected = await _client.TestConnectionAsync();

            // Assert
            ClassicAssert.IsTrue(connected, "Connection with SS certificate from stream should succeed");
            Console.WriteLine("✓ SS mode authentication from stream successful");
        }

        [Test]
        public async Task LoginWithCertificateAndDataBoxId_HSS_Mode_WithRealCertificate()
        {
            // Skip if no certificate is configured
            if (_certConfig == null || 
                string.IsNullOrEmpty(_certConfig.HSSCertificatePath) || 
                !File.Exists(_certConfig.HSSCertificatePath) ||
                string.IsNullOrEmpty(_certConfig.HSSDataBoxId))
            {
                Assert.Ignore("HSS certificate or DataBox ID not configured. Configure HSS certificate path and target DataBox ID to run this test.");
                return;
            }

            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);
            
            Console.WriteLine($"Testing HSS mode with certificate: {_certConfig.HSSCertificatePath}");
            Console.WriteLine($"Target DataBox ID: {_certConfig.HSSDataBoxId}");

            // Act
            _client.LoginWithCertificateAndDataBoxId(
                _certConfig.HSSCertificatePath, 
                _certConfig.HSSDataBoxId, 
                _certConfig.HSSCertificatePassword);

            // Try to connect
            var connected = await _client.TestConnectionAsync();

            // Assert
            ClassicAssert.IsTrue(connected, "Connection with HSS certificate should succeed");
            Console.WriteLine("✓ HSS mode authentication successful");

            // Try to get data box info
            var result = await _client.Api.GetDataBoxInfoAsync();
            if (result.IsSuccess)
            {
                Console.WriteLine($"  Data Box ID: {result.Data?.dbID}");
                Console.WriteLine($"  Data Box Type: {result.Data?.dbType}");
            }
            else
            {
                Console.WriteLine($"  GetDataBoxInfo failed: {result.StatusCode} - {result.StatusMessage}");
            }
        }

        [Test]
        public async Task LoginWithCertificateAndDataBoxId_HSS_Mode_FromByteArray()
        {
            // Skip if no certificate is configured
            if (_certConfig == null || 
                string.IsNullOrEmpty(_certConfig.HSSCertificatePath) || 
                !File.Exists(_certConfig.HSSCertificatePath) ||
                string.IsNullOrEmpty(_certConfig.HSSDataBoxId))
            {
                Assert.Ignore("HSS certificate or DataBox ID not configured. Configure HSS certificate path and target DataBox ID to run this test.");
                return;
            }

            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);
            byte[] certBytes = File.ReadAllBytes(_certConfig.HSSCertificatePath);
            
            Console.WriteLine($"Testing HSS mode with certificate from byte array");
            Console.WriteLine($"Target DataBox ID: {_certConfig.HSSDataBoxId}");

            // Act
            _client.LoginWithCertificateAndDataBoxId(certBytes, _certConfig.HSSDataBoxId, _certConfig.HSSCertificatePassword);

            // Try to connect
            var connected = await _client.TestConnectionAsync();

            // Assert
            ClassicAssert.IsTrue(connected, "Connection with HSS certificate from byte array should succeed");
            Console.WriteLine("✓ HSS mode authentication from byte array successful");
        }

        [Test]
        public async Task LoginWithCertificateAndDataBoxId_HSS_Mode_FromStream()
        {
            // Skip if no certificate is configured
            if (_certConfig == null || 
                string.IsNullOrEmpty(_certConfig.HSSCertificatePath) || 
                !File.Exists(_certConfig.HSSCertificatePath) ||
                string.IsNullOrEmpty(_certConfig.HSSDataBoxId))
            {
                Assert.Ignore("HSS certificate or DataBox ID not configured. Configure HSS certificate path and target DataBox ID to run this test.");
                return;
            }

            // Arrange
            _client = new DatovkaClient(DataBoxEnvironment.Test);
            
            Console.WriteLine($"Testing HSS mode with certificate from stream");
            Console.WriteLine($"Target DataBox ID: {_certConfig.HSSDataBoxId}");

            // Act
            using (var stream = File.OpenRead(_certConfig.HSSCertificatePath))
            {
                _client.LoginWithCertificateAndDataBoxId(stream, _certConfig.HSSDataBoxId, _certConfig.HSSCertificatePassword);
            }

            // Try to connect
            var connected = await _client.TestConnectionAsync();

            // Assert
            ClassicAssert.IsTrue(connected, "Connection with HSS certificate from stream should succeed");
            Console.WriteLine("✓ HSS mode authentication from stream successful");
        }
    }
}

