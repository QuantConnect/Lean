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
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting universe selection happens during warmup
    /// </summary>
    public class WarmupSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int NumberOfSymbols = 3;
        private Queue<DateTime> _selection = new Queue<DateTime>(new[]
        {
            new DateTime(2014, 03, 24),

            new DateTime(2014, 03, 25),
            new DateTime(2014, 03, 26),
            new DateTime(2014, 03, 27),
            new DateTime(2014, 03, 28),
            new DateTime(2014, 03, 29),

            new DateTime(2014, 04, 01),
            new DateTime(2014, 04, 02),
            new DateTime(2014, 04, 03),
            new DateTime(2014, 04, 04),
            new DateTime(2014, 04, 05),
        });

        // initialize our changes to nothing
        private SecurityChanges _changes = SecurityChanges.None;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 26);
            SetEndDate(2014, 04, 07);

            AddUniverse(CoarseSelectionFunction);
            SetWarmup(2, Resolution.Daily);
        }

        // sort the data by daily dollar volume and take the top 'NumberOfSymbols'
        private IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            Debug($"Coarse selection happening at {Time} {IsWarmingUp}");
            var expected = _selection.Dequeue();
            if (expected != Time && !LiveMode)
            {
                throw new RegressionTestException($"Unexpected selection time: {Time}. Expected {expected}");
            }

            // sort descending by daily dollar volume
            var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume);

            // take the top entries from our sorted collection
            var top = sortedByDollarVolume.Take(NumberOfSymbols);

            // we need to return only the symbol objects
            return top.Select(x => x.Symbol);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            Debug($"OnData({UtcTime:o}): {IsWarmingUp}. {string.Join(", ", slice.Values.OrderBy(x => x.Symbol))}");

            // if we have no changes, do nothing
            if (_changes == SecurityChanges.None || IsWarmingUp)
            {
                return;
            }

            // liquidate removed securities
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            // we want 1/N allocation in each security in our universe
            foreach (var security in _changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, 1m / NumberOfSymbols);
            }

            _changes = SecurityChanges.None;
        }

        // this event fires whenever we have changes to our universe
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
            Debug($"OnSecuritiesChanged({UtcTime:o}):: {changes}");
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"OnOrderEvent({UtcTime:o}):: {orderEvent}");
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
        public virtual long DataPoints => 78067;

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
            {"Total Orders", "8"},
            {"Average Win", "0.64%"},
            {"Average Loss", "-0.13%"},
            {"Compounding Annual Return", "11.057%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "0.938"},
            {"Start Equity", "100000"},
            {"End Equity", "100374.24"},
            {"Net Profit", "0.374%"},
            {"Sharpe Ratio", "1.048"},
            {"Sortino Ratio", "1.627"},
            {"Probabilistic Sharpe Ratio", "50.929%"},
            {"Loss Rate", "67%"},
            {"Win Rate", "33%"},
            {"Profit-Loss Ratio", "4.81"},
            {"Alpha", "0.088"},
            {"Beta", "0.152"},
            {"Annual Standard Deviation", "0.073"},
            {"Annual Variance", "0.005"},
            {"Information Ratio", "1.369"},
            {"Tracking Error", "0.109"},
            {"Treynor Ratio", "0.504"},
            {"Total Fees", "$43.94"},
            {"Estimated Strategy Capacity", "$620000000.00"},
            {"Lowest Capacity Asset", "FB V6OIPNZEM8V9"},
            {"Portfolio Turnover", "15.44%"},
            {"OrderListHash", "e401e24ad8c273d99611d79d59e804d7"}
        };
    }
}
