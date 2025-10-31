using System;
using System.Linq;
using DatovkaSharp.Exceptions;
using DatovkaSharp.Services.Operations;

namespace DatovkaSharp
{
    /// <summary>
    /// Validates data messages before sending to ensure they meet ISDS requirements.
    /// </summary>
    public static class MessageValidator
    {
        /// <summary>
        /// Maximum number of recipients allowed for a single message.
        /// </summary>
        public const int MaxRecipients = 50;

        /// <summary>
        /// Maximum total size of all attachments in bytes (25 MB).
        /// </summary>
        public const long MaxTotalSizeBytes = 25 * 1024 * 1024;

        /// <summary>
        /// Validates a message before sending to ensure it meets all ISDS requirements.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        /// <exception cref="MissingRequiredFieldException">Thrown when required fields are missing.</exception>
        /// <exception cref="RecipientCountOverflowException">Thrown when recipient count exceeds maximum.</exception>
        /// <exception cref="FileSizeOverflowException">Thrown when total file size exceeds maximum.</exception>
        /// <exception cref="MissingMainFileException">Thrown when no attachments are present.</exception>
        public static void ValidateMessageForSending(tMessageCreateInput message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // 1. Validate envelope exists
            if (message.dmEnvelope == null)
                throw new MissingRequiredFieldException("dmEnvelope");

            // 2. Validate subject/annotation
            if (string.IsNullOrWhiteSpace(message.dmEnvelope.dmAnnotation))
                throw new MissingRequiredFieldException("annotation");

            // 3. Validate at least one recipient exists
            int recipientCount = GetRecipientCount(message);
            if (recipientCount < 1)
                throw new MissingRequiredFieldException("recipient");

            // 4. Validate recipient count doesn't exceed maximum
            if (recipientCount > MaxRecipients)
                throw new RecipientCountOverflowException(recipientCount, MaxRecipients);

            // 5. Validate at least one attachment exists (ISDS requirement)
            if (message.dmFiles == null || message.dmFiles.Length == 0)
                throw new MissingMainFileException("At least one attachment is required");

            // 6. Validate total file size doesn't exceed maximum
            long totalSize = CalculateTotalFileSize(message.dmFiles);
            if (totalSize > MaxTotalSizeBytes)
                throw new FileSizeOverflowException(totalSize, MaxTotalSizeBytes);
        }

        /// <summary>
        /// Gets the total number of recipients in the message.
        /// </summary>
        private static int GetRecipientCount(tMessageCreateInput message)
        {
            int count = 0;

            // Check primary recipient in envelope
            if (!string.IsNullOrWhiteSpace(message.dmEnvelope?.dbIDRecipient))
                count++;

            // Note: For multiple recipients, they would be in different structures
            // Current implementation supports single recipient in envelope
            // For bulk messages, additional logic would be needed

            return count;
        }

        /// <summary>
        /// Calculates the total size of all files in bytes.
        /// </summary>
        private static long CalculateTotalFileSize(tFilesArrayDmFile[]? files)
        {
            if (files == null || files.Length == 0)
                return 0;

            long totalSize = 0;
            foreach (var file in files)
            {
                if (file.Item is byte[] content)
                {
                    totalSize += content.Length;
                }
            }

            return totalSize;
        }

        /// <summary>
        /// Validates attachment file size before adding to message.
        /// </summary>
        /// <param name="currentTotalSize">Current total size of attachments.</param>
        /// <param name="newFileSize">Size of the new file to add.</param>
        /// <exception cref="FileSizeOverflowException">Thrown when adding the file would exceed maximum.</exception>
        public static void ValidateAddingFile(long currentTotalSize, long newFileSize)
        {
            long newTotalSize = currentTotalSize + newFileSize;
            if (newTotalSize > MaxTotalSizeBytes)
                throw new FileSizeOverflowException(newTotalSize, MaxTotalSizeBytes);
        }
    }
}

