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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Future;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Continuous Futures History Regression algorithm. Asserting and showcasing the behavior of adding a continuous future
    /// </summary>
    public class ContinuousFutureHistoryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private bool _warmedUp;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 10);
            SetEndDate(2013, 10, 11);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.OpenInterest,
                contractDepthOffset: 1
            );
            SetWarmup(1, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            // ES has an expiration on december but because we are using 'contractDepthOffset: 1' we expect to use the next contract
            if (_continuousContract.Mapped.ID.Date.Month != 3)
            {
                throw new RegressionTestException($"Unexpected mapped continuous contract future {_continuousContract.Mapped}");
            }

            if (IsWarmingUp)
            {
                // warm up data
                _warmedUp = true;

                if (!_continuousContract.HasData)
                {
                    throw new RegressionTestException($"ContinuousContract did not get any data during warmup!");
                }

                var backMonthExpiration =   slice.Keys.Single().Underlying.ID.Date;
                var frontMonthExpiration = FuturesExpiryFunctions.FuturesExpiryFunction(_continuousContract.Symbol)(Time.AddMonths(1));
                if (backMonthExpiration <= frontMonthExpiration.Date)
                {
                    throw new RegressionTestException($"Unexpected current mapped contract expiration {backMonthExpiration}" +
                        $" @ {Time} it should be AFTER front month expiration {frontMonthExpiration}");
                }
            }
            if (slice.Keys.Count != 1)
            {
                throw new RegressionTestException($"We are getting data for more than one symbols! {string.Join(",", slice.Keys.Select(symbol => symbol))}");
            }

            if (!Portfolio.Invested && !IsWarmingUp)
            {
                Buy(_continuousContract.Mapped, 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_warmedUp)
            {
                throw new RegressionTestException("Algorithm didn't warm up!");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Debug($"{Time}-{changes}");
            if (changes.AddedSecurities.Any(security => security.Symbol != _continuousContract.Symbol)
                || changes.RemovedSecurities.Any(security => security.Symbol != _continuousContract.Symbol))
            {
                throw new RegressionTestException($"We got an unexpected security changes {changes}");
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
        public virtual long DataPoints => 5469;

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
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101558.2"},
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
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$130000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "41.23%"},
            {"OrderListHash", "b9f8e1a0704c086944e5df07e0ab04d6"}
        };
    }
}
