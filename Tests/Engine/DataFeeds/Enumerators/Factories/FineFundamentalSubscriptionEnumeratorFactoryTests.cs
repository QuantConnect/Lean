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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Securities;
using QuantConnect.Util;
using Log = QuantConnect.Logging.Log;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators.Factories
{
    [TestFixture]
    public class FineFundamentalSubscriptionEnumeratorFactoryTests
    {
        [Parallelizable(ParallelScope.Self)]
        [TestCaseSource(nameof(GetFineFundamentalTestParametersBacktest))]
        [TestCaseSource(nameof(GetFineFundamentalTestParametersLive))]
        public void ReadsFineFundamental(FineFundamentalTestParameters parameters)
        {
            var stopwatch = Stopwatch.StartNew();
            var rows = new List<FineFundamental>();

            var config = new SubscriptionDataConfig(typeof(FineFundamental), parameters.Symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            var request = new SubscriptionRequest(false, null, security, config, parameters.StartDate, parameters.EndDate);
            var fileProvider = new DefaultDataProvider();

            var factory = new FineFundamentalSubscriptionEnumeratorFactory(parameters.LiveMode);
            var enumerator = factory.CreateEnumerator(request, fileProvider);
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current as FineFundamental;
                rows.Add(current);
            }

            stopwatch.Stop();
            Log.Trace("Total rows: {0}, elapsed time: {1}", rows.Count, stopwatch.Elapsed);

            Assert.AreEqual(parameters.RowCount, rows.Count);

            if (parameters.RowCount != 1) return;

            var row = rows[0];
            Assert.AreEqual(parameters.CompanyShortName, row.CompanyReference.ShortName);
            Assert.AreEqual(parameters.Symbol, row.Symbol);
            Assert.IsTrue(row.CompanyReference.PrimarySymbol == parameters.Symbol.Value || row.CompanyReference.PrimarySymbol == null);
            Assert.IsTrue(row.SecurityReference.SecuritySymbol == parameters.Symbol.Value || row.SecurityReference.SecuritySymbol == null);
            Assert.AreEqual(parameters.Ebitda3M, row.FinancialStatements.IncomeStatement.EBITDA.ThreeMonths);
            Assert.AreEqual(parameters.Ebitda12M, row.FinancialStatements.IncomeStatement.EBITDA.TwelveMonths);
            Assert.AreEqual(parameters.Ebitda12M, (decimal)row.FinancialStatements.IncomeStatement.EBITDA);
            Assert.AreEqual(parameters.CostOfRevenue3M, row.FinancialStatements.IncomeStatement.CostOfRevenue.ThreeMonths);
            Assert.AreEqual(parameters.CostOfRevenue12M, row.FinancialStatements.IncomeStatement.CostOfRevenue.TwelveMonths);
            Assert.AreEqual(parameters.CostOfRevenue12M, (decimal)row.FinancialStatements.IncomeStatement.CostOfRevenue);
            Assert.AreEqual(parameters.EquityPerShareGrowth1Y, row.EarningRatios.EquityPerShareGrowth.OneYear);
            Assert.AreEqual(parameters.EquityPerShareGrowth1Y, (decimal)row.EarningRatios.EquityPerShareGrowth);
            Assert.AreEqual(parameters.PeRatio, row.ValuationRatios.PERatio);
            if (!parameters.FinancialHealthGrade.IsNullOrEmpty())
            {
                Assert.AreEqual(parameters.FinancialHealthGrade, row.AssetClassification.FinancialHealthGrade);
            }
            enumerator.Dispose();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void DeserializesAssetClassificationAndCompanyProfile()
        {
            var symbol = Symbols.AAPL;
            var startDate = new DateTime(2014, 4, 30);
            var endDate = new DateTime(2014, 4, 30);

            var config = new SubscriptionDataConfig(typeof(FineFundamental), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            var request = new SubscriptionRequest(false, null, security, config, startDate, endDate);
            var fileProvider = new DefaultDataProvider();
            var factory = new FineFundamentalSubscriptionEnumeratorFactory(false);
            var enumerator = factory.CreateEnumerator(request, fileProvider);
            enumerator.MoveNext();
            var fine = (FineFundamental)enumerator.Current;
            Assert.AreEqual(438783011299, fine.CompanyProfile.EnterpriseValue);
            Assert.AreEqual(311, fine.AssetClassification.MorningstarSectorCode);
            enumerator.Dispose();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void DeserializesUpdatedFileFormat()
        {
            var json = File.ReadAllText("./TestData/aapl_fine_fundamental.json");
            Assert.DoesNotThrow(() =>
            {
                var obj = JsonConvert.DeserializeObject<FineFundamental>(json);
            });
        }

        // This test reports higher memory usage when ran with Travis, so we exclude it for now
        [Test, Category("TravisExclude")]
        public void DoesNotLeakMemory()
        {
            // allow system to stabilize
            Thread.Sleep(250);
            var symbol = Symbols.AAPL;
            var startDate = new DateTime(2014, 4, 30);
            var endDate = new DateTime(2014, 4, 30);

            var config = new SubscriptionDataConfig(typeof(FineFundamental), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            var request = new SubscriptionRequest(false, null, security, config, startDate, endDate);
            var fileProvider = new DefaultDataProvider();
            var factory = new FineFundamentalSubscriptionEnumeratorFactory(false);

            GC.Collect();
            var ramUsageBeforeLoop = OS.TotalPhysicalMemoryUsed;

            const int iterations = 1000;
            for (var i = 0; i < iterations; i++)
            {
                using (var enumerator = factory.CreateEnumerator(request, fileProvider))
                {
                    enumerator.MoveNext();
                }
            }

            GC.Collect();
            var ramUsageAfterLoop = OS.TotalPhysicalMemoryUsed;

            Log.Trace($"RAM usage - before: {ramUsageBeforeLoop} MB, after: {ramUsageAfterLoop} MB");

            Assert.IsTrue(ramUsageAfterLoop - ramUsageBeforeLoop < 10);
        }

        private static TestCaseData[] GetFineFundamentalTestParametersBacktest =>
            GetFineFundamentalTestParameters(false);

        private static TestCaseData[] GetFineFundamentalTestParametersLive =>
            GetFineFundamentalTestParameters(true);

        private static TestCaseData[] GetFineFundamentalTestParameters(bool liveMode)
        {
            return new List<FineFundamentalTestParameters>
            {
                new FineFundamentalTestParameters("AAPL-OneYear")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 2, 28),
                    EndDate = new DateTime(2014, 12, 31),
                    // 2014-2-28 is the first fine data point we have in the OS data folder
                    RowCount = (new DateTime(2014, 12, 31) - new DateTime(2014, 2, 28)).Days,
                    LiveMode = liveMode
                },
                new FineFundamentalTestParameters("AAPL-BeforeFirstDate")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 2, 20),
                    EndDate = new DateTime(2014, 2, 20),
                    // FineFundamentalSubscriptionEnumeratorFactory will not emit null data points
                    RowCount = 0,
                    LiveMode = liveMode
                },
                new FineFundamentalTestParameters("AAPL-FirstDate")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 3, 1),
                    EndDate = new DateTime(2014, 3, 1),
                    RowCount = 1,
                    CompanyShortName = "Apple",
                    Ebitda3M = 19937000000m,
                    Ebitda12M = 57048000000m,
                    CostOfRevenue3M = 35748000000m,
                    CostOfRevenue12M = 106606000000m,
                    EquityPerShareGrowth1Y = 0.091652m,
                    PeRatio = 13.012858m,
                    LiveMode = liveMode
                },
                new FineFundamentalTestParameters("AAPL-AfterFirstDate")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 3, 2),
                    EndDate = new DateTime(2014, 3, 2),
                    RowCount = 1,
                    CompanyShortName = "Apple",
                    Ebitda3M = 19937000000m,
                    Ebitda12M = 57048000000m,
                    CostOfRevenue3M = 35748000000m,
                    CostOfRevenue12M = 106606000000m,
                    EquityPerShareGrowth1Y = 0.091652m,
                    PeRatio = 13.012858m
                },
                new FineFundamentalTestParameters("AAPL-PreviousBeforeLastDate")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 4, 15),
                    EndDate = new DateTime(2014, 4, 15),
                    RowCount = 1,
                    CompanyShortName = "Apple",
                    Ebitda3M = 19937000000m,
                    Ebitda12M = 57048000000m,
                    CostOfRevenue3M = 35748000000m,
                    CostOfRevenue12M = 106606000000m,
                    EquityPerShareGrowth1Y = 0.091652m,
                    PeRatio = 13.272502m,
                    LiveMode = liveMode
                },
                new FineFundamentalTestParameters("AAPL-BeforeLastDate")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 4, 26),
                    EndDate = new DateTime(2014, 4, 26),
                    RowCount = 1,
                    CompanyShortName = "Apple",
                    Ebitda3M = 15790000000m,
                    Ebitda12M = 57048000000m,
                    CostOfRevenue3M = 27699000000m,
                    CostOfRevenue12M = 106606000000m,
                    EquityPerShareGrowth1Y = 0.091652m,
                    PeRatio = 13.272502m,
                    FinancialHealthGrade = "A",
                    LiveMode = liveMode
                },
                new FineFundamentalTestParameters("AAPL-LastDate")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 4, 30),
                    EndDate = new DateTime(2014, 4, 30),
                    RowCount = 1,
                    CompanyShortName = "Apple",
                    Ebitda3M = 15790000000m,
                    Ebitda12M = 57048000000m,
                    CostOfRevenue3M = 27699000000m,
                    CostOfRevenue12M = 106606000000m,
                    EquityPerShareGrowth1Y = 0.091652m,
                    PeRatio = 13.272502m,
                    // different than AAPL-BeforeLastDate
                    FinancialHealthGrade = "B",
                    LiveMode = liveMode
                },
                new FineFundamentalTestParameters("AAPL-AfterLastDate")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 5, 10),
                    EndDate = new DateTime(2014, 5, 10),
                    RowCount = 1,
                    CompanyShortName = "Apple",
                    Ebitda3M = 15790000000m,
                    Ebitda12M = 57048000000m,
                    CostOfRevenue3M = 27699000000m,
                    CostOfRevenue12M = 106606000000m,
                    EquityPerShareGrowth1Y = 0.091652m,
                    PeRatio = 13.272502m,
                    // different than AAPL-BeforeLastDate
                    FinancialHealthGrade = "B",
                    LiveMode = liveMode
                },
                new FineFundamentalTestParameters("No-Fine")
                {
                    Symbol = Symbols.EURUSD,
                    StartDate = new DateTime(2014, 5, 10),
                    EndDate = new DateTime(2014, 5, 10),
                    // FineFundamentalSubscriptionEnumeratorFactory will not emit null data points
                    RowCount = 0,
                    LiveMode = liveMode
                }
            }.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
        }

        public class FineFundamentalTestParameters
        {
            public string Name { get; }
            public bool LiveMode { get; set; }
            public Symbol Symbol { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int RowCount { get; set; }
            public string CompanyShortName { get; set; }
            public decimal Ebitda3M { get; set; }
            public decimal Ebitda12M { get; set; }
            public decimal CostOfRevenue3M { get; set; }
            public decimal CostOfRevenue12M { get; set; }
            public decimal EquityPerShareGrowth1Y { get; set; }
            public decimal PeRatio { get; set; }
            public string FinancialHealthGrade { get; set; }

            public FineFundamentalTestParameters(string name)
            {
                Name = name;
            }
        }
    }
}
