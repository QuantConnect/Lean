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
using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Python;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class PythonDataTests
    {
        [Test]
        public void TimeAndEndTimeCanBeSet()
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class CustomDataTest(PythonData):
    def Reader(self, config, line, date, isLiveMode):
        result = CustomDataTest()
        result.Symbol = config.Symbol
        result.Value = 10
        result.Time = datetime.strptime(""2022-05-05"", ""%Y-%m-%d"")
        result.EndTime = datetime.strptime(""2022-05-15"", ""%Y-%m-%d"")
        return result");

                var data = GetDataFromModule(testModule);

                Assert.AreEqual(new DateTime(2022, 5, 5), data.Time);
                Assert.AreEqual(new DateTime(2022, 5, 15), data.EndTime);
            }
        }

        [Test]
        public void OnlyEndTimeCanBeSet()
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class CustomDataTest(PythonData):
    def Reader(self, config, line, date, isLiveMode):
        result = CustomDataTest()
        result.Symbol = config.Symbol
        result.Value = 10
        result.EndTime = datetime.strptime(""2022-05-05"", ""%Y-%m-%d"")
        return result");

                var data = GetDataFromModule(testModule);

                Assert.AreEqual(new DateTime(2022, 5, 5), data.Time);
                Assert.AreEqual(new DateTime(2022, 5, 5), data.EndTime);
            }
        }

        [Test]
        public void OnlyTimeCanBeSet()
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

class CustomDataTest(PythonData):
    def Reader(self, config, line, date, isLiveMode):
        result = CustomDataTest()
        result.Symbol = config.Symbol
        result.Value = 10
        result.Time = datetime.strptime(""2022-05-05"", ""%Y-%m-%d"")
        return result");

                var data = GetDataFromModule(testModule);

                Assert.AreEqual(new DateTime(2022, 5, 5), data.Time);
                Assert.AreEqual(new DateTime(2022, 5, 5), data.EndTime);
            }
        }

        private static BaseData GetDataFromModule(dynamic testModule)
        {
            var type = Extensions.CreateType(testModule.GetAttr("CustomDataTest"));
            var customDataTest = new PythonData(testModule.GetAttr("CustomDataTest")());
            var config = new SubscriptionDataConfig(type, Symbols.SPY, Resolution.Daily, DateTimeZone.Utc,
                DateTimeZone.Utc, false, false, false, isCustom: true);
            return customDataTest.Reader(config, "something", DateTime.UtcNow, false);
        }
    }
}
