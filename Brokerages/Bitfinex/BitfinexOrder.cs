using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Bitfinex order object with additional fields
    /// </summary>
    public class BitfinexOrder : MarketOrder
    {

        /// <summary>
        /// Amount executed
        /// </summary>
        public decimal ExecutedAmount { get; set; }

        /// <summary>
        /// Amount left to be executed
        /// </summary>
        public decimal RemainingAmount { get; set; }

        /// <summary>
        /// Full amount of trade
        /// </summary>
        public decimal OriginalAmount { get; set; }

    }
}
