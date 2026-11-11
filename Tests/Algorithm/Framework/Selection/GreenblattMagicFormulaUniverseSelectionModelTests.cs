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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp.Alphas;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using static QuantConnect.Data.UniverseSelection.CoarseFundamentalDataProvider;

namespace QuantConnect.Tests.Algorithm.Framework.Selection
{
    [TestFixture]
    public class GreenblattMagicFormulaUniverseSelectionModelTests
    {
        // Morningstar codes the NYSE as "NYS" in older data and "NYSE" in current data
        [TestCase(Language.CSharp, "NYSE", true)]
        [TestCase(Language.Python, "NYSE", true)]
        [TestCase(Language.CSharp, "NYS", true)]
        [TestCase(Language.Python, "NYS", true)]
        [TestCase(Language.CSharp, "NAS", true)]
        [TestCase(Language.Python, "NAS", true)]
        [TestCase(Language.CSharp, "TSX", false)]
        [TestCase(Language.Python, "TSX", false)]
        [TestCase(Language.CSharp, "", false)]
        [TestCase(Language.Python, "", false)]
        [TestCase(Language.CSharp, null, false)]
        [TestCase(Language.Python, null, false)]
        public void FiltersFineByPrimaryExchange(Language language, string primaryExchangeId, bool selects)
        {
            var stocks = Enumerable.Range(1, 10)
                .Select(i => new TestStock($"{i:0000}", primaryExchangeId, "N", 100000 * i, i, 1d / i))
                .ToList();

            var selected = Select(language, stocks);

            Assert.AreEqual(selects ? 10 : 0, selected.Count);
        }

        // S01 has both the lowest EV/EBITDA and the highest ROA, so each stock's combined rank grows with its number
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void SelectsLowestCombinedRankAcrossIndustries(Language language)
        {
            var stocks = Enumerable.Range(1, 21)
                .Select(i => new TestStock($"S{i:00}", "NAS", i <= 10 ? "N" : "M", 1e6, i, (22 - i) / 100d))
                .ToList();

            var selected = Select(language, stocks);

            CollectionAssert.AreEquivalent(Enumerable.Range(1, 10).Select(i => $"S{i:00}"), selected);
        }

        // Stocks without EV/EBITDA or ROA, such as banks, can't be ranked by the Magic Formula
        [TestCase(Language.CSharp, true)]
        [TestCase(Language.Python, true)]
        [TestCase(Language.CSharp, false)]
        [TestCase(Language.Python, false)]
        public void SkipsStocksMissingAFactor(Language language, bool missingEVToEBITDA)
        {
            var stocks = Enumerable.Range(1, 21)
                .Select(i =>
                {
                    var missing = i is 12 or 16;
                    var industry = missing ? "B" : i <= 10 ? "N" : "M";
                    var evToEbitda = missing && missingEVToEBITDA ? double.NaN : i;
                    var forwardRoa = missing && !missingEVToEBITDA ? double.NaN : (22 - i) / 100d;
                    return new TestStock($"S{i:00}", "NAS", industry, 1e6, evToEbitda, forwardRoa);
                })
                .ToList();

            var selected = Select(language, stocks);

            CollectionAssert.AreEquivalent(Enumerable.Range(1, 10).Select(i => $"S{i:00}"), selected);
        }

        // N01-N10 and M01-M10 tie on one factor (or both) and N stocks are better on the other. Tied stocks share a rank,
        // and equal combined ranks are ordered by symbol, so neither the industry nor the input order changes the selection
        [TestCase(Language.CSharp, "ForwardROA", true, "N")]
        [TestCase(Language.Python, "ForwardROA", true, "N")]
        [TestCase(Language.CSharp, "ForwardROA", false, "N")]
        [TestCase(Language.Python, "ForwardROA", false, "N")]
        [TestCase(Language.CSharp, "EVToEBITDA", true, "N")]
        [TestCase(Language.Python, "EVToEBITDA", true, "N")]
        [TestCase(Language.CSharp, "EVToEBITDA", false, "N")]
        [TestCase(Language.Python, "EVToEBITDA", false, "N")]
        [TestCase(Language.CSharp, "Both", true, "M")]
        [TestCase(Language.Python, "Both", true, "M")]
        [TestCase(Language.CSharp, "Both", false, "M")]
        [TestCase(Language.Python, "Both", false, "M")]
        public void BreaksTiesIndependentlyOfInputOrder(Language language, string tiedFactor, bool industryMFirst, string expectedIndustry)
        {
            var industries = industryMFirst ? new[] { "M", "N" } : new[] { "N", "M" };
            var stocks = industries
                .SelectMany(industry => Enumerable.Range(1, 10).Select(i =>
                {
                    var rank = industry == "N" ? i : 10 + i;
                    var evToEbitda = tiedFactor is "EVToEBITDA" or "Both" ? 10 : rank;
                    var forwardRoa = tiedFactor is "ForwardROA" or "Both" ? 0.1 : (21 - rank) / 100d;
                    return new TestStock($"{industry}{i:00}", "NAS", industry, 1e6, evToEbitda, forwardRoa);
                }))
                .ToList();

            var selected = Select(language, stocks);

            CollectionAssert.AreEquivalent(Enumerable.Range(1, 10).Select(i => $"{expectedIndustry}{i:00}"), selected);
        }

