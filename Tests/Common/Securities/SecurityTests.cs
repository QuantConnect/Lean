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
using QuantConnect.Indicators;
using Microsoft.CSharp.RuntimeBinder;
using Python.Runtime;
using QuantConnect.Statistics;

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
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
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
            Assert.IsInstanceOf<NullSlippageModel>(security.SlippageModel);
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

        private static TestCaseData[] IsMarketOpenWithMarketDataTestCases => new[]
        {
            // Without extended market hours
            new TestCaseData(new TimeSpan(3, 59, 59), false, false),
            new TestCaseData(new TimeSpan(4, 0, 0), false, false),
            new TestCaseData(new TimeSpan(9, 29, 59), false, false),
            new TestCaseData(new TimeSpan(9, 30, 0), false, true),
            new TestCaseData(new TimeSpan(15, 59, 59), false, true),
            new TestCaseData(new TimeSpan(16, 0, 0), false, false),
            new TestCaseData(new TimeSpan(19, 59, 59), false, false),
            new TestCaseData(new TimeSpan(20, 0, 0), false, false),
            new TestCaseData(new TimeSpan(21, 0, 0), false, false),
            // With extended market hours
            new TestCaseData(new TimeSpan(3, 59, 59), true, false),
            new TestCaseData(new TimeSpan(4, 0, 0), true, true),
            new TestCaseData(new TimeSpan(9, 29, 59), true, true),
            new TestCaseData(new TimeSpan(9, 30, 0), true, true),
            new TestCaseData(new TimeSpan(15, 59, 59), true, true),
            new TestCaseData(new TimeSpan(16, 0, 0), true, true),
            new TestCaseData(new TimeSpan(19, 59, 59), true, true),
            new TestCaseData(new TimeSpan(20, 0, 0), true, false),
            new TestCaseData(new TimeSpan(21, 0, 0), true, false),
        };

        [TestCaseSource(nameof(IsMarketOpenWithMarketDataTestCases))]
        public void IsMarketOpenIsAccurate(TimeSpan time, bool extendedMarketHours, bool expected)
        {
            var security = GetSecurity(isMarketAlwaysOpen: false);

            var dateTime = new DateTime(2023, 6, 26) + time;
            var timeKeeper = new LocalTimeKeeper(dateTime.ConvertToUtc(security.Exchange.TimeZone), security.Exchange.TimeZone);
            security.SetLocalTimeKeeper(timeKeeper);

            Assert.AreEqual(expected, security.IsMarketOpen(extendedMarketHours));
        }

        #region Custom properties tests

        [Test]
        public void SetsAndGetsDynamicCustomPropertiesUsingCacheInterface()
        {
            var security = GetSecurity();
            security.Cache.Properties.Add("Bool", true);
            security.Cache.Properties.Add("Integer", 1);
            security.Cache.Properties.Add("Double", 2.0);
            security.Cache.Properties.Add("Decimal", 3.0m);
            security.Cache.Properties.Add("String", "4");
            security.Cache.Properties.Add("DateTime", DateTime.UtcNow);
            security.Cache.Properties.Add("EMA", new ExponentialMovingAverage(10));

            Assert.AreEqual(7, security.Cache.Properties.Count);
            Assert.IsTrue(security.Cache.Properties.ContainsKey("Bool"));
            Assert.IsTrue(security.Cache.Properties.ContainsKey("Integer"));
            Assert.IsTrue(security.Cache.Properties.ContainsKey("Double"));
            Assert.IsTrue(security.Cache.Properties.ContainsKey("Decimal"));
            Assert.IsTrue(security.Cache.Properties.ContainsKey("String"));
            Assert.IsTrue(security.Cache.Properties.ContainsKey("DateTime"));
            Assert.IsTrue(security.Cache.Properties.ContainsKey("EMA"));

            Assert.IsFalse(security.Cache.Properties.ContainsKey("NotAProperty"));
        }

        [Test]
        public void SetsAndGetsDynamicCustomPropertiesUsingDynamicInterface()
        {
            var security = GetSecurity();
            dynamic dynamicSecurity = security;
            dynamicSecurity.Bool = true;
            dynamicSecurity.Integer = 1;
            dynamicSecurity.Double = 2.0;
            dynamicSecurity.Decimal = 3.0m;
            dynamicSecurity.String = "string";
            dynamicSecurity.DateTime = new DateTime(2023, 06, 20);
            dynamicSecurity.EMA = new ExponentialMovingAverage(10);

            Assert.AreEqual(true, dynamicSecurity.Bool);
            Assert.AreEqual(1, dynamicSecurity.Integer);
            Assert.AreEqual(2.0, dynamicSecurity.Double);
            Assert.AreEqual(3.0m, dynamicSecurity.Decimal);
            Assert.AreEqual("string", dynamicSecurity.String);
            Assert.AreEqual(new DateTime(2023, 06, 20), dynamicSecurity.DateTime);
            Assert.AreEqual(new ExponentialMovingAverage(10), dynamicSecurity.EMA);

            Assert.Throws<RuntimeBinderException>(() => { var notAProperty = dynamicSecurity.NotAProperty; });
        }

        [Test]
        public void SetsAndGetsDynamicCustomPropertiesUsingGenericInterface()
        {
            var security = GetSecurity();
            security.Add("Bool", true);
            security.Add("Integer", 1);
            security.Add("Double", 2.0);
            security.Add("Decimal", 3.0m);
            security.Add("String", "string");
            security.Add("DateTime", new DateTime(2023, 06, 20));
            security.Add("EMA", new ExponentialMovingAverage(10));

            Assert.AreEqual(true, security.TryGet<bool>("Bool", out var boolValue));
            Assert.AreEqual(true, boolValue);
            Assert.AreEqual(true, security.Get<bool>("Bool"));

            Assert.AreEqual(true, security.TryGet<int>("Integer", out var intValue));
            Assert.AreEqual(1, intValue);
            Assert.AreEqual(1, security.Get<int>("Integer"));

            Assert.AreEqual(true, security.TryGet<double>("Double", out var doubleValue));
            Assert.AreEqual(2.0, doubleValue);
            Assert.AreEqual(2.0, security.Get<double>("Double"));

            Assert.AreEqual(true, security.TryGet<decimal>("Decimal", out var decimalValue));
            Assert.AreEqual(3.0m, decimalValue);
            Assert.AreEqual(3.0m, security.Get<decimal>("Decimal"));

            Assert.AreEqual(true, security.TryGet<string>("String", out var stringValue));
            Assert.AreEqual("string", stringValue);
            Assert.AreEqual("string", security.Get<string>("String"));

            Assert.AreEqual(true, security.TryGet<DateTime>("DateTime", out var dateTimeValue));
            Assert.AreEqual(new DateTime(2023, 06, 20), dateTimeValue);
            Assert.AreEqual(new DateTime(2023, 06, 20), security.Get<DateTime>("DateTime"));

            Assert.AreEqual(true, security.TryGet<ExponentialMovingAverage>("EMA", out var emaValue));
            Assert.AreEqual(new ExponentialMovingAverage(10), emaValue);
            Assert.AreEqual(new ExponentialMovingAverage(10), security.Get<ExponentialMovingAverage>("EMA"));

            Assert.AreEqual(false, security.TryGet<bool>("NotAProperty", out _));
            Assert.Throws<KeyNotFoundException>(() => security.Get<bool>("NotAProperty"));

            Assert.Throws<InvalidCastException>(() => security.TryGet<SimpleMovingAverage>("EMA", out _));
            Assert.Throws<InvalidCastException>(() => security.Get<SimpleMovingAverage>("EMA"));
        }

        [Test]
        public void SetsAndGetsDynamicCustomPropertiesUsingIndexer()
        {
            var security = GetSecurity();
            security["Bool"] = true;
            security["Integer"] = 1;
            security["Double"] = 2.0;
            security["Decimal"] = 3.0m;
            security["String"] = "string";
            security["DateTime"] = new DateTime(2023, 06, 20);
            security["EMA"] = new ExponentialMovingAverage(10);

            Assert.AreEqual(true, security["Bool"]);
            Assert.AreEqual(1, security["Integer"]);
            Assert.AreEqual(2.0, security["Double"]);
            Assert.AreEqual(3.0m, security["Decimal"]);
            Assert.AreEqual("string", security["String"]);
            Assert.AreEqual(new DateTime(2023, 06, 20), security["DateTime"]);
            Assert.AreEqual(new ExponentialMovingAverage(10), security["EMA"]);

            Assert.Throws<KeyNotFoundException>(() => { var notAProperty = security["NotAProperty"]; });
        }

        [Test]
        public void RemovesCustomProperties()
        {
            var security = GetSecurity();
            security.Add("Bool", true);
            security.Add("DateTime", new DateTime(2023, 06, 20));

            Assert.IsTrue(security.Remove("Bool"));
            Assert.IsFalse(security.TryGet<bool>("Bool", out _));
            Assert.IsFalse(security.Remove("Bool"));

            Assert.IsTrue(security.Remove("DateTime", out DateTime dateTime));
            Assert.AreEqual(new DateTime(2023, 06, 20), dateTime);
            Assert.IsFalse(security.TryGet<DateTime>("DateTime", out _));
            Assert.IsFalse(security.Remove<DateTime>("DateTime", out _));
        }

        [Test]
        public void ClearsCustomProperties()
        {
            var security = GetSecurity();
            security.Add("Decimal", 3.0m);
            security.Add("DateTime", new DateTime(2023, 06, 20));

            Assert.AreEqual(2, security.Cache.Properties.Count);

            security.Clear();
            Assert.AreEqual(0, security.Cache.Properties.Count);
            Assert.IsFalse(security.TryGet<decimal>("Decimal", out _));
            Assert.IsFalse(security.TryGet<DateTime>("DateTime", out _));

        }

        [Test]
        public void OverwritesCustomProperties()
        {
            var security = GetSecurity();
            dynamic dynamicSecurity = security;

            dynamicSecurity.DateTime = new DateTime(2023, 06, 20);
            Assert.AreEqual(new DateTime(2023, 06, 20), dynamicSecurity.DateTime);

            dynamicSecurity.DateTime = new DateTime(2024, 06, 20);
            Assert.AreEqual(new DateTime(2024, 06, 20), dynamicSecurity.DateTime);
        }

        [Test]
        public void InvokesCustomPropertyMethod()
        {
            var security = GetSecurity();
            dynamic dynamicSecurity = security;

            dynamicSecurity.MakeEma = new Func<int, decimal, ExponentialMovingAverage>(
                (period, smoothingFactor) => new ExponentialMovingAverage(period, smoothingFactor));

            Assert.AreEqual(new ExponentialMovingAverage(10, 0.5m), dynamicSecurity.MakeEma(10, 0.5m));
        }

        [Test]
        public void KeepsPythonClassDerivedFromCSharpClassObjectReference()
        {
            var expectedCSharpPropertyValue = "C# property";
            var expectedPythonPropertyValue = "Python property";

            using (Py.GIL())
            {
                var testModule = PyModule.FromString("testModule",
                    $@"
from AlgorithmImports import *
from QuantConnect.Tests.Common.Securities import SecurityTests

class PythonTestClass(SecurityTests.CSharpTestClass):
    def __init__(self):
        super().__init__()

def SetSecurityDynamicProperty(security: Security) -> None:
    obj = PythonTestClass()
    obj.CSharpProperty = '{expectedCSharpPropertyValue}'
    obj.PythonProperty = '{expectedPythonPropertyValue}'
    security.PythonClassObject = obj

def AssertPythonClassObjectType(security: Security) -> None:
    if type(security.PythonClassObject) != PythonTestClass:
        raise Exception('PythonClassObject is not of type PythonTestClass')

def AccessCSharpProperty(security: Security) -> str:
    return security.PythonClassObject.CSharpProperty

def AccessPythonProperty(security: Security) -> str:
    return security.PythonClassObject.PythonProperty
        ");

                var security = GetSecurity();
                dynamic dynamicSecurity = security;

                dynamic SetSecurityDynamicProperty = testModule.GetAttr("SetSecurityDynamicProperty");
                SetSecurityDynamicProperty(security);

                dynamic AssertPythonClassObjectType = testModule.GetAttr("AssertPythonClassObjectType");
                Assert.DoesNotThrow(() => AssertPythonClassObjectType(security));

                // Access the C# class property
                dynamic AccessCSharpProperty = testModule.GetAttr("AccessCSharpProperty");
                Assert.AreEqual(expectedCSharpPropertyValue, AccessCSharpProperty(security).As<string>());
                Assert.AreEqual(expectedCSharpPropertyValue, dynamicSecurity.PythonClassObject.CSharpProperty.As<string>());

                // Access the Python class property
                dynamic AccessPythonProperty = testModule.GetAttr("AccessPythonProperty");
                Assert.AreEqual(expectedPythonPropertyValue, AccessPythonProperty(security).As<string>());
                Assert.AreEqual(expectedPythonPropertyValue, dynamicSecurity.PythonClassObject.PythonProperty.As<string>());
            }
        }

        [Test]
        public void RunSecurityDynamicPropertyPythonObjectReferenceRegressionAlgorithm()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("SecurityDynamicPropertyPythonClassAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "0"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "0%"},
                    {"Drawdown", "0%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "0%"},
                    {"Sharpe Ratio", "0"},
                    {"Probabilistic Sharpe Ratio", "0%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0"},
                    {"Beta", "0"},
                    {"Annual Standard Deviation", "0"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "0"},
                    {"Tracking Error", "0"},
                    {"Treynor Ratio", "0"},
                    {"Total Fees", "$0.00"},
                    {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }

        #endregion

        internal static Security GetSecurity(bool isMarketAlwaysOpen = true)
        {
            SecurityExchangeHours securityExchangeHours;
            if (isMarketAlwaysOpen)
            {
                securityExchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            }
            else
            {
                var marketHourDbEntry = MarketHoursDatabase.FromDataFolder().GetEntry(Market.USA, "SPY", SecurityType.Equity);
                securityExchangeHours = marketHourDbEntry.ExchangeHours;
            }

            return new Security(
                securityExchangeHours,
                CreateTradeBarConfig(),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        internal static SubscriptionDataConfig CreateTradeBarConfig(Resolution resolution = Resolution.Minute)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, resolution, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }

        public class CSharpTestClass
        {
            public string CSharpProperty { get; set; }
        }
    }
}
