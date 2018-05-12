using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Kraken.DataType {

    public class TradeInfo {
        /// <summary>
        /// Order responsible for execution of trade.
        /// </summary>
        public string OrderTxid;

        /// <summary>
        /// Asset pair.
        /// </summary>
        public string Pair;

        /// <summary>
        /// Unix timestamp of trade.
        /// </summary>
        public double Time;

        /// <summary>
        /// Type of order (buy/sell).
        /// </summary>
        public string Type;

        /// <summary>
        /// Order type.
        /// </summary>
        public string OrderType;

        /// <summary>
        /// Average price order was executed at (quote currency).
        /// </summary>
        public decimal Price;

        /// <summary>
        /// Total cost of order (quote currency).
        /// </summary>
        public decimal Cost;

        /// <summary>
        /// Total fee (quote currency).
        /// </summary>
        public decimal Fee;

        /// <summary>
        /// Volume (base currency).
        /// </summary>
        public decimal Vol;

        /// <summary>
        /// Initial margin (quote currency).
        /// </summary>
        public decimal Margin;

        /// <summary>
        /// Comma delimited list of miscellaneous info.
        /// closing = trade closes all or part of a position.
        /// </summary>
        public string Misc;

        /// <summary>
        /// Position status(open/closed).
        /// </summary>
        public string PosStatus;

        /// <summary>
        /// Average price of closed portion of position(quote currency).
        /// </summary>
        public decimal? CPrice;

        /// <summary>
        /// Total cost of closed portion of position(quote currency).
        /// </summary>
        public decimal? CCost;

        /// <summary>
        /// Total fee of closed portion of position(quote currency).
        /// </summary>
        public decimal? CFee;

        /// <summary>
        /// Total fee of closed portion of position(quote currency).
        /// </summary>
        public decimal? CVol;

        /// <summary>
        /// Total margin freed in closed portion of position(quote currency).
        /// </summary>
        public decimal? CMargin;

        /// <summary>
        /// Net profit/loss of closed portion of position(quote currency, quote currency scale).
        /// </summary>
        public decimal? Net;

        /// <summary>
        /// List of closing trades for position(if available).
        /// </summary>
        public string[] Trades;
    }

}
