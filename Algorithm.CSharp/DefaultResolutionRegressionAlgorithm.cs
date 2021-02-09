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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Custom.CBOE;
using QuantConnect.Data.Custom.Fred;
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Data.Custom.USEnergy;
using QuantConnect.Data.Custom.USTreasury;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests the performance related GH issue 3772
    /// </summary>
    public class DefaultResolutionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 11);
            SetEndDate(2013, 10, 12);
            var spy = AddEquity("SPY").Symbol;

            var types = new[]
            {
                typeof(SECReport8K),
                typeof(SECReport10K),
                typeof(SECReport10Q),
                typeof(USTreasuryYieldCurveRate),
                typeof(USEnergy),
                typeof(CBOE),
                typeof(TiingoPrice),
                typeof(Fred)
            };

            foreach (var type in types)
            {
                var custom = AddData(type, spy);

                if (SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(custom.Symbol)
                    .Any(config => config.Resolution != Resolution.Daily))
                {
                    throw new Exception("Was expecting resolution to be set to Daily");
                }

                try
                {
                    AddData(type, spy, Resolution.Tick);
                    throw new Exception("Was expecting an ArgumentException to be thrown");
                }
                catch (ArgumentException)
                {
                    // expected, these custom types don't support tick resolution
                }
            }

            var security = AddData<USEnergyAPI>(spy);
            if (SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(security.Symbol)
                .Any(config => config.Resolution != Resolution.Hour))
            {
                throw new Exception("Was expecting resolution to be set to Hour");
            }

            try
            {

                AddOption("AAPL", Resolution.Daily);
                throw new Exception("Was expecting an ArgumentException to be thrown");
            }
            catch (ArgumentException)
            {
                // expected, options only support minute resolution
            }

            var option = AddOption("AAPL");
            if (SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(option.Symbol)
                .Any(config => config.Resolution != Resolution.Minute))
            {
                throw new Exception("Was expecting resolution to be set to Minute");
            }

            Quit();
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0"},
            {"Return Over Maximum Drawdown", "0"},
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
