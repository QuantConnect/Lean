using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class UpdateStopOrdersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private OrderTicket _ticket;

        public override void Initialize()
        {
            SetStartDate(2018, 4, 3);
            SetEndDate(2018, 4, 4);

            AddForex("EURUSD", Resolution.Minute);
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                var qty = CalculateOrderQuantity("EURUSD", 50m);

                MarketOrder("EURUSD", qty);

                _ticket = StopMarketOrder("EURUSD", -qty / 2, Securities["EURUSD"].Price - 0.003m);

                Log($"Before TotalMarginUsed: {Portfolio.TotalMarginUsed}");

                var updateSettings = new UpdateOrderFields
                {
                    Quantity = -qty * 10,
                    StopPrice = Securities["EURUSD"].Price - 0.003m
                };
                var response = _ticket.Update(updateSettings);

                if (response.IsSuccess)
                {
                    Log($"After TotalMarginUsed: {Portfolio.TotalMarginUsed}");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            if (order.Type == OrderType.StopMarket && orderEvent.Status == OrderStatus.Filled)
            {
                _ticket = null;
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
        public virtual List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2893;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 60;

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
            {"Start Equity", "100000.00"},
            {"End Equity", "91982.00"},
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$250000.00"},
            {"Lowest Capacity Asset", "EURUSD 8G"},
            {"Portfolio Turnover", "3074.60%"},
            {"OrderListHash", "373a8b2323b0fa4c15c80cd1abd25dd3"}
        };
    }
}
