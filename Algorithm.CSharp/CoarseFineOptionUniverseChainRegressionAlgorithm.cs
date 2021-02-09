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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to chain a coarse and fine universe selection with an option chain universe selection model
    /// that will add and remove an <see cref="OptionChainUniverse"/> for each symbol selected on fine
    /// </summary>
    public class CoarseFineOptionUniverseChainRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        // initialize our changes to nothing
        private SecurityChanges _changes = SecurityChanges.None;
        private int _optionCount;
        private Symbol _lastEquityAdded;
        private Symbol _aapl;
        private Symbol _twx;

        public override void Initialize()
        {
            _twx = QuantConnect.Symbol.Create("TWX", SecurityType.Equity, Market.USA);
            _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 06);

            var selectionUniverse = AddUniverse(enumerable => new[] { Time.Date <= new DateTime(2014, 6, 5) ? _twx : _aapl },
                enumerable => new[] { Time.Date <= new DateTime(2014, 6, 5) ? _twx : _aapl });

            AddUniverseOptions(selectionUniverse, universe =>
            {
                if (universe.Underlying == null)
                {
                    throw new Exception("Underlying data point is null! This shouldn't happen, each OptionChainUniverse handles and should provide this");
                }
                return universe.IncludeWeeklys()
                    .FrontMonth()
                    .Contracts(universe.Take(5));
            });
        }

        public override void OnData(Slice data)
        {
            // if we have no changes, do nothing
            if (_changes == SecurityChanges.None ||
                _changes.AddedSecurities.Any(security => security.Price == 0))
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

            foreach (var security in _changes.AddedSecurities)
            {
                if (!security.Symbol.HasUnderlying)
                {
                    _lastEquityAdded = security.Symbol;
                }
                else
                {
                    // options added should all match prev added security
                    if (security.Symbol.Underlying != _lastEquityAdded)
                    {
                        throw new Exception($"Unexpected symbol added {security.Symbol}");
                    }

                    _optionCount++;
                }

                SetHoldings(security.Symbol, 0.05m);

                var config = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(security.Symbol).ToList();

                if (!config.Any())
                {
                    throw new Exception($"Was expecting configurations for {security.Symbol}");
                }
                if (config.Any(dataConfig => dataConfig.DataNormalizationMode != DataNormalizationMode.Raw))
                {
                    throw new Exception($"Was expecting DataNormalizationMode.Raw configurations for {security.Symbol}");
                }
            }
            _changes = SecurityChanges.None;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes += changes;
        }

        public override void OnEndOfAlgorithm()
        {
            var config = SubscriptionManager.Subscriptions.ToList();
            if (config.Any(dataConfig => dataConfig.Symbol == _twx || dataConfig.Symbol.Underlying == _twx))
            {
                throw new Exception($"Was NOT expecting any configurations for {_twx} or it's options, since coarse/fine should have deselected it");
            }

            if (_optionCount == 0)
            {
                throw new Exception("Option universe chain did not add any option!");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "13"},
            {"Average Win", "0.65%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "3216040423556140000000000%"},
            {"Drawdown", "0.500%"},
            {"Expectancy", "1.393"},
            {"Net Profit", "32.840%"},
            {"Sharpe Ratio", "7.14272222483913E+15"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "83%"},
            {"Win Rate", "17%"},
            {"Profit-Loss Ratio", "13.36"},
            {"Alpha", "2.59468989671647E+16"},
            {"Beta", "67.661"},
            {"Annual Standard Deviation", "3.633"},
            {"Annual Variance", "13.196"},
            {"Information Ratio", "7.24987266907741E+15"},
            {"Tracking Error", "3.579"},
            {"Treynor Ratio", "383485597312030"},
            {"Total Fees", "$13.00"},
            {"Fitness Score", "0.232"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.232"},
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
            {"OrderListHash", "12470afd9a74ad9c9802361f6f092777"}
        };
    }
}
