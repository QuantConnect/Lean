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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting AddFutureOptionContract does not throw even when the underlying security configurations are internal
    /// </summary>
    public class AddFutureOptionContractWithInternalMappedUnderlyingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Option _fopContract;

        public override void Initialize()
        {
            SetStartDate(2020, 01, 04);
            SetEndDate(2020, 01 , 06);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.LastTradingDay,
                contractDepthOffset: 0);
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.AddedSecurities.Any(security => security.Symbol == _continuousContract.Symbol))
            {
                if (SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(_continuousContract.Mapped).Count != 0 ||
                    SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(_continuousContract.Mapped, includeInternalConfigs: true).Count == 0)
                {
                    throw new RegressionTestException("Continuous future underlying should only have internal subscription configs");
                }

                var contract = OptionChain(_continuousContract.Mapped).FirstOrDefault()?.Symbol;

                try
                {
                    _fopContract = AddFutureOptionContract(contract);
                }
                catch (Exception e)
                {
                    throw new RegressionTestException($"Failed to add future option contract {contract}", e);
                }
            }
            else if (_fopContract != null && changes.AddedSecurities.Any(security => security.Symbol == _fopContract.Symbol))
            {
                var underlyingSubscriptions = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(_fopContract.Symbol.Underlying);
                if (underlyingSubscriptions.Any(x => x.DataNormalizationMode == DataNormalizationMode.Raw))
                {
                    throw new RegressionTestException("Future option underlying should not have raw data normalization mode");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_fopContract == null)
            {
                throw new RegressionTestException("Failed to add future option contract");
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
        public long DataPoints => 3181;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1;

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
            {"Information Ratio", "-14.395"},
            {"Tracking Error", "0.043"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
