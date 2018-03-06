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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Tests.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmRegisterIndicatorTests
    {
        private Symbol _spy;
        private QCAlgorithm _algorithm;
        private IEnumerable<IGrouping<Type, Type>> _indicatorTestsGroups;

        [SetUp]
        public void Setup()
        {
            _algorithm = new QCAlgorithm();
            _spy = _algorithm.AddEquity("SPY").Symbol;

            _indicatorTestsGroups =
                from type in GetType().Assembly.GetTypes()
                where type.IsPublic && !type.IsAbstract
                where
                   typeof(CommonIndicatorTests<TradeBar>).IsAssignableFrom(type) ||
                   typeof(CommonIndicatorTests<IBaseDataBar>).IsAssignableFrom(type) ||
                   typeof(CommonIndicatorTests<IndicatorDataPoint>).IsAssignableFrom(type)
                group type by type.BaseType.GetGenericArguments().FirstOrDefault();
        }

        [Test]
        public void RegistersIndicatorProperly()
        {
            var expected = 0;
            foreach (var group in _indicatorTestsGroups)
            {
                var key = group.Key;
                foreach (var type in group)
                {
                    var indicatorTest = Activator.CreateInstance(type);
                    if (key == typeof(IndicatorDataPoint))
                    {
                        var indicator = (indicatorTest as CommonIndicatorTests<IndicatorDataPoint>).Indicator;
                        Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute, Field.Close));
                        expected++;
                    }
                    else if (key == typeof(IBaseDataBar))
                    {
                        var indicator = (indicatorTest as CommonIndicatorTests<IBaseDataBar>).Indicator;
                        Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute));
                        expected++;
                    }
                    else if (key == typeof(TradeBar))
                    {
                        var indicator = (indicatorTest as CommonIndicatorTests<TradeBar>).Indicator;
                        Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute));
                        expected++;
                    }
                    var actual = _algorithm.SubscriptionManager.Subscriptions.FirstOrDefault().Consolidators.Count;
                    Assert.AreEqual(expected, actual);
                }
            }
        }

        [Test, Ignore]
        public void RegistersIndicatorProperlyPython()
        {
            var expected = 0;
            foreach (var group in _indicatorTestsGroups)
            {
                var key = group.Key;
                foreach (var type in group)
                {
                    var indicatorTest = Activator.CreateInstance(type);
                    if (key == typeof(IndicatorDataPoint))
                    {
                        using (Py.GIL())
                        {
                            var indicator = (indicatorTest as CommonIndicatorTests<IndicatorDataPoint>).Indicator.ToPython();
                            Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute));
                        }
                        expected++;
                    }
                    else if (key == typeof(IBaseDataBar))
                    {
                        using (Py.GIL())
                        {
                            var indicator = (indicatorTest as CommonIndicatorTests<IBaseDataBar>).Indicator.ToPython();
                            Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute));
                        }
                        expected++;
                    }
                    else if (key == typeof(TradeBar))
                    {
                        using (Py.GIL())
                        {
                            var indicator = (indicatorTest as CommonIndicatorTests<TradeBar>).Indicator.ToPython();
                            Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute));
                        }
                        expected++;
                    }
                    var actual = _algorithm.SubscriptionManager.Subscriptions.FirstOrDefault().Consolidators.Count;
                    Assert.AreEqual(expected, actual);
                }
            }
        }

        [Test, Ignore]
        public void RegisterPythonCustomIndicatorProperly()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString("x",
                    "class GoodCustomIndicator:\n" +
                    "    def __init__(self):\n" +
                    "        pass\n" +
                    "    def Update(self, input):\n" +
                    "        return input\n" +
                    "class BadCustomIndicator:\n" +
                    "    def __init__(self):\n" +
                    "        pass\n" +
                    "    def Updat(self, input):\n" +
                    "        return input");

                var goodIndicator = module.GetAttr("GoodCustomIndicator").Invoke();
                Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, goodIndicator, Resolution.Minute));

                var actual = _algorithm.SubscriptionManager.Subscriptions.FirstOrDefault().Consolidators.Count;
                Assert.AreEqual(1, actual);

                var badIndicator = module.GetAttr("BadCustomIndicator").Invoke();
                Assert.Throws<ArgumentException>(() => _algorithm.RegisterIndicator(_spy, badIndicator, Resolution.Minute));
            }
        }
    }
}