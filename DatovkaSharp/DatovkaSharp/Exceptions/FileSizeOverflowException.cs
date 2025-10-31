using System;

namespace DatovkaSharp.Exceptions
{
    /// <summary>
    /// Exception thrown when the total size of message attachments exceeds the maximum allowed size.
    /// </summary>
    public class FileSizeOverflowException : DataBoxException
    {
        /// <summary>
        /// Gets the current total size of attachments in bytes.
        /// </summary>
        public long CurrentSize { get; }

        /// <summary>
        /// Gets the maximum allowed size in bytes.
        /// </summary>
        public long MaxSize { get; }

        /// <summary>
        /// Initializes a new instance of the FileSizeOverflowException class.
        /// </summary>
        /// <param name="currentSize">The current total size of attachments in bytes.</param>
        /// <param name="maxSize">The maximum allowed size in bytes.</param>
        public FileSizeOverflowException(long currentSize, long maxSize)
            : base($"Total size {FormatBytes(currentSize)} exceeds maximum {FormatBytes(maxSize)}")
        {
            CurrentSize = currentSize;
            MaxSize = maxSize;
        }

        /// <summary>
        /// Formats bytes into a human-readable string.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} bytes";
        }
    }
}

