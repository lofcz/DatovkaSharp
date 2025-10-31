using System;

namespace DatovkaSharp.Exceptions
{
    /// <summary>
    /// Exception thrown when there is a connection error with the ISDS service.
    /// </summary>
    public class ConnectionException : DataBoxException
    {
        /// <summary>
        /// Initializes a new instance of the ConnectionException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConnectionException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ConnectionException(string message, Exception? innerException)
            : base(message, innerException ?? new Exception("No exception provided"))
        {
        }
    }
}

