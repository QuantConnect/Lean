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
    /// This regression algorithm has two different Universe using the same Security but with
    /// different SubscriptionDataConfig. One of them will add and remove it in a toggle fashion and it should also remove the
    /// corresponding SubscriptionDataConfig.
    /// Also will test manually adding and removing a security.
    /// </summary>
    /// <meta name="tag" content="regression test" />
    public class UniverseSharingSecurityDifferentSubscriptionRequestRegressionAlgorithm
        : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private readonly Symbol _aig = QuantConnect.Symbol.Create("AIG", SecurityType.Equity, Market.USA);
        private int _onDataCalls;
        private bool _alreadyRemoved;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000); //Set Strategy Cash

            AddEquity("SPY");
            AddEquity("AIG");

            UniverseSettings.Resolution = Resolution.Minute;
            UniverseSettings.ExtendedMarketHours = true;
            AddUniverse(SecurityType.Equity,
                "SecondUniverse",
                Resolution.Daily,
                Market.USA,
                UniverseSettings,
                time => time.Day % 2 == 0 ? new[] { "SPY" } : Enumerable.Empty<string>()
            );
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            _onDataCalls++;

            if (_alreadyRemoved)
            {
                var config = SubscriptionManager
                    .SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(_aig);
                if (config.Any())
                {
                    throw new Exception($"Unexpected SubscriptionDataConfig: {config}");
                }
            }

            if (!_alreadyRemoved)
            {
                _alreadyRemoved = true;
                var config = SubscriptionManager
                    .SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(_aig);
                if (!config.Any())
                {
                    throw new Exception("Expecting to find a SubscriptionDataConfig for AIG");
                }
                RemoveSecurity(_aig);
            }

            var isExtendedMarketHours = SubscriptionManager
                .SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(_spy)
                .IsExtendedMarketHours();

            if (Time.Day % 2 == 0)
            {
                if (!isExtendedMarketHours)
                {
                    throw new Exception($"Unexpected isExtendedMarketHours value: {false}");
                }
            }
            else
            {
                if (isExtendedMarketHours)
                {
                    throw new Exception($"Unexpected isExtendedMarketHours value: {true}");
                }

            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_onDataCalls == 0)
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
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-58.133"},
            {"Tracking Error", "0.173"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
