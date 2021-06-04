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
using System.IO;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataProviders
{
    [TestFixture, Explicit("Requires configured api access, and also makes calls to data endpoints which are charging transactions")]
    public class ApiDataProviderTests
    {
        [TestCase(Resolution.Daily, 6, true)]
        [TestCase(Resolution.Daily, 3, false)]
        [TestCase(Resolution.Hour, 6, true)]
        [TestCase(Resolution.Hour, 3, false)]
        [TestCase(Resolution.Minute, 6, false)]
        [TestCase(Resolution.Second, 6, false)]
        [TestCase(Resolution.Tick, 6, false)]
        public void OutOfDateTest(Resolution resolution, int days, bool expected)
        {
            var path = "./testfile.txt";
            var time = DateTime.Now - TimeSpan.FromDays(days);

            File.Create(path).Close();
            File.SetLastWriteTime(path, time);
            var test = ApiDataProvider.IsOutOfDate(resolution, path);

            Assert.AreEqual(expected, test);
            File.Delete(path);
        }

        [TestCase("forex/oanda/minute/eurusd/20131011_quote.zip")]
        [TestCase("equity/usa/factor_files/tsla.csv")]
        [TestCase("equity/usa/factor_files/factor_files_20210601.zip")]
        [TestCase("equity/usa/map_files/googl.csv")]
        [TestCase("equity/usa/map_files/map_files_20210601.zip")]
        [TestCase("crypto/gdax/daily/btcusd_quote.zip")]
        [TestCase("crypto/bitfinex/hour/ethusd_quote.zip")]
        [TestCase("option/usa/minute/aapl/20210601_openinterest_american.zip")]
        [TestCase("future/sgx/margins/IN.csv")]
        public void DownloadFiles(string file)
        {
            var dataProvider = new ApiDataProvider();
            var path = Path.Combine(Globals.DataFolder, file);

            // Delete it if we already have it
            if (File.Exists(path))
            {
                File.Delete(path);
                Assert.IsFalse(File.Exists(path));
            }

            // Go get the file
            var stream = dataProvider.Fetch(path);
            Assert.IsNotNull(stream);
            stream.Dispose();
            
            Assert.IsTrue(File.Exists(path));
        }
    }
}
