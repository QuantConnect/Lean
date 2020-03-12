using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class PaperEnvironment : IEnvironment
    {
        public Uri AlpacaTradingApi { get; } = new Uri("https://paper-api.alpaca.markets");

        public Uri AlpacaDataApi => Environments.Live.AlpacaDataApi;

        public Uri PolygonDataApi { get { throw new InvalidOperationException("Polygon.io REST API does not available on this environment."); } }

        public Uri AlpacaStreamingApi { get; } = new Uri("wss://paper-api.alpaca.markets/stream");

        public Uri PolygonStreamingApi { get { throw new InvalidOperationException("Polygon.io streaming API is not available on this environment."); } }
    }
}
