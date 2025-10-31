using NUnit.Framework;
using DatovkaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatovkaSharp.Services.Info;
using DatovkaSharp.Services.Operations;

namespace DatovkaSharp.Tests
{
    [TestFixture]
    public class MessageOperationsTests
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
        public async Task GetListOfReceivedMessages_ShouldReturnMessages()
        {
            // Act
            var result = await _client!.Api.GetListOfReceivedMessagesAsync(days: 90, limit: 100);

            // Assert
            Assert.IsTrue(result.IsSuccess, "Operation should succeed");
            Assert.IsNotNull(result.Data, "Messages list should not be null");
            Console.WriteLine($"Received {result.Count} messages");
            
            if (result.HasItems)
            {
                tRecord firstMessage = result.Data.First();
                Console.WriteLine($"First message ID: {firstMessage.dmID}");
                Console.WriteLine($"First message annotation: {firstMessage.dmAnnotation}");
            }
        }

        [Test]
        public async Task GetListOfSentMessages_ShouldReturnMessages()
        {
            // Act
            var result = await _client!.Api.GetListOfSentMessagesAsync(days: 90, limit: 100);

            // Assert
            Assert.IsTrue(result.IsSuccess, "Operation should succeed");
            Assert.IsNotNull(result.Data, "Messages list should not be null");
            Console.WriteLine($"Sent {result.Count} messages");
            
            if (result.HasItems)
            {
                tRecord firstMessage = result.Data.First();
                Console.WriteLine($"First message ID: {firstMessage.dmID}");
                Console.WriteLine($"First message annotation: {firstMessage.dmAnnotation}");
            }
        }

        [Test]
        [Ignore("Only run when there are received messages")]
        public async Task DownloadSignedReceivedMessage_ShouldReturnZFO()
        {
            // Arrange
            var messagesResult = await _client!.Api.GetListOfReceivedMessagesAsync(days: 90, limit: 1);
            Assert.IsTrue(messagesResult.HasItems, "Need at least one received message for this test");
            string? messageId = messagesResult.Data.First().dmID;

            // Act
            var result = await _client!.Api.DownloadSignedReceivedMessageAsync(messageId!);

            // Assert
            Assert.IsTrue(result.IsSuccess, "Operation should succeed");
            Assert.IsNotNull(result.Data, "ZFO data should not be null");
            Assert.IsTrue(result.Data.Length > 0, "ZFO data should not be empty");
            Console.WriteLine($"Downloaded ZFO, size: {result.Data.Length} bytes");
        }

        [Test]
        [Ignore("Only run when there are sent messages")]
        public async Task DownloadSignedSentMessage_ShouldReturnZFO()
        {
            // Arrange
            var messagesResult = await _client!.Api.GetListOfSentMessagesAsync(days: 90, limit: 1);
            Assert.IsTrue(messagesResult.HasItems, "Need at least one sent message for this test");
            string? messageId = messagesResult.Data.First().dmID;

            // Act
            var result = await _client!.Api.DownloadSignedSentMessageAsync(messageId!);

            // Assert
            Assert.IsTrue(result.IsSuccess, "Operation should succeed");
            Assert.IsNotNull(result.Data, "ZFO data should not be null");
            Assert.IsTrue(result.Data.Length > 0, "ZFO data should not be empty");
            Console.WriteLine($"Downloaded ZFO, size: {result.Data.Length} bytes");
        }

        [Test]
        [Ignore("Only run when there are received messages")]
        public async Task GetReceivedDataMessageAttachments_ShouldReturnAttachments()
        {
            // Arrange
            var messagesResult = await _client!.Api.GetListOfReceivedMessagesAsync(days: 90, limit: 10);
            Assert.IsTrue(messagesResult.HasItems, "Need at least one received message for this test");
            string? messageId = messagesResult.Data.First().dmID;

            // Act
            var result = await _client!.Api.GetReceivedDataMessageAttachmentsAsync(messageId!);

            // Assert
            Assert.IsTrue(result.IsSuccess, "Operation should succeed");
            Assert.IsNotNull(result.Data, "Attachments list should not be null");
            Console.WriteLine($"Message has {result.Count} attachments");
            
            foreach (DataBoxAttachment attachment in result.Data)
            {
                Console.WriteLine($"Attachment: {attachment.FileName}, Type: {attachment.MimeType}, Size: {attachment.Content.Length} bytes");
            }
        }

