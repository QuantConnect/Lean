using System.Collections.Generic;
using QuantConnect.Algorithm;
using QuantConnect.Api;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class RiskManager : IRiskManager
    {
        private readonly QCAlgorithm _algorithm;
        private const decimal StopLossPercent = 0.05m;
        private const decimal TakeProfitPercent = 0.10m;

        public RiskManager(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }
        public IEnumerable<RiskSignal> CheckRisks(QuantConnect.Securities.SecurityPortfolioManager portfolio)
        {
            var risks = new List<RiskSignal>();
            // 遍历投资组合中的持仓
            foreach (var holding in portfolio.Values)
            {
                var symbol = holding.Symbol;
                var currentPrice = portfolio.Securities[symbol].Price;
                // 检查平均价格是否为零，避免除零错误
                var profitRatio = holding.AveragePrice != 0 
                    ? (currentPrice - holding.AveragePrice) / holding.AveragePrice 
                    : 0;
                if (profitRatio <= -StopLossPercent)
                {
                    risks.Add(new RiskSignal {
                        Symbol = symbol,
                        Action = RiskAction.StopLoss,
                        TriggerPrice = currentPrice
                    });
                }
                else if (profitRatio >= TakeProfitPercent)
                {
                    risks.Add(new RiskSignal {
                        Symbol = symbol,
                        Action = RiskAction.TakeProfit,
                        TriggerPrice = currentPrice
                    });
                }
            }
            
            return risks;
        }
    }
}