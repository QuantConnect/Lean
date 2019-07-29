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

using NUnit.Framework;
using QuantConnect.Data.Custom;
using QuantConnect.ToolBox;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QuantConnect.Tests.ToolBox
{
    /// <summary>
    /// The rationality is to test the main functionalities:
    ///     - Data is correctly parsed.
    ///     - Data is correctly saved.
    /// </summary>
    [TestFixture, Ignore("FXCM API goes down on weekends")]
    public class FxcmVolumeDownloaderTest
    {
        private string _dataDirectory;
        private List<string> _testingTempFolders = new List<string>();

        private FxcmVolumeDownloader _downloader;
        private readonly Symbol _eurusd = Symbol.Create("EURUSD", SecurityType.Base, Market.FXCM);

        [SetUp]
        public void SetUpTemporatyFolder()
        {
            var randomFolder = Guid.NewGuid().ToStringInvariant("N").Substring(startIndex: 0, length: 8);
            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _dataDirectory = Path.Combine(assemblyFolder, randomFolder);
            _downloader = new FxcmVolumeDownloader(_dataDirectory);
            _testingTempFolders.Add(_dataDirectory);
        }

        [TestFixtureTearDown]
        public void CleanTemporaryFolder()
        {
            foreach (var testingTempFolder in _testingTempFolders)
            {
                if (Directory.Exists(testingTempFolder))
                {
                    Directory.Delete(testingTempFolder, true);
                }
            }
        }

        [TestCase("./TestData/fxVolumeDaily.csv", "EURUSD", Resolution.Daily, "2016-12-01", "2017-01-30")]
        [TestCase("./TestData/fxVolumeHourly.csv", "USDJPY", Resolution.Hour, "2014-12-24", "2015-01-05")]
        [TestCase("./TestData/fxVolumeMinute.csv", "EURUSD", Resolution.Minute, "2012-11-23", "2012-11-27")]
        public void DataIsCorrectlyParsed(string testingFilePath, string ticker, Resolution resolution, string startDate, string endDate)
        {
            //Arrange
            var expectedData = File.ReadAllLines(testingFilePath)
                .Skip(count: 1) // Skip headers.
                .Select(x => x.Split(','))
                .ToArray();
            var symbol = Symbol.Create(ticker, SecurityType.Base, Market.FXCM);
            var startUtc = Parse.DateTimeExact(startDate, "yyyy-MM-dd");
            var endUtc = Parse.DateTimeExact(endDate, "yyyy-MM-dd");
            //Act
            var actualData = _downloader.Get(symbol, resolution, startUtc,
                endUtc).Cast<FxcmVolume>().ToArray();
            //Assert
            Assert.AreEqual(expectedData.Length, actualData.Length);
            for (var i = 0; i < expectedData.Length - 1; i++)
            {
                Assert.AreEqual(expectedData[i][0], actualData[i].Time.ToStringInvariant("yyyy/MM/dd HH:mm"));
                Assert.AreEqual(expectedData[i][1], actualData[i].Value.ToString(CultureInfo.InvariantCulture));
                Assert.AreEqual(expectedData[i][2], actualData[i].Transactions.ToString(CultureInfo.InvariantCulture));
            }
        }

        [TestCase("GBPUSD", Resolution.Daily, "2015-11-27", 20)]
        [TestCase("USDCAD", Resolution.Hour, "2016-09-15", 5)]
        [TestCase("EURJPY", Resolution.Minute, "2015-01-26", 2)]
        public void ParsedDataIsCorrectlySaved(string ticker, Resolution resolution, string startDate, int requestLength)
        {
            // Arrange
            var symbol = Symbol.Create(ticker, SecurityType.Base, Market.FXCM);
            var startUtc = Parse.DateTimeExact(startDate, "yyyy-MM-dd");
            var endUtc = startUtc.AddDays(requestLength);
            var data = _downloader.Get(symbol, resolution, startUtc, endUtc);
            // Act
            var writer = new FxcmVolumeWriter(resolution, symbol, _dataDirectory);
            writer.Write(data);
            // Assert
            var expectedData = data.Cast<FxcmVolume>().ToArray();
            var expectedFolder = Path.Combine(_dataDirectory, $"forex/fxcm/{resolution.ToLower()}");
            if (resolution == Resolution.Minute)
            {
                expectedFolder = Path.Combine(expectedFolder, symbol.Value.ToLowerInvariant());
            }
            Assert.True(Directory.Exists(expectedFolder));

            if (resolution == Resolution.Minute)
            {
                var zipFiles = Directory.GetFiles(expectedFolder, "*_volume.zip").Length;
                // Minus one because the Downloader starts one day earlier.
                Assert.AreEqual(requestLength + 1, zipFiles);
            }
            else
            {
                var expectedFilename = $"{symbol.Value.ToLowerInvariant()}_volume.zip";
                Assert.True(File.Exists(Path.Combine(expectedFolder, expectedFilename)));
            }

            var actualdata = FxcmVolumeAuxiliaryMethods.ReadZipFolderData(expectedFolder);
            Assert.AreEqual(expectedData.Length, actualdata.Count);

            var lines = actualdata.Count;
            for (var i = 0; i < lines - 1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, Parse.Long(actualdata[i][1]));
                Assert.AreEqual(expectedData[i].Transactions, Parse.Int(actualdata[i][2]));
            }
        }

        [Ignore("Long test")]
        [Test]
        public void RequestWithMoreThan10KMinuteObservationIsCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Minute;
            var startDate = new DateTime(year: 2013, month: 04, day: 01);
            var endDate = startDate.AddMonths(months: 1);
            // Act
            _downloader.Run(_eurusd, resolution, startDate, endDate);
            // Assert
            var outputFolder = Path.Combine(_dataDirectory, "forex/fxcm/minute");
            var files = Directory.GetFiles(outputFolder, "*_volume.zip", SearchOption.AllDirectories);
            Assert.AreEqual(expected: 27, actual: files.Length);
        }

        [Ignore("Long test")]
        [Test]
        public void RequestWithMoreThan10KHourlyObservationIsCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Hour;
            var startDate = new DateTime(year: 2014, month: 01, day: 01);
            var endDate = startDate.AddYears(value: 3);
            // Act
            _downloader.Run(_eurusd, resolution, startDate, endDate);
            // Assert
            var outputFile = Path.Combine(_dataDirectory, "forex/fxcm/hour/eurusd_volume.zip");
            var observationsCount = FxcmVolumeAuxiliaryMethods.ReadZipFileData(outputFile).Count;
            // 3 years x 52 weeks x 5 days x 24 hours = 18720 hours at least.
            Assert.True(observationsCount >= 18720, $"Actual observations: {observationsCount}");
        }
    }
}