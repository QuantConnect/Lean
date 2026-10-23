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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class EtoroFeeModelTests
    {
        private readonly EtoroFeeModel _feeModel = new();

        // Stocks and ETFs are commission free: "We charge zero commissions when you buy or sell a stock."
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(-5)]
        public void GetOrderFeeEquityReturnsZero(decimal quantity)
        {
            var security = CreateSecurity(Symbol.Create("AAPL", SecurityType.Equity, Market.USA), 200m);
            var order = new MarketOrder(security.Symbol, quantity, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(0m));
        }

        // Crypto pays 1% of the trade value (price * quantity) on every buy and sell.
        // The live BTC market buy of 85.75 USD was booked at 0.86 USD: 0.8575 rounded to the cent.
        [TestCase(85.75, 1, 0.8575)]
        [TestCase(100, 2, 2)]
        [TestCase(50, -3, 1.5)]
        public void GetOrderFeeCryptoReturnsPercentOfTradeValue(decimal price, decimal quantity, decimal expectedFee)
        {
            var security = CreateSecurity(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase), price);
            var order = new MarketOrder(security.Symbol, quantity, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(expectedFee));
            Assert.That(fee.Value.Currency, Is.EqualTo(Currencies.USD));
        }

        private static Security CreateSecurity(Symbol symbol, decimal price)
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute,
                TimeZones.Utc, TimeZones.Utc, false, true, false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            security.SetMarketPrice(new Tick(DateTime.UtcNow, symbol, price, price));
            return security;
        }
    }
}
