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
using System.Collections.Generic;
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
        private List<double> _simplexTestArray;
        private double[] _simplexExpectedArray1, _simplexExpectedArray2;

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

            _simplexTestArray = new List<double> {0.2d, 0.5d, 0.4d, -0.1d, 0d};
            _simplexExpectedArray1 = new double[] {1d/6, 7d/15, 11d/30, 0d, 0d};
            _simplexExpectedArray2 = new double[] {0d, 0.3d, 0.2d, 0d, 0d};
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void DoesNotReturnTargetsIfSecurityPriceIsZero(Language language)
        {
            _algorithm.AddEquity(Symbols.SPY.Value);
            _algorithm.SetDateTime(DateTime.MinValue.ConvertToUtc(_algorithm.TimeZone));

            SetPortfolioConstruction(language, PortfolioBias.Long);

            var insights = new[] { new Insight(_nowUtc, Symbols.SPY, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, null, null) };
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights);

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
                    throwsConstraint.And.Message.EqualTo("Long position must be allowed in MeanReversionPortfolioConstructionModel."));
                return;
            }

            var targets = GeneratePortfolioTargets(language, InsightDirection.Up, InsightDirection.Up, 1, 1);
            foreach (var target in targets)
            {
                if (target.Quantity == 0)
                {
                    continue;
                }
                Assert.AreEqual(Math.Sign((int)bias), Math.Sign(target.Quantity));
            }
        }

        [TestCase(Language.CSharp, InsightDirection.Up, InsightDirection.Up, null, null, 47, 47)]
        [TestCase(Language.Python, InsightDirection.Up, InsightDirection.Up, null, null, 47, 47)]
        [TestCase(Language.CSharp, InsightDirection.Up, InsightDirection.Up, 0, 0, 47, 47)]
        [TestCase(Language.Python, InsightDirection.Up, InsightDirection.Up, 0, 0, 47, 47)]
        [TestCase(Language.CSharp, InsightDirection.Up, InsightDirection.Up, 1, -0.5, 31, 63)]
        [TestCase(Language.Python, InsightDirection.Up, InsightDirection.Up, 1, -0.5, 31, 63)]
        [TestCase(Language.CSharp, InsightDirection.Up, InsightDirection.Down, 1, 0.5, 31, 63)]
        [TestCase(Language.Python, InsightDirection.Up, InsightDirection.Down, 1, 0.5, 31, 63)]
        [TestCase(Language.CSharp, InsightDirection.Up, InsightDirection.Up, 0, -0.5, 47, 47)]
        [TestCase(Language.Python, InsightDirection.Up, InsightDirection.Up, 0, -0.5, 47, 47)]
        [TestCase(Language.CSharp, InsightDirection.Up, InsightDirection.Up, 0, 1, 94, 0)]
        [TestCase(Language.Python, InsightDirection.Up, InsightDirection.Up, 0, 1, 94, 0)]
        [TestCase(Language.CSharp, InsightDirection.Up, InsightDirection.Up, 0.5, -1, 47, 47)]
        [TestCase(Language.Python, InsightDirection.Up, InsightDirection.Up, 0.5, -1, 47, 47)]
        public void CorrectWeightings(Language language,
                                      InsightDirection direction1,
                                      InsightDirection direction2,
                                      double? magnitude1,
                                      double? magnitude2,
                                      decimal expectedQty1,
                                      decimal expectedQty2)
        {
            var targets = GeneratePortfolioTargets(language, direction1, direction2, magnitude1, magnitude2);
            var quantities = targets.ToDictionary(target => {
                QuantConnect.Logging.Log.Debug($"{target.Symbol}: {target.Quantity}");
                return target.Symbol.Value;
            },
            target => target.Quantity);

            Assert.AreEqual(expectedQty1, quantities["AAPL"]);
            Assert.AreEqual(expectedQty2, quantities.ContainsKey("SPY") ? quantities["SPY"] : 0);
        }

        [Test]
        public void CumulativeSum()
        {
            var list = new List<double>{1.1d, 2.5d, 0.7d, 13.6d, -5.2d, 3.9d, -1.6d};
            var expected = new List<double>{1.1d, 3.6d, 4.3d, 17.9d, 12.7d, 16.6d, 15.0d};

            var result = MeanReversionPortfolioConstructionModel.CumulativeSum(list)
                .Select(x => Math.Round(x, 1));
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetPriceRelatives()
        {
            var model = new TestMeanReversionPortfolioConstructionModel();
            SetPortfolioConstruction(Language.CSharp, PortfolioBias.Long, model);

            var aapl = _algorithm.AddEquity("AAPL");
            var spy = _algorithm.AddEquity("SPY");

            var insights = new List<Insight>
            {
                new Insight(_nowUtc, aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, null, null),
                new Insight(_nowUtc, spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, null, null),
            };
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, SecurityChangesTests.AddedNonInternal(aapl, spy));

            var history = _algorithm.History<TradeBar>(new[] {aapl.Symbol, spy.Symbol}, 2, Resolution.Daily);
            var aaplHist = history.Select(slice => slice[aapl.Symbol].Close);
            var spyHist = history.Select(slice => slice[spy.Symbol].Close);
            var aaplRelative = (double) (aaplHist.Last() / aaplHist.Average());
            var spyRelative = (double) (spyHist.Last() / spyHist.Average());

            var result = model.TestGetPriceRelatives(insights).Select(x => Math.Round(x, 8)).ToArray();
            var expected = new double[] {aaplRelative, spyRelative};
            expected = expected.Select(x => Math.Round(x, 8)).ToArray();
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetPriceRelativesPython()
        {
            SetPortfolioConstruction(Language.Python, PortfolioBias.Long);

            var aapl = _algorithm.AddEquity("AAPL");
            var spy = _algorithm.AddEquity("SPY");

            var insights = new List<Insight>
            {
                new Insight(_nowUtc, aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, null, null),
                new Insight(_nowUtc, spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, null, null),
            };

            var history = _algorithm.History<TradeBar>(new[] {aapl.Symbol, spy.Symbol}, 2, Resolution.Daily);
            var aaplHist = history.Select(slice => slice[aapl.Symbol].Close);
            var spyHist = history.Select(slice => slice[spy.Symbol].Close);
            var aaplRelative = (double) (aaplHist.Last() / aaplHist.Average());
            var spyRelative = (double) (spyHist.Last() / spyHist.Average());

            using (Py.GIL())
            {
                const string name = nameof(MeanReversionPortfolioConstructionModel);
                var model = Py.Import(name).GetAttr(name).Invoke(((int)Resolution.Daily).ToPython(), ((int)PortfolioBias.LongShort).ToPython(), 1.ToPython(), 2.ToPython());
                model.InvokeMethod("OnSecuritiesChanged", _algorithm.ToPython(), SecurityChangesTests.AddedNonInternal(aapl, spy).ToPython());

                var result = PyList.AsList(model.InvokeMethod("GetPriceRelatives", insights.ToPython()));
                var resultArray = result.Select(x => Math.Round(Convert.ToDouble(x), 8)).ToArray();
                var expected = new double[] {aaplRelative, spyRelative};
                expected = expected.Select(x => Math.Round(x, 8)).ToArray();
                Assert.AreEqual(expected, resultArray);
            }
        }

        [Test]
        public void GetPriceRelativesWithInsightMagnitude()
        {
            var model = new TestMeanReversionPortfolioConstructionModel();
            SetPortfolioConstruction(Language.CSharp, PortfolioBias.Long, model);

            var aapl = _algorithm.AddEquity("AAPL");
            var spy = _algorithm.AddEquity("SPY");

            var insights = new List<Insight>
            {
                new Insight(_nowUtc, aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, 1, null),
                new Insight(_nowUtc, spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, -0.5, null),
            };
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, SecurityChangesTests.AddedNonInternal(aapl, spy));

            var result = model.TestGetPriceRelatives(insights);
            var expected = new double[] {2d, 0.5d};
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetPriceRelativesWithInsightMagnitudePython()
        {
            SetPortfolioConstruction(Language.Python, PortfolioBias.Long);

            var aapl = _algorithm.AddEquity("AAPL");
            var spy = _algorithm.AddEquity("SPY");

            var insights = new List<Insight>
            {
                new Insight(_nowUtc, aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, 1, null),
                new Insight(_nowUtc, spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Up, -0.5, null),
            };

            using (Py.GIL())
            {
                const string name = nameof(MeanReversionPortfolioConstructionModel);
                var model = Py.Import(name).GetAttr(name).Invoke(((int)Resolution.Daily).ToPython());
                model.InvokeMethod("OnSecuritiesChanged", _algorithm.ToPython(), SecurityChangesTests.AddedNonInternal(aapl, spy).ToPython());

                var result = PyList.AsList(model.InvokeMethod("GetPriceRelatives", insights.ToPython()));
                var resultArray = result.Select(x => Convert.ToDouble(x)).ToArray();
                var expected = new double[] {2d, 0.5d};
                Assert.AreEqual(expected, resultArray);
            }
        }

        [TestCase(1)]
        [TestCase(0.5)]
        [TestCase(0)]
        [TestCase(-0.5)]
        public void SimplexProjection(double regulator)
        {
            if (regulator <= 0)
            {
                var exception = Assert.Throws<ArgumentException>(() => MeanReversionPortfolioConstructionModel.SimplexProjection(_simplexTestArray, regulator));
                Assert.That(exception.Message, Is.EqualTo("Total must be > 0 for Euclidean Projection onto the Simplex."));
                return;
            }

            double[] expected;
            if (regulator == 1d)
            {
                expected = _simplexExpectedArray1;
            }
            else
            {
                expected = _simplexExpectedArray2;
            }
            expected = expected.Select(x => Math.Round(x, 8)).ToArray();

            var result = MeanReversionPortfolioConstructionModel.SimplexProjection(_simplexTestArray, regulator);
            result = result.Select(x => Math.Round(x, 8)).ToArray();
            Assert.AreEqual(expected, result);
        }

        [TestCase(1)]
        [TestCase(0.5)]
        [TestCase(0)]
        [TestCase(-0.5)]
        public void SimplexProjectionPython(double regulator)
        {
            using (Py.GIL())
            {
                const string name = nameof(MeanReversionPortfolioConstructionModel);
                var model = Py.Import(name).GetAttr(name).Invoke(((int)Resolution.Daily).ToPython());

                if (regulator <= 0)
                {
                    Assert.That(() => model.InvokeMethod("SimplexProjection", _simplexTestArray.ToPython(), new PyFloat(regulator)),
                        Throws.InstanceOf<ClrBubbledException>()
                            .With.InnerException.InstanceOf<ArgumentException>()
                            .And.Message.EqualTo("Total must be > 0 for Euclidean Projection onto the Simplex."));
                    return;
                }

                double[] expected;
                if (regulator == 1d)
                {
                    expected = _simplexExpectedArray1;
                }
                else
                {
                    expected = _simplexExpectedArray2;
                }
                var expectedArray = expected.Select(x => Math.Round(x, 8)).ToArray();

                var result = PyList.AsList(model.InvokeMethod("SimplexProjection", _simplexTestArray.ToPython(), new PyFloat(regulator)));
                var resultArray = result.Select(x => Math.Round(Convert.ToDouble(x), 8)).ToArray();
                Assert.AreEqual(expectedArray, resultArray);
            }
        }

        private IEnumerable<IPortfolioTarget> GeneratePortfolioTargets(Language language, InsightDirection direction1, InsightDirection direction2, double? magnitude1, double? magnitude2)
        {
            SetPortfolioConstruction(language, PortfolioBias.Long);

            var aapl = _algorithm.AddEquity("AAPL");
            var spy = _algorithm.AddEquity("SPY");

            foreach (var equity in new[] { aapl, spy })
            {
                equity.SetMarketPrice(new Tick(_nowUtc, equity.Symbol, 10, 10));
            }

            var insights = new[]
            {
                new Insight(_nowUtc, aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, direction1, magnitude1, null),
                new Insight(_nowUtc, spy.Symbol, TimeSpan.FromDays(1), InsightType.Price, direction2, magnitude2, null),
            };
            _algorithm.Insights.AddRange(insights);
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, SecurityChangesTests.AddedNonInternal(aapl, spy));

            return _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights);
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
                return new MeanReversionPortfolioConstructionModel(resolution, bias, 1, 1, resolution);
            }

            using (Py.GIL())
            {
                const string name = nameof(MeanReversionPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name)
                    .Invoke(((int)resolution).ToPython(), ((int)bias).ToPython(), 1.ToPython(), 1.ToPython(), ((int)resolution).ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
        }

        private class TestMeanReversionPortfolioConstructionModel : MeanReversionPortfolioConstructionModel
        {
            public TestMeanReversionPortfolioConstructionModel()
                : base(Resolution.Daily, windowSize: 2)
            {
            }

            public double[] TestGetPriceRelatives(List<Insight> insights)
            {
                return base.GetPriceRelatives(insights);
            }
        }
    }
}
