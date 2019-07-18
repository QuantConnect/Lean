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
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Algorithm.Framework
{
    [TestFixture]
    public class InsightTests
    {
        [Test]
        public void HasReferenceTypeEqualitySemantics()
        {
            var one = Insight.Price(Symbols.SPY, Time.OneSecond, InsightDirection.Up);
            var two = Insight.Price(Symbols.SPY, Time.OneSecond, InsightDirection.Up);
            Assert.AreNotEqual(one, two);
            Assert.AreEqual(one, one);
            Assert.AreEqual(two, two);
        }

        [Test]
        public void SurvivesRoundTripSerializationUsingJsonConvert()
        {
            var time = new DateTime(2000, 01, 02, 03, 04, 05, 06);
            var insight = new Insight(time, Symbols.SPY, Time.OneMinute, InsightType.Volatility, InsightDirection.Up, 1, 2, "source-model", 1);
            Insight.Group(insight);
            insight.ReferenceValueFinal = 10;
            var serialized = JsonConvert.SerializeObject(insight);
            var deserialized = JsonConvert.DeserializeObject<Insight>(serialized);

            Assert.AreEqual(insight.CloseTimeUtc, deserialized.CloseTimeUtc);
            Assert.AreEqual(insight.Confidence, deserialized.Confidence);
            Assert.AreEqual(insight.Direction, deserialized.Direction);
            Assert.AreEqual(insight.EstimatedValue, deserialized.EstimatedValue);
            Assert.AreEqual(insight.GeneratedTimeUtc, deserialized.GeneratedTimeUtc);
            Assert.AreEqual(insight.GroupId, deserialized.GroupId);
            Assert.AreEqual(insight.Id, deserialized.Id);
            Assert.AreEqual(insight.Magnitude, deserialized.Magnitude);
            Assert.AreEqual(insight.Period, deserialized.Period);
            Assert.AreEqual(insight.SourceModel, deserialized.SourceModel);
            Assert.AreEqual(insight.Score.Direction, deserialized.Score.Direction);
            Assert.AreEqual(insight.Score.Magnitude, deserialized.Score.Magnitude);
            Assert.AreEqual(insight.Score.UpdatedTimeUtc, deserialized.Score.UpdatedTimeUtc);
            Assert.AreEqual(insight.Score.IsFinalScore, deserialized.Score.IsFinalScore);
            Assert.AreEqual(insight.Symbol, deserialized.Symbol);
            Assert.AreEqual(insight.Type, deserialized.Type);
            Assert.AreEqual(insight.Weight, deserialized.Weight);
            Assert.AreEqual(insight.ReferenceValueFinal, deserialized.ReferenceValueFinal);
        }

        [Test]
        public void SerializationUsingJsonConvertTrimsEstimatedValue()
        {
            var time = new DateTime(2000, 01, 02, 03, 04, 05, 06);
            var insight = new Insight(time, Symbols.SPY, Time.OneMinute, InsightType.Volatility, InsightDirection.Up, 1, 2, "source-model");
            insight.EstimatedValue = 0.00001m;
            insight.Score.SetScore(InsightScoreType.Direction, 0.00001, DateTime.UtcNow);
            insight.Score.SetScore(InsightScoreType.Magnitude, 0.00001, DateTime.UtcNow);
            var serialized = JsonConvert.SerializeObject(insight);
            var deserialized = JsonConvert.DeserializeObject<Insight>(serialized);

            Assert.AreEqual(0, deserialized.EstimatedValue);
            Assert.AreEqual(0, deserialized.Score.Direction);
            Assert.AreEqual(0, deserialized.Score.Magnitude);
        }

        [Test]
        public void SurvivesRoundTripCopy()
        {
            var time = new DateTime(2000, 01, 02, 03, 04, 05, 06);
            var original = new Insight(time, Symbols.SPY, Time.OneMinute, InsightType.Volatility, InsightDirection.Up, 1, 2, "source-model", 1);
            original.ReferenceValueFinal = 10;
            Insight.Group(original);

            var copy = original.Clone();

            Assert.AreEqual(original.CloseTimeUtc, copy.CloseTimeUtc);
            Assert.AreEqual(original.Confidence, copy.Confidence);
            Assert.AreEqual(original.Direction, copy.Direction);
            Assert.AreEqual(original.EstimatedValue, copy.EstimatedValue);
            Assert.AreEqual(original.GeneratedTimeUtc, copy.GeneratedTimeUtc);
            Assert.AreEqual(original.GroupId, copy.GroupId);
            Assert.AreEqual(original.Id, copy.Id);
            Assert.AreEqual(original.Magnitude, copy.Magnitude);
            Assert.AreEqual(original.Period, copy.Period);
            Assert.AreEqual(original.SourceModel, copy.SourceModel);
            Assert.AreEqual(original.Score.Direction, copy.Score.Direction);
            Assert.AreEqual(original.Score.Magnitude, copy.Score.Magnitude);
            Assert.AreEqual(original.Score.UpdatedTimeUtc, copy.Score.UpdatedTimeUtc);
            Assert.AreEqual(original.Score.IsFinalScore, copy.Score.IsFinalScore);
            Assert.AreEqual(original.Symbol, copy.Symbol);
            Assert.AreEqual(original.Type, copy.Type);
            Assert.AreEqual(original.Weight, copy.Weight);
            Assert.AreEqual(original.ReferenceValueFinal, copy.ReferenceValueFinal);
        }

        [Test]
        public void GroupAssignsGroupId()
        {
            var insight1 = Insight.Price(Symbols.SPY, Time.OneMinute, InsightDirection.Up);
            var insight2 = Insight.Price(Symbols.SPY, Time.OneMinute, InsightDirection.Up);
            var group = Insight.Group(insight1, insight2).ToList();
            foreach (var member in group)
            {
                Assert.IsTrue(member.GroupId.HasValue);
            }
            var groupId = insight1.GroupId.Value;
            foreach (var member in group)
            {
                Assert.AreEqual(groupId, member.GroupId);
            }
        }

        [Test]
        public void GroupThrowsExceptionIfInsightAlreadyHasGroupId()
        {
            var insight1 = Insight.Price(Symbols.SPY, Time.OneMinute, InsightDirection.Up);
            Insight.Group(insight1);
            Assert.That(() => Insight.Group(insight1), Throws.InvalidOperationException);
        }

        [Test]
        [TestCase(Resolution.Tick, 1)]
        [TestCase(Resolution.Tick, 10)]
        [TestCase(Resolution.Tick, 100)]
        [TestCase(Resolution.Second, 1)]
        [TestCase(Resolution.Second, 10)]
        [TestCase(Resolution.Second, 100)]
        [TestCase(Resolution.Minute, 1)]
        [TestCase(Resolution.Minute, 10)]
        [TestCase(Resolution.Minute, 100)]
        [TestCase(Resolution.Hour, 1)]
        [TestCase(Resolution.Hour, 10)]
        [TestCase(Resolution.Hour, 100)]
        [TestCase(Resolution.Daily, 1)]
        [TestCase(Resolution.Daily, 10)]
        [TestCase(Resolution.Daily, 100)]
        public void SetPeriodAndCloseTimeUsingResolutionBarCount(Resolution resolution, int barCount)
        {
            var generatedTimeUtc = new DateTime(2018, 08, 06, 13, 31, 0).ConvertToUtc(TimeZones.NewYork);

            var symbol = Symbols.SPY;
            var insight = Insight.Price(symbol, resolution, barCount, InsightDirection.Up);
            insight.GeneratedTimeUtc = generatedTimeUtc;
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            insight.SetPeriodAndCloseTime(exchangeHours);
            var expectedPeriod = Time.Max(Time.OneSecond, resolution.ToTimeSpan()).Multiply(barCount);
            Assert.AreEqual(expectedPeriod, insight.Period);

            var expectedCloseTime = Insight.ComputeCloseTime(exchangeHours, insight.GeneratedTimeUtc, resolution, barCount);
            Assert.AreEqual(expectedCloseTime, insight.CloseTimeUtc);
        }

        [Test]
        [TestCase(Resolution.Second, 1)]
        [TestCase(Resolution.Second, 10)]
        [TestCase(Resolution.Second, 100)]
        [TestCase(Resolution.Minute, 1)]
        [TestCase(Resolution.Minute, 10)]
        [TestCase(Resolution.Minute, 100)]
        [TestCase(Resolution.Hour, 1)]
        [TestCase(Resolution.Hour, 10)]
        [TestCase(Resolution.Hour, 100)]
        [TestCase(Resolution.Daily, 1)]
        [TestCase(Resolution.Daily, 10)]
        [TestCase(Resolution.Daily, 100)]
        public void SetPeriodAndCloseTimeUsingPeriod(Resolution resolution, int barCount)
        {
            var period = resolution.ToTimeSpan().Multiply(barCount);
            var generatedTimeUtc = new DateTime(2018, 08, 06, 13, 31, 0).ConvertToUtc(TimeZones.NewYork);

            var symbol = Symbols.SPY;
            var insight = Insight.Price(symbol, period, InsightDirection.Up);
            insight.GeneratedTimeUtc = generatedTimeUtc;
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            insight.SetPeriodAndCloseTime(exchangeHours);
            Assert.AreEqual(Time.Max(period, Time.OneSecond), insight.Period);

            var expectedCloseTime = Insight.ComputeCloseTime(exchangeHours, insight.GeneratedTimeUtc, period);
            Assert.AreEqual(expectedCloseTime, insight.CloseTimeUtc);
        }

        [Test]
        [TestCase(Resolution.Tick, 1)]
        [TestCase(Resolution.Tick, 10)]
        [TestCase(Resolution.Tick, 100)]
        [TestCase(Resolution.Second, 1)]
        [TestCase(Resolution.Second, 10)]
        [TestCase(Resolution.Second, 100)]
        [TestCase(Resolution.Minute, 1)]
        [TestCase(Resolution.Minute, 10)]
        [TestCase(Resolution.Minute, 100)]
        [TestCase(Resolution.Hour, 1)]
        [TestCase(Resolution.Hour, 10)]
        [TestCase(Resolution.Hour, 100)]
        [TestCase(Resolution.Daily, 1)]
        [TestCase(Resolution.Daily, 10)]
        [TestCase(Resolution.Daily, 100)]
        public void SetPeriodAndCloseTimeUsingCloseTime(Resolution resolution, int barCount)
        {
            // consistency test -- first compute expected close time and then back-compute period to verify
            var symbol = Symbols.SPY;
            var generatedTimeUtc = new DateTime(2018, 08, 06, 13, 31, 0).ConvertToUtc(TimeZones.NewYork);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);

            var baseline = Insight.Price(symbol, resolution, barCount, InsightDirection.Up);
            baseline.GeneratedTimeUtc = generatedTimeUtc;
            baseline.SetPeriodAndCloseTime(exchangeHours);
            var baselineCloseTimeLocal = baseline.CloseTimeUtc.ConvertFromUtc(TimeZones.NewYork);

            var insight = Insight.Price(symbol, baselineCloseTimeLocal, baseline.Direction);
            insight.GeneratedTimeUtc = generatedTimeUtc;
            insight.SetPeriodAndCloseTime(exchangeHours);

            Assert.AreEqual(baseline.Period, insight.Period);
            Assert.AreEqual(baseline.CloseTimeUtc, insight.CloseTimeUtc);
        }

        [Test]
        [TestCase("SPY", SecurityType.Equity, Market.USA, 2018, 12, 4, 9, 30)]
        [TestCase("EURUSD", SecurityType.Forex, Market.FXCM, 2018, 12, 4, 0, 0)]
        public void SetPeriodAndCloseTimeUsingExpiryEndOfDay(string ticker, SecurityType securityType, string market, int year, int month, int day, int hour, int minute)
        {
            var symbol = Symbol.Create(ticker, securityType, market);

            SetPeriodAndCloseTimeUsingExpiryFunc(
                Insight.Price(symbol, Expiry.EndOfDay, InsightDirection.Up),
                new DateTime(year, month, day, hour, minute, 0).ConvertToUtc(TimeZones.NewYork));
        }

        [Test]
        [TestCase("SPY", SecurityType.Equity, Market.USA, 2018, 12, 10, 9, 30)]
        [TestCase("EURUSD", SecurityType.Forex, Market.FXCM, 2018, 12, 10, 0, 0)]
        public void SetPeriodAndCloseTimeUsingExpiryEndOfWeek(string ticker, SecurityType securityType, string market, int year, int month, int day, int hour, int minute)
        {
            var symbol = Symbol.Create(ticker, securityType, market);

            SetPeriodAndCloseTimeUsingExpiryFunc(
                Insight.Price(symbol, Expiry.EndOfWeek, InsightDirection.Up),
                new DateTime(year, month, day, hour, minute, 0).ConvertToUtc(TimeZones.NewYork));
        }

        [Test]
        [TestCase("SPY", SecurityType.Equity, Market.USA, 2019, 1, 2, 9, 30)]
        [TestCase("EURUSD", SecurityType.Forex, Market.FXCM, 2019, 1, 2, 0, 0)]
        public void SetPeriodAndCloseTimeUsingExpiryEndOfMonth(string ticker, SecurityType securityType, string market, int year, int month, int day, int hour, int minute)
        {
            var symbol = Symbol.Create(ticker, securityType, market);

            SetPeriodAndCloseTimeUsingExpiryFunc(
                Insight.Price(symbol, Expiry.EndOfMonth, InsightDirection.Up),
                new DateTime(year, month, day, hour, minute, 0).ConvertToUtc(TimeZones.NewYork));
        }

        [Test]
        [TestCase("SPY", SecurityType.Equity, Market.USA, 2019, 1, 3, 9, 31)]
        [TestCase("EURUSD", SecurityType.Forex, Market.FXCM, 2019, 1, 3, 9, 31)]
        public void SetPeriodAndCloseTimeUsingExpiryOneMonth(string ticker, SecurityType securityType, string market, int year, int month, int day, int hour, int minute)
        {
            var symbol = Symbol.Create(ticker, securityType, market);

            SetPeriodAndCloseTimeUsingExpiryFunc(
                Insight.Price(symbol, Expiry.OneMonth, InsightDirection.Up),
                new DateTime(year, month, day, hour, minute, 0).ConvertToUtc(TimeZones.NewYork));
        }

        private void SetPeriodAndCloseTimeUsingExpiryFunc(Insight insight, DateTime expected)
        {
            var symbol = insight.Symbol;
            insight.GeneratedTimeUtc = new DateTime(2018, 12, 3, 9, 31, 0).ConvertToUtc(TimeZones.NewYork);

            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            insight.SetPeriodAndCloseTime(exchangeHours);

            Assert.AreEqual(expected, insight.CloseTimeUtc);
        }

        [Test]
        [TestCase(Resolution.Tick, 1)]
        [TestCase(Resolution.Tick, 10)]
        [TestCase(Resolution.Tick, 100)]
        [TestCase(Resolution.Second, 1)]
        [TestCase(Resolution.Second, 10)]
        [TestCase(Resolution.Second, 100)]
        [TestCase(Resolution.Minute, 1)]
        [TestCase(Resolution.Minute, 10)]
        [TestCase(Resolution.Minute, 100)]
        [TestCase(Resolution.Hour, 1)]
        [TestCase(Resolution.Hour, 5)]
        public void ComputePeriodOnSameLocalDateUsesSimpleSubtraction(Resolution resolution, int barCount)
        {
            var symbol = Symbols.SPY;
            var generatedTimeUtc = new DateTime(2018, 08, 06, 9, 31, 0).ConvertToUtc(TimeZones.NewYork);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var closeTimeUtc = generatedTimeUtc.Add(resolution.ToTimeSpan().Multiply(barCount));

            // confirm we're on the same date for the purposes of this test
            if (generatedTimeUtc.ConvertFromUtc(TimeZones.NewYork).Date != closeTimeUtc.ConvertFromUtc(TimeZones.NewYork).Date)
            {
                Assert.Fail("Precondition failed. This test requires generated and close times are on the same local date.");
            }

            var period = Insight.ComputePeriod(exchangeHours, generatedTimeUtc, closeTimeUtc);

            Assert.AreEqual(resolution.ToTimeSpan().Multiply(barCount), period);
        }

        [Test]
        [TestCase(Resolution.Hour, 10)]
        [TestCase(Resolution.Hour, 100)]
        [TestCase(Resolution.Hour, 1000)]
        [TestCase(Resolution.Daily, 1)]
        [TestCase(Resolution.Daily, 10)]
        [TestCase(Resolution.Daily, 100)]
        public void ComputePeriodOnDifferentLocalDatesPicksPeriodThatMinimizesAbsoluteErrorInComputedCloseTimeUsingResolutionBarCountApproach(Resolution resolution, int barCount)
        {
            var symbol = Symbols.SPY;
            var generatedTimeUtc = new DateTime(2018, 08, 06, 9, 31, 0).ConvertToUtc(TimeZones.NewYork);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var closeTimeUtc = Insight.ComputeCloseTime(exchangeHours, generatedTimeUtc, resolution, barCount);

            // confirm we're on the same date for the purposes of this test
            if (generatedTimeUtc.ConvertFromUtc(TimeZones.NewYork).Date == closeTimeUtc.ConvertFromUtc(TimeZones.NewYork).Date)
            {
                Assert.Fail("Precondition failed. This test requires generated and close times are on different local dates.");
            }

            var period = Insight.ComputePeriod(exchangeHours, generatedTimeUtc, closeTimeUtc);

            Assert.AreEqual(resolution.ToTimeSpan().Multiply(barCount), period);
        }

        [Test]
        public void ComputeCloseTimeHandlesFractionalDays()
        {
            var symbol = Symbols.SPY;
            // Friday @ 3PM + 2.5 days => Wednesday @ 12:45 by counting 2 dates (Mon, Tues@3PM) and then half a trading day (+3.25hrs) => Wed@11:45AM
            var generatedTimeUtc = new DateTime(2018, 08, 03, 12+3, 0, 0).ConvertToUtc(TimeZones.NewYork);
            var expectedClosedTimeUtc = new DateTime(2018, 08, 08, 11, 45, 0).ConvertToUtc(TimeZones.NewYork);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var actualCloseTimeUtc = Insight.ComputeCloseTime(exchangeHours, generatedTimeUtc, TimeSpan.FromDays(2.5));
            Assert.AreEqual(expectedClosedTimeUtc, actualCloseTimeUtc);
        }

        [Test]
        public void ComputeCloseTimeHandlesFractionalHours()
        {
            var symbol = Symbols.SPY;
            // Friday @ 3PM + 2.5 hours => Monday @ 11:00 (1 hr on Friday, 1.5 hours on Monday)
            var generatedTimeUtc = new DateTime(2018, 08, 03, 12 + 3, 0, 0).ConvertToUtc(TimeZones.NewYork);
            var expectedClosedTimeUtc = new DateTime(2018, 08, 06, 11, 0, 0).ConvertToUtc(TimeZones.NewYork);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var actualCloseTimeUtc = Insight.ComputeCloseTime(exchangeHours, generatedTimeUtc, TimeSpan.FromHours(2.5));
            Assert.AreEqual(expectedClosedTimeUtc, actualCloseTimeUtc);
        }

        [Test]
        public void ComputeCloseHandlesOffsetHourOverMarketClosuresUsingTimeSpan()
        {
            var symbol = Symbols.SPY;
            // Friday @ 3:59PM + 1 hours => Monday @ 10:29 (1 min on Friday, 59 min on Monday)
            var generatedTimeUtc = new DateTime(2018, 08, 03, 12 + 3, 59, 0).ConvertToUtc(TimeZones.NewYork);
            var expectedClosedTimeUtc = new DateTime(2018, 08, 06, 10, 29, 0).ConvertToUtc(TimeZones.NewYork);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var actualCloseTimeUtc = Insight.ComputeCloseTime(exchangeHours, generatedTimeUtc, TimeSpan.FromHours(1));
            Assert.AreEqual(expectedClosedTimeUtc, actualCloseTimeUtc);
        }

        [Test]
        public void ComputeCloseHandlesOffsetHourOverMarketClosuresUsingResolutionBarCount()
        {
            var symbol = Symbols.SPY;
            // Friday @ 3:59PM + 1 hours => Monday @ 10:29 (1 min on Friday, 59 min on Monday)
            var generatedTimeUtc = new DateTime(2018, 08, 03, 12 + 3, 59, 0).ConvertToUtc(TimeZones.NewYork);
            var expectedClosedTimeUtc = new DateTime(2018, 08, 06, 10, 29, 0).ConvertToUtc(TimeZones.NewYork);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var actualCloseTimeUtc = Insight.ComputeCloseTime(exchangeHours, generatedTimeUtc, Resolution.Hour, 1);
            Assert.AreEqual(expectedClosedTimeUtc, actualCloseTimeUtc);
        }

        [Test]
        public void SetPeriodAndCloseTimeThrowsWhenGeneratedTimeUtcNotSet()
        {
            var insight = Insight.Price(Symbols.SPY, Time.OneDay, InsightDirection.Up);
            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);

            Assert.That(() => insight.SetPeriodAndCloseTime(exchangeHours),
                Throws.InvalidOperationException);
        }

        [Test]
        public void SetPeriodAndCloseTimeDoesNotThrowWhenGeneratedTimeUtcIsSet()
        {
            var insight = Insight.Price(Symbols.SPY, Time.OneDay, InsightDirection.Up);
            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);

            insight.GeneratedTimeUtc = new DateTime(2018, 08, 07, 00, 33, 00).ConvertToUtc(TimeZones.NewYork);
            Assert.That(() => insight.SetPeriodAndCloseTime(exchangeHours),
                Throws.Nothing);
        }

        [Test]
        public void IsExpiredUsesOpenIntervalSemantics()
        {
            var generatedTime = new DateTime(2000, 01, 01);
            var insight = Insight.Price(Symbols.SPY, Time.OneMinute, InsightDirection.Up);
            insight.GeneratedTimeUtc = generatedTime;
            insight.CloseTimeUtc = insight.GeneratedTimeUtc + insight.Period;

            Assert.IsFalse(insight.IsExpired(insight.CloseTimeUtc));
            Assert.IsTrue(insight.IsExpired(insight.CloseTimeUtc.AddTicks(1)));
        }

        [Test]
        public void IsActiveUsesClosedIntervalSemantics()
        {
            var generatedTime = new DateTime(2000, 01, 01);
            var insight = Insight.Price(Symbols.SPY, Time.OneMinute, InsightDirection.Up);
            insight.GeneratedTimeUtc = generatedTime;
            insight.CloseTimeUtc = insight.GeneratedTimeUtc + insight.Period;

            Assert.IsTrue(insight.IsActive(insight.CloseTimeUtc));
            Assert.IsFalse(insight.IsActive(insight.CloseTimeUtc.AddTicks(1)));
        }
    }
}
