using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Ticker message object
    /// </summary>
    public class TickerMessage : BaseMessage
    {

        /// <summary>
        /// Ticker Message constructor
        /// </summary>
        /// <param name="values"></param>
        public TickerMessage(string[] values)
            : base(values)
        {
            allKeys = new string[] { "CHANNEL_ID", "BID", "BID_SIZE", "ASK", "ASK_SIZE", "DAILY_CHANGE", "DAILY_CHANGE_PERC", "LAST_PRICE", "VOLUME", "HIGH", "LOW" };

            CHANNEL_ID = GetInt("CHANNEL_ID");
            BID = GetDecimal("BID");
            BID_SIZE = GetDecimal("BID_SIZE");
            ASK = GetDecimal("ASK");
            ASK_SIZE = GetDecimal("ASK_SIZE");
            DAILY_CHANGE = GetDecimal("DAILY_CHANGE");
            DAILY_CHANGE_PERC = GetDecimal("DAILY_CHANGE_PERC");
            LAST_PRICE = GetDecimal("LAST_PRICE");
            VOLUME = GetDecimal("VOLUME");
            HIGH = GetDecimal("HIGH");
            LOW = GetDecimal("LOW");
        }

        /// <summary>
        /// Channel Id
        /// </summary>
        public int CHANNEL_ID { get; set; }
        /// <summary>
        /// Bid
        /// </summary>
        public decimal BID { get; set; }
        /// <summary>
        /// Bid Size
        /// </summary>
        public decimal BID_SIZE { get; set; }
        /// <summary>
        /// Ask
        /// </summary>
        public decimal ASK { get; set; }
        /// <summary>
        /// Ask Size
        /// </summary>
        public decimal ASK_SIZE { get; set; }
        /// <summary>
        /// Daily Change
        /// </summary>
        public decimal DAILY_CHANGE { get; set; }
        /// <summary>
        /// Daily Change %
        /// </summary>
        public decimal DAILY_CHANGE_PERC { get; set; }
        /// <summary>
        /// Last Price
        /// </summary>
        public decimal LAST_PRICE { get; set; }
        /// <summary>
        /// Volume
        /// </summary>
        public decimal VOLUME { get; set; }
        /// <summary>
        /// High
        /// </summary>
        public decimal HIGH { get; set; }
        /// <summary>
        /// Low
        /// </summary>
        public decimal LOW { get; set; }

    }
}
