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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class OptionPriceModelTests
    {
        [Test]
        public void BaroneAdesiWhaleyCallTest()
        {
            const decimal price = 20.00m;
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.15m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2015, 2, 19);

            var equity = new Equity(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            equity.VolatilityModel = new DummyVolatilityModel(underlyingVol);

            var contract = new OptionContract(Symbols.SPY_C_192_Feb19_2016, Symbols.SPY) { Time = evaluationDate };
            var optionCall = new Option(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY_C_192_Feb19_2016, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new OptionSymbolProperties(SymbolProperties.GetDefault(CashBook.AccountCurrency)));
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;

            var priceModel = OptionPriceModels.BaroneAdesiWhaley();
            var results = priceModel.Evaluate(optionCall, null, contract);

            var theoreticalPrice = results.TheoreticalPrice;
            var impliedVolatility = results.ImpliedVolatility;

            Assert.Greater(price, theoreticalPrice);
            Assert.Greater(impliedVolatility, underlyingVol);
        }

        [Test]
        public void BlackScholesPortfolioTest()
        {
            const decimal price = 20.00m;
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.15m;
            const decimal riskFreeRate = 0.01m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2015, 2, 19);
            var SPY_C_192_Feb19_2016E = Symbol.CreateOption("SPY", Market.USA, OptionStyle.European, OptionRight.Call, 192m, new DateTime(2016, 02, 19));

            // setting up underlying
            var equity = new Equity(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            equity.VolatilityModel = new DummyVolatilityModel(underlyingVol);

            // setting up European style option
            var contract = new OptionContract(SPY_C_192_Feb19_2016E, Symbols.SPY) { Time = evaluationDate };
            var optionCall = new Option(SecurityExchangeHours.AlwaysOpen(tz), new SubscriptionDataConfig(typeof(TradeBar), SPY_C_192_Feb19_2016E, Resolution.Minute, tz, tz, true, false, false), new Cash(CashBook.AccountCurrency, 0, 1m), new OptionSymbolProperties(SymbolProperties.GetDefault(CashBook.AccountCurrency)));
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;

            // running evaluation
            var priceModel = OptionPriceModels.BlackScholes();
            var results = priceModel.Evaluate(optionCall, null, contract);
            var theoreticalPrice = results.TheoreticalPrice;
            var greeks = results.Greeks;

            // BS equation
            var rightPart = greeks.Theta + riskFreeRate * underlyingPrice * greeks.Delta + 0.5m * underlyingVol * underlyingVol * underlyingPrice * underlyingPrice * greeks.Gamma;
            var leftPart = riskFreeRate * theoreticalPrice;

            Assert.AreEqual((double)leftPart, (double)rightPart, 0.0001);
        }

        /// <summary>
        /// Dummy implementation of volatility model (for tests only)
        /// </summary>
        class DummyVolatilityModel : IVolatilityModel
        {
            private decimal _volatility;

            public DummyVolatilityModel(decimal volatility)
            {
                _volatility = volatility;
            }
            public decimal Volatility
            {
                get
                {
                    return _volatility;
                }
            }

            public void Update(Security security, BaseData data)
            {
            }
        }
    }
}
