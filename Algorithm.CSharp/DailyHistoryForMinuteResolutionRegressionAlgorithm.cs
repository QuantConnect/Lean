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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm simply fetch one-day history prior current time.
    /// </summary>
    public class DailyHistoryForMinuteResolutionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol[] _symbols = {
            QuantConnect.Symbol.Create("GBPUSD", SecurityType.Forex, market: Market.FXCM),
            QuantConnect.Symbol.Create("EURUSD", SecurityType.Forex, market: Market.Oanda),
            QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, market: Market.USA),
            QuantConnect.Symbol.Create("BTCUSD", SecurityType.Crypto, market: Market.GDAX),
            QuantConnect.Symbol.Create("XAUUSD", SecurityType.Cfd, market: Market.Oanda)
        };

        private HashSet<Symbol> _received = new HashSet<Symbol>();

        public override void Initialize()
        {
            SetStartDate(2018, 3, 26);
            SetEndDate(2018, 4, 10);
            foreach (var symbol in _symbols)
            {
                AddSecurity(symbol, Resolution.Minute);
            }

            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromHours(1)), MakeHistoryCall);
        }

        private void MakeHistoryCall()
        {
            foreach (var symbol in _symbols)
            {
                _received.Add(symbol);

                bool hasHistory = false;

                foreach (var dataType in SubscriptionManager.AvailableDataTypes[symbol.SecurityType])
                {
                    if (dataType == TickType.Quote)
                    {
                        hasHistory |= History(1, Resolution.Daily).Get<QuoteBar>(symbol).Any();
                    }
                    else
                    {
                        hasHistory |= History(1, Resolution.Daily).Get<TradeBar>(symbol).Any();
                    }
                }

                if (!hasHistory) throw new Exception($"No {symbol} data on the eve of {Time} {Time.DayOfWeek}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_received.Count != _symbols.Length)
            {
                throw new Exception($"Data for symbols {string.Join(",", _symbols.Except(_received))} were not received");
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
        public long DataPoints => 29579;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 42660;

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
            {"Start Equity", "100000.00"},
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
            {"Information Ratio", "-0.104"},
            {"Tracking Error", "0.192"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
