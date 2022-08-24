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
        private double[,] _covariance1, _covariance2, _covariance3, _covariance4;
        private double[] _expectedReturn, _expectedResult1, _expectedResult2, _expectedResult3, _expectedResult4;

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
                null,
                TestGlobals.DataProvider,
                new SingleEntryDataCacheProvider(TestGlobals.DataProvider),
                TestGlobals.MapFileProvider,
                TestGlobals.FactorFileProvider,
                i => { },
                true,
                new DataPermissionManager()));

            // Test by known convex objective: Mean-variance
            _covariance1 = new double[,] {{0.25, -0.2}, {-0.2, 0.25}};      // both 50% variance, -80% correlation
            _covariance2 = new double[,] {{0.01, 0}, {0, 0.04}};            // sigma(A) 10%, sigma(B) 20%, 0% correlation
            _covariance3 = new double[,] {{1, 0.45}, {0.45, 0.25}};         // sigma(A) 100%, sigma(B) 50%, 90% correlation
            _covariance4 = new double[,] {{0.25, 0.05}, {0.05, 0.04}};      // sigma(A) 50%, sigma(B) 10%, 25% correlation

            _expectedReturn = new double[] {0.25d, 0.5d};                   // E_return(A) 25%, E_return(B) 50%

            _expectedResult1 = new double[] {7.22d, 7.78d};
            _expectedResult2 = new double[] {25d, 12.5d};
            _expectedResult3 = new double[] {-3.42d, 8.16d};
            _expectedResult4 = new double[] {-2d, 15d};
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
                var exception = Assert.Throws<ArgumentException>(() => GetPortfolioConstructionModel(language, bias, Resolution.Daily));
                Assert.That(exception.Message, Is.EqualTo("Long position must be allowed in RiskParityPortfolioConstructionModel."));
                return;
            }

            SetPortfolioConstruction(language, PortfolioBias.Long);

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

        [Test]
        public void NewtonMethodOptimizationTest()
        {
            var testOptimizer = new TestRiskParityPortfolioOptimizer();

            Func<double[], double> objective = (x) => 1/2 * Matrix.Dot(Matrix.Dot(x, _covariance1), x) - Matrix.Dot(_expectedReturn, x);
            Func<double[], double[]> jacobian = (x) => Elementwise.Subtract(Matrix.Dot(_covariance1, x), _expectedReturn);
            Func<double[], double[,]> hessian = (x) => _covariance1;

            var result = testOptimizer.TestNewtonMethodOptimization(1, objective, jacobian, hessian);
            Assert.AreEqual(new double[]{1d}, result);

            var exception = Assert.Throws<ArgumentException>(() => testOptimizer.TestNewtonMethodOptimization(0, objective, jacobian, hessian));
            Assert.That(exception.Message, Is.EqualTo("Argument \"numOfVar\" must be a positive integer between 1 and 1000"));

            exception = Assert.Throws<ArgumentException>(() => testOptimizer.TestNewtonMethodOptimization(2000, objective, jacobian, hessian));
            Assert.That(exception.Message, Is.EqualTo("Argument \"numOfVar\" must be a positive integer between 1 and 1000"));

            result = testOptimizer.TestNewtonMethodOptimization(2, objective, jacobian, hessian);
            result = result.Select(x => Math.Round(x, 2)).ToArray();
            Assert.AreEqual(_expectedResult1, result);
            
            objective = (x) => 1/2 * Matrix.Dot(Matrix.Dot(x, _covariance2), x) - Matrix.Dot(_expectedReturn, x);
            jacobian = (x) => Elementwise.Subtract(Matrix.Dot(_covariance2, x), _expectedReturn);
            hessian = (x) => _covariance2;

            result = testOptimizer.TestNewtonMethodOptimization(2, objective, jacobian, hessian);
            result = result.Select(x => Math.Round(x, 2)).ToArray();
            Assert.AreEqual(_expectedResult2, result);
            
            objective = (x) => 1/2 * Matrix.Dot(Matrix.Dot(x, _covariance3), x) - Matrix.Dot(_expectedReturn, x);
            jacobian = (x) => Elementwise.Subtract(Matrix.Dot(_covariance3, x), _expectedReturn);
            hessian = (x) => _covariance3;

            result = testOptimizer.TestNewtonMethodOptimization(2, objective, jacobian, hessian);
            result = result.Select(x => Math.Round(x, 2)).ToArray();
            Assert.AreEqual(_expectedResult3, result);
            
            objective = (x) => 1/2 * Matrix.Dot(Matrix.Dot(x, _covariance4), x) - Matrix.Dot(_expectedReturn, x);
            jacobian = (x) => Elementwise.Subtract(Matrix.Dot(_covariance4, x), _expectedReturn);
            hessian = (x) => _covariance4;

            result = testOptimizer.TestNewtonMethodOptimization(2, objective, jacobian, hessian);
            result = result.Select(x => Math.Round(x, 2)).ToArray();
            Assert.AreEqual(_expectedResult4, result);
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

        private class TestRiskParityPortfolioOptimizer : RiskParityPortfolioOptimizer
        {
            public TestRiskParityPortfolioOptimizer()
                : base()
            {
            }

            public double[] TestNewtonMethodOptimization(int numOfVar, Func<double[], double> objective, Func<double[], double[]> gradient, Func<double[], double[,]> hessian)
            {
                return base.NewtonMethodOptimization(numOfVar, objective, gradient, hessian);
            }
        }
    }
}