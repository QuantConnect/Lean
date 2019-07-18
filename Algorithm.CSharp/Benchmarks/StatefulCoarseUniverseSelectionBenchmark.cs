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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    public class StatefulCoarseUniverseSelectionBenchmark : QCAlgorithm
    {
        private const int NumberOfSymbolsCoarse = 250;
        private readonly List<Symbol> _blackList = new List<Symbol>();

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2017, 11, 01);
            SetEndDate(2018, 01, 01);
            SetCash(50000);

            AddUniverse(CoarseSelectionFunction);
        }

        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            // select only symbols with fundamental data and sort descending by daily dollar volume
            var sortedByDollarVolume = coarse
                .Where(x => x.HasFundamentalData)
                .OrderByDescending(x => x.DollarVolume);

            var top = sortedByDollarVolume
                .Where(fundamental => !_blackList.Contains(fundamental.Symbol))
                .Take(NumberOfSymbolsCoarse);

            // we need to return only the symbol objects
            return top.Select(x => x.Symbol);
        }

        public override void OnData(Slice slice)
        {
            if (slice.HasData)
            {
                var symbol = slice.Keys.FirstOrDefault();
                if (symbol != null)
                {
                    if (_blackList.Count > 50)
                    {
                        _blackList.RemoveAt(0);
                    }
                    _blackList.Add(symbol);
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }
            foreach (var security in changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, 0.001m);
            }
        }
    }
}
