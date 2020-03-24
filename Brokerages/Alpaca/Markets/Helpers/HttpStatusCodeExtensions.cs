/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Net;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class HttpStatusCodeExtensions
    {
        public static Boolean IsSuccessHttpStatusCode(
            this HttpStatusCode httpStatusCode) =>
            httpStatusCode >= HttpStatusCode.OK &&
            httpStatusCode < HttpStatusCode.Ambiguous;

        public static Boolean IsSuccessHttpStatusCode(
            this Int64 httpStatusCode) =>
            IsSuccessHttpStatusCode((HttpStatusCode) httpStatusCode);
    }
}
