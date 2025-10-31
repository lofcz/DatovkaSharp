using System;
using System.IO;

namespace DatovkaSharp
{
    /// <summary>
    /// Represents an attachment in a data box message
    /// </summary>
    public class DataBoxAttachment
    {
        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// File content (base64 encoded)
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// MIME type
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }

        public DataBoxAttachment()
        {
            FileName = string.Empty;
            Content = Array.Empty<byte>();
            MimeType = "application/octet-stream";
        }

        public DataBoxAttachment(string fileName, byte[] content, string? mimeType = null)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            MimeType = mimeType ?? DataBoxHelper.GetMimeType(fileName);
        }

        /// <summary>
        /// Create attachment from file path
        /// </summary>
        public static DataBoxAttachment FromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            string? fileName = Path.GetFileName(filePath);
            byte[] content = File.ReadAllBytes(filePath);
            string mimeType = DataBoxHelper.GetMimeType(fileName);

            // Handle text files - they need double base64 encoding
            string extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
            if (extension == "txt")
            {
                // Double encode text files as per Czech Data Box requirements
                string base64Once = Convert.ToBase64String(content);
                content = System.Text.Encoding.UTF8.GetBytes(base64Once);
            }

            return new DataBoxAttachment(fileName, content, mimeType);
        }

        /// <summary>
        /// Save attachment to file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filePath, Content);
        }

        /// <summary>
        /// Get content as base64 string
        /// </summary>
        public string GetContentAsBase64()
        {
            return Convert.ToBase64String(Content);
        }
    }
}

