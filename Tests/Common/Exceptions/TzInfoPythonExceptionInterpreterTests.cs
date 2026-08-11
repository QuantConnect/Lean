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
using QuantConnect.Exceptions;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Exceptions
{
    [TestFixture]
    public class TzInfoPythonExceptionInterpreterTests
    {
        private PythonException _pythonException;

        [OneTimeSetUp]
        public void Setup()
        {
            // x = datetime.now(TimeZones.NEW_YORK)
            _pythonException = UnsupportedOperandPythonExceptionInterpreterTests.CreatePythonException("lean_time_zone_as_tzinfo");
        }

        [Test]
        [TestCase(typeof(Exception), ExpectedResult = false)]
        [TestCase(typeof(KeyNotFoundException), ExpectedResult = false)]
        [TestCase(typeof(DivideByZeroException), ExpectedResult = false)]
        [TestCase(typeof(InvalidOperationException), ExpectedResult = false)]
        [TestCase(typeof(PythonException), ExpectedResult = true)]
        public bool CanInterpretReturnsTrueForOnlyTzInfoPythonExceptionType(Type exceptionType)
        {
            var exception = CreateExceptionFromType(exceptionType);
            return new TzInfoPythonExceptionInterpreter().CanInterpret(exception);
        }

        [Test]
        public void InterpretedMessagePointsAtZoneInfo()
        {
            var interpreted = new TzInfoPythonExceptionInterpreter().Interpret(_pythonException, NullExceptionInterpreter.Instance);
            Assert.True(interpreted.Message.Contains("zoneinfo"), interpreted.Message);
            Assert.True(interpreted.Message.Contains("ZoneInfo(\"America/New_York\")"), interpreted.Message);
            // The stack trace should point at the offending line
            Assert.True(interpreted.Message.Contains("datetime.now(TimeZones.NEW_YORK)"), interpreted.Message);
        }

        [Test]
        public void DoesNotInterpretOtherTzInfoTypeErrors()
        {
            // A non-Lean type as tzinfo should not get the Lean-specific hint
            PythonException exception = null;
            using (Py.GIL())
            {
                try
                {
                    PythonEngine.Exec("from datetime import datetime\ndatetime.now('America/New_York')");
                }
                catch (PythonException pythonException)
                {
                    exception = pythonException;
                }
            }

            Assert.IsNotNull(exception);
            Assert.False(new TzInfoPythonExceptionInterpreter().CanInterpret(exception));
        }

        private Exception CreateExceptionFromType(Type type) => type == typeof(PythonException) ? _pythonException : (Exception)Activator.CreateInstance(type);
    }
}
