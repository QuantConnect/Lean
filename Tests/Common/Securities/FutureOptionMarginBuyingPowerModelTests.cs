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
    [TestFixture]
    public class FutureOptionMarginBuyingPowerModelTests
    {
        [Test]
        public void MarginWithNoFutureOptionHoldings()
        {
            const decimal price = 1.2345m;
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
            Assert.AreEqual(
                futureBuyingPowerModel.GetInitialMarginRequirement(optionSecurity.Underlying, 10) *
                FuturesOptionsMarginModel.FixedMarginMultiplier,
                futureOptionBuyingPowerModel.GetInitialMarginRequirement(optionSecurity, 10));
        }

        [Test]
        public void MarginWithFutureAndFutureOptionHoldings()
        {
            const decimal price = 1.2345m;
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

            var futureBuyingPowerModel = new FutureMarginModel(security: optionSecurity.Underlying);
            var futureOptionBuyingPowerModel = new FuturesOptionsMarginModel(futureOption: optionSecurity);

            Assert.AreNotEqual(0m, futureOptionBuyingPowerModel.GetMaintenanceMargin(optionSecurity));
            Assert.AreEqual(
                futureBuyingPowerModel.GetMaintenanceMargin(optionSecurity.Underlying) *
                FuturesOptionsMarginModel.FixedMarginMultiplier,
                futureOptionBuyingPowerModel.GetMaintenanceMargin(optionSecurity));
        }

        [Test]
        public void MarginWithFutureOptionHoldings()
        {
            const decimal price = 1.2345m;
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
        public void OptionExersiceWhenFullyInvested()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetFinishedWarmingUp();
            var backtestingTransactionHandler = new BacktestingTransactionHandler();
            algorithm.Transactions.SetOrderProcessor(backtestingTransactionHandler);
            backtestingTransactionHandler.Initialize(algorithm, new BacktestingBrokerage(algorithm), new TestResultHandler(packet => { }));

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

            var request = optionSecurity.CreateDelistedSecurityOrderRequest(algorithm.UtcTime);

            var ticket = algorithm.Transactions.AddOrder(request);

            Assert.AreEqual(OrderStatus.Filled, ticket.Status);
        }
    }
}
