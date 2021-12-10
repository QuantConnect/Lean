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
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IBApi;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Securities;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    [Explicit("These tests require the IBGateway to be installed.")]
    public class InteractiveBrokersBrokerageAdditionalTests
    {
        private readonly List<Order> _orders = new List<Order>();

        [SetUp]
        public void Setup()
        {
            Log.LogHandler = new NUnitLogHandler();
        }

        [Test(Description = "Requires an existing IB connection with the same user credentials.")]
        public void ThrowsWhenExistingSessionDetected()
        {
            Assert.Throws<Exception>(() => GetBrokerage());
        }

        [Test]
        public void TestRateLimiting()
        {
            using (var brokerage = GetBrokerage())
            {
                Assert.IsTrue(brokerage.IsConnected);

                var method = brokerage.GetType().GetMethod("GetContractDetails", BindingFlags.NonPublic | BindingFlags.Instance);

                var contract = new Contract
                {
                    Symbol = "EUR",
                    Exchange = "IDEALPRO",
                    SecType = "CASH",
                    Currency = Currencies.USD
                };
                var parameters = new object[] { contract };

                var result = Parallel.For(1, 100, x =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    var value = (ContractDetails)method.Invoke(brokerage, parameters);
                    stopwatch.Stop();
                    Log.Trace($"{DateTime.UtcNow:O} Response time: {stopwatch.Elapsed}");
                });
                while (!result.IsCompleted) Thread.Sleep(1000);
            }
        }

        [Test]
        public void GetsHistoryWithMultipleApiCalls()
        {
            using (var brokerage = GetBrokerage())
            {
                Assert.IsTrue(brokerage.IsConnected);

                // request a week of historical data (including a weekend)
                var request = new HistoryRequest(
                    new DateTime(2018, 2, 1, 9, 30, 0).ConvertToUtc(TimeZones.NewYork),
                    new DateTime(2018, 2, 7, 16, 0, 0).ConvertToUtc(TimeZones.NewYork),
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    TimeZones.NewYork,
                    null,
                    false,
                    false,
                    DataNormalizationMode.Raw,
                    TickType.Trade);

                var history = brokerage.GetHistory(request).ToList();

                // check if data points are in chronological order
                var previousEndTime = DateTime.MinValue;
                foreach (var bar in history)
                {
                    Assert.IsTrue(bar.EndTime > previousEndTime);

                    previousEndTime = bar.EndTime;
                }

                // should return 5 days of data (Thu-Fri-Mon-Tue-Wed)
                // each day has 390 minute bars for equities
                Assert.AreEqual(5 * 390, history.Count);
            }
        }

        [Test]
        public void GetHistoryDoesNotThrowError504WhenDisconnected()
        {
            using (var brokerage = GetBrokerage())
            {
                Assert.IsTrue(brokerage.IsConnected);

                brokerage.Disconnect();
                Assert.IsFalse(brokerage.IsConnected);

                var hasError = false;
                brokerage.Message += (s, e) =>
                {
                    // ErrorCode: 504 - Not connected
                    if (e.Code == "504")
                    {
                        hasError = true;
                    }
                };

                var request = new HistoryRequest(
                    new DateTime(2021, 1, 1).ConvertToUtc(TimeZones.NewYork),
                    new DateTime(2021, 1, 27).ConvertToUtc(TimeZones.NewYork),
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Daily,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    TimeZones.NewYork,
                    null,
                    false,
                    false,
                    DataNormalizationMode.Raw,
                    TickType.Trade);

                var history = brokerage.GetHistory(request).ToList();

                Assert.AreEqual(0, history.Count);

                Assert.IsFalse(hasError);
            }
        }

        [Test, TestCaseSource(nameof(GetHistoryData))]
        public void GetHistory(
            Symbol symbol,
            Resolution resolution,
            DateTimeZone exchangeTimeZone,
            DateTimeZone dataTimeZone,
            DateTime endTimeInExchangeTimeZone,
            TimeSpan historyTimeSpan,
            bool includeExtendedMarketHours,
            int expectedCount)
        {
            using var brokerage = GetBrokerage();
            Assert.IsTrue(brokerage.IsConnected);

            var request = new HistoryRequest(
                endTimeInExchangeTimeZone.ConvertToUtc(exchangeTimeZone).Subtract(historyTimeSpan),
                endTimeInExchangeTimeZone.ConvertToUtc(exchangeTimeZone),
                typeof(TradeBar),
                symbol,
                resolution,
                SecurityExchangeHours.AlwaysOpen(exchangeTimeZone),
                dataTimeZone,
                null,
                includeExtendedMarketHours,
                false,
                DataNormalizationMode.Raw,
                TickType.Trade);

            var history = brokerage.GetHistory(request).ToList();

            // check if data points are in chronological order
            var previousEndTime = DateTime.MinValue;
            foreach (var bar in history)
            {
                Assert.IsTrue(bar.EndTime > previousEndTime);

                previousEndTime = bar.EndTime;
            }

            Log.Trace($"History count: {history.Count}");

            Assert.AreEqual(expectedCount, history.Count);
        }

        private static TestCaseData[] GetHistoryData
        {
            get
            {
                var futureSymbolUsingCents = Symbols.CreateFutureSymbol("LE", new DateTime(2021, 12, 31));
                var futureOptionSymbolUsingCents = Symbols.CreateFutureOptionSymbol(futureSymbolUsingCents, OptionRight.Call, 1.23m, new DateTime(2021, 12, 3));

                var futureSymbol = Symbol.CreateFuture("NQ", Market.CME, new DateTime(2021, 9, 17));
                var optionSymbol = Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Call, 145, new DateTime(2021, 8, 20));

                return new[]
                {
                    // 30 min RTH today + 60 min RTH yesterday
                    new TestCaseData(Symbols.SPY, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork,
                        new DateTime(2021, 8, 6, 10, 0, 0), TimeSpan.FromHours(19), false, 5400),

                    // 30 min RTH + 30 min ETH
                    new TestCaseData(Symbols.SPY, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork,
                        new DateTime(2021, 8, 6, 10, 0, 0), TimeSpan.FromHours(1), true, 3600),

                    // daily
                    new TestCaseData(futureSymbolUsingCents, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork,
                        new DateTime(2021, 9, 20, 0, 0, 0), TimeSpan.FromDays(10), true, 6),
                    // hourly
                    new TestCaseData(futureOptionSymbolUsingCents, Resolution.Hour, TimeZones.NewYork, TimeZones.NewYork,
                        new DateTime(2021, 9, 20, 0, 0, 0), TimeSpan.FromDays(10), true, 11),

                    // 60 min
                    new TestCaseData(futureSymbol, Resolution.Second, TimeZones.NewYork, TimeZones.Utc,
                        new DateTime(2021, 8, 6, 10, 0, 0), TimeSpan.FromHours(1), false, 3600),

                    // 60 min - RTH flag ignored, no ETH market hours
                    new TestCaseData(futureSymbol, Resolution.Second, TimeZones.NewYork, TimeZones.Utc,
                        new DateTime(2021, 8, 6, 10, 0, 0), TimeSpan.FromHours(1), true, 3600),

                    // 30 min today + 60 min yesterday
                    new TestCaseData(optionSymbol, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork,
                        new DateTime(2021, 8, 6, 10, 0, 0), TimeSpan.FromHours(19), false, 5400),

                    // 30 min today + 60 min yesterday - RTH flag ignored, no ETH market hours
                    new TestCaseData(optionSymbol, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork,
                        new DateTime(2021, 8, 6, 10, 0, 0), TimeSpan.FromHours(19), true, 5400)
                };
            }
        }

        private InteractiveBrokersBrokerage GetBrokerage()
        {
            // grabs account info from configuration
            var securityProvider = new SecurityProvider();
            securityProvider[Symbols.USDJPY] = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.USDJPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var brokerage = new InteractiveBrokersBrokerage(
                new QCAlgorithm(),
                new OrderProvider(_orders),
                securityProvider,
                new AggregationManager(),
                TestGlobals.MapFileProvider);
            brokerage.Connect();

            return brokerage;
        }
    }
}
