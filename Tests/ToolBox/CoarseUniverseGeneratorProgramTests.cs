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
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Tests.Common.Data.Fundamental;
using QuantConnect.ToolBox.CoarseUniverseGenerator;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    public class CoarseUniverseGeneratorProgramTests
    {
        private DirectoryInfo _destinationFolder;

        [SetUp]
        public void SetUp()
        {
            _destinationFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            FundamentalService.Initialize(TestGlobals.DataProvider, new NullFundamentalDataProvider(), false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_destinationFolder.Exists)
            {
                _destinationFolder.Delete(true);
            }
        }

        [Test]
        public void GeneratesASingleRowPerSecurityOnMapFileBoundaryDates()
        {
            // GOOG: 20140327 goocv, 20140402 goocv, 20501231 goog. The class C shares traded as GOOCV
            // through 2014-04-02 and as GOOG from the next trading day on. On 2014-04-02 the goog.zip
            // daily file holds the bar of the legacy GOOG (today's GOOGL), a different security.
            var mapFile = new MapFile("goog", new[]
            {
                new MapFileRow(new DateTime(2014, 03, 27), "goocv"),
                new MapFileRow(new DateTime(2014, 04, 02), "goocv"),
                new MapFileRow(new DateTime(2050, 12, 31), "goog")
            });
            var boundaryDate = new DateTime(2014, 04, 02);

            var generator = new CoarseUniverseGeneratorProgram(
                new DirectoryInfo(Path.Combine(Globals.DataFolder, "equity", "usa", "daily")),
                _destinationFolder,
                Market.USA,
                new FileInfo(Path.Combine(_destinationFolder.FullName, "blacklisted-tickers.txt")),
                "quantconnect-",
                new TestMapFileProvider(mapFile),
                TestGlobals.FactorFileProvider);

            Assert.IsTrue(generator.Run(out var coarsePerSecurity, out var dates));

            var sid = SecurityIdentifier.GenerateEquity(new DateTime(2014, 03, 27), "goocv", Market.USA);
            Assert.IsTrue(coarsePerSecurity.TryGetValue(sid, out var coarse));
            Assert.IsTrue(dates.Contains(boundaryDate));

            var duplicatedDates = coarse.GroupBy(x => x.Time).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.IsEmpty(duplicatedDates, $"Duplicated dates: {string.Join(", ", duplicatedDates)}");

            // the boundary date belongs to the epoch that ends on it, so it uses the old ticker's daily bar
            var boundaryRow = coarse.Single(x => x.Time == boundaryDate);
            Assert.AreEqual("GOOCV", boundaryRow.Symbol.Value);
            Assert.AreEqual(567m, boundaryRow.Price);

            // the next trading date already belongs to the new ticker
            var nextRow = coarse.Single(x => x.Time == new DateTime(2014, 04, 03));
            Assert.AreEqual("GOOG", nextRow.Symbol.Value);

            // the first epoch is inclusive of its start date
            var firstRow = coarse.Single(x => x.Time == new DateTime(2014, 03, 27));
            Assert.AreEqual("GOOCV", firstRow.Symbol.Value);

            // the generated file for the boundary date has a single row for the sid
            var boundaryFile = Path.Combine(_destinationFolder.FullName, $"{boundaryDate:yyyyMMdd}.csv");
            var sidRows = File.ReadAllLines(boundaryFile).Where(line => line.StartsWith(sid.ToString(), StringComparison.Ordinal)).ToList();
            Assert.AreEqual(1, sidRows.Count, string.Join(Environment.NewLine, sidRows));
        }

        private class TestMapFileProvider : IMapFileProvider
        {
            private readonly MapFileResolver _resolver;

            public TestMapFileProvider(params MapFile[] mapFiles)
            {
                _resolver = new MapFileResolver(mapFiles);
            }

            public void Initialize(IDataProvider dataProvider)
            {
            }

            public MapFileResolver Get(AuxiliaryDataKey auxiliaryDataKey)
            {
                return _resolver;
            }
        }
    }
}
