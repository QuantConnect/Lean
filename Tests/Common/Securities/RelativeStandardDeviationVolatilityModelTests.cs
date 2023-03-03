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
using System.Linq;
using MathNet.Numerics.Statistics;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class RelativeStandardDeviationVolatilityModelTests
    {
        [Test]
        public void UpdatesAfterCorrectPeriodElapses()
        {
            const int periods = 3;
            var periodSpan = Time.OneMinute;
            var model = new RelativeStandardDeviationVolatilityModel(periodSpan, periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
            var security = GetSecurity(reference, model);

            var first = new IndicatorDataPoint(reference, 1);
            security.SetMarketPrice(first);

            Assert.AreEqual(0m, model.Volatility);

            const decimal value = 0.471404520791032M; // std of 1,2 is ~0.707 over a mean of 1.5
            var second = new IndicatorDataPoint(reference.AddMinutes(1), 2);
            security.SetMarketPrice(second);
            Assert.AreEqual(value, model.Volatility);

            // update should not be applied since not enough time has passed
            var third = new IndicatorDataPoint(reference.AddMinutes(1.01), 1000);
            security.SetMarketPrice(third);
            Assert.AreEqual(value, model.Volatility);

            var fourth = new IndicatorDataPoint(reference.AddMinutes(2), 3m);
            security.SetMarketPrice(fourth);
            Assert.AreEqual(0.5m, model.Volatility);
        }

        [Test]
        public void DoesntUpdateOnZeroPrice()
        {
            const int periods = 3;
            var periodSpan = Time.OneMinute;
            var model = new RelativeStandardDeviationVolatilityModel(periodSpan, periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
            var security = GetSecurity(reference, model);

            var first = new IndicatorDataPoint(reference, 1);
            security.SetMarketPrice(first);

            Assert.AreEqual(0m, model.Volatility);

            const decimal value = 0.471404520791032M; // std of 1,2 is ~0.707 over a mean of 1.5
            var second = new IndicatorDataPoint(reference.AddMinutes(1), 2);
            security.SetMarketPrice(second);
            Assert.AreEqual(value, model.Volatility);

            var third = new IndicatorDataPoint(reference.AddMinutes(2), 3m);
            security.SetMarketPrice(third);
            Assert.AreEqual(0.5m, model.Volatility);

            // update should not be applied as price is 0
            var forth = new IndicatorDataPoint(reference.AddMinutes(3), 0m);
            security.SetMarketPrice(forth);
            Assert.AreEqual(0.5m, model.Volatility);
        }

        [Test]
        public void GetHistoryRequirementsWorks()
        {
            var model = new RelativeStandardDeviationVolatilityModel(TimeSpan.FromDays(2), 4);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
            var security = GetSecurity(reference, model);

            var config = security.Subscriptions.First();
            model.SetSubscriptionDataConfigProvider(new MockSubscriptionDataConfigProvider(config));

            var result = model.GetHistoryRequirements(security, DateTime.UtcNow).First();

            Assert.AreEqual(config.DataNormalizationMode, result.DataNormalizationMode);
            Assert.AreEqual(config.Symbol, result.Symbol);
            Assert.AreEqual(config.DataTimeZone, result.DataTimeZone);
            Assert.AreEqual(config.IsCustomData, result.IsCustomData);
            Assert.AreEqual(config.FillDataForward, result.FillForwardResolution != null);
            Assert.AreEqual(config.ExtendedMarketHours, result.IncludeExtendedMarketHours);
            Assert.AreEqual(Resolution.Minute, result.Resolution);
        }

        [Test]
        public void GetHistoryRequirementsWorksForTwoDifferentSubscriptions()
        {
            var model = new RelativeStandardDeviationVolatilityModel(TimeSpan.FromDays(2), 4);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
            var security = GetSecurity(reference, model);

            var config = security.Subscriptions.First();
            var mock = new MockSubscriptionDataConfigProvider(config);
            mock.SubscriptionDataConfigs.Add(
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Second,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    false,
                    true));
            model.SetSubscriptionDataConfigProvider(mock);
            var result = model.GetHistoryRequirements(security, DateTime.UtcNow).First();

            Assert.AreEqual(config.DataNormalizationMode, result.DataNormalizationMode);
            Assert.AreEqual(config.Symbol, result.Symbol);
            Assert.AreEqual(config.DataTimeZone, result.DataTimeZone);
            Assert.AreEqual(Resolution.Second, result.Resolution);
            Assert.AreEqual(true, result.IsCustomData);
            Assert.AreEqual(true, result.FillForwardResolution != null);
            Assert.AreEqual(true, result.IncludeExtendedMarketHours);
        }

        [Test]
        public void HandlesPriceDiscontinuityDueToSplit()
        {
            const int periods = 3;
            var periodSpan = Time.OneMinute;
            var model = new RelativeStandardDeviationVolatilityModel(periodSpan, periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            security.SetMarketPrice(new IndicatorDataPoint(reference, 1));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(1), 2));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(1.01), 1000));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(2), 3m));

            var split = new Split(security.Symbol, reference.AddMinutes(2.5), 3m, 10m, SplitType.SplitOccurred);
            model.ApplySplit(split, false, DataNormalizationMode.Raw);
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(3), 30m));

            var prices = new[] { 20.0, 30.0, 30.0 };
            var expected = prices.StandardDeviation().SafeDecimalCast() / Math.Abs(prices.Mean().SafeDecimalCast());
            Assert.AreEqual(expected, model.Volatility);
        }

        [Test]
        public void HandlesPriceDiscontinuityDueToDividend()
        {
            const int periods = 3;
            var periodSpan = Time.OneMinute;
            var model = new RelativeStandardDeviationVolatilityModel(periodSpan, periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            security.SetMarketPrice(new IndicatorDataPoint(reference, 1));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(1), 2));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(1.01), 1000));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(2), 3m));

            var dividend = new Dividend(security.Symbol, reference.AddMinutes(2.5), .031m, 3.1m);
            model.ApplyDividend(dividend, false, DataNormalizationMode.Raw);
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(3), 3m));

            var prices = new[] { 2 * .99, 3 * .99, 3.0 };
            var expected = prices.StandardDeviation().SafeDecimalCast() / Math.Abs(prices.Mean().SafeDecimalCast());
            Assert.AreEqual(expected, model.Volatility);
        }

        [Test]
        public void HandlesPriceDiscontinuityDueToSplitAndDividend()
        {
            const int periods = 3;
            var periodSpan = Time.OneMinute;
            var model = new RelativeStandardDeviationVolatilityModel(periodSpan, periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            security.SetMarketPrice(new IndicatorDataPoint(reference, 1));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(1), 2));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(1.01), 1000));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(2), 3m));

            var split = new Split(security.Symbol, reference.AddMinutes(2.5), 3m, 10m, SplitType.SplitOccurred);
            model.ApplySplit(split, false, DataNormalizationMode.Raw);
            var dividend = new Dividend(security.Symbol, reference.AddMinutes(2.5), .031m, 3.1m);
            model.ApplyDividend(dividend, false, DataNormalizationMode.Raw);
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddMinutes(3), 30m));

            const double factor = 9.9; // Split 10 and Dividend 0.99
            var prices = new[] { 2 * factor, 3 * factor, 30.0 };
            var expected = prices.StandardDeviation().SafeDecimalCast() / Math.Abs(prices.Mean().SafeDecimalCast());
            Assert.AreEqual(expected, model.Volatility);
        }

        private static Security GetSecurity(DateTime reference, IVolatilityModel model)
        {
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, false, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            security.VolatilityModel = model;

            return security;
        }
    }
}
