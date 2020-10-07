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
using System.Collections.Concurrent;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// In this algorithm we demonstrate how to perform some technical analysis as
    /// part of your coarse fundamental universe selection
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="coarse universes" />
    public class EmaCrossUniverseSelectionAlgorithm : QCAlgorithm
    {

        private IDictionary<Symbol, SelectionData> averages = new Dictionary<Symbol, SelectionData>();
        private List<Symbol> symbols;
        private bool canTrade = false;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2019, 12, 23);
            SetEndDate(2020, 1, 15);
            SetCash(100000);
            UniverseSettings.Resolution = Resolution.Daily;
            AddUniverse(CoarseSelectionFunction);
        }
        
        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            var sortedByDollarVolume = coarse
                .Where(x => x.Price > 10)
                .OrderByDescending(x => x.DollarVolume)
                .Take(100);
                
            IDictionary<Symbol, CoarseFundamental> selected = new Dictionary<Symbol, CoarseFundamental>();
            foreach(var c in sortedByDollarVolume)
                selected.Add(c.Symbol, c);
            IEnumerable<Symbol> allSymbols = sortedByDollarVolume.Select(x => x.Symbol);
            
            IEnumerable<Symbol> newSymbols = allSymbols.Where(symbol => !averages.ContainsKey(symbol));
            
            if (newSymbols.Count() > 0) {
                var history = History(newSymbols, 200, Resolution.Daily);
                if (history.Count() == 0) {
                    Debug("Empty history on " + Time.ToString() + " for " + newSymbols.ToString());
                    return Universe.Unchanged;
                }
                
                foreach(var symbol in newSymbols) {
                    averages[symbol] = new SelectionData(history, symbol);
                }
            }
            
            symbols = new List<Symbol>();
            
            foreach(KeyValuePair<Symbol, CoarseFundamental> entry in selected) {
                Symbol symbol = entry.Key;
                CoarseFundamental cf = entry.Value;
                SelectionData symbolData = averages[symbol];
                
                if (!newSymbols.Contains(symbol))
                    symbolData.Update(cf.EndTime, cf.AdjustedPrice);
                    
                if (symbolData.IsReady() & symbolData.fast > symbolData.slow)
                    symbols.Add(symbol);
            }
            
            return symbols;
        }
        
        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Object containing AddedSecurities and RemovedSecurities</param>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach(var security in changes.RemovedSecurities)
                Liquidate(security.Symbol, "Removed from Universe");

            canTrade = true;
        }
        

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars dictionary object keyed by symbol containing the stock data</param>
        public void OnData(TradeBars data)
        {
            if (canTrade) {
                int length = symbols.Count;
                
                foreach(var symbol in symbols)
                    SetHoldings(symbol, 1.0 / length);
                
                canTrade = false;
            }
        }
        
        
        public class SelectionData
        {
            public ExponentialMovingAverage slow = new ExponentialMovingAverage(200);
            public ExponentialMovingAverage fast = new ExponentialMovingAverage(50);
            
            public SelectionData(IEnumerable<Slice> history, Symbol symbol){
                foreach(var slice in history)
                    if (slice.ContainsKey(symbol))
                        Update(slice.Time, slice.Bars[symbol].Close);
            }
            
            public void Update(DateTime time, decimal price) {
                fast.Update(time, price);
                slow.Update(time, price);
            }
            
            public bool IsReady() {
                return slow.IsReady & fast.IsReady;
            }
        }
    }
    
}