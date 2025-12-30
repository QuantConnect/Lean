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

using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Securities.Future;
using QuantConnect.Util;
using System;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    public class DatabentoFuturesTestAlgorithm : QCAlgorithm
    {
        private Future _es;

        public override void Initialize()
        {
            Log("Algorithm Initialize");

            SetStartDate(2025, 10, 1);
            SetEndDate(2025, 10, 16);
            SetCash(100000);

            var exp = new DateTime(2025, 12, 19);
            var symbol = QuantConnect.Symbol.CreateFuture("ES", Market.CME, exp);
            //_es = AddFutureContract(symbol, Resolution.Tick, true, 1, true);
            _es = AddFutureContract(symbol, Resolution.Second, true, 1, true);
            Log($"_es: {_es}");

            var history = History<TradeBar>(_es.Symbol, 10, Resolution.Minute).ToList();
            
            Log($"History returned {history.Count} bars");
            
            foreach (var bar in history)
            {
                Log($"History Bar: {bar.Time} - O:{bar.Open} H:{bar.High} L:{bar.Low} C:{bar.Close} V:{bar.Volume}");
            }
            
        }

        public override void OnData(Slice slice)
        {
            if (!slice.HasData)
            {
                Log("Slice has no data");
                return;
            }
            
            Log($"OnData: Slice has {slice.Count} data points");

            // For Tick resolution, check Ticks collection
            if (slice.Ticks.ContainsKey(_es.Symbol))
            {
                var ticks = slice.Ticks[_es.Symbol];
                Log($"Received {ticks.Count} ticks for {_es.Symbol}");

                foreach (var tick in ticks)
                {
                    if (tick.TickType == TickType.Trade)
                    {
                        Log($"Trade Tick - Price: {tick.Price}, Quantity: {tick.Quantity}, Time: {tick.Time}");
                    }
                    else if (tick.TickType == TickType.Quote)
                    {
                        Log($"Quote Tick - Bid: {tick.BidPrice}x{tick.BidSize}, Ask: {tick.AskPrice}x{tick.AskSize}, Time: {tick.Time}");
                    }
                }
            }

            // Access OHLCV bars
            foreach (var bar in slice.Bars.Values)
            {
                Log($"OHLCV BAR: {bar.Symbol.Value} - O: {bar.Open}, H: {bar.High}, L: {bar.Low}, C: {bar.Close}, V: {bar.Volume}");
            }
        }
    }
}
