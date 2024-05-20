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
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;
using QuantConnect.Lean.Engine.Storage;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class CustomLiveDataFeedTests
    {
        private LiveSynchronizer _synchronizer;
        private IDataFeed _feed;

        [TearDown]
        public void TearDown()
        {
            _feed.Exit();
            _synchronizer.DisposeSafely();
        }

        [Test]
        public void EmitsDailyCustomFutureDataOverWeekends()
        {
            using var api = new Api.Api();
            RemoteFileSubscriptionStreamReader.SetDownloadProvider(api);
            var tickers = new[] { "CHRIS/CME_ES1", "CHRIS/CME_ES2" };
            var startDate = new DateTime(2018, 4, 1);
            var endDate = new DateTime(2018, 4, 20);

            // delete temp files
            foreach (var ticker in tickers)
            {
                var fileName = TestableCustomFuture.GetLocalFileName(ticker, "test");
                File.Delete(fileName);
            }

            var algorithm = new QCAlgorithm();
            CreateDataFeed(algorithm.Settings);
            var dataManager = new DataManagerStub(algorithm, _feed);
            algorithm.SubscriptionManager.SetDataManager(dataManager);

            var symbols = tickers.Select(ticker => algorithm.AddData<TestableCustomFuture>(ticker, Resolution.Daily).Symbol).ToList();

            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            timeProvider.SetCurrentTime(startDate);

            var dataPointsEmitted = 0;
            RunLiveDataFeed(algorithm, startDate, symbols, timeProvider, dataManager);

            using var cancellationTokenSource = new CancellationTokenSource();
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
                            var source = TestableCustomFuture.GetLocalFileName(ticker, "csv");

                            // write new local file including only rows up to current date
                            var outputFileName = TestableCustomFuture.GetLocalFileName(ticker, "test");

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


        [Test]
        public void RemoteDataDoesNotIncreaseNumberOfSlices()
        {
            using var api = new Api.Api();
            RemoteFileSubscriptionStreamReader.SetDownloadProvider(api);

            var startDate = new DateTime(2017, 4, 2);
            var endDate = new DateTime(2017, 4, 23);
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
            }, timeProvider, algorithm.Settings);
            CreateDataFeed(algorithm.Settings, dataQueueHandler);
            var dataManager = new DataManagerStub(algorithm, _feed);

            algorithm.SubscriptionManager.SetDataManager(dataManager);
            var symbols = new List<Symbol>
            {
                algorithm.AddData<TestableRemoteCustomData>("FB", Resolution.Daily).Symbol,
                algorithm.AddData<TestableRemoteCustomData>("IBM", Resolution.Daily).Symbol,
                algorithm.AddEquity("SPY", Resolution.Daily).Symbol,
                algorithm.AddEquity("AAPL", Resolution.Daily).Symbol
            };

            using var cancellationTokenSource = new CancellationTokenSource();

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
            dataQueueHandler.DisposeSafely();
            Assert.AreEqual(14, slicesEmitted);
            Assert.AreEqual(14 * symbols.Count, dataPointsEmitted);
        }

        [Test]
        public void LiveDataFeedSourcesDataFromObjectStore()
        {
            using var api = new Api.Api();
            RemoteFileSubscriptionStreamReader.SetDownloadProvider(api);

            var startDate = new DateTime(2017, 4, 2);
            var endDate = new DateTime(2017, 4, 23);
            var algorithm = new QCAlgorithm();

            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            timeProvider.SetCurrentTime(startDate);

            CreateDataFeed(algorithm.Settings);
            var dataManager = new DataManagerStub(algorithm, _feed);
            algorithm.SubscriptionManager.SetDataManager(dataManager);

            using var store = new LocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StoragePermissions = FileAccess.ReadWrite });
            algorithm.SetObjectStore(store);
            algorithm.ObjectStore.Save("CustomData/CustomIBM", "2017-04-03,173.82\n2017-04-04,173.52\n2017-04-05,174.7\n2017-04-06,173.47\n2017-04-07,172.08\n2017-04-10,172.53\n2017-04-11,170.65\n2017-04-12,171.04\n2017-04-13,169.92\n2017-04-17,169.75\n2017-04-18,170.79\n2017-04-19,161.76\n2017-04-20,161.32\n2017-04-21,162.05\n2017-04-24,161.29\n2017-04-25,161.78\n2017-04-26,160.53\n2017-04-27,160.29\n2017-04-28,160.5");

            var symbol = algorithm.AddData<TestableObjectStoreCustomData>("IBM", Resolution.Daily).Symbol;

            using var cancellationTokenSource = new CancellationTokenSource();

            var dataPointsEmitted = 0;
            var slicesEmitted = 0;

            RunLiveDataFeed(algorithm, startDate, new[] { symbol }, timeProvider, dataManager);

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
                        Assert.AreEqual(symbol, timeSlice.Slice.Values.Single().Symbol, $"Slice doesn't contain {symbol}");
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
            Assert.AreEqual(slicesEmitted, dataPointsEmitted);
        }

        private void CreateDataFeed(IAlgorithmSettings settings,
            FuncDataQueueHandler funcDataQueueHandler = null)
        {
            _feed = new TestableLiveTradingDataFeed(settings, funcDataQueueHandler ?? new FuncDataQueueHandler(x => Enumerable.Empty<BaseData>(), RealTimeProvider.Instance, new AlgorithmSettings()));
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

            _feed.Initialize(algorithm, new LiveNodePacket(), new BacktestingResultHandler(),
                TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, TestGlobals.DataProvider, dataManager, _synchronizer, new DataChannelProvider());

            foreach (var symbol in symbols)
            {
                var config = algorithm.Securities[symbol].SubscriptionDataConfig;
                var request = new SubscriptionRequest(false, null, algorithm.Securities[symbol], config, startDate, Time.EndOfTime);
                dataManager.AddSubscription(request);
            }
        }

        private static int _countFilesWritten;

        public class TestableCustomFuture : BaseData
        {
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                // use local file instead of remote file
                var source = GetLocalFileName(config.Symbol.Value, "test");

                return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
            }

            public static string GetLocalFileName(string ticker, string fileExtension)
            {
                return $"./TestData/custom_future_{ticker.Replace("/", "_").ToLowerInvariant()}.{fileExtension}";
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var csv = line.Split(',');

                var data = new TestableCustomFuture
                {
                    Symbol = config.Symbol,
                    Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Value = csv[6].ToDecimal()
                };

                return data;
            }
        }

        public class TestableRemoteCustomData : BaseData
        {
            public override DateTime EndTime
            {
                get { return Time + Period; }
                set { Time = value - Period; }
            }

            /// <summary>
            /// Gets a time span of one day
            /// </summary>
            public TimeSpan Period
            {
                get { return QuantConnect.Time.OneDay; }
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                var source = $"https://www.dl.dropboxusercontent.com/s/1w6x1kfrlvx3d2v/CustomIBM.csv?dl=0";
                return new SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var csv = line.Split(',');

                var data = new TestableRemoteCustomData
                {
                    Symbol = config.Symbol,
                    Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Value = csv[1].ToDecimal()
                };

                return data;
            }
        }

        public class TestableObjectStoreCustomData : TestableRemoteCustomData
        {
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource("CustomData/CustomIBM", SubscriptionTransportMedium.ObjectStore, FileFormat.Csv);
            }
        }
    }
}
