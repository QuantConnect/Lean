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
using NUnit.Framework;
using QuantConnect.Securities.Future;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace QuantConnect.Tests.Common.Securities.Futures
{
    [TestFixture]
    public class FuturesExpiryFunctionsTests
    {
        /// <summary>
        /// Dates for Termination Conditions of futures
        /// </summary>
        public class Dates
        {
            public DateTime contractMonth;
            public DateTime lastTrade;
            public Dates() { }
            public Dates(DateTime c, DateTime l)
            {
                contractMonth = c;
                lastTrade = l;
            }
        }

        /*
        /// <summary>
        /// Symbol and list of dates for testing Futures
        /// </summary>
        public class SymbolData
        {
            public String symbol;
            public List<Dates> dateList;
            public SymbolData() { }
            public SymbolData(String symbol, List<Dates> dateList)
            {
                this.symbol = symbol;
                this.dateList = dateList;
            }
        } */

        /// <summary>
        /// Class to convert Array into Dictionary using XmlSerializer
        /// </summary>
        public class Item
        {
            [XmlAttribute]
            public String symbol;
            public List<Dates> symbolDates;
        }

        private IDictionary<String, List<Dates>> data = new Dictionary<String, List<Dates>>();
        private const string zero = "00:00:00";
        private const string nineSixteenCentralTime = "14:16:00";
        private const string nineThirtyEasternTime = "13:30:00";
        private const string twelveOclock = "12:00:00";
        private const string twelveOne = "12:01:00";

        [TestFixtureSetUp]
        public void Init()
        {
            var path = Path.Combine("TestData", "FuturesExpiryFunctionsTestData.xml");
            using (var reader = XmlReader.Create(path))
            {
                var serializer = new XmlSerializer(typeof(Item[]));
                data = ((Item[])serializer.Deserialize(reader)).ToDictionary(i=>i.symbol,i=>i.symbolDates);
            }
        }

        /// <summary>
        /// Utility method to do the common work for all the symbols i.e. asserting the 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="dayTime"></param>
        /// <param name="market"></param>
        [TestCase(QuantConnect.Securities.Futures.Grains.Wheat, zero)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Corn, zero)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Soybeans, zero)]
        [TestCase(QuantConnect.Securities.Futures.Grains.SoybeanMeal, zero)]
        [TestCase(QuantConnect.Securities.Futures.Grains.SoybeanOil, zero)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Oats, zero)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.GBP, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CAD, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.JPY, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CHF, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EUR, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.AUD, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.NZD, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Energies.CrudeOilWTI, zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.HeatingOil, zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.Gasoline, zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.NaturalGas, zero)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y30TreasuryBond, twelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y10TreasuryNote , twelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y5TreasuryNote, twelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y2TreasuryNote, twelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Indices.SP500EMini,nineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.NASDAQ100EMini, nineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Dow30EMini, nineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Meats.LiveCattle, twelveOclock)]
        [TestCase(QuantConnect.Securities.Futures.Meats.LeanHogs, twelveOclock)]
        [TestCase(QuantConnect.Securities.Futures.Meats.FeederCattle, zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Gold, zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Silver, zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Platinum, zero)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Palladium, zero)]
        public void TestExpiryDateFunction(string symbol, string dayTime)
        {
            Assert.IsTrue(data.ContainsKey(symbol),"Symbol "+ symbol + " not present in Test Data");
            foreach(var dates in data[symbol])
            {
                var security = Symbol.CreateFuture(symbol, Market.USA, dates.contractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);
                var calculated = func(security.ID.Date);
                var expected = dates.lastTrade + TimeSpan.Parse(dayTime);
                Assert.AreEqual(calculated, expected, "Failed for symbol: " + symbol);
            }
        }

        [Test]
        public void TestAllExpiryDateFunctions()
        {
            var symbolsForNineThirtyEasternTime = new List<String>
            {
                QuantConnect.Securities.Futures.Indices.NASDAQ100EMini,
                QuantConnect.Securities.Futures.Indices.SP500EMini,
                QuantConnect.Securities.Futures.Indices.Dow30EMini
            };
            var symbolsForNineSixteenCentralTime = new List<String>
            {
                QuantConnect.Securities.Futures.Currencies.GBP,
                QuantConnect.Securities.Futures.Currencies.CAD,
                QuantConnect.Securities.Futures.Currencies.JPY,
                QuantConnect.Securities.Futures.Currencies.CHF,
                QuantConnect.Securities.Futures.Currencies.EUR,
                QuantConnect.Securities.Futures.Currencies.AUD,
                QuantConnect.Securities.Futures.Currencies.NZD
            };
            var symbolsForTwelveOne = new List<String>
            {
                QuantConnect.Securities.Futures.Financials.Y30TreasuryBond,
                QuantConnect.Securities.Futures.Financials.Y10TreasuryNote,
                QuantConnect.Securities.Futures.Financials.Y5TreasuryNote,
                QuantConnect.Securities.Futures.Financials.Y2TreasuryNote
            };
            var symbolsForTwelveOclock = new List<String>
            {
                QuantConnect.Securities.Futures.Meats.LiveCattle,
                QuantConnect.Securities.Futures.Meats.LeanHogs
            };

            foreach(var symbol in data.Keys)
            {
                var symbolDates = data[symbol];
                foreach (var dates in symbolDates)
                {
                    var security = Symbol.CreateFuture(symbol, Market.USA, dates.contractMonth);
                    var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);
                    var calculated = func(security.ID.Date);
                    var expected = dates.lastTrade;
                    if (symbolsForNineThirtyEasternTime.Contains(security.ID.Symbol))
                    {
                        expected = expected + new TimeSpan(13,30,0);
                    }else if(symbolsForNineSixteenCentralTime.Contains(security.ID.Symbol))
                    {
                        expected = expected + new TimeSpan(14, 16, 0);
                    }else if(symbolsForTwelveOclock.Contains(security.ID.Symbol))
                    {
                        expected = expected + new TimeSpan(12, 0, 0);
                    }else if (symbolsForTwelveOne.Contains(security.ID.Symbol))
                    {
                        expected = expected + new TimeSpan(12, 1, 0);
                    }
                    Assert.AreEqual(calculated,expected,"Failed for symbol " + symbol);
                        
                }
            }
            
        }

        [Test]
        public void TestGoldExpiryDateFunction()
        {
            var april2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2017, 4, 1));
            var april2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(april2017.ID.Symbol);
            Assert.AreEqual(april2017Func(april2017.ID.Date), new DateTime(2017, 4, 26));

            var may2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2017, 5, 31));
            var may2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(may2017.ID.Symbol);
            Assert.AreEqual(may2017Func(may2017.ID.Date), new DateTime(2017, 5, 26));

            var june2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2017, 6, 15));
            var june2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(june2017.ID.Symbol);
            Assert.AreEqual(june2017Func(june2017.ID.Date), new DateTime(2017, 6, 28));

            var july2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2017, 7, 15));
            var july2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(july2017.ID.Symbol);
            Assert.AreEqual(july2017Func(july2017.ID.Date), new DateTime(2017, 7, 27));

            var october2018 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2018, 10, 15));
            var october2018Func = FuturesExpiryFunctions.FuturesExpiryFunction(october2018.ID.Symbol);
            Assert.AreEqual(october2018Func(october2018.ID.Date), new DateTime(2018, 10, 29));

            var december2021 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Gold, Market.USA, new DateTime(2021, 12, 15));
            var december2021Func = FuturesExpiryFunctions.FuturesExpiryFunction(december2021.ID.Symbol);
            Assert.AreEqual(december2021Func(december2021.ID.Date), new DateTime(2021, 12, 29));
        }

        [Test]
        public void TestSilverExpiryDateFunction()
        {
            var april2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Silver, Market.USA, new DateTime(2017, 4, 1));
            var april2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(april2017.ID.Symbol);
            Assert.AreEqual(april2017Func(april2017.ID.Date), new DateTime(2017, 4, 26));

            var may2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Silver, Market.USA, new DateTime(2017, 5, 31));
            var may2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(may2017.ID.Symbol);
            Assert.AreEqual(may2017Func(may2017.ID.Date), new DateTime(2017, 5, 26));

            var june2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Silver, Market.USA, new DateTime(2017, 6, 15));
            var june2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(june2017.ID.Symbol);
            Assert.AreEqual(june2017Func(june2017.ID.Date), new DateTime(2017, 6, 28));

            var july2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Metals.Silver, Market.USA, new DateTime(2017, 7, 15));
            var july2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(july2017.ID.Symbol);
            Assert.AreEqual(july2017Func(july2017.ID.Date), new DateTime(2017, 7, 27));

        }

        [Test]
        public void TestSP500EMiniExpiryDateFunction()
        {
            var june2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2017, 6, 15));
            var june2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(june2017.ID.Symbol);
            Assert.AreEqual(june2017Func(june2017.ID.Date), new DateTime(2017, 6, 16, 13, 30, 0));

            var september2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2017, 9, 1));
            var september2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(september2017.ID.Symbol);
            Assert.AreEqual(september2017Func(september2017.ID.Date), new DateTime(2017, 9, 15, 13, 30, 0));

            var december2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2017, 12, 31));
            var december2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(december2017.ID.Symbol);
            Assert.AreEqual(december2017Func(december2017.ID.Date), new DateTime(2017, 12, 15, 13, 30, 0));

            var march2018 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2018, 3, 31));
            var march2018Func = FuturesExpiryFunctions.FuturesExpiryFunction(march2018.ID.Symbol);
            Assert.AreEqual(march2018Func(march2018.ID.Date), new DateTime(2018, 3, 16, 13, 30, 0));

            var june2018 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2018, 3, 31));
            var june2018Func = FuturesExpiryFunctions.FuturesExpiryFunction(june2018.ID.Symbol);
            Assert.AreEqual(june2018Func(june2018.ID.Date), new DateTime(2018, 3, 16, 13, 30, 0));
        }

        [Test]
        public void TestWheatExpiryDateFunction()
        {
            var may2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Grains.Wheat, Market.USA, new DateTime(2017, 5, 12));
            var may2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(may2017.ID.Symbol);
            Assert.AreEqual(may2017Func(may2017.ID.Date), new DateTime(2017, 5, 12));
        }

        [Test]
        public void TestGBPExpiryDateFunction()
        {
            var may2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Currencies.GBP, Market.USA, new DateTime(2017, 5, 15));
            var may2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(may2017.ID.Symbol);
            Assert.AreEqual(may2017Func(may2017.ID.Date), new DateTime(2017, 5, 15, 14, 16, 0));
        }

        [Test]
        public void TestY30TreasuryBondExpiryDateFunction()
        {
            var jun2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Financials.Y30TreasuryBond, Market.USA, new DateTime(2017, 6, 21));
            var jun2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(jun2017.ID.Symbol);
            Assert.AreEqual(jun2017Func(jun2017.ID.Date), new DateTime(2017, 6, 21, 12, 01, 0));
        }

        [Test]
        public void TestCADExpiryDateFunction()
        {
            var jun2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Currencies.CAD, Market.USA, new DateTime(2017, 6, 20));
            var jun2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(jun2017.ID.Symbol);
            Assert.AreEqual(jun2017Func(jun2017.ID.Date), new DateTime(2017, 6, 20, 14, 16, 0));
        }

        [Test]
        public void TestY5TreasuryNotesExpiryDateFunction()
        {
            var jun2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Financials.Y5TreasuryNote, Market.USA, new DateTime(2017, 6, 30));
            var jun2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(jun2017.ID.Symbol);
            Assert.AreEqual(jun2017Func(jun2017.ID.Date), new DateTime(2017, 6, 30, 12, 1, 0));
        }

        [Test]
        public void TestCrudeOilWTINotesExpiryDateFunction()
        {
            var jun2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Energies.CrudeOilWTI, Market.USA, new DateTime(2017, 6, 1));
            var jun2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(jun2017.ID.Symbol);
            Assert.AreEqual(jun2017Func(jun2017.ID.Date), new DateTime(2017, 5, 22));
        }

        [Test]
        public void TestHeatingOilNotesExpiryDateFunction()
        {
            var jun2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Energies.HeatingOil, Market.USA, new DateTime(2017, 6, 1));
            var jun2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(jun2017.ID.Symbol);
            Assert.AreEqual(jun2017Func(jun2017.ID.Date), new DateTime(2017, 5, 31));
        }

        [Test]
        public void TestNaturalGasNotesExpiryDateFunction()
        {
            var jun2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Energies.NaturalGas, Market.USA, new DateTime(2017, 6, 1));
            var jun2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(jun2017.ID.Symbol);
            Assert.AreEqual(jun2017Func(jun2017.ID.Date), new DateTime(2017, 5, 26));
        }

        [Test]
        public void TestLiveCattleExpiryDateFunction()
        {
            var jun2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Meats.LiveCattle, Market.USA, new DateTime(2017, 6, 1));
            var jun2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(jun2017.ID.Symbol);
            Assert.AreEqual(jun2017Func(jun2017.ID.Date), new DateTime(2017, 6, 30, 12, 0, 0));
        }

        [Test]
        public void TestLeanHogsExpiryDateFunction()
        {
            var jun2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Meats.LeanHogs, Market.USA, new DateTime(2017, 6, 1));
            var jun2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(jun2017.ID.Symbol);
            Assert.AreEqual(jun2017Func(jun2017.ID.Date), new DateTime(2017, 6, 14, 12, 0, 0));
        }

        [Test]
        public void TestFeederCattleExpiryDateFunction()
        {
            var may2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Meats.FeederCattle, Market.USA, new DateTime(2017, 5, 1));
            var may2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(may2017.ID.Symbol);
            Assert.AreEqual(may2017Func(may2017.ID.Date), new DateTime(2017, 5, 25));
        }
    }
}
