using System;
using System.IO;
using System.Text;
using DatovkaSharp.Exceptions;
using NUnit.Framework;

namespace DatovkaSharp.Tests
{
    [TestFixture]
    public class MessageBuilderTests
    {
        private string _testFilePath = string.Empty;

        [SetUp]
        public void Setup()
        {
            // Create a temporary test file
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_attachment_{Guid.NewGuid()}.txt");
            File.WriteAllText(_testFilePath, "Test content for attachment", Encoding.UTF8);
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up the temporary file
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [Test]
        public void Build_WithValidMessage_ShouldSucceed()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();

            // Act
            var message = builder
                .To("testbox123")
                .WithSubject("Test Subject")
                .AddTextContent("content.txt", "Test message content")
                .Build();

            // Assert
            Assert.IsNotNull(message);
            Assert.AreEqual("testbox123", message.dmEnvelope.dbIDRecipient);
            Assert.AreEqual("Test Subject", message.dmEnvelope.dmAnnotation);
            Assert.IsNotNull(message.dmFiles);
            Assert.AreEqual(1, message.dmFiles.Length);
        }

        [Test]
        public void Build_WithFileAttachment_ShouldSucceed()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();

            // Act
            var message = builder
                .To("testbox123")
                .WithSubject("Test with attachment")
                .AddAttachment(_testFilePath)
                .Build();

            // Assert
            Assert.IsNotNull(message);
            Assert.AreEqual(1, message.dmFiles.Length);
            Assert.AreEqual(Path.GetFileName(_testFilePath), message.dmFiles[0].dmFileDescr);
        }

        [Test]
        public void Build_WithMultipleAttachments_ShouldSucceed()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();

            // Act
            var message = builder
                .To("testbox123")
                .WithSubject("Multiple attachments")
                .AddTextContent("file1.txt", "Content 1")
                .AddTextContent("file2.txt", "Content 2")
                .AddAttachment(_testFilePath)
                .Build();

            // Assert
            Assert.IsNotNull(message);
            Assert.AreEqual(3, message.dmFiles.Length);
        }

        [Test]
        public void Build_WithoutRecipient_ShouldThrowException()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();

            // Act & Assert
            var ex = Assert.Throws<MissingRequiredFieldException>(() =>
            {
                builder
                    .WithSubject("Test")
                    .AddTextContent("content.txt", "Content")
                    .Build();
            });

            Assert.That(ex?.Message, Does.Contain("recipient"));
        }

        [Test]
        public void Build_WithoutSubject_ShouldThrowException()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();

            // Act & Assert
            var ex = Assert.Throws<MissingRequiredFieldException>(() =>
            {
                builder
                    .To("testbox123")
                    .AddTextContent("content.txt", "Content")
                    .Build();
            });

            Assert.That(ex?.Message, Does.Contain("annotation"));
        }

        [Test]
        public void Build_WithoutAttachments_ShouldThrowException()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();

            // Act & Assert
            var ex = Assert.Throws<MissingMainFileException>(() =>
            {
                builder
                    .To("testbox123")
                    .WithSubject("Test")
                    .Build();
            });

            Assert.That(ex?.Message, Does.Contain("attachment"));
        }

        [Test]
        public void AddAttachment_ExceedingSizeLimit_ShouldThrowException()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();
            var largeContent = new byte[MessageValidator.MaxTotalSizeBytes + 1];

            // Act & Assert
            Assert.Throws<FileSizeOverflowException>(() =>
            {
                builder
                    .To("testbox123")
                    .WithSubject("Test")
                    .AddAttachment("large_file.bin", largeContent);
            });
        }

        [Test]
        public void WithPersonalDelivery_ShouldSetFlag()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();

            // Act
            var message = builder
                .To("testbox123")
                .WithSubject("Test")
                .AsPersonalDelivery(true)
                .AddTextContent("content.txt", "Content")
                .Build();

            // Assert
            Assert.IsTrue(message.dmEnvelope.dmPersonalDelivery);
        }

        [Test]
        public void GetCurrentTotalSize_ShouldReturnCorrectSize()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();
            var content = "Test content";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            // Act
            builder.AddTextContent("test.txt", content);

            // Assert
            Assert.AreEqual(contentBytes.Length, builder.GetCurrentTotalSize());
        }

        [Test]
        public void GetAttachmentCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var builder = new DatovkaMessageBuilder();

            // Act
            builder.AddTextContent("file1.txt", "Content 1");
            builder.AddTextContent("file2.txt", "Content 2");

            // Assert
            Assert.AreEqual(2, builder.GetAttachmentCount());
        }

        [Test]
        public void FluentAPI_ShouldChainProperly()
        {
            // Arrange & Act
            var builder = new DatovkaMessageBuilder()
                .To("testbox123")
                .WithSubject("Fluent test")
                .WithSenderIdent("SENDER001")
                .WithSenderRefNumber("REF123")
                .WithRecipientRefNumber("REF456")
                .AsPersonalDelivery(true)
                .AddTextContent("content.txt", "Test content");

            var message = builder.Build();

            // Assert
            Assert.IsNotNull(message);
            Assert.AreEqual("testbox123", message.dmEnvelope.dbIDRecipient);
            Assert.AreEqual("Fluent test", message.dmEnvelope.dmAnnotation);
            Assert.AreEqual("SENDER001", message.dmEnvelope.dmSenderIdent);
            Assert.AreEqual("REF123", message.dmEnvelope.dmSenderRefNumber);
            Assert.AreEqual("REF456", message.dmEnvelope.dmRecipientRefNumber);
            Assert.IsTrue(message.dmEnvelope.dmPersonalDelivery);
        }
    }
}

