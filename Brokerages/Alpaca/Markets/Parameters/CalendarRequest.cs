/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates request parameters for <see cref="AlpacaTradingClient.ListCalendarAsync(CalendarRequest,System.Threading.CancellationToken)"/> call.
    /// </summary>
    public sealed class CalendarRequest : Validation.IRequest
    {
        /// <summary>
        /// Gets start time for filtering (inclusive).
        /// </summary>
        public DateTime? StartDateInclusive { get; private set; }

        /// <summary>
        /// Gets end time for filtering (inclusive).
        /// </summary>
        public DateTime? EndDateInclusive { get; private set; }

        /// <summary>
        /// Sets exclusive time interval for request (start/end time included into interval if specified).
        /// </summary>
        /// <param name="start">Filtering interval start time.</param>
        /// <param name="end">Filtering interval end time.</param>
        /// <returns>Fluent interface method return same <see cref="CalendarRequest"/> instance.</returns>
        public CalendarRequest SetInclusiveTimeInterval(
            DateTime? start,
            DateTime? end)
        {
            StartDateInclusive = start;
            EndDateInclusive = end;
            return this;
        }

        IEnumerable<RequestValidationException> Validation.IRequest.GetExceptions()
        {
            if (EndDateInclusive > StartDateInclusive)
            {
                yield return new RequestValidationException(
                    "Time interval should be valid.", nameof(StartDateInclusive));
                yield return new RequestValidationException(
                    "Time interval should be valid.", nameof(EndDateInclusive));
            }
        }
    }
}
