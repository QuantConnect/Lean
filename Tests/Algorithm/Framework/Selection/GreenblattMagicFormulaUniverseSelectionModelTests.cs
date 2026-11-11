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
        private readonly List<Symbol> _symbols = Enumerable.Range(1, 10)
            .Select(x => Symbol.Create($"{x:0000}", SecurityType.Equity, Market.USA))
            .ToList();

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
            var algorithm = new QCAlgorithm();
            algorithm.SetDateTime(new DateTime(2025, 1, 2, 6, 0, 0));
            FundamentalService.Initialize(TestGlobals.DataProvider, new TestFundamentalDataProvider(primaryExchangeId), false);

            GetUniverseSelectionModel(language, out var selectCoarse, out var selectFine);

            var time = algorithm.UtcTime;
            var coarse = _symbols.Select(symbol => new CoarseFundamentalSource
            {
                Symbol = symbol,
                EndTime = time,
                Value = 100,
                VolumeSetter = 1000,
                DollarVolumeSetter = 100000 * double.Parse(symbol.Value),
                HasFundamentalDataSetter = true
            });
            var fine = selectCoarse(algorithm, coarse).Select(symbol => new FineFundamental(time, symbol));
            var selected = selectFine(algorithm, fine).ToList();

            Assert.AreEqual(selects ? 10 : 0, selected.Count);
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

        private class TestFundamentalDataProvider : IFundamentalDataProvider
        {
            private readonly string _primaryExchangeId;

            public TestFundamentalDataProvider(string primaryExchangeId)
            {
                _primaryExchangeId = primaryExchangeId;
            }

            public T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty name)
            {
                if (securityIdentifier == SecurityIdentifier.Empty)
                {
                    return default;
                }
                return Get(time, name) is T value ? value : default;
            }

            private object Get(DateTime time, FundamentalProperty name)
            {
                switch (name)
                {
                    case FundamentalProperty.CompanyReference_CountryId:
                        return "USA";
                    case FundamentalProperty.CompanyReference_PrimaryExchangeID:
                        return _primaryExchangeId;
                    case FundamentalProperty.CompanyReference_IndustryTemplateCode:
                        return "N";
                    case FundamentalProperty.SecurityReference_IPODate:
                        return time.AddDays(-200);
                    case FundamentalProperty.EarningReports_BasicAverageShares_ThreeMonths:
                        return 100000000d;
                    case FundamentalProperty.EarningReports_BasicEPS_TwelveMonths:
                        return 1d;
                    case FundamentalProperty.ValuationRatios_PERatio:
                        return 10d;
                }
                return null;
            }

            public void Initialize(IDataProvider dataProvider, bool liveMode)
            {
            }
        }
    }
}
