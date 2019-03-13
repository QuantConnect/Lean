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

using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to estimate constituents of QC500 index based on the company fundamentals
    /// The algorithm creates a default tradable and liquid universe containing 500 US equities 
    /// which are chosen at the first trading day of each month.   
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="coarse universes" />
    /// <meta name="tag" content="fine universes" />
    public class ConstituentsQC500GeneratorAlgorithm : QCAlgorithm
    {
        private const int _numberOfSymbolsCoarse = 1000;
        private const int _numberOfSymbolsFine = 500;

        private readonly Dictionary<Symbol, decimal> _dollarVolumeBySymbol = new Dictionary<Symbol, decimal>();
        private int _lastMonth = -1;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2018, 1, 1);   // Set Start Date
            SetEndDate(2019, 1, 1);     // Set End Date
            SetCash(100000);            // Set Strategy Cash

            // this add universe method accepts two parameters:
            // - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
            // - fine selection function: accepts an IEnumerable<FineFundamental> and returns an IEnumerable<Symbol>
            AddUniverse(CoarseSelectionFunction, FineSelectionFunction);
        }

        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            if (Time.Month == _lastMonth)
            {
                return Universe.Unchanged;
            }

            // The stocks must have fundamental data
            // The stock must have positive previous-day close price
            // The stock must have positive volume on the previous trading day
            var sortedByDollarVolume =
                (from x in coarse
                 where x.HasFundamentalData && x.Volume > 0 && x.Price > 0
                 orderby x.DollarVolume descending
                 select x).Take(_numberOfSymbolsCoarse).ToList();

            _dollarVolumeBySymbol.Clear();
            foreach (var i in sortedByDollarVolume)
            {
                _dollarVolumeBySymbol[i.Symbol] = i.DollarVolume;
            }
            return _dollarVolumeBySymbol.Keys;
        }

        public IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> fine)
        {
            if (Time.Month == _lastMonth)
            {
                return Universe.Unchanged;
            }
            _lastMonth = Time.Month;

            // The company's headquarter must in the U.S.
            // The stock must be traded on either the NYSE or NASDAQ 
            // At least half a year since its initial public offering
            // The stock's market cap must be greater than 500 million
            var filteredFine =
                (from x in fine
                 where x.CompanyReference.CountryId == "USA" &&
                       (x.CompanyReference.PrimaryExchangeID == "NYS" || x.CompanyReference.PrimaryExchangeID == "NAS") &&
                       (Time - x.SecurityReference.IPODate).Days > 180 &&
                       x.EarningReports.BasicAverageShares.ThreeMonths *
                       x.EarningReports.BasicEPS.TwelveMonths * x.ValuationRatios.PERatio > 500000000m
                 select x).ToList();

            var percent = _numberOfSymbolsFine / (double)filteredFine.Count;

            // select stocks with top dollar volume in every single sector 
            var topFineBySector =
                (from x in filteredFine
                 // Group by sector
                 group x by x.CompanyReference.IndustryTemplateCode into g
                 let y = from item in g
                         orderby _dollarVolumeBySymbol[item.Symbol] descending
                         select item
                 let c = (int)Math.Ceiling(y.Count() * percent)
                 select new { g.Key, Value = y.Take(c) }
                 ).ToDictionary(x => x.Key, x => x.Value);

            return topFineBySector.SelectMany(x => x.Value)
                .OrderByDescending(x => _dollarVolumeBySymbol[x.Symbol])
                .Take(_numberOfSymbolsFine)
                .Select(x => x.Symbol);
        }
    }
}