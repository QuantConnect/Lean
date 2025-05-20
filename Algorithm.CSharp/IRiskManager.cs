using System.Collections.Generic;
using QuantConnect.Algorithm;

namespace QuantConnect.Algorithm.CSharp
{
    public interface IRiskManager
    {
        /// <summary>
        /// 检查持仓风险
        /// </summary>
        IEnumerable<RiskSignal> CheckRisks(QuantConnect.Securities.SecurityPortfolioManager portfolio);
    }

    public class RiskSignal
    {
        public Symbol Symbol { get; set; }
        public RiskAction Action { get; set; }
        public decimal TriggerPrice { get; set; }
    }

    public enum RiskAction { StopLoss, TakeProfit }
}