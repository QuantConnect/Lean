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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Indicators
{
    public abstract class OptionBaseIndicatorTests<T> : CommonIndicatorTests<IBaseData>
        where T : OptionIndicatorBase
    {
        // count of risk free rate calls per each update on opiton indicator
        protected int RiskFreeRateUpdatesPerIteration { get; set; }

        // count of dividend yield calls per each update on option indicator
        protected int DividendYieldUpdatesPerIteration { get; set; }

        protected static DateTime _reference = new DateTime(2023, 8, 1, 10, 0, 0);
        protected static Symbol _symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 450m, new DateTime(2023, 9, 1));
        protected Symbol _underlying => _symbol.Underlying;

        protected override IndicatorBase<IBaseData> CreateIndicator()
        {
            throw new NotImplementedException("method `CreateIndicator()` is required to be set up");
        }

        protected virtual OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
        {
            throw new NotImplementedException("method `CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)` is required to be set up");
        }

        protected virtual OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
        {
            throw new NotImplementedException("method `CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)` is required to be set up");
        }

        protected virtual OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
        {
            throw new NotImplementedException("method `CreateIndicator(QCAlgorithm algorithm)` is required to be set up");
        }

        protected OptionPricingModelType ParseSymbols(string[] items, bool american, out Symbol call, out Symbol put)
        {
            var ticker = items[0];
            var expiry = DateTime.ParseExact(items[1], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var strike = Parse.Decimal(items[2]);
            var style = american ? OptionStyle.American : OptionStyle.European;

            call = Symbol.CreateOption(ticker, Market.USA, style, OptionRight.Call, strike, expiry);
            put = Symbol.CreateOption(ticker, Market.USA, style, OptionRight.Put, strike, expiry);

            return american ? OptionPricingModelType.ForwardTree : OptionPricingModelType.BlackScholes;
        }

        protected void RunTestIndicator(Symbol call, Symbol put, OptionIndicatorBase callIndicator, OptionIndicatorBase putIndicator,
            string[] items, int callColumn, int putColumn, double errorRate, double errorMargin = 1e-4)
        {
            var time = DateTime.ParseExact(items[3], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            var callDataPoint = new IndicatorDataPoint(call, time, decimal.Parse(items[5], NumberStyles.Any, CultureInfo.InvariantCulture));
            var putDataPoint = new IndicatorDataPoint(put, time, decimal.Parse(items[4], NumberStyles.Any, CultureInfo.InvariantCulture));
            var underlyingDataPoint = new IndicatorDataPoint(call.Underlying, time, decimal.Parse(items[^4], NumberStyles.Any, CultureInfo.InvariantCulture));

            callIndicator.Update(callDataPoint);
            callIndicator.Update(underlyingDataPoint);
            if (callIndicator.UseMirrorContract)
            {
                callIndicator.Update(putDataPoint);
            }

            var expected = double.Parse(items[callColumn], NumberStyles.Any, CultureInfo.InvariantCulture);
            var acceptance = Math.Max(errorRate * Math.Abs(expected), errorMargin);     // percentage error
            Assert.AreEqual(expected, (double)callIndicator.Current.Value, acceptance);

            putIndicator.Update(putDataPoint);
            putIndicator.Update(underlyingDataPoint);
            if (putIndicator.UseMirrorContract)
            {
                putIndicator.Update(callDataPoint);
            }

            expected = double.Parse(items[putColumn], NumberStyles.Any, CultureInfo.InvariantCulture);
            acceptance = Math.Max(errorRate * Math.Abs(expected), errorMargin);     // percentage error
            Assert.AreEqual(expected, (double)putIndicator.Current.Value, acceptance);
        }

        protected override List<Symbol> GetSymbols()
        {
            return [Symbols.GOOG];
        }

        [Test]
        public void ZeroGreeksIfExpired()
        {
            var indicator = CreateIndicator();
            var date = new DateTime(2099, 1, 1);    // date that the option must be expired already
            var price = 500m;
            var optionPrice = 10m;

            indicator.Update(new IndicatorDataPoint(_symbol, date, optionPrice));
            indicator.Update(new IndicatorDataPoint(_underlying, date, price));

            Assert.AreEqual(0m, indicator.Current.Value);
        }

        [Test]
        public override void ResetsProperly()
        {
            var indicator = CreateIndicator();

            for (var i = 0; i < 5; i++)
            {
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                indicator.Update(new IndicatorDataPoint(_symbol, _reference.AddDays(1 + i), optionPrice));
                indicator.Update(new IndicatorDataPoint(_underlying, _reference.AddDays(1 + i), price));
            }

            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(indicator);
        }

        [Test]
        public override void TimeMovesForward()
        {
            var indicator = CreateIndicator();

            for (var i = 10; i > 0; i--)
            {
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                indicator.Update(new IndicatorDataPoint(_symbol, _reference.AddDays(1 + i), optionPrice));
                indicator.Update(new IndicatorDataPoint(_underlying, _reference.AddDays(1 + i), price));
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = CreateIndicator();
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

                Assert.IsTrue(indicator.IsReady);
            }

            Assert.AreEqual(2 * warmUpPeriod.Value, indicator.Samples);
        }

        [Test]
        public override void WarmUpIndicatorProducesConsistentResults()
        {
            var algo = CreateAlgorithm();

            algo.SetStartDate(2015, 12, 24);
            algo.SetEndDate(2015, 12, 24);

            var underlying = Symbols.GOOG;

            var expiration = new DateTime(2015, 12, 24);
            var strike = 650m;

            var option = Symbol.CreateOption(underlying, Market.USA, OptionStyle.American, OptionRight.Put, strike, expiration);
            SymbolList = [option];

            var symbolsForWarmUp = new List<Symbol> { option, option.Underlying };
            // Define the risk-free rate and dividend yield models
            var risk = new ConstantRiskFreeRateInterestRateModel(12);
            var dividend = new ConstantDividendYieldModel(12);

            // Create the first indicator using the risk and dividend models
            var firstIndicator = CreateIndicator(risk, dividend);
            var period = (firstIndicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;
            if (period == null || period == 0)
            {
                Assert.Ignore($"{firstIndicator.Name}, Skipping this test because it's not applicable.");
            }

            // Warm up the first indicator
            algo.WarmUpIndicator(symbolsForWarmUp, firstIndicator, Resolution.Daily);

            // Warm up the second indicator manually
            var secondIndicator = CreateIndicator(risk, dividend);
            var history = algo.History(symbolsForWarmUp, period.Value, Resolution.Daily).ToList();
            foreach (var slice in history)
            {
                foreach (var symbol in symbolsForWarmUp)
                {
                    secondIndicator.Update(slice[symbol]);
                }
            }
            SymbolList.Clear();

            // Assert that the indicators are ready
            Assert.IsTrue(firstIndicator.IsReady);
            Assert.IsTrue(secondIndicator.IsReady);
            if (!ValueCanBeZero)
            {
                Assert.AreNotEqual(firstIndicator.Current.Value, 0);
            }

            // Ensure that the first indicator has processed some data
            Assert.AreNotEqual(firstIndicator.Samples, 0);

            // Validate that both indicators have the same number of processed samples
            Assert.AreEqual(firstIndicator.Samples, secondIndicator.Samples);

            // Validate that both indicators produce the same final computed value
            Assert.AreEqual(firstIndicator.Current.Value, secondIndicator.Current.Value);
        }

        [Test]
        public override void WorksWithLowValues()
        {
            Symbol = _symbol;
            base.WorksWithLowValues();
        }

        [Test]
        public void UsesRiskFreeInterestRateModel()
        {
            const int count = 20;
            var dates = Enumerable.Range(0, count).Select(i => new DateTime(2022, 11, 21, 10, 0, 0) + TimeSpan.FromDays(i)).ToList();
            var interestRateValues = Enumerable.Range(0, count).Select(i => 0m + (10 - 0m) * (i / (count - 1m))).ToList();

            var interestRateProviderMock = new Mock<IRiskFreeInterestRateModel>();

            // Set up
            for (int i = 0; i < count; i++)
            {
                interestRateProviderMock.Setup(x => x.GetInterestRate(dates[i])).Returns(interestRateValues[i]).Verifiable();
            }

            var indicator = CreateIndicator(interestRateProviderMock.Object);

            for (int i = 0; i < count; i++)
            {
                indicator.Update(new IndicatorDataPoint(_symbol, dates[i], 80m + i));
                indicator.Update(new IndicatorDataPoint(_underlying, dates[i], 500m + i));
                Assert.AreEqual(interestRateValues[i], indicator.RiskFreeRate.Current.Value);
            }

            // Assert
            Assert.IsTrue(indicator.IsReady);
            interestRateProviderMock.Verify(x => x.GetInterestRate(It.IsAny<DateTime>()), Times.Exactly(dates.Count * RiskFreeRateUpdatesPerIteration));
            for (int i = 0; i < count; i++)
            {
                interestRateProviderMock.Verify(x => x.GetInterestRate(dates[i]), Times.Exactly(RiskFreeRateUpdatesPerIteration));
            }
        }

        [Test]
        public void UsesPythonDefinedRiskFreeInterestRateModel()
        {
            using var _ = Py.GIL();

            var module = PyModule.FromString(Guid.NewGuid().ToString(), $@"
from AlgorithmImports import *

class TestRiskFreeInterestRateModel:
    CallCount = 0

    def GetInterestRate(self, date: datetime) -> float:
        TestRiskFreeInterestRateModel.CallCount += 1
        return 0.5

def getOptionIndicatorBaseIndicator(symbol: Symbol) -> OptionIndicatorBase:
    return {typeof(T).Name}(symbol, TestRiskFreeInterestRateModel())
            ");

            var iv = module.GetAttr("getOptionIndicatorBaseIndicator").Invoke(_symbol.ToPython()).GetAndDispose<T>();
            var modelClass = module.GetAttr("TestRiskFreeInterestRateModel");

            var reference = new DateTime(2022, 11, 21, 10, 0, 0);
            for (int i = 0; i < 20; i++)
            {
                iv.Update(new IndicatorDataPoint(_symbol, reference + TimeSpan.FromMinutes(i), 10m + i));
                iv.Update(new IndicatorDataPoint(_underlying, reference + TimeSpan.FromMinutes(i), 1000m + i));
                Assert.AreEqual((i + 1) * RiskFreeRateUpdatesPerIteration, modelClass.GetAttr("CallCount").GetAndDispose<int>());
            }
        }

        [Test]
        public void OptionIndicatorUsesAlgorithmsRiskFreeRateModelSetAfterIndicatorRegistration()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null,
                TestGlobals.DataProvider, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider,
                null, true, new DataPermissionManager(), algorithm.ObjectStore, algorithm.Settings));
            algorithm.SetHistoryProvider(historyProvider);

            algorithm.SetDateTime(_reference);
            algorithm.AddEquity(_underlying.Value);
            algorithm.AddOptionContract(_symbol);
            algorithm.Settings.AutomaticIndicatorWarmUp = true;

            // Register indicator
            var indicator = CreateIndicator(algorithm);

            // Setup risk free rate model
            var interestRateProviderMock = new Mock<IRiskFreeInterestRateModel>();
            interestRateProviderMock.Setup(x => x.GetInterestRate(_reference)).Verifiable();

            // Update indicator
            indicator.Update(new IndicatorDataPoint(_symbol, _reference, 30m));
            indicator.Update(new IndicatorDataPoint(_underlying, _reference, 300m));

            // Our interest rate provider shouldn't have been called yet since it's hasn't been set to the algorithm
            interestRateProviderMock.Verify(x => x.GetInterestRate(_reference), Times.Never);

            // Set the interest rate provider to the algorithm
            algorithm.SetRiskFreeInterestRateModel(interestRateProviderMock.Object);

            // Update indicator
            indicator.Update(new IndicatorDataPoint(_symbol, _reference.AddDays(1), 30m));
            indicator.Update(new IndicatorDataPoint(_underlying, _reference.AddDays(1), 300m));

            // Our interest rate provider should have been called once by each update
            interestRateProviderMock.Verify(x => x.GetInterestRate(_reference.AddDays(1)), Times.Exactly(RiskFreeRateUpdatesPerIteration));
        }

        [Test]
        public void UsesDividendYieldModel()
        {
            const int count = 20;
            var dates = Enumerable.Range(0, count).Select(i => new DateTime(2022, 11, 21, 10, 0, 0) + TimeSpan.FromDays(i)).ToList();
            var dividends = Enumerable.Range(0, count).Select(i => 0m + (10 - 0m) * (i / (count - 1m))).ToList();

            var dividendYieldProviderMock = new Mock<IDividendYieldModel>();

            // Set up
            var underlyingBasePrice = 500m;
            for (int i = 0; i < count; i++)
            {
                dividendYieldProviderMock.Setup(x => x.GetDividendYield(dates[i], underlyingBasePrice + i)).Returns(dividends[i]).Verifiable();
            }

            var indicator = CreateIndicator(new ConstantRiskFreeRateInterestRateModel(0.05m), dividendYieldProviderMock.Object);

            for (int i = 0; i < count; i++)
            {
                indicator.Update(new IndicatorDataPoint(_symbol, dates[i], 80m + i));
                indicator.Update(new IndicatorDataPoint(_underlying, dates[i], underlyingBasePrice + i));
                Assert.AreEqual(dividends[i], indicator.DividendYield.Current.Value);
            }

            // Assert
            Assert.IsTrue(indicator.IsReady);
            dividendYieldProviderMock.Verify(x => x.GetDividendYield(It.IsAny<DateTime>(), It.IsAny<decimal>()), Times.Exactly(dates.Count * DividendYieldUpdatesPerIteration));
            for (int i = 0; i < count; i++)
            {
                dividendYieldProviderMock.Verify(x => x.GetDividendYield(dates[i], underlyingBasePrice + i), Times.Exactly(DividendYieldUpdatesPerIteration));
            }
        }

        [Test]
        public void UsesPythonDefinedDividendYieldModel()
        {
            using var _ = Py.GIL();

            var module = PyModule.FromString(Guid.NewGuid().ToString(), $@"
from AlgorithmImports import *

class TestDividendYieldModel:
    call_count = 0

    def get_dividend_yield(self, date: datetime, price: float) -> float:
        TestDividendYieldModel.call_count += 1
        return 0.5

def get_option_indicator_base_indicator(symbol: Symbol) -> OptionIndicatorBase:
    return {typeof(T).Name}(symbol, InterestRateProvider(), TestDividendYieldModel())
            ");

            var indicator = module.GetAttr("get_option_indicator_base_indicator").Invoke(_symbol.ToPython()).GetAndDispose<T>();
            var modelClass = module.GetAttr("TestDividendYieldModel");

            var reference = new DateTime(2022, 11, 21, 10, 0, 0);
            for (int i = 0; i < 20; i++)
            {
                indicator.Update(new IndicatorDataPoint(_symbol, reference + TimeSpan.FromMinutes(i), 10m + i));
                indicator.Update(new IndicatorDataPoint(_underlying, reference + TimeSpan.FromMinutes(i), 1000m + i));
                Assert.AreEqual((i + 1) * DividendYieldUpdatesPerIteration, modelClass.GetAttr("call_count").GetAndDispose<int>());
            }
        }

        // Not used
        protected override string TestFileName => null;
        protected override string TestColumnName => null;

        [Test]
        public override void ComparesAgainstExternalData()
        {
            // Not used
        }

        [Test]
        public override void ComparesAgainstExternalDataAfterReset()
        {
            // Not used
        }

        public override void AcceptsRenkoBarsAsInput()
        {
            // Not used
        }

        public override void AcceptsVolumeRenkoBarsAsInput()
        {
            // Not used
        }
    }
}
