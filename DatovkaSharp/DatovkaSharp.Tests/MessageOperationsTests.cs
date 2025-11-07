using NUnit.Framework;
using DatovkaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatovkaSharp.Services.Access;
using DatovkaSharp.Services.Info;
using DatovkaSharp.Services.Operations;
using NUnit.Framework.Legacy;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DatovkaSharp.Tests
{
    [TestFixture]
    public class MessageOperationsTests
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
        public async Task SendTextFile_RecipientShouldReceiveCorrectContent()
        {
            // Arrange - Create a text file with known content (UTF-8 without BOM)
            string originalText = "Hello from DatovkaSharp!\nThis is a test message.\nLine 3 with special chars: čřžýáíé";
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"test_content_{DateTime.Now:yyyyMMddHHmmss}.txt");
            await File.WriteAllTextAsync(tempFilePath, originalText, new UTF8Encoding(false)); // UTF-8 without BOM

            try
            {
                // Get recipient's data box ID
                AppConfig config = TestConfiguration.Config;
                using DatovkaClient recipientClient = new DatovkaClient(DataBoxEnvironment.Test);
                recipientClient.LoginWithUsernameAndPassword(config.Account2.Username, config.Account2.Password);
                DatovkaResult<tDbOwnerInfo> recipientInfoResult = await recipientClient.Api.GetDataBoxInfoAsync();
                ClassicAssert.IsTrue(recipientInfoResult.IsSuccess, "Should get recipient data box info");
                ClassicAssert.IsNotNull(recipientInfoResult.Data, "Recipient data box info should not be null");
                string recipientId = recipientInfoResult.Data.dbID!;
                
                // Send message with the text file
                List<string> attachmentPaths = new List<string> { tempFilePath };
                tMessageCreateInput message = _client!.Api.CreateBasicDataMessage(
                    recipientDataBoxId: recipientId,
                    subject: $"Content verification test - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    attachmentPaths: attachmentPaths
                );

                DatovkaResult<tMessageCreateOutput> sendResult = await _client!.Api.SendDataMessageAsync(message);
                ClassicAssert.IsTrue(sendResult.IsSuccess, $"Message should be sent successfully. Got: {sendResult.StatusMessage}");
                ClassicAssert.IsNotNull(sendResult.Data.dmID, "Message ID should not be null");
                string messageId = sendResult.Data.dmID!;
                Console.WriteLine($"✓ Message sent! Message ID: {messageId}");

                // Poll for message delivery
                Console.WriteLine("  Polling for message delivery...");
                tRecord? receivedMessage = null;
                int attempts = 0;
                int maxAttempts = 10;
                
                while (receivedMessage == null && attempts < maxAttempts)
                {
                    attempts++;
                    await Task.Delay(1000);
                    
                    DatovkaListResult<tRecord> messagesResult = await recipientClient.Api.GetListOfReceivedMessagesAsync(days: 1, limit: 50);
                    if (messagesResult is { IsSuccess: true, HasItems: true })
                    {
                        receivedMessage = messagesResult.Data!.FirstOrDefault(m => m.dmID == messageId);
                    }
                    
                    if (receivedMessage == null)
                    {
                        Console.WriteLine($"  Attempt {attempts}/{maxAttempts}: Message not yet received...");
                    }
                }

                ClassicAssert.IsNotNull(receivedMessage, $"Should find the sent message {messageId} after {maxAttempts} attempts");
                Console.WriteLine($"✓ Message received by recipient!");

                // Download attachments
                DatovkaListResult<DataBoxAttachment> attachmentsResult = await recipientClient.Api.GetMessageAttachmentsAsync(messageId);
                ClassicAssert.IsTrue(attachmentsResult.IsSuccess, "Should download attachments successfully");
                ClassicAssert.IsTrue(attachmentsResult.HasItems, "Should have at least one attachment");
                Console.WriteLine($"✓ Downloaded {attachmentsResult.Count} attachment(s)");

                // Get the attachment (should be the first one)
                DataBoxAttachment attachment = attachmentsResult.Data!.First();
                ClassicAssert.IsNotNull(attachment, "Attachment should not be null");
                ClassicAssert.IsNotNull(attachment.Content, "Attachment content should not be null");
                Console.WriteLine($"  Attachment: {attachment.FileName}, Type: {attachment.MimeType}, Size: {attachment.Content.Length} bytes");

                // Decode the content and compare with original
                // Note: If double base64 encoding is used, we need to decode twice
                string receivedText;
                try
                {
                    // First, try to decode as if it's double-encoded (base64 string inside the bytes)
                    string base64String = Encoding.UTF8.GetString(attachment.Content);
                    byte[] decodedBytes = Convert.FromBase64String(base64String);
                    receivedText = Encoding.UTF8.GetString(decodedBytes);
                    Console.WriteLine("  ℹ Attachment was double base64 encoded (as expected for .txt files)");
                }
                catch
                {
                    // If that fails, just decode once
                    receivedText = Encoding.UTF8.GetString(attachment.Content);
                    Console.WriteLine("  ℹ Attachment was single base64 encoded");
                }

                // Assert content matches
                ClassicAssert.AreEqual(originalText, receivedText, "Received text content should match original text");
                Console.WriteLine($"✓ Content verification PASSED!");
                Console.WriteLine($"  Original : \"{originalText.Substring(0, Math.Min(50, originalText.Length))}...\"");
                Console.WriteLine($"  Received : \"{receivedText.Substring(0, Math.Min(50, receivedText.Length))}...\"");
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
        public async Task SendPdfFile_RecipientShouldReceiveCorrectContent()
        {
            // Arrange - Get the sample PDF file
            string pdfFilePath = Path.Combine(AppContext.BaseDirectory, "Files", "sample.pdf");
            ClassicAssert.IsTrue(File.Exists(pdfFilePath), $"Sample PDF file should exist at {pdfFilePath}");

            // Extract text from original PDF
            string originalText;
            using (PdfDocument document = PdfDocument.Open(pdfFilePath))
            {
                StringBuilder textBuilder = new StringBuilder();
                foreach (Page page in document.GetPages())
                {
                    textBuilder.Append(page.Text);
                }
                originalText = textBuilder.ToString().Trim();
            }
            
            Console.WriteLine($"Original PDF text length: {originalText.Length} characters");
            Console.WriteLine($"First 100 chars: {originalText.Substring(0, Math.Min(100, originalText.Length))}...");

            try
            {
                // Get recipient's data box ID
                AppConfig config = TestConfiguration.Config;
                using DatovkaClient recipientClient = new DatovkaClient(DataBoxEnvironment.Test);
                recipientClient.LoginWithUsernameAndPassword(config.Account2.Username, config.Account2.Password);
                DatovkaResult<tDbOwnerInfo> recipientInfoResult = await recipientClient.Api.GetDataBoxInfoAsync();
                ClassicAssert.IsTrue(recipientInfoResult.IsSuccess, "Should get recipient data box info");
                ClassicAssert.IsNotNull(recipientInfoResult.Data, "Recipient data box info should not be null");
                string recipientId = recipientInfoResult.Data.dbID!;
                
                // Send message with the PDF file
                List<string> attachmentPaths = new List<string> { pdfFilePath };
                tMessageCreateInput message = _client!.Api.CreateBasicDataMessage(
                    recipientDataBoxId: recipientId,
                    subject: $"PDF content verification test - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    attachmentPaths: attachmentPaths
                );

                DatovkaResult<tMessageCreateOutput> sendResult = await _client!.Api.SendDataMessageAsync(message);
                ClassicAssert.IsTrue(sendResult.IsSuccess, $"Message should be sent successfully. Got: {sendResult.StatusMessage}");
                ClassicAssert.IsNotNull(sendResult.Data.dmID, "Message ID should not be null");
                string messageId = sendResult.Data.dmID!;
                Console.WriteLine($"✓ Message sent! Message ID: {messageId}");

                // Poll for message delivery
                Console.WriteLine("  Polling for message delivery...");
                tRecord? receivedMessage = null;
                int attempts = 0;
                int maxAttempts = 10;
                
                while (receivedMessage == null && attempts < maxAttempts)
                {
                    attempts++;
                    await Task.Delay(1000);
                    
                    DatovkaListResult<tRecord> messagesResult = await recipientClient.Api.GetListOfReceivedMessagesAsync(days: 1, limit: 50);
                    if (messagesResult is { IsSuccess: true, HasItems: true })
                    {
                        receivedMessage = messagesResult.Data!.FirstOrDefault(m => m.dmID == messageId);
                    }
                    
                    if (receivedMessage == null)
                    {
                        Console.WriteLine($"  Attempt {attempts}/{maxAttempts}: Message not yet received...");
                    }
                }

                ClassicAssert.IsNotNull(receivedMessage, $"Should find the sent message {messageId} after {maxAttempts} attempts");
                Console.WriteLine($"✓ Message received by recipient!");

                // Download attachments
                DatovkaListResult<DataBoxAttachment> attachmentsResult = await recipientClient.Api.GetMessageAttachmentsAsync(messageId);
                ClassicAssert.IsTrue(attachmentsResult.IsSuccess, "Should download attachments successfully");
                ClassicAssert.IsTrue(attachmentsResult.HasItems, "Should have at least one attachment");
                Console.WriteLine($"✓ Downloaded {attachmentsResult.Count} attachment(s)");

                // Get the PDF attachment
                DataBoxAttachment? pdfAttachment = attachmentsResult.Data!.FirstOrDefault(a => a.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
                ClassicAssert.IsNotNull(pdfAttachment, "Should have a PDF attachment");
                ClassicAssert.IsNotNull(pdfAttachment.Content, "PDF attachment content should not be null");
                Console.WriteLine($"  PDF Attachment: {pdfAttachment.FileName}, Type: {pdfAttachment.MimeType}, Size: {pdfAttachment.Content.Length} bytes");

                // Extract text from received PDF
                string receivedText;
                using (MemoryStream memoryStream = new MemoryStream(pdfAttachment.Content))
                using (PdfDocument document = PdfDocument.Open(memoryStream))
                {
                    StringBuilder textBuilder = new StringBuilder();
                    foreach (Page page in document.GetPages())
                    {
                        textBuilder.Append(page.Text);
                    }
                    receivedText = textBuilder.ToString().Trim();
                }

                Console.WriteLine($"Received PDF text length: {receivedText.Length} characters");
                Console.WriteLine($"First 100 chars: {receivedText.Substring(0, Math.Min(100, receivedText.Length))}...");

                // Assert content matches
                ClassicAssert.AreEqual(originalText, receivedText, "Received PDF text content should match original PDF text");
                Console.WriteLine($"✓ PDF Content verification PASSED!");
                Console.WriteLine($"  Text length: {originalText.Length} characters");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed with exception: {ex.Message}");
                throw;
            }
        }

        [Test]
        public async Task SendMessageWithStreamAttachment_ShouldSucceed()
        {
            // Arrange - Create attachments from streams
            AppConfig config = TestConfiguration.Config;
            using DatovkaClient recipientClient = new DatovkaClient(DataBoxEnvironment.Test);
            recipientClient.LoginWithUsernameAndPassword(config.Account2.Username, config.Account2.Password);
            DatovkaResult<tDbOwnerInfo> recipientInfoResult = await recipientClient.Api.GetDataBoxInfoAsync();
            ClassicAssert.IsTrue(recipientInfoResult.IsSuccess, "Should get recipient data box info");
            ClassicAssert.IsNotNull(recipientInfoResult.Data, "Recipient data box info should not be null");
            string recipientId = recipientInfoResult.Data.dbID!;

            // Create a memory stream with some content
            string testContent = "Test content from stream\nCreated at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            byte[] contentBytes = Encoding.UTF8.GetBytes(testContent);
            using MemoryStream stream = new MemoryStream(contentBytes);

            // Create attachment from stream
            DataBoxAttachment attachment = DataBoxAttachment.FromStream(stream, "test_from_stream.txt");
            List<DataBoxAttachment> attachments = new List<DataBoxAttachment> { attachment };

            // Create message using the stream-based overload
            tMessageCreateInput message = _client!.Api.CreateBasicDataMessage(
                recipientDataBoxId: recipientId,
                subject: $"Stream attachment test - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                attachments: attachments
            );

            // Act
            DatovkaResult<tMessageCreateOutput> result = await _client!.Api.SendDataMessageAsync(message);

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, $"Message should be sent successfully. Got: {result.StatusMessage}");
            ClassicAssert.IsNotNull(result.Data.dmID, "Message ID should not be null");
            Console.WriteLine($"✓ Message with stream attachment sent! Message ID: {result.Data.dmID}");
            Console.WriteLine($"  Attachment: {attachment.FileName}");
        }

        [Test]
        public async Task GetListOfReceivedMessages_ShouldReturnMessages()
        {
            // Act
            DatovkaListResult<tRecord> result = await _client!.Api.GetListOfReceivedMessagesAsync(days: 90, limit: 100);

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, "Operation should succeed");
            ClassicAssert.IsNotNull(result.Data, "Messages list should not be null");
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
            DatovkaListResult<tRecord> result = await _client!.Api.GetListOfSentMessagesAsync(days: 90, limit: 100);

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, "Operation should succeed");
            ClassicAssert.IsNotNull(result.Data, "Messages list should not be null");
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
            DatovkaListResult<tRecord> messagesResult = await _client!.Api.GetListOfReceivedMessagesAsync(days: 90, limit: 1);
            ClassicAssert.IsTrue(messagesResult.HasItems, "Need at least one received message for this test");
            string? messageId = messagesResult.Data.First().dmID;

            // Act
            DatovkaResult<byte[]> result = await _client!.Api.DownloadSignedReceivedMessageAsync(messageId!);

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, "Operation should succeed");
            ClassicAssert.IsNotNull(result.Data, "ZFO data should not be null");
            ClassicAssert.IsTrue(result.Data.Length > 0, "ZFO data should not be empty");
            Console.WriteLine($"Downloaded ZFO, size: {result.Data.Length} bytes");
        }

        [Test]
        [Ignore("Only run when there are sent messages")]
        public async Task DownloadSignedSentMessage_ShouldReturnZFO()
        {
            // Arrange
            DatovkaListResult<tRecord> messagesResult = await _client!.Api.GetListOfSentMessagesAsync(days: 90, limit: 1);
            ClassicAssert.IsTrue(messagesResult.HasItems, "Need at least one sent message for this test");
            string? messageId = messagesResult.Data.First().dmID;

            // Act
            DatovkaResult<byte[]> result = await _client!.Api.DownloadSignedSentMessageAsync(messageId!);

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, "Operation should succeed");
            ClassicAssert.IsNotNull(result.Data, "ZFO data should not be null");
            ClassicAssert.IsTrue(result.Data.Length > 0, "ZFO data should not be empty");
            Console.WriteLine($"Downloaded ZFO, size: {result.Data.Length} bytes");
        }

        [Test]
        [Ignore("Only run when there are received messages")]
        public async Task GetReceivedDataMessageAttachments_ShouldReturnAttachments()
        {
            // Arrange
            DatovkaListResult<tRecord> messagesResult = await _client!.Api.GetListOfReceivedMessagesAsync(days: 90, limit: 10);
            ClassicAssert.IsTrue(messagesResult.HasItems, "Need at least one received message for this test");
            string? messageId = messagesResult.Data.First().dmID;

            // Act
            DatovkaListResult<DataBoxAttachment> result = await _client!.Api.GetReceivedDataMessageAttachmentsAsync(messageId!);

            // Assert
            ClassicAssert.IsTrue(result.IsSuccess, "Operation should succeed");
            ClassicAssert.IsNotNull(result.Data, "Attachments list should not be null");
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
            AppConfig config = TestConfiguration.Config;
            using DatovkaClient recipientClient = new DatovkaClient(DataBoxEnvironment.Test);
            recipientClient.LoginWithUsernameAndPassword(config.Account2.Username, config.Account2.Password);
            DatovkaResult<tDbOwnerInfo> recipientInfoResult = await recipientClient.Api.GetDataBoxInfoAsync();
            ClassicAssert.IsTrue(recipientInfoResult.IsSuccess, "Should get recipient data box info");
            ClassicAssert.IsNotNull(recipientInfoResult.Data, "Recipient data box info should not be null");
            string recipientId = recipientInfoResult.Data.dbID!;
            Console.WriteLine($"Recipient data box ID: {recipientId}");

            // Create a minimal text file (Czech Data Box requires at least one attachment)
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"message_content_{DateTime.Now:yyyyMMddHHmmss}.txt");
            string fileContent = $"Test message content\nSent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            await File.WriteAllTextAsync(tempFilePath, fileContent, Encoding.UTF8);

            try
            {
                // Create message with minimal text attachment
                List<string> attachmentPaths = new List<string> { tempFilePath };
                tMessageCreateInput message = _client!.Api.CreateBasicDataMessage(
                    recipientDataBoxId: recipientId,
                    subject: $"Test message with text content - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    attachmentPaths: attachmentPaths
                );

                // Act
                DatovkaResult<tMessageCreateOutput> result = await _client!.Api.SendDataMessageAsync(message);

                // Assert
                ClassicAssert.IsTrue(result.IsSuccess, $"Operation should succeed. Got: {result.StatusMessage}");
                ClassicAssert.IsNotNull(result.Data, "Send result should not be null");
                Console.WriteLine($"Message API response: {result.StatusCode} - {result.StatusMessage}");
                
                ClassicAssert.AreEqual("0000", result.StatusCode, 
                    $"Message should be sent successfully. Got: {result.StatusMessage}");
                
                ClassicAssert.IsNotNull(result.Data.dmID, "Message ID should not be null");
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
            AppConfig config = TestConfiguration.Config;
            using DatovkaClient recipientClient = new DatovkaClient(DataBoxEnvironment.Test);
            recipientClient.LoginWithUsernameAndPassword(config.Account2.Username, config.Account2.Password);
            DatovkaResult<tDbOwnerInfo> recipientInfoResult = await recipientClient.Api.GetDataBoxInfoAsync();
            ClassicAssert.IsTrue(recipientInfoResult.IsSuccess, "Should get recipient data box info");
            ClassicAssert.IsNotNull(recipientInfoResult.Data, "Recipient data box info should not be null");
            string recipientId = recipientInfoResult.Data.dbID!;
            Console.WriteLine($"Recipient data box ID: {recipientId}");

            // Create a test file
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"datovka_test_attachment_{DateTime.Now:yyyyMMddHHmmss}.txt");
            string fileContent = $"This is a test attachment created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                               "DatovkaSharp library test message.\n" +
                               "This message was sent to test attachment functionality.";
            await File.WriteAllTextAsync(tempFilePath, fileContent, Encoding.UTF8);

            try
            {
                // Create message with attachment
                List<string> attachmentPaths = new List<string> { tempFilePath };
                tMessageCreateInput message = _client!.Api.CreateBasicDataMessage(
                    recipientDataBoxId: recipientId,
                    subject: $"Test message with attachment - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    attachmentPaths: attachmentPaths
                );

                // Act
                DatovkaResult<tMessageCreateOutput> result = await _client!.Api.SendDataMessageAsync(message);

                // Assert
                ClassicAssert.IsTrue(result.IsSuccess, $"Operation should succeed. Got: {result.StatusMessage}");
                ClassicAssert.IsNotNull(result.Data, "Send result should not be null");
                Console.WriteLine($"Message API response: {result.StatusCode} - {result.StatusMessage}");
                
                ClassicAssert.AreEqual("0000", result.StatusCode, 
                    $"Message should be sent successfully. Got: {result.StatusMessage}");
                
                ClassicAssert.IsNotNull(result.Data.dmID, "Message ID should not be null");
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
            AppConfig config = TestConfiguration.Config;
            using DatovkaClient recipientClient = new DatovkaClient(DataBoxEnvironment.Test);
            recipientClient.LoginWithUsernameAndPassword(config.Account2.Username, config.Account2.Password);
            DatovkaResult<tDbOwnerInfo> recipientInfoResult = await recipientClient.Api.GetDataBoxInfoAsync();
            ClassicAssert.IsTrue(recipientInfoResult.IsSuccess, "Should get recipient data box info");
            ClassicAssert.IsNotNull(recipientInfoResult.Data, "Recipient data box info should not be null");
            string recipientId = recipientInfoResult.Data.dbID!;
            Console.WriteLine($"Recipient data box ID: {recipientId}");
            
            string testSubject = $"Verification test message - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            // Create a minimal text file (Czech Data Box requires at least one attachment)
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"verify_content_{DateTime.Now:yyyyMMddHHmmss}.txt");
            string fileContent = $"Test verification message\nSent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            await File.WriteAllTextAsync(tempFilePath, fileContent, Encoding.UTF8);

            try
            {
                // Act - Send message with minimal attachment
                List<string> attachmentPaths = new List<string> { tempFilePath };
                tMessageCreateInput message = _client!.Api.CreateBasicDataMessage(
                    recipientDataBoxId: recipientId,
                    subject: testSubject,
                    attachmentPaths: attachmentPaths
                );
                DatovkaResult<tMessageCreateOutput> sendResult = await _client!.Api.SendDataMessageAsync(message);

                // Assert - Check send result
                ClassicAssert.IsTrue(sendResult.IsSuccess, $"Operation should succeed. Got: {sendResult.StatusMessage}");
                ClassicAssert.IsNotNull(sendResult.Data, "Send result should not be null");
                Console.WriteLine($"Message API response: {sendResult.StatusCode} - {sendResult.StatusMessage}");
                
                ClassicAssert.AreEqual("0000", sendResult.StatusCode, 
                    $"Message should be sent successfully. Got: {sendResult.StatusMessage}");
                
                string sentMessageId = sendResult.Data.dmID!;
                ClassicAssert.IsNotNull(sentMessageId, "Message ID should not be null");
                Console.WriteLine($"✓ Message sent successfully! Message ID: {sentMessageId}");

                // Wait a moment for message delivery
                Console.WriteLine("  Waiting for message delivery...");
                await Task.Delay(5000);

                // Check received messages
                DatovkaListResult<tRecord> receivedResult = await recipientClient.Api.GetListOfReceivedMessagesAsync(days: 1, limit: 100);
                ClassicAssert.IsTrue(receivedResult.IsSuccess, "Should get received messages");
                ClassicAssert.IsNotNull(receivedResult.Data, "Received messages should not be null");
                Console.WriteLine($"  Found {receivedResult.Count} recent message(s)");

                // Find the sent message
                tRecord? receivedMessage = receivedResult.Data.FirstOrDefault(m => m.dmAnnotation == testSubject);
                
                ClassicAssert.IsNotNull(receivedMessage, $"Should find the sent message with subject: {testSubject}");
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

