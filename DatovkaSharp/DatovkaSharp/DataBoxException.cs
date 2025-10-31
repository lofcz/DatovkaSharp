using System;

namespace DatovkaSharp
{
    /// <summary>
    /// Exception thrown by Czech Data Box operations
    /// </summary>
    public class DataBoxException : Exception
    {
        public DataBoxException()
        {
        }

        public DataBoxException(string message) : base(message)
        {
        }

        public DataBoxException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

