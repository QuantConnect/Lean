using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class LiveTradingDataFeedTest
    {
        [Test]
        public void SubscribesToOnlyEquityAndForex()
        {
            var algorithm = new AlgorithmStub(equities: new List<string> {"SPY"}, forex: new List<string> {"EURUSD"});
            algorithm.AddData<ForexRemoteFileBaseData>("RemoteFile");

            FakeDataQueueHandler dataQueueHandler;
            var feed = RunDataFeed(algorithm, out dataQueueHandler);

            Assert.AreEqual(2, dataQueueHandler.Subscriptions.Count);
            Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(new FakeDataQueueHandler.Subscription("SPY", SecurityType.Equity)));
            Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(new FakeDataQueueHandler.Subscription("EURUSD", SecurityType.Forex)));
            Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(new FakeDataQueueHandler.Subscription("RemoteFile", SecurityType.Base)));
        }

        [Test]
        public void UnsubscribesFromOnlyEquityAndForex()
        {
            var algorithm = new AlgorithmStub(equities: new List<string> { "SPY" }, forex: new List<string> { "EURUSD" });
            algorithm.AddData<ForexRemoteFileBaseData>("RemoteFile");

            FakeDataQueueHandler dataQueueHandler;
            var feed = RunDataFeed(algorithm, out dataQueueHandler);

            feed.RemoveSubscription(algorithm.Securities["SPY"]);

            Assert.AreEqual(1, dataQueueHandler.Subscriptions.Count);
            Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(new FakeDataQueueHandler.Subscription("SPY", SecurityType.Equity)));
            Assert.IsTrue(dataQueueHandler.Subscriptions.Contains(new FakeDataQueueHandler.Subscription("EURUSD", SecurityType.Forex)));
            Assert.IsFalse(dataQueueHandler.Subscriptions.Contains(new FakeDataQueueHandler.Subscription("RemoteFile", SecurityType.Base)));
        }

        [Test]
        public void EmitsForexDataWithRoundedUtcTimes()
        {
            var algorithm = new AlgorithmStub(forex: new List<string>{"EURUSD"});

            var feed = RunDataFeed(algorithm);

            ConsumeBridge(feed, ts =>
            {
                Console.WriteLine(ts.Time);
                Assert.AreEqual(DateTime.UtcNow.RoundDown(Time.OneSecond), ts.Time);
                Assert.AreEqual(1, ts.Slice.Bars.Count);
            });
        }

        [Test]
        public void HandlesRemoteUpdatingFiles()
        {
            var resolution = Resolution.Second;
            var algorithm = new AlgorithmStub();
            algorithm.AddData<ForexRemoteFileBaseData>("EURUSD", resolution);

            var feed = RunDataFeed(algorithm);

            int count = 0;
            bool receivedData = false;
            var timeZone = algorithm.Securities["EURUSD"].Exchange.TimeZone;
            ConsumeBridge(feed, TimeSpan.FromSeconds(20), ts =>
            {
                // because this is a remote file we may skip data points while the newest
                // version of the file is downloading [internet speed] and also we decide
                // not to emit old data
                if (ts.Slice.ContainsKey("EURUSD"))
                {
                    count++;
                    receivedData = true;
                    var data = (ForexRemoteFileBaseData) ts.Slice["EURUSD"];
                    var time = data.EndTime.ConvertToUtc(timeZone);
                    Console.WriteLine("Data time: " + time.ConvertFromUtc(TimeZones.NewYork) + Environment.NewLine);
                    // make sure within 2 seconds
                    var delta = ts.Time.Subtract(time);
                    Assert.IsTrue(delta <= TimeSpan.FromSeconds(2), delta.ToString());
                }
            });
            // even though we're doing 20 seconds, give a little
            // leeway for slow internet traffic
            Assert.IsTrue(count > 18);
            Assert.IsTrue(receivedData);
        }

        [Test]
        public void HandlesRestApi()
        {
            var resolution = Resolution.Second;
            var algorithm = new AlgorithmStub();
            algorithm.AddData<ForexRestApiBaseData>("EURUSD", resolution);

            var feed = RunDataFeed(algorithm);

            int count = 0;
            bool receivedData = false;
            var timeZone = algorithm.Securities["EURUSD"].Exchange.TimeZone;
            ForexRestApiBaseData last = null;
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), ts =>
            {
                count++;
                receivedData = true;
                var data = (ForexRestApiBaseData)ts.Slice["EURUSD"];
                var time = data.EndTime.ConvertToUtc(timeZone);
                Console.WriteLine("Data time: " + time.ConvertFromUtc(TimeZones.NewYork) + Environment.NewLine);
                // make sure within 1 seconds
                var delta = ts.Time.Subtract(time);
                Assert.IsTrue(delta <= resolution.ToTimeSpan(), delta.ToString());

                if (last != null)
                {
                    Assert.AreEqual(last.EndTime, data.EndTime.Subtract(resolution.ToTimeSpan()));
                }
                last = data;
            });
            // even though we're doing 20 seconds, give a little
            // leeway for slow internet traffic
            Assert.IsTrue(count >= 9);
            Assert.IsTrue(receivedData);
        }

        [Test]
        public void HandlesCoarseFundamentalData()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetUniverse(coarse => coarse.Select(x => x.Symbol));

            var lck = new object();
            CoarseFundamentalList list = null;
            var timer = new Timer(state =>
            {
                lock (state)
                {
                    Console.WriteLine("timer.Elapsed");
                    list = new CoarseFundamentalList
                    {
                        Symbol = "qc-universe-coarse-usa",
                        Data =
                        {
                            new CoarseFundamental {Symbol = "SPY"},
                            new CoarseFundamental {Symbol = "IBM"},
                            new CoarseFundamental {Symbol = "MSFT"},
                            new CoarseFundamental {Symbol = "AAPL"},
                            new CoarseFundamental {Symbol = "GOOG"}
                        }
                    };
                }
            }, lck, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));

            var feed = RunDataFeed(algorithm, fdqh =>
            {
                lock (lck)
                {
                    if (list != null)
                        try { return new List<BaseData> {list}; }
                        finally { list = null; }
                }
                return Enumerable.Empty<BaseData>();
            });

            Assert.IsTrue(feed.Subscriptions.Any(x => x.IsUniverseSelectionSubscription));

            var firedUniverseSelection = false;
            feed.UniverseSelection += (sender, args) =>
            {
                firedUniverseSelection = true;
                Console.WriteLine("Fired universe selection");
            };


            ConsumeBridge(feed, TimeSpan.FromSeconds(2), ts =>
            {
            });

            Assert.IsTrue(firedUniverseSelection);
        }



        private LiveTradingDataFeed RunDataFeed(IAlgorithm algorithm, Func<FakeDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction = null)
        {
            FakeDataQueueHandler dataQueueHandler;
            return RunDataFeed(algorithm, out dataQueueHandler, getNextTicksFunction);
        }

        private LiveTradingDataFeed RunDataFeed(IAlgorithm algorithm, out FakeDataQueueHandler dataQueueHandler, Func<FakeDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction = null)
        {
            getNextTicksFunction = getNextTicksFunction ?? (fdqh => fdqh.Subscriptions.Select(x => new Tick(DateTime.Now, x.Symbol, 1, 2)));

            // job is used to send into DataQueueHandler
            var job = new LiveNodePacket();
            // result handler is used due to dependency in SubscriptionDataReader
            var resultHandler = new ConsoleResultHandler();// new ResultHandlerStub();

            dataQueueHandler = new FakeDataQueueHandler(getNextTicksFunction);

            var feed = new TestableLiveTradingDataFeed(dataQueueHandler);
            feed.Initialize(algorithm, job, resultHandler);

            var feedThreadStarted = new ManualResetEvent(false);
            Task.Factory.StartNew(() =>
            {
                feedThreadStarted.Set();
                feed.Run();
            });
            
            // wait for feed.Run to actually begin
            feedThreadStarted.WaitOne();

            return feed;
        }

        private static void ConsumeBridge(IDataFeed feed, Action<TimeSlice> handler)
        {
            ConsumeBridge(feed, TimeSpan.FromSeconds(10), handler);
        }

        private static void ConsumeBridge(IDataFeed feed, TimeSpan timeout, Action<TimeSlice> handler)
        {
            bool startedReceivingata = false;
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            foreach (var timeSlice in feed.Bridge.GetConsumingEnumerable(cancellationTokenSource.Token))
            {
                Console.WriteLine("TimeSlice.Time (EDT): " + timeSlice.Time.ConvertFromUtc(TimeZones.NewYork));
                if (!startedReceivingata && timeSlice.Slice.Count != 0)
                {
                    startedReceivingata = true;
                    Console.WriteLine("Recieved data");
                }
                if (startedReceivingata)
                {
                    handler(timeSlice);
                }
            }
        }

        #region Test classes

        public class ForexRemoteFileBaseData : BaseData
        {
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var csv = line.Split(',');
                if (csv[1].ToLower() != config.Symbol.ToLower())
                {
                    // this row isn't for me
                    return null;
                }

                var time = QuantConnect.Time.UnixTimeStampToDateTime(double.Parse(csv[0])).ConvertFromUtc(config.TimeZone).Subtract(config.Increment);
                return new ForexRemoteFileBaseData
                {
                    Symbol = config.Symbol,
                    Time = time,
                    EndTime = time.Add(config.Increment),
                    Value = decimal.Parse(csv[3])

                };
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                // this file is only a few seconds worth of data, so it's quick to download
                var remoteFileSource = @"http://www.quantconnect.com/live-test?type=file&symbols=" + config.Symbol;
                remoteFileSource = @"http://beta.quantconnect.com/live-test?type=file&symbols=" + config.Symbol;
                return new SubscriptionDataSource(remoteFileSource, SubscriptionTransportMedium.RemoteFile, FileFormat.Csv);
            }
        }

        public class ForexRestApiBaseData : TradeBar
        {
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                //[{"symbol":"SPY","time":1444271505,"alpha":1,"beta":2}]
                var array = JsonConvert.DeserializeObject<JsonSerialization[]>(line);
                if (array.Length > 0)
                {
                    return array[0].ToBaseData(config.TimeZone, config.Increment);
                }
                return null;
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                // this file is only a few seconds worth of data, so it's quick to download
                var remoteFileSource = @"http://www.quantconnect.com/live-test?type=rest&symbols=" + config.Symbol;
                remoteFileSource = @"http://beta.quantconnect.com/live-test?type=rest&symbols=" + config.Symbol;
                return new SubscriptionDataSource(remoteFileSource, SubscriptionTransportMedium.Rest, FileFormat.Csv);
            }

            class JsonSerialization
            {
                public string symbol;
                public double time;
                public double alpha;
                public double beta;

                public ForexRestApiBaseData ToBaseData(DateTimeZone timeZone, TimeSpan period)
                {
                    var dateTime = QuantConnect.Time.UnixTimeStampToDateTime(time).ConvertFromUtc(timeZone).Subtract(period);
                    return new ForexRestApiBaseData
                    {
                        Symbol = symbol,
                        Time = dateTime,
                        EndTime = dateTime.Add(period),
                        Value = (decimal) alpha
                    };
                }
            }
        }

        public class FakeDataQueueHandler : IDataQueueHandler
        {
            private readonly HashSet<Subscription> _subscriptions = new HashSet<Subscription>();
            private readonly Func<FakeDataQueueHandler, IEnumerable<BaseData>> _getNextTicksFunction;

            public List<Subscription> Subscriptions
            {
                get { return _subscriptions.ToList(); }
            }

            public FakeDataQueueHandler(Func<FakeDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction)
            {
                _getNextTicksFunction = getNextTicksFunction;
            }

            public IEnumerable<BaseData> GetNextTicks()
            {
                return _getNextTicksFunction(this);
            }

            public void Subscribe(LiveNodePacket job, IDictionary<SecurityType, List<string>> symbols)
            {
                foreach (var kvp in symbols)
                {
                    foreach (var item in kvp.Value)
                    {
                        _subscriptions.Add(new Subscription(item, kvp.Key));
                    }
                }
            }

            public void Unsubscribe(LiveNodePacket job, IDictionary<SecurityType, List<string>> symbols)
            {
                foreach (var kvp in symbols)
                {
                    foreach (var item in kvp.Value)
                    {
                        _subscriptions.Remove(new Subscription(item, kvp.Key));
                    }
                }
            }

            public class Subscription : IEquatable<Subscription>
            {
                public readonly string Symbol;
                public readonly SecurityType SecurityType;

                public Subscription(string symbol, SecurityType securityType)
                {
                    Symbol = symbol;
                    SecurityType = securityType;
                }

                #region Equality members

                public bool Equals(Subscription other)
                {
                    if (ReferenceEquals(null, other)) return false;
                    if (ReferenceEquals(this, other)) return true;
                    return SecurityType == other.SecurityType && string.Equals(Symbol, other.Symbol);
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj)) return false;
                    if (ReferenceEquals(this, obj)) return true;
                    if (obj.GetType() != this.GetType()) return false;
                    return Equals((Subscription) obj);
                }

                public override int GetHashCode()
                {
                    unchecked
                    {
                        return ((int) SecurityType*397) ^ (Symbol != null ? Symbol.GetHashCode() : 0);
                    }
                }

                public static bool operator ==(Subscription left, Subscription right)
                {
                    return Equals(left, right);
                }

                public static bool operator !=(Subscription left, Subscription right)
                {
                    return !Equals(left, right);
                }

                #endregion

            }
        }

        public class TestableLiveTradingDataFeed : LiveTradingDataFeed
        {
            private readonly IDataQueueHandler _dataQueueHandler;
            private readonly ISubscriptionEnumeratorProvider _enumeratorProvider;

            public TestableLiveTradingDataFeed(IDataQueueHandler dataQueueHandler, ISubscriptionEnumeratorProvider enumeratorProvider = null)
            {
                _dataQueueHandler = dataQueueHandler;
                _enumeratorProvider = enumeratorProvider;
            }

            protected override IDataQueueHandler GetDataQueueHandler()
            {
                return _dataQueueHandler;
            }

            protected override IEnumerator<BaseData> CreateSubscriptionEnumerator(IAlgorithm algorithm,
                IResultHandler resultHandler,
                Security security,
                DateTime periodStart,
                DateTime periodEnd)
            {
                if (_enumeratorProvider == null)
                {
                    return base.CreateSubscriptionEnumerator(algorithm, resultHandler, security, periodStart, periodEnd);
                }
                return _enumeratorProvider.CreateSubscriptionEnumerator(algorithm, resultHandler, security, periodStart, periodEnd);
            }
        }

        public class AlgorithmStub : QCAlgorithm
        {
            public AlgorithmStub(Resolution resolution = Resolution.Second, List<string> equities = null, List<string> forex = null)
            {
                foreach (var symbol in equities ?? new List<string>())
                {
                    AddSecurity(SecurityType.Equity, symbol, resolution);
                }
                foreach (var symbol in forex ?? new List<string>())
                {
                    AddSecurity(SecurityType.Forex, symbol, resolution);
                }
            }
        }

        public class ResultHandlerStub : IResultHandler
        {
            public ConcurrentQueue<Packet> Messages { get; set; }
            public ConcurrentDictionary<string, Chart> Charts { get; set; }
            public TimeSpan ResamplePeriod { get; private set; }
            public TimeSpan NotificationPeriod { get; private set; }
            public bool IsActive { get; private set; }

            public void Initialize(AlgorithmNodePacket job,
                IMessagingHandler messagingHandler,
                IApi api,
                IDataFeed dataFeed,
                ISetupHandler setupHandler,
                ITransactionHandler transactionHandler)
            {
            }

            public void Run()
            {
            }

            public void DebugMessage(string message)
            {
            }

            public void SecurityType(List<SecurityType> types)
            {
            }

            public void LogMessage(string message)
            {
            }

            public void ErrorMessage(string error, string stacktrace = "")
            {
            }

            public void RuntimeError(string message, string stacktrace = "")
            {
            }

            public void Sample(string chartName,
                ChartType chartType,
                string seriesName,
                SeriesType seriesType,
                DateTime time,
                decimal value,
                string unit = "$")
            {
            }

            public void SampleEquity(DateTime time, decimal value)
            {
            }

            public void SamplePerformance(DateTime time, decimal value)
            {
            }

            public void SampleBenchmark(DateTime time, decimal value)
            {
            }

            public void SampleAssetPrices(Symbol symbol, DateTime time, decimal value)
            {
            }

            public void SampleRange(List<Chart> samples)
            {
            }

            public void SetAlgorithm(IAlgorithm algorithm)
            {
            }

            public void StoreResult(Packet packet, bool async = false)
            {
            }

            public void SendFinalResult(AlgorithmNodePacket job,
                Dictionary<int, Order> orders,
                Dictionary<DateTime, decimal> profitLoss,
                Dictionary<string, Holding> holdings,
                StatisticsResults statisticsResults,
                Dictionary<string, string> banner)
            {
            }

            public void SendStatusUpdate(string algorithmId, AlgorithmStatus status, string message = "")
            {
            }

            public void SetChartSubscription(string symbol)
            {
            }

            public void RuntimeStatistic(string key, string value)
            {
            }

            public void OrderEvent(OrderEvent newEvent)
            {
            }

            public void Exit()
            {
            }

            public void PurgeQueue()
            {
            }

            public void ProcessSynchronousEvents(bool forceProcess = false)
            {
            }
        }

        public interface ISubscriptionEnumeratorProvider
        {
            /// <summary>
            /// Creates the security's subscription enumerator
            /// </summary>
            IEnumerator<BaseData> CreateSubscriptionEnumerator(IAlgorithm algorithm,
                IResultHandler resultHandler,
                Security security,
                DateTime periodStart,
                DateTime periodEnd);
        }

        public class EnumerableSubscriptionEnumeratorProvider : ISubscriptionEnumeratorProvider
        {
            private readonly IEnumerable<BaseData> _enumerable;

            public EnumerableSubscriptionEnumeratorProvider(IEnumerable<BaseData> enumerable)
            {
                _enumerable = enumerable;
            }

            public IEnumerator<BaseData> CreateSubscriptionEnumerator(IAlgorithm algorithm,
                IResultHandler resultHandler,
                Security security,
                DateTime periodStart,
                DateTime periodEnd)
            {
                return _enumerable.GetEnumerator();
            }
        }

        #endregion
    }
}
