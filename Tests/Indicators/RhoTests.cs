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
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class RhoTests : OptionBaseIndicatorTests<Rho>
    {
        protected override IndicatorBase<IBaseData> CreateIndicator()
            => new Rho("testRhoIndicator", _symbol, 0.0403m, 0.0m);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new Rho("testRhoIndicator", _symbol, riskFreeRateModel);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
        {
            var symbol = (SymbolList.Count > 0) ? SymbolList[0] : _symbol;
            return new Rho("testRhoIndicator", symbol, riskFreeRateModel, dividendYieldModel);
        }

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
            => algorithm.R(_symbol);

        [SetUp]
        public void SetUp()
        {
            // 2 updates per iteration, 1 for greek, 1 for IV
            RiskFreeRateUpdatesPerIteration = 2;
            DividendYieldUpdatesPerIteration = 2;
        }

        // No Rho data available from IB

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.3628, OptionStyle.European)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.3885, OptionStyle.European)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.4761, OptionStyle.European)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.2119, OptionStyle.European)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.1652, OptionStyle.European)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.4498, OptionStyle.European)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 1.2862, OptionStyle.European)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -1.0337, OptionStyle.European)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 1.5558, OptionStyle.European)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.1235, OptionStyle.European)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.5326, OptionStyle.European)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -1.3178, OptionStyle.European)]
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.3628, OptionStyle.American)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.3884, OptionStyle.American)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.4761, OptionStyle.American)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.2119, OptionStyle.American)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.1648, OptionStyle.American)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.4498, OptionStyle.American)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 1.2861, OptionStyle.American)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -1.0336, OptionStyle.American)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 1.5558, OptionStyle.American)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.1230, OptionStyle.American)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.5306, OptionStyle.American)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -1.3180, OptionStyle.American)]
        public void ComparesAgainstExternalData2(decimal price, decimal spotPrice, OptionRight right, int expiry, double refRho, OptionStyle style)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, style, right, 450m, _reference.AddDays(expiry));
            var model = style == OptionStyle.European ? OptionPricingModelType.BlackScholes : OptionPricingModelType.BinomialCoxRossRubinstein;
            var indicator = new Rho(symbol, 0.053m, 0.0153m, optionModel: model, ivModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refRho, (double)indicator.Current.Value, 0.017d);
        }

        [TestCase(0.5, 470.0, OptionRight.Put, 0)]
        [TestCase(0.5, 470.0, OptionRight.Put, 5)]
        [TestCase(0.5, 470.0, OptionRight.Put, 10)]
        [TestCase(0.5, 470.0, OptionRight.Put, 15)]
        [TestCase(15.0, 450.0, OptionRight.Call, 0)]
        [TestCase(15.0, 450.0, OptionRight.Call, 5)]
        [TestCase(15.0, 450.0, OptionRight.Call, 10)]
        [TestCase(0.5, 450.0, OptionRight.Call, 15)]
        public void CanComputeOnExpirationDate(decimal price, decimal spotPrice, OptionRight right, int hoursAfterExpiryDate)
        {
            var expiration = new DateTime(2024, 12, 6);
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, expiration);
            var indicator = new Rho(symbol, 0.053m, 0.0153m,
                optionModel: OptionPricingModelType.BinomialCoxRossRubinstein, ivModel: OptionPricingModelType.BlackScholes);

            var currentTime = expiration.AddHours(hoursAfterExpiryDate);

            var optionDataPoint = new IndicatorDataPoint(symbol, currentTime, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, currentTime, spotPrice);

            Assert.IsFalse(indicator.Update(optionDataPoint));
            Assert.IsTrue(indicator.Update(spotDataPoint));

            Assert.AreNotEqual(0, indicator.Current.Value);
        }
    }
}
