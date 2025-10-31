using System;

namespace DatovkaSharp.Exceptions
{
    /// <summary>
    /// Exception thrown when a required field is missing or empty.
    /// </summary>
    public class MissingRequiredFieldException : DataBoxException
    {
        /// <summary>
        /// Gets the name of the missing required field.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Initializes a new instance of the MissingRequiredFieldException class.
        /// </summary>
        /// <param name="fieldName">The name of the missing required field.</param>
        public MissingRequiredFieldException(string fieldName)
            : base($"Required field '{fieldName}' is missing or empty")
        {
            FieldName = fieldName;
        }
    }
}

