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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Category("TravisExclude")]
    public class FileSystemDataFeedTests
    {
        [Test]
        public void TestsFileSystemDataFeedSpeed()
        {
            var job = new BacktestNodePacket();
            var resultHandler = new BacktestingResultHandler();
            var mapFileProvider = new LocalDiskMapFileProvider();
            var factorFileProvider = new LocalDiskFactorFileProvider(mapFileProvider);

            var algorithm = new BenchmarkTest();
            var feed = new FileSystemDataFeed();

            feed.Initialize(algorithm, job, resultHandler, mapFileProvider, factorFileProvider);
            algorithm.Initialize();

            var feedThreadStarted = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                feedThreadStarted.Set();
                feed.Run();
            });
            feedThreadStarted.WaitOne();

            var stopwatch = Stopwatch.StartNew();
            var lastMonth = -1;
            var count = 0;
            foreach (var timeSlice in feed)
            {
                if (timeSlice.Time.Month != lastMonth)
                {
                    Console.WriteLine(DateTime.Now + " - Time: " + timeSlice.Time);
                    lastMonth = timeSlice.Time.Month;
                }
                count++;
            }
            Console.WriteLine("Count: " + count);

            stopwatch.Stop();
            Console.WriteLine("Elapsed time: " + stopwatch.Elapsed);
        }

        public class BenchmarkTest : QCAlgorithm
        {
            private readonly string[] _tickers =
            {
                // 400 symbols

                //"spy", "aapl", "fb", "vxx", "vrx", "nflx", "uvxy", "qqq", "iwm", "baba", "gild", "xiv", "xom", "cvx", "msft", "ge", "slb", "jpm", "xle",
                //"dis", "amzn", "twtr", "pfe", "c", "bac", "abbv", "jnj", "hal", "xlv", "intc", "wfc", "v", "yhoo", "cop", "myl", "agn", "wmt", "kmi",
                //"mrk", "tsla", "gdx", "lly", "fcx", "cat", "celg", "qcom", "mcd", "cmcsa", "xop", "cvs", "amgn", "dow", "aal", "apc", "sune", "mu", "vlo",
                //"sbux", "wmb", "pg", "eog", "dvn", "bmy", "apa", "unh", "eem", "ibm", "nke", "t", "hd", "unp", "dal", "endp", "csco", "oxy", "mro", "mdt",
                //"txn", "wll", "orcl", "googl", "ual", "wynn", "ms", "hznp", "biib", "vz", "gm", "nbl", "twx", "swks", "jd", "hca", "avgo", "yum", "ko",
                //"goog", "gs", "pep", "aig", "emc", "bidu", "clr", "pypl", "lvs", "swn", "axp", "atvi", "rrc", "wba", "mpc", "nxpi", "ete", "nov", "foxa",
                //"sndk", "dia", "utx", "dd", "wdc", "aa", "m", "fxi", "rig", "ma", "dust", "tgt", "aet", "ebay", "luv", "efa", "brk.b", "ba", "met", "lyb",
                //"svxy", "uwti", "hon", "hpq", "oas", "abt", "mo", "esrx", "teva", "stx", "ibb", "f", "cbs", "tlt", "pm", "esv", "ne", "psx", "schw", "mon",
                //"hes", "gpro", "tvix", "mnk", "nvda", "nfx", "uso", "nugt", "ewz", "low", "ua", "tna", "xly", "mmm", "pxd", "viab", "mdlz", "nem", "usb",
                //"mur", "etn", "feye", "pten", "oih", "ups", "chk", "dhr", "rai", "tqqq", "ccl", "brcm", "dg", "jblu", "crm", "adbe", "cog", "pbr", "hp",
                //"bhi", "bk", "tjx", "de", "cof", "incy", "dhi", "abc", "xli", "zts", "bp", "iyr", "pnc", "cnx", "xlf", "lrcx", "gg", "rds.a", "wfm", "tso",
                //"antm", "kss", "ea", "pru", "rad", "wft", "xbi", "thc", "vwo", "ctsh", "abx", "vmw", "csx", "acn", "emr", "se", "mjn", "skx", "ace", "p",
                //"cmi", "cl", "cah", "exc", "duk", "amat", "aem", "fti", "stt", "ilmn", "hog", "kr", "expe", "vrtx", "ivv", "cam", "gps", "mck", "adsk",
                //"cmcsk", "htz", "mgm", "dltr", "sti", "cyh", "mos", "cnq", "glw", "key", "kors", "siri", "epd", "su", "dfs", "tmo", "tap", "hst", "nbr",
                //"eqt", "xlu", "bsx", "cost", "ctrp", "hfc", "vnq", "trv", "pot", "cern", "lltc", "do", "adi", "bax", "amt", "uri", "uco", "eca", "mas",
                //"all", "pcar", "vips", "atw", "spxu", "lnkd", "x", "tsm", "so", "bbt", "syf", "vfc", "cxo", "ir", "pwr", "gld", "lng", "etp", "jnpr", "mat",
                //"klac", "xlk", "trip", "aep", "vtr", "rost", "rdc", "cf", "fas", "hcn", "ar", "sm", "wpx", "d", "hot", "prgo", "alxn", "cnc", "vale", "jcp",
                //"gdxj", "oke", "adm", "joy", "tsn", "mar", "khc", "nsc", "cma", "coh", "gmcr", "fl", "fitb", "bhp", "jwn", "dnr", "pbf", "xlnx", "phm", "hig",
                //"ppg", "mbly", "itub", "fdx", "ip", "el", "len", "afl", "hlt", "xrx", "nee", "rcl", "nrg", "wtw", "qep", "jah", "sqqq", "rio", "cfg", "pah",
                //"xlb", "rspp", "fox", "lbtyk", "fast", "xlp", "tza", "kmb", "hcp", "jci", "lmt", "spg", "hbi", "bby", "bmrn", "ben", "gis", "pcg", "wy", "rtn",
                //"disca", "ivz", "slw", "tck", "bwa", "nue", "adp", "cci", "lnc", "fold"

                // 2 symbols

                //"aapl", "spy"

                // 1 symbol

                //"ibm",
            };

            public override void Initialize()
            {
                SetStartDate(1998, 1, 1);
                SetEndDate(2016, 3, 31);
                SetCash(100000);

                // no benchmark
                SetBenchmark(time => 0m);

                // Use all symbols above
                //foreach (var ticker in _tickers)
                //{
                //    AddEquity(ticker, Resolution.Minute);
                //}

                // Use only two symbols with or without FillForward
                AddEquity("SPY", Resolution.Daily, "usa", true);
                AddEquity("IBM", Resolution.Minute, "usa", false);
            }
        }
    }
}
