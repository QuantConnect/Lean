using System.Collections.Generic;
using System.Threading.Tasks;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Orders
{
    public class OrderExecutor : IOrderExecutor
    {
        private readonly QCAlgorithm _algorithm;

        public OrderExecutor(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        public async Task ExecuteSignals(IEnumerable<TradingSignal> signals, IEnumerable<RiskSignal> risks)
        {
            // 处理交易信号
            foreach (var signal in signals)
            {
                var quantity = _algorithm.CalculateOrderQuantity(signal.Symbol, 0.1m); // 10%仓位
                _algorithm.LimitOrder(signal.Symbol, quantity, signal.SuggestedPrice);
            }
            
            // 处理风控信号
            foreach (var risk in risks)
            {
                _algorithm.Liquidate(risk.Symbol, "风控触发");
            }
            
            await Task.CompletedTask;
        }
    }
}