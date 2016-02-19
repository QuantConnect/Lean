using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{
    public class TradeMessage : BaseMessage
    {

        public TradeMessage(string[] values)
            : base(values)
        {

            if (allValues.Length == 11)
            {
                allKeys = new string[] { "TRD_SEQ", "TRD_ID","TRD_PAIR","TRD_TIMESTAMP", "TRD_ORD_ID",  "TRD_AMOUNT_EXECUTED",
            "TRD_PRICE_EXECUTED", "ORD_TYPE", "ORD_PRICE", "FEE", "FEE_CURRENCY" };
            }
            else
            {
                allKeys = new string[] { "TRD_SEQ", "TRD_PAIR","TRD_TIMESTAMP", "TRD_ORD_ID",  "TRD_AMOUNT_EXECUTED",
            "TRD_PRICE_EXECUTED", "ORD_TYPE", "ORD_PRICE" };
            }



            TRD_SEQ = allValues[Array.IndexOf(allKeys, "TRD_SEQ")];
            TRD_PAIR = allValues[Array.IndexOf(allKeys, "TRD_PAIR")];
            TRD_TIMESTAMP = GetDateTime("TRD_TIMESTAMP");
            TRD_ORD_ID = GetInt("TRD_ORD_ID");
            TRD_AMOUNT_EXECUTED = GetDecimal("TRD_AMOUNT_EXECUTED");
            TRD_PRICE_EXECUTED = GetDecimal("TRD_PRICE_EXECUTED");
            ORD_TYPE = allValues[Array.IndexOf(allKeys, "ORD_TYPE")];
            ORD_PRICE = GetDecimal("ORD_PRICE");
            if (allValues.Length == 11)
            {
                TRD_ID = TryGetInt("TRD_ID");
                FEE = GetDecimalFromScientific("FEE");
                FEE_CURRENCY = allValues[Array.IndexOf(allKeys, "FEE_CURRENCY")];
            }
        }

        public string TRD_SEQ { get; set; }
        public int TRD_ID { get; set; }
        public string TRD_PAIR { get; set; }
        public DateTime TRD_TIMESTAMP { get; set; }
        public int TRD_ORD_ID { get; set; }
        public decimal TRD_AMOUNT_EXECUTED { get; set; }
        public decimal TRD_PRICE_EXECUTED { get; set; }
        public string ORD_TYPE { get; set; }
        public decimal ORD_PRICE { get; set; }
        public decimal FEE { get; set; }
        public string FEE_CURRENCY { get; set; }

    }
}
