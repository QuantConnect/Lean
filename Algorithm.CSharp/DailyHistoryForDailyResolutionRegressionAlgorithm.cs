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
    public class DailyHistoryForDailyResolutionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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
                AddSecurity(symbol, Resolution.Daily);
            }
        }

        public override void OnData(Slice data)
        {
            using (var enumerator = data.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    var symbol = current.Key;
                    _received.Add(symbol);

                    List<BaseData> history;

                    if (current.Value.DataType == MarketDataType.QuoteBar)
                    {
                        history = History(1, Resolution.Daily).Get<QuoteBar>(symbol).Cast<BaseData>().ToList();
                    }
                    else
                    {
                        history = History(1, Resolution.Daily).Get<TradeBar>(symbol).Cast<BaseData>().ToList();
                    }

                    if (!history.Any()) throw new Exception($"No {symbol} data on the eve of {Time} {Time.DayOfWeek}");
                }
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.084"},
            {"Tracking Error", "0.183"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0"},
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
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
