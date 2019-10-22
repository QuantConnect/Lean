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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Tests.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmRegisterIndicatorTests
    {
        private Symbol _spy;
        private QCAlgorithm _algorithm;
        private IEnumerable<Type> _indicatorTestsTypes;

        [SetUp]
        public void Setup()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _spy = _algorithm.AddEquity("SPY").Symbol;

            _indicatorTestsTypes =
                from type in GetType().Assembly.GetTypes()
                where type.IsPublic && !type.IsAbstract
                where
                   typeof(CommonIndicatorTests<TradeBar>).IsAssignableFrom(type) ||
                   typeof(CommonIndicatorTests<IBaseDataBar>).IsAssignableFrom(type) ||
                   typeof(CommonIndicatorTests<IndicatorDataPoint>).IsAssignableFrom(type)
                select type;
        }

        [Test]
        public void RegistersIndicatorProperly()
        {
            var expected = 0;

            foreach (var type in _indicatorTestsTypes)
            {
                var indicatorTest = Activator.CreateInstance(type);
                if (indicatorTest is CommonIndicatorTests<IndicatorDataPoint>)
                {
                    var indicator = (indicatorTest as CommonIndicatorTests<IndicatorDataPoint>).Indicator;
                    Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute, Field.Close));
                    expected++;
                }
                else if (indicatorTest is CommonIndicatorTests<IBaseDataBar>)
                {
                    var indicator = (indicatorTest as CommonIndicatorTests<IBaseDataBar>).Indicator;
                    Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute));
                    expected++;
                }
                else if (indicatorTest is CommonIndicatorTests<TradeBar>)
                {
                    var indicator = (indicatorTest as CommonIndicatorTests<TradeBar>).Indicator;
                    Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute));
                    expected++;
                }
                else
                {
                    throw new NotSupportedException($"RegistersIndicatorProperlyPython(): Unsupported indicator data type: {indicatorTest.GetType()}");
                }
                var actual = _algorithm.SubscriptionManager.Subscriptions.FirstOrDefault().Consolidators.Count;
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void PlotAndRegistersIndicatorProperlyPython()
        {
            var expected = 0;
            PyObject indicator;

            foreach (var type in _indicatorTestsTypes)
            {
                var indicatorTest = Activator.CreateInstance(type);
                if (indicatorTest is CommonIndicatorTests<IndicatorDataPoint>)
                {
                    indicator = (indicatorTest as CommonIndicatorTests<IndicatorDataPoint>).GetIndicatorAsPyObject();
                }
                else if (indicatorTest is CommonIndicatorTests<IBaseDataBar>)
                {
                    indicator = (indicatorTest as CommonIndicatorTests<IBaseDataBar>).GetIndicatorAsPyObject();
                }
                else if (indicatorTest is CommonIndicatorTests<TradeBar>)
                {
                    indicator = (indicatorTest as CommonIndicatorTests<TradeBar>).GetIndicatorAsPyObject();
                }
                else
                {
                    throw new NotSupportedException($"RegistersIndicatorProperlyPython(): Unsupported indicator data type: {indicatorTest.GetType()}");
                }
                Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, indicator, Resolution.Minute));
                Assert.DoesNotThrow(() => _algorithm.Plot(_spy.Value, indicator));
                expected++;

                var actual = _algorithm.SubscriptionManager.Subscriptions.FirstOrDefault().Consolidators.Count;
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void RegisterPythonCustomIndicatorProperly()
        {
            const string code = @"
class GoodCustomIndicator :
    def __init__(self):
        self.IsReady = True
        self.Value = 0
    def Update(self, input):
        self.Value = input.Value
        return True
class BadCustomIndicator:
    def __init__(self):
        self.IsReady = True
        self.Value = 0
    def Updat(self, input):
        self.Value = input.Value
        return True";

            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(), code);

                var goodIndicator = module.GetAttr("GoodCustomIndicator").Invoke();
                Assert.DoesNotThrow(() => _algorithm.RegisterIndicator(_spy, goodIndicator, Resolution.Minute));

                var actual = _algorithm.SubscriptionManager.Subscriptions.FirstOrDefault().Consolidators.Count;
                Assert.AreEqual(1, actual);

                var badIndicator = module.GetAttr("BadCustomIndicator").Invoke();
                Assert.Throws<NotImplementedException>(() => _algorithm.RegisterIndicator(_spy, badIndicator, Resolution.Minute));
            }
        }

        [Test]
        public void RegistersIndicatorProperlyPythonScript()
        {
            const string code = @"
from clr import AddReference
AddReference('System')
AddReference('QuantConnect.Algorithm')
AddReference('QuantConnect.Indicators')
AddReference('QuantConnect.Common')
AddReference('QuantConnect.Lean.Engine')

from System import *
from QuantConnect import *
from QuantConnect.Securities import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *
from QuantConnect.Lean.Engine.DataFeeds import *

algo = QCAlgorithm()

marketHoursDatabase = MarketHoursDatabase.FromDataFolder()
symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder()
securityService =  SecurityService(algo.Portfolio.CashBook, marketHoursDatabase, symbolPropertiesDatabase, algo, RegisteredSecurityDataTypesProvider.Null, SecurityCacheProvider(algo.Portfolio))
algo.Securities.SetSecurityService(securityService)
dataManager = DataManager(None, UniverseSelection(algo, securityService), algo, algo.TimeKeeper, marketHoursDatabase, False, RegisteredSecurityDataTypesProvider.Null)
algo.SubscriptionManager.SetDataManager(dataManager)


forex = algo.AddForex('EURUSD', Resolution.Daily)
indicator = IchimokuKinkoHyo('EURUSD', 9, 26, 26, 52, 26, 26)
algo.RegisterIndicator(forex.Symbol, indicator, Resolution.Daily)";

            using (Py.GIL())
            {
                Assert.DoesNotThrow(() => PythonEngine.ModuleFromString("RegistersIndicatorProperlyPythonScript", code));
            }
        }
    }
}