/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Diagnostics.CodeAnalysis;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    [SuppressMessage(
        "Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
    internal sealed class WebSocketClientFactory : IWebSocketFactory
    {
        private sealed class AlpacaWebSocketClientWrapper : WebSocketClientWrapper
        {
            public AlpacaWebSocketClientWrapper(Uri url)
            {
                Initialize(url.ToString());
            }
        }

        public IWebSocket CreateWebSocket(Uri url) => new AlpacaWebSocketClientWrapper(url);
    }
}
