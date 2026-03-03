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

using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Common;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class IndicatorBasedOptionPriceModelTests
    {
        [TestCase(true, 6.05392693521696, 0.3559978, 0.7560627, 0.0430897, 0.0663327, -1599.430292, 0.0000904)]
        [TestCase(false, 5.05414551764534, 0.1427122, 0.957485, 0.0311303, 0.020584, -163.902082, 0.0000057)]
        public void WorksWithAndWithoutMirrorContract([Values] bool withMirrorContract, decimal expectedTheoreticalPrice,
            decimal expectedIv, decimal expectedDelta, decimal expectedGamma, decimal expectedVega,
            decimal expectedTheta, decimal expectedRho)
        {
            GetTestData(true, true, withMirrorContract, out var option, out var contract, out var securities);

            var model = new IndicatorBasedOptionPriceModel(securityProvider: securities);

            var result = model.Evaluate(new OptionPriceModelParameters(option, null, contract));
            var theoreticalPrice = result.TheoreticalPrice;
            var iv = result.ImpliedVolatility;
            var greeks = result.Greeks;

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expectedTheoreticalPrice, theoreticalPrice);
                Assert.AreEqual(expectedIv, iv);
                Assert.AreEqual(expectedDelta, greeks.Delta);
                Assert.AreEqual(expectedGamma, greeks.Gamma);
                Assert.AreEqual(expectedVega, greeks.Vega);
                Assert.AreEqual(expectedTheta, greeks.Theta);
                Assert.AreEqual(expectedRho, greeks.Rho);
            });
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void WontCalculateIfMissindData(bool withUnderlyingData, bool withOptionData)
        {
            GetTestData(withUnderlyingData, withOptionData, true, out var option, out var contract, out var securities);
            
            var model = new IndicatorBasedOptionPriceModel(securityProvider: securities);
            var result = model.Evaluate(new OptionPriceModelParameters(option, null, contract));

            Assert.AreEqual(OptionPriceModelResult.None, result);
        }

        private static void GetTestData(bool withUnderlying, bool withOption, bool withMirrorOption,
            out Option option, out OptionContract contract, out SecurityManager securities)
        {
            var underlyingSymbol = Symbols.GOOG;
            var date = new DateTime(2015, 11, 24);
            var contractSymbol = Symbols.CreateOptionSymbol(underlyingSymbol.Value, OptionRight.Call, 745m, date);

            var tz = TimeZones.NewYork;
            var underlyingPrice = 750m;
            var underlyingVolume = 10000;
            var contractPrice = 5.05m;
            var mirrorContractPrice = 1.05m;
            var underlying = OptionPriceModelTests.GetEquity(underlyingSymbol, 0m, underlyingVolume, tz);
            option = OptionPriceModelTests.GetOption(contractSymbol, underlying, tz);
            contract = OptionPriceModelTests.GetOptionContract(contractSymbol, underlyingSymbol, date);

            var time = date.Add(new TimeSpan(9, 31, 0));
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(tz));
            securities = new SecurityManager(timeKeeper);

            if (withUnderlying)
            {
                var underlyingData = new Tick { Symbol = underlying.Symbol, Time = time, Value = underlyingPrice, Quantity = underlyingVolume, TickType = TickType.Trade };
                underlying.SetMarketPrice(underlyingData);
                securities.Add(underlying);
            }

            if (withOption)
            {
                var contractData = new Tick { Symbol = contractSymbol, Time = time, Value = contractPrice, Quantity = 10, TickType = TickType.Trade };
                option.SetMarketPrice(contractData);
                securities.Add(option);
            }

            if (withMirrorOption)
            {
                var mirrorContractSymbol = Symbol.CreateOption(contractSymbol.Underlying,
                    contractSymbol.ID.Symbol,
                    contractSymbol.ID.Market,
                    contractSymbol.ID.OptionStyle,
                    contractSymbol.ID.OptionRight == OptionRight.Call ? OptionRight.Put : OptionRight.Call,
                    contractSymbol.ID.StrikePrice,
                    contractSymbol.ID.Date);
                var mirrorContractData = new Tick { Symbol = mirrorContractSymbol, Time = time, Value = mirrorContractPrice, Quantity = 10, TickType = TickType.Trade };
                var mirrorOption = OptionPriceModelTests.GetOption(mirrorContractSymbol, underlying, tz);
                mirrorOption.SetMarketPrice(mirrorContractData);
                securities.Add(mirrorOption);
            }
        }
    }
}
