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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Common;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class IndicatorBasedOptionPriceModelTests
    {
        [TestCase(true, 6.05391914652262, 0.3564563, 0.7560627, 0.0430897, 0.0662474, -4.3932945, 0.0000902)]
        [TestCase(false, 5.05413609164657, 0.1428964, 0.9574846, 0.0311305, 0.0205564, -0.4502054, 0.0000057)]
        public void WorksWithAndWithoutMirrorContract([Values] bool withMirrorContract, decimal expectedTheoreticalPrice,
            decimal expectedIv, decimal expectedDelta, decimal expectedGamma, decimal expectedVega,
            decimal expectedTheta, decimal expectedRho)
        {
            GetTestData(true, true, withMirrorContract, out var option, out var contract, out var slice);

            var model = new IndicatorBasedOptionPriceModel();

            var result = model.Evaluate(option, slice, contract);
            var theoreticalPrice = result.TheoreticalPrice;
            var iv = result.ImpliedVolatility;
            var greeks = result.Greeks;

            Assert.AreEqual(expectedTheoreticalPrice, theoreticalPrice);
            Assert.AreEqual(expectedIv, iv);
            Assert.AreEqual(expectedDelta, greeks.Delta);
            Assert.AreEqual(expectedGamma, greeks.Gamma);
            Assert.AreEqual(expectedVega, greeks.Vega);
            Assert.AreEqual(expectedTheta, greeks.Theta);
            Assert.AreEqual(expectedRho, greeks.Rho);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void WontCalculateIfMissindData(bool withUnderlyingData, bool withOptionData)
        {
            GetTestData(withUnderlyingData, withOptionData, true, out var option, out var contract, out var slice);
            
            var model = new IndicatorBasedOptionPriceModel();
            var result = model.Evaluate(option, slice, contract);

            Assert.AreEqual(OptionPriceModelResult.None, result);
        }

        private static void GetTestData(bool withUnderlying, bool withOption, bool withMirrorOption,
            out Option option, out OptionContract contract, out Slice slice)
        {
            var underlyingSymbol = Symbols.GOOG;
            var date = new DateTime(2015, 11, 24);
            var contractSymbol = Symbols.CreateOptionSymbol(underlyingSymbol.Value, OptionRight.Call, 745m, date);

            var tz = TimeZones.NewYork;
            var underlyingPrice = 750m;
            var underlyingVolume = 10000;
            var contractPrice = 5m;
            var underlying = OptionPriceModelTests.GetEquity(underlyingSymbol, underlyingPrice, underlyingVolume, tz);
            option = OptionPriceModelTests.GetOption(contractSymbol, underlying, tz);
            contract = OptionPriceModelTests.GetOptionContract(contractSymbol, underlyingSymbol, date);

            var time = date.Add(new TimeSpan(9, 31, 0));

            var data = new List<BaseData>();

            if (withUnderlying)
            {
                var underlyingData = new TradeBar(time, underlyingSymbol, underlyingPrice, underlyingPrice, underlyingPrice, underlyingPrice, underlyingVolume, TimeSpan.FromMinutes(1));
                data.Add(underlyingData);
                underlying.SetMarketPrice(underlyingData);
            }

            if (withOption)
            {
                var contractData = new QuoteBar(time,
                    contractSymbol,
                    new Bar(contractPrice, contractPrice, contractPrice, contractPrice),
                    10,
                    new Bar(contractPrice + 0.1m, contractPrice + 0.1m, contractPrice + 0.1m, contractPrice + 0.1m),
                    10,
                    TimeSpan.FromMinutes(1));
                data.Add(contractData);
                option.SetMarketPrice(contractData);
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
                var mirrorContractPrice = 1m;
                data.Add(new QuoteBar(time,
                    mirrorContractSymbol,
                    new Bar(mirrorContractPrice, mirrorContractPrice, mirrorContractPrice, mirrorContractPrice),
                    10,
                    new Bar(mirrorContractPrice + 0.1m, mirrorContractPrice + 0.1m, mirrorContractPrice + 0.1m, mirrorContractPrice + 0.1m),
                    10,
                    TimeSpan.FromMinutes(1)));
            }

            slice = new Slice(time, data, time.ConvertToUtc(tz));
        }
    }
}
