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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class CustomLiveDataFeedTests
    {
        private LiveSynchronizer _synchronizer;
        private IDataFeed _feed;

        [Test]
        public void EmitsDailyQuandlFutureDataOverWeekends()
        {
            RemoteFileSubscriptionStreamReader.SetDownloadProvider(new Api.Api());
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
            CreateDataFeed();
            var dataManager = new DataManagerStub(algorithm, _feed);
            algorithm.SubscriptionManager.SetDataManager(dataManager);

            var symbols = tickers.Select(ticker => algorithm.AddData<TestableQuandlFuture>(ticker, Resolution.Daily).Symbol).ToList();

            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            timeProvider.SetCurrentTime(startDate);

            var dataPointsEmitted = 0;
            RunLiveDataFeed(algorithm, startDate, symbols, timeProvider, dataManager);

            var cancellationTokenSource = new CancellationTokenSource();
            var lastFileWriteDate = DateTime.MinValue;

            // create a timer to advance time much faster than realtime and to simulate live Quandl data file updates
            var timerInterval = TimeSpan.FromMilliseconds(20);
            var timer = Ref.Create<Timer>(null);
            timer.Value = new Timer(state =>
            {
                try
                {
                    var currentTime = timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork);

                    if (currentTime.Date > endDate.Date)
                    {
                        Log.Trace($"Total data points emitted: {dataPointsEmitted.ToStringInvariant()}");

                        _feed.Exit();
                        cancellationTokenSource.Cancel();
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
                                        var time = Parse.DateTimeExact(csv[0], "yyyy-MM-dd");
                                        if (time.Date >= currentTime.Date)
                                            break;

                                        sb.AppendLine(line);
                                    }
                                }
                            }

                            if (currentTime.Date.DayOfWeek != DayOfWeek.Saturday && currentTime.Date.DayOfWeek != DayOfWeek.Sunday)
                            {
                                var fileContent = sb.ToString();
                                try
                                {
                                    File.WriteAllText(outputFileName, fileContent);
                                }
                                catch (IOException)
                                {
                                    Log.Error("IOException: will sleep 200ms and retry once more");
                                    // lets sleep 200ms and retry once more, consumer could be reading the file
                                    // this exception happens in travis intermittently, GH issue 3273
                                    Thread.Sleep(200);
                                    File.WriteAllText(outputFileName, fileContent);
                                }

                                Log.Trace($"Time:{currentTime} - Ticker:{ticker} - Files written:{++_countFilesWritten}");
                            }
                        }

                        lastFileWriteDate = currentTime;
                    }

                    // 30 minutes is the check interval for daily remote files, so we choose a smaller one to advance time
                    timeProvider.Advance(TimeSpan.FromMinutes(20));

                    //Log.Trace($"Time advanced to: {timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork)}");

                    // restart the timer
                    timer.Value.Change(timerInterval.Milliseconds, Timeout.Infinite);

                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                    _feed.Exit();
                    cancellationTokenSource.Cancel();
                }
            }, null, timerInterval.Milliseconds, Timeout.Infinite);

            try
            {
                foreach (var timeSlice in _synchronizer.StreamData(cancellationTokenSource.Token))
                {
                    foreach (var dataPoint in timeSlice.Slice.Values)
                    {
                        Log.Trace($"Data point emitted at {timeSlice.Slice.Time.ToStringInvariant()}: " +
                            $"{dataPoint.Symbol.Value} {dataPoint.Value.ToStringInvariant()} " +
                            $"{dataPoint.EndTime.ToStringInvariant()}"
                        );

                        dataPointsEmitted++;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Trace($"Error: {exception}");
            }

            timer.Value.Dispose();
            dataManager.RemoveAllSubscriptions();
            Assert.AreEqual(14 * tickers.Length, dataPointsEmitted);
        }


        [Test, Ignore("To run this test please set a valid Quandl token")]
        public void RemoteDataDoesNotIncreaseNumberOfSlices()
        {
            Config.Set("quandl-auth-token", "QUANDL-TOKEN");

            var startDate = new DateTime(2018, 4, 2);
            var endDate = new DateTime(2018, 4, 19);
            var algorithm = new QCAlgorithm();

            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            timeProvider.SetCurrentTime(startDate);
            var dataQueueHandler = new FuncDataQueueHandler(fdqh =>
            {
                var time = timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork);
                var tick = new Tick(time, Symbols.SPY, 1.3m, 1.2m, 1.3m)
                {
                    TickType = TickType.Trade
                };
                var tick2 = new Tick(time, Symbols.AAPL, 1.3m, 1.2m, 1.3m)
                {
                    TickType = TickType.Trade
                };
                return new[] { tick, tick2 };
            });
            CreateDataFeed(dataQueueHandler);
            var dataManager = new DataManagerStub(algorithm, _feed);

            algorithm.SubscriptionManager.SetDataManager(dataManager);
            var symbols = new List<Symbol>
            {
                algorithm.AddData<Quandl>("CBOE/VXV", Resolution.Daily).Symbol,
                algorithm.AddData<QuandlVix>("CBOE/VIX", Resolution.Daily).Symbol,
                algorithm.AddEquity("SPY", Resolution.Daily).Symbol,
                algorithm.AddEquity("AAPL", Resolution.Daily).Symbol
            };
            algorithm.PostInitialize();

            var cancellationTokenSource = new CancellationTokenSource();

            var dataPointsEmitted = 0;
            var slicesEmitted = 0;

            RunLiveDataFeed(algorithm, startDate, symbols, timeProvider, dataManager);
            Thread.Sleep(5000); // Give remote sources a handicap, so the data is available in time

            // create a timer to advance time much faster than realtime and to simulate live Quandl data file updates
            var timerInterval = TimeSpan.FromMilliseconds(100);
            var timer = Ref.Create<Timer>(null);
            timer.Value = new Timer(state =>
            {
                // stop the timer to prevent reentrancy
                timer.Value.Change(Timeout.Infinite, Timeout.Infinite);

                var currentTime = timeProvider.GetUtcNow().ConvertFromUtc(TimeZones.NewYork);

                if (currentTime.Date > endDate.Date)
                {
                    _feed.Exit();
                    cancellationTokenSource.Cancel();
                    return;
                }

                timeProvider.Advance(TimeSpan.FromHours(3));

                // restart the timer
                timer.Value.Change(timerInterval, timerInterval);

            }, null, TimeSpan.FromSeconds(2), timerInterval);

            try
            {
                foreach (var timeSlice in _synchronizer.StreamData(cancellationTokenSource.Token))
                {
                    if (timeSlice.Slice.HasData)
                    {
                        slicesEmitted++;
                        dataPointsEmitted += timeSlice.Slice.Values.Count;
                        Assert.IsTrue(timeSlice.Slice.Values.Any(x => x.Symbol == symbols[0]), $"Slice doesn't contain {symbols[0]}");
                        Assert.IsTrue(timeSlice.Slice.Values.Any(x => x.Symbol == symbols[1]), $"Slice doesn't contain {symbols[1]}");
                        Assert.IsTrue(timeSlice.Slice.Values.Any(x => x.Symbol == symbols[2]), $"Slice doesn't contain {symbols[2]}");
                        Assert.IsTrue(timeSlice.Slice.Values.Any(x => x.Symbol == symbols[3]), $"Slice doesn't contain {symbols[3]}");
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Trace($"Error: {exception}");
            }

            timer.Value.Dispose();
            dataManager.RemoveAllSubscriptions();
            Assert.AreEqual(14, slicesEmitted);
            Assert.AreEqual(14 * symbols.Count, dataPointsEmitted);
        }

        private void CreateDataFeed(
            FuncDataQueueHandler funcDataQueueHandler = null)
        {
            _feed = new TestableLiveTradingDataFeed(funcDataQueueHandler ?? new FuncDataQueueHandler(x => Enumerable.Empty<BaseData>()));
        }

        private void RunLiveDataFeed(
            IAlgorithm algorithm,
            DateTime startDate,
            IEnumerable<Symbol> symbols,
            ITimeProvider timeProvider,
            DataManager dataManager)
        {
            _synchronizer = new TestableLiveSynchronizer(timeProvider);
            _synchronizer.Initialize(algorithm, dataManager);

            var mapFileProvider = new LocalDiskMapFileProvider();
            _feed.Initialize(algorithm, new LiveNodePacket(), new BacktestingResultHandler(),
                mapFileProvider, new LocalDiskFactorFileProvider(mapFileProvider), new DefaultDataProvider(), dataManager, _synchronizer, new DataChannelProvider());

            foreach (var symbol in symbols)
            {
                var config = algorithm.Securities[symbol].SubscriptionDataConfig;
                var request = new SubscriptionRequest(false, null, algorithm.Securities[symbol], config, startDate, Time.EndOfTime);
                dataManager.AddSubscription(request);
            }
        }

        private static int _countFilesWritten;

        public class TestableQuandlFuture : Quandl
        {
            public TestableQuandlFuture()
                : base("Settle")
            {
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                // use local file instead of remote file
                var source = GetLocalFileName(config.Symbol.Value, "test");

                return new SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile);
            }

            public static string GetLocalFileName(string ticker, string fileExtension)
            {
                return $"./TestData/quandl_future_{ticker.Replace("/", "_").ToLowerInvariant()}.{fileExtension}";
            }
        }
    }
}