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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the option chain APIs return consistent values.
    /// See <see cref="QCAlgorithm.OptionChain(Symbol)"/> and <see cref="QCAlgorithm.OptionChainProvider"/>
    /// </summary>
    public class OptionChainApisConsistencyRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected virtual DateTime TestDate => new DateTime(2015, 12, 25);

        public override void Initialize()
        {
            SetStartDate(TestDate);
            SetEndDate(TestDate);

            var option = GetOption();

            var optionChainFromAlgorithmApi = OptionChain(option.Symbol).Contracts.Values.Select(x => x.Symbol).ToList();

            var exchangeTime = UtcTime.ConvertFromUtc(option.Exchange.TimeZone);
            var optionChainFromProviderApi = OptionChainProvider.GetOptionContractList(option.Symbol, exchangeTime).ToList();

            if (optionChainFromAlgorithmApi.Count == 0)
            {
                throw new RegressionTestException("No options in chain from algorithm API");
            }

            if (optionChainFromProviderApi.Count == 0)
            {
                throw new RegressionTestException("No options in chain from provider API");
            }

            if (optionChainFromAlgorithmApi.Count != optionChainFromProviderApi.Count)
            {
                throw new RegressionTestException($"Expected {optionChainFromProviderApi.Count} options in chain from provider API, " +
                    $"but got {optionChainFromAlgorithmApi.Count}");
            }

            for (var i = 0; i < optionChainFromAlgorithmApi.Count; i++)
            {
                var symbol1 = optionChainFromAlgorithmApi[i];
                var symbol2 = optionChainFromProviderApi[i];

                if (symbol1 != symbol2)
                {
                    throw new RegressionTestException($"Expected {symbol2} in chain from provider API, but got {symbol1}");
                }
            }
        }

        protected virtual Option GetOption()
        {
            return AddOption("GOOG");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 2021;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 2;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public virtual AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
