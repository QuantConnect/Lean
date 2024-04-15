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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Tests.Common.Data.Fundamental;
using System;
using System.Collections.Generic;
using System.Linq;
using static QuantConnect.Data.UniverseSelection.CoarseFundamentalDataProvider;

namespace QuantConnect.Tests.Algorithm.Framework.Selection
{
    [TestFixture]
    public class QC500UniverseSelectionModelTests
    {
        private readonly Dictionary<char, string> _industryTemplateCodeDict =
            new Dictionary<char, string>
            {
                {'0', "B"}, {'1', "I"}, {'2', "M"}, {'3', "N"}, {'4', "T"}, {'5', "U"}
            };

        private readonly List<Symbol> _symbols = Enumerable.Range(0, 6000)
            .Select(x => Symbol.Create($"{x:0000}", SecurityType.Equity, Market.USA))
            .ToList();

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void FiltersUniverseCorrectlyWithValidData(Language language)
        {
            QCAlgorithm algorithm;
            Dictionary<DateTime, int> coarseCountByDateTime;
            Dictionary<DateTime, int> fineCountByDateTime;

            RunSimulation(language,
                (symbol, time) => new CoarseFundamentalSource
                {
                    Symbol = symbol,
                    EndTime = time,
                    Value = 100,
                    VolumeSetter = 1000,
                    DollarVolumeSetter = 100000 * double.Parse(symbol.Value.Substring(3)),
                    HasFundamentalDataSetter = true
                },
                (symbol, time) => new FineFundamental(time, symbol)
                {
                    Value = 100
                },
                new TestFundamentalDataProvider(_industryTemplateCodeDict),
                out algorithm,
                out coarseCountByDateTime,
                out fineCountByDateTime);

            var months = (algorithm.EndDate - algorithm.StartDate).Days / 30;
            // Universe Changed 4 times
            Assert.AreEqual(months, coarseCountByDateTime.Count);
            Assert.AreEqual(months, fineCountByDateTime.Count);
            // Universe Changed on the 1st. Coarse returned 1000 and Fine 500
            Assert.IsTrue(coarseCountByDateTime.All(kvp => kvp.Key.Day == 1 && kvp.Value == 1000));
            Assert.IsTrue(fineCountByDateTime.All(kvp => kvp.Key.Day == 1 && kvp.Value == 500));
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DoesNotFilterUniverseWithCoarseDataHasFundamentalFalse(Language language)
        {
            QCAlgorithm algorithm;
            Dictionary<DateTime, int> coarseCountByDateTime;
            Dictionary<DateTime, int> fineCountByDateTime;

            RunSimulation(language,
                (symbol, time) => new CoarseFundamentalSource
                {
                    Symbol = symbol,
                    EndTime = time,
                },
                (symbol, time) => new FineFundamental(time, symbol),
                new TestFundamentalDataProvider(_industryTemplateCodeDict),
                out algorithm,
                out coarseCountByDateTime,
                out fineCountByDateTime); ;

            // No Universe Changes
            Assert.AreEqual(0, coarseCountByDateTime.Count);
            Assert.AreEqual(0, fineCountByDateTime.Count);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DoesNotFilterUniverseWithInvalidFineData(Language language)
        {
            QCAlgorithm algorithm;
            Dictionary<DateTime, int> coarseCountByDateTime;
            Dictionary<DateTime, int> fineCountByDateTime;

            RunSimulation(language,
                (symbol, time) => new CoarseFundamentalSource
                {
                    Symbol = symbol,
                    EndTime = time,
                    Value = 100,
                    VolumeSetter = 1000,
                    DollarVolumeSetter = 100000 * double.Parse(symbol.Value.Substring(3)),
                    HasFundamentalDataSetter = true
                },
                (symbol, time) => new FineFundamental(time, symbol)
                {
                    Value = 100
                },
                new NullFundamentalDataProvider(),
                out algorithm,
                out coarseCountByDateTime,
                out fineCountByDateTime);

            // Coarse Fundamental called every day.
            Assert.Greater(coarseCountByDateTime.Count, 4);
            Assert.IsTrue(coarseCountByDateTime.All(kvp => kvp.Value == 1000));
            // No Universe Changes
            Assert.AreEqual(0, fineCountByDateTime.Count);
        }

        private void RunSimulation(Language language,
            Func<Symbol, DateTime, CoarseFundamental> getCoarseFundamental,
            Func<Symbol, DateTime, FineFundamental> getFineFundamental,
            IFundamentalDataProvider fundamentalDataProvider,
            out QCAlgorithm algorithm,
            out Dictionary<DateTime, int> coarseCountByDateTime,
            out Dictionary<DateTime, int> fineCountByDateTime)
        {
            algorithm = new QCAlgorithm();
            algorithm.SetStartDate(2019, 10, 1);
            algorithm.SetEndDate(2020, 1, 30);
            algorithm.SetDateTime(algorithm.StartDate.AddHours(6));

            FundamentalService.Initialize(TestGlobals.DataProvider, fundamentalDataProvider, false);

            coarseCountByDateTime = new Dictionary<DateTime, int>();
            fineCountByDateTime = new Dictionary<DateTime, int>();

            Func<QCAlgorithm, IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> SelectCoarse;
            Func<QCAlgorithm, IEnumerable<FineFundamental>, IEnumerable<Symbol>> SelectFine;
            GetUniverseSelectionModel(language, out SelectCoarse, out SelectFine);

            while (algorithm.EndDate > algorithm.UtcTime)
            {
                var time = algorithm.UtcTime;

                var coarse = _symbols.Select(x => getCoarseFundamental(x, time));

                var selectSymbolsResult = SelectCoarse(algorithm, coarse);

                if (!ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
                {
                    coarseCountByDateTime[time] = selectSymbolsResult.Count();

                    var fine = selectSymbolsResult.Select(x => getFineFundamental(x, time));

                    selectSymbolsResult = SelectFine(algorithm, fine);
                    if (!ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
                    {
                        fineCountByDateTime[time] = selectSymbolsResult.Count();
                    }
                }

                algorithm.SetDateTime(time.AddDays(1));
            }
        }

        private void GetUniverseSelectionModel(
            Language language,
            out Func<QCAlgorithm, IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> SelectCoarse,
            out Func<QCAlgorithm, IEnumerable<FineFundamental>, IEnumerable<Symbol>> SelectFine)
        {
            if (language == Language.CSharp)
            {
                var model = new QC500UniverseSelectionModel();
                SelectCoarse = model.SelectCoarse;
                SelectFine = model.SelectFine;
                return;
            }

            using (Py.GIL())
            {
                var name = "QC500UniverseSelectionModel";
                dynamic model = Py.Import(name).GetAttr(name).Invoke();
                SelectCoarse = ConvertToUniverseSelectionSymbolDelegate<IEnumerable<CoarseFundamental>>(model.select_coarse);
                SelectFine = ConvertToUniverseSelectionSymbolDelegate<IEnumerable<FineFundamental>>(model.select_fine);
            }
        }

        public static Func<QCAlgorithm, T, IEnumerable<Symbol>> ConvertToUniverseSelectionSymbolDelegate<T>(PyObject pySelector)
        {
            Func<QCAlgorithm, T, object> selector;
            pySelector.TryConvertToDelegate(out selector);

            return (algorithm, data) =>
            {
                var result = selector(algorithm, data);
                return ReferenceEquals(result, Universe.Unchanged)
                    ? Universe.Unchanged
                    : ((object[])result).Select(x => (Symbol)x);
            };
        }

        private class TestFundamentalDataProvider : IFundamentalDataProvider
        {
            private readonly Dictionary<char, string> _industryTemplateCodeDict;

            public TestFundamentalDataProvider(Dictionary<char, string> industryTemplateCodeDict)
            {
                _industryTemplateCodeDict = industryTemplateCodeDict;
            }
            public T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty name)
            {
                if (securityIdentifier == SecurityIdentifier.Empty)
                {
                    return default;
                }
                return Get(time, securityIdentifier, name);
            }

            private dynamic Get(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty enumName)
            {
                var name = Enum.GetName(enumName);
                switch (name)
                {
                    case "CompanyProfile_MarketCap":
                        return 500000001;
                    case "SecurityReference_IPODate":
                        return time.AddDays(-200);
                    case "EarningReports_BasicAverageShares_ThreeMonths":
                        return 5000000.01d;
                    case "CompanyReference_CountryId":
                        return "USA";
                    case "CompanyReference_PrimaryExchangeID":
                        return "NYS";
                    case "CompanyReference_IndustryTemplateCode":
                        return _industryTemplateCodeDict[securityIdentifier.Symbol[0]];
                }
                return null;
            }

            public void Initialize(IDataProvider dataProvider, bool liveMode)
            {
            }
        }
    }
}
