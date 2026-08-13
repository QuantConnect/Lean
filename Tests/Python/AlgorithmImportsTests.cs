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

using System;
using Python.Runtime;
using NUnit.Framework;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class AlgorithmImportsTests
    {
        private PyObject _module;

        [OneTimeSetUp]
        public void Setup()
        {
            using (Py.GIL())
            {
                _module = PyModule.FromString("AlgorithmImportsTests", @"
from AlgorithmImports import *
import builtins
import AlgorithmImports

def exception_is_the_python_builtin():
    return Exception is builtins.Exception

def except_clause_catches_python_exceptions():
    try:
        raise TypeError('expected')
    except Exception:
        return True
    except BaseException:
        return False

def except_clause_catches_clr_exceptions(action):
    try:
        action()
    except Exception:
        return True
    except BaseException:
        return False

def shadowed_builtin_names():
    return [name for name, value in vars(AlgorithmImports).items()
            if not name.startswith('_') and getattr(builtins, name, value) is not value]
");
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            using (Py.GIL())
            {
                _module.Dispose();
            }
        }

        [Test]
        public void ExceptionIsThePythonBuiltinException()
        {
            using (Py.GIL())
            {
                Assert.IsTrue(_module.GetAttr("exception_is_the_python_builtin").Invoke().As<bool>());
            }
        }

        [Test]
        public void ExceptClauseCatchesPythonExceptions()
        {
            using (Py.GIL())
            {
                Assert.IsTrue(_module.GetAttr("except_clause_catches_python_exceptions").Invoke().As<bool>());
            }
        }

        [Test]
        public void ExceptClauseCatchesClrExceptions()
        {
            using (Py.GIL())
            {
                Action action = () => throw new ArgumentException("thrown from C#");
                Assert.IsTrue(_module.GetAttr("except_clause_catches_clr_exceptions").Invoke(action.ToPython()).As<bool>());
            }
        }

        [Test]
        public void StarImportsDoNotShadowPythonBuiltins()
        {
            using (Py.GIL())
            {
                var shadowed = _module.GetAttr("shadowed_builtin_names").Invoke().As<string[]>();
                Assert.IsEmpty(shadowed, $"Python builtins shadowed by AlgorithmImports: {string.Join(", ", shadowed)}");
            }
        }
    }
}
