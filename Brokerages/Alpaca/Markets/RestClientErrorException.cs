/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Represents Alpaca/Polygon REST API specific error information.
    /// </summary>
    public sealed class RestClientErrorException : Exception
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