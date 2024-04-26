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
    /// Regression algorithm reproducing GH issue 6263. Where some data types would get dropped from the warmup feed
    /// </summary>
    public class WarmupDataTypesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _equityGotTradeBars;
        private bool _equityGotQuoteBars;

        private bool _cryptoGotTradeBars;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);

            AddEquity("SPY", Resolution.Minute, fillForward: false);
            AddCrypto("BTCUSD", Resolution.Hour, market: Market.Bitfinex, fillForward: false);

            SetWarmUp(24, Resolution.Hour);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            Debug($"[{Time}] Warmup: {IsWarmingUp}. Invested: {Portfolio.Invested} {string.Join(",", Securities.Select(pair => $"{pair.Key.Value}:{pair.Value.Price}"))}");
            if (IsWarmingUp)
            {
                _equityGotTradeBars |= data.Bars.ContainsKey("SPY");
                _equityGotQuoteBars |= data.QuoteBars.ContainsKey("SPY");

                _cryptoGotTradeBars |= data.Bars.ContainsKey("BTCUSD");
            }
            else
            {
                if (!Portfolio.Invested)
                {
                    AddEquity("AAPL", Resolution.Hour);
                    SetHoldings("BTCUSD", 0.3);
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_equityGotTradeBars || !_cryptoGotTradeBars)
            {
                throw new Exception("Did not get any TradeBar during warmup");
            }
            // we don't have quote bars for equity in daily/hour resolutions
            if (!_equityGotQuoteBars && !Settings.WarmupResolution.HasValue)
            {
                throw new Exception("Did not get any QuoteBar during warmup");
            }
            if (Securities["AAPL"].Price == 0)
            {
                throw new Exception("Security added after warmup didn't get any data!");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 3763;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 41;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "106.090%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000.0"},
            {"End Equity", "100596.13"},
            {"Net Profit", "0.596%"},
            {"Sharpe Ratio", "123.324"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.394"},
            {"Beta", "0.029"},
            {"Annual Standard Deviation", "0.007"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-65.071"},
            {"Tracking Error", "0.236"},
            {"Treynor Ratio", "29.932"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$3000.00"},
            {"Lowest Capacity Asset", "BTCUSD E3"},
            {"Portfolio Turnover", "9.97%"},
            {"OrderListHash", "98661718a82110916cdeceed756c5d37"}
        };
    }
}
