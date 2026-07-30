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
using NUnit.Framework.Constraints;
using Python.Runtime;
using QuantConnect.Exceptions;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Exceptions
{
    [TestFixture]
    public class ModuleNotFoundPythonExceptionInterpreterTests
    {
        private PythonException _pythonException;

        [SetUp]
        public void Setup()
        {
            using (Py.GIL())
            {
                var module = Py.Import("Test_PythonExceptionInterpreter");
                dynamic algorithm = module.GetAttr("Test_PythonExceptionInterpreter").Invoke();

                try
                {
                    // from MissingClrNamespace.Distributions import Normal
                    algorithm.module_not_found();
                }
                catch (PythonException pythonException)
                {
                    _pythonException = pythonException;
                }
            }
        }

        [Test]
        [TestCase(typeof(Exception), ExpectedResult = false)]
        [TestCase(typeof(KeyNotFoundException), ExpectedResult = false)]
        [TestCase(typeof(DivideByZeroException), ExpectedResult = false)]
        [TestCase(typeof(InvalidOperationException), ExpectedResult = false)]
        [TestCase(typeof(PythonException), ExpectedResult = true)]
        public bool CanInterpretReturnsTrueForOnlyModuleNotFoundPythonExceptionType(Type exceptionType)
        {
            var exception = CreateExceptionFromType(exceptionType);
            return new ModuleNotFoundPythonExceptionInterpreter().CanInterpret(exception);
        }

        [Test]
        [TestCase(typeof(Exception), true)]
        [TestCase(typeof(KeyNotFoundException), true)]
        [TestCase(typeof(DivideByZeroException), true)]
        [TestCase(typeof(InvalidOperationException), true)]
        [TestCase(typeof(PythonException), false)]
        public void InterpretThrowsForNonModuleNotFoundPythonExceptionTypes(Type exceptionType, bool expectThrow)
        {
            var exception = CreateExceptionFromType(exceptionType);
            var interpreter = new ModuleNotFoundPythonExceptionInterpreter();
            var constraint = expectThrow ? (IResolveConstraint)Throws.Exception : Throws.Nothing;
            Assert.That(() => interpreter.Interpret(exception, NullExceptionInterpreter.Instance), constraint);
        }

        [Test]
        public void VerifyMessageContainsModuleNameAndAdvice()
        {
            var exception = CreateExceptionFromType(typeof(PythonException));
            var interpreter = StackExceptionInterpreter.CreateFromAssemblies();
            exception = interpreter.Interpret(exception, NullExceptionInterpreter.Instance);
            Assert.True(exception.Message.Contains("No module named 'MissingClrNamespace'"));
            Assert.True(exception.Message.Contains("use an equivalent Python package instead"));
        }

        [Test]
        [TestCase("FakeMissingAssembly", true)]
        [TestCase("FakeMissingAssembly99", true)]
        [TestCase("fake_missing_module", false)]
        [TestCase("fakemissingmodule", false)]
        [TestCase("FAKEMISSINGMODULE", false)]
        [TestCase("Fake_Missing_Module", false)]
        public void AddsDotNetAssemblyAdviceOnlyForCamelCasedModuleNames(string moduleName, bool expectDotNetAdvice)
        {
            PythonException pythonException = null;
            using (Py.GIL())
            {
                try
                {
                    Py.Import(moduleName);
                }
                catch (PythonException exception)
                {
                    pythonException = exception;
                }
            }

            Assert.IsNotNull(pythonException);

            var interpreter = new ModuleNotFoundPythonExceptionInterpreter();
            Assert.IsTrue(interpreter.CanInterpret(pythonException));

            var interpretedException = interpreter.Interpret(pythonException, NullExceptionInterpreter.Instance);
            StringAssert.Contains($"No module named '{moduleName}'", interpretedException.Message);
            StringAssert.Contains("ensure it is installed in the environment", interpretedException.Message);
            Assert.AreEqual(expectDotNetAdvice, interpretedException.Message.Contains(".NET assembly", StringComparison.Ordinal));
        }

        private Exception CreateExceptionFromType(Type type) => type == typeof(PythonException) ? _pythonException : (Exception)Activator.CreateInstance(type);
    }
}
