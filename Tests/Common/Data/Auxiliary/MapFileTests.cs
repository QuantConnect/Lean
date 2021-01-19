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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class MapFileTests
    {
        [Test]
        public void ResolvesFirstTicker()
        {
            var mapFile = new MapFile("goog", new List<MapFileRow>
            {
                new MapFileRow(new DateTime(2014, 03, 27), "goocv"),
                new MapFileRow(new DateTime(2014, 04, 02), "goocv"),
                new MapFileRow(new DateTime(2050, 12, 31), "goog")
            });

            Assert.AreEqual("GOOCV", mapFile.FirstTicker);
        }

        [Test]
        [TestCase("abc", "ABC")]
        [TestCase("abc.1", "ABC")]
        [TestCase("brk.a", "BRK.A")]
        [TestCase("brk.a.1", "BRK.A")]
        public void ResolvesFirstTickerFromPermtickIfEmptyMapFile(string permtick, string expectedFirstTicker)
        {
            var mapFile = new MapFile(permtick, new List<MapFileRow>());
            Assert.AreEqual(expectedFirstTicker, mapFile.FirstTicker);
        }

        [Test]
        public void ResolvesFirstDate()
        {
            var mapFile = new MapFile("goog", new List<MapFileRow>
            {
                new MapFileRow(new DateTime(2014, 03, 27), "goocv"),
                new MapFileRow(new DateTime(2014, 04, 02), "goocv"),
                new MapFileRow(new DateTime(2050, 12, 31), "goog")
            });

            Assert.AreEqual(new DateTime(2014, 03, 27), mapFile.FirstDate);
        }

        [Test]
        public void GenerateMapFileCSV()
        {
            var mapFile = new MapFile("enrn", new List<MapFileRow>()
            {
                new MapFileRow(new DateTime(2001, 1, 1), "enrn"),
                new MapFileRow(new DateTime(2001, 12, 2), "enrnq")
            });

            var csvData = new List<string>()
            {
                "20010101,enrn",
                "20011202,enrnq"
            };

            Assert.True(mapFile.ToCsvLines().SequenceEqual(csvData));
        }

        [Test]
        public void ParsesExchangeCorrectly()
        {
            var mapFile = new MapFile("goog", new List<MapFileRow>
            {
                new MapFileRow(new DateTime(2014, 03, 27), "goocv", 'Q'),
                new MapFileRow(new DateTime(2014, 04, 02), "goocv", 'Q'),
                new MapFileRow(new DateTime(2050, 12, 31), "goog", 'Q')
            });

            Assert.AreEqual(PrimaryExchange.NASDAQ, (PrimaryExchange) mapFile.Last().PrimaryExchange);
        }

        [TestCase("20010213,aapl,Q", PrimaryExchange.NASDAQ)]
        [TestCase("20010213,aapl,Z", PrimaryExchange.BATS)]
        [TestCase("20010213,aapl,P", PrimaryExchange.ARCA)]
        [TestCase("20010213,aapl,N", PrimaryExchange.NYSE)]
        [TestCase("20010213,aapl,C", PrimaryExchange.NSE)]
        [TestCase("20010213,aapl,D", PrimaryExchange.FINRA)]
        [TestCase("20010213,aapl,I", PrimaryExchange.ISE)]
        [TestCase("20010213,aapl,M", PrimaryExchange.CSE)]
        [TestCase("20010213,aapl,W", PrimaryExchange.CBOE)]
        [TestCase("20010213,aapl,A", PrimaryExchange.AMEX)]
        [TestCase("20010213,aapl,J", PrimaryExchange.EDGA)]
        [TestCase("20010213,aapl,K", PrimaryExchange.EDGX)]
        [TestCase("20010213,aapl,B", PrimaryExchange.NASDAQ_BX)]
        [TestCase("20010213,aapl,X", PrimaryExchange.NASDAQ_PSX)]
        [TestCase("20010213,aapl,Y", PrimaryExchange.BATS_Y)]
        public void ParsesRowWithExchangesCorrectly(string mapFileRow, PrimaryExchange expectedPrimaryExchange)
        {
            // Arrange
            var rowParts = mapFileRow.Split(',');
            var expectedMapFileRow = new MapFileRow(
                DateTime.ParseExact(rowParts[0], DateFormat.EightCharacter, CultureInfo.InvariantCulture),
                rowParts[1],
                Convert.ToChar(rowParts[2], CultureInfo.InvariantCulture));
            // Act
            var actualMapFileRow = MapFileRow.Parse(mapFileRow);
            // Assert
            Assert.AreEqual(expectedPrimaryExchange, actualMapFileRow.PrimaryExchange);
            Assert.AreEqual(expectedMapFileRow, actualMapFileRow);
        }

        [Test]
        public void ParsesRowWithoutExchangesCorrectly()
        {
            // Arrange
            var mapFileRow = "20010213,aapl";
            var rowParts = mapFileRow.Split(',');
            var expectedMapFileRow = new MapFileRow(
                DateTime.ParseExact(rowParts[0], DateFormat.EightCharacter, CultureInfo.InvariantCulture),
                rowParts[1]);
            // Act
            var actualMapFileRow = MapFileRow.Parse(mapFileRow);
            // Assert
            Assert.AreEqual(PrimaryExchange.UNKNOWN, actualMapFileRow.PrimaryExchange);
            Assert.AreEqual(expectedMapFileRow, actualMapFileRow);
        }
    }
}
