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
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating FutureOption asset types and requesting history.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="future option" />
    public class BasicTemplateFutureOptionAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2022, 1, 1);
            SetEndDate(2022, 2, 1);
            SetCash(100000);

            var gold_futures = AddFuture(Futures.Metals.Gold, Resolution.Minute);
            gold_futures.SetFilter(0, 180);
            _symbol = gold_futures.Symbol;
            AddFutureOption(_symbol, universe => universe.Strikes(-5, +5)
                                                        .CallsOnly()
                                                        .BackMonth()
                                                        .OnlyApplyFilterAtMarketOpen());

            // Historical Data
            var history = History(_symbol, 60, Resolution.Daily);
            Log($"Received {history.Count()} bars from {_symbol} FutureOption historical data call.");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            // Access Data
            foreach(var kvp in slice.OptionChains)
            {
                var underlyingFutureContract = kvp.Key.Underlying;
                var chain = kvp.Value;

                if (chain.Count() == 0) continue;

                foreach(var contract in chain)
                {
                    Log($@"Canonical Symbol: {kvp.Key}; 
                        Contract: {contract}; 
                        Right: {contract.Right}; 
                        Expiry: {contract.Expiry}; 
                        Bid price: {contract.BidPrice}; 
                        Ask price: {contract.AskPrice}; 
                        Implied Volatility: {contract.ImpliedVolatility}");
                }

                if (!Portfolio.Invested)
                {
                    var atmStrike = chain.OrderBy(x => Math.Abs(chain.Underlying.Price - x.Strike)).First().Strike;
                    var selectedContract = chain.Where(x => x.Strike == atmStrike).OrderByDescending(x => x.Expiry).First();
                    MarketOrder(selectedContract.Symbol, 1);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{Time} {orderEvent.ToString()}");
        }
    }
}
