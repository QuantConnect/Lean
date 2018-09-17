using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Represents a full order book for a security.
    /// It contains prices and order sizes for each bid and ask level.
    /// The best bid and ask prices are also kept up to date.
    /// </summary>
    public class BinanceOrderBook: DefaultOrderBook
    {
        /// <summary>
        /// Last update event
        /// </summary>
        public long LastUpdateId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinanceOrderBook"/> class
        /// </summary>
        /// <param name="symbol">The symbol for the order book</param>
        public BinanceOrderBook(Symbol symbol): base(symbol) { }

        /// <summary>
        /// Clears all bid/ask levels and prices.
        /// </summary>
        public void Reset()
        {
            Clear();
            LastUpdateId = 0;
        }
    }
}
