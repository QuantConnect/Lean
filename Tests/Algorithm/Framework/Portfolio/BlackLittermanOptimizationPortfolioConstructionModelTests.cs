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

using Accord.Math;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using System;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Tests.Engine.DataFeeds;
using System.Collections.Generic;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class BlackLittermanOptimizationPortfolioConstructionModelTests
    {
        private QCAlgorithm _algorithm;
        private Insight[] _view1Insights;
        private Insight[] _view2Insights;

        [SetUp]
        public void SetUp()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));

            SetUtcTime(new DateTime(2018, 8, 7));

            // Germany will outperform the other European markets by 5%
            _view1Insights = new[]
            {
                GetInsight("View 1", "AUS",  0),
                GetInsight("View 1", "CAN",  0),
                GetInsight("View 1", "FRA", -0.01475),
                GetInsight("View 1", "GER",  0.05000),
                GetInsight("View 1", "JAP",  0),
                GetInsight("View 1", "UK" , -0.03525),
                GetInsight("View 1", "USA",  0)
            };

            // Canadian Equities will outperform US equities by 3 %
            _view2Insights = new[]
            {
                GetInsight("View 2", "AUS",  0),
                GetInsight("View 2", "CAN",  0.03),
                GetInsight("View 2", "FRA",  0),
                GetInsight("View 2", "GER",  0),
                GetInsight("View 2", "JAP",  0),
                GetInsight("View 2", "UK" ,  0),
                GetInsight("View 2", "USA", -0.03)
            };

            foreach (var symbol in _view1Insights.Select(x => x.Symbol))
            {
                var security = GetSecurity(symbol, Resolution.Daily);
                security.SetMarketPrice(new Tick(_algorithm.Time, symbol, 1m, 1m));
                _algorithm.Securities.Add(symbol, security);
            }
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void EmptyInsightsReturnsEmptyTargets(Language language)
        {
            SetPortfolioConstruction(language);

            var insights = new Insight[0];
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights);

            Assert.AreEqual(0, actualTargets.Count());
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void OneViewTest(Language language)
        {
            SetPortfolioConstruction(language);

            // Add outdated insight to check if only the latest one was considered
            var outdatedInsight = GetInsight("View 1", "CAN", 0.05);
            outdatedInsight.GeneratedTimeUtc -= TimeSpan.FromHours(1);
            outdatedInsight.CloseTimeUtc -= TimeSpan.FromHours(1);

            // Results from http://www.blacklitterman.org/code/hl_py.html (View 1)
            var expectedTargets = new[]
            {
                PortfolioTarget.Percent(_algorithm, GetSymbol("AUS"), 0.0152381),
                PortfolioTarget.Percent(_algorithm, GetSymbol("CAN"), 0.02095238),
                PortfolioTarget.Percent(_algorithm, GetSymbol("FRA"), -0.03948465),
                PortfolioTarget.Percent(_algorithm, GetSymbol("GER"), 0.35410454),
                PortfolioTarget.Percent(_algorithm, GetSymbol("JAP"), 0.11047619),
                PortfolioTarget.Percent(_algorithm, GetSymbol("UK"), -0.09461989),
                PortfolioTarget.Percent(_algorithm, GetSymbol("USA"), 0.58571429)
            };

            var insights = _view1Insights.Concat(new[] { outdatedInsight }).ToArray();
            Clear();
            _algorithm.Insights.AddRange(insights);
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights);

            Assert.AreEqual(expectedTargets.Length, actualTargets.Count());

            foreach (var expected in expectedTargets)
            {
                var actual = actualTargets.FirstOrDefault(x => x.Symbol == expected.Symbol);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Quantity, actual.Quantity);
            }
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TwoViewsTest(Language language)
        {
            SetPortfolioConstruction(language);

            // Results from http://www.blacklitterman.org/code/hl_py.html (View 1+2)
            var expectedTargets = new[]
            {
                PortfolioTarget.Percent(_algorithm, GetSymbol("AUS"), 0.0152381),
                PortfolioTarget.Percent(_algorithm, GetSymbol("CAN"), 0.41863571),
                PortfolioTarget.Percent(_algorithm, GetSymbol("FRA"), -0.03409321),
                PortfolioTarget.Percent(_algorithm, GetSymbol("GER"), 0.33582847),
                PortfolioTarget.Percent(_algorithm, GetSymbol("JAP"), 0.11047619),
                PortfolioTarget.Percent(_algorithm, GetSymbol("UK"), -0.08173526),
                PortfolioTarget.Percent(_algorithm, GetSymbol("USA"), 0.18803095)
            };

            // Add outdated insight to check if only the latest one was considered
            var outdatedInsight = GetInsight("View 2", "USA", 0.05);
            outdatedInsight.GeneratedTimeUtc -= TimeSpan.FromHours(1);
            outdatedInsight.CloseTimeUtc -= TimeSpan.FromHours(1);

            var insights = _view1Insights.Concat(_view2Insights).Concat(new[] { outdatedInsight });
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights.ToArray());

            Assert.AreEqual(expectedTargets.Length, actualTargets.Count());

            foreach (var expected in expectedTargets)
            {
                var actual = actualTargets.FirstOrDefault(x => x.Symbol == expected.Symbol);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Quantity, actual.Quantity);
            }
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void OneViewDimensionTest(Language language)
        {
            SetPortfolioConstruction(language);

            if (language == Language.CSharp)
            {
                double[,] P;
                double[] Q;
                ((BLOPCM)_algorithm.PortfolioConstruction).TestTryGetViews(_view1Insights, out P, out Q);

                Assert.AreEqual(P.GetLength(0), 1);
                Assert.AreEqual(P.GetLength(1), 7);
                Assert.AreEqual(Q.GetLength(0), 1);

                return;
            }

            using (Py.GIL())
            {
                var name = nameof(BLOPCM);
                var instance = PyModule.FromString(name, GetPythonBLOPCM()).GetAttr(name).Invoke(((int)PortfolioBias.LongShort).ToPython());
                var result = PyList.AsList(instance.InvokeMethod("get_views", _view1Insights.ToPython()));
                Assert.AreEqual(result[0].Length(), 1);
                Assert.AreEqual(result[0][0].Length(), 7);
                Assert.AreEqual(result[1].Length(), 1);
            }
        }

        [Test]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void TwoViewsDimensionTest(Language language)
        {
            SetPortfolioConstruction(language);

            // Test if a symbol has no view in one of the source models
            var insights = _view1Insights.Concat(_view2Insights.Skip(1)).ToList();

            if (language == Language.CSharp)
            {
                double[,] P;
                double[] Q;
                ((BLOPCM)_algorithm.PortfolioConstruction).TestTryGetViews(insights, out P, out Q);

                Assert.AreEqual(P.GetLength(0), 2);
                Assert.AreEqual(P.GetLength(1), 7);
                Assert.AreEqual(Q.GetLength(0), 2);

                return;
            }

            using (Py.GIL())
            {
                var name = nameof(BLOPCM);
                var instance = PyModule.FromString(name, GetPythonBLOPCM()).GetAttr(name).Invoke(((int)PortfolioBias.LongShort).ToPython());
                var result = PyList.AsList(instance.InvokeMethod("get_views", insights.ToPython()));
                Assert.AreEqual(result[0].Length(), 2);
                Assert.AreEqual(result[0][0].Length(), 7);
                Assert.AreEqual(result[1].Length(), 2);
            }
        }

        [Test]
        [TestCase(Language.CSharp, 11, true)]
        [TestCase(Language.CSharp, -11, true)]
        [TestCase(Language.CSharp, 0.001d, true)]
        [TestCase(Language.CSharp, -0.001d, true)]
        [TestCase(Language.CSharp, 0.1, false)]
        [TestCase(Language.CSharp, -0.1, false)]
        [TestCase(Language.CSharp, 0.011d, false)]
        [TestCase(Language.CSharp, -0.011d, false)]
        [TestCase(Language.CSharp, 0, true)]
        [TestCase(Language.Python, 0, true)]
        [TestCase(Language.Python, 11, true)]
        [TestCase(Language.Python, -11, true)]
        [TestCase(Language.Python, 0.001d, true)]
        [TestCase(Language.Python, -0.001d, true)]
        [TestCase(Language.Python, 0.1, false)]
        [TestCase(Language.Python, -0.1, false)]
        [TestCase(Language.Python, 0.011d, false)]
        [TestCase(Language.Python, -0.011d, false)]
        public void IgnoresInsightsWithInvalidMagnitudeValue(Language language, double magnitude, bool expectZero)
        {
            SetPortfolioConstruction(language);
            _algorithm.Settings.MaxAbsolutePortfolioTargetPercentage = 10;
            _algorithm.Settings.MinAbsolutePortfolioTargetPercentage = 0.01m;
            Clear();

            var insights = new[]
            {
                GetInsight("View 1", "AUS", magnitude),
                GetInsight("View 1", "CAN", magnitude),
                GetInsight("View 1", "FRA", magnitude),
                GetInsight("View 1", "GER", magnitude),
                GetInsight("View 1", "JAP", magnitude),
                GetInsight("View 1", "UK" , magnitude),
                GetInsight("View 1", "USA", magnitude)
            };

            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights);

            if (expectZero)
            {
                Assert.AreEqual(0, actualTargets.Count());
            }
            else
            {
                Assert.AreNotEqual(0, actualTargets.Count());
            }
        }

        [TestCase(Language.CSharp, PortfolioBias.Long)]
        [TestCase(Language.Python, PortfolioBias.Long)]
        [TestCase(Language.CSharp, PortfolioBias.Short)]
        [TestCase(Language.Python, PortfolioBias.Short)]
        public void PortfolioBiasIsRespected(Language language, PortfolioBias bias)
        {
            SetPortfolioConstruction(language, bias);

            var insights = new[]
            {
                GetInsight("View 1", "AUS", -10.1),
                GetInsight("View 1", "CAN", -0.1),
                GetInsight("View 1", "FRA", 0.1),
                GetInsight("View 1", "GER", -0.1),
                GetInsight("View 1", "JAP", -0.1),
                GetInsight("View 1", "UK" , 0.1),
                GetInsight("View 1", "USA", -0.1)
            };

            var createdValidTarget = false;
            foreach (var target in _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights))
            {
                QuantConnect.Logging.Log.Trace($"{target.Symbol}: {target.Quantity}");
                if (target.Quantity == 0)
                {
                    continue;
                }

                createdValidTarget = true;
                Assert.AreEqual(Math.Sign((int)bias), Math.Sign(target.Quantity));
            }

            Assert.IsTrue(createdValidTarget);
        }

        [Test]
        public void NewSymbolPortfolioConstructionModelDoesNotThrow()
        {
            var algorithm = new QCAlgorithm();
            var timezone = algorithm.TimeZone;
            algorithm.SetDateTime(new DateTime(2018, 8, 7).ConvertToUtc(timezone));
            algorithm.SetPortfolioConstruction(new NewSymbolPortfolioConstructionModel());

            var spySymbol = Symbols.SPY;
            var spy = GetSecurity(spySymbol, Resolution.Daily);

            spy.SetMarketPrice(new Tick(algorithm.Time, spySymbol, 1m, 1m));
            algorithm.Securities.Add(spySymbol, spy);

            algorithm.PortfolioConstruction.OnSecuritiesChanged(algorithm, SecurityChangesTests.AddedNonInternal(spy));

            var insights = new[] { Insight.Price(spySymbol, Time.OneMinute, InsightDirection.Up, .1) };

            Assert.DoesNotThrow(() => algorithm.PortfolioConstruction.CreateTargets(algorithm, insights));

            algorithm.SetDateTime(algorithm.Time.AddDays(1));

            var aaplSymbol = Symbols.AAPL;
            var aapl = GetSecurity(spySymbol, Resolution.Daily);

            aapl.SetMarketPrice(new Tick(algorithm.Time, aaplSymbol, 1m, 1m));
            algorithm.Securities.Add(aaplSymbol, aapl);

            algorithm.PortfolioConstruction.OnSecuritiesChanged(algorithm, SecurityChangesTests.AddedNonInternal(aapl));

            insights = new[] { spySymbol, aaplSymbol }
                .Select(x => Insight.Price(x, Time.OneMinute, InsightDirection.Up, .1)).ToArray();

            Assert.DoesNotThrow(() => algorithm.PortfolioConstruction.CreateTargets(algorithm, insights));
        }

        private Security GetSecurity(Symbol symbol, Resolution resolution)
        {
            var timezone = _algorithm.TimeZone;
            var exchangeHours = SecurityExchangeHours.AlwaysOpen(timezone);
            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, resolution, timezone, timezone, true, false, false);
            return new Security(
                exchangeHours,
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private Symbol GetSymbol(string ticker) => Symbol.Create(ticker, SecurityType.Equity, Market.USA);

        private Insight GetInsight(string SourceModel, string ticker, double magnitude)
        {
            var period = Time.OneDay;
            var direction = (InsightDirection)Math.Sign(magnitude);
            var insight = Insight.Price(GetSymbol(ticker), period, direction, magnitude, sourceModel: SourceModel);
            insight.GeneratedTimeUtc = _algorithm.UtcTime;
            insight.CloseTimeUtc = _algorithm.UtcTime.Add(insight.Period);
            _algorithm.Insights.Add(insight);
            return insight;
        }

        private void SetPortfolioConstruction(Language language, PortfolioBias portfolioBias = PortfolioBias.LongShort)
        {
            _algorithm.SetPortfolioConstruction(new BLOPCM(new UnconstrainedMeanVariancePortfolioOptimizer(), portfolioBias));
            if (language == Language.Python)
            {
                try
                {
                    using (Py.GIL())
                    {
                        var name = nameof(BLOPCM);
                        var instance = PyModule.FromString(name, GetPythonBLOPCM()).GetAttr(name).Invoke(((int)portfolioBias).ToPython());
                        var model = new PortfolioConstructionModelPythonWrapper(instance);
                        _algorithm.SetPortfolioConstruction(model);
                    }
                }
                catch (Exception e)
                {
                    Assert.Ignore(e.Message);
                }
            }

            var changes = SecurityChangesTests.AddedNonInternal(_algorithm.Securities.Values.ToList().ToArray());
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, changes);
        }

        private void SetUtcTime(DateTime dateTime)
        {
            _algorithm.SetDateTime(dateTime.ConvertToUtc(_algorithm.TimeZone));
        }

        private class BLOPCM : BlackLittermanOptimizationPortfolioConstructionModel
        {
            public BLOPCM(IPortfolioOptimizer optimizer, PortfolioBias portfolioBias)
                : base(optimizer: optimizer, portfolioBias: portfolioBias)
            {
            }

            public override double[] GetEquilibriumReturns(double[,] returns, out double[,] Σ)
            {
                // Take the values from He & Litterman, 1999.
                var C = new[,]
                {
                    { 1.000, 0.488, 0.478, 0.515, 0.439, 0.512, 0.491 },
                    { 0.488, 1.000, 0.664, 0.655, 0.310, 0.608, 0.779 },
                    { 0.478, 0.664, 1.000, 0.861, 0.355, 0.783, 0.668 },
                    { 0.515, 0.655, 0.861, 1.000, 0.354, 0.777, 0.653 },
                    { 0.439, 0.310, 0.355, 0.354, 1.000, 0.405, 0.306 },
                    { 0.512, 0.608, 0.783, 0.777, 0.405, 1.000, 0.652 },
                    { 0.491, 0.779, 0.668, 0.653, 0.306, 0.652, 1.000 }
                };
                var σ = new[] { 0.160, 0.203, 0.248, 0.271, 0.210, 0.200, 0.187 };
                var w = new[] { 0.016, 0.022, 0.052, 0.055, 0.116, 0.124, 0.615 };
                var delta = 2.5;

                // Equilibrium covariance matrix
                Σ = Elementwise.Multiply(C, σ.Outer(σ));
                return w.Dot(Σ.Multiply(delta));
            }

            public bool TestTryGetViews(ICollection<Insight> insights, out double[,] P, out double[] Q)
            {
                return base.TryGetViews(insights, out P, out Q);
            }

            public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {

            }
        }

        private string GetPythonBLOPCM()
        {
            return @"
from AlgorithmImports import *

from Portfolio.BlackLittermanOptimizationPortfolioConstructionModel import BlackLittermanOptimizationPortfolioConstructionModel
from Portfolio.UnconstrainedMeanVariancePortfolioOptimizer import UnconstrainedMeanVariancePortfolioOptimizer

def GetSymbol(ticker):
    return str(Symbol.Create(ticker, SecurityType.Equity, Market.USA))

class BLOPCM(BlackLittermanOptimizationPortfolioConstructionModel):

    def __init__(self, portfolioBias):
        super().__init__(portfolio_bias = portfolioBias, optimizer = UnconstrainedMeanVariancePortfolioOptimizer())

    def get_equilibrium_return(self, returns):

        # Take the values from He & Litterman, 1999.
        weq = np.array([0.016, 0.022, 0.052, 0.055, 0.116, 0.124, 0.615])
        C = np.array([[ 1.000, 0.488, 0.478, 0.515, 0.439, 0.512, 0.491],
                       [0.488, 1.000, 0.664, 0.655, 0.310, 0.608, 0.779],
                       [0.478, 0.664, 1.000, 0.861, 0.355, 0.783, 0.668],
                       [0.515, 0.655, 0.861, 1.000, 0.354, 0.777, 0.653],
                       [0.439, 0.310, 0.355, 0.354, 1.000, 0.405, 0.306],
                       [0.512, 0.608, 0.783, 0.777, 0.405, 1.000, 0.652],
                       [0.491, 0.779, 0.668, 0.653, 0.306, 0.652, 1.000]])
        Sigma = np.array([0.160, 0.203, 0.248, 0.271, 0.210, 0.200, 0.187])
        refPi = np.array([0.039, 0.069, 0.084, 0.090, 0.043, 0.068, 0.076])
        assets= [GetSymbol(x) for x in ['AUS', 'CAN', 'FRA', 'GER', 'JAP', 'UK', 'USA']]
        delta = 2.5

        # Equilibrium covariance matrix
        V = np.multiply(np.outer(Sigma,Sigma), C)

        return weq.dot(V * delta), pd.DataFrame(V, columns=assets, index=assets)

    def on_securities_changed(self, algorithm, changes):
        pass";
        }

        private void Clear() => _algorithm.Insights.Clear(_algorithm.Securities.Keys.ToArray());

        private class NewSymbolPortfolioConstructionModel : BlackLittermanOptimizationPortfolioConstructionModel
        {
            private readonly Dictionary<Symbol, ReturnsSymbolData> _symbolDataDict = new Dictionary<Symbol, ReturnsSymbolData>();

            public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
            {
                // Updates the ReturnsSymbolData with insights
                foreach (var insight in insights)
                {
                    ReturnsSymbolData symbolData;
                    if (_symbolDataDict.TryGetValue(insight.Symbol, out symbolData))
                    {
                        symbolData.Add(algorithm.Time, .1m);
                    }
                }

                double[,] returns = null;
                Assert.DoesNotThrow(() => returns = _symbolDataDict.FormReturnsMatrix(insights.Select(x => x.Symbol)));

                // Calculate posterior estimate of the mean and uncertainty in the mean
                double[,] Σ;
                var Π = GetEquilibriumReturns(returns, out Σ);

                Assert.IsFalse(double.IsNaN(Π[0]));

                return Enumerable.Empty<PortfolioTarget>();
            }

            public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
                const int period = 2;
                var reference = algorithm.Time.AddDays(-period);

                foreach (var security in changes.AddedSecurities)
                {
                    var symbol = security.Symbol;
                    var symbolData = new ReturnsSymbolData(symbol, 1, period);

                    for (var i = 0; i <= period * 2; i++)
                    {
                        symbolData.Update(reference.AddDays(i), i);
                    }

                    _symbolDataDict[symbol] = symbolData;
                }
            }
        }
    }
}
