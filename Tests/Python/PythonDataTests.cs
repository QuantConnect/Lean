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
using System.IO;
using System.Linq;
using NodaTime;
using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Python;
using System.Collections.Generic;
using System.Reflection;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class PythonDataTests
    {
        [TestCase("value", "symbol")]
        [TestCase("Value", "Symbol")]
        public void ValueAndSymbol(string value, string symbol)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    $@"
from AlgorithmImports import *

class CustomDataTest(PythonData):
    def Reader(self, config, line, date, isLiveMode):
        result = CustomDataTest()
        result.{symbol} = config.Symbol
        result.{value} = 10
        result.time = datetime.strptime(""2022-05-05"", ""%Y-%m-%d"")
        result.end_time = datetime.strptime(""2022-05-15"", ""%Y-%m-%d"")
        return result");

                var data = GetDataFromModule(testModule);

                Assert.AreEqual(Symbols.SPY, data.Symbol);
                Assert.AreEqual(10, data.Value);
                Assert.AreEqual(Symbols.SPY, data.symbol);
                Assert.AreEqual(10, data.value);
            }
        }

        [TestCase("EndTime", "Time")]
        [TestCase("endtime", "Time")]
        [TestCase("end_time", "Time")]
        [TestCase("EndTime", "time")]
        [TestCase("endtime", "time")]
        [TestCase("end_time", "time")]
        public void TimeAndEndTimeCanBeSet(string endtime, string time)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    $@"
from AlgorithmImports import *

class CustomDataTest(PythonData):
    def Reader(self, config, line, date, isLiveMode):
        result = CustomDataTest()
        result.Symbol = config.Symbol
        result.Value = 10
        result.{time} = datetime.strptime(""2022-05-05"", ""%Y-%m-%d"")
        result.{endtime} = datetime.strptime(""2022-05-15"", ""%Y-%m-%d"")
        return result");

                var data = GetDataFromModule(testModule);

                Assert.AreEqual(new DateTime(2022, 5, 5), data.Time);
                Assert.AreEqual(new DateTime(2022, 5, 15), data.EndTime);
            }
        }

        [TestCase("EndTime")]
        [TestCase("endtime")]
        [TestCase("end_time")]
        public void OnlyEndTimeCanBeSet(string endtime)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    $@"
from AlgorithmImports import *

class CustomDataTest(PythonData):
    def Reader(self, config, line, date, isLiveMode):
        result = CustomDataTest()
        result.Symbol = config.Symbol
        result.Value = 10
        result.{endtime} = datetime.strptime(""2022-05-05"", ""%Y-%m-%d"")
        return result");

                var data = GetDataFromModule(testModule);

                Assert.AreEqual(new DateTime(2022, 5, 5), data.Time);
                Assert.AreEqual(new DateTime(2022, 5, 5), data.EndTime);
            }
        }

        [TestCase("Time")]
        [TestCase("time")]
        public void OnlyTimeCanBeSet(string time)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    $@"
from AlgorithmImports import *

class CustomDataTest(PythonData):
    def Reader(self, config, line, date, isLiveMode):
        result = CustomDataTest()
        result.Symbol = config.Symbol
        result.Value = 10
        result.{time} = datetime.strptime(""2022-05-05"", ""%Y-%m-%d"")
        return result");

                var data = GetDataFromModule(testModule);

                Assert.AreEqual(new DateTime(2022, 5, 5), data.Time);
                Assert.AreEqual(new DateTime(2022, 5, 5), data.EndTime);
            }
        }

        public class TestPythonData : PythonData
        {
            private static void Throw()
            {
                throw new Exception("TestPythonData.Throw()");
            }

            public override bool RequiresMapping()
            {
                Throw();
                return true;
            }

            public override bool IsSparseData()
            {
                Throw();
                return true;
            }

            public override Resolution DefaultResolution()
            {
                Throw();
                return Resolution.Daily;
            }

            public override List<Resolution> SupportedResolutions()
            {
                Throw();
                return new List<Resolution> { Resolution.Daily };
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                Throw();
                return new TestPythonData();
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                Throw();
                return new SubscriptionDataSource("test", SubscriptionTransportMedium.LocalFile);
            }
        }

        [TestCase("RequiresMapping")]
        [TestCase("IsSparseData")]
        [TestCase("DefaultResolution")]
        [TestCase("SupportedResolutions")]
        [TestCase("Reader")]
        [TestCase("GetSource")]
        public void CallsCSharpMethodsIfNotDefinedInPython(string methodName)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    $@"
from AlgorithmImports import *

from QuantConnect.Tests.Python import *

