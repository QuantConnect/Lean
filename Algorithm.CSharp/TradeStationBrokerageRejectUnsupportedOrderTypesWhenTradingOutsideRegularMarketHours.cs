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

using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Brokerages;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates extended market hours trading.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="assets" />
    /// <meta name="tag" content="regression test" />
    public class TradeStationBrokerageRejectUnsupportedOrderTypesWhenTradingOutsideRegularMarketHours : QCAlgorithm
    {
        private Symbol _spy;

        private TradeStationOrderProperties tradeStationOrderProperties;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetBrokerageModel(BrokerageName.TradeStation, AccountType.Margin);
            var BrokerageModel = new TradeStationBrokerageModel(AccountType.Margin);

            tradeStationOrderProperties = new TradeStationOrderProperties();
            tradeStationOrderProperties.OutsideRegularTradingHours = true;
            //ensures security is warmed up and ready
            SetSecurityInitializer(new BrokerageModelSecurityInitializer(BrokerageModel, new FuncSecuritySeeder(GetLastKnownPrices)));
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            _spy = AddEquity("SPY", Resolution.Minute, extendedMarketHours: true).Symbol;


            // Schedule a task to place an order at 4:00 AM UTC

            var _tradeOnThisTime = (Time + TimeSpan.FromHours(4)).TimeOfDay;

            Debug($"Current Time: {Time}");
            Debug($"Trade on this time: {_tradeOnThisTime}");


            Schedule.On(DateRules.EveryDay(), TimeRules.At(_tradeOnThisTime), PlaceOrder);

        }

        private void PlaceOrder()
        {   
            // Only test 1 invalid order type as we assume that the .Contains operation is correct
            // and that the other order types are also rejected.
            StopLimitOrder(_spy, 5, 200, 201, orderProperties: tradeStationOrderProperties);

        }

        // /// <summary>
        // /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // /// </summary>
        // /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        // public override void OnData(Slice slice)
        // {
        //     //Only take an action once a day.
        //     if (_lastAction.Date == Time.Date) return;
        //     TradeBar spyBar = slice["SPY"];

        //     //If it isnt during market hours, go ahead and buy ten!
        //     if (!InMarketHours())
        //     {
        //         LimitOrder(_spy, 10, spyBar.Low);
        //         _lastAction = Time;
        //     }
        // }

        /// <summary>
        /// Order events are triggered on order status changes. There are many order events including non-fill messages.
        /// </summary>
        /// <param name="orderEvent">OrderEvent object with details about the order status</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            throw new RegressionTestException("Order processed during market hours.");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };


        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        // /// <summary>
        // /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        // /// </summary>
        // public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        // {
        //     {"Total Orders", "5"},
        //     {"Average Win", "0%"},
        //     {"Average Loss", "0%"},
        //     {"Compounding Annual Return", "10.774%"},
        //     {"Drawdown", "0.100%"},
        //     {"Expectancy", "0"},
        //     {"Start Equity", "100000"},
        //     {"End Equity", "100135.59"},
        //     {"Net Profit", "0.136%"},
        //     {"Sharpe Ratio", "8.723"},
        //     {"Sortino Ratio", "41.728"},
        //     {"Probabilistic Sharpe Ratio", "90.001%"},
        //     {"Loss Rate", "0%"},
        //     {"Win Rate", "0%"},
        //     {"Profit-Loss Ratio", "0"},
        //     {"Alpha", "0.005"},
        //     {"Beta", "0.039"},
        //     {"Annual Standard Deviation", "0.009"},
        //     {"Annual Variance", "0"},
        //     {"Information Ratio", "-8.852"},
        //     {"Tracking Error", "0.214"},
        //     {"Treynor Ratio", "2.102"},
        //     {"Total Fees", "$5.00"},
        //     {"Estimated Strategy Capacity", "$14000000.00"},
        //     {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
        //     {"Portfolio Turnover", "1.44%"},
        //     {"OrderListHash", "ac13139c0d75afb3d39a5143eb506658"}
        // };
    }
}
