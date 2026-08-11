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
    public class UnsupportedOperandPythonExceptionInterpreterTests
    {
        private PythonException _pythonException;

        [OneTimeSetUp]
        public void Setup()
        {
            using (Py.GIL())
            {
                var module = Py.Import("Test_PythonExceptionInterpreter");
                dynamic algorithm = module.GetAttr("Test_PythonExceptionInterpreter").Invoke();

                try
                {
                    // x = None + "Pepe Grillo"
                    algorithm.unsupported_operand();
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
        public bool CanInterpretReturnsTrueForOnlyUnsupportedOperandPythonExceptionType(Type exceptionType)
        {
            var exception = CreateExceptionFromType(exceptionType);
            return new UnsupportedOperandPythonExceptionInterpreter().CanInterpret(exception);
        }

        [Test]
        [TestCase(typeof(Exception), true)]
        [TestCase(typeof(KeyNotFoundException), true)]
        [TestCase(typeof(DivideByZeroException), true)]
        [TestCase(typeof(InvalidOperationException), true)]
        [TestCase(typeof(PythonException), false)]
        public void InterpretThrowsForNonUnsupportedOperandPythonExceptionTypes(Type exceptionType, bool expectThrow)
        {
            var exception = CreateExceptionFromType(exceptionType);
            var interpreter = new UnsupportedOperandPythonExceptionInterpreter();
            var constraint = expectThrow ? (IResolveConstraint)Throws.Exception : Throws.Nothing;
            Assert.That(() => interpreter.Interpret(exception, NullExceptionInterpreter.Instance), constraint);
        }

        [Test]
        public void VerifyMessageContainsStackTraceInformation()
        {
            var exception = CreateExceptionFromType(typeof(PythonException));
            var assembly = typeof(PythonExceptionInterpreter).Assembly;
            var interpreter = StackExceptionInterpreter.CreateFromAssemblies();
            exception = interpreter.Interpret(exception, NullExceptionInterpreter.Instance);
            Assert.True(exception.Message.Contains("x = None + \"Pepe Grillo\""));
        }

        [Test]
        public void NonDatetimeOperandsDoNotGetTheDatetimeHint()
        {
            var interpreted = new UnsupportedOperandPythonExceptionInterpreter()
                .Interpret(_pythonException, NullExceptionInterpreter.Instance);
            Assert.False(interpreted.Message.Contains("days_to_expiry"));
        }

        [Test]
        public void DatetimeAndDateSubtractionGetsTheDatetimeHint()
        {
            // (contract.id.date - self.time.date()).days, the most common fleet shape of this error
            var exception = CreatePythonException("datetime_and_date_subtraction");
            var interpreter = new UnsupportedOperandPythonExceptionInterpreter();
            Assert.True(interpreter.CanInterpret(exception));

            var interpreted = interpreter.Interpret(exception, NullExceptionInterpreter.Instance);
            Assert.True(interpreted.Message.Contains("'datetime.datetime' and 'datetime.date'"), interpreted.Message);
            Assert.True(interpreted.Message.Contains("days_to_expiry"), interpreted.Message);
            Assert.True(interpreted.Message.Contains(".date()"), interpreted.Message);
        }

        [Test]
        public void DatetimeAndDateComparisonIsInterpretedWithTheDatetimeHint()
        {
            // "can't compare datetime.datetime to datetime.date", e.g. self.time.date() <= some_stored_datetime
            var exception = CreatePythonException("datetime_and_date_comparison");
            var interpreter = new UnsupportedOperandPythonExceptionInterpreter();
            Assert.True(interpreter.CanInterpret(exception));

            var interpreted = interpreter.Interpret(exception, NullExceptionInterpreter.Instance);
            Assert.True(interpreted.Message.Contains("Trying to compare"), interpreted.Message);
            Assert.True(interpreted.Message.Contains("'datetime.datetime'"), interpreted.Message);
            Assert.True(interpreted.Message.Contains("'datetime.date'"), interpreted.Message);
            Assert.True(interpreted.Message.Contains("days_to_expiry"), interpreted.Message);
        }

        [Test]
        public void OtherComparisonTypeErrorsAreNotInterpreted()
        {
            // "can't compare offset-naive and offset-aware datetimes" is not the datetime-vs-date shape
            PythonException exception = null;
            using (Py.GIL())
            {
                try
                {
                    PythonEngine.Exec("from datetime import datetime, timezone\ndatetime.now() < datetime.now(timezone.utc)");
                }
                catch (PythonException pythonException)
                {
                    exception = pythonException;
                }
            }

            Assert.IsNotNull(exception);
            Assert.False(new UnsupportedOperandPythonExceptionInterpreter().CanInterpret(exception));
        }

        internal static PythonException CreatePythonException(string methodName)
        {
            using (Py.GIL())
            {
                var module = Py.Import("Test_PythonExceptionInterpreter");
                dynamic algorithm = module.GetAttr("Test_PythonExceptionInterpreter").Invoke();

                try
                {
                    algorithm.InvokeMethod(methodName);
                }
                catch (PythonException pythonException)
                {
                    return pythonException;
                }
            }

            throw new InvalidOperationException($"Expected '{methodName}' to throw a PythonException");
        }

        private Exception CreateExceptionFromType(Type type) => type == typeof(PythonException) ? _pythonException : (Exception)Activator.CreateInstance(type);
    }
}
