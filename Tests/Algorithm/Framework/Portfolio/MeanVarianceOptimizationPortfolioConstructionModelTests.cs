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
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Packets;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class MeanVarianceOptimizationPortfolioConstructionModelTests
    {
        private DateTime _nowUtc;
        private QCAlgorithm _algorithm;

        [SetUp]
        public virtual void SetUp()
        {
            _nowUtc = new DateTime(2013, 10, 8);
            _algorithm = new QCAlgorithm();
            _algorithm.SetPandasConverter();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _algorithm.SetDateTime(_nowUtc.ConvertToUtc(_algorithm.TimeZone));
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            _algorithm.SetHistoryProvider(historyProvider);

            historyProvider.Initialize(new HistoryProviderInitializeParameters(
                new BacktestNodePacket(),
                new Api.Api(),
                new DefaultDataProvider(),
                new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                new LocalDiskMapFileProvider(),
                new LocalDiskFactorFileProvider(new LocalDiskMapFileProvider()),
                i => { },
                true,
                new DataPermissionManager()));
        }

        [TestCase(Language.CSharp, PortfolioBias.Long)]
        [TestCase(Language.Python, PortfolioBias.Long)]
        [TestCase(Language.CSharp, PortfolioBias.Short)]
        [TestCase(Language.Python, PortfolioBias.Short)]
        public void PortfolioBiasIsRespected(Language language, PortfolioBias bias)
        {
            SetPortfolioConstruction(language, bias);

            var appl = _algorithm.AddEquity("AAPL");
            var bac = _algorithm.AddEquity("BAC");
            var spy = _algorithm.AddEquity("SPY");
            var ibm = _algorithm.AddEquity("IBM");
            var aig = _algorithm.AddEquity("AIG");
            var qqq = _algorithm.AddEquity("QQQ");

            foreach (var equity in new[] { qqq, aig, ibm, spy, bac, appl })
            {
                equity.SetMarketPrice(new Tick(_nowUtc, equity.Symbol, 30, 30));
            }

            var insights = new[]
            {
                new Insight(_nowUtc, appl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, -0.1d, null),
                new Insight(_nowUtc, spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, -0.1d, null),
                new Insight(_nowUtc, ibm.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, 0d, null),
                new Insight(_nowUtc, aig.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, -0.1d, null),
                new Insight(_nowUtc, qqq.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, -0.1d, null),
                new Insight(_nowUtc, bac.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, -0.1d, null)
            };
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, SecurityChanges.Added(appl, spy, ibm, aig, qqq, bac));

            foreach (var target in _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights))
            {
                QuantConnect.Logging.Log.Trace($"{target.Symbol}: {target.Quantity}");
                if (target.Quantity == 0)
                {
                    continue;
                }
                Assert.AreEqual(Math.Sign((int)bias), Math.Sign(target.Quantity));
            }
        }

        protected void SetPortfolioConstruction(Language language, PortfolioBias bias)
        {
            var model = GetPortfolioConstructionModel(language, Resolution.Daily, bias);
            _algorithm.SetPortfolioConstruction(model);

            foreach (var kvp in _algorithm.Portfolio)
            {
                kvp.Value.SetHoldings(kvp.Value.Price, 0);
            }

            var changes = SecurityChanges.Added(_algorithm.Securities.Values.ToArray());
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, changes);
        }

        public IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, Resolution resolution, PortfolioBias bias)
        {
            if (language == Language.CSharp)
            {
                return new MeanVarianceOptimizationPortfolioConstructionModel(resolution, bias, 1, 63, Resolution.Daily, 0.0001);
            }

            using (Py.GIL())
            {
                const string name = nameof(MeanVarianceOptimizationPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name)
                    .Invoke(((int)resolution).ToPython(), ((int)bias).ToPython(), 1.ToPython(), 63.ToPython(), ((int)Resolution.Daily).ToPython(), 0.0001.ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
        }
    }
}
