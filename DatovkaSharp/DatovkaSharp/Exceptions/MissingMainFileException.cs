using System;

namespace DatovkaSharp.Exceptions
{
    /// <summary>
    /// Exception thrown when a data message is being sent without any attachments.
    /// According to Czech Data Box regulations, at least one attachment is required.
    /// </summary>
    public class MissingMainFileException : DataBoxException
    {
        /// <summary>
        /// Initializes a new instance of the MissingMainFileException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public MissingMainFileException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MissingMainFileException class with a default message.
        /// </summary>
        public MissingMainFileException()
            : base("The message cannot be sent without at least one attachment")
        {
        }
    }
}

