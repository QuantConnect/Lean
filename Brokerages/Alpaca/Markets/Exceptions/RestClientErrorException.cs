/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Updated to: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes:
 *   * Removed shorthand definitions for constructors using => ...
*/

using System;
using System.Net.Http;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Represents Alpaca/Polygon REST API specific error information.
    /// </summary>
    [Serializable]
    public sealed class RestClientErrorException : Exception
    {
        /// <summary>
        /// Creates new instance of <see cref="RestClientErrorException"/> class.
        /// </summary>
        public RestClientErrorException()
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="RestClientErrorException"/> class with specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RestClientErrorException(
            String message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="RestClientErrorException"/> class with
        /// specified error message and reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The  exception that is the cause of this exception.</param>
        public RestClientErrorException(
            String message,
            Exception inner)
            : base(message, inner)
        {
        }

#if !NETSTANDARD1_3
        private RestClientErrorException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
#endif

        internal RestClientErrorException(
            JsonError error)
            : base(error.Message)
        {
            ErrorCode = error.Code;
        }

        internal RestClientErrorException(
            HttpResponseMessage message)
            : base(message.ReasonPhrase ?? String.Empty)
        {
            ErrorCode = (Int32)message.StatusCode;
        }

        /// <summary>
        /// Original error code returned by REST API endpoint.
        /// </summary>
        public Int32 ErrorCode { get; }
    }
}
