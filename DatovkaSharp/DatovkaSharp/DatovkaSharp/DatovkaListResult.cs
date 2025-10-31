using System.Collections.Generic;

namespace DatovkaSharp
{
    /// <summary>
    /// Represents a result wrapper specifically for list operations.
    /// Provides additional properties for working with collections.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    public class DatovkaListResult<T> : DatovkaResult<List<T>>
    {
        /// <summary>
        /// Gets the number of items in the result.
        /// </summary>
        public int Count => Data?.Count ?? 0;

        /// <summary>
        /// Gets a value indicating whether the result contains any items.
        /// </summary>
        public bool HasItems => Count > 0;

        /// <summary>
        /// Initializes a new instance of the DatovkaListResult class.
        /// </summary>
        public DatovkaListResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DatovkaListResult class with specified values.
        /// </summary>
        public DatovkaListResult(List<T>? data, string statusCode, string statusMessage, object? rawResponse = null)
            : base(data, statusCode, statusMessage, rawResponse)
        {
        }

        /// <summary>
        /// Creates a successful list result with the specified data.
        /// </summary>
        public new static DatovkaListResult<T> Success(List<T> data, object? rawResponse = null)
        {
            return new DatovkaListResult<T>
            {
                Data = data,
                StatusCode = "0000",
                StatusMessage = "Operation completed successfully",
                RawResponse = rawResponse
            };
        }

        /// <summary>
        /// Creates a successful list result with the specified data and status information.
        /// </summary>
        public new static DatovkaListResult<T> Success(List<T> data, string statusCode, string statusMessage, object? rawResponse = null)
        {
            return new DatovkaListResult<T>
            {
                Data = data,
                StatusCode = statusCode,
                StatusMessage = statusMessage,
                RawResponse = rawResponse
            };
        }

        /// <summary>
        /// Creates a failed list result with the specified error information.
        /// </summary>
        public new static DatovkaListResult<T> Failure(string statusCode, string statusMessage, object? rawResponse = null)
        {
            return new DatovkaListResult<T>
            {
                Data = [],
                StatusCode = statusCode,
                StatusMessage = statusMessage,
                RawResponse = rawResponse
            };
        }

        /// <summary>
        /// Creates a failed list result from an exception.
        /// </summary>
        public new static DatovkaListResult<T> FromException(System.Exception ex)
        {
            return new DatovkaListResult<T>
            {
                Data = [],
                StatusCode = "9999",
                StatusMessage = ex.Message,
                RawResponse = ex
            };
        }
    }
}

