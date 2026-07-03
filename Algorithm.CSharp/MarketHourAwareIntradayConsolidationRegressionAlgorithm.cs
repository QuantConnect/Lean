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
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a <see cref="MarketHourAwareConsolidator"/> with an intraday period
    /// anchors each bar to the market open and never lets a bar extend past the market close.
    /// </summary>
    public class MarketHourAwareIntradayConsolidationRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly TimeSpan _period = TimeSpan.FromMinutes(7);
        private Future _future;
        private SecurityExchangeHours _hours;
        private int _consolidatedBarCount;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 06);
            SetEndDate(2013, 10, 11);

            _future = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, extendedMarketHours: true);
            _hours = _future.Exchange.Hours;

            var consolidator = new MarketHourAwareConsolidator(false, _period, typeof(TradeBar), TickType.Trade, extendedMarketHours: true);
            consolidator.DataConsolidated += OnSevenMinuteBar;
            SubscriptionManager.AddConsolidator(_future.Symbol, consolidator);
        }

        private void OnSevenMinuteBar(object sender, IBaseData consolidated)
        {
            var bar = (TradeBar)consolidated;
            var marketOpen = _hours.GetPreviousMarketOpen(bar.Time.AddTicks(1), extendedMarketHours: true);
            var marketClose = _hours.GetNextMarketClose(marketOpen, extendedMarketHours: true);

            // the bar must be anchored to the market open
            if ((bar.Time - marketOpen).Ticks % _period.Ticks != 0)
            {
                throw new RegressionTestException($"Bar starting at {bar.Time} is not anchored to the market open {marketOpen}");
            }

            // the bar must not extend past the market close
            if (bar.EndTime > marketClose)
            {
                throw new RegressionTestException($"Bar ending at {bar.EndTime} extends past the market close {marketClose}");
            }

            // bars span the full period unless the last one is clipped at the market close
            var barPeriod = bar.EndTime - bar.Time;
            if (barPeriod != _period && bar.EndTime != marketClose)
            {
                throw new RegressionTestException($"Bar from {bar.Time} to {bar.EndTime} has period {barPeriod} instead of {_period}");
            }

            _consolidatedBarCount++;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_consolidatedBarCount == 0)
            {
                throw new RegressionTestException("The consolidator did not produce any bar");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 41486;

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
            {"Information Ratio", "-2.564"},
            {"Tracking Error", "0.214"},
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
