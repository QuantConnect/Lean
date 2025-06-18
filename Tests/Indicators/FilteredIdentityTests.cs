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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    /// <summary>
    /// Test class for QuantConnect.Indicators.FilteredIdentity
    /// </summary>
    [TestFixture]
    public class FilteredIdentityTests
    {
        [TestCase("lambda")]
        [TestCase("function")]
        public void FilteredIdentityWorksWithPythonFilter(string filterType)
        {
            using (Py.GIL())
            {
                string filterCode = filterType == "lambda"
                    ? "filter = lambda x: x.Close > x.Open"
                    : "filter = filter";

                string functionCode = filterType == "function"
                    ? @"
def filter(data):
    return data.Close > data.Open
"
                    : "";

                var testModule = PyModule.FromString("TestFilteredIdentity",
                    $@"
from AlgorithmImports import *
from QuantConnect.Tests import *

{functionCode}

def test_filtered_identity():
    test = FilteredIdentity(Symbols.SPY, {filterCode})
    tradeBar1 = TradeBar()
    tradeBar1.Close = 100
    tradeBar1.Open = 50
    tradeBar2 = TradeBar()
    tradeBar2.Close = 20
    tradeBar2.Open = 50
    tradeBar3 = TradeBar()
    tradeBar3.Close = 300
    tradeBar3.Open = 50
    test.Update(tradeBar1)
    test.Update(tradeBar2)
    test.Update(tradeBar3)
    return test
");

                var test = testModule.GetAttr("test_filtered_identity").Invoke();
                var filteredIdentity = test.As<FilteredIdentity>();
                Assert.AreEqual(3, filteredIdentity.Samples);
                Assert.AreEqual(300, filteredIdentity.Current.Value);
                Assert.AreEqual(100, filteredIdentity.Previous.Value);
            }
        }
    }
}
