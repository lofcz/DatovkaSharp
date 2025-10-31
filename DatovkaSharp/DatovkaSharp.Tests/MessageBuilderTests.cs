using System;
using System.IO;
using System.Text;
using DatovkaSharp.Exceptions;
using DatovkaSharp.Services.Operations;
using NUnit.Framework;
using NUnit.Framework.Legacy;

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
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();

            // Act
            tMessageCreateInput message = builder
                .To("testbox123")
                .WithSubject("Test Subject")
                .AddTextContent("content.txt", "Test message content")
                .Build();

            // Assert
            ClassicAssert.IsNotNull(message);
            ClassicAssert.AreEqual("testbox123", message.dmEnvelope.dbIDRecipient);
            ClassicAssert.AreEqual("Test Subject", message.dmEnvelope.dmAnnotation);
            ClassicAssert.IsNotNull(message.dmFiles);
            ClassicAssert.AreEqual(1, message.dmFiles.Length);
        }

        [Test]
        public void Build_WithFileAttachment_ShouldSucceed()
        {
            // Arrange
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();

            // Act
            tMessageCreateInput message = builder
                .To("testbox123")
                .WithSubject("Test with attachment")
                .AddAttachment(_testFilePath)
                .Build();

            // Assert
            ClassicAssert.IsNotNull(message);
            ClassicAssert.AreEqual(1, message.dmFiles.Length);
            ClassicAssert.AreEqual(Path.GetFileName(_testFilePath), message.dmFiles[0].dmFileDescr);
        }

        [Test]
        public void Build_WithMultipleAttachments_ShouldSucceed()
        {
            // Arrange
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();

            // Act
            tMessageCreateInput message = builder
                .To("testbox123")
                .WithSubject("Multiple attachments")
                .AddTextContent("file1.txt", "Content 1")
                .AddTextContent("file2.txt", "Content 2")
                .AddAttachment(_testFilePath)
                .Build();

            // Assert
            ClassicAssert.IsNotNull(message);
            ClassicAssert.AreEqual(3, message.dmFiles.Length);
        }

        [Test]
        public void Build_WithoutRecipient_ShouldThrowException()
        {
            // Arrange
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();

            // Act & Assert
            MissingRequiredFieldException? ex = ClassicAssert.Throws<MissingRequiredFieldException>(() =>
            {
                builder
                    .WithSubject("Test")
                    .AddTextContent("content.txt", "Content")
                    .Build();
            });

            ClassicAssert.That(ex?.Message, Does.Contain("recipient"));
        }

        [Test]
        public void Build_WithoutSubject_ShouldThrowException()
        {
            // Arrange
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();

            // Act & Assert
            MissingRequiredFieldException? ex = ClassicAssert.Throws<MissingRequiredFieldException>(() =>
            {
                builder
                    .To("testbox123")
                    .AddTextContent("content.txt", "Content")
                    .Build();
            });

            ClassicAssert.That(ex?.Message, Does.Contain("annotation"));
        }

        [Test]
        public void Build_WithoutAttachments_ShouldThrowException()
        {
            // Arrange
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();

            // Act & Assert
            MissingMainFileException? ex = ClassicAssert.Throws<MissingMainFileException>(() =>
            {
                builder
                    .To("testbox123")
                    .WithSubject("Test")
                    .Build();
            });

            ClassicAssert.That(ex?.Message, Does.Contain("attachment"));
        }

        [Test]
        public void AddAttachment_ExceedingSizeLimit_ShouldThrowException()
        {
            // Arrange
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();
            byte[] largeContent = new byte[MessageValidator.MaxTotalSizeBytes + 1];

            // Act & Assert
            ClassicAssert.Throws<FileSizeOverflowException>(() =>
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
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();

            // Act
            tMessageCreateInput message = builder
                .To("testbox123")
                .WithSubject("Test")
                .AsPersonalDelivery(true)
                .AddTextContent("content.txt", "Content")
                .Build();

            // Assert
            ClassicAssert.IsTrue(message.dmEnvelope.dmPersonalDelivery);
        }

        [Test]
        public void GetCurrentTotalSize_ShouldReturnCorrectSize()
        {
            // Arrange
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();
            string content = "Test content";
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);

            // Act
            builder.AddTextContent("test.txt", content);

            // Assert
            ClassicAssert.AreEqual(contentBytes.Length, builder.GetCurrentTotalSize());
        }

        [Test]
        public void GetAttachmentCount_ShouldReturnCorrectCount()
        {
            // Arrange
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder();

            // Act
            builder.AddTextContent("file1.txt", "Content 1");
            builder.AddTextContent("file2.txt", "Content 2");

            // Assert
            ClassicAssert.AreEqual(2, builder.GetAttachmentCount());
        }

        [Test]
        public void FluentAPI_ShouldChainProperly()
        {
            // Arrange & Act
            DatovkaMessageBuilder builder = new DatovkaMessageBuilder()
                .To("testbox123")
                .WithSubject("Fluent test")
                .WithSenderIdent("SENDER001")
                .WithSenderRefNumber("REF123")
                .WithRecipientRefNumber("REF456")
                .AsPersonalDelivery(true)
                .AddTextContent("content.txt", "Test content");

            tMessageCreateInput message = builder.Build();

            // Assert
            ClassicAssert.IsNotNull(message);
            ClassicAssert.AreEqual("testbox123", message.dmEnvelope.dbIDRecipient);
            ClassicAssert.AreEqual("Fluent test", message.dmEnvelope.dmAnnotation);
            ClassicAssert.AreEqual("SENDER001", message.dmEnvelope.dmSenderIdent);
            ClassicAssert.AreEqual("REF123", message.dmEnvelope.dmSenderRefNumber);
            ClassicAssert.AreEqual("REF456", message.dmEnvelope.dmRecipientRefNumber);
            ClassicAssert.IsTrue(message.dmEnvelope.dmPersonalDelivery);
        }
    }
}

