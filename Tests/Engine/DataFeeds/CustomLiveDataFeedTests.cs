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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class CustomLiveDataFeedTests
    {
        [Test]
        public void EmitsDailyQuandlFutureDataOverWeekends()
        {
            var tickers = new[] { "CHRIS/CME_ES1", "CHRIS/CME_ES2" };
            var startDate = new DateTime(2018, 4, 1);
            var endDate = new DateTime(2018, 4, 20);

            // delete temp files
            foreach (var ticker in tickers)
            {
                var fileName = TestableQuandlFuture.GetLocalFileName(ticker, "test");
                File.Delete(fileName);
            }

            var algorithm = new QCAlgorithm();

            var symbols = tickers.Select(ticker => algorithm.AddData<TestableQuandlFuture>(ticker, Resolution.Daily).Symbol).ToList();

            algorithm.PostInitialize();

            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            timeProvider.SetCurrentTime(startDate);

            var dataPointsEmitted = 0;
            var feed = RunLiveDataFeed<TestableQuandlFuture>(algorithm, startDate, symbols, timeProvider);

            var lastFileWriteDate = DateTime.MinValue;

            // create a timer to advance time much faster than realtime and to simulate live Quandl data file updates
            var timerInterval = TimeSpan.FromMilliseconds(50);
            var timer = Ref.Create<Timer>(null);
            timer.Value = new Timer(state =>
            {
                // stop the timer to prevent reentrancy
                timer.Value.Change(Timeout.Infinite, Timeout.Infinite);

                var currentTime = timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork);

                if (currentTime.Date > endDate.Date)
                {
                    Log.Trace($"Total data points emitted: {dataPointsEmitted}");

                    feed.Exit();
                    return;
                }

                if (currentTime.Date > lastFileWriteDate.Date)
                {
                    foreach (var ticker in tickers)
                    {
                        var source = TestableQuandlFuture.GetLocalFileName(ticker, "csv");

                        // write new local file including only rows up to current date
                        var outputFileName = TestableQuandlFuture.GetLocalFileName(ticker, "test");

                        var sb = new StringBuilder();
                        {
                            using (var reader = new StreamReader(source))
                            {
                                var firstLine = true;
                                string line;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (firstLine)
                                    {
                                        sb.AppendLine(line);
                                        firstLine = false;
                                        continue;
                                    }

                                    var csv = line.Split(',');
                                    var time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                    if (time.Date >= currentTime.Date)
                                        break;

                                    sb.AppendLine(line);
                                }
                            }
                        }

                        if (currentTime.Date.DayOfWeek != DayOfWeek.Saturday && currentTime.Date.DayOfWeek != DayOfWeek.Sunday)
                        {
                            File.WriteAllText(outputFileName, sb.ToString());

                            Log.Trace($"Time:{currentTime} - Ticker:{ticker} - Files written:{++_countFilesWritten}");
                        }
                    }

                    lastFileWriteDate = currentTime;
                }

                // 30 minutes is the check interval for daily remote files, so we choose a smaller one to advance time
                timeProvider.Advance(TimeSpan.FromMinutes(15));

                //Log.Trace($"Time advanced to: {timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork)}");

                // restart the timer
                timer.Value.Change(timerInterval, timerInterval);

            }, null, TimeSpan.FromSeconds(2), timerInterval);

            try
            {
                foreach (var timeSlice in feed)
                {
                    foreach (var dataPoint in timeSlice.Slice.Values)
                    {
                        Log.Trace($"Data point emitted at {timeSlice.Slice.Time}: {dataPoint.Symbol.Value} {dataPoint.Value} {dataPoint.EndTime}");
                        dataPointsEmitted++;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Trace($"Error: {exception}");
            }

            Assert.AreEqual(14 * tickers.Length, dataPointsEmitted);
        }

        private static IDataFeed RunLiveDataFeed<T>(
            IAlgorithm algorithm,
            DateTime startDate,
            IEnumerable<Symbol> symbols,
            ITimeProvider timeProvider)
        {
            var feed = new TestableLiveTradingDataFeed(new FuncDataQueueHandler(x => Enumerable.Empty<BaseData>()), timeProvider);

            var mapFileProvider = new LocalDiskMapFileProvider();
            feed.Initialize(algorithm, new LiveNodePacket(), new BacktestingResultHandler(),
                mapFileProvider, new LocalDiskFactorFileProvider(mapFileProvider), new DefaultDataProvider());

            foreach (var symbol in symbols)
            {
                var config = new SubscriptionDataConfig(typeof(T), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, true, false, true);
                var request = new SubscriptionRequest(false, null, algorithm.Securities[symbol], config, startDate, Time.EndOfTime);
                feed.AddSubscription(request);
            }

            var feedThreadStarted = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                feedThreadStarted.Set();
                feed.Run();
            });

            feedThreadStarted.WaitOne();

            return feed;
        }

        private static int _countFilesWritten;

        public class TestableQuandlFuture : QuandlFuture
        {
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                // use local file instead of remote file
                var source = GetLocalFileName(config.Symbol.Value, "test");

                return new SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile);
            }

            public static string GetLocalFileName(string ticker, string fileExtension)
            {
                return $"./TestData/quandl_future_{ticker.Replace("/", "_").ToLower()}.{fileExtension}";
            }
        }
    }
}