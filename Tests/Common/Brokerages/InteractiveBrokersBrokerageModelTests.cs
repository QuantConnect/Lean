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

using Moq;
using NUnit.Framework;

using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Data;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Forex;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Securities.Cfd;

namespace QuantConnect.Tests.Common.Brokerages
{

    [TestFixture, Parallelizable(ParallelScope.All)]
    public class InteractiveBrokersBrokerageModelTests
    {
        private readonly InteractiveBrokersBrokerageModel _interactiveBrokersBrokerageModel = new InteractiveBrokersBrokerageModel();

        [TestCaseSource(nameof(GetUnsupportedOptions))]
        public void CannotSubmitOrder_IndexOptionExercise(Security security)
        {
            var order = new Mock<OptionExerciseOrder>();
            order.Setup(x => x.Type).Returns(OrderType.OptionExercise);

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order.Object, out var message);

            Assert.IsFalse(canSubmit, message.Message);
            Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
            Assert.AreEqual("NotSupported", message.Code);
            StringAssert.Contains("exercises for index and cash-settled options", message.Message);
        }

        [TestCaseSource(nameof(GetForexOrderTestCases))]
        public void CanSubmitOrder_ForexWithinAllowableOrderSize(Forex security, decimal quantity, bool shouldSubmit)
        {
            var order = new MarketOrder(security.Symbol, quantity, new DateTime(2023, 1, 20));

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.AreEqual(shouldSubmit, canSubmit);

            if (shouldSubmit)
            {
                Assert.IsNull(message);
            }
            else
            {
                Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
                Assert.AreEqual("OrderSizeLimit", message.Code);
                StringAssert.Contains("minimum and maximum limits for the allowable order size are", message.Message);
            }
        }

