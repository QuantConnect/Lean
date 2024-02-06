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

using System.IO;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class DeltaTests : OptionBaseIndicatorTests<Delta>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator() 
            => new Delta("testDeltaIndicator", _symbol, 0.0530m, 0.0153m);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new Delta("testDeltaIndicator", _symbol, riskFreeRateModel);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
            => new Delta("testDeltaIndicator", _symbol, riskFreeRateModel, dividendYieldModel);

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm) 
            => algorithm.D(_symbol);

        [SetUp]
        public void SetUp()
        {
            // 3 updates per iteration, 1 for greek, 2 for IV
            RiskFreeRateUpdatesPerIteration = 3;
            DividendYieldUpdatesPerIteration = 3;
        }

        [TestCase("SPX230811C04300000", 0.021)]
        [TestCase("SPX230811C04500000", 0.009)]
        [TestCase("SPX230811C04700000")]
        [TestCase("SPX230811P04300000")]
        [TestCase("SPX230811P04500000")]
        [TestCase("SPX230811P04700000", 0.030)]
        [TestCase("SPX230901C04300000", 0.011)]
        [TestCase("SPX230901C04500000", 0.006)]
        [TestCase("SPX230901C04700000")]
        [TestCase("SPX230901P04300000")]
        [TestCase("SPX230901P04500000")]
        [TestCase("SPX230901P04700000", 0.018)]
        public void ComparesAgainstExternalData(string fileName, double errorMargin = 0.005, int column = 3)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new Delta(symbol, 0.0530m, 0.0153m);
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin, column);
        }

        [TestCase("SPY230811C00430000", 0.028)]
        [TestCase("SPY230811C00450000", 0.014)]
        [TestCase("SPY230811C00470000")]
        [TestCase("SPY230811P00430000")]
        [TestCase("SPY230811P00450000", 0.006)]
        [TestCase("SPY230811P00470000", 0.052)]
        [TestCase("SPY230901C00430000", 0.033)]
        [TestCase("SPY230901C00450000", 0.020)]
        [TestCase("SPY230901C00470000")]
        [TestCase("SPY230901P00430000")]
        [TestCase("SPY230901P00450000", 0.009)]
        [TestCase("SPY230901P00470000", 0.123)]
        public void ComparesAgainstExternalDataAmericanOption(string fileName, double errorMargin = 0.005, int column = 3)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new Delta(symbol, 0.0530m, 0.0153m, optionModel: OptionPricingModelType.BinomialCoxRossRubinstein, 
                ivModel: OptionPricingModelType.BlackScholes);
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin, column);
        }

        [TestCase("SPX230811C04300000", 0.021)]
        [TestCase("SPX230811C04500000", 0.009)]
        [TestCase("SPX230811C04700000")]
        [TestCase("SPX230811P04300000")]
        [TestCase("SPX230811P04500000")]
        [TestCase("SPX230811P04700000", 0.030)]
        [TestCase("SPX230901C04300000", 0.011)]
        [TestCase("SPX230901C04500000", 0.006)]
        [TestCase("SPX230901C04700000")]
        [TestCase("SPX230901P04300000")]
        [TestCase("SPX230901P04500000")]
        [TestCase("SPX230901P04700000", 0.018)]
        public void ComparesAgainstExternalDataAfterReset(string fileName, double errorMargin = 0.005, int column = 3)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new Delta(symbol, 0.0530m, 0.0153m);
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin, column);

            indicator.Reset();
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin, column);
        }

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.5433)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.4456)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.6884)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.2606)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.2412)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.5254)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.6163)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.4174)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.7461)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.0524)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.2573)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.5569)]
        public void ComparesDeltaOnBSMModel(decimal price, decimal spotPrice, OptionRight right, int expiry, double refDelta)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new Delta(symbol, 0.0530m, 0.0153m, optionModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refDelta, (double)indicator.Current.Value, 0.0005d);
        }

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.5432)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.4494)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.6884)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.2650)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.2419)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.5301)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.6162)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.4392)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.7461)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.0606)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.2579)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.6020)]
        public void ComparesDeltaOnCRRModel(decimal price, decimal spotPrice, OptionRight right, int expiry, double refDelta)
        {
            // Under CRR framework
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new Delta(symbol, 0.0530m, 0.0153m,
                    optionModel: OptionPricingModelType.BinomialCoxRossRubinstein,
                    ivModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refDelta, (double)indicator.Current.Value, 0.0005d);
        }
    }
}
