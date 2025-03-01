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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityPortfolioModelTests
    {
        [Test]
        public void LastTradeProfit_FlatToLong()
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            var security = InitializeTest(reference, out portfolio);

            var fillPrice = 100m;
            var fillQuantity = 100;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, security.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // zero since we're from flat
            Assert.AreEqual(0, security.Holdings.LastTradeProfit);
        }

        [Test]
        public void LastTradeProfit_FlatToShort()
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            var security = InitializeTest(reference, out portfolio);

            var fillPrice = 100m;
            var fillQuantity = -100;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, security.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // zero since we're from flat
            Assert.AreEqual(0, security.Holdings.LastTradeProfit);
        }

        [Test]
        public void LastTradeProfit_LongToLonger()
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            var security = InitializeTest(reference, out portfolio);

            security.Holdings.SetHoldings(50m, 100);

            var fillPrice = 100m;
            var fillQuantity = 100;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, security.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // zero since we're from flat
            Assert.AreEqual(0, security.Holdings.LastTradeProfit);
        }

        [Test]
        public void LastTradeProfit_LongToFlat()
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            var security = InitializeTest(reference, out portfolio);

            security.Holdings.SetHoldings(50m, 100);

            var fillPrice = 100m;
            var fillQuantity = -security.Holdings.Quantity;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, security.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // bought @50 and sold @100 = (-50*100)+(100*100 - 1) = 4999
            // current implementation doesn't back out fees.
            Assert.AreEqual(5000m, security.Holdings.LastTradeProfit);
        }

        [Test]
        public void LastTradeProfit_LongToShort()
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            var security = InitializeTest(reference, out portfolio);

            security.Holdings.SetHoldings(50m, 100);

            var fillPrice = 100m;
            var fillQuantity = -2*security.Holdings.Quantity;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, security.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // we can only take 'profit' on the closing part of the position, so we closed 100
            // shares and opened a new for the second 100, so ony the frst 100 go into the calculation
            // bought @50 and sold @100 = (-50*100)+(100*100 - 1) = 4999
            // current implementation doesn't back out fees.
            Assert.AreEqual(5000m, security.Holdings.LastTradeProfit);
        }

        [Test]
        public void LastTradeProfit_ShortToShorter()
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            var security = InitializeTest(reference, out portfolio);

            security.Holdings.SetHoldings(50m, -100);

            var fillPrice = 100m;
            var fillQuantity = -100;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, security.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            Assert.AreEqual(0, security.Holdings.LastTradeProfit);
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void LastTradeProfit_ShortToFlat(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            var security = InitializeTest(reference, out portfolio, accountCurrency);

            security.Holdings.SetHoldings(50m, -100);

            var fillPrice = 100m;
            var fillQuantity = -security.Holdings.Quantity;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, security.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // sold @50 and bought @100 = (50*100)+(-100*100 - 1) = -5001
            // current implementation doesn't back out fees.
            Assert.AreEqual(-5000m, security.Holdings.LastTradeProfit);
        }

        public void LastTradeProfit_ShortToLong()
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            var security = InitializeTest(reference, out portfolio);

            security.Holdings.SetHoldings(50m, -100);

            var fillPrice = 100m;
            var fillQuantity = -2*security.Holdings.Quantity; // flip from -100 to +100
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, security.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // we can only take 'profit' on the closing part of the position, so we closed 100
            // shares and opened a new for the second 100, so ony the frst 100 go into the calculation
            // sold @50 and bought @100 = (50*100)+(-100*100 - 1) = -5001
            // current implementation doesn't back out fees.
            Assert.AreEqual(-5000m, security.Holdings.LastTradeProfit);
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyEquity_LongToFlat(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = new Cash("EUR", 0, 10);
            portfolio.CashBook.Add("EUR", cash);
            var equity = new Security(
                Symbols.AAPL,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            equity.Holdings.SetHoldings(50m, 100);
            portfolio.Securities.Add(equity);

            var fillPrice = 100m;
            var fillQuantity = -equity.Holdings.Quantity;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, equity.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, equity.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            // bought @50 and sold @100 = (-50*100)+(100*100) = 50000 * 10 (conversion rate to account currency)
            Assert.AreEqual(50000m, equity.Holdings.LastTradeProfit);
            // sold @100 = (100*100) = 10000 - 1 fee
            Assert.AreEqual(9999, portfolio.CashBook["EUR"].Amount);
            Assert.AreEqual(0m, equity.Holdings.AveragePrice);
            Assert.AreEqual(0m, equity.Holdings.AbsoluteQuantity);
            Assert.AreEqual(0m, equity.Holdings.AbsoluteHoldingsCost);
            Assert.AreEqual(0m, equity.Holdings.AbsoluteHoldingsValue);
            Assert.AreEqual(0m, equity.Holdings.TotalCloseProfit());
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyEquity_ShortToFlat(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = new Cash("EUR", 0, 10);
            portfolio.CashBook.Add("EUR", cash);
            var equity = new Security(
                Symbols.AAPL,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            equity.Holdings.SetHoldings(50m, -100);
            portfolio.Securities.Add(equity);

            var fillPrice = 100m;
            var fillQuantity = -equity.Holdings.Quantity;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, equity.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, equity.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            // sold @50 and bought @100 = (-50*100)+(100*100) = -50000 * 10 (conversion rate to account currency)
            Assert.AreEqual(-50000m, equity.Holdings.LastTradeProfit);
            // bought @100 = (-100*100) = -10000 - 1 fee
            Assert.AreEqual(-10001, portfolio.CashBook["EUR"].Amount);
            Assert.AreEqual(0m, equity.Holdings.AveragePrice);
            Assert.AreEqual(0m, equity.Holdings.AbsoluteQuantity);
            Assert.AreEqual(0m, equity.Holdings.AbsoluteHoldingsCost);
            Assert.AreEqual(0m, equity.Holdings.AbsoluteHoldingsValue);
            Assert.AreEqual(0m, equity.Holdings.TotalCloseProfit());
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyEquity_FlatToShort(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = new Cash("EUR", 0, 10);
            portfolio.CashBook.Add("EUR", cash);
            var equity = new Security(
                Symbols.AAPL,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            portfolio.Securities.Add(equity);

            var fillPrice = 100m;
            var fillQuantity = -100;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, equity.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, equity.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            Assert.AreEqual(-10, equity.Holdings.NetProfit); // fees
            Assert.AreEqual(0m, equity.Holdings.LastTradeProfit);
            // sold @100 = (100*100) = 10000 - 1 fee
            Assert.AreEqual(9999, portfolio.CashBook["EUR"].Amount);
            Assert.AreEqual(100m, equity.Holdings.AveragePrice);
            Assert.AreEqual(100m, equity.Holdings.AbsoluteQuantity);

            equity.SetMarketPrice(new Tick(DateTime.UtcNow, equity.Symbol, 90, 90));

            // -100 quantity * 100 average price * 10 rate = 100000m
            Assert.AreEqual(100000m, equity.Holdings.AbsoluteHoldingsCost);
            // -100 quantity * 90 current price * 10 rate = 90000m
            Assert.AreEqual(90000m, equity.Holdings.AbsoluteHoldingsValue);
            // (90 average price - 100 current price) * -100 quantity * 10 rate - 1 fee = 9999m
            Assert.AreEqual(9999m, equity.Holdings.TotalCloseProfit());
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyEquity_FlatToLong(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = new Cash("EUR", 0, 10);
            portfolio.CashBook.Add("EUR", cash);
            var equity = new Security(
                Symbols.AAPL,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            portfolio.Securities.Add(equity);

            var fillPrice = 100m;
            var fillQuantity = 100;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, equity.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, equity.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            Assert.AreEqual(-10, equity.Holdings.NetProfit); // fees
            Assert.AreEqual(0m, equity.Holdings.LastTradeProfit);
            // bought @100 = -(100*100) = -10000 - 1 fee
            Assert.AreEqual(-10001, portfolio.CashBook["EUR"].Amount);
            Assert.AreEqual(100m, equity.Holdings.AveragePrice);
            Assert.AreEqual(100m, equity.Holdings.AbsoluteQuantity);

            equity.SetMarketPrice(new Tick(DateTime.UtcNow, equity.Symbol, 110, 110));

            // 100 quantity * 100 average price * 10 rate = 100000m
            Assert.AreEqual(100000m, equity.Holdings.AbsoluteHoldingsCost);
            // 100 quantity * 110 current price * 10 rate = 110000m
            Assert.AreEqual(110000m, equity.Holdings.AbsoluteHoldingsValue);
            // (110 current price - 100 average price) * 100 quantity * 10 rate - 1 fee = 9999m
            Assert.AreEqual(9999m, equity.Holdings.TotalCloseProfit());
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyFuture_LongToFlat(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = new Cash("EUR", 0, 10);
            portfolio.CashBook.Add("EUR", cash);
            var future = new Future(
                Symbols.Fut_SPY_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            future.Holdings.SetHoldings(50m, 100);
            portfolio.Securities.Add(future);

            var fillPrice = 100m;
            var fillQuantity = -future.Holdings.Quantity;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, future.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, future.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            // bought @50 and sold @100 = (-50*100)+(100*100) = 50000 * 10 (conversion rate to account currency)
            Assert.AreEqual(50000m, future.Holdings.LastTradeProfit);
            Assert.AreEqual(49990m, future.Holdings.NetProfit); // LastTradeProfit - fees
            // bought @50 and sold @100 = (-50*100)+(100*100) = 5000 - 1 fee
            Assert.AreEqual(4999, portfolio.CashBook["EUR"].Amount);
            Assert.AreEqual(0m, future.Holdings.AveragePrice);
            Assert.AreEqual(0m, future.Holdings.AbsoluteQuantity);
            Assert.AreEqual(0m, future.Holdings.AbsoluteHoldingsCost);
            Assert.AreEqual(0m, future.Holdings.AbsoluteHoldingsValue);
            Assert.AreEqual(0m, future.Holdings.TotalCloseProfit());
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyFuture_ShortToFlat(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = new Cash("EUR", 0, 10);
            portfolio.CashBook.Add("EUR", cash);
            var future = new Future(
                Symbols.Fut_SPY_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            future.Holdings.SetHoldings(50m, -100);
            portfolio.Securities.Add(future);

            var fillPrice = 100m;
            var fillQuantity = -future.Holdings.Quantity;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, future.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, future.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            // sold @50 and bought @100 = (50*100)+(-100*100) = -50000 * 10 (conversion rate to account currency)
            Assert.AreEqual(-50000m, future.Holdings.LastTradeProfit);
            Assert.AreEqual(-50010m, future.Holdings.NetProfit); // LastTradeProfit - fees
            // sold @50 and bought @100  = (50*100)+(-100*100) = -5000 - 1 fee
            Assert.AreEqual(-5001, portfolio.CashBook["EUR"].Amount);
            Assert.AreEqual(0m, future.Holdings.AveragePrice);
            Assert.AreEqual(0m, future.Holdings.AbsoluteQuantity);
            Assert.AreEqual(0m, future.Holdings.AbsoluteHoldingsCost);
            Assert.AreEqual(0m, future.Holdings.AbsoluteHoldingsValue);
            Assert.AreEqual(0m, future.Holdings.TotalCloseProfit());
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyFuture_FlatToLong(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = new Cash("EUR", 1, 10);
            portfolio.CashBook.Add("EUR", cash);
            var future = new Future(
                Symbols.Fut_SPY_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            portfolio.Securities.Add(future);

            var fillPrice = 100m;
            var fillQuantity = 100;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, future.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, future.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            Assert.AreEqual(0m, future.Holdings.LastTradeProfit);
            Assert.AreEqual(100m, future.Holdings.Quantity);
            Assert.AreEqual(100m, future.Holdings.AveragePrice);
            // had 1 EUR - 1 fee
            Assert.AreEqual(0, portfolio.CashBook["EUR"].Amount);

            // 100 quantity * 100 average price * 10 rate = 100000m
            Assert.AreEqual(100000m, future.Holdings.AbsoluteHoldingsCost);

            future.SetMarketPrice(new Tick(DateTime.UtcNow, future.Symbol, 110, 110));

            // 100 quantity * 110 current price * 10 rate = 110000m
            Assert.AreEqual(110000m, future.Holdings.AbsoluteHoldingsValue);
            // (110 current price - 100 average price) * 100 quantity * 10 rate - 2.15 fee * 100 quantity = 9785m
            Assert.AreEqual(9785m, future.Holdings.TotalCloseProfit());
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyFuture_FlatToShort(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = new Cash("EUR", 1, 10);
            portfolio.CashBook.Add("EUR", cash);
            var future = new Future(
                Symbols.Fut_SPY_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            portfolio.Securities.Add(future);

            var fillPrice = 100m;
            var fillQuantity = -100;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, future.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, future.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            Assert.AreEqual(0m, future.Holdings.LastTradeProfit);
            Assert.AreEqual(-100m, future.Holdings.Quantity);
            Assert.AreEqual(100m, future.Holdings.AveragePrice);
            // had 1 EUR - 1 fee
            Assert.AreEqual(0, portfolio.CashBook["EUR"].Amount);

            // 100 quantity * 100 average price * 10 rate = 100000m
            Assert.AreEqual(100000m, future.Holdings.AbsoluteHoldingsCost);

            future.SetMarketPrice(new Tick(DateTime.UtcNow, future.Symbol, 110, 110));

            // 100 quantity * 110 current price * 10 rate = 110000m
            Assert.AreEqual(110000m, future.Holdings.AbsoluteHoldingsValue);
            // (110 current price - 100 average price) * - 100 quantity * 10 rate - 2.15 fee * 100 quantity = -10215m
            Assert.AreEqual(-10215m, future.Holdings.TotalCloseProfit());
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyCrypto_LongToFlat(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = portfolio.CashBook.Add("EUR", 0, 10);
            var btcCash = portfolio.CashBook.Add("BTC", 0, 1000);
            var crypto = new Crypto(
                Symbols.BTCEUR,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                btcCash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            crypto.Holdings.SetHoldings(50m, 100);
            portfolio.Securities.Add(crypto);

            var fillPrice = 100m;
            var fillQuantity = -crypto.Holdings.Quantity;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, crypto.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, crypto.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            // bought @50 and sold @100 = (-50*100)+(100*100) = 50000 * 10 (conversion rate to account currency)
            Assert.AreEqual(50000m, crypto.Holdings.LastTradeProfit);
            // sold @100 * 100 = 10000 - 1 fee
            Assert.AreEqual(9999, portfolio.CashBook["EUR"].Amount);
            Assert.AreEqual(0m, crypto.Holdings.AveragePrice);
            Assert.AreEqual(0m, crypto.Holdings.AbsoluteQuantity);
        }

        [TestCase("USD")]
        [TestCase("ARG")]
        public void NonAccountCurrencyCrypto_FlatToLong(string accountCurrency)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            SecurityPortfolioManager portfolio;
            InitializeTest(reference, out portfolio, accountCurrency);

            var cash = portfolio.CashBook.Add("EUR", 0, 10);
            var btcCash = portfolio.CashBook.Add("BTC", 0, 1000);
            var crypto = new Crypto(
                Symbols.BTCEUR,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                cash,
                btcCash,
                SymbolProperties.GetDefault("EUR"),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            portfolio.Securities.Add(crypto);

            var fillPrice = 100m;
            var fillQuantity = 100;
            var orderFee = new OrderFee(new CashAmount(1m, "EUR"));
            var orderDirection = fillQuantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
            var fill = new OrderEvent(1, crypto.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            portfolio.ProcessFills(new List<OrderEvent> {fill});

            // current implementation doesn't back out fees.
            Assert.AreEqual(10, crypto.Holdings.TotalFees); // 1 * 10 (conversion rate to account currency)
            Assert.AreEqual(0m, crypto.Holdings.LastTradeProfit);
            Assert.AreEqual(100m, crypto.Holdings.Quantity);
            Assert.AreEqual(100m, crypto.Holdings.AveragePrice);
            // had 0 EUR - 1 fee
            Assert.AreEqual(-10001, portfolio.CashBook["EUR"].Amount);
            Assert.AreEqual(100, portfolio.CashBook["BTC"].Amount);
        }

        [Test]
        public void ITMOptionExerciseWinLossCount(
            [Values(OrderDirection.Buy, OrderDirection.Sell)] OrderDirection orderDirection,
            [Values] bool win)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            var option = InitializeTestWithOption(reference, out var portfolio);
            var underlying = option.Underlying;

            option.SetMarketPrice(new Tick { Value = 100m });

            var underlyingPrice = 0m;
            if (win)
            {
                underlyingPrice = orderDirection == OrderDirection.Buy ? 300m : 290m;
            }
            else
            {
                underlyingPrice = orderDirection == OrderDirection.Buy ? 290m : 300m;
            }
            underlying.SetMarketPrice(new Tick { Value = underlyingPrice });

            var orderProcessor = new FakeOrderProcessor();
            var quantity = orderDirection == OrderDirection.Buy ? 10 : -10;
            var request = new SubmitOrderRequest(OrderType.Market, option.Type, option.Symbol, quantity, 0, 0, reference, "");
            var order = Order.CreateOrder(request);
            order.Id = 1;
            orderProcessor.AddOrder(order);
            portfolio.Transactions.SetOrderProcessor(orderProcessor);

            var fillPrice = 100m;
            var fillQuantity = quantity;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var fill = new OrderEvent(1, option.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee) { Ticket = new OrderTicket(portfolio.Transactions, request) };
            fill.IsInTheMoney = true;
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            Assert.AreEqual(0, portfolio.Transactions.WinCount);
            Assert.AreEqual(0, portfolio.Transactions.LossCount);

            // Now close the option position simulating an assignment on expiration
            fillPrice = 0;
            fillQuantity *= -1;
            var closingOrderDirection = orderDirection == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
            var ticket = new OrderTicket(null, new SubmitOrderRequest(OrderType.OptionExercise, option.Type, option.Symbol, fillQuantity, 0, 0,
                reference, ""));
            fill = new OrderEvent(1, option.Symbol, reference, OrderStatus.Filled, closingOrderDirection, fillPrice, fillQuantity, orderFee)
            {
                IsInTheMoney = true,
                Ticket = ticket,
            };
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            Assert.AreEqual(win ? 1 : 0, portfolio.Transactions.WinCount);
            Assert.AreEqual(win ? 0 : 1, portfolio.Transactions.LossCount);
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void OTMOptionExerciseWinLossCount(OrderDirection orderDirection)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            var option = InitializeTestWithOption(reference, out var portfolio);
            var underlying = option.Underlying;

            option.SetMarketPrice(new Tick { Value = 100m });
            underlying.SetMarketPrice(new Tick { Value = 150m });

            var orderProcessor = new FakeOrderProcessor();
            var quantity = orderDirection == OrderDirection.Buy ? 10 : -10;
            var request = new SubmitOrderRequest(OrderType.Market, option.Type, option.Symbol, quantity, 0, 0, reference, "");
            var order = Order.CreateOrder(request);
            order.Id = 1;
            orderProcessor.AddOrder(order);
            portfolio.Transactions.SetOrderProcessor(orderProcessor);

            var fillPrice = 100m;
            var fillQuantity = quantity;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var fill = new OrderEvent(1, option.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee);
            fill.IsInTheMoney = true;
            fill.Ticket = new OrderTicket(portfolio.Transactions, request);
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            Assert.AreEqual(0, portfolio.Transactions.WinCount);
            Assert.AreEqual(0, portfolio.Transactions.LossCount);

            // Now close the option position simulating an assignment on expiration
            fillPrice = 0;
            fillQuantity *= -1;
            var closingOrderDirection = orderDirection == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
            var ticket = new OrderTicket(null, new SubmitOrderRequest(OrderType.OptionExercise, option.Type, option.Symbol, fillQuantity, 0, 0,
                reference, ""));
            fill = new OrderEvent(1, option.Symbol, reference, OrderStatus.Filled, closingOrderDirection, fillPrice, fillQuantity, orderFee)
            {
                IsInTheMoney = true,
                Ticket = ticket,
            };
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            var expectedWin = orderDirection == OrderDirection.Buy ? false : true;
            Assert.AreEqual(expectedWin ? 1 : 0, portfolio.Transactions.WinCount);
            Assert.AreEqual(expectedWin ? 0 : 1, portfolio.Transactions.LossCount);
        }

        [Test]
        public void OptionPositionCloseWithoutExerciseWinLossCount(
            [Values(OrderDirection.Buy, OrderDirection.Sell)] OrderDirection orderDirection,
            [Values] bool win)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            var option = InitializeTestWithOption(reference, out var portfolio);
            var underlying = option.Underlying;

            var initialOptionPrice = 100m;
            option.SetMarketPrice(new Tick { Value = initialOptionPrice });
            underlying.SetMarketPrice(new Tick { Value = 300m });

            var orderProcessor = new FakeOrderProcessor();
            var quantity = orderDirection == OrderDirection.Buy ? 10 : -10;
            var request = new SubmitOrderRequest(OrderType.Market, option.Type, option.Symbol, quantity, 0, 0, reference, "");
            var order = Order.CreateOrder(request);
            order.Id = 1;
            orderProcessor.AddOrder(order);
            portfolio.Transactions.SetOrderProcessor(orderProcessor);

            var fillPrice = 100m;
            var fillQuantity = quantity;
            var orderFee = new OrderFee(new CashAmount(1m, Currencies.USD));
            var fill = new OrderEvent(1, option.Symbol, reference, OrderStatus.Filled, orderDirection, fillPrice, fillQuantity, orderFee) { Ticket = new OrderTicket(portfolio.Transactions, request) };
            fill.IsInTheMoney = true;
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            Assert.AreEqual(0, portfolio.Transactions.WinCount);
            Assert.AreEqual(0, portfolio.Transactions.LossCount);

            // Before closing, update option market price
            var finalOptionPrice = 0m;
            if (win)
            {
                finalOptionPrice = orderDirection == OrderDirection.Buy ? 150m : 50m;
            }
            else
            {
                finalOptionPrice = orderDirection == OrderDirection.Buy ? 50m : 150m;
            }
            option.SetMarketPrice(new Tick { Value = finalOptionPrice });

            // Now close the option position simulating an assignment on expiration
            fillPrice = finalOptionPrice;
            fillQuantity *= -1;
            var closingOrderDirection = orderDirection == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
            var ticket = new OrderTicket(null, new SubmitOrderRequest(OrderType.Market, option.Type, option.Symbol, fillQuantity, 0, 0,
                reference, ""));
            fill = new OrderEvent(1, option.Symbol, reference, OrderStatus.Filled, closingOrderDirection, fillPrice, fillQuantity, orderFee)
            {
                IsInTheMoney = true,
                Ticket = ticket,
            };
            portfolio.ProcessFills(new List<OrderEvent> { fill });

            Assert.AreEqual(win ? 1 : 0, portfolio.Transactions.WinCount);
            Assert.AreEqual(win ? 0 : 1, portfolio.Transactions.LossCount);
        }

        private Security InitializeTest(DateTime reference,
            out SecurityPortfolioManager portfolio,
            string accountCurrency = "USD")
        {
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                CreateTradeBarConfig(),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick { Value = 100 });
            var timeKeeper = new TimeKeeper(reference);
            var securityManager = new SecurityManager(timeKeeper);
            securityManager.Add(security);
            var transactionManager = new SecurityTransactionManager(null, securityManager);
            portfolio = new SecurityPortfolioManager(securityManager, transactionManager, new AlgorithmSettings());
            portfolio.SetCash(accountCurrency, 100 * 1000m, 1m);
            Assert.AreEqual(0, security.Holdings.Quantity);
            Assert.AreEqual(100*1000m, portfolio.CashBook[accountCurrency].Amount);

            portfolio.SetCash(security.QuoteCurrency.Symbol, 0, 1m);
            return security;
        }

        private Option InitializeTestWithOption(DateTime reference,
            out SecurityPortfolioManager portfolio,
            string accountCurrency = "USD")
        {
            var underlying = InitializeTest(reference, out portfolio, accountCurrency);
            var option = new Option(
                Symbols.SPY_C_192_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                underlying
            );

            portfolio.Securities.Add(option);

            return option;
        }

        private static SubscriptionDataConfig CreateTradeBarConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
