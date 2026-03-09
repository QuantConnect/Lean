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
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities.Equity
{
    [TestFixture]
    public class PriceVariationModelsTests
    {
        [TestCase("SPY", SecurityType.Equity, Market.USA, DataNormalizationMode.Adjusted)]
        [TestCase("SPY", SecurityType.Equity, Market.USA, DataNormalizationMode.SplitAdjusted)]
        [TestCase("EURUSD", SecurityType.Forex, Market.FXCM, DataNormalizationMode.Adjusted)]
        [TestCase("EURUSD", SecurityType.Forex, Market.FXCM, DataNormalizationMode.SplitAdjusted)]
        [TestCase("ZO", SecurityType.Future, Market.CBOT, DataNormalizationMode.Adjusted)]
        public void CheckSecurityMinimumPriceVariation(string ticker, SecurityType securityType, string market, DataNormalizationMode mode)
        {
            var symbol = Symbol.Create(ticker, securityType, market);
            var security = GetSecurity(symbol, mode);
            var expected = security.SymbolProperties.MinimumPriceVariation;
            var adjutedEquity = mode == DataNormalizationMode.Adjusted && securityType == SecurityType.Equity;

            security.SetMarketPrice(new IndicatorDataPoint(symbol, DateTime.Now, 10m));
            var actual = security.PriceVariationModel.GetMinimumPriceVariation(
                new GetMinimumPriceVariationParameters(security, security.Price));
            Assert.AreEqual(adjutedEquity ? 0 : expected, actual);

            security.SetMarketPrice(new IndicatorDataPoint(symbol, DateTime.Now, 1m));
            actual = security.PriceVariationModel.GetMinimumPriceVariation(
                new GetMinimumPriceVariationParameters(security, security.Price));
            Assert.AreEqual(adjutedEquity ? 0 : expected, actual);

            // Special case, if stock price less than $1, minimum price variation is $0.0001
            if (securityType == SecurityType.Equity) expected = 0.0001m;

            security.SetMarketPrice(new IndicatorDataPoint(symbol, DateTime.Now, .99m));
            actual = security.PriceVariationModel.GetMinimumPriceVariation(
                new GetMinimumPriceVariationParameters(security, security.Price));
            Assert.AreEqual(adjutedEquity ? 0 : expected, actual);
        }

        [TestCase("ZO", SecurityType.Future, Market.CBOT, DataNormalizationMode.Adjusted, new float[] { 3.7025f, 3.72f, 3.6875f, 3.6425f, 3.5225f, 3.5125f, 3.47f, 3.46f, 3.445f, 3.4625f, 3.435f, 3.3575f })]
        public void CheckMinimumPriceVariationWithData(string ticker, SecurityType securityType, string market, DataNormalizationMode mode, float[] data)
        {
            var symbol = Symbol.Create(ticker, securityType, market);
            var security = GetSecurity(symbol, mode);
            var minimumPriceVariation = (float)security.SymbolProperties.MinimumPriceVariation;

            var lastPrice = data[0];
            for(var index = 1; index< data.Length; index++)
            {
                Assert.IsTrue(Math.Round(Math.Abs(data[index] - lastPrice) % minimumPriceVariation) == 0);
                lastPrice = data[index];
            }
        }

        [TestCase(0.9, 1.123456789, 0.01)]
        [TestCase(0.9, 0.987654321, 0.0001)]
        [TestCase(0.9, 0.999999999, 0.0001)]
        [TestCase(0.9, 1, 0.01)]
        [TestCase(0.9, 1.000000001, 0.01)]
        [TestCase(1.1, 1.123456789, 0.01)]
        [TestCase(1.1, 0.987654321, 0.0001)]
        [TestCase(1.1, 0.999999999, 0.0001)]
        [TestCase(1.1, 1, 0.01)]
        [TestCase(1.1, 1.000000001, 0.01)]
        public void MinimumPriceVariationChangesWithOrderPrice(decimal securityPrice, decimal orderPrice, decimal expected)
        {
            var symbol = Symbol.Create("YGTY", SecurityType.Equity, Market.USA);
            var security = GetSecurity(symbol, DataNormalizationMode.Raw);

            security.SetMarketPrice(new Tick { Value = securityPrice });

            var actual = security.PriceVariationModel.GetMinimumPriceVariation(
                new GetMinimumPriceVariationParameters(security, orderPrice));
            Assert.AreEqual(expected, actual);
        }

        private Security GetSecurity(Symbol symbol, DataNormalizationMode mode)
        {
            var symbolProperties = SymbolPropertiesDatabase.FromDataFolder()
                .GetSymbolProperties(symbol.ID.Market, symbol, symbol.ID.SecurityType, Currencies.USD);

            Security security;
            if (symbol.ID.SecurityType == SecurityType.Equity)
            {
                security = new QuantConnect.Securities.Equity.Equity(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new SubscriptionDataConfig(
                        typeof(TradeBar),
                        symbol,
                        Resolution.Minute,
                        TimeZones.NewYork,
                        TimeZones.NewYork,
                        true,
                        true,
                        false
                    ),
                    new Cash(Currencies.USD, 0, 1m),
                    symbolProperties,
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                );
            }
            else
            {
                security = new QuantConnect.Securities.Forex.Forex(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new Cash(Currencies.USD, 0, 1m),
                    new Cash(Currencies.EUR, 0, 1m),
                    new SubscriptionDataConfig(
                        typeof(TradeBar),
                        symbol,
                        Resolution.Minute,
                        TimeZones.NewYork,
                        TimeZones.NewYork,
                        true,
                        true,
                        false
                    ),
                    symbolProperties,
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                );
            }

            var TimeKeeper = new TimeKeeper(DateTime.Now.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetDataNormalizationMode(mode);

            return security;
        }
    }
}
