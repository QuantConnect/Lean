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

using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Tests.Research;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmIndicatorsTests
    {
        private QCAlgorithm _algorithm;
        private Symbol _equity;
        private Symbol _option;

        [SetUp]
        public void Setup()
        {
            _algorithm = new AlgorithmStub();
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null,
                TestGlobals.DataProvider, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider,
                null, true, new DataPermissionManager(), _algorithm.ObjectStore, _algorithm.Settings));
            _algorithm.SetHistoryProvider(historyProvider);

            _algorithm.SetDateTime(new DateTime(2013, 10, 11, 15, 0, 0));
            _equity = _algorithm.AddEquity("SPY").Symbol;
            _option = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 450m, new DateTime(2023, 9, 1));
            _algorithm.AddOptionContract(_option);
            _algorithm.Settings.AutomaticIndicatorWarmUp = true;
        }

        [Test]
        public void IndicatorsPassSelectorToWarmUp()
        {
            var mockSelector = new Mock<Func<IBaseData, TradeBar>>();
            mockSelector.Setup(_ => _(It.IsAny<IBaseData>())).Returns<TradeBar>(_ => (TradeBar)_);

            var indicator = _algorithm.ABANDS(Symbols.SPY, 20, selector: mockSelector.Object);

            Assert.IsTrue(indicator.IsReady);
            mockSelector.Verify(_ => _(It.IsAny<IBaseData>()), Times.Exactly(indicator.WarmUpPeriod));
        }

        [Test]
        public void SharpeRatioIndicatorUsesAlgorithmsRiskFreeRateModelSetAfterIndicatorRegistration()
        {
            // Register indicator
            var sharpeRatio = _algorithm.SR(Symbols.SPY, 10);

            // Setup risk free rate model
            var interestRateProviderMock = new Mock<IRiskFreeInterestRateModel>();
            var reference = new DateTime(2023, 11, 21, 10, 0, 0);
            interestRateProviderMock.Setup(x => x.GetInterestRate(reference)).Verifiable();

            // Update indicator
            sharpeRatio.Update(new IndicatorDataPoint(Symbols.SPY, reference, 300m));

            // Our interest rate provider shouldn't have been called yet since it's hasn't been set to the algorithm
            interestRateProviderMock.Verify(x => x.GetInterestRate(reference), Times.Never);

            // Set the interest rate provider to the algorithm
            _algorithm.SetRiskFreeInterestRateModel(interestRateProviderMock.Object);

            // Update indicator
            sharpeRatio.Update(new IndicatorDataPoint(Symbols.SPY, reference, 300m));

            // Our interest rate provider should have been called once
            interestRateProviderMock.Verify(x => x.GetInterestRate(reference), Times.Once);
        }

        [TestCase("Span")]
        [TestCase("Count")]
        [TestCase("StartAndEndDate")]
        public void IndicatorsDataPoint(string testCase)
        {
            var indicator = new BollingerBands(10, 2);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            using (Py.GIL())
            {
                PyObject pandasFrame = null;
                if (testCase == "StartAndEndDate")
                {
                    pandasFrame = _algorithm.Indicator(indicator, _equity, new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                }
                else if (testCase == "Span")
                {
                    pandasFrame = _algorithm.Indicator(indicator, _equity, TimeSpan.FromDays(5), Resolution.Minute);
                }
                else if (testCase == "Count")
                {
                    pandasFrame = _algorithm.Indicator(indicator, _equity, (int)(4 * 60 * 6.5), Resolution.Minute);
                }

                Assert.IsTrue(indicator.IsReady);
                Assert.AreEqual(1551, QuantBookIndicatorsTests.GetDataFrameLength(pandasFrame));
            }
        }

        [TestCase("Span")]
        [TestCase("Count")]
        [TestCase("StartAndEndDate")]
        public void IndicatorsBar(string testCase)
        {
            var indicator = new AverageTrueRange(10);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            using (Py.GIL())
            {
                PyObject pandasFrame = null;
                if (testCase == "StartAndEndDate")
                {
                    pandasFrame = _algorithm.Indicator(indicator, _equity, new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                }
                else if (testCase == "Span")
                {
                    pandasFrame = _algorithm.Indicator(indicator, _equity, TimeSpan.FromDays(5), Resolution.Minute);
                }
                else if (testCase == "Count")
                {
                    pandasFrame = _algorithm.Indicator(indicator, _equity, (int)(4 * 60 * 6.5), Resolution.Minute);
                }

                Assert.IsTrue(indicator.IsReady);
                Assert.AreEqual(1551, QuantBookIndicatorsTests.GetDataFrameLength(pandasFrame));
            }
        }

        [Test]
        public void IndicatorsPassingHistory()
        {
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new Beta(_equity, referenceSymbol, 10);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            using (Py.GIL())
            {
                var history = _algorithm.History(new[] { _equity, referenceSymbol }, TimeSpan.FromDays(5), Resolution.Minute);
                PyObject pandasFrame = _algorithm.Indicator(indicator, history);

                Assert.IsTrue(indicator.IsReady);
                Assert.AreEqual(3099, QuantBookIndicatorsTests.GetDataFrameLength(pandasFrame));
            }
        }
    }
}
