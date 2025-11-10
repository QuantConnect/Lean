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
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that security are automatically seeded by default
    /// </summary>
    public abstract class AutomaticSeedBaseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected virtual bool ShouldHaveTradeData { get; }
        protected virtual bool ShouldHaveQuoteData { get; }
        protected virtual bool ShouldHaveOpenInterestData { get; }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            var gotTrades = false;
            var gotQuotes = false;
            var gotOpenInterest = false;

            foreach (var addedSecurity in changes.AddedSecurities.Where(x => !x.Symbol.IsCanonical() || x.Symbol.SecurityType == SecurityType.Future))
            {
                if (addedSecurity.Price == 0)
                {
                    throw new RegressionTestException("Security was not seeded");
                }

                if (!addedSecurity.HasData)
                {
                    throw new RegressionTestException("Security does not have TradeBar or QuoteBar or OpenInterest data");
                }

                gotTrades |= addedSecurity.Cache.GetData<TradeBar>() != null;
                gotQuotes |= addedSecurity.Cache.GetData<QuoteBar>() != null;
                gotOpenInterest |= addedSecurity.Cache.GetData<OpenInterest>() != null;
            }

            if (changes.AddedSecurities.Count > 0)
            {
                if (ShouldHaveTradeData && !gotTrades)
                {
                    throw new RegressionTestException("No contract had TradeBar data");
                }

                if (ShouldHaveQuoteData && !gotQuotes)
                {
                    throw new RegressionTestException("No contract had QuoteBar data");
                }

                if (ShouldHaveOpenInterestData && !gotOpenInterest)
                {
                    throw new RegressionTestException("No contract had OpenInterest data");
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
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public abstract long DataPoints { get; }

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public abstract int AlgorithmHistoryDataPoints { get; }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
