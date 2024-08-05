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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests that we can use selectors in the indicators
    /// that need quote data
    /// </summary>
    public class IndicatorSelectorsWorkWithDifferentOptions: QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<Indicator> _equityIndicators;
        private Indicator _optionIndicator;
        private Minimum _indicatorHistory;
        private Symbol _equity;
        private Symbol _option;
        private bool _quoteBarsFound;
        private bool _tradeBarsFound;

        public override void Initialize()
        {
            SetStartDate(2013, 06, 07);
            SetEndDate(2013, 11, 08);

            _equity = AddEquity("SPY", Resolution.Minute).Symbol;
            _option = AddOption("NWSA", Resolution.Minute).Symbol;
            _option = QuantConnect.Symbol.CreateOption("NWSA", Market.USA, OptionStyle.American, OptionRight.Put, 33, new DateTime(2013, 07, 20));
            AddOptionContract(_option, Resolution.Minute);

            _equityIndicators = new List<Indicator>()
            {
                Identity(_equity, Resolution.Minute, Field.BidClose, "Bid.Close."),
                Identity(_equity, Resolution.Minute, Field.BidOpen, "Bid.Open."),
                Identity(_equity, Resolution.Minute, Field.BidLow, "Bid.Low."),
                Identity(_equity, Resolution.Minute, Field.BidHigh, "Bid.High."),
                Identity(_equity, Resolution.Minute, Field.AskClose, "Ask.Close."),
                Identity(_equity, Resolution.Minute, Field.AskOpen, "Ask.Open."),
                Identity(_equity, Resolution.Minute, Field.AskLow, "Ask.Low."),
                Identity(_equity, Resolution.Minute, Field.AskHigh, "Ask.High."),
            };

            _optionIndicator = Identity(_option, Resolution.Minute, Field.Volume, "Volume.");
            _indicatorHistory = MIN(_equity, 5, Resolution.Minute);
        }

        public override void OnData(Slice slice)
        {
            if (slice.QuoteBars.ContainsKey(_equity))
            {
                _quoteBarsFound = true;
                var wrongEquityIndicators = _equityIndicators.Where(x =>
                {
                    var propertyName = x.Name.Split(".")[0]; // This could be Ask/Bid
                    var secondPropertyName = x.Name.Split(".")[1]; // This could be Open/Close/High/Low
                    var property = slice.QuoteBars[_equity].GetType().GetProperty(propertyName).GetValue(slice.QuoteBars[_equity], null);
                    var value = (decimal)property.GetType().GetProperty(secondPropertyName).GetValue(property, null);
                    return x.Current.Value != value;
                });

                if (wrongEquityIndicators.Any())
                {
                    throw new RegressionTestException();
                }
            }

            if (slice.OptionChains.TryGetValue(_option.Canonical, out var optionChain) && optionChain.TradeBars.TryGetValue(_option, out var optionChainTradeBar))
            {
                _tradeBarsFound = true;
                if (_optionIndicator.Current.Value != optionChainTradeBar.Volume)
                {
                    throw new RegressionTestException();
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_quoteBarsFound)
            {
                throw new RegressionTestException("At least one quote bar should have been found, but none was found");
            }

            if (!_tradeBarsFound)
            {
                throw new RegressionTestException("At least one trade bar should have been found, but none was found");
            }

            var volumeHistory = IndicatorHistory(_indicatorHistory, _equity, 30000, Resolution.Minute, Field.Volume);

            if (volumeHistory.Count == 0)
            {
                throw new Exception("No history indicator data point was found using Field.Volume indicator!");
            }

            var bidCloseHistory = IndicatorHistory(_indicatorHistory, _equity, 30000, Resolution.Minute, Field.BidClose);

            if (bidCloseHistory.Count == 0)
            {
                throw new Exception("No history indicator data point was found using BidClose indicator!");
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
        public long DataPoints => 4536849;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 40560;

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
            {"Information Ratio", "-1.543"},
            {"Tracking Error", "0.098"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
