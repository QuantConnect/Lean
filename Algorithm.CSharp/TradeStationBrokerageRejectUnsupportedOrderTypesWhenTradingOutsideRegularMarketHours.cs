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

using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities;
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
    public class TradeStationBrokerageRejectUnsupportedOrderTypesWhenTradingOutsideRegularMarketHours : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Equity SPY;

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

            SetSecurityInitializer(new BrokerageModelSecurityInitializer(new TradeStationBrokerageModel(AccountType.Margin), new FuncSecuritySeeder(GetLastKnownPrices)));

            SPY = AddEquity("SPY", Resolution.Minute, extendedMarketHours: true);

            Schedule.On(DateRules.EveryDay(), TimeRules.At(3, 59), PlaceOrder);
        }

        private void PlaceOrder()
        {
            // Only test 1 invalid order type as we assume that the .Contains operation is correct
            // and that the other order types are also rejected.
            StopLimitOrder(SPY.Symbol, 5, 200m, 201m, orderProperties: _tradeStationOrderProperties);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {

        }

        /// <summary>
        /// Order events are triggered on order status changes. There are many order events including non-fill messages.
        /// </summary>
        /// <param name="orderEvent">OrderEvent object with details about the order status</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Invalid)
            {
                throw new RegressionTestException("Order processed during market hours.");
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
        public int AlgorithmHistoryDataPoints => 9;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new()
        {
             {"Total Orders", "5"},
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
             {"Information Ratio", "-8.91"},
             {"Tracking Error", "0.223"},
             {"Treynor Ratio", "0"},
             {"Total Fees", "$0.00"},
             {"Estimated Strategy Capacity", "$0"},
             {"Lowest Capacity Asset", ""},
             {"Portfolio Turnover", "0%"},
             {"OrderListHash", "eea64ec836abb8ffe84f58cc75aac980"}
         };
    }
}
