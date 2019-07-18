/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Represents Alpaca/Polygon REST API specific error information.
    /// </summary>
    internal sealed class RestClientErrorException : Exception
    {
        internal RestClientErrorException(
            JsonError error)
            : base(error.Message)
        {
            ErrorCode = error.Code;
        }

        /// <summary>
        /// Original error code returned by REST API endpoint.
        /// </summary>
        public Int32 ErrorCode { get; }
    }
}
