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
using System.IO;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ImpliedVolatilityTests : OptionBaseIndicatorTests<ImpliedVolatility>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
           => new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, 0.0530m, 0.0153m);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, riskFreeRateModel);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
            => new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, riskFreeRateModel, dividendYieldModel);

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
            => algorithm.IV(_symbol);

        [SetUp]
        public void SetUp()
        {
            RiskFreeRateUpdatesPerIteration = 1;
            DividendYieldUpdatesPerIteration = 1;
        }

        // For comparing IB's value
        [TestCase("SPX230811C04300000", 0.2)]
        [TestCase("SPX230811C04500000")]
        [TestCase("SPX230811C04700000")]
        [TestCase("SPX230811P04300000", 0.015)]
        [TestCase("SPX230811P04500000")]
        [TestCase("SPX230811P04700000", 0.075)]
        [TestCase("SPX230901C04300000")]
        [TestCase("SPX230901C04500000")]
        [TestCase("SPX230901C04700000")]
        [TestCase("SPX230901P04300000")]
        [TestCase("SPX230901P04500000")]
        [TestCase("SPX230901P04700000")]
        [TestCase("SPY230811C00430000", 0.055)]
        [TestCase("SPY230811C00450000", 0.015)]
        [TestCase("SPY230811C00470000")]
        [TestCase("SPY230811P00430000", 0.015)]
        [TestCase("SPY230811P00450000")]
        [TestCase("SPY230811P00470000", 0.075)]
        [TestCase("SPY230901C00430000", 0.025)]
        [TestCase("SPY230901C00450000")]
        [TestCase("SPY230901C00470000")]
        [TestCase("SPY230901P00430000")]
        [TestCase("SPY230901P00450000")]
        [TestCase("SPY230901P00470000", 0.035)]
        public void ComparesAgainstExternalData(string fileName, double errorMargin = 0.01, int column = 2)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new ImpliedVolatility(symbol, 0.0530m, 0.0153m);
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin, column);
        }

        // For comparing IB's value
        [TestCase("SPX230811C04300000", 0.2)]
        [TestCase("SPX230811C04500000")]
        [TestCase("SPX230811C04700000")]
        [TestCase("SPX230811P04300000", 0.015)]
        [TestCase("SPX230811P04500000")]
        [TestCase("SPX230811P04700000", 0.075)]
        [TestCase("SPX230901C04300000")]
        [TestCase("SPX230901C04500000")]
        [TestCase("SPX230901C04700000")]
        [TestCase("SPX230901P04300000")]
        [TestCase("SPX230901P04500000")]
        [TestCase("SPX230901P04700000")]
        [TestCase("SPY230811C00430000", 0.055)]
        [TestCase("SPY230811C00450000", 0.015)]
        [TestCase("SPY230811C00470000")]
        [TestCase("SPY230811P00430000", 0.015)]
        [TestCase("SPY230811P00450000")]
        [TestCase("SPY230811P00470000", 0.075)]
        [TestCase("SPY230901C00430000", 0.025)]
        [TestCase("SPY230901C00450000")]
        [TestCase("SPY230901C00470000")]
        [TestCase("SPY230901P00430000")]
        [TestCase("SPY230901P00450000")]
        [TestCase("SPY230901P00470000", 0.035)]
        public void ComparesAgainstExternalDataAfterReset(string fileName, double errorMargin = 0.01, int column = 2)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new ImpliedVolatility(symbol, 0.0530m, 0.0153m);
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin, column);

            indicator.Reset();
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin, column);
        }

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.309)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, 0.515)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.279)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, 0.205)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.133)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, 0.545)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.097)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, 0.207)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.140)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, 0.055)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.057)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, 0.177)]
        public void ComparesIVOnBSMModel(decimal price, decimal spotPrice, OptionRight right, int expiry, double refIV)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new ImpliedVolatility(symbol, 0.0530m, 0.0153m);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refIV, (double)indicator.Current.Value, 0.001d);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var period = 5;
            var indicator = new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, 0.0530m, 0.0153m, period: period);
            var warmUpPeriod = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!warmUpPeriod.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            // warmup period is 5 + 1
            for (var i = 1; i <= warmUpPeriod.Value; i++)
            {
                var time = _reference.AddDays(i);
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                indicator.Update(new IndicatorDataPoint(_symbol, time, optionPrice));

                Assert.IsFalse(indicator.IsReady);

                indicator.Update(new IndicatorDataPoint(_underlying, time, price));

                // At least 2 days data for historical daily volatility
                if (time <= _reference.AddDays(3))
                {
                    Assert.IsFalse(indicator.IsReady);
                }
                else
                {
                    Assert.IsTrue(indicator.IsReady);
                }
            }

            Assert.AreEqual(2 * warmUpPeriod.Value, indicator.Samples);
        }
    }
}
