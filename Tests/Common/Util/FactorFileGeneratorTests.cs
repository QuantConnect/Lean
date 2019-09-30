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
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.ToolBox;
using QuantConnect.ToolBox.YahooDownloader;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    // For now these tests are excluded from the Travis build because of occasional Yahoo server errors.
    // In future they should be updated to read the Yahoo data from a local test file.
    [TestFixture, Category("TravisExclude")]
    public class FactorFileGeneratorTests
    {
        private const string PermTick = "AAPL";
        private const string Market = "usa";
        readonly Symbol _symbol = new Symbol(SecurityIdentifier.GenerateEquity(PermTick, Market), PermTick);
        private readonly string _dataPath = LeanData.GenerateZipFilePath(Config.Get("data-folder"),
                                                                        new Symbol(SecurityIdentifier.GenerateEquity(PermTick, Market), PermTick),
                                                                        DateTime.MaxValue,
                                                                        Resolution.Daily,
                                                                        TickType.Quote);

        private FactorFileGenerator _factorFileGenerator;
        private YahooDataDownloader _yahooDataDownloader;

        [TestFixtureSetUp]
        public void Setup()
        {
            _factorFileGenerator = new FactorFileGenerator(_symbol, _dataPath);
            _yahooDataDownloader = new YahooDataDownloader();
        }

        [Test]
        public void SplitsAndDividends_CanBeDownloadedFromYahoo_Successfully()
        {
            var yahooEvents = _yahooDataDownloader.DownloadSplitAndDividendData(_symbol, DateTime.MinValue, DateTime.MaxValue);

            Assert.IsTrue(yahooEvents.Any());
        }

        [Test]
        public void FactorFile_CanBeCreatedFromYahooData_Successfully()
        {
            var yahooEvents = _yahooDataDownloader.DownloadSplitAndDividendData(_symbol, Parse.DateTime("01/01/1980"), DateTime.MaxValue);
            var factorFile = _factorFileGenerator.CreateFactorFile(yahooEvents.ToList());

            Assert.IsTrue(factorFile.Permtick == _symbol.Value);
        }

        [Test, Ignore("Fix me - GH issue 3435")]
        public void FactorFiles_CanBeGenerated_Accurately()
        {
            // Arrange
            var yahooEvents = _yahooDataDownloader.DownloadSplitAndDividendData(_symbol, Parse.DateTime("01/01/1970"), DateTime.MaxValue);
            var filePath = LeanData.GenerateRelativeFactorFilePath(_symbol);
            var tolerance = 0.00001m;

            if (!File.Exists(filePath))
                throw new ArgumentException("This test requires an already calculated factor file." +
                                            "Try using one of the pre-existing factor files ");

            var originalFactorFileInstance = FactorFile.Read(PermTick, Market);

            // Act
            var newFactorFileInstance = _factorFileGenerator.CreateFactorFile(yahooEvents.ToList());

            var earliestDate = originalFactorFileInstance.SortedFactorFileData.First().Key;
            var latestDate = originalFactorFileInstance.SortedFactorFileData.Last().Key;

            // Assert
            Assert.AreEqual(originalFactorFileInstance.SortedFactorFileData.Count,
                            newFactorFileInstance.SortedFactorFileData.Count);

            for (var i = earliestDate; i < latestDate; i = i.AddDays(1))
            {
                FactorFileRow expected = null;
                FactorFileRow actual = null;

                originalFactorFileInstance.SortedFactorFileData.TryGetValue(i, out expected);
                newFactorFileInstance.SortedFactorFileData.TryGetValue(i, out actual);

                if (expected == null || actual == null)
                {
                    Assert.IsTrue(actual == null);
                    Assert.IsTrue(expected == null);
                }
                else
                {
                    Assert.IsTrue(Math.Abs(expected.PriceFactor - actual.PriceFactor) < tolerance);
                    Assert.IsTrue(Math.Abs(expected.SplitFactor - actual.SplitFactor) < tolerance);
                }
            }
        }
    }
}