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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{

    public class CoarseFineUniverseSelectionBenchmark : QCAlgorithm
    {
        private const int NumberOfSymbolsCoarse = 150;
        private const int NumberOfSymbolsFine = 40;

        private SecurityChanges _changes = SecurityChanges.None;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2017, 11, 01);
            SetEndDate(2018, 01, 01);
            SetCash(50000);

            AddUniverse(CoarseSelectionFunction, FineSelectionFunction);
        }

        // sort the data by daily dollar volume and take the top 'NumberOfSymbolsCoarse'
        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            // select only symbols with fundamental data and sort descending by daily dollar volume
            var sortedByDollarVolume = coarse
                .Where(x => x.HasFundamentalData)
                .OrderByDescending(x => x.DollarVolume);

            // take the top entries from our sorted collection
            var top5 = sortedByDollarVolume.Take(NumberOfSymbolsCoarse);

            // we need to return only the symbol objects
            return top5.Select(x => x.Symbol);
        }

        // sort the data by P/E ratio and take the top 'NumberOfSymbolsFine'
        public IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> fine)
        {
            // sort descending by P/E ratio
            var sortedByPeRatio = fine.OrderByDescending(x => x.ValuationRatios.PERatio);

            // take the top entries from our sorted collection
            var topFine = sortedByPeRatio.Take(NumberOfSymbolsFine);

            // we need to return only the symbol objects
            return topFine.Select(x => x.Symbol);
        }

        //Data Event Handler: New data arrives here. "TradeBars" type is a dictionary of strings so you can access it by symbol.
        public void OnData(TradeBars data)
        {
            // if we have no changes, do nothing
            if (_changes == SecurityChanges.None) return;

            // liquidate removed securities
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            // we want allocation in each security in our universe
            foreach (var security in _changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, 0.02m);
            }

            _changes = SecurityChanges.None;
        }

        // this event fires whenever we have changes to our universe
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
        }
    }
}