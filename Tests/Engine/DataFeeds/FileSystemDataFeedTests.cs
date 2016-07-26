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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using Equity = QuantConnect.Algorithm.CSharp.Benchmarks.Symbols.Equity;

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
            public override void Initialize()
            {
                SetStartDate(1998, 1, 1);
                SetEndDate(2016, 3, 31);
                SetCash(100000);

                // no benchmark
                SetBenchmark(time => 0m);

                // Use 400 symbols
                //foreach (var ticker in Equity.All.Take(400))
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
