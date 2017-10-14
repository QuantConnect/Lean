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

using System;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// A demonstration of consolidating futures data into larger bars for your algorithm.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="consolidating data" />
    /// <meta name="tag" content="futures" />
    public class BasicTemplateFuturesConsolidationAlgorithm : QCAlgorithm
    {
        private const string RootSP500 = Futures.Indices.SP500EMini;
        public Symbol SP500 = QuantConnect.Symbol.Create(RootSP500, SecurityType.Future, Market.USA);
        private HashSet<Symbol> _futureContracts = new HashSet<Symbol>();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 10, 11);
            SetCash(1000000);

            var futureSP500 = AddFuture(RootSP500);
            futureSP500.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));

            SetBenchmark(x => 0);
        }

        public override void OnData(Slice slice)
        {
            foreach (var chain in slice.FutureChains)
            {
                foreach (var contract in chain.Value)
                {
                    if (!_futureContracts.Contains(contract.Symbol))
                    {
                        _futureContracts.Add(contract.Symbol);

                        var consolidator = new QuoteBarConsolidator(TimeSpan.FromMinutes(5));
                        consolidator.DataConsolidated += OnDataConsolidated;
                        SubscriptionManager.AddConsolidator(contract.Symbol, consolidator);

                        Log("Added new consolidator for " + contract.Symbol.Value);
                    }
                }
            }
        }

        public void OnDataConsolidated(object sender, QuoteBar quoteBar)
        {
            Log("OnDataConsolidated called");
            Log(quoteBar.ToString());
        }
    }
}