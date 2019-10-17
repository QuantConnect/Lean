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

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class BlackLittermanOptimizationPortfolioConstructionModelTests
    {
        private QCAlgorithm _algorithm;
        private Insight[] _view1Insights;
        private Insight[] _view2Insights;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _algorithm = new QCAlgorithm();
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

            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, _view1Insights);

            Assert.AreEqual(expectedTargets.Count(), actualTargets.Count());

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

            var insights = _view1Insights.Concat(_view2Insights);
            var actualTargets = _algorithm.PortfolioConstruction.CreateTargets(_algorithm, insights.ToArray());

            Assert.AreEqual(expectedTargets.Count(), actualTargets.Count());

            foreach (var expected in expectedTargets)
            {
                var actual = actualTargets.FirstOrDefault(x => x.Symbol == expected.Symbol);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Quantity, actual.Quantity);
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
            return insight;
        }

        private void SetPortfolioConstruction(Language language)
        {
            _algorithm.SetPortfolioConstruction(new BLOPCM(new UnconstrainedMeanVariancePortfolioOptimizer()));
            if (language == Language.Python)
            {
                try
                {
                    using (Py.GIL())
                    {
                        var name = nameof(BLOPCM);
                        var instance = PythonEngine.ModuleFromString(name, GetPythonBLOPCM()).GetAttr(name).Invoke();
                        var model = new PortfolioConstructionModelPythonWrapper(instance);
                        _algorithm.SetPortfolioConstruction(model);
                    }
                }
                catch (Exception e)
                {
                    Assert.Ignore(e.Message);
                }
            }

            var changes = SecurityChanges.Added(_algorithm.Securities.Values.ToList().ToArray());
            _algorithm.PortfolioConstruction.OnSecuritiesChanged(_algorithm, changes);
        }

        private void SetUtcTime(DateTime dateTime)
        {
            _algorithm.SetDateTime(dateTime.ConvertToUtc(_algorithm.TimeZone));
        }

        private class BLOPCM : BlackLittermanOptimizationPortfolioConstructionModel
        {
            public BLOPCM(IPortfolioOptimizer optimizer)
                : base(optimizer: optimizer)
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

            public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {

            }
        }

        private string GetPythonBLOPCM()
        {
            return @"import os, sys
sys.path.append(os.getcwd())

from clr import AddReference
AddReference('QuantConnect.Common')
from QuantConnect import *

from Portfolio.BlackLittermanOptimizationPortfolioConstructionModel import BlackLittermanOptimizationPortfolioConstructionModel
from Portfolio.UnconstrainedMeanVariancePortfolioOptimizer import UnconstrainedMeanVariancePortfolioOptimizer
import numpy as np
import pandas as pd

def GetSymbol(ticker):
    return str(Symbol.Create(ticker, SecurityType.Equity, Market.USA))

class BLOPCM(BlackLittermanOptimizationPortfolioConstructionModel):

    def __init__(self):
        super().__init__(optimizer = UnconstrainedMeanVariancePortfolioOptimizer())

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

    def OnSecuritiesChanged(self, algorithm, changes):
        pass";
        }
    }
}