        // Neither factor alone picks the stocks with the lowest sum of both ranks:
        // S01-S05 and S11-S15 score 12, 14, 16, 18 and 20, and the next stocks score 22
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void SelectsBySumOfFactorRanks(Language language)
        {
            var stocks = Enumerable.Range(1, 20)
                .Select(i => new TestStock($"S{i:00}", "NAS", "N", 1e6, i, (i <= 10 ? 11 - i : 31 - i) / 100d))
                .ToList();

            var selected = Select(language, stocks);

            var expected = Enumerable.Range(1, 5).Concat(Enumerable.Range(11, 5)).Select(i => $"S{i:00}");
            CollectionAssert.AreEquivalent(expected, selected);
        }

        private static List<string> Select(Language language, List<TestStock> stocks)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetDateTime(new DateTime(2025, 1, 2, 6, 0, 0));
            FundamentalService.Initialize(TestGlobals.DataProvider, new TestFundamentalDataProvider(stocks), false);

            GetUniverseSelectionModel(language, out var selectCoarse, out var selectFine);

            var time = algorithm.UtcTime;
            var coarse = stocks.Select(stock => new CoarseFundamentalSource
            {
                Symbol = Symbol.Create(stock.Ticker, SecurityType.Equity, Market.USA),
                EndTime = time,
                Value = 100,
                VolumeSetter = 1000,
                DollarVolumeSetter = stock.DollarVolume,
                HasFundamentalDataSetter = true
            });
            var fine = selectCoarse(algorithm, coarse).Select(symbol => new FineFundamental(time, symbol));
            return selectFine(algorithm, fine).Select(symbol => symbol.Value).ToList();
        }

        private static void GetUniverseSelectionModel(
            Language language,
            out Func<QCAlgorithm, IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> selectCoarse,
            out Func<QCAlgorithm, IEnumerable<FineFundamental>, IEnumerable<Symbol>> selectFine)
        {
            if (language == Language.CSharp)
            {
                var alpha = new GreenblattMagicFormulaAlpha();
                alpha.Initialize();
                var model = (FundamentalUniverseSelectionModel)alpha.UniverseSelection;
                selectCoarse = model.SelectCoarse;
                selectFine = model.SelectFine;
                return;
            }

            using (Py.GIL())
            {
                dynamic model = Py.Import("Alphas.GreenblattMagicFormulaAlpha").GetAttr("GreenBlattMagicFormulaUniverseSelectionModel").Invoke();
                selectCoarse = QC500UniverseSelectionModelTests.ConvertToUniverseSelectionSymbolDelegate<IEnumerable<CoarseFundamental>>(model.select_coarse);
                selectFine = QC500UniverseSelectionModelTests.ConvertToUniverseSelectionSymbolDelegate<IEnumerable<FineFundamental>>(model.select_fine);
            }
        }

        private record TestStock(string Ticker, string PrimaryExchangeId, string IndustryTemplateCode, double DollarVolume, double EVToEBITDA, double ForwardROA);

        private class TestFundamentalDataProvider : IFundamentalDataProvider
        {
            private readonly Dictionary<string, TestStock> _stocksByTicker;

            public TestFundamentalDataProvider(IEnumerable<TestStock> stocks)
            {
                _stocksByTicker = stocks.ToDictionary(x => x.Ticker);
            }

            public T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty name)
            {
                if (securityIdentifier == SecurityIdentifier.Empty)
                {
                    return default;
                }
                return Get(time, _stocksByTicker[securityIdentifier.Symbol], name) is T value ? value : default;
            }

            private static object Get(DateTime time, TestStock stock, FundamentalProperty name)
            {
                switch (name)
                {
                    case FundamentalProperty.CompanyReference_CountryId:
                        return "USA";
                    case FundamentalProperty.CompanyReference_PrimaryExchangeID:
                        return stock.PrimaryExchangeId;
                    case FundamentalProperty.CompanyReference_IndustryTemplateCode:
                        return stock.IndustryTemplateCode;
                    case FundamentalProperty.SecurityReference_IPODate:
                        return time.AddDays(-200);
                    case FundamentalProperty.EarningReports_BasicAverageShares_ThreeMonths:
                        return 100000000d;
                    case FundamentalProperty.EarningReports_BasicEPS_TwelveMonths:
                        return 1d;
                    case FundamentalProperty.ValuationRatios_PERatio:
                        return 10d;
                    case FundamentalProperty.ValuationRatios_EVToEBITDA:
                        return stock.EVToEBITDA;
                    case FundamentalProperty.ValuationRatios_ForwardROA:
                        return stock.ForwardROA;
                }
                return null;
            }

            public void Initialize(IDataProvider dataProvider, bool liveMode)
            {
            }
        }
    }
}
