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
using System.Threading;
using QuantConnect.Util;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataProviders
{
    [TestFixture, Explicit("Requires configured api access, and also makes calls to data endpoints which are charging transactions")]
    public class ApiDataProviderTests
    {
        private ApiDataProvider _dataProvider;

        [OneTimeSetUp]
        public void Setup()
        {
            _dataProvider = new ApiDataProvider();
        }

        // Note: Most of these cases reflect real data files included in the repo. 
        // May be useful for other tests. Only ones that aren't included are map and factor file zips
        // CFD Cases
        [TestCase("cfd/oanda/daily/xauusd.zip", 2, true)]                           // Daily stale data
        [TestCase("cfd/oanda/daily/xauusd.zip", 0, false)]                          // Daily fresh data
        [TestCase("cfd/oanda/hour/xauusd.zip", 6, true)]                            // Hourly stale data
        [TestCase("cfd/oanda/hour/xauusd.zip", 0, false)]                           // Hourly fresh data
        [TestCase("cfd/oanda/minute/xauusd/20140501_quote.zip", 10, false)]         // Date based minute data
        [TestCase("cfd/oanda/second/xauusd/20140501_quote.zip", 10, false)]         // Date based second data
        [TestCase("cfd/oanda/tick/xauusd/20140501_quote.zip", 10, false)]           // Date based tick data
        // Crypto Cases
        [TestCase("crypto/coinbase/daily/btcusd_quote.zip", 6, true)]                   // Daily stale data
        [TestCase("crypto/coinbase/daily/btcusd_quote.zip", 0, false)]                  // Daily fresh data
        [TestCase("crypto/coinbase/minute/btcusd/20161007_trade.zip", 6, false)]        // Date based minute data
        [TestCase("crypto/coinbase/second/btcusd/20161007_trade.zip", 6, false)]        // Date based second data
        // Equity Cases
        [TestCase("equity/usa/daily/aaa.zip", 4, true)]                             // Daily stale data
        [TestCase("equity/usa/daily/aaa.zip", 0, false)]                            // Daily fresh data
        [TestCase("equity/usa/hour/aapl.zip", 4, true)]                             // Hourly stale data
        [TestCase("equity/usa/hour/aapl.zip", 0, false)]                            // Hourly fresh data
        [TestCase("equity/usa/minute/aapl/20140605_quote.zip", 0, false)]           // Date Based minute data
        [TestCase("equity/usa/second/aig/20131004_trade.zip", 0, false)]            // Date Based second data
        [TestCase("equity/usa/tick/aig/20131007_quote.zip", 0, false)]              // Date Based tick data
        [TestCase("equity/usa/factor_files/aaa.csv", 5, true)]                      // Stale factor file
        [TestCase("equity/usa/factor_files/aaa.csv", 0, false)]                     // Fresh factor file
        [TestCase("equity/usa/factor_files/factor_files_20210601.zip", 25, false)]  // Date based factor files ** Not included in repo
        [TestCase("equity/usa/map_files/aaa.csv", 5, true)]                         // Stale map file
        [TestCase("equity/usa/map_files/aaa.csv", 0, false)]                        // Fresh map file
        [TestCase("equity/usa/map_files/map_files_20210601.zip", 12, false)]        // Date based map files ** Not included in repo
        [TestCase("equity/usa/fundamental/coarse/20140324.csv", 5, false)]          // Date based fundamental coarse files
        [TestCase("equity/usa/fundamental/fine/aapl/20140228.csv", 30, false)]      // Date based fundamental fine files (Always false because we don't support fine files)
        [TestCase("equity/usa/shortable/testbrokerage/dates/20140325.csv", 3, false)] // Date based shortable files
        [TestCase("equity/usa/shortable/testbrokerage/symbols/aapl.csv", 10, true)] // Symbol based shortable files
        [TestCase("equity/usa/universes/daily/qctest/20131007.csv", 10, false)]     // Date based universe file
        // Equity Option Cases
        [TestCase("option/usa/minute/aapl/20140606_openinterest_american.zip", 10, false)]   // Date based minute data
        // Forex Cases
        [TestCase("forex/oanda/daily/eurgbp.zip", 0, false)]                        // Daily fresh Forex data
        [TestCase("forex/oanda/daily/eurgbp.zip", 10, true)]                        // Daily stale Forex data
        [TestCase("forex/oanda/hour/eurusd.zip", 0, false)]                         // Hourly fresh Forex data
        [TestCase("forex/oanda/hour/eurusd.zip", 10, true)]                         // Hourly stale Forex data
        [TestCase("forex/oanda/minute/eurusd/20140501_quote.zip", 10, false)]       // Date based Forex minute data
        [TestCase("forex/oanda/second/eurusd/20140501_quote.zip", 10, false)]       // Date based Forex second data
        [TestCase("forex/oanda/tick/eurusd/20140501_quote.zip", 10, false)]         // Date based Forex tick data
        // Price Future Cases False because Unsupported
        [TestCase("future/cboe/margins/VX.csv", 0, false)]                           // Fresh Margins data
        [TestCase("future/cboe/margins/VX.csv", 10, false)]                          // Stale Margins data
        [TestCase("future/comex/minute/gc/20131007_openinterest.zip", 10, false)]   // Date based minute data
        [TestCase("future/comex/tick/gc/20131007_openinterest.zip", 10, false)]     // Date based tick data
        // Future Option Cases ** All False because Unsupported
        [TestCase("futureoption/cme/minute/es/20200320/20200105_quote_american.zip", 10, false)]   // Date based minute data
        // Index Cases / Index Option Cases *Not included because not allowed to download
        public void ShouldDownloadTest(string file, int days, bool expected)
        {
            // First create our test file and set its time
            var path = Path.Combine("./ApiDataTests", file);
            var time = DateTime.Now - TimeSpan.FromDays(days);
            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
           
            File.Create(path).Dispose();
            File.SetLastWriteTime(path, time);

            var dataProvider = new ApiDataProviderTest();
            var result = dataProvider.NeedToDownloadExposed(path);
            Assert.AreEqual(expected, result);

            // Cleanup after test
            File.Delete(path);
        }

        // Alternative
        [TestCase("alternative/sec/aapl/19980108_8K.zip")]
        // CFD
        [TestCase("cfd/oanda/daily/ch20hkd.zip")]
        // Crypto
        [TestCase("crypto/coinbase/minute/btcusd/20150114_trade.zip")]
        // Equities
        [TestCase("equity/usa/shortable/atreyu/dates/20180117.csv")]
        [TestCase("equity/usa/factor_files/tsla.csv")]
        [TestCase("equity/usa/factor_files/factor_files_20210601.zip")]
        [TestCase("equity/usa/map_files/googl.csv")]
        [TestCase("equity/usa/map_files/map_files_20210601.zip")]
        // Equity Options
        [TestCase("option/usa/minute/aapl/20100603_quote_american.zip")]
        // Forex
        [TestCase("forex/oanda/minute/eurusd/20020516_quote.zip")]
        // Price Futures          * False because unsupported
        [TestCase("future/cbot/minute/zs/20090501_trade.zip", false)]
        [TestCase("future/sgx/margins/IN.csv", false)]
        // Auxiliary Data Future Cases true
        [TestCase("future/comex/map_files/map_files_20211225.zip", true)]
        [TestCase("future/cme/factor_files/factor_files_20211225.zip", true)]
        // Future Options   * False because unsupported
        [TestCase("futureoption/comex/minute/og/20120227/20120105_quote_american.zip", false)]
        // Index            * False because unsupported
        [TestCase("index/usa/minute/spx/19980217_trade.zip", false)]
        // Index Options    * False because unsupported
        [TestCase("indexoption/usa/minute/spx/20100603_quote_european.zip", false)]
        public void DownloadFiles(string file, bool expectedToExist = true)
        {
            var path = Path.Combine(Globals.DataFolder, file);

            // Delete it if we already have it
            if (File.Exists(path))
            {
                File.Delete(path);
                Assert.IsFalse(File.Exists(path));
            }

            // Go get the file
            var stream = _dataProvider.Fetch(path);

            if (expectedToExist)
            {
                Assert.IsNotNull(stream);
                stream.Dispose();
                Assert.IsTrue(File.Exists(path));
            }
            else
            {
                Assert.IsNull(stream);
                Assert.IsFalse(File.Exists(path));
            }
        }

        [Test]
        public void DownloadsFileOnceConcurrently()
        {
            var dataProvider = new ApiDataProviderTest();

            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                    dataProvider.Fetch(Path.Combine(Globals.DataFolder, "equity", Market.USA, "hour", "f.zip"))));
            }

            tasks.ForEach(task => task.Wait());
            dataProvider.DisposeSafely();
            Assert.AreEqual(1, dataProvider.DownLoadCount);
        }

        private class ApiDataProviderTest : ApiDataProvider
        {
            public int DownLoadCount;

            public bool NeedToDownloadExposed(string filePath)
            {
                return base.NeedToDownload(filePath);
            }
            protected override bool DownloadData(string filePath)
            {
                Interlocked.Increment(ref DownLoadCount);
                // simulate download delay
                Thread.Sleep(100);
                return false;
            }
        }
    }
}
