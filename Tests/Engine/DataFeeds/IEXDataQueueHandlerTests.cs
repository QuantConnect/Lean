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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Math;
using LaunchDarkly.EventSource;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.ToolBox.IEX;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    [Explicit("Tests are dependent on network and are long")]
    public class IEXDataQueueHandlerTests
    {
        private readonly string _apiKey = Config.Get("iex-cloud-api-key");

        private static readonly string[] HardCodedSymbolsSNP = {
            "AAPL", "MSFT", "AMZN", "FB", "GOOGL", "GOOG", "BRK.B", "JNJ", "PG", "NVDA", "V", "JPM", "HD", "UNH", "MA", "VZ", 
            "PYPL", "NFLX", "ADBE", "DIS", "CRM", "CMCSA", "PFE", "WMT", "MRK", "T", "INTC", "TMO", "ABT", "KO", "PEP", "BAC", 
            "COST", "MCD", "NKE", "CSCO", "DHR", "NEE", "QCOM", "ABBV", "AVGO", "XOM", "ACN", "MDT", "TXN", "CVX", "BMY", 
            "AMGN", "LOW", "UNP", "HON", "LIN", "UPS", "PM", "ORCL", "LLY", "SBUX", "AMT", "NOW", "IBM", "AMD", "MMM", "WFC", 
            "C", "LMT", "CHTR", "BLK", "INTU", "CAT", "RTX", "ISRG", "BA", "SPGI", "FIS", "TGT", "ZTS", "MDLZ", "PLD", "GILD", 
            "CVS", "DE", "ANTM", "MS", "MO", "ADP", "D", "DUK", "BDX", "SYK", "BKNG", "CCI", "EQIX", "CL", "GS", "FDX", "GE", 
            "TMUS", "TJX", "SO", "APD", "ATVI", "CB", "CI", "SCHW", "CSX", "AXP", "REGN", "SHW", "ITW", "TFC", "MU", "AMAT", 
            "VRTX", "ICE", "CME", "ADSK", "FISV", "PGR", "DG", "NSC", "HUM", "USB", "MMC", "LRCX", "EL", "NEM", "BSX", "GPN", 
            "PNC", "ECL", "ILMN", "EW", "NOC", "KMB", "AEP", "GM", "ADI", "AON", "DD", "MCO", "WM", "ETN", "TWTR", "DLR", 
            "BAX", "EXC", "BIIB", "CTSH", "EMR", "ROP", "IDXX", "XEL", "SRE", "EA", "GIS", "LHX", "PSA", "CMG", "DOW", "CNC", 
            "APH", "COF", "SNPS", "HCA", "SBAC", "EBAY", "ORLY", "CMI", "DXCM", "TEL", "TT", "JCI", "A", "WEC", "KLAC", "COP", 
            "ALGN", "TRV", "ROST", "CDNS", "F", "GD", "PPG", "INFO", "XLNX", "PEG", "ES", "TROW", "IQV", "PCAR", "BLL", "VRSK", 
            "MSCI", "MET", "YUM", "MNST", "SYY", "CTAS", "BK", "ADM", "CARR", "STZ", "ALL", "MSI", "ZBH", "AWK", "ROK", "AIG", 
            "MCHP", "PH", "APTV", "ANSS", "ED", "AZO", "SWK", "PAYX", "CLX", "ALXN", "TDG", "BBY", "RMD", "FCX", "KR", "PRU", 
            "MAR", "OTIS", "FAST", "GLW", "HPQ", "SWKS", "CTVA", "WBA", "MTD", "HLT", "WLTW", "DTE", "LUV", "KMI", "MCK", "WMB", 
            "WELL", "CPRT", "AFL", "DHI", "MKC", "AME", "VFC", "DLTR", "CERN", "EIX", "CHD", "PPL", "FRC", "WY", "FTV", "MKTX", 
            "STT", "HSY", "WST", "ETR", "PSX", "O", "SLB", "AEE", "EOG", "LEN", "DAL", "DFS", "AJG", "KEYS", "KHC", "SPG", 
            "AMP", "VRSN", "LH", "AVB", "VMC", "MXIM", "MPC", "LYB", "FLT", "RSG", "PAYC", "HOLX", "TTWO", "ODFL", "CMS", 
            "ARE", "CDW", "IP", "FE", "CAG", "CBRE", "TSN", "EFX", "AMCR", "KSU", "FITB", "MLM", "DGX", "NTRS", "INCY", "DOV", 
            "FTNT", "VIAC", "COO", "EQR", "ETSY", "BR", "GWW", "LVS", "K", "VAR", "ZBRA", "XYL", "TYL", "AKAM", "TSCO", "VLO", 
            "EXR", "STE", "TFX", "EXPD", "DPZ", "PEAK", "QRVO", "VTR", "TER", "CTLT", "SIVB", "GRMN", "KMX", "NUE", "POOL", 
            "PKI", "MAS", "CTXS", "WAT", "TIF", "NDAQ", "NVR", "HIG", "DRE", "ABC", "SYF", "HRL", "OKE", "PXD", "CE", "FMC", 
            "CAH", "LNT", "GPC", "IR", "EXPE", "ESS", "AES", "MAA", "MTB", "RF", "BF.B", "EVRG", "SJM", "IEX", "KEY", "URI", 
            "J", "NLOK", "BIO", "CHRW", "DRI", "CNP", "WHR", "AVY", "ULTA", "ABMD", "CFG", "TDY", "JKHY", "FBHS", "EMN", "WDC", 
            "PHM", "HPE", "ANET", "ATO", "IFF", "LDOS", "PKG", "CINF", "HAS", "STX", "WAB", "IT", "HBAN", "HAL", "JBHT", 
            "HES", "BXP", "AAP", "PFG", "ALB", "OMC", "NTAP", "XRAY", "UAL", "RCL", "WRK", "CPB", "LW", "RJF", "PNW", "BKR", 
            "ALLE", "HSIC", "LKQ", "FOXA", "UDR", "MGM", "BWA", "PWR", "CBOE", "WU", "WRB", "SNA", "NI", "CXO", "PNR", 
            "ROL", "LUMN", "UHS", "L", "RE", "FFIV", "TXT", "GL", "NRG", "OXY", "AIZ", "IRM", "HST", "MYL", "LB", "COG", 
            "AOS", "IPG", "LYV", "WYNN", "CCL", "HWM", "IPGP", "JNPR", "DVA", "NWL", "MOS", "TPR", "DISH", "SEE", "TAP", 
            "LNC", "CMA", "HII", "PRGO", "HBI", "CF", "RHI", "REG", "MHK", "AAL", "DISCK", "LEG", "ZION", "IVZ", "BEN", "NWSA", 
            "NLSN", "FRT", "VNO", "DXC", "FLIR", "AIV", "PBCT", "ALK", "PVH", "KIM", "FANG", "FOX", "NCLH", "GPS", "VNT", "FLS", 
            "UNM", "RL", "XRX", "DVN", "MRO", "DISCA", "NOV", "APA", "SLG", "HFC", "UAA", "UA", "FTI", "NWS"
        };

        [SetUp]
        public void Setup()
        {
            Log.DebuggingEnabled = Config.GetBool("debug-mode");
        }

        private void ProcessFeed(IEnumerator<BaseData> enumerator, Action<BaseData> callback = null)
        {
            Task.Run(() =>
            {
                try
                {
                    while (enumerator.MoveNext())
                    {
                        BaseData tick = enumerator.Current;
                        callback?.Invoke(tick);
                    }
                }
                catch (AssertionException)
                {
                    throw;
                }
                catch (Exception err)
                {
                    Log.Error(err.Message);
                }
            });
        }

        /// <summary>
        /// Subscribe to multiple symbols in a series of calls
        /// </summary>
        [Test]
        public void IEXCouldSubscribeManyTimes()
        {
            var iex = new IEXDataQueueHandler();

            var configs = new[] {
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("GOOG", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("FB", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("AAPL", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("MSFT", SecurityType.Equity, Market.USA), Resolution.Second)
            };

            Array.ForEach(configs, (c) =>
            {
                ProcessFeed(
                    iex.Subscribe(c, (s, e) => { }),
                    tick =>
                    {
                        if (tick != null)
                        {
                            Log.Trace(tick.ToString());
                        }
                    });
            });

            Thread.Sleep(20000);

            Log.Trace("Unsubscribing from all except AAPL");

            Array.ForEach(configs, (c) =>
            {
                if (!string.Equals(c.Symbol.Value, "AAPL"))
                {
                    iex.Unsubscribe(c);
                }
            });

            Thread.Sleep(20000);

            iex.Dispose();
        }

        [Test]
        public void IEXCouldSubscribeAndUnsubscribe()
        {
            // MBLY is the most liquid IEX instrument
            var iex = new IEXDataQueueHandler();
            var unsubscribed = false;
            Action<BaseData> callback = (tick) =>
            {
                if (tick == null)
                    return;

                Log.Trace(tick.ToString());
                if (unsubscribed && tick.Symbol.Value == "MBLY")
                {
                    Assert.Fail("Should not receive data for unsubscribed symbol");
                }
            };

            var configs = new[] {
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("MBLY", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("USO", SecurityType.Equity, Market.USA), Resolution.Second)
            };

            Array.ForEach(configs, (c) =>
            {
                ProcessFeed(
                    iex.Subscribe(c, (s, e) => { }),
                    callback);
            });

            Thread.Sleep(20000);

            iex.Unsubscribe(Enumerable.First(configs, c => string.Equals(c.Symbol.Value, "MBLY")));

            Log.Trace("Unsubscribing");
            Thread.Sleep(2000);
            // some messages could be inflight, but after a pause all MBLY messages must have beed consumed by ProcessFeed
            unsubscribed = true;

            Thread.Sleep(20000);
            iex.Dispose();
        }

        [Test]
        public void IEXCouldSubscribeMoreThan100Symbols()
        {
            var configs = HardCodedSymbolsSNP.Select(s =>
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create(s, SecurityType.Equity, Market.USA),
                    Resolution.Second)).ToArray();
            
            using (var iex = new IEXDataQueueHandler())
            {
                Array.ForEach(configs, dataConfig => iex.Subscribe(dataConfig, (s, e) => {  }));
                Thread.Sleep(20000);
                Assert.IsTrue(iex.IsConnected);
            }
        }

        [Test]
        public void IEXEventSourceCollectionCanSubscribeManyTimes()
        {
            // Send few consecutive requests to subscribe to a large amount of symbols (after the first request to change the subscription)
            // and make sure no exception will be thrown - if event-source-collection can't subscribe to all it throws after timeout 
            Assert.DoesNotThrow(() =>
            {
                using (var events = new IEXEventSourceCollection((o, args) => { }, _apiKey))
                {
                    var rnd = new Random();
                    for (var i = 0; i < 5; i++)
                    {
                        // Shuffle and select first random amount of symbol
                        // in range from 300 to 500 (snp symbols count)
                        var shuffled = HardCodedSymbolsSNP
                            .OrderBy(n => Guid.NewGuid())
                            .ToArray();

                        var selected = shuffled
                            .Take(rnd.Next(300, shuffled.Length))
                            .ToArray();

                        events.UpdateSubscription(selected);
                    }
                }
            });
        }

        [Test]
        public void IEXEventSourceCollectionSubscriptionThoroughTest()
        {
            using (var mockedEventsSource = new MockedIEXEventSourceCollection((o, args) => { }, _apiKey))
            {
                // -- 1 -- SUBSCRIBE FOR THE FIRST TIME --
                var amount1 = 210;
                var symbols1 = HardCodedSymbolsSNP.Take(amount1).ToArray();

                mockedEventsSource.UpdateSubscription(symbols1);

                var subscribed = mockedEventsSource.GetAllSubscribedSymbols().ToArray();
                Assert.AreEqual(amount1, subscribed.Count());
                Assert.IsTrue(subscribed.All(i => symbols1.Contains(i)));

                Assert.AreEqual(5, mockedEventsSource.CreateNewSubscriptionCalledTimes);
                Assert.AreEqual(0, mockedEventsSource.RemoveOldClientCalledTimes);

                // Signal can be obtained
                mockedEventsSource.TestCounter.Signal();
                Assert.AreEqual(0, mockedEventsSource.TestCounter.CurrentCount);
                mockedEventsSource.TestCounter.Reset(1);

                // -- 2 -- SUBSCRIBE FOR THE SECOND TIME -- UPDATES SUBSCRIPTION --
                mockedEventsSource.Reset();

                // Remove 3 elements with a step 50
                var symbols2 = symbols1.RemoveAt(51).RemoveAt(101).RemoveAt(151);
                mockedEventsSource.UpdateSubscription(symbols2);

                // We removed three symbols, so three subscription became invalid
                Assert.AreEqual(3, mockedEventsSource.RemoveOldClientCalledTimes);
                // Two of old subscriptions were good, 3 new ones to be initiated to replace removed 
                Assert.AreEqual(3, mockedEventsSource.CreateNewSubscriptionCalledTimes);

                subscribed = mockedEventsSource.GetAllSubscribedSymbols().ToArray();
                Assert.AreEqual(symbols2.Length, subscribed.Length);
                Assert.IsTrue(subscribed.All(i => symbols2.Contains(i)));

                mockedEventsSource.TestCounter.Signal();
                Assert.AreEqual(0, mockedEventsSource.TestCounter.CurrentCount);
                mockedEventsSource.TestCounter.Reset(1);

                // Removed subscriptions had a symbol that was not in current subscription
                mockedEventsSource.RemovedClientSymbols.DoForEach(removed =>
                {
                    Assert.IsFalse(removed.All(s => symbols2.Contains(s)));
                });
            }
        }

        private SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);
        }

        public class MockedIEXEventSourceCollection : IEXEventSourceCollection
        {
            public CountdownEvent TestCounter => Counter;
            public int CreateNewSubscriptionCalledTimes;
            public int RemoveOldClientCalledTimes;
            public List<string[]> RemovedClientSymbols = new List<string[]>();

            public MockedIEXEventSourceCollection(EventHandler<MessageReceivedEventArgs> messageAction, string apiKey)
                : base(messageAction, apiKey)
            {
            }

            protected override EventSource CreateNewSubscription(string[] symbols)
            {
                CreateNewSubscriptionCalledTimes++;
                // Decrement the counter
                Counter.Signal();
                return CreateNewClient(symbols);
            }

            protected override void RemoveOldClient(EventSource oldClient)
            {
                RemoveOldClientCalledTimes++;
                RemovedClientSymbols.Add(ClientSymbolsDictionary[oldClient]);
                // Call base class to remove from inner dictionary and dispose
                base.RemoveOldClient(oldClient);
            }

            public IEnumerable<string> GetAllSubscribedSymbols()
            {
                return ClientSymbolsDictionary.Values.SelectMany(x => x);
            }

            public void Reset()
            {
                CreateNewSubscriptionCalledTimes = 0;
                RemoveOldClientCalledTimes = 0;
                RemovedClientSymbols.Clear();
            }
        }


        #region History provider tests

        public static TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    // valid parameters
                    new TestCaseData(Symbols.SPY, Resolution.Daily, typeof(TradeBar), TimeSpan.FromDays(15), true, false),
                    new TestCaseData(Symbols.SPY, Resolution.Minute, typeof(TradeBar), TimeSpan.FromDays(5), true, false),

                    // invalid resolution == empty result.
                    new TestCaseData(Symbols.SPY, Resolution.Tick, typeof(TradeBar), TimeSpan.FromSeconds(15), false, false),
                    new TestCaseData(Symbols.SPY, Resolution.Second, typeof(TradeBar), Time.OneMinute, false, false),
                    new TestCaseData(Symbols.SPY, Resolution.Hour, typeof(TradeBar), Time.OneDay, false, false),

                    // invalid period == empty result
                    new TestCaseData(Symbols.SPY, Resolution.Minute, typeof(TradeBar), TimeSpan.FromDays(45), false, false), // beyond 30 days
                    new TestCaseData(Symbols.SPY, Resolution.Daily, typeof(TradeBar), TimeSpan.FromDays(-15), false, false), // date in future

                    // invalid data type = empty result
                    new TestCaseData(Symbols.SPY, Resolution.Daily, typeof(QuoteBar), TimeSpan.FromDays(15), false, false),

                    // invalid symbol: XYZ -> not found Exception
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Equity, Market.FXCM), Resolution.Daily, typeof(TradeBar), TimeSpan.FromDays(15), false, true),

                    // invalid security type, no exception, empty result
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, typeof(TradeBar), TimeSpan.FromDays(15), false, false)
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void IEXCouldGetHistory(Symbol symbol, Resolution resolution, Type dataType, TimeSpan period, bool received, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var historyProvider = new IEXDataQueueHandler();
                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
                                       now,
                                       dataType,
                                       symbol,
                                       resolution,
                                       SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                                       TimeZones.NewYork,
                                       resolution,
                                       true,
                                       false,
                                       DataNormalizationMode.Raw,
                                       TickType.Trade)
                };

                var slices = historyProvider.GetHistory(requests, TimeZones.Utc).ToArray();
                Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);

                if (received)
                {
                    // Slices not empty
                    Assert.IsNotEmpty(slices);

                    // And are ordered by time
                    Assert.That(slices, Is.Ordered.By("Time"));
                }
                else
                {
                    Assert.IsEmpty(slices);
                }
            };

            if (throwsException)
            {
                Assert.That(test, Throws.Exception);
            }
            else
            {
                Assert.DoesNotThrow(test);
            }
        }

        #endregion

    }
}