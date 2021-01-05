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
    /// We add an option contract using <see cref="QCAlgorithm.AddOptionContract"/> and place a trade, the underlying
    /// gets deselected from the universe selection but should still be present since we manually added the option contract.
    /// Later we call <see cref="QCAlgorithm.RemoveOptionContract"/> and expect both option and underlying to be removed.
    /// </summary>
    public class AddOptionContractFromUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _expiration = new DateTime(2014, 06, 21);
        private SecurityChanges _securityChanges = SecurityChanges.None;
        private Symbol _option;
        private Symbol _aapl;
        private Symbol _twx;
        private bool _traded;

        public override void Initialize()
        {
            _twx = QuantConnect.Symbol.Create("TWX", SecurityType.Equity, Market.USA);
            _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            UniverseSettings.Resolution = Resolution.Minute;
            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 09);

            AddUniverse(enumerable => new[] { Time.Date <= new DateTime(2014, 6, 5) ? _twx : _aapl },
                enumerable => new[] { Time.Date <= new DateTime(2014, 6, 5) ? _twx : _aapl });
        }

        public override void OnData(Slice data)
        {
            if (_option != null && Securities[_option].Price != 0 && !_traded)
            {
                _traded = true;
                Buy(_option, 1);
            }

            if (Time.Date > new DateTime(2014, 6, 5))
            {
                if (Time < new DateTime(2014, 6, 6, 14, 0, 0))
                {
                    var configs = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(_twx);
                    // assert underlying still there after the universe selection removed it, still used by the manually added option contract
                    if (!configs.Any())
                    {
                        throw new Exception($"Was expecting configurations for {_twx}" +
                                            $" even after it has been deselected from coarse universe because we still have the option contract.");
                    }
                }
                else if (Time == new DateTime(2014, 6, 6, 14, 0, 0))
                {
                    // liquidate & remove the option
                    RemoveOptionContract(_option);
                }
                // assert underlying was finally removed
                else if(Time > new DateTime(2014, 6, 6, 14, 0, 0))
                {
                    foreach (var symbol in new[] { _option, _option.Underlying })
                    {
                        var configs = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol);
                        if (configs.Any())
                        {
                            throw new Exception($"Unexpected configuration for {symbol} after it has been deselected from coarse universe and option contract is removed.");
                        }
                    }
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (_securityChanges.RemovedSecurities.Intersect(changes.RemovedSecurities).Any())
            {
                throw new Exception($"SecurityChanges.RemovedSecurities intersect {changes.RemovedSecurities}. We expect no duplicate!");
            }
            if (_securityChanges.AddedSecurities.Intersect(changes.AddedSecurities).Any())
            {
                throw new Exception($"SecurityChanges.AddedSecurities intersect {changes.RemovedSecurities}. We expect no duplicate!");
            }
            // keep track of all removed and added securities
            _securityChanges += changes;

            if (changes.AddedSecurities.Any(security => security.Symbol.SecurityType == SecurityType.Option))
            {
                return;
            }

            foreach (var addedSecurity in changes.AddedSecurities)
            {
                var option = OptionChainProvider.GetOptionContractList(addedSecurity.Symbol, Time)
                    .OrderBy(symbol => symbol.ID.Symbol)
                    .First(optionContract => optionContract.ID.Date == _expiration
                                                      && optionContract.ID.OptionRight == OptionRight.Call
                                                      && optionContract.ID.OptionStyle == OptionStyle.American);
                AddOptionContract(option);

                foreach (var symbol in new[] { option, option.Underlying })
                {
                    var config = SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol).ToList();

                    if (!config.Any())
                    {
                        throw new Exception($"Was expecting configurations for {symbol}");
                    }
                    if (config.Any(dataConfig => dataConfig.DataNormalizationMode != DataNormalizationMode.Raw))
                    {
                        throw new Exception($"Was expecting DataNormalizationMode.Raw configurations for {symbol}");
                    }
                }

                // just keep the first we got
                if (_option == null)
                {
                    _option = option;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (SubscriptionManager.Subscriptions.Any(dataConfig => dataConfig.Symbol == _twx || dataConfig.Symbol.Underlying == _twx))
            {
                throw new Exception($"Was NOT expecting any configurations for {_twx} or it's options, since we removed the contract");
            }

            if (SubscriptionManager.Subscriptions.All(dataConfig => dataConfig.Symbol != _aapl))
            {
                throw new Exception($"Was expecting configurations for {_aapl}");
            }
            if (SubscriptionManager.Subscriptions.All(dataConfig => dataConfig.Symbol.Underlying != _aapl))
            {
                throw new Exception($"Was expecting options configurations for {_aapl}");
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
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.23%"},
            {"Compounding Annual Return", "-15.596%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.232%"},
            {"Sharpe Ratio", "-7.739"},
            {"Probabilistic Sharpe Ratio", "1.216%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.027"},
            {"Beta", "-0.174"},
            {"Annual Standard Deviation", "0.006"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-11.586"},
            {"Tracking Error", "0.042"},
            {"Treynor Ratio", "0.286"},
            {"Total Fees", "$2.00"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-19.883"},
            {"Return Over Maximum Drawdown", "-67.224"},
            {"Portfolio Turnover", "0.014"},
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
            {"OrderListHash", "721476625"}
        };
    }
}
