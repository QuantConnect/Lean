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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "15"},
            {"Average Win", "0.68%"},
            {"Average Loss", "-0.14%"},
            {"Compounding Annual Return", "36.195%"},
            {"Drawdown", "1.000%"},
            {"Expectancy", "3.303"},
            {"Net Profit", "2.572%"},
            {"Sharpe Ratio", "3.185"},
            {"Loss Rate", "29%"},
            {"Win Rate", "71%"},
            {"Profit-Loss Ratio", "5.02"},
            {"Alpha", "0.036"},
            {"Beta", "0.444"},
            {"Annual Standard Deviation", "0.075"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-2.596"},
            {"Tracking Error", "0.084"},
            {"Treynor Ratio", "0.54"},
            {"Total Fees", "$48.51"}
        };
    }
}
