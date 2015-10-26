using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Bar class for second, minute, hour and daily resolution data: 
    /// An OHLC implementation of the QuantConnect BaseData class with parameters for candles.
    /// </summary>
    public class Bar : BaseData
    {
        // scale factor used in QC data files
        public const decimal _scaleFactor = 10000m;

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// High price of the TradeBar during the time period.
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low price of the TradeBar during the time period.
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Closing price of the TradeBar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close
        {
            get { return Value; }
            set { Value = value; }
        }

        /// <summary>
        /// The closing time of Bar, computed via the Time and Period
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time + Period; }
            set { Period = value - Time; }
        }

        /// <summary>
        /// The period of this trade bar, (second, minute, daily, ect...)
        /// </summary>
        public TimeSpan Period { get; set; }

        //In Base Class: Alias of Closing:
        //public decimal Price;

        //Symbol of Asset.
        //In Base Class: public Symbol Symbol;

        //In Base Class: DateTime Of this TradeBar
        //public DateTime Time;

        /// <summary>
        /// Default initializer to setup an empty tradebar.
        /// </summary>
        public Bar()
        {
            Symbol = Symbol.Empty;
            Time = new DateTime();
            Open = 0; 
            High = 0;
            Low = 0; 
            Close = 0;
            Value = 0;
            Period = TimeSpan.FromMinutes(1);
            DataType = MarketDataType.Base;
        }

        /// <summary>
        /// Initialize Bar with OHLC values:
        /// </summary>
        /// <param name="time">DateTime Timestamp of the bar</param>
        /// <param name="symbol">Market MarketType Symbol</param>
        /// <param name="open">Decimal Opening Price</param>
        /// <param name="high">Decimal High Price of this bar</param>
        /// <param name="low">Decimal Low Price of this bar</param>
        /// <param name="close">Decimal Close price of this bar</param>
        /// <param name="period">The period of this bar, specify null for default of 1 minute</param>
        public Bar(DateTime time, Symbol symbol, decimal open, decimal high, decimal low, decimal close, TimeSpan? period = null)
        {
            Time = time;
            Symbol = symbol;
            Value = close;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Period = period ?? TimeSpan.FromMinutes(1);
            DataType = MarketDataType.Base;
        }
    }
}
