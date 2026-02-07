/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using Newtonsoft.Json;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.WebSocket
{

    public class WebSocketResponse
    {
        [JsonProperty("header")]
        public WebSocketHeader Header { get; }

        [JsonProperty("contract")]
        public WebSocketContract? Contract { get; }

        [JsonProperty("trade")]
        public WebSocketTrade? Trade { get; }

        [JsonProperty("quote")]
        public WebSocketQuote? Quote { get; }

        public WebSocketResponse(WebSocketHeader header, WebSocketContract? contract, WebSocketTrade? trade, WebSocketQuote? quote)
        {
            Header = header;
            Contract = contract;
            Trade = trade;
            Quote = quote;

        }
    }
}
