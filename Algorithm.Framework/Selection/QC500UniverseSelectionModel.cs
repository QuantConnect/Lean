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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Defines the QC500 universe as a universe selection model for framework algorithm
    /// For details: https://github.com/QuantConnect/Lean/pull/1663
    /// </summary>
    public class QC500UniverseSelectionModel : FundamentalUniverseSelectionModel
    {
        private const int _numberOfSymbolsCoarse = 1000;
        private const int _numberOfSymbolsFine = 500;

        // rebalances at the start of each month
        private int _lastMonth = -1;
        private readonly Dictionary<Symbol, decimal> _dollarVolumeBySymbol = new Dictionary<Symbol, decimal>();

        /// <summary>
        /// Initializes a new default instance of the <see cref="QC500UniverseSelectionModel"/>
        /// </summary>
        public QC500UniverseSelectionModel()
            : base(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QC500UniverseSelectionModel"/>
        /// </summary>
        /// <param name="universeSettings">Universe settings defines what subscription properties will be applied to selected securities</param>
        /// <param name="securityInitializer">Security initializer initializes newly selected securities</param>
        public QC500UniverseSelectionModel(UniverseSettings universeSettings, ISecurityInitializer securityInitializer)
            : base(true, universeSettings, securityInitializer)
        {
        }

        /// <summary>
        /// Performs coarse selection for the QC500 constituents.
        /// The stocks must have fundamental data
        /// The stock must have positive previous-day close price
        /// The stock must have positive volume on the previous trading day
        /// </summary>
        public override IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm, IEnumerable<CoarseFundamental> coarse)
        {
            if (algorithm.Time.Month == _lastMonth)
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

        /// <summary>
        /// Performs fine selection for the QC500 constituents
        /// The company's headquarter must in the U.S.
        /// The stock must be traded on either the NYSE or NASDAQ
        /// At least half a year since its initial public offering
        /// The stock's market cap must be greater than 500 million
        /// </summary>
        public override IEnumerable<Symbol> SelectFine(QCAlgorithm algorithm, IEnumerable<FineFundamental> fine)
        {
            if (algorithm.Time.Month == _lastMonth)
            {
                return Universe.Unchanged;
            }
            _lastMonth = algorithm.Time.Month;

            // The company's headquarter must in the U.S.
            // The stock must be traded on either the NYSE or NASDAQ
            // At least half a year since its initial public offering
            // The stock's market cap must be greater than 500 million
            var filteredFine =
                (from x in fine
                 where x.CompanyReference.CountryId == "USA" &&
                       (x.CompanyReference.PrimaryExchangeID == "NYS" || x.CompanyReference.PrimaryExchangeID == "NAS") &&
                       (algorithm.Time - x.SecurityReference.IPODate).Days > 180 &&
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