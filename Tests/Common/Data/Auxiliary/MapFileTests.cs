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
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class MapFileTests
    {
        [Test]
        public void HandlesUnknownMappingMode()
        {
            var fileName = "testMapFile.csv";
            var lines = new string[]
            {
                "20110221,cl uucg3i0a3zy9,NYMEX,1",
                "20110418,cl uvvl4qhqe8xt,NYMEX,999"
            };
            File.WriteAllLines(fileName, lines);

            var result = MapFileRow
                .Read(
                    fileName,
                    QuantConnect.Market.NYMEX,
                    SecurityType.Future,
                    TestGlobals.DataProvider
                )
                .ToList();
            File.Delete(fileName);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new DateTime(2011, 2, 21), result[0].Date);
        }

        [Test]
        public void RowThrowsForUnknownMappingMode()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    MapFileRow.Parse(
                        "20110418,cl uvvl4qhqe8xt,NYMEX,999",
                        QuantConnect.Market.NYMEX,
                        SecurityType.Future
                    )
            );
        }

        [Test]
        public void ResolvesFirstTicker()
        {
            var mapFile = new MapFile(
                "goog",
                new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(2014, 03, 27), "goocv"),
                    new MapFileRow(new DateTime(2014, 04, 02), "goocv"),
                    new MapFileRow(new DateTime(2050, 12, 31), "goog")
                }
            );

            Assert.AreEqual("GOOCV", mapFile.FirstTicker);
        }

        [Test]
        [TestCase("abc", "ABC")]
        [TestCase("abc.1", "ABC")]
        [TestCase("brk.a", "BRK.A")]
        [TestCase("brk.a.1", "BRK.A")]
        public void ResolvesFirstTickerFromPermtickIfEmptyMapFile(
            string permtick,
            string expectedFirstTicker
        )
        {
            var mapFile = new MapFile(permtick, new List<MapFileRow>());
            Assert.AreEqual(expectedFirstTicker, mapFile.FirstTicker);
        }

        [Test]
        public void ResolvesFirstDate()
        {
            var mapFile = new MapFile(
                "goog",
                new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(2014, 03, 27), "goocv"),
                    new MapFileRow(new DateTime(2014, 04, 02), "goocv"),
                    new MapFileRow(new DateTime(2050, 12, 31), "goog")
                }
            );

            Assert.AreEqual(new DateTime(2014, 03, 27), mapFile.FirstDate);
        }

        [Test]
        public void GenerateMapFileCSV()
        {
            var mapFile = new MapFile(
                "enrn",
                new List<MapFileRow>()
                {
                    new MapFileRow(new DateTime(2001, 1, 1), "enrn"),
                    new MapFileRow(new DateTime(2001, 12, 2), "enrnq")
                }
            );

            var csvData = new List<string>() { "20010101,enrn", "20011202,enrnq" };

            Assert.True(mapFile.ToCsvLines().SequenceEqual(csvData));
        }

        [Test]
        public void ParsesExchangeCorrectly()
        {
            var mapFile = new MapFile(
                "goog",
                new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(2014, 03, 27), "goocv", "Q"),
                    new MapFileRow(new DateTime(2014, 04, 02), "goocv", "Q"),
                    new MapFileRow(new DateTime(2050, 12, 31), "goog", "Q")
                }
            );

            Assert.AreEqual(Exchange.NASDAQ, (Exchange)mapFile.Last().PrimaryExchange);
        }

        [TestCaseSource(nameof(ParsesRowWithExchangesCorrectlyCases))]
        public void ParsesRowWithExchangesCorrectly(string mapFileRow, Exchange expectedExchange)
        {
            // Arrange
            var rowParts = mapFileRow.Split(',');
            var expectedMapFileRow = new MapFileRow(
                DateTime.ParseExact(
                    rowParts[0],
                    DateFormat.EightCharacter,
                    CultureInfo.InvariantCulture
                ),
                rowParts[1],
                rowParts[2]
            );
            // Act
            var actualMapFileRow = MapFileRow.Parse(
                mapFileRow,
                QuantConnect.Market.USA,
                SecurityType.Equity
            );
            // Assert
            Assert.AreEqual(expectedExchange, actualMapFileRow.PrimaryExchange);
            Assert.AreEqual(expectedMapFileRow, actualMapFileRow);
        }

        [Test]
        public void ParsesRowWithoutExchangesCorrectly()
        {
            // Arrange
            var mapFileRow = "20010213,aapl";
            var rowParts = mapFileRow.Split(',');
            var expectedMapFileRow = new MapFileRow(
                DateTime.ParseExact(
                    rowParts[0],
                    DateFormat.EightCharacter,
                    CultureInfo.InvariantCulture
                ),
                rowParts[1]
            );
            // Act
            var actualMapFileRow = MapFileRow.Parse(
                mapFileRow,
                QuantConnect.Market.USA,
                SecurityType.Equity
            );
            // Assert
            Assert.AreEqual(Exchange.UNKNOWN, actualMapFileRow.PrimaryExchange);
            Assert.AreEqual(expectedMapFileRow, actualMapFileRow);
        }

        private static TestCaseData[] ParsesRowWithExchangesCorrectlyCases()
        {
            return new[]
            {
                new TestCaseData("20010213,aapl,Q", Exchange.NASDAQ),
                new TestCaseData("20010213,aapl,Z", Exchange.BATS),
                new TestCaseData("20010213,aapl,P", Exchange.ARCA),
                new TestCaseData("20010213,aapl,N", Exchange.NYSE),
                new TestCaseData("20010213,aapl,C", Exchange.NSX),
                new TestCaseData("20010213,aapl,D", Exchange.FINRA),
                new TestCaseData("20010213,aapl,I", Exchange.ISE),
                new TestCaseData("20010213,aapl,M", Exchange.CSE),
                new TestCaseData("20010213,aapl,W", Exchange.CBOE),
                new TestCaseData("20010213,aapl,A", Exchange.AMEX),
                new TestCaseData("20010213,aapl,J", Exchange.EDGA),
                new TestCaseData("20010213,aapl,K", Exchange.EDGX),
                new TestCaseData("20010213,aapl,B", Exchange.NASDAQ_BX),
                new TestCaseData("20010213,aapl,X", Exchange.NASDAQ_PSX),
                new TestCaseData("20010213,aapl,Y", Exchange.BATS_Y),
            };
        }
    }
}
