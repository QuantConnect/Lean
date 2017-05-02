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

        /// <summary>
        /// Symbol and list of dates for testing Futures
        /// </summary>
        public class SymbolData
        {
            public String symbol;
            public List<Dates> dateList;
            public SymbolData() { }
            public SymbolData(String symbol, List<Dates> list)
            {
                this.symbol = symbol;
                this.dateList = list;
            }
        }

        [Test]
        public void TestAllExpiryDateFunctions()
        {
            var _path = Directory.GetCurrentDirectory();
            _path = _path.Substring(0,_path.Length-10) + "\\Common\\Securities\\Futures\\FuturesExpiryFunctionsTestData.xml";
            IList<String> symbolsForNineThirtyEasternTime = new List<String>
            {
                QuantConnect.Securities.Futures.Indices.NASDAQ100EMini,
                QuantConnect.Securities.Futures.Indices.SP500EMini,
                QuantConnect.Securities.Futures.Indices.Dow30EMini
            };
            IList<String> symbolsForNineSixteenCentralTime = new List<String>
            {
                QuantConnect.Securities.Futures.Currencies.GBP,
                QuantConnect.Securities.Futures.Currencies.CAD,
                QuantConnect.Securities.Futures.Currencies.JPY,
                QuantConnect.Securities.Futures.Currencies.CHF,
                QuantConnect.Securities.Futures.Currencies.EUR,
                QuantConnect.Securities.Futures.Currencies.AUD,
                QuantConnect.Securities.Futures.Currencies.NZD
            };
            IList<String> symbolsForTwelveOne = new List<String>
            {
                QuantConnect.Securities.Futures.Financials.Y30TreasuryBond,
                QuantConnect.Securities.Futures.Financials.Y10TreasuryNote,
                QuantConnect.Securities.Futures.Financials.Y5TreasuryNote,
                QuantConnect.Securities.Futures.Financials.Y2TreasuryNote
            };
            IList<String> symbolsForTwelveOclock = new List<String>
            {
                QuantConnect.Securities.Futures.Meats.LiveCattle,
                QuantConnect.Securities.Futures.Meats.LeanHogs
            };
            using (XmlReader reader = XmlReader.Create(_path))
            {
                List<SymbolData> data = new List<SymbolData>();
                XmlSerializer serializer = new XmlSerializer(typeof(List<SymbolData>));
                data = (List<SymbolData>)serializer.Deserialize(reader);
                foreach(var _symdata in data)
                {
                    var symbol = _symdata.symbol;
                    foreach (var dates in _symdata.dateList)
                    {
                        var _security = Symbol.CreateFuture(symbol, Market.USA, dates.contractMonth);
                        var func = FuturesExpiryFunctions.FuturesExpiryFunction(_security.ID.Symbol);
                        var calculated = func(_security.ID.Date);
                        var expected = dates.lastTrade;
                        if (symbolsForNineThirtyEasternTime.Contains(_security.ID.Symbol))
                        {
                            expected = expected + new TimeSpan(13,30,0);
                        }else if(symbolsForNineSixteenCentralTime.Contains(_security.ID.Symbol))
                        {
                            expected = expected + new TimeSpan(14, 16, 0);
                        }else if(symbolsForTwelveOclock.Contains(_security.ID.Symbol))
                        {
                            expected = expected + new TimeSpan(12, 0, 0);
                        }else if (symbolsForTwelveOne.Contains(_security.ID.Symbol))
                        {
                            expected = expected + new TimeSpan(12, 1, 0);
                        }
                        Assert.AreEqual(calculated,expected);
                        
                    }
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
            Assert.AreEqual(june2017Func(june2017.ID.Date), new DateTime(2017, 6, 16, 9, 30, 0));

            var september2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2017, 9, 1));
            var september2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(september2017.ID.Symbol);
            Assert.AreEqual(september2017Func(september2017.ID.Date), new DateTime(2017, 9, 15, 9, 30, 0));

            var december2017 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2017, 12, 31));
            var december2017Func = FuturesExpiryFunctions.FuturesExpiryFunction(december2017.ID.Symbol);
            Assert.AreEqual(december2017Func(december2017.ID.Date), new DateTime(2017, 12, 15, 9, 30, 0));

            var march2018 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2018, 3, 31));
            var march2018Func = FuturesExpiryFunctions.FuturesExpiryFunction(march2018.ID.Symbol);
            Assert.AreEqual(march2018Func(march2018.ID.Date), new DateTime(2018, 3, 16, 9, 30, 0));

            var june2018 = Symbol.CreateFuture(QuantConnect.Securities.Futures.Indices.SP500EMini, Market.USA, new DateTime(2018, 3, 31));
            var june2018Func = FuturesExpiryFunctions.FuturesExpiryFunction(june2018.ID.Symbol);
            Assert.AreEqual(june2018Func(june2018.ID.Date), new DateTime(2018, 3, 16, 9, 30, 0));
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
