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
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using DateTime = System.DateTime;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class PortfolioConstructionModelTests
    {
        [Test]
        public void NoRebalanceFunction()
        {
            var constructionModel = new TestPortfolioConstructionModel
            {
                RebalanceOnSecurityChanges = false
            };

            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 1), new Insight[0]));
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2019, 1, 1), new Insight[0]));

            var security = new Security(Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash(Currencies.USD, 1, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache());

            constructionModel.OnSecuritiesChanged(null, SecurityChanges.Added(security));
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 1), new Insight[0]));
            constructionModel.OnSecuritiesChanged(null, SecurityChanges.None);
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 1), new Insight[0]));

            constructionModel.RebalanceOnSecurityChanges = true;
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void RebalanceFunctionPeriodDue(Language language)
        {
            TestPortfolioConstructionModel constructionModel;
            if (language == Language.Python)
            {
                constructionModel = new TestPortfolioConstructionModel();
                using (Py.GIL())
                {
                    var func = PythonEngine.ModuleFromString("RebalanceFunc",
                        @"
from datetime import timedelta

def RebalanceFunc(time):
    return time + timedelta(days=1)").GetAttr("RebalanceFunc");
                    constructionModel.SetRebalancingFunc(func);
                }
            }
            else
            {
                constructionModel = new TestPortfolioConstructionModel(time => time.AddDays(1));
            }

            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 1), new Insight[0]));
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(
                new DateTime(2020, 1, 1, 23, 0, 0), new Insight[0]));

            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 2), new Insight[0]));

            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(
                new DateTime(2020, 1, 2, 1, 0, 0), new Insight[0]));
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void RebalanceFunctionSecurityChanges(Language language)
        {
            TestPortfolioConstructionModel constructionModel;
            if (language == Language.Python)
            {
                constructionModel = new TestPortfolioConstructionModel();
                using (Py.GIL())
                {
                    var func = PythonEngine.ModuleFromString("RebalanceFunc",
                        @"
from datetime import timedelta

def RebalanceFunc(time):
    return time + timedelta(days=1)").GetAttr("RebalanceFunc");
                    constructionModel.SetRebalancingFunc(func);
                }
            }
            else
            {
                constructionModel = new TestPortfolioConstructionModel(time => time.AddDays(1));
            }

            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(
                new DateTime(2020, 1, 2, 1, 0, 0), new Insight[0]));

            var security = new Security(Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash(Currencies.USD, 1, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache());

            constructionModel.OnSecuritiesChanged(null, SecurityChanges.Added(security));
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 2), new Insight[0]));
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 2), new Insight[0]));
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void RebalanceFunctionNewInsights(Language language)
        {
            TestPortfolioConstructionModel constructionModel;
            if (language == Language.Python)
            {
                constructionModel = new TestPortfolioConstructionModel();
                using (Py.GIL())
                {
                    var func = PythonEngine.ModuleFromString("RebalanceFunc",
                        @"
from datetime import timedelta

def RebalanceFunc(time):
    return time + timedelta(days=1)").GetAttr("RebalanceFunc");
                    constructionModel.SetRebalancingFunc(func);
                }
            }
            else
            {
                constructionModel = new TestPortfolioConstructionModel(time => time.AddDays(1));
            }

            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 1), new Insight[0]));

            var insights = new[] { Insight.Price(Symbols.SPY, Resolution.Daily, 1, InsightDirection.Down) };

            constructionModel.RebalanceOnInsightChanges = false;
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 1), insights));
            constructionModel.RebalanceOnInsightChanges = true;
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 1), insights));
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void RebalanceFunctionInsightExpiration(Language language)
        {
            TestPortfolioConstructionModel constructionModel;
            if (language == Language.Python)
            {
                constructionModel = new TestPortfolioConstructionModel();
                using (Py.GIL())
                {
                    var func = PythonEngine.ModuleFromString("RebalanceFunc",
                        @"
from datetime import timedelta

def RebalanceFunc(time):
    return time + timedelta(days=10)").GetAttr("RebalanceFunc");
                    constructionModel.SetRebalancingFunc(func);
                }
            }
            else
            {
                constructionModel = new TestPortfolioConstructionModel(time => time.AddDays(10));
            }

            constructionModel.SetNextExpiration(new DateTime(2020, 1, 2));
            constructionModel.RebalanceOnInsightChanges = false;
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 3), new Insight[0]));
            constructionModel.RebalanceOnInsightChanges = true;
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 3), new Insight[0]));
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void NoRebalanceFunction(Language language)
        {
            TestPortfolioConstructionModel constructionModel;
            if (language == Language.Python)
            {
                constructionModel = new TestPortfolioConstructionModel();
                using (Py.GIL())
                {
                    var func = PythonEngine.ModuleFromString(
                        "RebalanceFunc",
                        @"
from datetime import timedelta

def RebalanceFunc():
    return None"
                    ).GetAttr("RebalanceFunc");
                    constructionModel.SetRebalancingFunc(func.Invoke());
                }
            }
            else
            {
                constructionModel = new TestPortfolioConstructionModel();
            }

            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 1), new Insight[0]));
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 2), new Insight[0]));
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 3), new Insight[0]));
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void RebalanceFunctionNull(Language language)
        {
            TestPortfolioConstructionModel constructionModel;
            if (language == Language.Python)
            {
                constructionModel = new TestPortfolioConstructionModel();
                using (Py.GIL())
                {
                    var func = PythonEngine.ModuleFromString(
                        "RebalanceFunc",
                        @"
from datetime import timedelta

def RebalanceFunc(time):
    if time.day == 17:
        return time + timedelta(hours=1)
    if time.day == 18:
        return time
    return None"
                    ).GetAttr("RebalanceFunc");
                    constructionModel.SetRebalancingFunc(func);
                }
            }
            else
            {
                constructionModel = new TestPortfolioConstructionModel(
                    time =>
                    {
                        if (time.Day == 18)
                        {
                            return time;
                        }
                        if (time.Day == 17)
                        {
                            return time.AddHours(1);
                        }
                        return null;
                    });
            }

            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 1), new Insight[0]));
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(
                new DateTime(2020, 1, 1, 23, 0, 0), new Insight[0]));
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 2), new Insight[0]));
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(
                new DateTime(2020, 1, 2, 0, 0, 1), new Insight[0]));

            // day number '17' should trigger rebalance in the next hour
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 17), new Insight[0]));
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(
                new DateTime(2020, 1, 17, 0, 59, 59), new Insight[0]));
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(
                new DateTime(2020, 1, 17, 1, 0, 0), new Insight[0]));

            constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 20), new Insight[0]);
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 21), new Insight[0]));

            // day number '18' should trigger rebalance immediately
            Assert.IsTrue(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 18), new Insight[0]));
            Assert.IsFalse(constructionModel.IsRebalanceDueWrapper(new DateTime(2020, 1, 21), new Insight[0]));
        }

        class TestPortfolioConstructionModel : PortfolioConstructionModel
        {
            public TestPortfolioConstructionModel(Func<DateTime, DateTime> func = null)
                : base(func)
            {
            }
            public TestPortfolioConstructionModel(Func<DateTime, DateTime?> func)
                : base(func)
            {
            }

            public void SetRebalancingFunc(PyObject rebalancingFunc)
            {
                base.SetRebalancingFunc(rebalancingFunc);
            }

            public bool IsRebalanceDueWrapper(DateTime now, Insight[] insights)
            {
                return base.IsRebalanceDue(insights, now);
            }

            public void SetNextExpiration(DateTime nextExpiration)
            {
                InsightCollection.Add(
                    new Insight(Symbols.SPY, time => nextExpiration, InsightType.Price, InsightDirection.Down));
            }
        }
    }
}
