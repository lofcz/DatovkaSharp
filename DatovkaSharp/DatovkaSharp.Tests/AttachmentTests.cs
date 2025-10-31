using NUnit.Framework;
using DatovkaSharp;
using System;
using System.IO;
using System.Text;
using NUnit.Framework.Legacy;

namespace DatovkaSharp.Tests
{
    [TestFixture]
    public class AttachmentTests
    {
        private string _testFilePath = string.Empty;

        [SetUp]
        public void Setup()
        {
            // Create a temporary test file
            _testFilePath = Path.Combine(Path.GetTempPath(), "datovka_test_file.txt");
            File.WriteAllText(_testFilePath, "This is a test file for DatovkaSharp");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up the test file
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [Test]
        public void DataBoxAttachment_FromFile_ShouldCreateAttachment()
        {
            // Act
            DataBoxAttachment attachment = DataBoxAttachment.FromFile(_testFilePath);

            // Assert
            ClassicAssert.IsNotNull(attachment, "Attachment should not be null");
            ClassicAssert.AreEqual("datovka_test_file.txt", attachment.FileName);
            ClassicAssert.IsTrue(attachment.Content.Length > 0, "Attachment content should not be empty");
            ClassicAssert.IsNotNull(attachment.MimeType, "MIME type should not be null");
            Console.WriteLine($"Created attachment: {attachment.FileName}, Type: {attachment.MimeType}, Size: {attachment.Content.Length} bytes");
        }

        [Test]
        public void DataBoxAttachment_SaveToFile_ShouldSaveFile()
        {
            // Arrange
            byte[] content = Encoding.UTF8.GetBytes("Test content for saving");
            DataBoxAttachment attachment = new DataBoxAttachment("test.txt", content, "text/plain");
            string outputPath = Path.Combine(Path.GetTempPath(), "datovka_save_test.txt");

            try
            {
                // Act
                attachment.SaveToFile(outputPath);

                // Assert
                ClassicAssert.IsTrue(File.Exists(outputPath), "File should be saved");
                byte[] savedContent = File.ReadAllBytes(outputPath);
                ClassicAssert.AreEqual(content.Length, savedContent.Length, "Saved content length should match");
            }
            finally
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
        }

        [Test]
        public void DataBoxAttachment_GetContentAsBase64_ShouldReturnBase64String()
        {
            // Arrange
            byte[] content = Encoding.UTF8.GetBytes("Test content");
            DataBoxAttachment attachment = new DataBoxAttachment("test.txt", content, "text/plain");

            // Act
            string base64 = attachment.GetContentAsBase64();

            // Assert
            ClassicAssert.IsNotNull(base64, "Base64 string should not be null");
            ClassicAssert.IsNotEmpty(base64, "Base64 string should not be empty");
            ClassicAssert.AreEqual(Convert.ToBase64String(content), base64);
        }

        [Test]
        public void DataBoxHelper_GetMimeType_ShouldReturnCorrectMimeType()
        {
            // Arrange & Act
            string pdfMime = DataBoxHelper.GetMimeType("document.pdf");
            string jpgMime = DataBoxHelper.GetMimeType("image.jpg");
            string txtMime = DataBoxHelper.GetMimeType("file.txt");
            string zfoMime = DataBoxHelper.GetMimeType("message.zfo");

            // Assert
            ClassicAssert.AreEqual("application/pdf", pdfMime);
            ClassicAssert.AreEqual("image/jpeg", jpgMime);
            ClassicAssert.AreEqual("text/plain", txtMime);
            ClassicAssert.AreEqual("application/vnd.software602.filler.form-xml-zip", zfoMime);
        }
    }
}

