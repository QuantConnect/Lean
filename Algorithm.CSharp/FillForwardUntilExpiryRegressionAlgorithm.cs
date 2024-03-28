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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm checks FillForwardEnumerator should FF the data until it reaches the delisting date
    /// replicates GH issue https://github.com/QuantConnect/Lean/issues/4872
    /// </summary>
    public class FillForwardUntilExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _realEndDate = new DateTime(2014, 06, 07);
        private SecurityExchange _exchange;
        private Dictionary<Symbol, HashSet<DateTime>> _options;

        private string[] _contracts =
        {
            "TWX   140621P00067500",
            "TWX   140621C00067500",
            "TWX   140621C00070000",
            "TWX   140621P00070000"
        };

        public override void Initialize()
        {
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 30);

            _options = new Dictionary<Symbol, HashSet<DateTime>>();
            var _twxOption = AddOption("TWX", Resolution.Minute);
            _exchange = _twxOption.Exchange;
            _twxOption.SetFilter((x) => x
                .Contracts(c => c.Where(s => _contracts.Contains(s.Value))));
            SetBenchmark(t => 1);
        }

        public override void OnData(Slice data)
        {
            foreach (var value in data.OptionChains.Values)
            {
                foreach (var contact in value.Contracts)
                {
                    BaseData bar = null;
                    QuoteBar quoteBar;
                    if (bar == null && value.QuoteBars.TryGetValue(contact.Key, out quoteBar))
                    {
                        bar = quoteBar;
                    }
                    TradeBar tradeBar;
                    if (bar == null && value.TradeBars.TryGetValue(contact.Key, out tradeBar))
                    {
                        bar = tradeBar;
                    }
                    if (bar.IsFillForward)
                    {
                        _options[contact.Key].Add(value.Time.Date);
                    }
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.AddedSecurities.OfType<Option>())
            {
                _options.Add(security.Symbol, new HashSet<DateTime>());
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_options.Count != _contracts.Length)
            {
                throw new Exception($"Options weren't setup properly. Expected: {_contracts.Length}");
            }

            foreach (var option in _options)
            {
                for (DateTime date = _realEndDate; date < option.Key.ID.Date; date = date.AddDays(1))
                {
                    if (_exchange.Hours.IsDateOpen(date) &&
                        !option.Value.Contains(date))
                    {
                        throw new Exception("Delisted security should be FF until expiry date");
                    }
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1291113;

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
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
