using System;
using Newtonsoft.Json.Linq;

namespace QuantConnect.ToolBox.PolygonDownloader
{
    /// <summary>
    /// Response structrue received from 'Historic Trades' endpoint
    /// https://polygon.io/docs/#!/Stocks--Equities/get_v1_historic_trades_symbol_date
    /// </summary>
    [Serializable]
    public class EquityHistoricTradesResponseTexture
    {
        /// <summary>
        /// Day of historic trade request
        /// </summary>
        public DateTime Day { get; set; }

        /// <summary>
        /// Response map of trade object <see cref="EquityTradeTexture"/>
        /// </summary>
        public JToken Map { get; set; }

        /// <summary>
        /// Latency
        /// </summary>
        public int Latency { get; set; }

        /// <summary>
        /// Response status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Symbol string
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Historic trades array
        /// </summary>
        public EquityTradeTexture[] Ticks { get; set; }

        /// <summary>
        /// Ticks type - 'trades'
        /// </summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// A trade structure returned as part of response from 'Historic Trades' or "Last Trade for a Symbol' endp.
    /// https://polygon.io/docs/#!/Stocks--Equities/get_v1_historic_trades_symbol_date
    /// https://polygon.io/docs/#!/Stocks--Equities/get_v1_last_stocks_symbol
    /// </summary>
    [Serializable]
    public class EquityTradeTexture
    {
        /// <summary>
        /// Condition 1
        /// </summary>
        public int C1 { get; set; }

        /// <summary>
        /// Condition 2
        /// </summary>
        public int C2 { get; set; }

        /// <summary>
        /// Condition 3
        /// </summary>
        public int C3 { get; set; }

        /// <summary>
        /// Condition 4
        /// </summary>
        public int C4 { get; set; }

        /// <summary>
        /// Exchange
        /// </summary>
        public int E { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal P { get; set; }

        /// <summary>
        /// Size
        /// </summary>
        public int S { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public long T { get; set; }
    }

    /// <summary>
    /// Json structure of a single exchange record returned by API.
    /// </summary>
    [Serializable]
    public class ExchangeTexture
    {
        /// <summary>
        /// Exchange id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Exchange organization type -  exchange/ TRF.
        /// </summary>
        public string Infrastructure { get; set; }

        /// <summary>
        /// Market type - equities/ index/ etc.
        /// </summary>
        public string Market { get; set; }

        /// <summary>
        /// Exchange Market Identifier Code
        /// </summary>
        public string Mic { get; set; }

        /// <summary>
        /// Full name of exchange
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Very short - one letter - denomination of the exchange
        /// </summary>
        public string Tape { get; set; }

        /// <summary>
        /// Exchange code
        /// </summary>
        public string Code { get; set; }
    }
}
