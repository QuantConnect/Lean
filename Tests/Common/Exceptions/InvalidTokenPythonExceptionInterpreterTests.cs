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
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Python.Runtime;
using QuantConnect.Exceptions;

namespace QuantConnect.Tests.Common.Exceptions
{
    [TestFixture]
    public class InvalidTokenPythonExceptionInterpreterTests
    {
        private PythonException _pythonException;

        [SetUp]
        public void Setup()
        {
            using (Py.GIL())
            {
                try
                {
                    // importing a module with syntax error 'x = 01' will throw
                    PyModule.FromString(Guid.NewGuid().ToString(), "x = 01");
                }
                catch (PythonException pythonException)
                {
                    _pythonException = pythonException;
                }
            }
        }

        [TestCase(typeof(Exception), false)]
        [TestCase(typeof(KeyNotFoundException), false)]
        [TestCase(typeof(DivideByZeroException), false)]
        [TestCase(typeof(InvalidOperationException), false)]
        [TestCase(typeof(PythonException), true)]
        public void CanInterpretReturnsTrueForOnlyInvalidTokenPythonExceptionType(
            Type exceptionType,
            bool expectedResult
        )
        {
            var exception = CreateExceptionFromType(exceptionType);
            Assert.AreEqual(
                expectedResult,
                new InvalidTokenPythonExceptionInterpreter().CanInterpret(exception),
                $"Unexpected response for: {exception}"
            );
        }

        [TestCase(typeof(Exception), true)]
        [TestCase(typeof(KeyNotFoundException), true)]
        [TestCase(typeof(DivideByZeroException), true)]
        [TestCase(typeof(InvalidOperationException), true)]
        [TestCase(typeof(PythonException), false)]
        public void InterpretThrowsForNonInvalidTokenPythonExceptionTypes(
            Type exceptionType,
            bool expectThrow
        )
        {
            var exception = CreateExceptionFromType(exceptionType);
            var interpreter = new InvalidTokenPythonExceptionInterpreter();
            var constraint = expectThrow ? (IResolveConstraint)Throws.Exception : Throws.Nothing;
            Assert.That(
                () => interpreter.Interpret(exception, NullExceptionInterpreter.Instance),
                constraint
            );
        }

        [Test]
        public void VerifyMessageContainsStackTraceInformation()
        {
            var exception = CreateExceptionFromType(typeof(PythonException));
            var assembly = typeof(PythonExceptionInterpreter).Assembly;
            var interpreter = StackExceptionInterpreter.CreateFromAssemblies(new[] { assembly });
            exception = interpreter.Interpret(exception, NullExceptionInterpreter.Instance);
            Assert.True(exception.Message.Contains("x = 01"));
        }

        private Exception CreateExceptionFromType(Type type) =>
            type == typeof(PythonException)
                ? _pythonException
                : (Exception)Activator.CreateInstance(type);
    }
}
