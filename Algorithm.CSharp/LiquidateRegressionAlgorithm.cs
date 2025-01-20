using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class LiquidateRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected Symbol _spy;
        protected Symbol _ibm;
        public override void Initialize()
        {
            SetStartDate(2018, 1, 5);
            SetEndDate(2018, 1, 10);
            SetCash(100000);
            _spy = AddEquity("SPY", Resolution.Daily).Symbol;
            _ibm = Symbol("IBM R735QTJ8XC9X");
            var security = AddSecurity(_ibm, Resolution.Daily);

            // Schedule Rebalance method to be called on specific dates
            Schedule.On(DateRules.On(2018, 1, 5), TimeRules.Midnight, Rebalance);
            Schedule.On(DateRules.On(2018, 1, 8), TimeRules.Midnight, Rebalance);
        }

        public virtual void Rebalance()
        {
            // Place a MarketOrder
            MarketOrder(_ibm, 10);

            // Place a LimitOrder to sell 1 share at a price below the current market price
            LimitOrder(_ibm, 1, Securities[_ibm].Price - 5);
            LimitOrder(_spy, 1, Securities[_spy].Price - 5);

            // Liquidate all holdings immediately
            PerformLiquidation();
        }

        public virtual void PerformLiquidation()
        {
            Liquidate();
        }

        public override void OnEndOfAlgorithm()
        {
            // Check if there are any orders that should have been canceled
            var orders = Transactions.GetOrders().ToList();
            var cnt = orders.Where(e => e.Status != OrderStatus.Canceled).Count();
            if (cnt > 0)
            {
                throw new RegressionTestException($"There are {cnt} orders that should have been cancelled");
            }

            // Check if there are any holdings left in the portfolio
            foreach (var kvp in Portfolio)
            {
                var symbol = kvp.Key;
                var holdings = kvp.Value;
                if (holdings.Quantity != 0)
                {
                    throw new RegressionTestException("There are holdings in portfolio");
                }
            }
        }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 44;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-5.634"},
            {"Tracking Error", "0.024"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "3dc667d309559a7df141959a22aef64c"}
        };
    }
}
