/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using System.Collections.Generic;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates extended market hours trading.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="assets" />
    /// <meta name="tag" content="regression test" />
    public class TradeStationBrokerageTradeWithOutsideRegularMarketHoursParameter : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Equity _spy;

        private readonly TradeStationOrderProperties _tradeStationOrderProperties = new() { OutsideRegularTradingHours = true };

        /// <summary>
        /// Initialize the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            SetBrokerageModel(BrokerageName.TradeStation, AccountType.Margin);

            _spy = AddEquity("SPY", Resolution.Minute, extendedMarketHours: true);

            Schedule.On(DateRules.EveryDay(), TimeRules.At(3, 50), () => StopLimitOrder(_spy.Symbol, 5, 200m, 201m, orderProperties: _tradeStationOrderProperties));
            Schedule.On(DateRules.EveryDay(), TimeRules.At(3, 55), () => LimitOrder(_spy.Symbol, 5, 200m, orderProperties: _tradeStationOrderProperties));
        }

        /// <summary>
        /// Order events are triggered on order status changes. There are many order events including non-fill messages.
        /// </summary>
        /// <param name="orderEvent">OrderEvent object with details about the order status</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            var isLimitOrder = order.Type == OrderType.Limit;

            if (orderEvent.Status == OrderStatus.Invalid && isLimitOrder)
            {
                throw new RegressionTestException("Limit order was incorrectly rejected during extended market hours.");
            }

            if (orderEvent.Status != OrderStatus.Invalid && !isLimitOrder)
            {
                throw new RegressionTestException("Non-limit order was unexpectedly processed outside regular market hours.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = [Language.CSharp];

        /// <summary>
        /// Data Points count of all TimeSlices of algorithm
        /// </summary>
        public long DataPoints => 9643;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "8"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "4.287%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100055.60"},
            {"Net Profit", "0.056%"},
            {"Sharpe Ratio", "8.327"},
            {"Sortino Ratio", "59.174"},
            {"Probabilistic Sharpe Ratio", "97.096%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.001"},
            {"Beta", "0.014"},
            {"Annual Standard Deviation", "0.003"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.872"},
            {"Tracking Error", "0.219"},
            {"Treynor Ratio", "2.053"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$6300000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.58%"},
            {"OrderListHash", "17daf701f7408999f77a3afe125aa175"}
        };
    }
}
