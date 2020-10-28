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
using System.Net;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.ToolBox.IEX;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    [Explicit("Tests are dependent on network and are long")]
    public class IEXDataQueueHandlerTests
    {
        private readonly string _apiKey = Config.Get("iex-cloud-api-key");

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
                    Console.WriteLine(err.Message);
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
                            Console.WriteLine(tick.ToString());
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

                Console.WriteLine(tick.ToString());
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

            iex.Unsubscribe(configs.First(c => string.Equals(c.Symbol.Value, "MBLY")));

            Console.WriteLine("Unsubscribing");
            Thread.Sleep(2000);
            // some messages could be inflight, but after a pause all MBLY messages must have beed consumed by ProcessFeed
            unsubscribed = true;

            Thread.Sleep(20000);
            iex.Dispose();
        }

        [Test]
        public void IEXCouldSubscribeMoreThan100Symbols()
        {
            var symbols = GetSnpStocksArray().Select(i => i.Symbol).ToArray();

            var configs = symbols.Select(s =>
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
        public void IEXEventSourceCollectionSubscribes()
        {
            var snpShares = GetSnpStocksArray();

            // Send few consecutive requests to subscribe to a large amount of symbols (after the first request to change the subscription)
            // and make sure no exception will be thrown - if event-source-collection can't subscribe to all it throws after timeout 
            Assert.DoesNotThrow(() =>
            {
                using (var events = new IEXEventSourceCollection((o, args) => { }, _apiKey))
                {
                    var rnd = new Random();
                    for (var i = 0; i < 5; i++)
                    {
                        // Shuffle and select first random amount of symbol in range from 300 to 500 (snp symbols count)
                        var shuffled = snpShares.OrderBy(n => Guid.NewGuid()).ToArray();
                        var selected = shuffled
                            .Take(rnd.Next(300, shuffled.Length))
                            .Select(data => data.Symbol)
                            .ToArray();

                        events.UpdateSubscription(selected);
                    }
                }
            });
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

        /// <summary>
        /// Retrieves list of companies that constitute SNP index from slick-charts-dot-com
        /// </summary>
        /// <returns></returns>
        public static SnpComponentData[] GetSnpStocksArray()
        {
            var web = new HtmlWeb();
            var doc = web.Load(@"https://www.slickcharts.com/sp500");

            // This query does the following:
            // fetches the html data that represents a table for S&P components
            // then parses all the rows, handles it as cell string array and creates ComponentData.
            // see: https://stackoverflow.com/questions/13005098/parsing-html-table-in-c-sharp

            var table = doc.DocumentNode
                .SelectSingleNode("//table[@class='table table-hover table-borderless table-sm']")
                .Descendants("tr")
                .Skip(1)
                // Here we take cells extract inner text as string and apply HtmlDecode to it
                // as there may be companies that have ampersand sign in naming ( 'S&P Global Inc.')
                // which is displayed differently in html :
                // https://stackoverflow.com/questions/122641/how-can-i-decode-html-characters-in-c
                .Select(row => row.Elements("td").Select(htmlNode => HttpUtility.HtmlDecode(htmlNode.InnerText)).ToArray())
                .Select(cells => new SnpComponentData
                {
                    Position = int.Parse(cells[0], CultureInfo.InvariantCulture),
                    Name = cells[1],
                    Symbol = cells[2],
                    Weight = decimal.Parse(cells[3], CultureInfo.InvariantCulture)
                })
                .ToArray();

            return table;
        }

        /// <summary>
        /// Class representing the stock data that can be obtained from SlickCharts
        /// </summary>
        public class SnpComponentData
        {
            public int Position { get; set; }
            public string Name { get; set; }
            public string Symbol { get; set; }
            public decimal Weight { get; set; }
        }


        #region History provider tests

        public static TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    // valid parameters
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(15), true, false),
                    new TestCaseData(Symbols.SPY, Resolution.Minute, TimeSpan.FromDays(3), true, false),

                    // invalid resolution == empty result.
                    new TestCaseData(Symbols.SPY, Resolution.Tick, TimeSpan.FromSeconds(15), false, false),
                    new TestCaseData(Symbols.SPY, Resolution.Second, Time.OneMinute, false, false),
                    new TestCaseData(Symbols.SPY, Resolution.Hour, Time.OneDay, false, false),

                    // invalid period == empty result
                    new TestCaseData(Symbols.SPY, Resolution.Minute, TimeSpan.FromDays(45), false, false), // beyond 30 days
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(-15), false, false), // date in future
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(365*5.5), false, false), // beyond 5 years

                    // invalid symbol: XYZ -> not found WebException
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Equity, Market.FXCM), Resolution.Daily, TimeSpan.FromDays(15), false, true),

                    // invalid security type, no exception, empty result
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), false, false)
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void IEXCouldGetHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool received, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var historyProvider = new IEXDataQueueHandler();
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, new DataPermissionManager()));

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
                                       now,
                                       typeof(QuoteBar),
                                       symbol,
                                       resolution,
                                       SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                                       DateTimeZone.Utc,
                                       Resolution.Minute,
                                       false,
                                       false,
                                       DataNormalizationMode.Adjusted,
                                       TickType.Quote)
                };

                var history = historyProvider.GetHistory(requests, TimeZones.Utc);

                foreach (var slice in history)
                {
                    if (resolution == Resolution.Tick || resolution == Resolution.Second || resolution == Resolution.Hour)
                    {
                        Log.Trace($"IEXCouldGetHistory(): Invalid resolution {resolution}");

                        Assert.IsNull(slice);
                    }
                    else if (resolution == Resolution.Daily || resolution == Resolution.Minute)
                    {
                        Assert.IsNotNull(slice);

                        var bar = slice.Bars[symbol];

                        Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}: {6}, {7}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close, resolution, period);
                    }
                }

                Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
                if (received)
                {
                    Assert.IsTrue(historyProvider.DataPointCount > 0);
                }
                else
                {
                    Assert.IsTrue(historyProvider.DataPointCount == 0);
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