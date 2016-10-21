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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.ToolBox;
using QuantConnect.ToolBox.YahooDownloader;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class FactorFileGeneratorTests
    {
        // Symbol can be choosen
        Symbol symbol = new Symbol(SecurityIdentifier.GenerateEquity("ETN", "usa"), "1");
        private string dataPath = LeanData.GenerateZipFilePath(Config.Get("data-folder"), new Symbol(SecurityIdentifier.GenerateEquity("ETN", "usa"), "ETN"), DateTime.MaxValue, Resolution.Daily, TickType.Quote);

        [Test]
        public void SplitsAndDividends_CanBeDownloadedFromYahoo_Successfully()
        {
            var yahooDataDownloader = new YahooDataDownloader();

            var yahooEvents = yahooDataDownloader.DownloadSplitAndDividendData(symbol, DateTime.MinValue, DateTime.MaxValue);

            Assert.IsTrue(yahooEvents.Any());
        }


        [Test]
        public void DailyEquityData_CanBeRead_Successfully()
        {
            var factorFileGenerator = new FactorFileGenerator(symbol, dataPath);

            Assert.IsTrue(factorFileGenerator.DailyDataForEquity.Any());
        }

        [Test]
        public void FactorFile_CanBeCreatedFromYahooData_Successfully()
        {
            var factorFileGenerator = new FactorFileGenerator(symbol, dataPath);
            var yahooDataDownloader = new YahooDataDownloader();

            var yahooEvents = yahooDataDownloader.DownloadSplitAndDividendData(symbol, DateTime.Parse("01/01/1980"), DateTime.MaxValue);
            var factorFile = factorFileGenerator.CreateFactorFileFromData(yahooEvents);

            Assert.IsTrue(factorFile.Permtick == symbol.Value);
        }

        [Test]
        public void SameDaySplitsAndDividends_AreRearranged_Successfully()
        {
            var factorFileGenerator = new FactorFileGenerator(symbol, dataPath);
            var date = DateTime.Parse("1/11/2010");
            var marketEventsList = new List<BaseData>()
            {
                new Split(symbol, date, 1.0M, 1.0M),
                new Dividend(symbol, date, 3.0M),
            };

            var marketEventsQueue = new Queue<BaseData>(marketEventsList);

            var factorFile = factorFileGenerator.CreateFactorFileFromData(marketEventsQueue);

            // There should be 4, the first one in the future, the two added and one for the last date of the data
            Assert.IsTrue(factorFile.SortedFactorFileData.Count == 4);
            //Assert.IsTrue(factorFile.SortedFactorFileData. == typeof(Split));
        }

        [Test]
        public void FactorFiles_CanBeWritten_Successfully()
        {
            var factorFileGenerator = new FactorFileGenerator(symbol, dataPath);
            var yahooDataDownloader = new YahooDataDownloader();

            var yahooEvents = yahooDataDownloader.DownloadSplitAndDividendData(symbol, DateTime.Parse("01/01/1970"), DateTime.MaxValue);
            var factorFile = factorFileGenerator.CreateFactorFileFromData(yahooEvents);

            factorFile.WriteToCsv(symbol);
        }
    }
}