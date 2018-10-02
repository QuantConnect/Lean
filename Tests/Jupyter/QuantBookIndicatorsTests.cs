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
using System;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Jupyter
{
    [TestFixture]
    public class QuantBookIndicatorsTests
    {
        dynamic _module;

        [TestFixtureSetUp]
        public void Setup()
        {
            SymbolCache.Clear();
            MarketHoursDatabase.Reset();

            using (Py.GIL())
            {
                _module = Py.Import("Test_QuantBookIndicator");
            }
        }

        [Test]
        [TestCase(2013, 10, 11, SecurityType.Equity, "SPY")]
        [TestCase(2014, 5, 9, SecurityType.Forex, "EURUSD")]
        [TestCase(2016, 10, 9, SecurityType.Crypto, "BTCUSD")]
        public void QuantBookIndicatorTests(int year, int month, int day, SecurityType securityType, string symbol)
        {
            using (Py.GIL())
            {
                var startDate = new DateTime(year, month, day);
                var indicatorTest = _module.IndicatorTest(startDate, securityType, symbol);

                var endDate = startDate;
                startDate = endDate.AddYears(-1);

                // Tests a data point indicator
                var dfBB = indicatorTest.test_bollinger_bands(symbol, startDate, endDate, Resolution.Daily);
                Assert.IsTrue(GetDataFrameLength(dfBB) > 0);

                // Tests a bar indicator
                var dfATR = indicatorTest.test_average_true_range(symbol, startDate, endDate, Resolution.Daily);
                Assert.IsTrue(GetDataFrameLength(dfATR) > 0);

                if (securityType == SecurityType.Forex)
                {
                    return;
                }

                // Tests a trade bar indicator
                var dfOBV = indicatorTest.test_on_balance_volume(symbol, startDate, endDate, Resolution.Daily);
                Assert.IsTrue(GetDataFrameLength(dfOBV) > 0);
            }
        }

        private int GetDataFrameLength(dynamic df) => (int)(df.shape[0] as PyObject).AsManagedObject(typeof(int));
    }
}