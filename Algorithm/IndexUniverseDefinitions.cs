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

using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Provides helpers for defining universes based on index definitions
    /// </summary>
    public class IndexUniverseDefinitions
    {
        private readonly QCAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexUniverseDefinitions"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance, used for obtaining the default <see cref="UniverseSettings"/></param>
        public IndexUniverseDefinitions(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Creates a new fine universe that contains the constituents of QC500 index based onthe company fundamentals
        /// The algorithm creates a default tradable and liquid universe containing 500 US equities
        /// which are chosen at the first trading day of each month.
        /// </summary>
        /// <returns>A new coarse universe for the top count of stocks by dollar volume</returns>
        public Universe QC500
        {
            get
            {
                var lastMonth = -1;
                var numberOfSymbolsCoarse = 1000;
                var numberOfSymbolsFine = 500;
                var dollarVolumeBySymbol = new Dictionary<Symbol, decimal>();
                var symbol = Symbol.Create("qc-500", SecurityType.Equity, Market.USA);

                var coarseUniverse = new CoarseFundamentalUniverse(
                    symbol,
                    _algorithm.UniverseSettings,
                    _algorithm.SecurityInitializer,
                    coarse =>
                    {
                        if (_algorithm.Time.Month == lastMonth)
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
                                 select x).Take(numberOfSymbolsCoarse).ToList();

                        dollarVolumeBySymbol.Clear();
                        foreach (var i in sortedByDollarVolume)
                        {
                            dollarVolumeBySymbol[i.Symbol] = i.DollarVolume;
                        }
                        return dollarVolumeBySymbol.Keys;
                    });

                return new FineFundamentalFilteredUniverse(
                    coarseUniverse,
                    fine =>
                    {
                        if (_algorithm.Time.Month == lastMonth)
                        {
                            return Universe.Unchanged;
                        }
                        lastMonth = _algorithm.Time.Month;

                        // The company's headquarter must in the U.S.
                        // The stock must be traded on either the NYSE or NASDAQ 
                        // At least half a year since its initial public offering
                        // The stock's market cap must be greater than 500 million
                        var filteredFine =
                                (from x in fine
                                 where x.CompanyReference.CountryId == "USA" &&
                                       (x.CompanyReference.PrimaryExchangeID == "NYS" || x.CompanyReference.PrimaryExchangeID == "NAS") &&
                                       (_algorithm.Time - x.SecurityReference.IPODate).Days > 180 &&
                                       x.EarningReports.BasicAverageShares.ThreeMonths *
                                       x.EarningReports.BasicEPS.TwelveMonths * x.ValuationRatios.PERatio > 500000000m
                                 select x).ToList();

                        var percent = numberOfSymbolsFine / (double)filteredFine.Count;

                        // select stocks with top dollar volume in every single sector 
                        var topFineBySector =
                                (from x in filteredFine
                                 // Group by sector
                                 group x by x.CompanyReference.IndustryTemplateCode into g
                                 let y = from item in g
                                         orderby dollarVolumeBySymbol[item.Symbol] descending
                                         select item
                                 let c = (int)Math.Ceiling(y.Count() * percent)
                                 select new { g.Key, Value = y.Take(c) }
                                 ).ToDictionary(x => x.Key, x => x.Value);

                        return topFineBySector.SelectMany(x => x.Value)
                            .OrderByDescending(x => dollarVolumeBySymbol[x.Symbol])
                            .Take(numberOfSymbolsFine)
                            .Select(x => x.Symbol);
                    });
            }
        }
    }
}