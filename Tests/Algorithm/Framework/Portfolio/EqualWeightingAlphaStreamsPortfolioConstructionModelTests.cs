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
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class EqualWeightingAlphaStreamsPortfolioConstructionModelTests
    {
        private ZipDataCacheProvider _cacheProvider;
        private DefaultDataProvider _dataProvider;
        private QCAlgorithm _algorithm;

        [SetUp]
        public virtual void SetUp()
        {
            _algorithm = new QCAlgorithm();
            _dataProvider = new DefaultDataProvider();
            var mapFileProvider = new LocalDiskMapFileProvider();
            mapFileProvider.Initialize(_dataProvider);
            var factorFileProvider = new LocalZipFactorFileProvider();
            factorFileProvider.Initialize(mapFileProvider, _dataProvider);
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            _cacheProvider = new ZipDataCacheProvider(_dataProvider);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null,
                _dataProvider, _cacheProvider, mapFileProvider, factorFileProvider,
                null, true, new DataPermissionManager()));
            _algorithm.SetHistoryProvider(historyProvider);
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
        }

        [TearDown]
        public virtual void TearDown()
        {
            _cacheProvider.DisposeSafely();
            _dataProvider.DisposeSafely();
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
            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { data }));

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
            var data = _algorithm.History<AlphaStreamsPortfolioState>(alpha, TimeSpan.FromDays(2)).ToList()[0];
            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { data }));

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
            var position = data.PositionGroups.Single().Positions.Single();
            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { data }));

            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(position.Symbol, targets.Single().Symbol);
            Assert.AreEqual(position.Quantity, targets.Single().Quantity);


            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { new AlphaStreamsPortfolioState { Symbol = alpha } }));
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(position.Symbol, targets.Single().Symbol);
            Assert.AreEqual(0, targets.Single().Quantity);

            // no new targets
            targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(0, targets.Count);
        }

        [TestCase(Language.CSharp)]
        public void MultipleAlphaPositionAggregation(Language language)
        {
            SetPortfolioConstruction(language);

            var symbol = _algorithm.AddData<AlphaStreamsPortfolioState>("9fc8ef73792331b11dbd5429a").Symbol;
            var symbol2 = _algorithm.AddData<AlphaStreamsPortfolioState>("623b06b231eb1cc1aa3643a46").Symbol;
            var data = _algorithm.History<AlphaStreamsPortfolioState>(symbol, TimeSpan.FromDays(1)).Last();
            var position = data.PositionGroups.Single().Positions.Single();

            var data2 = (AlphaStreamsPortfolioState)data.Clone();
            data2.Symbol = symbol2;
            data2.PositionGroups =
                new List<PositionGroupState>
                {
                    new PositionGroupState { Positions =
                        new List<PositionState>
                        {
                            new PositionState
                            {
                                Quantity = position.Quantity * -10,
                                Symbol = position.Symbol
                            }
                        }}
                };

            _algorithm.SetCurrentSlice(new Slice(_algorithm.UtcTime, new List<BaseData> { data, data2 }));

            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, Array.Empty<Insight>()).ToList();
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(targets.Single().Symbol, position.Symbol);

            // each alpha gets 50% of allocation => all have the same TPV
            Assert.AreEqual(targets.Single().Quantity, (position.Quantity * 0.5m + position.Quantity * -10m * 0.5m).DiscretelyRoundBy(1, MidpointRounding.ToZero));
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

            var changes = SecurityChanges.Added(_algorithm.Securities.Values.ToArray());
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, changes);
        }
    }
}
