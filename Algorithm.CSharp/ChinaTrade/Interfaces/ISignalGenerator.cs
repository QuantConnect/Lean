using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces
{
    public interface ISignalGenerator
    {
        /// <summary>
        /// 生成交易信号
        /// </summary>
        IEnumerable<TradingSignal> GenerateSignals(Slice data);
    }
    public class TradingSignal
    {
        public Symbol Symbol { get; set; }
        public OrderDirection Direction { get; set; } // Buy/Sell
        public decimal SuggestedPrice { get; set; }
        public DateTime SignalTime { get; set; }
    }
}