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
using QuantConnect.Util;
using QuantConnect.Algorithm;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Tests.Common.Data.UniverseSelection;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class EqualWeightingAlphaStreamsPortfolioConstructionModelTests
    {
        private ZipDataCacheProvider _cacheProvider;
        private QCAlgorithm _algorithm;

        [SetUp]
        public virtual void SetUp()
        {
            _algorithm = new QCAlgorithm();
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            _cacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null,
                TestGlobals.DataProvider, _cacheProvider, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider,
                null, true, new DataPermissionManager(), _algorithm.ObjectStore));
            _algorithm.SetHistoryProvider(historyProvider);
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _algorithm.Settings.FreePortfolioValue = 0;
            _algorithm.SetFinishedWarmingUp();
        }

        [TearDown]
        public virtual void TearDown()
        {
            _cacheProvider.DisposeSafely();
        }

        [TestCase(Language.CSharp)]
        public void NoTargets(Language language)
        {
            SetPortfolioConstruction(language);
            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(0, targets.Count);
        }

        [TestCase(Language.CSharp)]
        public void IgnoresInsights(Language language)
        {
            SetPortfolioConstruction(language);
            var insight = Insight.Price(Symbols.AAPL, Resolution.Minute, 1, InsightDirection.Down, 1, 1, "AlphaId", 0.5);
            insight.GeneratedTimeUtc = _algorithm.UtcTime;
            insight.CloseTimeUtc = _algorithm.UtcTime.Add(insight.Period);
            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, new[] { insight }).ToList();
            Assert.AreEqual(0, targets.Count);
        }

        [TestCase(Language.CSharp)]
        public void SingleAlphaSinglePosition(Language language)
        {
            SetPortfolioConstruction(language);

            var alpha = _algorithm.AddData<AlphaStreamsPortfolioState>("9fc8ef73792331b11dbd5429a").Symbol;
            var data = _algorithm.History<AlphaStreamsPortfolioState>(alpha, TimeSpan.FromDays(2)).Last();
            AddSecurities(_algorithm, data);
            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { data }, _algorithm.UtcTime));

            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(1, targets.Count);
            var position = data.PositionGroups.Single().Positions.Single();
            Assert.AreEqual(position.Symbol, targets.Single().Symbol);
            Assert.AreEqual(position.Quantity, targets.Single().Quantity);
        }

        [TestCase(Language.CSharp)]
        public void SingleAlphaMultiplePositions(Language language)
        {
            SetPortfolioConstruction(language);

            var alpha = _algorithm.AddData<AlphaStreamsPortfolioState>("9fc8ef73792331b11dbd5429a").Symbol;
            var data = _algorithm.History<AlphaStreamsPortfolioState>(alpha, TimeSpan.FromDays(1)).ToList()[0];
            AddSecurities(_algorithm, data);
            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { data }, _algorithm.UtcTime));

            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(600, targets.Single(target => target.Symbol == Symbols.GOOG).Quantity);

            var option = Symbol.CreateOption(Symbols.GOOG, Market.USA, OptionStyle.American, OptionRight.Call, 750, new DateTime(2016, 6, 17));
            Assert.AreEqual(-5, targets.Single(target => target.Symbol == option).Quantity);
        }

        [TestCase(Language.CSharp)]
        public void SingleAlphaPositionRemoval(Language language)
        {
            SetPortfolioConstruction(language);

            var alpha = _algorithm.AddData<AlphaStreamsPortfolioState>("9fc8ef73792331b11dbd5429a").Symbol;
            var data = _algorithm.History<AlphaStreamsPortfolioState>(alpha, TimeSpan.FromDays(2)).Last();
            AddSecurities(_algorithm, data);
            var position = data.PositionGroups.Single().Positions.Single();
            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { data }, _algorithm.UtcTime));

            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(position.Symbol, targets.Single().Symbol);
            Assert.AreEqual(position.Quantity, targets.Single().Quantity);


            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { new AlphaStreamsPortfolioState { Symbol = alpha } }, _algorithm.UtcTime));
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(position.Symbol, targets.Single().Symbol);
            Assert.AreEqual(0, targets.Single().Quantity);

            // no new targets
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(0, targets.Count);
        }

        [TestCase(Language.CSharp, 1000000, 10000, 0)]
        [TestCase(Language.CSharp, 10000, 1000000, 0)]
        [TestCase(Language.CSharp, 10000, 10000, 0)]
        [TestCase(Language.CSharp, 100000, 100000, 0)]
        [TestCase(Language.CSharp, 1000000, 1000000, 0)]
        [TestCase(Language.CSharp, 1000000, 10000, 5000)]
        [TestCase(Language.CSharp, 10000, 1000000, 5000)]
        [TestCase(Language.CSharp, 10000, 10000, 5000)]
        [TestCase(Language.CSharp, 100000, 100000, 5000)]
        [TestCase(Language.CSharp, 1000000, 1000000, 5000)]
        public void MultipleAlphaPositionAggregation(Language language, decimal totalPortfolioValueAlpha1, decimal totalPortfolioValueAlpha2, decimal freePortfolioValue)
        {
            SetPortfolioConstruction(language);

            _algorithm.Settings.FreePortfolioValue = freePortfolioValue;
            var alpha1 = _algorithm.AddData<AlphaStreamsPortfolioState>("9fc8ef73792331b11dbd5429a");
            var alpha2 = _algorithm.AddData<AlphaStreamsPortfolioState>("623b06b231eb1cc1aa3643a46");
            _algorithm.OnFrameworkSecuritiesChanged(SecurityChangesTests.AddedNonInternal(alpha1, alpha2));
            var symbol = alpha1.Symbol;
            var symbol2 = alpha2.Symbol;
            var data = _algorithm.History<AlphaStreamsPortfolioState>(symbol, TimeSpan.FromDays(1)).Last();
            AddSecurities(_algorithm, data);
            data.TotalPortfolioValue = totalPortfolioValueAlpha1;
            var position = data.PositionGroups.Single().Positions.Single();

            var data2 = (AlphaStreamsPortfolioState)data.Clone();
            data2.Symbol = symbol2;
            data2.TotalPortfolioValue = totalPortfolioValueAlpha2;
            data2.PositionGroups =
                new List<PositionGroupState>
                {
                    new PositionGroupState { Positions =
                        new List<PositionState>
                        {
                            new PositionState
                            {
                                Quantity = position.Quantity * -10,
                                Symbol = position.Symbol,
                                UnitQuantity = 1
                            }
                        }}
                };

            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { data, data2 }, _algorithm.UtcTime));

            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(position.Symbol, targets.Single().Symbol);

            var tvpPerAlpha = (_algorithm.Portfolio.TotalPortfolioValue - freePortfolioValue) * 0.5m;
            var alpha1Weight =  tvpPerAlpha / data.TotalPortfolioValue;
            var alpha2Weight =  tvpPerAlpha / data2.TotalPortfolioValue;

            Assert.AreEqual((position.Quantity * alpha1Weight).DiscretelyRoundBy(1, MidpointRounding.ToZero)
                + (position.Quantity * -10m * alpha2Weight).DiscretelyRoundBy(1, MidpointRounding.ToZero),
                targets.Single().Quantity);
        }

        private void AddSecurities(QCAlgorithm algorithm, AlphaStreamsPortfolioState portfolioState)
        {
            foreach (var symbol in portfolioState.PositionGroups?.SelectMany(positionGroup => positionGroup.Positions)
                .Select(state => state.Symbol) ?? Enumerable.Empty<Symbol>())
            {
                _algorithm.AddSecurity(symbol);
            }
        }

        private void SetUtcTime(DateTime dateTime) => _algorithm.SetDateTime(dateTime.ConvertToUtc(_algorithm.TimeZone));

        private void SetPortfolioConstruction(Language language)
        {
            _algorithm.SetCurrentSlice(null);
            IPortfolioConstructionModel model;
            if (language == Language.CSharp)
            {
                model = new EqualWeightingAlphaStreamsPortfolioConstructionModel();
            }
            else
            {
                throw new NotImplementedException($"{language} not implemented");
            }
            _algorithm.SetPortfolioConstruction(model);

            foreach (var kvp in _algorithm.Portfolio)
            {
                kvp.Value.SetHoldings(kvp.Value.Price, 0);
            }
            _algorithm.Portfolio.SetCash(100000);
            SetUtcTime(new DateTime(2018, 4, 5));

            var changes = SecurityChangesTests.AddedNonInternal(_algorithm.Securities.Values.ToArray());
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, changes);
        }
    }
}
