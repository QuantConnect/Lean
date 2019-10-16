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
 *
*/

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class AlgorithmPythonWrapperTests
    {
        private string _baseCode;

        [SetUp]
        public void Setup()
        {
            _baseCode = File.ReadAllText(Path.Combine("./RegressionAlgorithms", "Test_AlgorithmPythonWrapper.py"));
        }

        [Test]
        [TestCase("def OnEndOfDay(self): pass")]
        [TestCase("def OnEndOfDay(self, symbol): pass")]
        public void CallOnEndOfDayDoesNotThrow(string code)
        {
            // If we define either one or the other overload of OnEndOfDay.
            // the algorithm will not throw or log the error
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm(code);

                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay());
                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay(Symbols.SPY));
                Assert.Null(algorithm.RunTimeError);
            }
        }

        [Test]
        public void CallOnEndOfDayExceptionNoParameter()
        {
            // When we define OnEndOfDay without a parameter and it has an user error (divide by zero)
            // it doesn't throw and stop the algorithm, but set its RuntimeError
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm("def OnEndOfDay(self): 1/0");

                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay());
                Assert.NotNull(algorithm.RunTimeError);
            }
        }

        [TestCase("", false)]
        [TestCase("def OnMarginCall(self, orders): pass", true)]
        [TestCase("def OnMarginCall(self, orders): return orders", false)]
        public void OnMarginCall(string code, bool throws)
        {
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm(code);
                Assert.Null(algorithm.RunTimeError);

                var order = new SubmitOrderRequest(OrderType.Limit,
                        SecurityType.Base,
                        Symbol.Empty,
                        1,
                        1,
                        1,
                        DateTime.UtcNow,
                        "");
                if (throws)
                {
                    Assert.Throws<Exception>(() => algorithm.OnMarginCall(new List<SubmitOrderRequest> { order }));
                }
                else
                {
                    Assert.DoesNotThrow(() => algorithm.OnMarginCall(new List<SubmitOrderRequest> { order }));
                }
            }
        }

        [Test]
        public void CallOnEndOfDayExceptionWithParameter()
        {
            // When we define OnEndOfDay with the Symbol parameter and it has an user error (divide by zero)
            // it doesn't throw and stop the algorithm, but set its RuntimeError
            using (Py.GIL())
            {
                var algorithm = GetAlgorithm("def OnEndOfDay(self, symbol): 1/0");

                Assert.Null(algorithm.RunTimeError);
                Assert.DoesNotThrow(() => algorithm.OnEndOfDay(Symbols.SPY));
                Assert.NotNull(algorithm.RunTimeError);
            }
        }

        private AlgorithmPythonWrapper GetAlgorithm(string code)
        {
            code = $"{_baseCode}{Environment.NewLine}    {code}";

            using (Py.GIL())
            {
                 PythonEngine.ModuleFromString("Test_AlgorithmPythonWrapper", code);
                return new AlgorithmPythonWrapper("Test_AlgorithmPythonWrapper");
            }
        }
    }
}