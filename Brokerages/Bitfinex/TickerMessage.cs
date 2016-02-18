using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{
    public class TickerMessage : BaseMessage
    {

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

        public int CHANNEL_ID { get; set; }
        public decimal BID { get; set; }
        public decimal BID_SIZE { get; set; }
        public decimal ASK { get; set; }
        public decimal ASK_SIZE { get; set; }
        public decimal DAILY_CHANGE { get; set; }
        public decimal DAILY_CHANGE_PERC { get; set; }
        public decimal LAST_PRICE { get; set; }
        public decimal VOLUME { get; set; }
        public decimal HIGH { get; set; }
        public decimal LOW { get; set; }

    }
}
