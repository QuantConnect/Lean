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

using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmIndicatorsTests
    {
        private QCAlgorithm _algorithm;
        private Symbol _option;

        [SetUp]
        public void Setup()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));

            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null,
                TestGlobals.DataProvider, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider,
                null, true, new DataPermissionManager(), _algorithm.ObjectStore, _algorithm.Settings));
            _algorithm.SetHistoryProvider(historyProvider);

            _algorithm.SetDateTime(new DateTime(2013, 10, 11, 15, 0, 0));
            _algorithm.AddEquity("SPY");
            _option = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 450m, new DateTime(2023, 9, 1));
            _algorithm.AddOptionContract(_option);
            _algorithm.EnableAutomaticIndicatorWarmUp = true;
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
    }
}
