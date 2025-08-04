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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm asserts we can consolidate Tick data with different tick types
    /// </summary>
    public class ConsolidateDifferentTickTypesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _thereIsAtLeastOneQuoteTick;
        private bool _thereIsAtLeastOneTradeTick;

        private bool _thereIsAtLeastOneTradeBar;
        private bool _thereIsAtLeastOneQuoteBar;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 06);
            SetEndDate(2013, 10, 07);
            SetCash(1000000);

            var equity = AddEquity("SPY", Resolution.Tick, Market.USA);
            var quoteConsolidator = Consolidate(equity.Symbol, Resolution.Tick, TickType.Quote, (Tick tick) => OnQuoteTick(tick));
            _thereIsAtLeastOneQuoteTick = false;

            var tradeConsolidator = Consolidate(equity.Symbol, Resolution.Tick, TickType.Trade, (Tick tick) => OnTradeTick(tick));
            _thereIsAtLeastOneTradeTick = false;

            // TickConsolidators with max count
            Consolidate(equity.Symbol, 10m, TickType.Trade, (TradeBar tick) => OnTradeTickMaxCount(tick));
            Consolidate(equity.Symbol, 10m, TickType.Quote, (QuoteBar tick) => OnQuoteTickMaxCount(tick));
        }

        public void OnTradeTickMaxCount(TradeBar tradeBar)
        {
            _thereIsAtLeastOneTradeBar = true;
        }
        public void OnQuoteTickMaxCount(QuoteBar quoteBar)
        {
            _thereIsAtLeastOneQuoteBar = true;
        }

        public void OnQuoteTick(Tick tick)
        {
            _thereIsAtLeastOneQuoteTick = true;
            if (tick.TickType != TickType.Quote)
            {
                throw new RegressionTestException($"The type of the tick should be Quote, but was {tick.TickType}");
            }
        }

        public void OnTradeTick(Tick tick)
        {
            _thereIsAtLeastOneTradeTick = true;
            if (tick.TickType != TickType.Trade)
            {
                throw new RegressionTestException($"The type of the tick should be Trade, but was {tick.TickType}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_thereIsAtLeastOneQuoteTick)
            {
                throw new RegressionTestException($"There should have been at least one tick in OnQuoteTick() method, but there wasn't");
            }

            if (!_thereIsAtLeastOneTradeTick)
            {
                throw new RegressionTestException($"There should have been at least one tick in OnTradeTick() method, but there wasn't");
            }

            if (!_thereIsAtLeastOneTradeBar)
            {
                throw new RegressionTestException($"There should have been at least one bar in OnTradeTickMaxCount() method, but there wasn't");
            }

            if (!_thereIsAtLeastOneQuoteBar)
            {
                throw new RegressionTestException($"There should have been at least one bar in OnQuoteTickMaxCount() method, but there wasn't");
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
        public long DataPoints => 2857175;

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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
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