class CustomDataClass(PythonDataTests.TestPythonData):
    pass");

                var customDataClass = testModule.GetAttr("CustomDataClass");
                var type = Extensions.CreateType(customDataClass);
                var data = new PythonData(customDataClass());

                var args = Array.Empty<object>();
                var methodArgsType = Array.Empty<Type>();
                if (methodName.Equals("Reader", StringComparison.OrdinalIgnoreCase))
                {
                    var config = new SubscriptionDataConfig(type, Symbols.SPY, Resolution.Daily, DateTimeZone.Utc,
                        DateTimeZone.Utc, false, false, false, isCustom: true);
                    args = new object[] { config, "line", DateTime.MinValue, false };
                    methodArgsType = new[] { typeof(SubscriptionDataConfig), typeof(string), typeof(DateTime), typeof(bool) };
                }
                else if (methodName.Equals("GetSource", StringComparison.OrdinalIgnoreCase))
                {
                    var config = new SubscriptionDataConfig(type, Symbols.SPY, Resolution.Daily, DateTimeZone.Utc,
                        DateTimeZone.Utc, false, false, false, isCustom: true);
                    args = new object[] { config, DateTime.MinValue, false };
                    methodArgsType = new[] { typeof(SubscriptionDataConfig), typeof(DateTime), typeof(bool) };
                }

                var exception = Assert.Throws<TargetInvocationException>(() => typeof(PythonData).GetMethod(methodName, methodArgsType).Invoke(data, args));
                Assert.AreEqual($"TestPythonData.Throw()", exception.InnerException.Message);
            }
        }

        [Test]
        public void AdanosSentimentGetSourceIncludesApiKeyHeader()
        {
            using (Py.GIL())
            {
                dynamic module = LoadModuleFromFile("AdanosMarketSentimentAlgorithm");
                dynamic adanosSentimentType = module.GetAttr("AdanosSentiment");
                adanosSentimentType.configure("test-api-key", "news", 14);

                var data = new PythonData(adanosSentimentType());
                var type = Extensions.CreateType(adanosSentimentType);
                var config = new SubscriptionDataConfig(type, Symbols.AAPL, Resolution.Daily, DateTimeZone.Utc,
                    DateTimeZone.Utc, false, false, false, isCustom: true);

                var source = data.GetSource(config, new DateTime(2026, 4, 9), false);

                Assert.AreEqual(SubscriptionTransportMedium.Rest, source.TransportMedium);
                Assert.AreEqual("https://api.adanos.org/news/stocks/v1/stock/AAPL?days=14", source.Source);
                Assert.IsTrue(source.Headers.Any(entry => entry.Key == "X-API-Key" && entry.Value == "test-api-key"));
            }
        }

        [Test]
        public void AdanosSentimentReaderParsesStructuredPayload()
        {
            using (Py.GIL())
            {
                dynamic module = LoadModuleFromFile("AdanosMarketSentimentAlgorithm");
                dynamic adanosSentimentType = module.GetAttr("AdanosSentiment");
                adanosSentimentType.configure("test-api-key", "news", 30);

                var data = new PythonData(adanosSentimentType());
                var type = Extensions.CreateType(adanosSentimentType);
                var config = new SubscriptionDataConfig(type, Symbols.AAPL, Resolution.Daily, DateTimeZone.Utc,
                    DateTimeZone.Utc, false, false, false, isCustom: true);

                const string line = "{\"ticker\":\"AAPL\",\"found\":true,\"buzz_score\":67.5,\"mentions\":42," +
                                    "\"sentiment_score\":0.35,\"bullish_pct\":61,\"source_count\":8,\"trend\":\"rising\"," +
                                    "\"daily_trend\":[{\"date\":\"2026-04-08\",\"mentions\":12,\"sentiment_score\":0.22}]}";

                var result = (PythonData)data.Reader(config, line, new DateTime(2026, 4, 9), false);

                Assert.NotNull(result);
                Assert.AreEqual(Symbols.AAPL, result.Symbol);
                Assert.AreEqual(67.5m, result.Value);
                Assert.AreEqual(new DateTime(2026, 4, 8), result.Time);
                Assert.AreEqual(61, Convert.ToInt32(result["BullishPercent"]));
                Assert.AreEqual(42, Convert.ToInt32(result["ActivityCount"]));
                Assert.AreEqual(8, Convert.ToInt32(result["CoverageCount"]));
                Assert.AreEqual("rising", result.GetProperty("Trend"));
                Assert.AreEqual("AAPL", result.GetProperty("Ticker"));
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

        private static dynamic LoadModuleFromFile(string moduleName)
        {
            var filePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, $"../../../Algorithm.Python/{moduleName}.py"));
            dynamic importlibUtil = Py.Import("importlib.util");
            dynamic spec = importlibUtil.spec_from_file_location(moduleName, filePath);
            dynamic module = importlibUtil.module_from_spec(spec);
            spec.loader.exec_module(module);
            return module;
        }
    }
}