        [TestCase("SPY", SecurityType.Option)]
        [TestCase("SPX", SecurityType.IndexOption)]
        [TestCase("ES", SecurityType.FutureOption)]
        public void CannotSubmitMOCOrdersForOptions(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);
            if (securityType == SecurityType.FutureOption)
            {
                var underlyingFuture = Symbol.CreateFuture(
                QuantConnect.Securities.Futures.Indices.SP500EMini,
                Market.CME,
                new DateTime(2021, 3, 19));

                var futureOption = Symbol.CreateOption(underlyingFuture,
                    Market.CME,
                    OptionStyle.American,
                    OptionRight.Call,
                    2550m,
                    new DateTime(2021, 3, 19));

                security = new QuantConnect.Securities.FutureOption.FutureOption(
                    futureOption,
                    MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.CME, futureOption, futureOption.SecurityType),
                    new Cash("USD", 100000m, 1m),
                    new OptionSymbolProperties(string.Empty, "USD", 1m, 0.01m, 1m),
                    new CashBook(),
                    new RegisteredSecurityDataTypesProvider(),
                    new SecurityCache(),
                    null);
            }

            var order = new MarketOnCloseOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsFalse(result);
            var expectedMessage = "InteractiveBrokers does not support Market-on-Close orders for other security types different than Future and Equity.";
            Assert.AreEqual(expectedMessage, message.Message);
        }

        [TestCase("EURGBP", SecurityType.Forex)]
        public void CannotSubmitMOCOrdersForForexAndCfd(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);

            var order = new MarketOnCloseOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsFalse(result);
            var expectedMessage = "InteractiveBrokers does not support Market-on-Close orders for other security types different than Future and Equity.";
            Assert.AreEqual(expectedMessage, message.Message);
        }

        [TestCase("EURGBP", SecurityType.Forex)]
        [TestCase("ES", SecurityType.Future)]
        public void CannotSubmitMOOOrdersForForexCfdAndFutureOrders(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);

            var order = new MarketOnOpenOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsFalse(result);
            var expectedMessage = $"The broker does not support Market-on-Open orders for security type {security.Type}";
            Assert.AreEqual(expectedMessage, message.Message);
        }

        [TestCase("SPY", SecurityType.Option)]
        [TestCase("SPY", SecurityType.Equity)]
        [TestCase("DE10YBEUR", SecurityType.Cfd)]
        public void CanSubmitMOOOrdersForOptionAndEquity(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);

            var order = new MarketOnOpenOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsTrue(result);
        }

        [TestCase(OrderType.ComboLegLimit, 2, true)]
        [TestCase(OrderType.ComboLimit, 4, true)]
        [TestCase(OrderType.ComboLegLimit, 4, false)]
        public void CanSubmitComboOrdersWithExpectedLegValidation(OrderType orderType, int legCount, bool shouldSubmit)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(SecurityType.Option, "SPY");
            var groupOrderManager = new GroupOrderManager(1, legCount, 1, 100m);

            Order order = orderType switch
            {
                OrderType.ComboLimit => new ComboLimitOrder(security.Symbol, 1, 100m, DateTime.UtcNow, groupOrderManager),
                OrderType.ComboLegLimit => new ComboLegLimitOrder(security.Symbol, 1, 100m, DateTime.UtcNow, groupOrderManager),
                _ => throw new ArgumentOutOfRangeException(nameof(orderType), orderType, "Unexpected combo order type")
            };

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.AreEqual(shouldSubmit, canSubmit);

            if (shouldSubmit)
            {
                Assert.IsNull(message);
            }
            else
            {
                Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
                Assert.AreEqual("NotSupported", message.Code);
                StringAssert.Contains("does not support four-leg ComboLegLimit orders", message.Message);
            }
        }

        [TestCase("ES", SecurityType.Future)]
        [TestCase("SPY", SecurityType.Equity)]
        [TestCase("DE10YBEUR", SecurityType.Cfd)]
        public void CanSubmitMOCOrdersForFutureAndEquity(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);

            var order = new MarketOnCloseOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsTrue(result);
        }

        // where the backtest data lives
        [Test]
        public void CryptoDefaultsToTheCoinbaseMarket()
        {
            Assert.AreEqual(Market.Coinbase, InteractiveBrokersBrokerageModel.DefaultMarketMap[SecurityType.Crypto]);

            var security = GetInteractiveBrokersCrypto();
            Assert.AreEqual(Market.Coinbase, security.Symbol.ID.Market);
        }

        // the interactivebrokers entries are the registry of what IB lists
        [Test]
        public void KeepsTheBrokerageTickSizeOnTheInteractiveBrokersEntry()
        {
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.InteractiveBrokers);
            var properties = SymbolPropertiesDatabase.FromDataFolder()
                .GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, Currencies.USD);

            // measured from IB's contract details, the crypto market says 0.01
            Assert.AreEqual(0.25m, properties.MinimumPriceVariation);
        }

        // IB only accepts market and limit orders for cryptocurrencies
        [TestCase(OrderType.Market, true)]
        [TestCase(OrderType.Limit, true)]
        [TestCase(OrderType.StopMarket, false)]
        [TestCase(OrderType.StopLimit, false)]
        [TestCase(OrderType.TrailingStop, false)]
        [TestCase(OrderType.LimitIfTouched, false)]
        public void CanSubmitOnlyMarketAndLimitCryptoOrders(OrderType orderType, bool shouldSubmit)
        {
            var security = GetInteractiveBrokersCrypto();
            var now = new DateTime(2024, 1, 3);
            // a buy market order needs a price, a buy limit has to sit at the market
            security.SetMarketPrice(new Tick(now, security.Symbol, 100m, 100m));

            Order order = orderType switch
            {
                OrderType.Market => new MarketOrder(security.Symbol, 1, now),
                OrderType.Limit => new LimitOrder(security.Symbol, 1, 100m, now),
                OrderType.StopMarket => new StopMarketOrder(security.Symbol, 1, 100m, now),
                OrderType.StopLimit => new StopLimitOrder(security.Symbol, 1, 100m, 100m, now),
                OrderType.TrailingStop => new TrailingStopOrder(security.Symbol, 1, 100m, 1m, false, now),
                OrderType.LimitIfTouched => new LimitIfTouchedOrder(security.Symbol, 1, 100m, 100m, now),
                _ => throw new ArgumentOutOfRangeException(nameof(orderType), orderType, "Unexpected crypto order type")
            };

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.AreEqual(shouldSubmit, canSubmit);

            if (shouldSubmit)
            {
                Assert.IsNull(message);
            }
            else
            {
                Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
                Assert.AreEqual("NotSupported", message.Code);
                StringAssert.Contains($"does not support {orderType} orders for {SecurityType.Crypto}", message.Message);
            }
        }

        [TestCase("BTCUSD")]
        [TestCase("ETHUSD")]
        [TestCase("SOLUSD")]
        public void CreatesListedCryptoPairs(string ticker)
        {
            var security = GetInteractiveBrokersCrypto(ticker);

            Assert.AreEqual(Market.Coinbase, security.Symbol.ID.Market);
            Assert.AreEqual(Currencies.USD, security.QuoteCurrency.Symbol);
        }

        // creatable, so it can be backtested, rejected at order time
        [TestCase("BTCEUR")]     // IB quotes crypto against US dollars only
        [TestCase("ETHBTC")]     // no crypto quoted pairs either
        [TestCase("ZRXUSD")]     // a coinbase pair IB does not list
        public void CannotSubmitOrdersForUnlistedCryptoPairs(string ticker)
        {
            var security = GetInteractiveBrokersCrypto(ticker);
            var order = new MarketOrder(security.Symbol, 1, new DateTime(2024, 1, 3));

            Assert.IsFalse(_interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message));
            Assert.AreEqual("NotSupported", message.Code);
            StringAssert.Contains($"does not support {ticker}", message.Message);
        }

        [TestCase(2024, 1, 4, 15, true)]    // Thursday
        [TestCase(2024, 1, 5, 20, true)]    // Friday 15:00 New York
        [TestCase(2024, 1, 5, 22, false)]   // Friday 17:00 New York
        [TestCase(2024, 1, 6, 15, false)]   // Saturday
        [TestCase(2024, 1, 7, 7, false)]    // Sunday 02:00 New York
        [TestCase(2024, 1, 7, 9, true)]     // Sunday 04:00 New York
        public void CanSubmitCryptoOrdersOnlyWhileTheVenueIsOpen(int year, int month, int day, int utcHour, bool shouldSubmit)
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
            var security = algorithm.AddCrypto("BTCUSD");
            algorithm.SetDateTime(new DateTime(year, month, day, utcHour, 0, 0, DateTimeKind.Utc));
            security.SetMarketPrice(new Tick(algorithm.UtcTime, security.Symbol, 100m, 100m));

            var order = new LimitOrder(security.Symbol, 1, 100m, algorithm.UtcTime);

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.AreEqual(shouldSubmit, canSubmit, message?.Message);

            if (!shouldSubmit)
            {
                Assert.AreEqual("NotSupported", message.Code);
                StringAssert.Contains("Sunday 03:00 to Friday 16:00", message.Message);
            }
        }

        [Test]
        public void CannotSubmitCryptoBuyMarketOrdersWithoutAPrice()
        {
            var security = GetInteractiveBrokersCrypto();
            var order = new MarketOrder(security.Symbol, 1, new DateTime(2024, 1, 3));

            Assert.IsFalse(_interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message));
            Assert.AreEqual("NotSupported", message.Code);
            StringAssert.Contains("needs a known price", message.Message);

            security.SetMarketPrice(new Tick(new DateTime(2024, 1, 3), security.Symbol, 100m, 100m));
            Assert.IsTrue(_interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out message));
            Assert.IsNull(message);
        }

        [TestCase(1, true)]
        [TestCase(0.00000001, true)]
        [TestCase(0.000000001, false)] // below the pair's minimum order size
        public void CanSubmitCryptoOrdersAboveTheMinimumOrderSize(decimal quantity, bool shouldSubmit)
        {
            var security = GetInteractiveBrokersCrypto();
            Assert.AreEqual(0.00000001m, security.SymbolProperties.MinimumOrderSize,
                "unexpected database value, the test needs updating");

            var order = new LimitOrder(security.Symbol, quantity, 100m, new DateTime(2024, 1, 3));

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.AreEqual(shouldSubmit, canSubmit);

            if (shouldSubmit)
            {
                Assert.IsNull(message);
            }
            else
            {
                Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
                Assert.AreEqual("NotSupported", message.Code);
            }
        }

        // IB cancels a crypto BUY limit priced further than 10 dollars or 0.25% from the best ask.
        // Sells are not restricted, a sell limit far above the market rests as usual.
        [TestCase(OrderDirection.Buy, 100000, true)]   // at the ask
        [TestCase(OrderDirection.Buy, 99991, true)]    // within the 250 dollar band
        [TestCase(OrderDirection.Buy, 20000, false)]   // resting far below
        [TestCase(OrderDirection.Sell, 99800, true)]
        [TestCase(OrderDirection.Sell, 500000, true)]  // resting far above, accepted by IB
        public void CanSubmitCryptoLimitOrdersOnlyAtTheMarket(OrderDirection direction, decimal limitPrice, bool shouldSubmit)
        {
            var security = GetInteractiveBrokersCrypto();
            security.SetMarketPrice(new Tick(new DateTime(2024, 1, 3), security.Symbol, 99900m, 100000m));
            // sells would otherwise be rejected as short sales
            security.Holdings.SetHoldings(99900m, 10m);

            var quantity = direction == OrderDirection.Buy ? 1m : -1m;
            var order = new LimitOrder(security.Symbol, quantity, limitPrice, new DateTime(2024, 1, 3));

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.AreEqual(shouldSubmit, canSubmit, message?.Message);

            if (!shouldSubmit)
            {
                StringAssert.Contains("further than", message.Message);
            }
        }

        [TestCase(AccountType.Cash)]
        [TestCase(AccountType.Margin)]
        public void GetsUnleveragedCrypto(AccountType accountType)
        {
            var brokerageModel = new InteractiveBrokersBrokerageModel(accountType);
            Assert.AreEqual(1m, brokerageModel.GetLeverage(GetInteractiveBrokersCrypto()));
        }

        private static Security GetInteractiveBrokersCrypto(string ticker = "BTCUSD")
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
            return algorithm.AddCrypto(ticker);
        }

        [TestCase(AccountType.Cash, 1)]
        [TestCase(AccountType.Margin, 10)]
        public void GetsCorrectLeverageForCfds(AccountType accounType, decimal expectedLeverage)
        {
            var brokerageModel = new InteractiveBrokersBrokerageModel(accounType);
            var security = new Cfd(Symbols.DE10YBEUR,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash("USD", 0, 0),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            Assert.AreEqual(expectedLeverage, brokerageModel.GetLeverage(security));
        }

        [Test]
        public void CanSubmitCfdOrder()
        {
            var security = new Cfd(Symbols.DE10YBEUR,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash("USD", 0, 0),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            var order = new MarketOrder(security.Symbol, 1, new DateTime(2023, 1, 20));

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.IsTrue(canSubmit);
        }

        private static List<Security> GetUnsupportedOptions()
        {
            // Index option
            var spxSymbol = Symbol.Create("SPX", SecurityType.IndexOption, Market.USA);
            var spx = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new SubscriptionDataConfig(typeof(TradeBar), spxSymbol, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false),
                new Cash("USD", 1000, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            //Cash settled option
            var vixSymbol = Symbol.Create("VIX", SecurityType.Option, Market.USA);
            var vix = new Option(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new SubscriptionDataConfig(typeof(TradeBar), vixSymbol, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false),
                new Cash("USD", 1000, 1),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);
            vix.ExerciseSettlement = SettlementType.Cash;

            return new() {spx, vix};
        }

        private static TestCaseData[] GetForexOrderTestCases()
        {
            return new[]
            {
                Tuple.Create("USDCAD", 25000m, 7000000m),
                Tuple.Create("AUDUSD", 25000m, 6000000m),
                Tuple.Create("CADUSD", 25000m, 6000000m),
                Tuple.Create("CHFUSD", 25000m, 6000000m),
                Tuple.Create("CNHUSD", 150000m, 40000000m),
                Tuple.Create("CZKUSD", 0m, 0m), // need market price in USD or EUR -- do later when we support
                Tuple.Create("DKKUSD", 150000m, 35000000m),
                Tuple.Create("EURUSD", 20000m, 6000000m),
                Tuple.Create("GBPUSD", 20000m, 5000000m),
                Tuple.Create("HKDUSD", 200000m, 50000000m),
                Tuple.Create("HUFUSD", 0m, 0m), // need market price in USD or EUR -- do later when we support
                Tuple.Create("ILSUSD", 0m, 0m), // need market price in USD or EUR -- do later when we support
                Tuple.Create("KRWUSD", 0m, 200000000m),
                Tuple.Create("JPYUSD", 2500000m, 550000000m),
                Tuple.Create("MXNUSD", 300000m, 70000000m),
                Tuple.Create("NOKUSD", 150000m, 35000000m),
                Tuple.Create("NZDUSD", 35000m, 8000000m),
                Tuple.Create("PLNUSD", 0m, 0m), // need market price in USD or EUR -- do later when we support
                Tuple.Create("RUBUSD", 750000m, 30000000m),
                Tuple.Create("SEKUSD", 175000m, 40000000m),
                Tuple.Create("SGDUSD", 35000m, 8000000m),
                Tuple.Create("ZARUSD", 350000m, 100000000m),
                Tuple.Create("INRUSD", 0m, 0m) // not in the limits dictionary, should always return false
            }
            .Select(x =>
            {
                var currencyPair = x.Item1;
                Forex.DecomposeCurrencyPair(currencyPair, out var baseCurrency, out var quoteCurrency);
                var forexSymbol = Symbol.Create(currencyPair, SecurityType.Forex, Market.USA);
                var forex = new Forex(
                    forexSymbol,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    new Cash(quoteCurrency, 0, 0.7m),
                    new Cash(baseCurrency, 0, 1),
                    SymbolProperties.GetDefault(quoteCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new ForexCache());

                var min = x.Item2;
                var max = x.Item3;

                if (min != 0m || max != 0)
                {
                    if (min == 0m)
                    {
                        return new[]
                        {
                            // buy
                            new TestCaseData(forex, min, false),
                            new TestCaseData(forex, max * 1.001m, false),
                            new TestCaseData(forex, 0.001m, true),
                            new TestCaseData(forex, max, true),
                            new TestCaseData(forex, max / 2, true),
                            // sell
                            new TestCaseData(forex, -max * 1.001m, false),
                            new TestCaseData(forex, -0.001m, true),
                            new TestCaseData(forex, -max, true),
                            new TestCaseData(forex, -max / 2, true)
                        };
                    }

                    return new[]
                    {
                        // buy
                        new TestCaseData(forex, min * 0.999m, false),
                        new TestCaseData(forex, max * 1.001m, false),
                        new TestCaseData(forex, min, true),
                        new TestCaseData(forex, max, true),
                        new TestCaseData(forex, (min + max) / 2, true),
                        // sell
                        new TestCaseData(forex, -min * 0.999m, false),
                        new TestCaseData(forex, -max * 1.001m, false),
                        new TestCaseData(forex, -min, true),
                        new TestCaseData(forex, -max, true),
                        new TestCaseData(forex, -(min + max) / 2, true)
                    };
                }

                // min and max are 0, need market price in USD or EUR, we don't support yet
                return new[] { new TestCaseData(forex, 100000m, false) };
            })
            .SelectMany(x => x)
            .ToArray();
        }
    }
}
