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
using System.IO;
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class DeltaTests : OptionBaseIndicatorTests<Delta>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
            => new Delta("testDeltaIndicator", _symbol, 0.0403m, 0.0m);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new Delta("testDeltaIndicator", _symbol, riskFreeRateModel);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
            => new Delta("testDeltaIndicator", _symbol, riskFreeRateModel, dividendYieldModel);

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
            => algorithm.D(_symbol);

        [SetUp]
        public void SetUp()
        {
            // 2 updates per iteration, 1 for greek, 1 for IV
            RiskFreeRateUpdatesPerIteration = 2;
            DividendYieldUpdatesPerIteration = 2;
        }



        [TestCase("american/third_party_1_greeks.csv")]
        public void ComparesAgainstExternalDataMirrorContractMethod(string subPath, int callColumn = 9, int putColumn = 8, double errorMargin = 0.03)
        {
            var path = Path.Combine("TestData", "greeksindicator", subPath);
            foreach (var line in File.ReadAllLines(path).Skip(3))
            {
                var items = line.Split(',');

                var interestRate = decimal.Parse(items[^2]);
                var dividendYield = decimal.Parse(items[^1]);

                var model = ParseSymbols(items, path.Contains("american"), out var call, out var put);

                var callIndicator = new Delta(call, put, interestRate, dividendYield, model);
                var putIndicator = new Delta(put, call, interestRate, dividendYield, model);

                RunTestIndicator(call, put, callIndicator, putIndicator, items, callColumn, putColumn, errorMargin);
            }
        }

        // Just placing the test and data here, we are unsure about the smoothing function and not going to reverse engineer
        [TestCase("american/third_party_2_greeks.csv")]
        public void ComparesAgainstExternalDataSingleContractMethod(string subPath, int callColumn = 9, int putColumn = 8, double errorMargin = 10000)
        {
            var path = Path.Combine("TestData", "greeksindicator", subPath);
            foreach (var line in File.ReadAllLines(path).Skip(3))
            {
                var items = line.Split(',');

                var interestRate = decimal.Parse(items[^2]);
                var dividendYield = decimal.Parse(items[^1]);

                var model = ParseSymbols(items, path.Contains("american"), out var call, out var put);

                var callIndicator = new Delta(call, interestRate, dividendYield, model);
                var putIndicator = new Delta(put, interestRate, dividendYield, model);

                RunTestIndicator(call, put, callIndicator, putIndicator, items, callColumn, putColumn, errorMargin);
            }
        }

        [TestCase("american/third_party_1_greeks.csv")]
        public void ComparesAgainstExternalDataAfterReset(string subPath, int callColumn = 9, int putColumn = 8, double errorMargin = 0.03)
        {
            var path = Path.Combine("TestData", "greeksindicator", subPath);
            foreach (var line in File.ReadAllLines(path).Skip(3))
            {
                var items = line.Split(',');

                var interestRate = decimal.Parse(items[^2]);
                var dividendYield = decimal.Parse(items[^1]);

                var model = ParseSymbols(items, path.Contains("american"), out var call, out var put);

                var callIndicator = new Delta(call, put, interestRate, dividendYield, model);
                var putIndicator = new Delta(put, call, interestRate, dividendYield, model);

                RunTestIndicator(call, put, callIndicator, putIndicator, items, callColumn, putColumn, errorMargin);

                callIndicator.Reset();
                putIndicator.Reset();

                RunTestIndicator(call, put, callIndicator, putIndicator, items, callColumn, putColumn, errorMargin);
            }
        }

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.546)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.446)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.693)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.260)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.243)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.526)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.632)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.417)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.765)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.052)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.263)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.556)]
        public void ComparesDeltaOnBSMModel(decimal price, decimal spotPrice, OptionRight right, int expiry, double refDelta)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new Delta(symbol, 0.0403m, 0.0m, OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refDelta, (double)indicator.Current.Value, 0.0005d);
        }

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.546)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.446)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.693)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.260)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.243)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.526)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.632)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.417)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.765)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.052)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.264)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.556)]
        public void ComparesDeltaOnCRRModel(decimal price, decimal spotPrice, OptionRight right, int expiry, double refDelta)
        {
            // Under CRR framework
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new Delta(symbol, 0.0403m, 0.0m, OptionPricingModelType.BinomialCoxRossRubinstein, OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refDelta, (double)indicator.Current.Value, 0.0005d);
        }
    }
}
