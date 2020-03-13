/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class LiveEnvironment : IEnvironment
    {
        public Uri AlpacaTradingApi { get; } = new Uri("https://api.alpaca.markets");

        public Uri AlpacaDataApi { get; } = new Uri("https://data.alpaca.markets");

        public Uri PolygonDataApi { get; } = new Uri("https://api.polygon.io");

        public Uri AlpacaStreamingApi { get; } = new Uri("wss://api.alpaca.markets/stream");

        public Uri PolygonStreamingApi { get; } = new Uri("wss://alpaca.socket.polygon.io/stocks");
    }
}
