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
    public class StandardDeviationOfReturnsVolatilityModelTests
    {
        [Test]
        public void UpdatesAfterCorrectDailyPeriodElapses()
        {
            const int periods = 3;
            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            var first = new IndicatorDataPoint(reference, 1);
            security.SetMarketPrice(first);

            Assert.AreEqual(0m, model.Volatility);

            var second = new IndicatorDataPoint(reference.AddDays(1), 2);
            security.SetMarketPrice(second);
            Assert.AreEqual(0, model.Volatility);

            // update should not be applied since not enough time has passed
            var third = new IndicatorDataPoint(reference.AddDays(1.01), 1000);
            security.SetMarketPrice(third);
            Assert.AreEqual(0, model.Volatility);

            var fourth = new IndicatorDataPoint(reference.AddDays(2), 3);
            security.SetMarketPrice(fourth);
            Assert.AreEqual(5.6124, (double)model.Volatility, 0.0001);
        }

        [Test]
        public void DoesntUpdateOnZeroPrice()
        {
            const int periods = 3;
            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            var first = new IndicatorDataPoint(reference, 1);
            security.SetMarketPrice(first);

            Assert.AreEqual(0m, model.Volatility);

            var second = new IndicatorDataPoint(reference.AddDays(1), 2);
            security.SetMarketPrice(second);
            Assert.AreEqual(0, model.Volatility);

            // update should not be applied since not enough time has passed
            var third = new IndicatorDataPoint(reference.AddDays(1.01), 1000);
            security.SetMarketPrice(third);
            Assert.AreEqual(0, model.Volatility);

            var fourth = new IndicatorDataPoint(reference.AddDays(2), 3);
            security.SetMarketPrice(fourth);
            Assert.AreEqual(5.6124, (double)model.Volatility, 0.0001);

            // update should not be applied as price is 0
            var fifth = new IndicatorDataPoint(reference.AddDays(3), 0m);
            security.SetMarketPrice(fifth);
            Assert.AreEqual(5.6124, (double)model.Volatility, 0.0001);
        }

        [Test]
        public void GetHistoryRequirementsWorks()
        {
            const int periods = 3;
            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
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
            // the StandardDeviationOfReturnsVolatilityModel always uses daily
            Assert.AreEqual(Resolution.Daily, result.Resolution);
        }

        [Test]
        public void GetHistoryRequirementsWorksForTwoDifferentSubscriptions()
        {
            const int periods = 3;
            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
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
            Assert.AreEqual(true, result.IsCustomData);
            Assert.AreEqual(true, result.FillForwardResolution != null);
            Assert.AreEqual(true, result.IncludeExtendedMarketHours);
            // the StandardDeviationOfReturnsVolatilityModel always uses daily
            Assert.AreEqual(Resolution.Daily, result.Resolution);
        }

        [Test]
        public void UpdatesOnCustomConfigurationParametersOneMinute()
        {
            const int periods = 5;
            var model = new StandardDeviationOfReturnsVolatilityModel(periods, Resolution.Minute, TimeSpan.FromMinutes(1));
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            for (var i = 0; i < 5; i++)
            {
                if (i < 3)
                {
                    Assert.AreEqual(0, model.Volatility);
                }
                else
                {
                    Assert.AreNotEqual(0, model.Volatility);
                }

                model.Update(security, new TradeBar
                {
                    Open = 11 + (i - 1),
                    High = 11 + i,
                    Low = 9 - i,
                    Close = 11 + i,
                    Symbol = security.Symbol,
                    Time = reference.AddMinutes(i)
                });
            }

            Assert.AreNotEqual(0, model.Volatility);
        }

        [Test]
        public void MinuteResolutionSelectedForFuturesOptions()
        {
            const int periods = 5;
            var model = new StandardDeviationOfReturnsVolatilityModel(periods, Resolution.Minute, TimeSpan.FromMinutes(1));
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);
            var referenceUtc = reference.ConvertToUtc(TimeZones.Chicago);

            var underlyingSymbol = Symbol.Create("ES", SecurityType.Future, Market.CME);
            var futureOption = Symbol.CreateOption(
                underlyingSymbol,
                Market.CME,
                OptionStyle.American,
                OptionRight.Call,
                0,
                SecurityIdentifier.DefaultDate);

            var underlyingConfig = new SubscriptionDataConfig(typeof(TradeBar), underlyingSymbol, Resolution.Minute, TimeZones.Chicago, TimeZones.Chicago, true, false, false);
            var futureOptionConfig = new SubscriptionDataConfig(typeof(TradeBar), futureOption, Resolution.Minute, TimeZones.Chicago, TimeZones.Chicago, true, false, false);

            var underlyingSecurity = GetSecurity(reference, model, underlyingConfig);
            var futureOptionSecurity = GetSecurity(reference, model, futureOptionConfig);

            var mock = new MockSubscriptionDataConfigProvider();
            mock.SubscriptionDataConfigs.Add(underlyingConfig);
            mock.SubscriptionDataConfigs.Add(futureOptionConfig);
            model.SetSubscriptionDataConfigProvider(mock);

            var futureHistoryRequirements = model.GetHistoryRequirements(underlyingSecurity, referenceUtc);
            var optionHistoryRequirements = model.GetHistoryRequirements(futureOptionSecurity, referenceUtc);

            Assert.IsTrue(futureHistoryRequirements.All(x => x.Resolution == Resolution.Minute));
            Assert.IsTrue(optionHistoryRequirements.All(x => x.Resolution == Resolution.Minute));
        }

        [Test]
        public void HandlesPriceDiscontinuityDueToSplit()
        {
            const int periods = 3;
            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            security.SetMarketPrice(new IndicatorDataPoint(reference, 1));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(1), 2));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(1.01), 1000));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(2), 3));

            var split = new Split(security.Symbol, reference.AddDays(2.5), 3m, 10m, SplitType.SplitOccurred);
            model.ApplySplit(split, false, DataNormalizationMode.Raw);
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(3), 30m));

            var returns = new[] { 0.0, 1.0, 0.5 };
            var expected = returns.StandardDeviation().SafeDecimalCast() * (decimal)Math.Sqrt(252.0);
            Assert.AreEqual(expected, model.Volatility);
        }

        [Test]
        public void HandlesPriceDiscontinuityDueToDividend()
        {
            const int periods = 3;
            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            security.SetMarketPrice(new IndicatorDataPoint(reference, 1));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(1), 2));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(1.01), 1000));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(2), 3));

            var dividend = new Dividend(security.Symbol, reference.AddDays(2.5), .031m, 3.1m);
            model.ApplyDividend(dividend, false, DataNormalizationMode.Raw);
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(3), 3m));

            var returns = new[] { 3 / (3 * .99) - 1, 1.0, 0.5 };
            var expected = returns.StandardDeviation().SafeDecimalCast() * (decimal)Math.Sqrt(252.0);
            Assert.AreEqual(expected, model.Volatility);
        }

        [Test]
        public void HandlesPriceDiscontinuityDueToSplitAndDividend()
        {
            const int periods = 3;
            var model = new StandardDeviationOfReturnsVolatilityModel(periods);
            var reference = new DateTime(2016, 04, 06, 12, 0, 0);

            var security = GetSecurity(reference, model);

            security.SetMarketPrice(new IndicatorDataPoint(reference, 1));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(1), 2));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(1.01), 1000));
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(2), 3));

            var split = new Split(security.Symbol, reference.AddDays(2.5), 3m, 10m, SplitType.SplitOccurred);
            model.ApplySplit(split, false, DataNormalizationMode.Raw);
            var dividend = new Dividend(security.Symbol, reference.AddDays(2.5), .31m, 31m);
            model.ApplyDividend(dividend, false, DataNormalizationMode.Raw);
            security.SetMarketPrice(new IndicatorDataPoint(reference.AddDays(3), 30m));

            var returns = new[] { 30.0 / (3 * 9.9) - 1, 1.0, 0.5 };
            var expected = returns.StandardDeviation().SafeDecimalCast() * (decimal)Math.Sqrt(252.0);
            Assert.AreEqual(expected, model.Volatility);
        }

        private static Security GetSecurity(DateTime reference, IVolatilityModel model, SubscriptionDataConfig config = null)
        {
            var exchangeTimeZone = config?.ExchangeTimeZone ?? TimeZones.NewYork;
            var referenceUtc = reference.ConvertToUtc(exchangeTimeZone);
            var timeKeeper = new TimeKeeper(referenceUtc);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(exchangeTimeZone),
                config ?? new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, false, false),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(exchangeTimeZone));

            security.VolatilityModel = model;

            return security;
        }
    }
}
