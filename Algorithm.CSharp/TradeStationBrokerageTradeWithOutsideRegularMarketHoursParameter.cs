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

            SetSecurityInitializer(new BrokerageModelSecurityInitializer(new TradeStationBrokerageModel(AccountType.Margin), new FuncSecuritySeeder(GetLastKnownPrices)));

            _spy = AddEquity("SPY", Resolution.Minute, extendedMarketHours: true);

            Schedule.On(DateRules.EveryDay(), TimeRules.At(3, 50), () => StopLimitOrder(_spy.Symbol, 5, 200m, 201m, orderProperties: _tradeStationOrderProperties));
            Schedule.On(DateRules.EveryDay(), TimeRules.At(3, 55), () => LimitOrder(_spy.Symbol, 5, 200m, orderProperties: _tradeStationOrderProperties));
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
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            switch (order.Type)
            {
                case OrderType.Limit:
                    // Limit orders are allowed during extended market hours.
                    break;
                default:
                    // For non-limit orders, ensure they are not processed during extended hours.
                    if (orderEvent.Status != OrderStatus.Invalid)
                    {
                        throw new RegressionTestException("Unexpected order processing: Non-limit orders should not be accepted outside regular market hours.");
                    }
                    break;
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
             {"Total Orders", "10"},
             {"Average Win", "0%"},
             {"Average Loss", "0%"},
             {"Compounding Annual Return", "5.312%"},
             {"Drawdown", "0.000%"},
             {"Expectancy", "0"},
             {"Start Equity", "100000"},
             {"End Equity", "100068.56"},
             {"Net Profit", "0.069%"},
             {"Sharpe Ratio", "7.867"},
             {"Sortino Ratio", "36.258"},
             {"Probabilistic Sharpe Ratio", "90.614%"},
             {"Loss Rate", "0%"},
             {"Win Rate", "0%"},
             {"Profit-Loss Ratio", "0"},
             {"Alpha", "-0.002"},
             {"Beta", "0.02"},
             {"Annual Standard Deviation", "0.005"},
             {"Annual Variance", "0"},
             {"Information Ratio", "-8.884"},
             {"Tracking Error", "0.218"},
             {"Treynor Ratio", "1.89"},
             {"Total Fees", "$0.00"},
             {"Estimated Strategy Capacity", "$5100000.00"},
             {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
             {"Portfolio Turnover", "0.72%"},
             {"OrderListHash", "ce1d19cdc47506adaa4e6a946fb1a9d9"}
         };
    }
}
