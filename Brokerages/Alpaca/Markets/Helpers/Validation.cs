/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class Validation
    {
        internal interface IRequest
        {
            /// <summary>
            /// Gets all validation exceptions (inconsistent request data errors).
            /// </summary>
            /// <returns>Lazy-evaluated list of validation errors.</returns>
            IEnumerable<RequestValidationException> GetExceptions();
        }

        public static void Validate<TRequest>(this TRequest request)
            where TRequest : class, IRequest
        {
            var exception = new AggregateException(request.GetExceptions());
            if (exception.InnerExceptions.Count != 0)
            {
                throw exception;
            }
        }
    }
}
