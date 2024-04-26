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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of using universe selection with futures
    /// </summary>
    public class FuturesFrameworkRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static Symbol _es = QuantConnect.Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME);
        private static Symbol _gold = QuantConnect.Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX);

        private readonly Dictionary<Symbol, bool> _addedCanonical = new() { { _gold, false }, { _es, false } };
        private readonly Dictionary<Symbol, bool> _removedCanonical = new() { { _gold, false }, { _es, false } };
        private readonly Dictionary<Symbol, SimpleMovingAverage> _canonicalData = new() { { _gold, null }, { _es, null } };

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            SetUniverseSelection(new FutureUniverseSelectionModel(QuantConnect.Time.OneDay, SelectFutureChainSymbols));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
        }

        private static IEnumerable<Symbol> SelectFutureChainSymbols(DateTime utcTime)
        {
            var newYorkTime = utcTime.ConvertFromUtc(TimeZones.NewYork);
            if (newYorkTime.Date < new DateTime(2013, 10, 09))
            {
                yield return _es;
            }
            if (newYorkTime.Date >= new DateTime(2013, 10, 09))
            {
                yield return _gold;
            }
        }

        public override void OnData(Slice slice)
        {
            var future = _es;
            if (Time.Date >= new DateTime(2013, 10, 09))
            {
                future = _gold;
            }

            var continuous = Securities[future];
            if (continuous.Price == Securities[(continuous as Future).Mapped].Price)
            {
                // prices should never match because we are using the default backwards adjusted mode, they would match if we used raw mode
                throw new Exception($"Unexpected continuous future price {continuous.Price}");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                if (added.Symbol.IsCanonical())
                {
                    _addedCanonical[added.Symbol] = true;
                    _canonicalData[added.Symbol] = SMA(added.Symbol, 10);
                }
            }
            foreach (var removed in changes.RemovedSecurities)
            {
                if (removed.Symbol.IsCanonical())
                {
                    _removedCanonical[removed.Symbol] = true;
                }
            }

            var canonicals = changes.AddedSecurities.Select(x => x.Symbol.Canonical).ToHashSet();
            var nonCanonicals = changes.AddedSecurities.Where(x => !x.Symbol.IsCanonical()).ToList();
            foreach (var subscriptions in SubscriptionManager.Subscriptions.Where(x => canonicals.Contains(x.Symbol.Canonical)).GroupBy(x => x.Symbol.Canonical))
            {
                // trade & quote for canonical + contract chain (universe data)
                if (subscriptions.Count(x => x.Symbol.IsCanonical()) != canonicals.Count * 3)
                {
                    throw new Exception($"Unexpected canonical subscription count {subscriptions.Count(x => x.Symbol.IsCanonical())}");
                }

                // trade and quote for non canonicals
                if (subscriptions.Count(x => !x.Symbol.IsCanonical()) != nonCanonicals.Count * 2)
                {
                    throw new Exception($"Unexpected non canonical subscription count {subscriptions.Count(x => !x.Symbol.IsCanonical())}");
                }
            }

            var internalSubscriptions = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(includeInternalConfigs: true)
                .Where(x => x.SecurityType == SecurityType.Future && x.IsInternalFeed && canonicals.Contains(x.Symbol.Canonical)).ToList();
            // an open interest subscription for each + trade and quote for the currently mapped continuous future
            if (internalSubscriptions.Count != (nonCanonicals.Count + canonicals.Count + canonicals.Count * 2))
            {
                throw new Exception($"Unexpected internal subscription count {internalSubscriptions.Count}");
            }

            // we expect a single continuous universe at the time
            var universeSubscriptions = SubscriptionManager.Subscriptions.Count(x => x.Symbol.ID.Symbol.Contains("QC-UNIVERSE-CONTINUOUS"));
            if (universeSubscriptions != 1)
            {
                throw new Exception($"Unexpected universe subscription count {universeSubscriptions}");
            }

            // we expect a single canonical at the time
            var canonicalSubscriptions = SubscriptionManager.Subscriptions.Where(x => !x.Symbol.ID.Symbol.Contains("QC-UNIVERSE-CONTINUOUS") && x.Symbol.IsCanonical())
                .Select(x => x.Symbol.Canonical).ToHashSet();
            if (canonicalSubscriptions.Count != 1)
            {
                throw new Exception($"Unexpected universe subscription count {universeSubscriptions}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            foreach (var canonical in _addedCanonical)
            {
                if (!canonical.Value)
                {
                    throw new Exception($"Canonical {canonical} was not added!");
                }
            }
            foreach (var canonical in _removedCanonical)
            {
                if (canonical.Key.ID.Symbol == "ES" && !canonical.Value || canonical.Key.ID.Symbol == "GC" && canonical.Value)
                {
                    throw new Exception($"Canonical {canonical} was not removed!");
                }
            }
            foreach (var canonical in _canonicalData)
            {
                if (canonical.Value == null || !canonical.Value.IsReady)
                {
                    throw new Exception($"Canonical {canonical} emitted no data!");
                }
            }

            if (SubscriptionManager.Subscriptions.Any(x => x.Symbol.ID.Symbol == "ES"))
            {
                throw new Exception($"There should be no ES subscription!");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 126806;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "10"},
            {"Average Win", "0%"},
            {"Average Loss", "-4.59%"},
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "33.200%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "79014"},
            {"Net Profit", "-20.986%"},
            {"Sharpe Ratio", "-0.537"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "18.566%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-11.401"},
            {"Beta", "5.262"},
            {"Annual Standard Deviation", "1.875"},
            {"Annual Variance", "3.515"},
            {"Information Ratio", "-1.71"},
            {"Tracking Error", "1.744"},
            {"Treynor Ratio", "-0.191"},
            {"Total Fees", "$86.00"},
            {"Estimated Strategy Capacity", "$410000.00"},
            {"Lowest Capacity Asset", "ES VRJST036ZY0X"},
            {"Portfolio Turnover", "766.37%"},
            {"OrderListHash", "cdaa87b62e159eaa3b0da65b305e89bd"}
        };
    }
}
