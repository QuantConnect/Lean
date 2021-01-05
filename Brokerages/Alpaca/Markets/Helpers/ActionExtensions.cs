/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made from original:
 *   - Removed Nullable reference type definitions for compatibility with C# 6
*/

using System;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal static class ActionExtensions
    {
        public static void DeserializeAndInvoke<TApi, TJson>(
            this Action<TApi> eventHandler,
            JToken eventArg)
            where TJson : class, TApi
        {
            try
            {
                eventHandler?.Invoke(eventArg.ToObject<TJson>());
            }
            catch (Exception)
            {
                Log.Error($"Error deserializing JSON: {eventArg}");
                throw;
            }
        }
    }
}
