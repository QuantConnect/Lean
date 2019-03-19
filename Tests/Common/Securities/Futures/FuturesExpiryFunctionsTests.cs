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
        private IDictionary<String, List<Dates>> _data = new Dictionary<String, List<Dates>>();
        private const string Zero = "00:00:00";
        private const string ElevenOclockMoscowTime = "08:00:00";
        private const string NineFifteenCentralTime = "14:15:00";
        private const string NineSixteenCentralTime = "14:16:00";
        private const string NineThirtyEasternTime = "13:30:00";
        private const string FiveOClockPMEasternTime = "21:00:00";
        private const string EightOClockChicagoTime = "13:00:00";
        private const string TwelveOclock = "12:00:00";
        private const string TwelveOne = "12:01:00";

        [TestFixtureSetUp]
        public void Init()
        {
            var path = Path.Combine("TestData", "FuturesExpiryFunctionsTestData.xml");
            using (var reader = XmlReader.Create(path))
            {
                var serializer = new XmlSerializer(typeof(Item[]));
                _data = ((Item[])serializer.Deserialize(reader)).ToDictionary(i=>i.Symbol,i=>i.SymbolDates);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Grains.Wheat)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Corn)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Soybeans)]
        [TestCase(QuantConnect.Securities.Futures.Grains.SoybeanMeal)]
        [TestCase(QuantConnect.Securities.Futures.Grains.SoybeanOil)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Oats)]
        public void GrainsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade;

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Currencies.GBP, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CAD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.JPY, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CHF, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EUR, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.AUD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.NZD, NineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.RUB, ElevenOclockMoscowTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.BRL, NineFifteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.MXN, NineSixteenCentralTime)]
        public void CurrenciesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Energies.PropaneNonLDHMontBelvieu, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.ArgusPropaneFarEastIndex, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.CrudeOilWTI, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.HeatingOil, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.Gasoline, Zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.NaturalGas, Zero)]
        public void EnergiesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Financials.Y30TreasuryBond, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y10TreasuryNote, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y5TreasuryNote, TwelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y2TreasuryNote, TwelveOne)]
        public void FinancialsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Indices.SP500EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.NASDAQ100EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Dow30EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Russell2000EMini, NineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Nikkei225Dollar, FiveOClockPMEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.VIX, EightOClockChicagoTime)]
        public void IndicesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Meats.LiveCattle, TwelveOclock)]
        [TestCase(QuantConnect.Securities.Futures.Meats.LeanHogs, TwelveOclock)]
        [TestCase(QuantConnect.Securities.Futures.Meats.FeederCattle, Zero)]
        public void MeatsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Metals.Gold)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Silver)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Platinum)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Palladium)]
        public void MetalsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade;

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Softs.Sugar11CME)]
        public void SoftsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol)
        {
            Assert.IsTrue(_data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in _data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.ContractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var actual = func(security.ID.Date);
                var expected = date.LastTrade;

                //Assert
                Assert.AreEqual(expected, actual, "Failed for symbol: " + symbol);
            }
        }

        /// <summary>
        /// Dates for Termination Conditions of futures
        /// </summary>
        public class Dates
        {
            public DateTime ContractMonth;
            public DateTime LastTrade;
            public Dates() { }
            public Dates(DateTime c, DateTime l)
            {
                ContractMonth = c;
                LastTrade = l;
            }
        }

        /// <summary>
        /// Class to convert Array into Dictionary using XmlSerializer
        /// </summary>
        public class Item
        {
            [XmlAttribute]
            public String Symbol;
            public List<Dates> SymbolDates;
        }
    }
}
