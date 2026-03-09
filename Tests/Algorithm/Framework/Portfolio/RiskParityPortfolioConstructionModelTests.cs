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
using Accord.Math;
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
    public class RiskParityPortfolioConstructionModelTests
    {
        private DateTime _nowUtc;
        private QCAlgorithm _algorithm;

        [SetUp]
        public virtual void SetUp()
        {
            _nowUtc = new DateTime(2021, 1, 10);
            _algorithm = new AlgorithmStub();
            _algorithm.SetFinishedWarmingUp();
            _algorithm.Settings.MinimumOrderMarginPortfolioPercentage = 0;
            _algorithm.Settings.FreePortfolioValue = 250;
            _algorithm.SetDateTime(_nowUtc.ConvertToUtc(_algorithm.TimeZone));
            _algorithm.SetCash(1200);
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            _algorithm.SetHistoryProvider(historyProvider);

            historyProvider.Initialize(new HistoryProviderInitializeParameters(
                new BacktestNodePacket(),
                null,
                TestGlobals.DataProvider,
                TestGlobals.DataCacheProvider,
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                i => { },
                true,
                new DataPermissionManager(),
                _algorithm.ObjectStore,
                _algorithm.Settings));
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DoesNotReturnTargetsIfSecurityPriceIsZero(Language language)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.AddEquity(Symbols.SPY.Value);
            algorithm.SetDateTime(DateTime.MinValue.ConvertToUtc(algorithm.TimeZone));

            SetPortfolioConstruction(language, PortfolioBias.Long);

            var insights = new[] { new Insight(_nowUtc, Symbols.SPY, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, null, null) };
            var actualTargets = algorithm.PortfolioConstruction.CreateTargets(algorithm, insights);

            Assert.AreEqual(0, actualTargets.Count());
        }

        [TestCase(Language.CSharp, PortfolioBias.Long)]
        [TestCase(Language.Python, PortfolioBias.Long)]
        [TestCase(Language.CSharp, PortfolioBias.Short)]
        [TestCase(Language.Python, PortfolioBias.Short)]
        public void PortfolioBiasIsRespected(Language language, PortfolioBias bias)
        {
            if (bias == PortfolioBias.Short)
            {
                var throwsConstraint = language == Language.CSharp
                    ? Throws.InstanceOf<ArgumentException>()
                    : Throws.InstanceOf<ClrBubbledException>().With.InnerException.InstanceOf<ArgumentException>();
                Assert.That(() => GetPortfolioConstructionModel(language, bias, Resolution.Daily),
                    throwsConstraint.And.Message.EqualTo("Long position must be allowed in RiskParityPortfolioConstructionModel."));
                return;
            }

            SetPortfolioConstruction(language, bias);

            var aapl = _algorithm.AddEquity("AAPL");
            var spy = _algorithm.AddEquity("SPY");

            foreach (var equity in new[] { aapl, spy })
            {
                equity.SetMarketPrice(new Tick(_nowUtc, equity.Symbol, 10, 10));
            }
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, SecurityChangesTests.AddedNonInternal(aapl, spy));

            var insights = new[]
            {
                new Insight(_nowUtc, aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, null, null),
                new Insight(_nowUtc, spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, null, null)
            };

            foreach (var target in _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights))
            {
                if (target.Quantity == 0)
                {
                    continue;
                }
                Assert.AreEqual(Math.Sign((int)bias), Math.Sign(target.Quantity));
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void CorrectWeightings(Language language)
        {
            SetPortfolioConstruction(language, PortfolioBias.Long);

            var aapl = _algorithm.AddEquity("AAPL");
            var spy = _algorithm.AddEquity("SPY");

            aapl.SetMarketPrice(new Tick(_nowUtc, aapl.Symbol, 10, 10));
            spy.SetMarketPrice(new Tick(_nowUtc, spy.Symbol, 10, 10));
            aapl.SetMarketPrice(new Tick(_nowUtc.AddDays(1), aapl.Symbol, 12, 12));
            spy.SetMarketPrice(new Tick(_nowUtc.AddDays(1), spy.Symbol, 20, 20));
            aapl.SetMarketPrice(new Tick(_nowUtc.AddDays(2), aapl.Symbol, 13, 13));
            spy.SetMarketPrice(new Tick(_nowUtc.AddDays(2), spy.Symbol, 30, 30));

            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, SecurityChangesTests.AddedNonInternal(aapl, spy));

            var insights = new[]
            {
                new Insight(_nowUtc.AddDays(2), aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, null, null),
                new Insight(_nowUtc.AddDays(2), spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, null, null)
            };

            _algorithm.Insights.AddRange(insights);

            var targets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights).ToArray();
            Assert.AreEqual(targets[0].Quantity, 30m);      // AAPL
            Assert.AreEqual(targets[1].Quantity, 18m);      // SPY
        }

        protected void SetPortfolioConstruction(Language language, PortfolioBias bias, IPortfolioConstructionModel defaultModel = null)
        {
            var model = defaultModel ?? GetPortfolioConstructionModel(language, bias, Resolution.Daily);
            _algorithm.SetPortfolioConstruction(model);

            foreach (var kvp in _algorithm.Portfolio)
            {
                kvp.Value.SetHoldings(kvp.Value.Price, 0);
            }

            var changes = SecurityChangesTests.AddedNonInternal(_algorithm.Securities.Values.ToArray());
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, changes);
        }

        public IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, PortfolioBias bias, Resolution resolution)
        {
            if (language == Language.CSharp)
            {
                return new RiskParityPortfolioConstructionModel(resolution, bias, 1, 252, resolution);
            }

            using (Py.GIL())
            {
                const string name = nameof(RiskParityPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name)
                    .Invoke(((int)resolution).ToPython(), ((int)bias).ToPython(), 1.ToPython(), 252.ToPython(), ((int)resolution).ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
        }
    }
}
