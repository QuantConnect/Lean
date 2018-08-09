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
    public class NoMethodMatchPythonExceptionInterpreterTests
    {
        private PythonException _pythonException;

        [TestFixtureSetUp]
        public void Setup()
        {
            using (Py.GIL())
            {
                var module = Py.Import("Test_PythonExceptionInterpreter");
                dynamic algorithm = module.GetAttr("Test_PythonExceptionInterpreter").Invoke();

                try
                {
                    // self.SetCash('SPY')
                    algorithm.no_method_match();
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
        public bool CanInterpretReturnsTrueForOnlyNoMethodMatchPythonExceptionType(Type exceptionType)
        {
            var exception = CreateExceptionFromType(exceptionType);
            return new NoMethodMatchPythonExceptionInterpreter().CanInterpret(exception);
        }

        [Test]
        [TestCase(typeof(Exception), true)]
        [TestCase(typeof(KeyNotFoundException), true)]
        [TestCase(typeof(DivideByZeroException), true)]
        [TestCase(typeof(InvalidOperationException), true)]
        [TestCase(typeof(PythonException), false)]
        public void InterpretThrowsForNonNoMethodMatchPythonExceptionTypes(Type exceptionType, bool expectThrow)
        {
            var exception = CreateExceptionFromType(exceptionType);
            var interpreter = new NoMethodMatchPythonExceptionInterpreter();
            var constraint = expectThrow ? (IResolveConstraint)Throws.Exception : Throws.Nothing;
            Assert.That(() => interpreter.Interpret(exception, NullExceptionInterpreter.Instance), constraint);
        }

        [Test]
        public void VerifyMessageContainsStackTraceInformation()
        {
            var exception = CreateExceptionFromType(typeof(PythonException));
            var assembly = typeof(PythonExceptionInterpreter).Assembly;
            var interpreter = StackExceptionInterpreter.CreateFromAssemblies(new[] { assembly });
            exception = interpreter.Interpret(exception, NullExceptionInterpreter.Instance);
            Assert.True(exception.Message.Contains("self.SetCash(\\'SPY\\')"));
        }

        private Exception CreateExceptionFromType(Type type) => type == typeof(PythonException) ? _pythonException : (Exception)Activator.CreateInstance(type);
    }
}