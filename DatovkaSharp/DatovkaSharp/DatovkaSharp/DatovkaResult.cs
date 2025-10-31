using System;

namespace DatovkaSharp
{
    /// <summary>
    /// Represents a unified result wrapper for all Datovka API operations.
    /// Provides access to the typed data, status information, and raw response.
    /// </summary>
    /// <typeparam name="T">The type of data returned by the operation.</typeparam>
    public class DatovkaResult<T>
    {
        /// <summary>
        /// Gets or sets the typed data returned by the operation.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Gets or sets the status code returned by the ISDS service.
        /// "0000" indicates success.
        /// </summary>
        public string StatusCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable status message.
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool IsSuccess => StatusCode == "0000";

        /// <summary>
        /// Gets or sets the raw SOAP response object for debugging purposes.
        /// </summary>
        public object? RawResponse { get; set; }

        /// <summary>
        /// Initializes a new instance of the DatovkaResult class.
        /// </summary>
        public DatovkaResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DatovkaResult class with specified values.
        /// </summary>
        public DatovkaResult(T? data, string statusCode, string statusMessage, object? rawResponse = null)
        {
            Data = data;
            StatusCode = statusCode;
            StatusMessage = statusMessage;
            RawResponse = rawResponse;
        }

        /// <summary>
        /// Creates a successful result with the specified data.
        /// </summary>
        public static DatovkaResult<T> Success(T data, object? rawResponse = null)
        {
            return new DatovkaResult<T>
            {
                Data = data,
                StatusCode = "0000",
                StatusMessage = "Operation completed successfully",
                RawResponse = rawResponse
            };
        }

        /// <summary>
        /// Creates a successful result with the specified data and status information.
        /// </summary>
        public static DatovkaResult<T> Success(T data, string statusCode, string statusMessage, object? rawResponse = null)
        {
            return new DatovkaResult<T>
            {
                Data = data,
                StatusCode = statusCode,
                StatusMessage = statusMessage,
                RawResponse = rawResponse
            };
        }

        /// <summary>
        /// Creates a failed result with the specified error information.
        /// </summary>
        public static DatovkaResult<T> Failure(string statusCode, string statusMessage, object? rawResponse = null)
        {
            return new DatovkaResult<T>
            {
                Data = default,
                StatusCode = statusCode,
                StatusMessage = statusMessage,
                RawResponse = rawResponse
            };
        }

        /// <summary>
        /// Creates a failed result from an exception.
        /// </summary>
        public static DatovkaResult<T> FromException(Exception exception)
        {
            return new DatovkaResult<T>
            {
                Data = default,
                StatusCode = "9999",
                StatusMessage = exception.Message,
                RawResponse = exception
            };
        }

        /// <summary>
        /// Throws a DataBoxException if the result is not successful.
        /// </summary>
        public DatovkaResult<T> ThrowIfFailed()
        {
            if (!IsSuccess)
            {
                throw new DataBoxException($"Operation failed with status {StatusCode}: {StatusMessage}");
            }
            return this;
        }

        /// <summary>
        /// Returns the data if successful, otherwise returns the default value.
        /// </summary>
        public T? OrDefault(T? defaultValue = default)
        {
            return IsSuccess ? Data : defaultValue;
        }
    }
}

