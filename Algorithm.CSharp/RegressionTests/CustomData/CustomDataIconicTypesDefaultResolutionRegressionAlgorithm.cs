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
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests the performance related GH issue 3772
    /// </summary>
    public class CustomDataIconicTypesDefaultResolutionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 11);
            SetEndDate(2013, 10, 12);
            var spy = AddEquity("SPY").Symbol;

            var types = new[]
            {
                typeof(UnlinkedDataTradeBar),
                typeof(DailyUnlinkedData),
                typeof(DailyLinkedData)
            };

            foreach (var type in types)
            {
                var custom = AddData(type, spy);

                if (SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(custom.Symbol)
                    .Any(config => config.Resolution != Resolution.Daily))
                {
                    throw new RegressionTestException("Was expecting resolution to be set to Daily");
                }

                try
                {
                    AddData(type, spy, Resolution.Tick);
                    throw new RegressionTestException("Was expecting an ArgumentException to be thrown");
                }
                catch (ArgumentException)
                {
                    // expected, these custom types don't support tick resolution
                }
            }

            var security = AddData<HourlyDefaultResolutionUnlinkedData>(spy);
            if (SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(security.Symbol)
                .Any(config => config.Resolution != Resolution.Hour))
            {
                throw new RegressionTestException("Was expecting resolution to be set to Hour");
            }

            var option = AddOption("AAPL");
            if (SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(option.Symbol)
                .Any(config => config.Resolution != Resolution.Daily))
            {
                throw new RegressionTestException("Was expecting resolution to be set to Daily");
            }
        }

        private class DailyUnlinkedData : UnlinkedData
        {
            public override List<Resolution> SupportedResolutions()
            {
                return DailyResolution;
            }
        }

        private class DailyLinkedData : LinkedData
        {
            public override List<Resolution> SupportedResolutions()
            {
                return DailyResolution;
            }
        }

        private class HourlyDefaultResolutionUnlinkedData : UnlinkedData
        {
            public override Resolution DefaultResolution()
            {
                return Resolution.Hour;
            }
        }

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
        public long DataPoints => 796;

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
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