        [Test]
        public async Task SendMessage_WithTextContent_ShouldSucceed()
        {
            // Arrange - Get recipient's actual data box ID
            var config = TestConfiguration.Config;
            using var recipientClient = new DatovkaClient(DataBoxEnvironment.Test);
            recipientClient.LoginWithUsernameAndPassword(config.Account2.Username, config.Account2.Password);
            var recipientInfoResult = await recipientClient.Api.GetDataBoxInfoAsync();
            Assert.IsTrue(recipientInfoResult.IsSuccess, "Should get recipient data box info");
            Assert.IsNotNull(recipientInfoResult.Data, "Recipient data box info should not be null");
            string recipientId = recipientInfoResult.Data.dbID!;
            Console.WriteLine($"Recipient data box ID: {recipientId}");

            // Create a minimal text file (Czech Data Box requires at least one attachment)
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"message_content_{DateTime.Now:yyyyMMddHHmmss}.txt");
            string fileContent = $"Test message content\nSent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            File.WriteAllText(tempFilePath, fileContent, Encoding.UTF8);

            try
            {
                // Create message with minimal text attachment
                var attachmentPaths = new List<string> { tempFilePath };
                tMessageCreateInput message = _client!.Api.CreateBasicDataMessage(
                    recipientDataBoxId: recipientId,
                    subject: $"Test message with text content - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    attachmentPaths: attachmentPaths
                );

                // Act
                var result = await _client!.Api.SendDataMessageAsync(message);

                // Assert
                Assert.IsTrue(result.IsSuccess, $"Operation should succeed. Got: {result.StatusMessage}");
                Assert.IsNotNull(result.Data, "Send result should not be null");
                Console.WriteLine($"Message API response: {result.StatusCode} - {result.StatusMessage}");
                
                Assert.AreEqual("0000", result.StatusCode, 
                    $"Message should be sent successfully. Got: {result.StatusMessage}");
                
                Assert.IsNotNull(result.Data.dmID, "Message ID should not be null");
                Console.WriteLine($"✓ Message sent successfully! Message ID: {result.Data.dmID}");
                Console.WriteLine($"  Recipient: {recipientId}");
                Console.WriteLine($"  Content file: {Path.GetFileName(tempFilePath)} ({new FileInfo(tempFilePath).Length} bytes)");
            }
            finally
            {
                // Clean up the test file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        [Test]
        public async Task SendMessage_WithAttachment_ShouldSucceed()
        {
            // Arrange - Get recipient's actual data box ID
            var config = TestConfiguration.Config;
            using var recipientClient = new DatovkaClient(DataBoxEnvironment.Test);
            recipientClient.LoginWithUsernameAndPassword(config.Account2.Username, config.Account2.Password);
            var recipientInfoResult = await recipientClient.Api.GetDataBoxInfoAsync();
            Assert.IsTrue(recipientInfoResult.IsSuccess, "Should get recipient data box info");
            Assert.IsNotNull(recipientInfoResult.Data, "Recipient data box info should not be null");
            string recipientId = recipientInfoResult.Data.dbID!;
            Console.WriteLine($"Recipient data box ID: {recipientId}");

            // Create a test file
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"datovka_test_attachment_{DateTime.Now:yyyyMMddHHmmss}.txt");
            string fileContent = $"This is a test attachment created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                               "DatovkaSharp library test message.\n" +
                               "This message was sent to test attachment functionality.";
            File.WriteAllText(tempFilePath, fileContent, Encoding.UTF8);

            try
            {
                // Create message with attachment
                var attachmentPaths = new List<string> { tempFilePath };
                tMessageCreateInput message = _client!.Api.CreateBasicDataMessage(
                    recipientDataBoxId: recipientId,
                    subject: $"Test message with attachment - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    attachmentPaths: attachmentPaths
                );

                // Act
                var result = await _client!.Api.SendDataMessageAsync(message);

                // Assert
                Assert.IsTrue(result.IsSuccess, $"Operation should succeed. Got: {result.StatusMessage}");
                Assert.IsNotNull(result.Data, "Send result should not be null");
                Console.WriteLine($"Message API response: {result.StatusCode} - {result.StatusMessage}");
                
                Assert.AreEqual("0000", result.StatusCode, 
                    $"Message should be sent successfully. Got: {result.StatusMessage}");
                
                Assert.IsNotNull(result.Data.dmID, "Message ID should not be null");
                Console.WriteLine($"✓ Message with attachment sent successfully! Message ID: {result.Data.dmID}");
                Console.WriteLine($"  Recipient: {recipientId}");
                Console.WriteLine($"  Attachment: {Path.GetFileName(tempFilePath)} ({new FileInfo(tempFilePath).Length} bytes)");
            }
            finally
            {
                // Clean up the test file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        [Test]
        public async Task SendAndVerifyMessage_ShouldReceiveMessage()
        {
            // Arrange - Get recipient's actual data box ID
            var config = TestConfiguration.Config;
            using var recipientClient = new DatovkaClient(DataBoxEnvironment.Test);
            recipientClient.LoginWithUsernameAndPassword(config.Account2.Username, config.Account2.Password);
            var recipientInfoResult = await recipientClient.Api.GetDataBoxInfoAsync();
            Assert.IsTrue(recipientInfoResult.IsSuccess, "Should get recipient data box info");
            Assert.IsNotNull(recipientInfoResult.Data, "Recipient data box info should not be null");
            string recipientId = recipientInfoResult.Data.dbID!;
            Console.WriteLine($"Recipient data box ID: {recipientId}");
            
            string testSubject = $"Verification test message - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            // Create a minimal text file (Czech Data Box requires at least one attachment)
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"verify_content_{DateTime.Now:yyyyMMddHHmmss}.txt");
            string fileContent = $"Test verification message\nSent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            File.WriteAllText(tempFilePath, fileContent, Encoding.UTF8);

            try
            {
                // Act - Send message with minimal attachment
                var attachmentPaths = new List<string> { tempFilePath };
                tMessageCreateInput message = _client!.Api.CreateBasicDataMessage(
                    recipientDataBoxId: recipientId,
                    subject: testSubject,
                    attachmentPaths: attachmentPaths
                );
                var sendResult = await _client!.Api.SendDataMessageAsync(message);

                // Assert - Check send result
                Assert.IsTrue(sendResult.IsSuccess, $"Operation should succeed. Got: {sendResult.StatusMessage}");
                Assert.IsNotNull(sendResult.Data, "Send result should not be null");
                Console.WriteLine($"Message API response: {sendResult.StatusCode} - {sendResult.StatusMessage}");
                
                Assert.AreEqual("0000", sendResult.StatusCode, 
                    $"Message should be sent successfully. Got: {sendResult.StatusMessage}");
                
                string sentMessageId = sendResult.Data.dmID!;
                Assert.IsNotNull(sentMessageId, "Message ID should not be null");
                Console.WriteLine($"✓ Message sent successfully! Message ID: {sentMessageId}");

                // Wait a moment for message delivery
                Console.WriteLine("  Waiting for message delivery...");
                await Task.Delay(5000);

                // Check received messages
                var receivedResult = await recipientClient.Api.GetListOfReceivedMessagesAsync(days: 1, limit: 100);
                Assert.IsTrue(receivedResult.IsSuccess, "Should get received messages");
                Assert.IsNotNull(receivedResult.Data, "Received messages should not be null");
                Console.WriteLine($"  Found {receivedResult.Count} recent message(s)");

                // Find the sent message
                tRecord? receivedMessage = receivedResult.Data.FirstOrDefault(m => m.dmAnnotation == testSubject);
                
                Assert.IsNotNull(receivedMessage, $"Should find the sent message with subject: {testSubject}");
                Console.WriteLine($"✓ Message successfully received by recipient!");
                Console.WriteLine($"  Sent Message ID: {sentMessageId}");
                Console.WriteLine($"  Received Message ID: {receivedMessage.dmID}");
                Console.WriteLine($"  Subject: {receivedMessage.dmAnnotation}");
                Console.WriteLine($"  Sender: {receivedMessage.dmSender}");
                Console.WriteLine($"  Delivery Time: {receivedMessage.dmDeliveryTime}");
            }
            finally
            {
                // Clean up the test file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
    }
}

