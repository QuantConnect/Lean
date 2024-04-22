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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm has two different Universe using the same SubscriptionDataConfig.
    /// One of them will add and remove it in a toggle fashion but since it will still be consumed
    /// by the other Universe it should not be removed.
    /// </summary>
    /// <meta name="tag" content="regression test" />
    public class UniverseSharingSubscriptionRequestRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        private int _onDataCalls;
        private bool _restOneDay;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 01); //Set Start Date
            SetEndDate(2013, 10, 30); //Set End Date
            SetCash(100000); //Set Strategy Cash

            AddEquity("SPY", Resolution.Daily);

            UniverseSettings.Resolution = Resolution.Daily;
            AddUniverse(SecurityType.Equity,
                "SecondUniverse",
                Resolution.Daily,
                Market.USA,
                UniverseSettings,
                time => time.Day % 3 == 0 ? new[] { "SPY" } : Enumerable.Empty<string>()
            );
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (data.Count != 1)
            {
                throw new Exception($"Unexpected data count {data.Count}");
            }
            Debug($"{data.Time}. Data count {data.Count}. Data {data.Bars.First().Value}");
            _onDataCalls++;

            if (_restOneDay)
            {
                // let a day pass before trading again, this will cause
                // "SecondUniverse" remove request to be applied
                _restOneDay = false;
            }
            else if(!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
                Debug("Purchased Stock");
            }
            else
            {
                SetHoldings(_spy, 0);
                Debug("Sell Stock");
                _restOneDay = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_onDataCalls != 23)
            {
                throw new Exception($"Unexpected OnData() calls count {_onDataCalls}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 207;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "16"},
            {"Average Win", "0.68%"},
            {"Average Loss", "-0.14%"},
            {"Compounding Annual Return", "35.512%"},
            {"Drawdown", "1.000%"},
            {"Expectancy", "3.194"},
            {"Start Equity", "100000"},
            {"End Equity", "102529.20"},
            {"Net Profit", "2.529%"},
            {"Sharpe Ratio", "2.251"},
            {"Sortino Ratio", "3.431"},
            {"Probabilistic Sharpe Ratio", "66.065%"},
            {"Loss Rate", "29%"},
            {"Win Rate", "71%"},
            {"Profit-Loss Ratio", "4.87"},
            {"Alpha", "-0.022"},
            {"Beta", "0.439"},
            {"Annual Standard Deviation", "0.071"},
            {"Annual Variance", "0.005"},
            {"Information Ratio", "-3.173"},
            {"Tracking Error", "0.081"},
            {"Treynor Ratio", "0.366"},
            {"Total Fees", "$51.26"},
            {"Estimated Strategy Capacity", "$800000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "49.71%"},
            {"OrderListHash", "139fb59c53a61fa5543797057d86937d"}
        };
    }
}
