/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by aaplicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Packets;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class MeanReversionPortfolioConstructionModelTests
    {
        private DateTime _nowUtc;
        private QCAlgorithm _algorithm;

        [SetUp]
        public virtual void SetUp()
        {
            _nowUtc = new DateTime(2021, 1, 10);
            _algorithm = new QCAlgorithm();
            _algorithm.SetFinishedWarmingUp();
            _algorithm.SetPandasConverter();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _algorithm.SetDateTime(_nowUtc.ConvertToUtc(_algorithm.TimeZone));
            _algorithm.SetCash(1200);
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            _algorithm.SetHistoryProvider(historyProvider);

            historyProvider.Initialize(new HistoryProviderInitializeParameters(
                new BacktestNodePacket(),
                new Api.Api(),
                TestGlobals.DataProvider,
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                i => { },
                true,
                new DataPermissionManager()));
        }

        [TestCase(Language.CSharp, null, null, 47, 47)]
        [TestCase(Language.Python, null, null, 47, 47)]
        [TestCase(Language.CSharp, 1, -0.5, 31, 63)]
        [TestCase(Language.Python, 1, -0.5, 31, 63)]
        public void CorrectWeightings(Language language, double? magnitude1, double? magnitude2, decimal expectedQty1, decimal expectedQty2)
        {
            SetPortfolioConstruction(language);

            var aapl = _algorithm.AddEquity("AAPL");
            var spy = _algorithm.AddEquity("SPY");

            foreach (var equity in new[] { aapl, spy })
            {
                equity.SetMarketPrice(new Tick(_nowUtc, equity.Symbol, 10, 10));
            }

            var insights = new[]
            {
                new Insight(_nowUtc, aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, magnitude1, null),
                new Insight(_nowUtc, spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, magnitude2, null),
            };
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, SecurityChangesTests.AddedNonInternal(aapl, spy));

            var quantities = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).Select(x => x.Quantity).ToArray();
            Assert.AreEqual(quantities[0], expectedQty1);
            Assert.AreEqual(quantities[1], expectedQty2);
        }

        protected void SetPortfolioConstruction(Language language)
        {
            var model = GetPortfolioConstructionModel(language, Resolution.Daily);
            _algorithm.SetPortfolioConstruction(model);

            foreach (var kvp in _algorithm.Portfolio)
            {
                kvp.Value.SetHoldings(kvp.Value.Price, 0);
            }

            var changes = SecurityChangesTests.AddedNonInternal(_algorithm.Securities.Values.ToArray());
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, changes);
        }

        public IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, Resolution resolution)
        {
            if (language == Language.CSharp)
            {
                return new MeanReversionPortfolioConstructionModel(1, 1, resolution);
            }

            using (Py.GIL())
            {
                const string name = nameof(MeanReversionPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name)
                    .Invoke(1.ToPython(), 1.ToPython(), ((int)resolution).ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
        }
    }
}