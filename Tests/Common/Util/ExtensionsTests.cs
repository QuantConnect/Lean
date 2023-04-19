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
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        public void NonExistingEmptyDirectory()
        {
            var nonexistingDirectory = $"NonExistingEmptyDirectory-{new Guid()}";
            Assert.IsTrue(nonexistingDirectory.IsDirectoryEmpty());
        }

        [Test]
        public void EmptyDirectory()
        {
            var directory = $"EmptyDirectory-{new Guid()}";
            Directory.CreateDirectory(directory);
            Assert.IsTrue(directory.IsDirectoryEmpty());

            Directory.Delete(directory, true);
        }

        [Test]
        public void DirectoryWithFile()
        {
            var directory = $"DirectoryWithFile-{new Guid()}";
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "test"), "test");

            Assert.IsFalse(directory.IsDirectoryEmpty());

            Directory.Delete(directory, true);
        }

        [Test]
        public void DirectoryWithDirectory()
        {
            var directory = $"DirectoryWithDirectory-{new Guid()}";
            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(Path.Combine(directory, "test"));

            Assert.IsFalse(directory.IsDirectoryEmpty());

            Directory.Delete(directory, true);
        }

        [Test]
        public void EmptyDirectoryCached()
        {
            var directory = $"EmptyDirectoryCached-{new Guid()}";
            Directory.CreateDirectory(directory);

            Assert.IsTrue(directory.IsDirectoryEmpty());

            File.WriteAllText(Path.Combine(directory, "test"), "test");

            Assert.IsTrue(directory.IsDirectoryEmpty());

            Directory.Delete(directory, true);
        }

        [Test]
        public void ToMD5()
        {
            var result = "pinochopinochopino   ".ToMD5();
            Assert.AreEqual("261db8a511d4c433fe58f8b9870fc88e", result);
        }

        [Test]
        public void ToSha256()
        {
            var result = "pinochopinochopino   ".ToSHA256();
            Assert.AreEqual("327a5a3b33aef00daf26e414542e12bf4205adb716475fa22e53a178e5d8baca", result);
        }

        [TestCase("1000", 0)]
        [TestCase("0", 0)]
        [TestCase("1", 0)]
        [TestCase("1.0", 1)]
        [TestCase("0.01", 2)]
        [TestCase("0.001", 3)]
        [TestCase("0.0001", 4)]
        [TestCase("0.00001", 5)]
        [TestCase("0.000001", 6)]
        public void GetDecimalPlaces(string decimalInput, int expectedResult)
        {
            var value = decimal.Parse(decimalInput, NumberStyles.Any, CultureInfo.InvariantCulture);
            Assert.AreEqual(expectedResult, value.GetDecimalPlaces());
        }

        [TestCase(0, 10, 110)]
        [TestCase(900, 10, 110)]
        [TestCase(500, 10, 10)]

        [TestCase(0, 100, 100)]
        [TestCase(100, 100, 100)]
        [TestCase(500, 100, 100)]
        [TestCase(990, 100, 200)]
        [TestCase(900, 100, 200)]

        [TestCase(0, 1000, 1500)]
        [TestCase(100, 1000, 1000)]
        [TestCase(500, 1000, 1000)]
        [TestCase(990, 1000, 1500)]

        [TestCase(0, 10000, 10500)]
        [TestCase(100, 10000, 10000)]
        [TestCase(500, 10000, 10000)]
        [TestCase(990, 10000, 10500)]
        public void UnevenSecondWaitTime(int nowMilliseconds, int waitInterval, int expectedWaitInterval)
        {
            var nowUtc = new DateTime(2022, 04, 1);
            nowUtc = nowUtc.AddMilliseconds(nowMilliseconds);

            Assert.AreEqual(expectedWaitInterval, nowUtc.GetSecondUnevenWait(waitInterval));
        }

        [TestCase(SecurityType.Cfd, "20501231", false)]
        [TestCase(SecurityType.Equity, "20501231", false)]
        [TestCase(SecurityType.Base, "20501231", false)]
        [TestCase(SecurityType.Forex, "20501231", false)]
        [TestCase(SecurityType.Crypto, "20501231", false)]
        [TestCase(SecurityType.Index, "20501231", false)]

        [TestCase(SecurityType.Option, null, false)]
        [TestCase(SecurityType.Future, null, false)]
        [TestCase(SecurityType.FutureOption, null, false)]
        [TestCase(SecurityType.IndexOption, null, false)]

        [TestCase(SecurityType.Option, "20501231", true)]
        [TestCase(SecurityType.Future, "20501231", true)]
        [TestCase(SecurityType.FutureOption, "20501231", true)]
        [TestCase(SecurityType.IndexOption, "20501231", true)]
        public void GetDelistingDate(SecurityType securityType, string expectedExpiration, bool isChain)
        {
            Symbol symbol = null;

            switch (securityType)
            {
                case SecurityType.Base:
                    symbol = Symbol.CreateBase(typeof(IndexedBaseData), Symbols.AAPL, Market.USA);
                    break;
                case SecurityType.Equity:
                    symbol = Symbols.AAPL;
                    break;
                case SecurityType.Option:
                    symbol = Symbols.SPY_C_192_Feb19_2016;
                    if (isChain)
                    {
                        symbol = symbol.Canonical;
                    }
                    else
                    {
                        expectedExpiration = symbol.ID.Date.ToString(DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                    }
                    break;
                case SecurityType.Forex:
                    symbol = Symbols.EURUSD;
                    break;
                case SecurityType.Future:
                    symbol = Symbols.Fut_SPY_Feb19_2016;
                    if (isChain)
                    {
                        symbol = symbol.Canonical;
                    }
                    else
                    {
                        expectedExpiration = symbol.ID.Date.ToString(DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                    }
                    break;
                case SecurityType.Cfd:
                    symbol = Symbols.DE30EUR;
                    break;
                case SecurityType.Crypto:
                    symbol = Symbols.BTCEUR;
                    break;
                case SecurityType.FutureOption:
                    symbol = Symbols.CreateFutureOptionSymbol(Symbols.Fut_SPY_Feb19_2016, OptionRight.Call, 10, new DateTime(2022, 05, 01));
                    if (isChain)
                    {
                        symbol = symbol.Canonical;
                    }
                    else
                    {
                        expectedExpiration = symbol.ID.Date.ToString(DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                    }
                    break;
                case SecurityType.Index:
                    symbol = Symbols.SPX;
                    break;
                case SecurityType.IndexOption:
                    symbol = Symbol.CreateOption(Symbols.SPX, Symbols.SPX.ID.Market, OptionStyle.European, OptionRight.Call, 1, new DateTime(2022, 05, 02));
                    if (isChain)
                    {
                        symbol = symbol.Canonical;
                    }
                    else
                    {
                        expectedExpiration = symbol.ID.Date.ToString(DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                    }
                    break;
                default:
                    break;
            }
            var mapFile = TestGlobals.MapFileProvider.Get(AuxiliaryDataKey.Create(symbol)).ResolveMapFile(symbol);
            Assert.AreEqual(Time.ParseDate(expectedExpiration), symbol.GetDelistingDate(mapFile));
        }

        [TestCase("20220101", false, true, Resolution.Daily)]
        [TestCase("20220101", false, false, Resolution.Daily)]
        [TestCase("20220103 09:31", true, false, Resolution.Minute)]
        [TestCase("20220103 07:31", false, false, Resolution.Minute)]
        [TestCase("20220103 07:31", false, false, Resolution.Daily)]
        [TestCase("20220103 07:31", true, true, Resolution.Daily)]
        [TestCase("20220103 08:31", true, true, Resolution.Daily)]
        public void IsMarketOpenSecurity(string exchangeTime, bool expectedResult, bool extendedMarketHours, Resolution resolution)
        {
            var security = CreateSecurity(Symbols.SPY);
            var utcTime = Time.ParseDate(exchangeTime).ConvertToUtc(security.Exchange.TimeZone);
            security.SetLocalTimeKeeper(new LocalTimeKeeper(utcTime, security.Exchange.TimeZone));

            Assert.AreEqual(expectedResult, security.IsMarketOpen(extendedMarketHours));
        }

        [TestCase("20220101", false, true)]
        [TestCase("20220101", false, false)]
        [TestCase("20220103 09:31", true, false)]
        [TestCase("20220103 07:31", false, false)]
        [TestCase("20220103 08:31", true, true)]
        public void IsMarketOpenSymbol(string nyTime, bool expectedResult, bool extendedMarketHours)
        {
            var utcTime = Time.ParseDate(nyTime).ConvertToUtc(TimeZones.NewYork);
            Assert.AreEqual(expectedResult, Symbols.SPY.IsMarketOpen(utcTime, extendedMarketHours));
        }

        [TestCase("CL XTN6UA1G9QKH")]
        [TestCase("ES VU1EHIDJYLMP")]
        [TestCase("ES VRJST036ZY0X")]
        [TestCase("GE YYBCLAZG1NGH")]
        [TestCase("GE YTC58AEQ4C8X")]
        [TestCase("BTC XTU2YXLMT1XD")]
        [TestCase("UB XUIP59QUPVS5")]
        [TestCase("NQ XUERCWA6EWAP")]
        [TestCase("PL XVJ4OQA3JSN5")]
        public void AdjustSymbolByOffsetTest(string future)
        {
            var sid = SecurityIdentifier.Parse(future);
            var symbol = new Symbol(sid, sid.Symbol);

            Assert.AreEqual(symbol.ID.Date, symbol.AdjustSymbolByOffset(0).ID.Date);

            var nextExpiration = symbol.AdjustSymbolByOffset(1);
            Assert.Greater(nextExpiration.ID.Date, symbol.ID.Date);

            var nextNextExpiration = symbol.AdjustSymbolByOffset(2);
            Assert.Greater(nextNextExpiration.ID.Date, nextExpiration.ID.Date);
        }

        [TestCase("A", "a")]
        [TestCase("", "")]
        [TestCase(null, null)]
        [TestCase("Buy", "buy")]
        [TestCase("BuyTheDip", "buyTheDip")]
        public void ToCamelCase(string toConvert, string expected)
        {
            Assert.AreEqual(expected, toConvert.ToCamelCase());
        }

        [Test]
        public void BatchAlphaResultPacket()
        {
            var btcusd = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);
            var insights = new List<Insight>
            {
                new Insight(DateTime.UtcNow, btcusd, Time.OneMillisecond, InsightType.Price, InsightDirection.Up, 1, 2, "sourceModel1"),
                new Insight(DateTime.UtcNow, btcusd, Time.OneSecond, InsightType.Price, InsightDirection.Down, 1, 2, "sourceModel1")
            };
            var orderEvents = new List<OrderEvent>
            {
                new OrderEvent(1, btcusd, DateTime.UtcNow, OrderStatus.Submitted, OrderDirection.Buy, 0, 0, OrderFee.Zero, message: "OrderEvent1"),
                new OrderEvent(1, btcusd, DateTime.UtcNow, OrderStatus.Filled, OrderDirection.Buy, 1, 1000, OrderFee.Zero, message: "OrderEvent2")
            };
            var orders = new List<Order> { new MarketOrder(btcusd, 1000, DateTime.UtcNow, "ExpensiveOrder") { Id = 1 } };

            var packet1 = new AlphaResultPacket("1", 1, insights: insights, portfolio: new AlphaStreamsPortfolioState { TotalPortfolioValue = 11 });
            var packet2 = new AlphaResultPacket("1", 1, orders: orders);
            var packet3 = new AlphaResultPacket("1", 1, orderEvents: orderEvents, portfolio: new AlphaStreamsPortfolioState { TotalPortfolioValue = 12 });

            var result = new List<AlphaResultPacket> { packet1, packet2, packet3 }.Batch();

            Assert.AreEqual(2, result.Insights.Count);
            Assert.AreEqual(2, result.OrderEvents.Count);
            Assert.AreEqual(1, result.Orders.Count);
            Assert.AreEqual(12, result.Portfolio.TotalPortfolioValue);

            Assert.IsTrue(result.Insights.SequenceEqual(insights));
            Assert.IsTrue(result.OrderEvents.SequenceEqual(orderEvents));
            Assert.IsTrue(result.Orders.SequenceEqual(orders));

            Assert.IsNull(new List<AlphaResultPacket>().Batch());
        }

        [Test]
        public void BatchAlphaResultPacketDuplicateOrder()
        {
            var btcusd = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);
            var orders = new List<Order>
            {
                new MarketOrder(btcusd, 1000, DateTime.UtcNow, "ExpensiveOrder") { Id = 1 },
                new MarketOrder(btcusd, 100, DateTime.UtcNow, "ExpensiveOrder") { Id = 2 },
                new MarketOrder(btcusd, 2000, DateTime.UtcNow, "ExpensiveOrder") { Id = 1 },
                new MarketOrder(btcusd, 10, DateTime.UtcNow, "ExpensiveOrder") { Id = 3 },
                new MarketOrder(btcusd, 3000, DateTime.UtcNow, "ExpensiveOrder") { Id = 1 }
            };
            var orders2 = new List<Order>
            {
                new MarketOrder(btcusd, 200, DateTime.UtcNow, "ExpensiveOrder") { Id = 2 },
                new MarketOrder(btcusd, 20, DateTime.UtcNow, "ExpensiveOrder") { Id = 3 }
            };

            var packet1 = new AlphaResultPacket("1", 1, orders: orders);
            var packet2 = new AlphaResultPacket("1", 1, orders: orders2);

            var result = new List<AlphaResultPacket> { packet1, packet2 }.Batch();

            // we expect just 1 order instance per order id
            Assert.AreEqual(3, result.Orders.Count);
            Assert.IsTrue(result.Orders.Any(order => order.Id == 1 && order.Quantity == 3000));
            Assert.IsTrue(result.Orders.Any(order => order.Id == 2 && order.Quantity == 200));
            Assert.IsTrue(result.Orders.Any(order => order.Id == 3 && order.Quantity == 20));

            var expected = new List<Order> { orders[4], orders2[0], orders2[1] };
            Assert.IsTrue(result.Orders.SequenceEqual(expected));
        }

        [Test]
        public void SeriesIsNotEmpty()
        {
            var series = new Series("SadSeries")
                { Values = new List<ChartPoint> { new ChartPoint(1, 1) } };

            Assert.IsFalse(series.IsEmpty());
        }

        [Test]
        public void SeriesIsEmpty()
        {
            Assert.IsTrue((new Series("Cat")).IsEmpty());
        }

        [Test]
        public void ChartIsEmpty()
        {
            Assert.IsTrue((new Chart("HappyChart")).IsEmpty());
        }

        [Test]
        public void ChartIsEmptyWithEmptySeries()
        {
            Assert.IsTrue((new Chart("HappyChart")
                { Series = new Dictionary<string, Series> { { "SadSeries", new Series("SadSeries") } }}).IsEmpty());
        }

        [Test]
        public void ChartIsNotEmptyWithNonEmptySeries()
        {
            var series = new Series("SadSeries")
                { Values = new List<ChartPoint> { new ChartPoint(1, 1) } };

            Assert.IsFalse((new Chart("HappyChart")
                { Series = new Dictionary<string, Series> { { "SadSeries", series } } }).IsEmpty());
        }

        [Test]
        public void IsSubclassOfGenericWorksWorksForNonGenericType()
        {
            Assert.IsTrue(typeof(Derived2).IsSubclassOfGeneric(typeof(Derived1)));
        }

        [Test]
        public void IsSubclassOfGenericWorksForGenericTypeWithParameter()
        {
            Assert.IsTrue(typeof(Derived1).IsSubclassOfGeneric(typeof(Super<int>)));
            Assert.IsFalse(typeof(Derived1).IsSubclassOfGeneric(typeof(Super<bool>)));
        }

        [Test]
        public void IsSubclassOfGenericWorksForGenericTypeDefinitions()
        {
            Assert.IsTrue(typeof(Derived1).IsSubclassOfGeneric(typeof(Super<>)));
            Assert.IsTrue(typeof(Derived2).IsSubclassOfGeneric(typeof(Super<>)));
        }

        [Test]
        public void DateTimeRoundDownFullDayDoesntRoundDownByDay()
        {
            var date = new DateTime(2000, 01, 01);
            var rounded = date.RoundDown(TimeSpan.FromDays(1));
            Assert.AreEqual(date, rounded);
        }

        [Test]
        public void GetBetterTypeNameHandlesRecursiveGenericTypes()
        {
            var type = typeof (Dictionary<List<int>, Dictionary<int, string>>);
            const string expected = "Dictionary<List<Int32>, Dictionary<Int32, String>>";
            var actual = type.GetBetterTypeName();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ExchangeRoundDownSkipsWeekends()
        {
            var time = new DateTime(2015, 05, 02, 18, 01, 00);
            var expected = new DateTime(2015, 05, 01);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.FXCM, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDown(Time.OneDay, hours, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownHandlesMarketOpenTime()
        {
            var time = new DateTime(2016, 1, 25, 9, 31, 0);
            var expected = time.Date;
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity);
            var exchangeRounded = time.ExchangeRoundDown(Time.OneDay, hours, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ConvertToSkipsDiscontinuitiesBecauseOfDaylightSavingsStart_AddingOneHour()
        {
            var expected = new DateTime(2014, 3, 9, 3, 0, 0);
            var time = new DateTime(2014, 3, 9, 2, 0, 0).ConvertTo(TimeZones.NewYork, TimeZones.NewYork);
            var time2 = new DateTime(2014, 3, 9, 2, 0, 1).ConvertTo(TimeZones.NewYork, TimeZones.NewYork);
            Assert.AreEqual(expected, time);
            Assert.AreEqual(expected, time2);
        }

        [Test]
        public void ConvertToIgnoreDaylightSavingsEnd_SubtractingOneHour()
        {
            var time1Expected = new DateTime(2014, 11, 2, 1, 59, 59);
            var time2Expected = new DateTime(2014, 11, 2, 2, 0, 0);
            var time3Expected = new DateTime(2014, 11, 2, 2, 0, 1);
            var time1 = time1Expected.ConvertTo(TimeZones.NewYork, TimeZones.NewYork);
            var time2 = time2Expected.ConvertTo(TimeZones.NewYork, TimeZones.NewYork);
            var time3 = time3Expected.ConvertTo(TimeZones.NewYork, TimeZones.NewYork);

            Assert.AreEqual(time1Expected, time1);
            Assert.AreEqual(time2Expected, time2);
            Assert.AreEqual(time3Expected, time3);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneSkipsWeekends()
        {
            // moment before EST market open in UTC (time + one day)
            var time = new DateTime(2017, 10, 01, 9, 29, 59).ConvertToUtc(TimeZones.NewYork);
            var expected = new DateTime(2017, 09, 29).ConvertFromUtc(TimeZones.NewYork);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneDay, hours, TimeZones.Utc, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        // This unit test reproduces a fixed infinite loop situation, due to a daylight saving time change, in ExchangeRoundDownInTimeZone, GH issue 2368.
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_AddingOneHour_UTC()
        {
            var time = new DateTime(2014, 3, 9, 16, 0, 1);
            var expected = new DateTime(2014, 3, 7, 16, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.Utc, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        // This unit test reproduces a fixed infinite loop situation, due to a daylight saving time change, in ExchangeRoundDownInTimeZone, GH issue 2368.
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_UTC()
        {
            var time = new DateTime(2014, 11, 2, 2, 0, 1);
            var expected = new DateTime(2014, 10, 31, 16, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.Utc, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_AddingOneHour_ExtendedHours_UTC()
        {
            var time = new DateTime(2014, 3, 9, 2, 0, 1);
            var expected = new DateTime(2014, 3, 9, 2, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.GDAX, null, SecurityType.Crypto);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.Utc, true);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_ExtendedHours_UTC()
        {
            var time = new DateTime(2014, 11, 2, 2, 0, 1);
            var expected = new DateTime(2014, 11, 2, 2, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.GDAX, null, SecurityType.Crypto);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.Utc, true);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        // We assert the behavior of noda time convert to utc around daylight saving start and end
        // Even though start and end un exchange TZ are 1.01:00:00 long (1 day & 1 hour) in utc it's always 1 day
        public void ConvertToUtcAndDayLightSavings()
        {
            {
                // day light savings starts
                var start = new DateTime(2011, 3, 12, 19, 0, 0);
                var end = new DateTime(2011, 3, 13, 20, 0, 0);

                var utcStart = start.ConvertToUtc(TimeZones.NewYork);
                var utcEnd = end.ConvertToUtc(TimeZones.NewYork);
                Assert.AreEqual(Time.OneDay, utcEnd - utcStart);
            }
            {
                // day light savings ends
                var start = new DateTime(2011, 11, 5, 20, 0, 0);
                var end = new DateTime(2011, 11, 6, 19, 0, 0);

                var utcStart = start.ConvertToUtc(TimeZones.NewYork);
                var utcEnd = end.ConvertToUtc(TimeZones.NewYork);
                Assert.AreEqual(Time.OneDay, utcEnd - utcStart);
            }
        }

        [Test]
        // this unit test reproduces a fixed infinite loop situation, due to a daylight saving time change, GH issue 3707.
        public void RoundDownInTimeZoneAroundDaylightTimeChanges()
        {
            // sydney time advanced Sunday, 6 October 2019, 02:00:00 clocks were turned forward 1 hour to
            // Sunday, 6 October 2019, 03:00:00 local daylight time instead.
            var timeAt = new DateTime(2019, 10, 6, 10, 0, 0);
            var expected = new DateTime(2019, 10, 5, 10, 0, 0);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneDay, TimeZones.Sydney, TimeZones.Utc);
            // even though there is an entire 'roundingInterval' unit (1 day) between 'timeAt' and 'expected' round down
            // is affected by daylight savings and rounds down the timeAt
            Assert.AreEqual(expected, exchangeRoundedAt);

            timeAt = new DateTime(2019, 10, 7, 10, 0, 0);
            expected = new DateTime(2019, 10, 6, 11, 0, 0);

            exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneDay, TimeZones.Sydney, TimeZones.Utc);
            Assert.AreEqual(expected, exchangeRoundedAt);
        }

        [Test]
        public void RoundDownInTimeZoneReturnsCorrectValuesAroundDaylightTimeChanges_AddingOneHour_UTC()
        {
            var timeAt = new DateTime(2014, 3, 9, 2, 0, 0);
            var timeAfter = new DateTime(2014, 3, 9, 2, 0, 1);
            var timeBefore = new DateTime(2014, 3, 9, 1, 59, 59);
            var timeAfterDaylightTimeChanges = new DateTime(2014, 3, 9, 3, 0, 0);

            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedAfter = timeAfter.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedBefore = timeBefore.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedAfterDaylightTimeChanges = timeAfterDaylightTimeChanges.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);

            var expected = new DateTime(2014, 3, 9, 3, 0, 0);
            Assert.AreEqual(expected, exchangeRoundedAt);
            Assert.AreEqual(expected, exchangeRoundedAfter);
            Assert.AreEqual(timeBefore, exchangeRoundedBefore);
            Assert.AreEqual(expected, exchangeRoundedAfterDaylightTimeChanges);
        }

        [Test]
        public void RoundDownInTimeZoneReturnsCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_UTC()
        {
            var timeAt = new DateTime(2014, 11, 2, 2, 0, 0);
            var timeAfter = new DateTime(2014, 11, 2, 2, 0, 1);
            var timeBefore = new DateTime(2014, 11, 2, 1, 59, 59);
            var timeAfterDaylightTimeChanges = new DateTime(2014, 11, 2, 3, 0, 0);

            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedAfter = timeAfter.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedBefore = timeBefore.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);
            var exchangeRoundedAfterDaylightTimeChanges = timeAfterDaylightTimeChanges.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.Utc);

            Assert.AreEqual(timeAt, exchangeRoundedAt);
            Assert.AreEqual(timeAfter, exchangeRoundedAfter);
            Assert.AreEqual(timeBefore, exchangeRoundedBefore);
            Assert.AreEqual(timeAfterDaylightTimeChanges, exchangeRoundedAfterDaylightTimeChanges);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_AddingOneHour_NewYork()
        {
            var time = new DateTime(2014, 3, 9, 16, 0, 1);
            var expected = new DateTime(2014, 3, 7, 16, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.NewYork, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_NewYork()
        {
            var time = new DateTime(2014, 11, 2, 2, 0, 1);
            var expected = new DateTime(2014, 10, 31, 16, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.NewYork, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_AddingOneHour_ExtendedHours_NewYork()
        {
            var time = new DateTime(2014, 3, 9, 2, 0, 1);
            var expected = new DateTime(2014, 3, 9, 2, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.GDAX, null, SecurityType.Crypto);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.NewYork, true);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_ExtendedHours_NewYork()
        {
            var time = new DateTime(2014, 11, 2, 2, 0, 1);
            var expected = new DateTime(2014, 11, 2, 2, 0, 0);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.GDAX, null, SecurityType.Crypto);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneHour, hours, TimeZones.NewYork, true);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void RoundDownInTimeZoneReturnsCorrectValuesAroundDaylightTimeChanges_AddingOneHour_NewYork()
        {
            var timeAt = new DateTime(2014, 3, 9, 2, 0, 0);
            var timeAfter = new DateTime(2014, 3, 9, 2, 0, 1);
            var timeBefore = new DateTime(2014, 3, 9, 1, 59, 59);
            var timeAfterDaylightTimeChanges = new DateTime(2014, 3, 9, 3, 0, 0);

            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedAfter = timeAfter.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedBefore = timeBefore.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedAfterDaylightTimeChanges = timeAfterDaylightTimeChanges.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);

            var expected = new DateTime(2014, 3, 9, 3, 0, 0);
            Assert.AreEqual(expected, exchangeRoundedAt);
            Assert.AreEqual(expected, exchangeRoundedAfter);
            Assert.AreEqual(timeBefore, exchangeRoundedBefore);
            Assert.AreEqual(expected, exchangeRoundedAfterDaylightTimeChanges);
        }

        [Test]
        public void RoundDownInTimeZoneReturnsCorrectValuesAroundDaylightTimeChanges_SubtractingOneHour_NewYork()
        {
            var timeAt = new DateTime(2014, 11, 2, 2, 0, 0);
            var timeAfter = new DateTime(2014, 11, 2, 2, 0, 1);
            var timeBefore = new DateTime(2014, 11, 2, 1, 59, 59);
            var timeAfterDaylightTimeChanges = new DateTime(2014, 11, 2, 3, 0, 0);

            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.Oanda, null, SecurityType.Forex);

            var exchangeRoundedAt = timeAt.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedAfter = timeAfter.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedBefore = timeBefore.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);
            var exchangeRoundedAfterDaylightTimeChanges = timeAfterDaylightTimeChanges.RoundDownInTimeZone(Time.OneSecond, hours.TimeZone, TimeZones.NewYork);

            Assert.AreEqual(timeAt, exchangeRoundedAt);
            Assert.AreEqual(timeAfter, exchangeRoundedAfter);
            Assert.AreEqual(timeBefore, exchangeRoundedBefore);
            Assert.AreEqual(timeAfterDaylightTimeChanges, exchangeRoundedAfterDaylightTimeChanges);
        }

        [Test]
        public void ConvertsInt32FromString()
        {
            const string input = "12345678";
            var value = input.ToInt32();
            Assert.AreEqual(12345678, value);
        }

        [Test]
        public void ConvertsInt32FromStringWithDecimalTruncation()
        {
            const string input = "12345678.9";
            var value = input.ToInt32();
            Assert.AreEqual(12345678, value);
        }

        [Test]
        public void ConvertsInt64FromString()
        {
            const string input = "12345678900";
            var value = input.ToInt64();
            Assert.AreEqual(12345678900, value);
        }

        [Test]
        public void ConvertsInt64FromStringWithDecimalTruncation()
        {
            const string input = "12345678900.12";
            var value = input.ToInt64();
            Assert.AreEqual(12345678900, value);
        }

        [Test]
        public void ToCsvDataParsesCorrectly()
        {
            var csv = "\"hello\",\"world\"".ToCsvData();
            Assert.AreEqual(2, csv.Count);
            Assert.AreEqual("\"hello\"", csv[0]);
            Assert.AreEqual("\"world\"", csv[1]);

            var csv2 = "1,2,3,4".ToCsvData();
            Assert.AreEqual(4, csv2.Count);
            Assert.AreEqual("1", csv2[0]);
            Assert.AreEqual("2", csv2[1]);
            Assert.AreEqual("3", csv2[2]);
            Assert.AreEqual("4", csv2[3]);
        }

        [Test]
        public void ToCsvDataParsesEmptyFinalValue()
        {
            var line = "\"hello\",world,";
            var csv = line.ToCsvData();

            Assert.AreEqual(3, csv.Count);
            Assert.AreEqual("\"hello\"", csv[0]);
            Assert.AreEqual("hello", csv[0].Trim('"'));
            Assert.AreEqual("world", csv[1]);
            Assert.AreEqual(string.Empty, csv[2]);
        }

        [Test]
        public void ToCsvDataParsesEmptyValue()
        {
            Assert.AreEqual(string.Empty, string.Empty.ToCsvData()[0]);
        }

        [Test]
        public void ConvertsDecimalFromString()
        {
            const string input = "123.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(123.45678m, value);
        }

        [Test]
        public void ConvertsDecimalFromStringWithExtraWhiteSpace()
        {
            const string input = " 123.45678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(123.45678m, value);
        }

        [Test]
        public void ConvertsDecimalFromIntStringWithExtraWhiteSpace()
        {
            const string input = " 12345678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(12345678m, value);
        }

        [Test]
        public void ConvertsZeroDecimalFromString()
        {
            const string input = "0.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(0.45678m, value);
        }

        [Test]
        public void ConvertsOneNumberDecimalFromString()
        {
            const string input = "1.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(1.45678m, value);
        }

        [Test]
        public void ConvertsZeroDecimalValueFromString()
        {
            const string input = "0";
            var value = input.ToDecimal();
            Assert.AreEqual(0m, value);
        }

        [Test]
        public void ConvertsEmptyDecimalValueFromString()
        {
            const string input = "";
            var value = input.ToDecimal();
            Assert.AreEqual(0m, value);
        }

        [Test]
        public void ConvertsNegativeDecimalFromString()
        {
            const string input = "-123.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(-123.45678m, value);
        }

        [Test]
        public void ConvertsNegativeDecimalFromStringWithExtraWhiteSpace()
        {
            const string input = " -123.45678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(-123.45678m, value);
        }

        [Test]
        public void ConvertsNegativeDecimalFromIntStringWithExtraWhiteSpace()
        {
            const string input = " -12345678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(-12345678m, value);
        }

        [Test]
        public void ConvertsNegativeZeroDecimalFromString()
        {
            const string input = "-0.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(-0.45678m, value);
        }

        [Test]
        public void ConvertsNegavtiveOneNumberDecimalFromString()
        {
            const string input = "-1.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(-1.45678m, value);
        }

        [Test]
        public void ConvertsNegativeZeroDecimalValueFromString()
        {
            const string input = "-0";
            var value = input.ToDecimal();
            Assert.AreEqual(-0m, value);
        }

        [TestCase("1.23%", 0.0123d)]
        [TestCase("-1.23%", -0.0123d)]
        [TestCase("31.2300%", 0.3123d)]
        [TestCase("20%", 0.2d)]
        [TestCase("-20%", -0.2d)]
        [TestCase("220%", 2.2d)]
        public void ConvertsPercent(string input, double expected)
        {
            Assert.AreEqual(new decimal(expected), input.ToNormalizedDecimal());
        }

        [Test]
        public void ConvertsTimeSpanFromString()
        {
            const string input = "16:00";
            var timespan = input.ConvertTo<TimeSpan>();
            Assert.AreEqual(TimeSpan.FromHours(16), timespan);
        }

        [Test]
        public void ConvertsDictionaryFromString()
        {
            var expected = new Dictionary<string, int> {{"a", 1}, {"b", 2}};
            var input = JsonConvert.SerializeObject(expected);
            var actual = input.ConvertTo<Dictionary<string, int>>();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DictionaryAddsItemToExistsList()
        {
            const int key = 0;
            var list = new List<int> {1, 2};
            var dictionary = new Dictionary<int, List<int>> {{key, list}};
            Extensions.Add(dictionary, key, 3);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void DictionaryAddCreatesNewList()
        {
            const int key = 0;
            var dictionary = new Dictionary<int, List<int>>();
            Extensions.Add(dictionary, key, 1);
            Assert.IsTrue(dictionary.ContainsKey(key));
            var list = dictionary[key];
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void SafeDecimalCasts()
        {
            var input = 2d;
            var output = input.SafeDecimalCast();
            Assert.AreEqual(2m, output);
        }

        [Test]
        public void SafeDecimalCastRespectsUpperBound()
        {
            var input = (double) decimal.MaxValue;
            var output = input.SafeDecimalCast();
            Assert.AreEqual(decimal.MaxValue, output);
        }

        [Test]
        public void SafeDecimalCastRespectsLowerBound()
        {
            var input = (double) decimal.MinValue;
            var output = input.SafeDecimalCast();
            Assert.AreEqual(decimal.MinValue, output);
        }

        [TestCase(Language.CSharp, double.NaN)]
        [TestCase(Language.Python, double.NaN)]
        [TestCase(Language.CSharp, double.NegativeInfinity)]
        [TestCase(Language.Python, double.NegativeInfinity)]
        [TestCase(Language.CSharp, double.PositiveInfinity)]
        [TestCase(Language.Python, double.PositiveInfinity)]
        public void SafeDecimalCastThrowsArgumentException(Language language, double number)
        {
            if (language == Language.CSharp)
            {
                Assert.Throws<ArgumentException>(() => number.SafeDecimalCast());
                return;
            }

            using (Py.GIL())
            {
                var pyNumber = number.ToPython();
                var csNumber = pyNumber.As<double>();
                Assert.Throws<ArgumentException>(() => csNumber.SafeDecimalCast());
            }
        }

        [Test]
        [TestCase(1.200, "1.2")]
        [TestCase(1200, "1200")]
        [TestCase(123.456, "123.456")]
        public void NormalizeDecimalReturnsNoTrailingZeros(decimal input, string expectedOutput)
        {
            var output = input.Normalize();
            Assert.AreEqual(expectedOutput, output.ToStringInvariant());
        }

        [Test]
        [TestCase(0.072842, 3, "0.0728")]
        [TestCase(0.0019999, 2, "0.0020")]
        [TestCase(0.01234568423, 6, "0.0123457")]
        public void RoundToSignificantDigits(decimal input, int digits, string expectedOutput)
        {
            var output = input.RoundToSignificantDigits(digits).ToStringInvariant();
            Assert.AreEqual(expectedOutput, output);
        }

        [Test]
        public void RoundsDownInTimeZone()
        {
            var dataTimeZone = TimeZones.Utc;
            var exchangeTimeZone = TimeZones.EasternStandard;
            var time = new DateTime(2000, 01, 01).ConvertTo(dataTimeZone, exchangeTimeZone);
            var roundedTime = time.RoundDownInTimeZone(Time.OneDay, exchangeTimeZone, dataTimeZone);
            Assert.AreEqual(time, roundedTime);
        }

        [Test]
        public void GetStringBetweenCharsTests()
        {
            const string expected = "python3.6";

            // Different characters cases
            var input = "[ python3.6 ]";
            var actual = input.GetStringBetweenChars('[', ']');
            Assert.AreEqual(expected, actual);

            input = "[ python3.6 ] [ python2.7 ]";
            actual = input.GetStringBetweenChars('[', ']');
            Assert.AreEqual(expected, actual);

            input = "[ python2.7 [ python3.6 ] ]";
            actual = input.GetStringBetweenChars('[', ']');
            Assert.AreEqual(expected, actual);

            // Same character cases
            input = "\'python3.6\'";
            actual = input.GetStringBetweenChars('\'', '\'');
            Assert.AreEqual(expected, actual);

            input = "\' python3.6 \' \' python2.7 \'";
            actual = input.GetStringBetweenChars('\'', '\'');
            Assert.AreEqual(expected, actual);

            // In this case, it is not equal
            input = "\' python2.7 \' python3.6 \' \'";
            actual = input.GetStringBetweenChars('\'', '\'');
            Assert.AreNotEqual(expected, actual);
        }

        [Test]
        public void PyObjectTryConvertQuoteBar()
        {
            // Wrap a QuoteBar around a PyObject and convert it back
            var value = ConvertToPyObject(new QuoteBar());

            QuoteBar quoteBar;
            var canConvert = value.TryConvert(out quoteBar);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(quoteBar);
            Assert.IsAssignableFrom<QuoteBar>(quoteBar);
        }

        [Test]
        public void PyObjectTryConvertSMA()
        {
            // Wrap a SimpleMovingAverage around a PyObject and convert it back
            var value = ConvertToPyObject(new SimpleMovingAverage(14));

            IndicatorBase<IndicatorDataPoint> indicatorBaseDataPoint;
            var canConvert = value.TryConvert(out indicatorBaseDataPoint);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(indicatorBaseDataPoint);
            Assert.IsAssignableFrom<SimpleMovingAverage>(indicatorBaseDataPoint);
        }

        [Test]
        public void PyObjectTryConvertATR()
        {
            // Wrap a AverageTrueRange around a PyObject and convert it back
            var value = ConvertToPyObject(new AverageTrueRange(14, MovingAverageType.Simple));

            IndicatorBase<IBaseDataBar> indicatorBaseDataBar;
            var canConvert = value.TryConvert(out indicatorBaseDataBar);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(indicatorBaseDataBar);
            Assert.IsAssignableFrom<AverageTrueRange>(indicatorBaseDataBar);
        }

        [Test]
        public void PyObjectTryConvertAD()
        {
            // Wrap a AccumulationDistribution around a PyObject and convert it back
            var value = ConvertToPyObject(new AccumulationDistribution("AD"));

            IndicatorBase<TradeBar> indicatorBaseTradeBar;
            var canConvert = value.TryConvert(out indicatorBaseTradeBar);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(indicatorBaseTradeBar);
            Assert.IsAssignableFrom<AccumulationDistribution>(indicatorBaseTradeBar);
        }

        [Test]
        public void PyObjectTryConvertCustomCSharpData()
        {
            // Wrap a custom C# data around a PyObject and convert it back
            var value = ConvertToPyObject(new CustomData());

            BaseData baseData;
            var canConvert = value.TryConvert(out baseData);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(baseData);
            Assert.IsAssignableFrom<CustomData>(baseData);
        }

        [Test]
        public void PyObjectTryConvertPythonClass()
        {
            PyObject value;
            using (Py.GIL())
            {
                // Try to convert a python class which inherits from a C# object
                value = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class Test(PythonData):
    def __init__(self):
        return 0;").GetAttr("Test");
            }

            Type type;
            bool canConvert = value.TryConvert(out type, true);
            Assert.IsTrue(canConvert);
        }

        [Test]
        public void PyObjectTryConvertSymbolArray()
        {
            PyObject value;
            using (Py.GIL())
            {
                // Wrap a Symbol Array around a PyObject and convert it back
                value = new PyList(new[] { Symbols.SPY.ToPython(), Symbols.AAPL.ToPython() });
            }

            Symbol[] symbols;
            var canConvert = value.TryConvert(out symbols);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(symbols);
            Assert.IsAssignableFrom<Symbol[]>(symbols);
        }

        [Test]
        public void PyObjectTryConvertFailCSharp()
        {
            // Try to convert a AccumulationDistribution as a QuoteBar
            var value = ConvertToPyObject(new AccumulationDistribution("AD"));

            QuoteBar quoteBar;
            bool canConvert = value.TryConvert(out quoteBar);
            Assert.IsFalse(canConvert);
            Assert.IsNull(quoteBar);
        }

        [Test]
        public void PyObjectTryConvertFailPython()
        {
            using (Py.GIL())
            {
                // Try to convert a python object as a IndicatorBase<TradeBar>
                using var locals = new PyDict();
                PythonEngine.Exec("class A:\n    pass", null, locals);
                var value = locals.GetItem("A").Invoke();

                IndicatorBase<TradeBar> indicatorBaseTradeBar;
                bool canConvert = value.TryConvert(out indicatorBaseTradeBar);
                Assert.IsFalse(canConvert);
                Assert.IsNull(indicatorBaseTradeBar);
            }
        }

        [Test]
        public void PyObjectTryConvertFailPythonClass()
        {
            PyObject value;
            using (Py.GIL())
            {
                // Try to convert a python class which inherits from a C# object
                value = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class Test(PythonData):
    def __init__(self):
        return 0;").GetAttr("Test");
            }

            Type type;
            bool canConvert = value.TryConvert(out type);
            Assert.IsFalse(canConvert);
        }

        [Test]
        [TestCase("coarseSelector = lambda coarse: [ x.Symbol for x in coarse if x.Price % 2 == 0 ]")]
        [TestCase("def coarseSelector(coarse): return [ x.Symbol for x in coarse if x.Price % 2 == 0 ]")]
        public void PyObjectTryConvertToFunc(string code)
        {
            Func<IEnumerable<CoarseFundamental>, Symbol[]> coarseSelector;

            using (Py.GIL())
            {
                using var locals = new PyDict();
                PythonEngine.Exec(code, null, locals);
                var pyObject = locals.GetItem("coarseSelector");
                pyObject.TryConvertToDelegate(out coarseSelector);
            }

            var coarse = Enumerable
                .Range(0, 9)
                .Select(x => new CoarseFundamental { Symbol = Symbol.Create(x.ToStringInvariant(), SecurityType.Equity, Market.USA), Value = x });

            var symbols = coarseSelector(coarse);

            Assert.AreEqual(5, symbols.Length);
            foreach (var symbol in symbols)
            {
                var price = symbol.Value.ConvertInvariant<int>();
                Assert.AreEqual(0, price % 2);
            }
        }

        [Test]
        public void PyObjectTryConvertToAction1()
        {
            Action<int> action;

            using (Py.GIL())
            {
                using var locals = new PyDict();
                PythonEngine.Exec("def raise_number(a): raise ValueError(a)", null, locals);
                var pyObject = locals.GetItem("raise_number");
                pyObject.TryConvertToDelegate(out action);
            }

            try
            {
                action(2);
                Assert.Fail();
            }
            catch (PythonException e)
            {
                Assert.AreEqual($"{2}", e.Message);
            }
        }

        [Test]
        public void PyObjectTryConvertToAction2()
        {
            Action<int, decimal> action;

            using (Py.GIL())
            {
                using var locals = new PyDict();
                PythonEngine.Exec("def raise_number(a, b): raise ValueError(a * b)", null, locals);
                var pyObject = locals.GetItem("raise_number");
                pyObject.TryConvertToDelegate(out action);
            }

            try
            {
                action(2, 3m);
                Assert.Fail();
            }
            catch (PythonException e)
            {
                Assert.AreEqual("6.0", e.Message);
            }
        }

        [Test]
        public void PyObjectTryConvertToNonDelegateFail()
        {
            int action;

            using (Py.GIL())
            {
                using var locals = new PyDict();
                PythonEngine.Exec("def raise_number(a, b): raise ValueError(a * b)", null, locals);
                var pyObject = locals.GetItem("raise_number");
                Assert.Throws<ArgumentException>(() => pyObject.TryConvertToDelegate(out action));
            }
        }

        [Test]
        public void PyObjectStringConvertToSymbolEnumerable()
        {
            SymbolCache.Clear();
            SymbolCache.Set("SPY", Symbols.SPY);

            IEnumerable<Symbol> symbols;
            using (Py.GIL())
            {
                symbols = new PyString("SPY").ConvertToSymbolEnumerable();
            }

            Assert.AreEqual(Symbols.SPY, symbols.Single());
        }

        [Test]
        public void PyObjectStringListConvertToSymbolEnumerable()
        {
            SymbolCache.Clear();
            SymbolCache.Set("SPY", Symbols.SPY);

            IEnumerable<Symbol> symbols;
            using (Py.GIL())
            {
                symbols = new PyList(new[] { "SPY".ToPython() }).ConvertToSymbolEnumerable();
            }

            Assert.AreEqual(Symbols.SPY, symbols.Single());
        }

        [Test]
        public void PyObjectSymbolConvertToSymbolEnumerable()
        {
            IEnumerable<Symbol> symbols;
            using (Py.GIL())
            {
                symbols = Symbols.SPY.ToPython().ConvertToSymbolEnumerable();
            }

            Assert.AreEqual(Symbols.SPY, symbols.Single());
        }

        [Test]
        public void PyObjectSymbolListConvertToSymbolEnumerable()
        {
            IEnumerable<Symbol> symbols;
            using (Py.GIL())
            {
                symbols = new PyList(new[] {Symbols.SPY.ToPython()}).ConvertToSymbolEnumerable();
            }

            Assert.AreEqual(Symbols.SPY, symbols.Single());
        }

        [Test]
        public void PyObjectNonSymbolObjectConvertToSymbolEnumerable()
        {
            using (Py.GIL())
            {
                Assert.Throws<ArgumentException>(() => new PyInt(1).ConvertToSymbolEnumerable().ToList());
            }
        }

        [Test]
        public void PyObjectDictionaryConvertToDictionary_Success()
        {
            using (Py.GIL())
            {
                var actualDictionary = PyModule.FromString(
                    "PyObjectDictionaryConvertToDictionary_Success",
                    @"
from datetime import datetime as dt
actualDictionary = dict()
actualDictionary.update({'SPY': dt(2019,10,3)})
actualDictionary.update({'QQQ': dt(2019,10,4)})
actualDictionary.update({'IBM': dt(2019,10,5)})
"
                ).GetAttr("actualDictionary").ConvertToDictionary<string, DateTime>();

                Assert.AreEqual(3, actualDictionary.Count);
                var expectedDictionary = new Dictionary<string, DateTime>
                {
                    {"SPY", new DateTime(2019,10,3) },
                    {"QQQ", new DateTime(2019,10,4) },
                    {"IBM", new DateTime(2019,10,5) },
                };

                foreach (var kvp in expectedDictionary)
                {
                    Assert.IsTrue(actualDictionary.ContainsKey(kvp.Key));
                    var actual = actualDictionary[kvp.Key];
                    Assert.AreEqual(kvp.Value, actual);
                }
            }
        }

        [Test]
        public void PyObjectDictionaryConvertToDictionary_FailNotDictionary()
        {
            using (Py.GIL())
            {
                var pyObject = PyModule.FromString(
                    "PyObjectDictionaryConvertToDictionary_FailNotDictionary",
                    "actualDictionary = list()"
                ).GetAttr("actualDictionary");

                Assert.Throws<ArgumentException>(() => pyObject.ConvertToDictionary<string, DateTime>());
            }
        }

        [Test]
        public void PyObjectDictionaryConvertToDictionary_FailWrongItemType()
        {
            using (Py.GIL())
            {
                var pyObject = PyModule.FromString(
                    "PyObjectDictionaryConvertToDictionary_FailWrongItemType",
                    @"
actualDictionary = dict()
actualDictionary.update({'SPY': 3})
actualDictionary.update({'QQQ': 4})
actualDictionary.update({'IBM': 5})
"
                ).GetAttr("actualDictionary");

                Assert.Throws<ArgumentException>(() => pyObject.ConvertToDictionary<string, DateTime>());
            }
        }


        [Test]
        public void BatchByDoesNotDropItems()
        {
            var list = new List<int> {1, 2, 3, 4, 5};
            var by2 = list.BatchBy(2).ToList();
            Assert.AreEqual(3, by2.Count);
            Assert.AreEqual(2, by2[0].Count);
            Assert.AreEqual(2, by2[1].Count);
            Assert.AreEqual(1, by2[2].Count);
            CollectionAssert.AreEqual(list, by2.SelectMany(x => x));
        }

        [Test]
        public void ToOrderTicketCreatesCorrectTicket()
        {
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, SecurityType.Equity, Symbols.USDJPY, 1000, 0, 1.11m, DateTime.Now, "Pepe");
            var order = Order.CreateOrder(orderRequest);
            order.Status = OrderStatus.Submitted;
            order.Id = 11;
            var orderTicket = order.ToOrderTicket(null);
            Assert.AreEqual(order.Id, orderTicket.OrderId);
            Assert.AreEqual(order.Quantity, orderTicket.Quantity);
            Assert.AreEqual(order.Status, orderTicket.Status);
            Assert.AreEqual(order.Type, orderTicket.OrderType);
            Assert.AreEqual(order.Symbol, orderTicket.Symbol);
            Assert.AreEqual(order.Tag, orderTicket.Tag);
            Assert.AreEqual(order.Time, orderTicket.Time);
            Assert.AreEqual(order.SecurityType, orderTicket.SecurityType);
        }

        [TestCase(4000, "4K")]
        [TestCase(4103, "4.1K")]
        [TestCase(40000, "40K")]
        [TestCase(45321, "45.3K")]
        [TestCase(654321, "654K")]
        [TestCase(600031, "600K")]
        [TestCase(1304303, "1.3M")]
        [TestCase(2600000, "2.6M")]
        [TestCase(26000000, "26M")]
        [TestCase(260000000, "260M")]
        [TestCase(2600000000, "2.6B")]
        [TestCase(26000000000, "26B")]
        public void ToFinancialFigures(double number, string expected)
        {
            var value = ((decimal)number).ToFinancialFigures();
            Assert.AreEqual(expected, value);
        }

        [Test]
        public void DecimalTruncateTo3DecimalPlaces()
        {
            var value = 10.999999m;
            Assert.AreEqual(10.999m, value.TruncateTo3DecimalPlaces());
        }

        [Test]
        public void DecimalTruncateTo3DecimalPlacesDoesNotThrowException()
        {
            var value = decimal.MaxValue;
            Assert.DoesNotThrow(() => value.TruncateTo3DecimalPlaces());

            value = decimal.MinValue;
            Assert.DoesNotThrow(() => value.TruncateTo3DecimalPlaces());

            value = decimal.MaxValue - 1;
            Assert.DoesNotThrow(() => value.TruncateTo3DecimalPlaces());

            value = decimal.MinValue + 1;
            Assert.DoesNotThrow(() => value.TruncateTo3DecimalPlaces());
        }

        [Test]
        public void DecimalAllowExponentTests()
        {
            const string strWithExponent = "5e-5";
            Assert.AreEqual(strWithExponent.ToDecimalAllowExponent(), 0.00005);
            Assert.AreNotEqual(strWithExponent.ToDecimal(), 0.00005);
            Assert.AreEqual(strWithExponent.ToDecimal(), 10275);
        }

        [Test]
        public void DateRulesToFunc()
        {
            var dateRules = new DateRules(new SecurityManager(
                new TimeKeeper(new DateTime(2015, 1, 1), DateTimeZone.Utc)), DateTimeZone.Utc);
            var first = new DateTime(2015, 1, 10);
            var second = new DateTime(2015, 1, 30);
            var dateRule = dateRules.On(first, second);
            var func = dateRule.ToFunc();

            Assert.AreEqual(first, func(new DateTime(2015, 1, 1)));
            Assert.AreEqual(first, func(new DateTime(2015, 1, 5)));
            Assert.AreEqual(second, func(first));
            Assert.AreEqual(Time.EndOfTime, func(second));
            Assert.AreEqual(Time.EndOfTime, func(second));
        }

        [Test]
        [TestCase(OptionRight.Call, true, OrderDirection.Sell)]
        [TestCase(OptionRight.Call, false, OrderDirection.Buy)]
        [TestCase(OptionRight.Put, true, OrderDirection.Buy)]
        [TestCase(OptionRight.Put, false, OrderDirection.Sell)]
        public void GetsExerciseDirection(OptionRight right, bool isShort, OrderDirection expected)
        {
            var actual = right.GetExerciseDirection(isShort);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void AppliesScalingToEquityTickQuotes()
        {
            // This test ensures that all Ticks with TickType == TickType.Quote have adjusted BidPrice and AskPrice.
            // Relevant issue: https://github.com/QuantConnect/Lean/issues/4788

            var algo = new QCAlgorithm();
            var dataFeed = new NullDataFeed();

            algo.SubscriptionManager = new SubscriptionManager();
            algo.SubscriptionManager.SetDataManager(new DataManager(
                dataFeed,
                new UniverseSelection(
                    algo,
                    new SecurityService(
                        new CashBook(),
                        MarketHoursDatabase.FromDataFolder(),
                        SymbolPropertiesDatabase.FromDataFolder(),
                        algo,
                        null,
                        null
                    ),
                    new DataPermissionManager(),
                    TestGlobals.DataProvider
                ),
                algo,
                new TimeKeeper(DateTime.UtcNow),
                MarketHoursDatabase.FromDataFolder(),
                false,
                null,
                new DataPermissionManager()
            ));

            using (var zipDataCacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider))
            {
                algo.HistoryProvider = new SubscriptionDataReaderHistoryProvider();
                algo.HistoryProvider.Initialize(
                    new HistoryProviderInitializeParameters(
                        null,
                        null,
                        null,
                        zipDataCacheProvider,
                        TestGlobals.MapFileProvider,
                        TestGlobals.FactorFileProvider,
                        (_) => {},
                        false,
                        new DataPermissionManager()));

                algo.SetStartDate(DateTime.UtcNow.AddDays(-1));

                var history = algo.History(new[] { Symbols.IBM }, new DateTime(2013, 10, 7), new DateTime(2013, 10, 8), Resolution.Tick).ToList();
                Assert.AreEqual(57460, history.Count);

                foreach (var slice in history)
                {
                    if (!slice.Ticks.ContainsKey(Symbols.IBM))
                    {
                        continue;
                    }

                    foreach (var tick in slice.Ticks[Symbols.IBM])
                    {
                        if (tick.BidPrice != 0)
                        {
                            Assert.LessOrEqual(Math.Abs(tick.Value - tick.BidPrice), 0.05);
                        }
                        if (tick.AskPrice != 0)
                        {
                            Assert.LessOrEqual(Math.Abs(tick.Value - tick.AskPrice), 0.05);
                        }
                    }
                }
            }
        }

        [Test]
        [TestCase(PositionSide.Long, OrderDirection.Buy)]
        [TestCase(PositionSide.Short, OrderDirection.Sell)]
        [TestCase(PositionSide.None, OrderDirection.Hold)]
        public void ToOrderDirection(PositionSide side, OrderDirection expected)
        {
            Assert.AreEqual(expected, side.ToOrderDirection());
        }

        [Test]
        [TestCase(OrderDirection.Buy, PositionSide.Long, false)]
        [TestCase(OrderDirection.Buy, PositionSide.Short, true)]
        [TestCase(OrderDirection.Buy, PositionSide.None, false)]
        [TestCase(OrderDirection.Sell, PositionSide.Long, true)]
        [TestCase(OrderDirection.Sell, PositionSide.Short, false)]
        [TestCase(OrderDirection.Sell, PositionSide.None, false)]
        [TestCase(OrderDirection.Hold, PositionSide.Long, false)]
        [TestCase(OrderDirection.Hold, PositionSide.Short, false)]
        [TestCase(OrderDirection.Hold, PositionSide.None, false)]
        public void Closes(OrderDirection direction, PositionSide side, bool expected)
        {
            Assert.AreEqual(expected, direction.Closes(side));
        }

        [Test]
        public void ListEquals()
        {
            var left = new[] {1, 2, 3};
            var right = new[] {1, 2, 3};
            Assert.IsTrue(left.ListEquals(right));

            right[2] = 4;
            Assert.IsFalse(left.ListEquals(right));
        }

        [Test]
        public void GetListHashCode()
        {
            var ints1 = new[] {1, 2, 3};
            var ints2 = new[] {1, 3, 2};
            var longs = new[] {1L, 2L, 3L};
            var decimals = new[] {1m, 2m, 3m};

            // ordering dependent
            Assert.AreNotEqual(ints1.GetListHashCode(), ints2.GetListHashCode());

            Assert.AreEqual(ints1.GetListHashCode(), decimals.GetListHashCode());

            // known type collision - long has same hash code as int within the int range
            // we could take a hash of typeof(T) but this would require ListEquals to enforce exact types
            // and we would prefer to allow typeof(T)'s GetHashCode and Equals to make this determination.
            Assert.AreEqual(ints1.GetListHashCode(), longs.GetListHashCode());

            // deterministic
            Assert.AreEqual(ints1.GetListHashCode(), new[] {1, 2, 3}.GetListHashCode());
        }

        [Test]
        [TestCase("0.999", "0.0001", "0.999")]
        [TestCase("0.999", "0.001", "0.999")]
        [TestCase("0.999", "0.01", "1.000")]
        [TestCase("0.999", "0.1", "1.000")]
        [TestCase("0.999", "1", "1.000")]
        [TestCase("0.999", "2", "0")]
        [TestCase("1.0", "0.15", "1.05")]
        [TestCase("1.05", "0.15", "1.05")]
        [TestCase("0.975", "0.15", "1.05")]
        [TestCase("-0.975", "0.15", "-1.05")]
        [TestCase("-1.0", "0.15", "-1.05")]
        [TestCase("-1.05", "0.15", "-1.05")]
        public void DiscretelyRoundBy(string valueString, string quantaString, string expectedString)
        {
            var value = decimal.Parse(valueString, CultureInfo.InvariantCulture);
            var quanta = decimal.Parse(quantaString, CultureInfo.InvariantCulture);
            var expected = decimal.Parse(expectedString, CultureInfo.InvariantCulture);
            var actual = value.DiscretelyRoundBy(quanta);
            Assert.AreEqual(expected, actual);
        }

        [TestCase(new int[] { 1, 2 }, 1)]
        [TestCase(new int[] { -1, 10 }, 1)]
        [TestCase(new int[] { 2, -5 }, 1)]
        [TestCase(new int[] { 1, 2, 3 }, 1)]
        [TestCase(new int[] { 200, -11, 7 }, 1)]
        [TestCase(new int[] { 10, 20 }, 10)]
        [TestCase(new int[] { -10, 100 }, 10)]
        [TestCase(new int[] { 20, -50 }, 10)]
        [TestCase(new int[] { 10, 20, 30 }, 10)]
        [TestCase(new int[] { 1000, -55, 35 }, 5)]
        [TestCase(new int[] { 24, 148, 36, 48, 52, 364 }, 4)]
        [TestCase(new int[] { 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97 }, 1)]
        public void GreatestCommonDivisorTests(int[] values, int expectedResult)
        {
            Assert.AreEqual(expectedResult, values.GreatestCommonDivisor());
        }

        private PyObject ConvertToPyObject(object value)
        {
            using (Py.GIL())
            {
                return value.ToPython();
            }
        }

        private class Super<T>
        {
        }

        private class Derived1 : Super<int>
        {
        }

        private class Derived2 : Derived1
        {
        }

        private static Security CreateSecurity(Symbol symbol)
        {
            var entry = MarketHoursDatabase.FromDataFolder()
                .GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);

            return new Security(symbol,
                entry.ExchangeHours,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }
    }
}
