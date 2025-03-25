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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Future;
using System;
using QuantConnect.Data.UniverseSelection;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that continuous future universe selection happens right away for all futures.
    /// An example case is ES and Milk futures, which have different time zones. ES is in New York and Milk is in Chicago.
    /// ES selection would happen first just because of this, but all futures should have a mapped contract right away.
    /// </summary>
    public class ContinuousFutureImmediateUniverseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _es;
        private Future _milk;

        private bool _dataReceived;

        private DateTime _startDateUtc;

        private DateTime _esSelectionTimeUtc;
        private DateTime _milkSelectionTimeUtc;

        private bool _securitiesChangedEventReceived;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);

            _startDateUtc = StartDate.ConvertToUtc(TimeZone);

            // ES time zone is New York
            _es = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.OpenInterestAnnual,
                contractDepthOffset: 0,
                extendedMarketHours: true);

            // Milk time zone is Chicago, so market open will be after ES
            _milk = AddFuture(Futures.Dairy.ClassIIIMilk,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.OpenInterestAnnual,
                contractDepthOffset: 0,
                extendedMarketHours: true);

            _es.SetFilter(universe =>
            {
                if (_esSelectionTimeUtc == DateTime.MinValue)
                {
                    _esSelectionTimeUtc = universe.LocalTime.ConvertToUtc(_es.Exchange.TimeZone);

                    if (_esSelectionTimeUtc != _startDateUtc)
                    {
                        throw new RegressionTestException($"Expected ES universe selection to happen on algorithm start ({_startDateUtc}), " +
                            $"but happened on {_esSelectionTimeUtc}");
                    }

                }

                return universe;
            });

            _milk.SetFilter(universe =>
            {
                if (_milkSelectionTimeUtc == DateTime.MinValue)
                {
                    _milkSelectionTimeUtc = universe.LocalTime.ConvertToUtc(_milk.Exchange.TimeZone);

                    if (_milkSelectionTimeUtc != _startDateUtc)
                    {
                        throw new RegressionTestException($"Expected DC universe selection to happen on algorithm start ({_startDateUtc}), " +
                            $"but happened on {_milkSelectionTimeUtc}");
                    }
                }

                return universe;
            });
        }

        public override void OnData(Slice slice)
        {
            _dataReceived = true;

            if (_es.Mapped == null)
            {
                throw new RegressionTestException("ES mapped contract is null");
            }

            // This is what we actually want to assert: even though Milk future time zone is 1 hour behind,
            // we should have a mapped contract right away.
            if (_milk.Mapped == null)
            {
                throw new RegressionTestException("DC mapped contract is null");
            }

            Log($"{slice.Time} :: ES Mapped Contract: {_es.Mapped}. DC Mapped Contract: {_milk.Mapped}");
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (!_securitiesChangedEventReceived)
            {
                _securitiesChangedEventReceived = true;

                if (Time != StartDate)
                {
                    throw new RegressionTestException($"Expected OnSecuritiesChanged to be called on algorithm start ({StartDate}), " +
                        $"but happened on {Time}");
                }

                if (_esSelectionTimeUtc == DateTime.MinValue)
                {
                    throw new RegressionTestException("ES universe selection time was not set");
                }

                if (_milkSelectionTimeUtc == DateTime.MinValue)
                {
                    throw new RegressionTestException("DC universe selection time was not set");
                }

                if (changes.AddedSecurities.Count == 0 || changes.RemovedSecurities.Count != 0)
                {
                    throw new RegressionTestException($"Unexpected securities changes. Expected multiple securities added and none removed " +
                        $"but got {changes.AddedSecurities.Count} securities added and {changes.RemovedSecurities.Count} removed.");
                }

                if (!changes.AddedSecurities.Any(x => !x.Symbol.IsCanonical() && x.Symbol.Canonical == _es.Symbol))
                {
                    throw new RegressionTestException($"Expected to find a multiple futures for ES");
                }

                if (!changes.AddedSecurities.Any(x => !x.Symbol.IsCanonical() && x.Symbol.Canonical == _milk.Symbol))
                {
                    throw new RegressionTestException($"Expected to find a multiple futures for DC");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // Just a protection in case data is changed to make sure assertions in OnData were done.
            if (!_dataReceived)
            {
                throw new RegressionTestException("No data was received so no checks were done");
            }

            if (!_securitiesChangedEventReceived)
            {
                throw new RegressionTestException("OnSecuritiesChanged was not called");
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
        public long DataPoints => 445961;

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
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
