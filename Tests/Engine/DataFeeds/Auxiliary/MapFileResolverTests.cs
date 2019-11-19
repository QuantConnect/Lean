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
using NUnit.Framework;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds.Auxiliary
{
    [TestFixture]
    public class MapFileResolverTests
    {
        private readonly MapFileResolver _resolver = CreateMapFileResolver();

        [Test]
        public void ChecksFirstDate()
        {
            var mapFileProvider = new LocalDiskMapFileProvider();
            var mapFileResolver = mapFileProvider.Get(Market.USA);
            // QQQ started trading on 19990310
            var mapFile = mapFileResolver.ResolveMapFile("QQQ", new DateTime(1999, 3, 9));
            Assert.IsTrue(mapFile.IsNullOrEmpty());

            var mapFile2 = mapFileResolver.ResolveMapFile("QQQ", new DateTime(2015, 3, 10));
            Assert.IsFalse(mapFile2.IsNullOrEmpty());
        }

        [Test]
        public void ResolvesCorrectlyReUsedTicker()
        {
            var mapFileProvider = new LocalDiskMapFileProvider();
            var mapFileResolver = mapFileProvider.Get(Market.USA);

            // FB.1 started trading on 19990929 and ended on 20030328
            var mapFile = mapFileResolver.ResolveMapFile("FB", new DateTime(1999, 9, 28));
            Assert.IsTrue(mapFile.IsNullOrEmpty());

            mapFile = mapFileResolver.ResolveMapFile("FB", new DateTime(1999, 9, 29));
            Assert.IsFalse(mapFile.IsNullOrEmpty());

            // FB started trading on 20120518
            mapFile = mapFileResolver.ResolveMapFile("FB", new DateTime(2012, 5, 17));
            Assert.IsTrue(mapFile.IsNullOrEmpty());

            mapFile = mapFileResolver.ResolveMapFile("FB", new DateTime(2015, 5, 18));
            Assert.IsFalse(mapFile.IsNullOrEmpty());
        }

        [Test]
        public void InitializationSpeedTest()
        {
            var mapFileProvider = new LocalDiskMapFileProvider();
            var sw = Stopwatch.StartNew();
            var mapFileresolver = mapFileProvider.Get(Market.USA);
            sw.Stop();
            Console.WriteLine($"elapsed: {sw.Elapsed.TotalSeconds} seconds");
        }

        [Test]
        public void ResolvesStraightMapping()
        {
            var spyMapFile = _resolver.ResolveMapFile("SPY", new DateTime(2015, 08, 23));
            Assert.IsNotNull(spyMapFile);
            Assert.AreEqual("SPY", spyMapFile.GetMappedSymbol(new DateTime(2015, 08, 23)));
        }

        [Test]
        public void MapFileReturnDefaultValueCorrectly()
        {
            var spyMapFile = _resolver.ResolveMapFile("PEPE", new DateTime(2015, 08, 23));
            Assert.IsNotNull(spyMapFile);
            Assert.AreEqual("Pepito", spyMapFile.GetMappedSymbol(new DateTime(2015, 08, 23), "Pepito"));
        }

        [Test]
        public void ResolvesMapFilesOnNonSpecifiedDates()
        {
            var mapFile = _resolver.ResolveMapFile("GOOG", new DateTime(2014, 04, 01));
            Assert.AreEqual("GOOGL", mapFile.Permtick);

            mapFile = _resolver.ResolveMapFile("GOOG", new DateTime(2014, 04, 03));
            Assert.AreEqual("GOOG", mapFile.Permtick);
        }

        [Test]
        public void ResolvesOldSymbolRemapped()
        {
            // on 2014.04.02 a symbol GOOG traded its last day, the following day it would trade under GOOGL
            var april2 = new DateTime(2014, 04, 02);
            var googMapFile = _resolver.ResolveMapFile("GOOG", april2);
            Assert.IsNotNull(googMapFile);
            Assert.AreEqual("GOOG", googMapFile.GetMappedSymbol(april2));
            Assert.AreEqual("GOOGL", googMapFile.GetMappedSymbol(april2.AddDays(1)));
        }

        [Test]
        public void ResolvesExactMapping()
        {
            var oih1 = _resolver.ResolveMapFile("OIH.1", new DateTime(2011, 12, 20));
            Assert.IsNotNull(oih1);
            Assert.AreEqual("OIH", oih1.GetMappedSymbol(new DateTime(2010, 02, 06)));
            Assert.AreEqual("OIH", oih1.GetMappedSymbol(new DateTime(2010, 02, 07)));
            Assert.AreEqual("OIH", oih1.GetMappedSymbol(new DateTime(2011, 12, 20)));
            Assert.AreEqual(string.Empty, oih1.GetMappedSymbol(new DateTime(2011, 12, 21)));
        }

        [Test]
        public void ResolvesRemappedSymbolWithBothMapFiles()
        {
            var date = new DateTime(2012, 06, 28);
            var mapFile = _resolver.ResolveMapFile("SPXL", date);
            Assert.IsNotNull(mapFile);
            Assert.AreEqual("BGU", mapFile.GetMappedSymbol(date));
            Assert.AreEqual("SPXL", mapFile.GetMappedSymbol(date.AddDays(1)));
        }

        [Test]
        public void ResolvesRemappedAndDelistedSymbol()
        {
            var date = new DateTime(2018, 7, 23);
            var mapFile = _resolver.ResolveMapFile("TWX", date);
            Assert.IsNotNull(mapFile);
            Assert.AreEqual("AOL", mapFile.GetMappedSymbol(new DateTime(2000, 1, 1)));
            Assert.AreEqual("TWX", mapFile.GetMappedSymbol(new DateTime(2014, 1, 1)));
        }

        private static MapFileResolver CreateMapFileResolver()
        {
            return new MapFileResolver(new List<MapFile>
            {
                // remapped
                new MapFile("goog", new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(2014, 03, 27), "goocv"),
                    new MapFileRow(new DateTime(2014, 04, 02), "goocv"),
                    new MapFileRow(new DateTime(2050, 12, 31), "goog")
                }),
                new MapFile("googl", new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(2004, 08, 19), "goog"),
                    new MapFileRow(new DateTime(2014, 04, 02), "goog"),
                    new MapFileRow(new DateTime(2050, 12, 31), "googl")
                }),
                // remapped (with both map files)
                new MapFile("bgu", new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(2008, 11, 05), "bgu"),
                    new MapFileRow(new DateTime(2012, 06, 28), "bgu")
                }),
                new MapFile("spxl", new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(2008, 11, 05), "bgu"),
                    new MapFileRow(new DateTime(2012, 06, 28), "bgu"),
                    new MapFileRow(new DateTime(2050, 12, 31), "spxl")
                }),
                // straight mapping
                new MapFile("spy", new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(1998, 01, 02), "spy"),
                    new MapFileRow(new DateTime(2050, 12, 31), "spy")
                }),
                // new share class overtakes old share class same symbol
                new MapFile("oih.1", new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(2010, 02, 07), "oih"),
                    new MapFileRow(new DateTime(2011, 12, 20), "oih")
                }),
                new MapFile("oih", new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(2011, 12, 21), "oih"),
                    new MapFileRow(new DateTime(2050, 12, 31), "oih")
                }),
                // remapped + delisted
                new MapFile("twx.1", new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(1998, 1, 2), "twx"),
                    new MapFileRow(new DateTime(2001, 1, 11), "twx")
                }),
                new MapFile("twx", new List<MapFileRow>
                {
                    new MapFileRow(new DateTime(1998, 1, 2), "aol"),
                    new MapFileRow(new DateTime(2003, 10, 15), "aol"),
                    new MapFileRow(new DateTime(2018, 6, 15), "twx")
                }),
            });
        }
    }
}