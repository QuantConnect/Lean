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

        [TestCase(QuantConnect.Securities.Futures.Grains.Wheat)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Corn)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Soybeans)]
        [TestCase(QuantConnect.Securities.Futures.Grains.SoybeanMeal)]
        [TestCase(QuantConnect.Securities.Futures.Grains.SoybeanOil)]
        [TestCase(QuantConnect.Securities.Futures.Grains.Oats)]
        public void GrainsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol)
        {
            Assert.IsTrue(data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.contractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var calculated = func(security.ID.Date);
                var actual = date.lastTrade;

                //Assert
                Assert.AreEqual(calculated, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Currencies.GBP, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CAD, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.JPY, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.CHF, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.EUR, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.AUD, nineSixteenCentralTime)]
        [TestCase(QuantConnect.Securities.Futures.Currencies.NZD, nineSixteenCentralTime)]
        public void CurrenciesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.contractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var calculated = func(security.ID.Date);
                var actual = date.lastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(calculated, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Energies.CrudeOilWTI, zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.HeatingOil, zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.Gasoline, zero)]
        [TestCase(QuantConnect.Securities.Futures.Energies.NaturalGas, zero)]
        public void EnergiesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.contractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var calculated = func(security.ID.Date);
                var actual = date.lastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(calculated, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Financials.Y30TreasuryBond, twelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y10TreasuryNote, twelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y5TreasuryNote, twelveOne)]
        [TestCase(QuantConnect.Securities.Futures.Financials.Y2TreasuryNote, twelveOne)]
        public void FinancialsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.contractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var calculated = func(security.ID.Date);
                var actual = date.lastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(calculated, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Indices.SP500EMini, nineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.NASDAQ100EMini, nineThirtyEasternTime)]
        [TestCase(QuantConnect.Securities.Futures.Indices.Dow30EMini, nineThirtyEasternTime)]
        public void IndicesExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.contractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var calculated = func(security.ID.Date);
                var actual = date.lastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(calculated, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Meats.LiveCattle, twelveOclock)]
        [TestCase(QuantConnect.Securities.Futures.Meats.LeanHogs, twelveOclock)]
        [TestCase(QuantConnect.Securities.Futures.Meats.FeederCattle, zero)]
        public void MeatsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol, string dayTime)
        {
            Assert.IsTrue(data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.contractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var calculated = func(security.ID.Date);
                var actual = date.lastTrade + TimeSpan.Parse(dayTime);

                //Assert
                Assert.AreEqual(calculated, actual, "Failed for symbol: " + symbol);
            }
        }

        [TestCase(QuantConnect.Securities.Futures.Metals.Gold)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Silver)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Platinum)]
        [TestCase(QuantConnect.Securities.Futures.Metals.Palladium)]
        public void MetalsExpiryDateFunction_WithDifferentDates_ShouldFollowContract(string symbol)
        {
            Assert.IsTrue(data.ContainsKey(symbol), "Symbol " + symbol + " not present in Test Data");
            foreach (var date in data[symbol])
            {
                //Arrange
                var security = Symbol.CreateFuture(symbol, Market.USA, date.contractMonth);
                var func = FuturesExpiryFunctions.FuturesExpiryFunction(security.ID.Symbol);

                //Act
                var calculated = func(security.ID.Date);
                var actual = date.lastTrade;

                //Assert
                Assert.AreEqual(calculated, actual, "Failed for symbol: " + symbol);
            }
        }

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
        /// Class to convert Array into Dictionary using XmlSerializer
        /// </summary>
        public class Item
        {
            [XmlAttribute]
            public String symbol;
            public List<Dates> symbolDates;
        }
    }
}
