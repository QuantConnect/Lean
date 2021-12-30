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
            SetWarmup(10);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (IsWarmingUp)
            {
                // warm up data
                _warmedUp = true;

                if (!_continuousContract.HasData)
                {
                    throw new Exception($"ContinuousContract did not get any data during warmup!");
                }

                var backMonthExpiration =   data.Keys.Single().Underlying.ID.Date;
                var frontMonthExpiration = FuturesExpiryFunctions.FuturesExpiryFunction(_continuousContract.Symbol)(Time.AddMonths(1));
                if (backMonthExpiration <= frontMonthExpiration.Date)
                {
                    throw new Exception($"Unexpected current mapped contract expiration {backMonthExpiration}" +
                        $" @ {Time} it should be AFTER front month expiration {frontMonthExpiration}");
                }
            }
            if (data.Keys.Count != 1)
            {
                throw new Exception($"We are getting data for more than one symbols! {string.Join(",", data.Keys.Select(symbol => symbol))}");
            }

            if (!Portfolio.Invested)
            {
                Buy(_continuousContract.Symbol, 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_warmedUp)
            {
                throw new Exception("Algorithm didn't warm up!");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Debug($"{Time}-{changes}");
            if (changes.AddedSecurities.Any(security => security.Symbol != _continuousContract.Symbol)
                || changes.RemovedSecurities.Any(security => security.Symbol != _continuousContract.Symbol))
            {
                throw new Exception($"We got an unexpected security changes {changes}");
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
            {"Total Trades", "1"},
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
            {"Total Fees", "$1.85"},
            {"Estimated Strategy Capacity", "$42000000.00"},
            {"Lowest Capacity Asset", "ES 1S1"},
            {"Fitness Score", "0.76"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.76"},
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
            {"OrderListHash", "4de9344671d542e30066338e2bf9d400"}
        };
    }
}
