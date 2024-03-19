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

using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// A demonstration of consolidating options data into larger bars for your algorithm.
    /// </summary>
    public class BasicTemplateOptionsConsolidationAlgorithm: QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Dictionary<Symbol, IDataConsolidator> _consolidators = new();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(1000000);

            var option = AddOption("SPY");
            option.SetFilter(-2, 2, 0, 189);
        }

        public void OnQuoteBarConsolidated(object sender, QuoteBar quoteBar)
        {
            Log($"OnQuoteBarConsolidated called on {Time}");
            Log(quoteBar.ToString());
        }

        public void OnTradeBarConsolidated(object sender, TradeBar tradeBar)
        {
            Log($"OnTradeBarConsolidated called on {Time}");
            Log(tradeBar.ToString());
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach(var security in changes.AddedSecurities)
            {
                IDataConsolidator consolidator;
                if (security.Type == SecurityType.Equity)
                {
                    consolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(5));
                    (consolidator as TradeBarConsolidator).DataConsolidated += OnTradeBarConsolidated;
                }
                else
                {
                    consolidator = new QuoteBarConsolidator(new TimeSpan(0, 5, 0));
                    (consolidator as QuoteBarConsolidator).DataConsolidated += OnQuoteBarConsolidated;
                }

                SubscriptionManager.AddConsolidator(security.Symbol, consolidator);
                _consolidators[security.Symbol] = consolidator;
            }

            foreach(var security in changes.RemovedSecurities)
            {
                _consolidators.Remove(security.Symbol, out var consolidator);
                SubscriptionManager.RemoveConsolidator(security.Symbol, consolidator);

                if (security.Type == SecurityType.Equity)
                {
                    (consolidator as TradeBarConsolidator).DataConsolidated -= OnTradeBarConsolidated;
                }
                else
                {
                    (consolidator as QuoteBarConsolidator).DataConsolidated -= OnQuoteBarConsolidated;
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
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Start Equity", "1000000"},
            {"End Equity", "1000000"},
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
