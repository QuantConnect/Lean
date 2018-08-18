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