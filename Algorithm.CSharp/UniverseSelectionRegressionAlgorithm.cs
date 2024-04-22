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
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Universe Selection regression algorithm simulates an edge case. In one week, Google listed two new symbols, delisted one of them and changed tickers.
    /// </summary>
    /// <meta name="tag" content="regression test" />
    public class UniverseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private HashSet<Symbol> _delistedSymbols = new HashSet<Symbol>();
        private SecurityChanges _changes;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 22);  //Set Start Date
            SetEndDate(2014, 04, 07);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data

            // security that exists with no mappings
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily);
            // security that doesn't exist until half way in backtest (comes in as GOOCV)
            AddSecurity(SecurityType.Equity, "GOOG", Resolution.Daily);

            AddUniverse(coarse =>
            {
                // select the various google symbols over the period
                return from c in coarse
                       let sym = c.Symbol.Value
                       where sym == "GOOG" || sym == "GOOCV" || sym == "GOOAV" || sym == "GOOGL"
                       select c.Symbol;

                // Before March 28th 2014:
                // - Only GOOG  T1AZ164W5VTX existed
                // On March 28th 2014
                // - GOOAV VP83T1ZUHROL and GOOCV VP83T1ZUHROL are listed
                // On April 02nd 2014
                // - GOOAV VP83T1ZUHROL is delisted
                // - GOOG  T1AZ164W5VTX becomes GOOGL
                // - GOOCV VP83T1ZUHROL becomes GOOG
            });
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            // can access the current set of active securitie through UniverseManager.ActiveSecurities
            Log(Time + ": Active Securities: " + string.Join(", ", UniverseManager.ActiveSecurities.Keys));

            // verify we don't receive data for inactive securities
            var inactiveSymbols = data.Keys
                .Where(sym => !UniverseManager.ActiveSecurities.ContainsKey(sym))
                // on daily data we'll get the last data point and the delisting at the same time
                .Where(sym => !data.Delistings.ContainsKey(sym) || data.Delistings[sym].Type != DelistingType.Delisted)
                .ToList();
            if (inactiveSymbols.Any())
            {
                var symbols = string.Join(", ", inactiveSymbols);
                throw new Exception($"Received data for non-active security: {symbols}.");
            }

            if (Transactions.OrdersCount == 0)
            {
                MarketOrder("SPY", 100);
            }

            foreach (var kvp in data.Delistings)
            {
                _delistedSymbols.Add(kvp.Key);
            }

            if (_changes != null && _changes.AddedSecurities.All(x => data.Bars.ContainsKey(x.Symbol)))
            {
                foreach (var security in _changes.AddedSecurities)
                {
                    Log(Time + ": Added Security: " + security.Symbol.ID);
                    MarketOnOpenOrder(security.Symbol, 100);
                }
                foreach (var security in _changes.RemovedSecurities)
                {
                    Log(Time + ": Removed Security: " + security.Symbol.ID);
                    if (!_delistedSymbols.Contains(security.Symbol))
                    {
                        MarketOnOpenOrder(security.Symbol, -100);
                    }
                }
                _changes = null;
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Submitted)
            {
                Log(Time + ": Submitted: " + Transactions.GetOrderById(orderEvent.OrderId));
            }
            if (orderEvent.Status.IsFill())
            {
                Log(Time + ": Filled: " + Transactions.GetOrderById(orderEvent.OrderId));
            }
        }

        public override void OnEndOfAlgorithm()
        {
            foreach (var security in Portfolio.Securities.Values.Where(x => x.Invested))
            {
                // At the end, we should hold 100 shares of:
                // - SPY                (bought on March, 25th 2014),
                // - GOOG  T1AZ164W5VTX (bought on March, 26th 2014),
                // - GOOCV VP83T1ZUHROL (bought on March, 28th 2014).
                AssertQuantity(security, 100);
            }
        }

        private void AssertQuantity(Security security, int expected)
        {
            var actual = security.Holdings.Quantity;
            if (actual != expected)
            {
                var symbol = security.Symbol;
                throw new Exception($"{symbol}({symbol.ID}) expected {expected.ToStringInvariant()}, but received {actual.ToStringInvariant()}.");
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
        public long DataPoints => 78095;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "0.71%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-55.953%"},
            {"Drawdown", "3.700%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "96253.19"},
            {"Net Profit", "-3.747%"},
            {"Sharpe Ratio", "-2.713"},
            {"Sortino Ratio", "-3.067"},
            {"Probabilistic Sharpe Ratio", "13.421%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.266"},
            {"Beta", "1.241"},
            {"Annual Standard Deviation", "0.167"},
            {"Annual Variance", "0.028"},
            {"Information Ratio", "-2.443"},
            {"Tracking Error", "0.124"},
            {"Treynor Ratio", "-0.364"},
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$870000.00"},
            {"Lowest Capacity Asset", "GOOAV VP83T1ZUHROL"},
            {"Portfolio Turnover", "11.16%"},
            {"OrderListHash", "e8691cd69f5bf3381daa86933c7dfc4a"}
        };
    }
}
