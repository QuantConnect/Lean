using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{
    public class BitfinexOrder : MarketOrder
    {

        public decimal ExecutedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal OriginalAmount { get; set; }

    }
}
