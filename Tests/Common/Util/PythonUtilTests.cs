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

using System.Linq;
using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Util;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class PythonUtilTests
    {
        [Test]
        public void ConvertToSymbolsTest()
        {
            var expected = new List<Symbol>
            {
                Symbol.Create("AIG", SecurityType.Equity, Market.USA), 
                Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                Symbol.Create("IBM", SecurityType.Equity, Market.USA),
                Symbol.Create("GOOG", SecurityType.Equity, Market.USA)
            };

            using (Py.GIL())
            {
                // Test Python String
                var test1 = PythonUtil.ConvertToSymbols(new PyString("AIG"));
                Assert.IsTrue(typeof(List<Symbol>) == test1.GetType());
                Assert.AreEqual(expected.FirstOrDefault(), test1.FirstOrDefault());

                // Test Python List of Strings
                var list = (new List<string> {"AIG", "BAC", "IBM", "GOOG"}).ToPyList();
                var test2 = PythonUtil.ConvertToSymbols(list);
                Assert.IsTrue(typeof(List<Symbol>) == test2.GetType());
                Assert.IsTrue(test2.SequenceEqual(expected));

                // Test Python Symbol
                var test3 = PythonUtil.ConvertToSymbols(expected.FirstOrDefault().ToPython());
                Assert.IsTrue(typeof(List<Symbol>) == test3.GetType());
                Assert.AreEqual(expected.FirstOrDefault(), test3.FirstOrDefault());

                // Test Python List of Symbols
                var test4 = PythonUtil.ConvertToSymbols(expected.ToPyList());
                Assert.IsTrue(typeof(List<Symbol>) == test4.GetType());
                Assert.IsTrue(test4.SequenceEqual(expected));
            }
        }

        [TestCase("SyntaxError : invalid syntax (BasicTemplateAlgorithm.py, line 33)", "SyntaxError : invalid syntax (BasicTemplateAlgorithm.py, line 32)", 1)]
        [TestCase("SyntaxError : invalid syntax (BasicTemplateAlgorithm.py, line 1)", "SyntaxError : invalid syntax (BasicTemplateAlgorithm.py, line 32)", -31)]
        [TestCase("NameError : name 's' is not defined", "NameError : name 's' is not defined", -31)]
        public void ParsesPythonExceptionMessage(string expected, string original, int shift)
        {
            var originalShiftValue = PythonUtil.ExceptionLineShift;
            PythonUtil.ExceptionLineShift = shift;
            var result = PythonUtil.PythonExceptionMessageParser(original);

            PythonUtil.ExceptionLineShift = originalShiftValue;
            Assert.AreEqual(expected, result);
        }

        [TestCase(@"
  at Initialize
    s
===
   at Python.Runtime.PyObject.Invoke(PyTuple args in BasicTemplateAlgorithm.py: line 20
",
            @"   File ""D:/QuantConnect/MyLean/Lean/Algorithm.Python\BasicTemplateAlgorithm.py"", line 30, in Initialize
    s
===
   at Python.Runtime.PyObject.Invoke(PyTuple args, PyDict kw)
   at Python.Runtime.PyObject.InvokeMethod(String name, PyTuple args, PyDict kw)
   at Python.Runtime.PyObject.TryInvokeMember(InvokeMemberBinder binder, Object[] args, Object& result)
   at CallSite.Target(Closure , CallSite , Object )
   at System.Dynamic.UpdateDelegates.UpdateAndExecuteVoid1[T0](CallSite site, T0 arg0)
   at QuantConnect.AlgorithmFactory.Python.Wrappers.AlgorithmPythonWrapper.Initialize() in D:\QuantConnect\MyLean\Lean\AlgorithmFactory\Python\Wrappers\AlgorithmPythonWrapper.cs:line 528
   at QuantConnect.Lean.Engine.Setup.BacktestingSetupHandler.<>c__DisplayClass27_0.<Setup>b__0() in D:\QuantConnect\MyLean\Lean\Engine\Setup\BacktestingSetupHandler.cs:line 186",
            -10)]
        [TestCase(@"
  at Initialize
    self.SetEndDate(201)
===
   at Python.Runtime.PyObject.Invoke(PyTuple args in BasicTemplateAlgorithm.py: line 40
",
            @"  File ""D:/QuantConnect/MyLean/Lean/Algorithm.Python\BasicTemplateAlgorithm.py"", line 30, in Initialize
    self.SetEndDate(201)
===
   at Python.Runtime.PyObject.Invoke(PyTuple args, PyDict kw)
   at Python.Runtime.PyObject.InvokeMethod(String name, PyTuple args, PyDict kw)
   at Python.Runtime.PyObject.TryInvokeMember(InvokeMemberBinder binder, Object[] args, Object& result)
   at CallSite.Target(Closure , CallSite , Object )
   at System.Dynamic.UpdateDelegates.UpdateAndExecuteVoid1[T0](CallSite site, T0 arg0)
   at QuantConnect.AlgorithmFactory.Python.Wrappers.AlgorithmPythonWrapper.Initialize() in D:\QuantConnect\MyLean\Lean\AlgorithmFactory\Python\Wrappers\AlgorithmPythonWrapper.cs:line 528
   at QuantConnect.Lean.Engine.Setup.BacktestingSetupHandler.<>c__DisplayClass27_0.<Setup>b__0() in D:\QuantConnect\MyLean\Lean\Engine\Setup\BacktestingSetupHandler.cs:line 186",
            10)]
        public void ParsesPythonExceptionStackTrace(string expected, string original, int shift)
        {
            var originalShiftValue = PythonUtil.ExceptionLineShift;
            PythonUtil.ExceptionLineShift = shift;
            var result = PythonUtil.PythonExceptionStackParser(original);

            PythonUtil.ExceptionLineShift = originalShiftValue;
            Assert.AreEqual(expected, result);
        }
    }
}
