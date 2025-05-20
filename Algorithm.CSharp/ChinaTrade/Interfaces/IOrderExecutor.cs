using System.Collections.Generic;
using System.Threading.Tasks;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces
{
    public interface IOrderExecutor
    {
        /// <summary>
        /// 执行合并后的信号
        /// </summary>
        Task ExecuteSignals(IEnumerable<TradingSignal> signals, IEnumerable<RiskSignal> risks);
    }
}