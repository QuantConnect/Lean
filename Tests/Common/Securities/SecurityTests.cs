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
using System.Linq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityTests
    {
        [Test]
        public void SimplePropertiesTests()
        {
            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            var config = CreateTradeBarConfig();
            var security = new Security(
                exchangeHours,
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            Assert.AreEqual(config, security.Subscriptions.Single());
            Assert.AreEqual(config.Symbol, security.Symbol);
            Assert.AreEqual(config.SecurityType, security.Type);
            Assert.AreEqual(config.Resolution, security.Resolution);
            Assert.AreEqual(config.FillDataForward, security.IsFillDataForward);
            Assert.AreEqual(exchangeHours, security.Exchange.Hours);
        }

        [Test]
        public void ConstructorTests()
        {
            var security = GetSecurity();

            Assert.IsNotNull(security.Exchange);
            Assert.IsInstanceOf<SecurityExchange>(security.Exchange);
            Assert.IsNotNull(security.Cache);
            Assert.IsInstanceOf<SecurityCache>(security.Cache);
            Assert.IsNotNull(security.PortfolioModel);
            Assert.IsInstanceOf<SecurityPortfolioModel>(security.PortfolioModel);
            Assert.IsNotNull(security.FillModel);
            Assert.IsInstanceOf<ImmediateFillModel>(security.FillModel);
            Assert.IsNotNull(security.PortfolioModel);
            Assert.IsInstanceOf<InteractiveBrokersFeeModel>(security.FeeModel);
            Assert.IsNotNull(security.SlippageModel);
            Assert.IsInstanceOf<ConstantSlippageModel>(security.SlippageModel);
            Assert.IsNotNull(security.SettlementModel);
            Assert.IsInstanceOf<ImmediateSettlementModel>(security.SettlementModel);
            Assert.IsNotNull(security.BuyingPowerModel);
            Assert.IsInstanceOf<SecurityMarginModel>(security.BuyingPowerModel);
            Assert.IsNotNull(security.DataFilter);
            Assert.IsInstanceOf<SecurityDataFilter>(security.DataFilter);
        }

        [Test]
        public void HoldingsTests()
        {
            var security = GetSecurity();

            // Long 100 stocks test
            security.Holdings.SetHoldings(100m, 100);

            Assert.AreEqual(100m, security.Holdings.AveragePrice);
            Assert.AreEqual(100, security.Holdings.Quantity);
            Assert.IsTrue(security.HoldStock);
            Assert.IsTrue(security.Invested);
            Assert.IsTrue(security.Holdings.IsLong);
            Assert.IsFalse(security.Holdings.IsShort);

            // Short 100 stocks test
            security.Holdings.SetHoldings(100m, -100);

            Assert.AreEqual(100m, security.Holdings.AveragePrice);
            Assert.AreEqual(-100, security.Holdings.Quantity);
            Assert.IsTrue(security.HoldStock);
            Assert.IsTrue(security.Invested);
            Assert.IsFalse(security.Holdings.IsLong);
            Assert.IsTrue(security.Holdings.IsShort);

            // Flat test
            security.Holdings.SetHoldings(100m, 0);

            Assert.AreEqual(100m, security.Holdings.AveragePrice);
            Assert.AreEqual(0, security.Holdings.Quantity);
            Assert.IsFalse(security.HoldStock);
            Assert.IsFalse(security.Invested);
            Assert.IsFalse(security.Holdings.IsLong);
            Assert.IsFalse(security.Holdings.IsShort);

        }

        [Test]
        public void UpdatingSecurityPriceTests()
        {
            var security = GetSecurity();

            // Update securuty price with a TradeBar
            security.SetMarketPrice(new TradeBar(DateTime.Now, Symbols.SPY, 101m, 103m, 100m, 102m, 100000));

            Assert.AreEqual(101m, security.Open);
            Assert.AreEqual(103m, security.High);
            Assert.AreEqual(100m, security.Low);
            Assert.AreEqual(102m, security.Close);
            Assert.AreEqual(100000, security.Volume);

            // High/Close property is only modified by IBar instances
            security.SetMarketPrice(new Tick(DateTime.Now, Symbols.SPY, 104m, 104m, 104m));
            Assert.AreEqual(103m, security.High);
            Assert.AreEqual(102m, security.Close);
            Assert.AreEqual(104m, security.Price);

            // Low/Close property is only modified by IBar instances
            security.SetMarketPrice(new Tick(DateTime.Now, Symbols.SPY, 99m, 99m, 99m));
            Assert.AreEqual(100m, security.Low);
            Assert.AreEqual(102m, security.Close);
            Assert.AreEqual(99m, security.Price);
        }

        [Test]
        public void SetLeverageTest()
        {
            var security = GetSecurity();

            security.SetLeverage(4m);
            Assert.AreEqual(4m,security.Leverage);

            security.SetLeverage(5m);
            Assert.AreEqual(5m, security.Leverage);

            Assert.That(() => security.SetLeverage(0.1m),
                Throws.TypeOf<ArgumentException>().With.Message.EqualTo("Leverage must be greater than or equal to 1."));
        }

        [Test]
        public void DefaultDataNormalizationModeForOptionsIsRaw()
        {
            var option = new Option(
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY_P_192_Feb19_2016,
                    Resolution.Minute,
                    DateTimeZone.Utc,
                    DateTimeZone.Utc,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            Assert.AreEqual(option.DataNormalizationMode, DataNormalizationMode.Raw);
        }

        [Test]
        public void SetDataNormalizationForOptions()
        {
            var option = new Option(
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY_P_192_Feb19_2016,
                    Resolution.Minute,
                    DateTimeZone.Utc,
                    DateTimeZone.Utc,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            Assert.DoesNotThrow(() => { option.SetDataNormalizationMode(DataNormalizationMode.Raw); });

            Assert.Throws(typeof(ArgumentException), () => { option.SetDataNormalizationMode(DataNormalizationMode.Adjusted); });
            Assert.Throws(typeof(ArgumentException), () => { option.SetDataNormalizationMode(DataNormalizationMode.SplitAdjusted); });
            Assert.Throws(typeof(ArgumentException), () => { option.SetDataNormalizationMode(DataNormalizationMode.Adjusted); });
            Assert.Throws(typeof(ArgumentException), () => { option.SetDataNormalizationMode(DataNormalizationMode.TotalReturn); });
        }

        [Test]
        public void SetDataNormalizationForEquities()
        {
            var equity = new QuantConnect.Securities.Equity.Equity(
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    DateTimeZone.Utc,
                    DateTimeZone.Utc,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            Assert.DoesNotThrow(() => { equity.SetDataNormalizationMode(DataNormalizationMode.Raw); });
            Assert.DoesNotThrow(() => { equity.SetDataNormalizationMode(DataNormalizationMode.Adjusted); });
            Assert.DoesNotThrow(() => { equity.SetDataNormalizationMode(DataNormalizationMode.SplitAdjusted); });
            Assert.DoesNotThrow(() => { equity.SetDataNormalizationMode(DataNormalizationMode.Adjusted); });
            Assert.DoesNotThrow(() => { equity.SetDataNormalizationMode(DataNormalizationMode.TotalReturn); });
        }

        [Test]
        public void TickQuantityUpdatedInSecurityCache()
        {
            var tick1 = new Tick();
            tick1.Update(1, 1, 1, 10, 1, 1);

            var tick2 = new Tick();
            tick2.Update(1, 1, 1, 20, 1, 1);

            var securityCache = new SecurityCache();

            Assert.AreEqual(0, securityCache.Volume);

            securityCache.AddData(tick1);

            Assert.AreEqual(10, securityCache.Volume);

            securityCache.AddData(tick2);

            Assert.AreEqual(20, securityCache.Volume);
        }

        [Test]
        public void InvokingCacheStoreData_UpdatesSecurityData_ByTypeName()
        {
            var security = GetSecurity();
            var tradeBars = new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, security.Symbol, 10m, 20m, 5m, 15m, 10000)
            };

            security.Cache.StoreData(tradeBars, typeof(TradeBar));

            TradeBar fromSecurityData = security.Data.GetAll<TradeBar>()[0];
            Assert.AreEqual(tradeBars[0].Time, fromSecurityData.Time);
            Assert.AreEqual(tradeBars[0].Symbol, fromSecurityData.Symbol);
            Assert.AreEqual(tradeBars[0].Open, fromSecurityData.Open);
            Assert.AreEqual(tradeBars[0].High, fromSecurityData.High);
            Assert.AreEqual(tradeBars[0].Low, fromSecurityData.Low);
            Assert.AreEqual(tradeBars[0].Close, fromSecurityData.Close);
            Assert.AreEqual(tradeBars[0].Volume, fromSecurityData.Volume);

            // using dynamic accessor
            var fromDynamicSecurityData = security.Data.TradeBar[0];
            Assert.AreEqual(tradeBars[0].Time, fromDynamicSecurityData.Time);
            Assert.AreEqual(tradeBars[0].Symbol, fromDynamicSecurityData.Symbol);
            Assert.AreEqual(tradeBars[0].Open, fromDynamicSecurityData.Open);
            Assert.AreEqual(tradeBars[0].High, fromDynamicSecurityData.High);
            Assert.AreEqual(tradeBars[0].Low, fromDynamicSecurityData.Low);
            Assert.AreEqual(tradeBars[0].Close, fromDynamicSecurityData.Close);
            Assert.AreEqual(tradeBars[0].Volume, fromDynamicSecurityData.Volume);
        }

        internal static Security GetSecurity()
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                CreateTradeBarConfig(),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
        }

        internal static SubscriptionDataConfig CreateTradeBarConfig(Resolution resolution = Resolution.Minute)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, resolution, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
