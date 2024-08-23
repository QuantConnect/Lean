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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using System;
using QuantConnect.Util;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// </summary>
    public class ContinuousFutureRolloverExchangeTimeZoneBehindOfDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;

        private Dictionary<string, DateTime> _mappings = new();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 12, 20);
            SetCash(1000000);

            var ticker = Futures.Indices.SP500EMini;
            // Data time zone ahead of exchange time zone
            var exchangeTimeZone = TimeZones.NewYork;
            var dataTimeZone = TimeZones.Utc;

            var marketHours = MarketHoursDatabase.GetEntry(Market.CME, ticker, SecurityType.Future);
            var exchangeHours = new SecurityExchangeHours(exchangeTimeZone,
                marketHours.ExchangeHours.Holidays,
                marketHours.ExchangeHours.MarketHours.ToDictionary(),
                marketHours.ExchangeHours.EarlyCloses,
                marketHours.ExchangeHours.LateOpens);
            MarketHoursDatabase.SetEntry(Market.CME, ticker, SecurityType.Future, exchangeHours, dataTimeZone);

            SetTimeZone(exchangeTimeZone);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                resolution: Resolution.Minute,
                extendedMarketHours: true,
                dataNormalizationMode: DataNormalizationMode.Raw,
                dataMappingMode: DataMappingMode.OpenInterest,
                contractDepthOffset: 0
            );

            SetBenchmark(x => 0);
        }

        public override void OnData(Slice slice)
        {
            if (!_mappings.ContainsKey(_continuousContract.Mapped))
            {
                _mappings[_continuousContract.Mapped] = Time;
            }

            if (slice.SymbolChangedEvents.TryGetValue(_continuousContract.Symbol, out var changedEvent))
            {
                var configs = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs().ToList();

                var oldSymbol = changedEvent.OldSymbol;
                var newSymbol = changedEvent.NewSymbol;

                Log($"--------------> Rollover - Symbol changed at {Time}: {oldSymbol} -> {newSymbol}");
            }

            if (Securities[_continuousContract.Mapped].Close != _continuousContract.Close)
            {
                var configs = SubscriptionManager.Subscriptions.ToList();

                Log($"==========> [{Time}] [{_continuousContract.Mapped}] " +
                    $"Continuous contract close price: {_continuousContract.Close}, " +
                    $"mapped close price: {Securities[Symbol(_continuousContract.Mapped)].Close}");
                throw new Exception("Close prices do not match:\n" +
                    $"[{Time}] [{_continuousContract.Mapped}] " +
                    $"Continuous contract close price: {_continuousContract.Close}, " +
                    $"mapped close price: {Securities[Symbol(_continuousContract.Mapped)].Close}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"*****************************************************************");
            foreach (var mapping in _mappings)
            {
                Log($"{mapping.Key} -> {mapping.Value}");
            }
            Log($"*****************************************************************");

            if (_mappings.Count != 2)
            {
                throw new Exception($"Expected 2 mappings but got {_mappings.Count}");
            }

            if (!_mappings.TryGetValue("ES VMKLFZIH2MTD", out var firstMappingDate))
            {
                throw new Exception($"First mapping not found: ES VMKLFZIH2MTD");
            }

            var expectedFirstMappingDate = new DateTime(2013, 10, 8);
            if (firstMappingDate.Date != expectedFirstMappingDate)
            {
                throw new Exception($"First mapping date is not correct. Expected: {expectedFirstMappingDate}. Actual: {firstMappingDate.Date}");
            }

            if (!_mappings.TryGetValue("ES VP274HSU1AF5", out var secondMappingDate))
            {
                throw new Exception($"Second mapping not found: ES VP274HSU1AF5");
            }

            var expectedSecondMappingDate = new DateTime(2013, 12, 18);
            if (secondMappingDate.Date != expectedSecondMappingDate)
            {
                throw new Exception($"Second mapping date is not correct. Expected: {expectedSecondMappingDate}. Actual: {secondMappingDate.Date}");
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
        public long DataPoints => 1334;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 4;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0.53%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "3.011%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "1005283.2"},
            {"Net Profit", "0.528%"},
            {"Sharpe Ratio", "1.285"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "83.704%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.015"},
            {"Beta", "-0.004"},
            {"Annual Standard Deviation", "0.011"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-4.774"},
            {"Tracking Error", "0.084"},
            {"Treynor Ratio", "-3.121"},
            {"Total Fees", "$4.30"},
            {"Estimated Strategy Capacity", "$5900000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "0.27%"},
            {"OrderListHash", "90f952729deb9cb20be75867576e5b87"}
        };

        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;
    }
}
