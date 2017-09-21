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
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Randomly selects the specified number of symbols from the lists below
    /// </summary>
    /// <meta name="tag" content="regression test" />
    public class StressSymbolsAlgorithm : QCAlgorithm
    {
        public const int TickSymbolsToRun = 0;
        public const int SecondSymbolsToRun = 0;
        public const int MinuteSymbolsToRun = 0;
        public const int HourSymbolsToRun = 0;
        public const int DailySymbolsToRun = 1000;

        /// <summary>
        /// Add Hundreds of Stock and Forex Symbol
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2001, 10, 07);
            SetEndDate(2010, 10, 11);
            SetCash(250000);

            var allSymbols = StressSymbols.StockSymbols.ToList();//.Concat(ForexSymbols).ToList();
            if (TickSymbolsToRun + SecondSymbolsToRun + HourSymbolsToRun + DailySymbolsToRun > allSymbols.Count)
            {
                throw new Exception("Too many symbols, all symbols: " + allSymbols.Count);
            }


            var hash = new HashSet<string> {"DNY", "MLNK"};
            var ticks = GetRandomSymbols(allSymbols, hash, TickSymbolsToRun).ToList();
            var seconds = GetRandomSymbols(allSymbols, hash, SecondSymbolsToRun).ToList();
            var minutes = GetRandomSymbols(allSymbols, hash, MinuteSymbolsToRun).ToList();
            var hours = GetRandomSymbols(allSymbols, hash, HourSymbolsToRun).ToList();
            var daily = GetRandomSymbols(allSymbols, hash, DailySymbolsToRun).ToList();

            AddSecurity(ticks, Resolution.Tick);
            AddSecurity(seconds, Resolution.Second);
            AddSecurity(minutes, Resolution.Minute);
            AddSecurity(hours, Resolution.Hour);
            AddSecurity(daily, Resolution.Daily);

            //SetUniverse(coarse => coarse.Take(1));
        }

        private void AddSecurity(IEnumerable<string> symbols, Resolution resolution)
        {
            foreach (var symbol in symbols)
            {
                var securityType = StressSymbols.ForexSymbols.Contains(symbol) ? SecurityType.Forex : SecurityType.Equity;
                AddSecurity(securityType, symbol, resolution);
            }
        }

        private IEnumerable<string> GetRandomSymbols(List<string> allSymbols, HashSet<string> hash, int numberOfSymbols)
        {
            return Enumerable.Range(0, numberOfSymbols).Select(x => GetRandomItem(allSymbols, hash));
        }

        private readonly Random _random = new Random();
        private string GetRandomItem(IReadOnlyList<string> list, HashSet<string> hash)
        {
            var count = 0;
            string item;
            do
            {
                item = list[_random.Next(list.Count)];
                count++;
            }
            while (!hash.Add(item) && count < list.Count*2);
            return item;
        }

        /// <summary>
        /// TradeBar data event handler
        /// </summary>
        public void OnData(TradeBars data)
        {

        }
    }
}