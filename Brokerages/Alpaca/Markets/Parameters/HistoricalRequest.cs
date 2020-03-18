/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates request parameters for <see cref="PolygonDataClient.ListHistoricalTradesAsync(HistoricalRequest,System.Threading.CancellationToken)"/>
    /// and <see cref="PolygonDataClient.ListHistoricalQuotesAsync(HistoricalRequest,System.Threading.CancellationToken)"/> method calls.
    /// </summary>
    public sealed class HistoricalRequest : Validation.IRequest
    {
        /// <summary>
        /// Creates new instance of <see cref="HistoricalRequest"/> object.
        /// </summary>
        /// <param name="symbol">Asset name for data retrieval.</param>
        /// <param name="date">Single date for data retrieval.</param>
        public HistoricalRequest(
            String symbol,
            DateTime date)
        {
            Symbol = symbol;
            Date = date;

        }

        /// <summary>
        /// Gets asset name for data retrieval.
        /// </summary>
        public String Symbol { get; }

        /// <summary>
        /// Gets single date for data retrieval.
        /// </summary>
        public DateTime Date { get; }

        /// <summary>
        /// Gets or sets initial timestamp for request. Using the timestamp of the last result will give you the next page of results.
        /// </summary>
        public Int64? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets maximum timestamp allowed in the results.
        /// </summary>
        public Int64? TimestampLimit { get; set; }

        /// <summary>
        /// Gets or sets size (number of items) limits fore the response.
        /// </summary>
        public Int32? Limit { get; set; }

        /// <summary>
        /// Gets or sets flag that indicates reversed order of the results.
        /// </summary>
        public Boolean? Reverse { get; set; }

        IEnumerable<RequestValidationException> Validation.IRequest.GetExceptions()
        {
            if (String.IsNullOrEmpty(Symbol))
            {
                yield return new RequestValidationException(
                    "Symbols shouldn't be empty.", nameof(Symbol));
            }
        }
    }
}
