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
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Tests.Common.Securities.Forex
{
    [TestFixture]
    public class ForexHoldingTests
    {
        [TestCase("EURUSD", 1, 0.00001, 1000, 1.23456, 50, 10000)]
        [TestCase("USDJPY", 0.9, 0.001, 1000, 100.30, -40, 10000)]
        [TestCase("EURGBP", 1.1, 0.00001, 1000, 0.89012, 100, 10000)]
        public void TotalProfitIsCorrectlyEstimated(string ticker, decimal conversionRate,
                                                    decimal minimumPriceVariation,
                                                    int lotSize, decimal entryPrice, decimal pips, int entryQuantity)
        {
            // Arrange
            var timeKeeper = new TimeKeeper(DateTime.Now, TimeZones.NewYork);

            var symbol = Symbol.Create(ticker, SecurityType.Forex, Market.FXCM);
            var pairQuoteCurrency = symbol.Value.Substring(startIndex: 3);
            var cash = new Cash(pairQuoteCurrency,
                amount: 100000,
                conversionRate: conversionRate);
            var subscription = new SubscriptionDataConfig(typeof(QuoteBar), symbol, Resolution.Daily,
                                                          TimeZones.NewYork, TimeZones.NewYork, fillForward: true,
                                                          extendedHours: true, isInternalFeed: true);

            var pair = new QuantConnect.Securities.Forex.Forex(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                cash,
                subscription,
                new SymbolProperties(
                    "",
                    pairQuoteCurrency,
                    1,
                    minimumPriceVariation,
                    lotSize
                ),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            pair.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            pair.SetFeeModel(new ConstantFeeModel(decimal.Zero));
            var forexHolding = new ForexHolding(pair, new IdentityCurrencyConverter(Currencies.USD));
            // Act
            forexHolding.SetHoldings(entryPrice, entryQuantity);
            var priceVariation = pips * 10 * minimumPriceVariation;
            forexHolding.UpdateMarketPrice(entryPrice + priceVariation);
            pair.SetMarketPrice(new Tick(DateTime.Now, pair.Symbol, forexHolding.Price, forexHolding.Price));
            var actualPips = forexHolding.TotalCloseProfitPips();
            // Assert
            Assert.AreEqual(pips, actualPips);
        }
    }
}