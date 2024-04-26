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
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// We add an option contract using <see cref="QCAlgorithm.AddOptionContract"/> and place a trade and wait till it expires
    /// later will liquidate the resulting equity position and assert both option and underlying get removed
    /// </summary>
    public class AddOptionContractExpiresRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _expiration = new DateTime(2014, 06, 21);
        private Symbol _option;
        private Symbol _twx;
        private bool _traded;

        public override void Initialize()
        {
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 30);

            _twx = QuantConnect.Symbol.Create("TWX", SecurityType.Equity, Market.USA);

            AddUniverse("my-daily-universe-name", time => new List<string> { "AAPL" });
        }

        public override void OnData(Slice data)
        {
            if (_option == null)
            {
                var option = OptionChainProvider.GetOptionContractList(_twx, Time)
                    .OrderBy(symbol => symbol.ID.Symbol)
                    .FirstOrDefault(optionContract => optionContract.ID.Date == _expiration
                                                      && optionContract.ID.OptionRight == OptionRight.Call
                                                      && optionContract.ID.OptionStyle == OptionStyle.American);
                if (option != null)
                {
                    _option = AddOptionContract(option).Symbol;
                }
            }

            if (_option != null && Securities[_option].Price != 0 && !_traded)
            {
                _traded = true;
                Buy(_option, 1);

                foreach (var symbol in new [] { _option, _option.Underlying })
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
            }

            if (Time.Date > _expiration)
            {
                if (SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(_option).Any())
                {
                    throw new Exception($"Unexpected configurations for {_option} after it has been delisted");
                }

                if (Securities[_twx].Invested)
                {
                    if (!SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(_twx).Any())
                    {
                        throw new Exception($"Was expecting configurations for {_twx}");
                    }

                    // first we liquidate the option exercised position
                    Liquidate(_twx);
                }
            }
            else if (Time.Date > _expiration && !Securities[_twx].Invested)
            {
                if (SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(_twx).Any())
                {
                    throw new Exception($"Unexpected configurations for {_twx} after it has been liquidated");
                }
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 37597;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "2.73%"},
            {"Average Loss", "-2.98%"},
            {"Compounding Annual Return", "-4.619%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "-0.042"},
            {"Start Equity", "100000"},
            {"End Equity", "99668"},
            {"Net Profit", "-0.332%"},
            {"Sharpe Ratio", "-4.614"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0.427%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.92"},
            {"Alpha", "-0.022"},
            {"Beta", "-0.012"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-2.823"},
            {"Tracking Error", "0.049"},
            {"Treynor Ratio", "2.01"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$5700000.00"},
            {"Lowest Capacity Asset", "AOL VRKS95ENLBYE|AOL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.55%"},
            {"OrderListHash", "24191a4a3bf11c07622a21266618193d"}
        };
    }
}
