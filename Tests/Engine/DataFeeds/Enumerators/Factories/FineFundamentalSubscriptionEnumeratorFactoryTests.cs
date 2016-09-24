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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators.Factories
{
    [TestFixture]
    public class FineFundamentalSubscriptionEnumeratorFactoryTests
    {
        [Test, TestCaseSource("GetFineFundamentalTestParameters")]
        public void ReadsFineFundamental(FineFundamentalTestParameters parameters)
        {
            var stopwatch = Stopwatch.StartNew();
            var rows = new List<FineFundamental>();

            var config = new SubscriptionDataConfig(typeof(FineFundamental), parameters.Symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var security = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), config, new Cash(CashBook.AccountCurrency, 0, 1), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var request = new SubscriptionRequest(false, null, security, config, parameters.StartDate, parameters.EndDate);

            var factory = new FineFundamentalSubscriptionEnumeratorFactory();
            var enumerator = factory.CreateEnumerator(request);
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current as FineFundamental;
                rows.Add(current);
            }

            stopwatch.Stop();
            Console.WriteLine("Total rows: {0}, elapsed time: {1}", rows.Count, stopwatch.Elapsed);

            Assert.AreEqual(parameters.RowCount, rows.Count);

            if (parameters.RowCount != 1) return;

            var row = rows[0];
            Assert.AreEqual(parameters.CompanyShortName, row.CompanyReference.ShortName);
            Assert.AreEqual(parameters.Symbol, row.Symbol);
            Assert.AreEqual(parameters.Symbol != Symbol.Empty ? parameters.Symbol.Value : null, row.CompanyReference.PrimarySymbol);
            Assert.AreEqual(parameters.Symbol != Symbol.Empty ? parameters.Symbol.Value : null, row.SecurityReference.SecuritySymbol);
            Assert.AreEqual(parameters.Ebitda3M, row.FinancialStatements.IncomeStatement.EBITDA.ThreeMonths);
            Assert.AreEqual(parameters.Ebitda12M, row.FinancialStatements.IncomeStatement.EBITDA.TwelveMonths);
            Assert.AreEqual(parameters.Ebitda12M, row.FinancialStatements.IncomeStatement.EBITDA);
            Assert.AreEqual(parameters.CostOfRevenue3M, row.FinancialStatements.IncomeStatement.CostOfRevenue.ThreeMonths);
            Assert.AreEqual(parameters.CostOfRevenue12M, row.FinancialStatements.IncomeStatement.CostOfRevenue.TwelveMonths);
            Assert.AreEqual(parameters.CostOfRevenue12M, row.FinancialStatements.IncomeStatement.CostOfRevenue);
            Assert.AreEqual(parameters.EquityPerShareGrowth1Y, row.EarningRatios.EquityPerShareGrowth.OneYear);
            Assert.AreEqual(parameters.EquityPerShareGrowth1Y, row.EarningRatios.EquityPerShareGrowth);
            Assert.AreEqual(parameters.PeRatio, row.ValuationRatios.PERatio);
        }

        private static TestCaseData[] GetFineFundamentalTestParameters()
        {
            return new List<FineFundamentalTestParameters>
            {
                new FineFundamentalTestParameters("AAPL-OneYear")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 1, 1),
                    EndDate = new DateTime(2014, 12, 31),
                    RowCount = 365
                },
                new FineFundamentalTestParameters("AAPL-BeforeFirstDate")
                {
                    Symbol = Symbol.Empty,
                    StartDate = new DateTime(2014, 2, 20),
                    EndDate = new DateTime(2014, 2, 20),
                    RowCount = 1
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
                    PeRatio = 13.012858m
                },
                new FineFundamentalTestParameters("AAPL-BeforeLastDate")
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
                    PeRatio = 13.272502m
                },
                new FineFundamentalTestParameters("AAPL-LastDate")
                {
                    Symbol = Symbols.AAPL,
                    StartDate = new DateTime(2014, 4, 25),
                    EndDate = new DateTime(2014, 4, 25),
                    RowCount = 1,
                    CompanyShortName = "Apple",
                    Ebitda3M = 15790000000m,
                    Ebitda12M = 57048000000m,
                    CostOfRevenue3M = 27699000000m,
                    CostOfRevenue12M = 106606000000m,
                    EquityPerShareGrowth1Y = 0.091652m,
                    PeRatio = 13.272502m
                },
                new FineFundamentalTestParameters("AAPL-AfterLastDate")
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
                    PeRatio = 13.272502m
                },

            }.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
        }

        public class FineFundamentalTestParameters
        {
            public string Name { get; private set; }
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

            public FineFundamentalTestParameters(string name)
            {
                Name = name;
            }
        }
    }
}
