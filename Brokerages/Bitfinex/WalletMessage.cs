using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{
    public class WalletMessage : BaseMessage
    {


        public WalletMessage(string[] values) : base(values)
        {
            this.allKeys = new string[] { "WLT_NAME", "WLT_CURRENCY", "WLT_BALANCE", "WLT_INTEREST_UNSETTLED" };
        }

 		public string WLT_NAME { get; set; }
        public string WLT_CURRENCY { get; set; }
        public string WLT_BALANCE { get; set; }
        public string WLT_INTEREST_UNSETTLED { get; set; }


    }
}
