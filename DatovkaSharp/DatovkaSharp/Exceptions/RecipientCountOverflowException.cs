using System;

namespace DatovkaSharp.Exceptions
{
    /// <summary>
    /// Exception thrown when the number of recipients exceeds the maximum allowed count.
    /// </summary>
    public class RecipientCountOverflowException : DataBoxException
    {
        /// <summary>
        /// Gets the current number of recipients.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the maximum allowed number of recipients.
        /// </summary>
        public int MaxCount { get; }

        /// <summary>
        /// Initializes a new instance of the RecipientCountOverflowException class.
        /// </summary>
        /// <param name="count">The current number of recipients.</param>
        /// <param name="maxCount">The maximum allowed number of recipients.</param>
        public RecipientCountOverflowException(int count, int maxCount)
            : base($"Recipient count {count} exceeds maximum {maxCount}")
        {
            Count = count;
            MaxCount = maxCount;
        }
    }
}

