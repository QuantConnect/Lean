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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Engine;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Lean.Engine.TransactionHandlers;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class FutureOptionMarginBuyingPowerModelTests
    {
        [Test]
        public void MarginWithNoFutureOptionHoldings()
        {
            const decimal price = 2300m;
            var time = new DateTime(2020, 10, 14);
            var expDate = new DateTime(2021, 3, 19);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Indices.SP500EMini;
            var future = Symbol.CreateFuture(ticker, Market.CME, expDate);
            var symbol = Symbol.CreateOption(future, Market.CME, OptionStyle.American, OptionRight.Call, 2550m, new DateTime(2021, 3, 19));

            var optionSecurity = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionSecurity.Underlying = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), future, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionSecurity.Underlying.SetMarketPrice(new Tick { Value = price, Time = time });
            optionSecurity.Underlying.Holdings.SetHoldings(1.5m, 1);

            var futureBuyingPowerModel = new FutureMarginModel(security: optionSecurity.Underlying);
            var futureOptionBuyingPowerModel = new FuturesOptionsMarginModel(futureOption: optionSecurity);

            // we don't hold FOPs!
            Assert.AreEqual(0m, futureOptionBuyingPowerModel.GetMaintenanceMargin(optionSecurity));
            Assert.AreNotEqual(0m, futureBuyingPowerModel.GetMaintenanceMargin(optionSecurity.Underlying));

            Assert.AreNotEqual(0m, futureOptionBuyingPowerModel.GetInitialMarginRequirement(optionSecurity, 10));
        }

        [Test]
        public void MarginWithFutureAndFutureOptionHoldings()
        {
            const decimal price = 2300m;
            var time = new DateTime(2020, 10, 14);
            var expDate = new DateTime(2021, 3, 19);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Indices.SP500EMini;
            var future = Symbol.CreateFuture(ticker, Market.CME, expDate);
            var symbol = Symbol.CreateOption(future, Market.CME, OptionStyle.American, OptionRight.Call, 2550m,
                new DateTime(2021, 3, 19));

            var optionSecurity = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionSecurity.Underlying = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), future, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionSecurity.Underlying.SetMarketPrice(new Tick {Value = price, Time = time});
            optionSecurity.Holdings.SetHoldings(1.5m, 1);
            optionSecurity.Underlying.Holdings.SetHoldings(1.5m, 1);

            var futureOptionBuyingPowerModel = new FuturesOptionsMarginModel(futureOption: optionSecurity);

            Assert.AreNotEqual(0m, futureOptionBuyingPowerModel.GetMaintenanceMargin(optionSecurity));
        }

        [Test]
        public void MarginWithFutureOptionHoldings()
        {
            const decimal price = 2300m;
            var time = new DateTime(2020, 10, 14);
            var expDate = new DateTime(2021, 3, 19);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Indices.SP500EMini;
            var future = Symbol.CreateFuture(ticker, Market.CME, expDate);
            var symbol = Symbol.CreateOption(future, Market.CME, OptionStyle.American, OptionRight.Call, 2550m,
                new DateTime(2021, 3, 19));

            var optionSecurity = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionSecurity.Underlying = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), future, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionSecurity.Underlying.SetMarketPrice(new Tick { Value = price, Time = time });
            optionSecurity.Holdings.SetHoldings(1.5m, 1);

            var futureBuyingPowerModel = new FutureMarginModel(security: optionSecurity.Underlying);
            var futureOptionBuyingPowerModel = new FuturesOptionsMarginModel(futureOption: optionSecurity);

            Assert.AreNotEqual(0m, futureOptionBuyingPowerModel.GetMaintenanceMargin(optionSecurity));
            Assert.AreEqual(0, futureBuyingPowerModel.GetMaintenanceMargin(optionSecurity.Underlying));
        }

        [Test]
        public void OptionExerciseWhenFullyInvested()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetFinishedWarmingUp();
            var backtestingTransactionHandler = new BacktestingTransactionHandler();
            using var brokerage = new BacktestingBrokerage(algorithm);
            algorithm.Transactions.SetOrderProcessor(backtestingTransactionHandler);
            backtestingTransactionHandler.Initialize(algorithm, brokerage, new TestResultHandler());

            const decimal price = 2600m;
            var time = new DateTime(2020, 10, 14);
            var expDate = new DateTime(2021, 3, 19);

            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Indices.SP500EMini;
            var future = Symbol.CreateFuture(ticker, Market.CME, expDate);
            var symbol = Symbol.CreateOption(future, Market.CME, OptionStyle.American, OptionRight.Call, 2550m,
                new DateTime(2021, 3, 19));

            var optionSecurity = algorithm.AddOptionContract(symbol);
            optionSecurity.Underlying = algorithm.AddFutureContract(future);

            optionSecurity.Underlying.SetMarketPrice(new Tick { Value = price, Time = time });
            optionSecurity.SetMarketPrice(new Tick { Value = 150, Time = time });
            optionSecurity.Holdings.SetHoldings(1.5m, 10);

            algorithm.SetDateTime(time.AddHours(14)); // 10am
            var ticket = algorithm.ExerciseOption(optionSecurity.Symbol, 10, true);
            Assert.AreEqual(OrderStatus.Filled, ticket.Status);
        }

        [Test]
        public void MarginRequirementsAreSetCorrectly()
        {
            var expDate = new DateTime(2021, 3, 19);
            var tz = TimeZones.NewYork;

            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Indices.SP500EMini;
            var future = Symbol.CreateFuture(ticker, Market.CME, expDate);
            var symbol = Symbol.CreateOption(future, Market.CME, OptionStyle.American, OptionRight.Call, 2550m,
                new DateTime(2021, 3, 19));

            var futureSecurity = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), future, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var optionSecurity = new QuantConnect.Securities.FutureOption.FutureOption(symbol,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                futureSecurity
            );

            var futureMarginModel = new FuturesOptionsMarginModel(futureOption: optionSecurity);
            optionSecurity.Underlying.SetMarketPrice(new Tick { Value = 1500, Time = new DateTime(2001, 01, 07) });

            var initialIntradayMarginRequirement = futureMarginModel.InitialIntradayMarginRequirement;
            var maintenanceIntradayMarginRequirement = futureMarginModel.MaintenanceIntradayMarginRequirement;

            var initialOvernightMarginRequirement = futureMarginModel.MaintenanceOvernightMarginRequirement;
            var maintenanceOvernightMarginRequirement = futureMarginModel.InitialOvernightMarginRequirement;

            Assert.AreNotEqual(0, initialIntradayMarginRequirement);
            Assert.AreNotEqual(0, maintenanceIntradayMarginRequirement);
            Assert.AreNotEqual(0, initialOvernightMarginRequirement);
            Assert.AreNotEqual(0, maintenanceOvernightMarginRequirement);
        }

        // Long Call initial
        [TestCase(10, 70000, OptionRight.Call, PositionSide.Long, 59375)]
        [TestCase(23.5, 69000, OptionRight.Call, PositionSide.Long, 59375)]
        [TestCase(30.5, 68000, OptionRight.Call, PositionSide.Long, 59375)]
        [TestCase(55, 50000, OptionRight.Call, PositionSide.Long, 59375)]
        [TestCase(66, 30000, OptionRight.Call, PositionSide.Long, 59375)]
        [TestCase(72, 17000, OptionRight.Call, PositionSide.Long, 59375)]
        [TestCase(87, 3700, OptionRight.Call, PositionSide.Long, 59375)]
        [TestCase(108.5, 1000, OptionRight.Call, PositionSide.Long, 59375)]
        [TestCase(125, 570, OptionRight.Call, PositionSide.Long, 59375)]
        [TestCase(1000, 0, OptionRight.Call, PositionSide.Long, 59375)]

        // Long Call maintenance
        [TestCase(10, 56000, OptionRight.Call, PositionSide.Long, 47500)]
        [TestCase(23.5, 55000, OptionRight.Call, PositionSide.Long, 47500)]
        [TestCase(30.5, 54000, OptionRight.Call, PositionSide.Long, 47500)]
        [TestCase(55, 40000, OptionRight.Call, PositionSide.Long, 47500)]
        [TestCase(66, 24000, OptionRight.Call, PositionSide.Long, 47500)]
        [TestCase(72, 14000, OptionRight.Call, PositionSide.Long, 47500)]
        [TestCase(87, 3600, OptionRight.Call, PositionSide.Long, 47500)]
        [TestCase(108.5, 1000, OptionRight.Call, PositionSide.Long, 47500)]
        [TestCase(125, 540, OptionRight.Call, PositionSide.Long, 47500)]
        [TestCase(1000, 0, OptionRight.Call, PositionSide.Long, 47500)]

        // Short Call initial
        [TestCase(10, 59400, OptionRight.Call, PositionSide.Short, 59375)]
        [TestCase(23.5, 59680, OptionRight.Call, PositionSide.Short, 59375)]
        [TestCase(30.5, 59750, OptionRight.Call, PositionSide.Short, 59375)]
        [TestCase(55, 56712, OptionRight.Call, PositionSide.Short, 59375)]
        [TestCase(66, 48134, OptionRight.Call, PositionSide.Short, 59375)]
        [TestCase(72, 43492, OptionRight.Call, PositionSide.Short, 59375)]
        [TestCase(87, 28960, OptionRight.Call, PositionSide.Short, 59375)]
        [TestCase(108.5, 11373, OptionRight.Call, PositionSide.Short, 59375)]
        [TestCase(125, 3900, OptionRight.Call, PositionSide.Short, 59375)]
        [TestCase(1000, 0, OptionRight.Call, PositionSide.Short, 59375)]

        // Long Put initial
        [TestCase(10, 45, OptionRight.Put, PositionSide.Long, 59375)]
        [TestCase(18, 171, OptionRight.Put, PositionSide.Long, 59375)]
        [TestCase(26.5, 537, OptionRight.Put, PositionSide.Long, 59375)]
        [TestCase(37.5, 1920, OptionRight.Put, PositionSide.Long, 59375)]
        [TestCase(47.5, 6653, OptionRight.Put, PositionSide.Long, 59375)]
        [TestCase(69.5, 48637, OptionRight.Put, PositionSide.Long, 59375)]
        [TestCase(83, 59201, OptionRight.Put, PositionSide.Long, 59375)]
        [TestCase(108, 60000, OptionRight.Put, PositionSide.Long, 59375)]
        [TestCase(152, 59475, OptionRight.Put, PositionSide.Long, 59375)]

        // Long Put maintenance
        [TestCase(10, 45, OptionRight.Put, PositionSide.Long, 47500)]
        [TestCase(18, 171, OptionRight.Put, PositionSide.Long, 47500)]
        [TestCase(26.5, 537, OptionRight.Put, PositionSide.Long, 47500)]
        [TestCase(37.5, 1920, OptionRight.Put, PositionSide.Long, 47500)]
        [TestCase(47.5, 6653, OptionRight.Put, PositionSide.Long, 47500)]
        [TestCase(69.5, 38910, OptionRight.Put, PositionSide.Long, 47500)]
        [TestCase(83, 47361, OptionRight.Put, PositionSide.Long, 47500)]
        [TestCase(108, 48000, OptionRight.Put, PositionSide.Long, 47500)]
        [TestCase(152, 47580, OptionRight.Put, PositionSide.Long, 47500)]

        // Short Put initial
        [TestCase(10, 23729, OptionRight.Put, PositionSide.Short, 59375)]
        [TestCase(18, 33859, OptionRight.Put, PositionSide.Short, 59375)]
        [TestCase(26.5, 40000, OptionRight.Put, PositionSide.Short, 59375)]
        [TestCase(37.5, 52714, OptionRight.Put, PositionSide.Short, 59375)]
        [TestCase(47.5, 58414, OptionRight.Put, PositionSide.Short, 59375)]
        [TestCase(69.5, 72647, OptionRight.Put, PositionSide.Short, 59375)]
        [TestCase(83, 73160, OptionRight.Put, PositionSide.Short, 59375)]
        [TestCase(108, 71782, OptionRight.Put, PositionSide.Short, 59375)]
        [TestCase(152, 70637, OptionRight.Put, PositionSide.Short, 59375)]
        public void MarginRequirementCrudeOil(decimal strike, double expected, OptionRight optionRight, PositionSide positionSide, decimal underlyingRequirement)
        {
            var tz = TimeZones.NewYork;
            var expDate = new DateTime(2021, 3, 19);
            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Energy.CrudeOilWTI;
            var future = Symbol.CreateFuture(ticker, Market.NYMEX, expDate);
            var symbol = Symbol.CreateOption(future, Market.NYMEX, OptionStyle.American, optionRight, strike,
                new DateTime(2021, 3, 19));

            var futureSecurity = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), future, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var optionSecurity = new QuantConnect.Securities.FutureOption.FutureOption(symbol,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                futureSecurity
            );
            optionSecurity.Underlying.SetMarketPrice(new Tick { Value = 60, Time = new DateTime(2001, 01, 07) });
            var marginRequirement = FuturesOptionsMarginModel.GetMarginRequirement(optionSecurity, underlyingRequirement, positionSide);

            Log.Debug($"Side {positionSide}. Right {optionRight}. Strike {strike}. Margin: {marginRequirement}");
            Assert.AreEqual(expected, marginRequirement, (double)underlyingRequirement * 0.30d);
        }

        // Long Call initial
        [TestCase(1300, 154000, OptionRight.Call, PositionSide.Long, 112729)]
        [TestCase(1755, 97000, OptionRight.Call, PositionSide.Long, 112729)]
        [TestCase(1805, 84000, OptionRight.Call, PositionSide.Long, 112729)]
        [TestCase(1900, 55000, OptionRight.Call, PositionSide.Long, 112729)]
        [TestCase(2040, 24000, OptionRight.Call, PositionSide.Long, 112729)]
        [TestCase(2100, 16000, OptionRight.Call, PositionSide.Long, 112729)]
        [TestCase(2295, 5000, OptionRight.Call, PositionSide.Long, 112729)]
        [TestCase(3000, 740, OptionRight.Call, PositionSide.Long, 112729)]
        [TestCase(4000, 180, OptionRight.Call, PositionSide.Long, 112729)]
        public void MarginRequirementGold(decimal strike, double expected, OptionRight optionRight, PositionSide positionSide, decimal underlyingRequirement)
        {
            var tz = TimeZones.NewYork;
            var expDate = new DateTime(2021, 3, 19);
            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Metals.Gold;
            var future = Symbol.CreateFuture(ticker, Market.COMEX, expDate);
            var symbol = Symbol.CreateOption(future, Market.COMEX, OptionStyle.American, optionRight, strike,
                new DateTime(2021, 3, 19));

            var futureSecurity = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), future, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var optionSecurity = new QuantConnect.Securities.FutureOption.FutureOption(symbol,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                futureSecurity
            );
            optionSecurity.Underlying.SetMarketPrice(new Tick { Value = 1887, Time = new DateTime(2001, 01, 07) });
            var marginRequirement = FuturesOptionsMarginModel.GetMarginRequirement(optionSecurity, underlyingRequirement, positionSide);

            Log.Debug($"Side {positionSide}. Right {optionRight}. Strike {strike}. Margin: {marginRequirement}");
            Assert.AreEqual(expected, marginRequirement, (double)underlyingRequirement * 0.30d);
        }

        // Long Call initial
        [TestCase(2200, 16456, OptionRight.Call, PositionSide.Long, 15632)]
        [TestCase(3200, 15582, OptionRight.Call, PositionSide.Long, 15632)]
        [TestCase(3500, 14775, OptionRight.Call, PositionSide.Long, 15632)]
        [TestCase(3570, 14310, OptionRight.Call, PositionSide.Long, 15632)]
        [TestCase(4190, 7128, OptionRight.Call, PositionSide.Long, 15632)]
        [TestCase(4370, 4089, OptionRight.Call, PositionSide.Long, 15632)]
        [TestCase(4900, 233, OptionRight.Call, PositionSide.Long, 15632)]

        // Short Call initial
        [TestCase(2200, 17069, OptionRight.Call, PositionSide.Short, 15632)]
        [TestCase(3200, 16716, OptionRight.Call, PositionSide.Short, 15632)]
        [TestCase(3500, 16409, OptionRight.Call, PositionSide.Short, 15632)]
        [TestCase(3570, 16222, OptionRight.Call, PositionSide.Short, 15632)]
        [TestCase(4190, 14429, OptionRight.Call, PositionSide.Short, 15632)]
        [TestCase(4370, 13003, OptionRight.Call, PositionSide.Short, 15632)]
        [TestCase(4900, 6528, OptionRight.Call, PositionSide.Short, 15632)]
        public void MarginRequirementEs(decimal strike, double expected, OptionRight optionRight, PositionSide positionSide, decimal underlyingRequirement)
        {
            var tz = TimeZones.NewYork;
            var expDate = new DateTime(2021, 3, 19);
            // For this symbol we dont have any history, but only one date and margins line
            var ticker = QuantConnect.Securities.Futures.Indices.SP500EMini;
            var future = Symbol.CreateFuture(ticker, Market.Globex, expDate);
            var symbol = Symbol.CreateOption(future, Market.Globex, OptionStyle.American, optionRight, strike,
                new DateTime(2021, 3, 19));

            var futureSecurity = new Future(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), future, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var optionSecurity = new QuantConnect.Securities.FutureOption.FutureOption(symbol,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                futureSecurity
            );
            optionSecurity.Underlying.SetMarketPrice(new Tick { Value = 4172, Time = new DateTime(2001, 01, 07) });
            var marginRequirement = FuturesOptionsMarginModel.GetMarginRequirement(optionSecurity, underlyingRequirement, positionSide);

            Log.Debug($"Side {positionSide}. Right {optionRight}. Strike {strike}. Margin: {marginRequirement}");
            Assert.AreEqual(expected, marginRequirement, (double)underlyingRequirement * 0.30d);
        }
    }
}
