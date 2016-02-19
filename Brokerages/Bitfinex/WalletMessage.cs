using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Wallet update message object
    /// </summary>
    public class WalletMessage : BaseMessage
    {

        /// <summary>
        /// Constructor for Wallet Message
        /// </summary>
        /// <param name="values"></param>
        public WalletMessage(string[] values) : base(values)
        {
            this.allKeys = new string[] { "WLT_NAME", "WLT_CURRENCY", "WLT_BALANCE", "WLT_INTEREST_UNSETTLED" };

            WLT_CURRENCY = allValues[Array.IndexOf(allKeys, "WLT_CURRENCY")];
            WLT_BALANCE = GetDecimal("WLT_BALANCE");
        }

        /// <summary>
        /// Wallet Name
        /// </summary>
 		public string WLT_NAME { get; set; }
        /// <summary>
        /// Wallet Currency
        /// </summary>
        public string WLT_CURRENCY { get; set; }
        /// <summary>
        /// Wallet Balance
        /// </summary>
        public decimal WLT_BALANCE { get; set; }
        /// <summary>
        /// Wallet Interest Unsettled
        /// </summary>
        public string WLT_INTEREST_UNSETTLED { get; set; }


    }
}
