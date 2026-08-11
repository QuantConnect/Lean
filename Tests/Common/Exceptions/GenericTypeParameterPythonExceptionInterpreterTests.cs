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
    public class GenericTypeParameterPythonExceptionInterpreterTests
    {
        private PythonException _pythonException;

        [OneTimeSetUp]
        public void Setup()
        {
            // x = RollingWindow[datetime](10)
            _pythonException = UnsupportedOperandPythonExceptionInterpreterTests.CreatePythonException("python_type_as_generic_type_parameter");
        }

        [Test]
        [TestCase(typeof(Exception), ExpectedResult = false)]
        [TestCase(typeof(KeyNotFoundException), ExpectedResult = false)]
        [TestCase(typeof(DivideByZeroException), ExpectedResult = false)]
        [TestCase(typeof(InvalidOperationException), ExpectedResult = false)]
        [TestCase(typeof(PythonException), ExpectedResult = true)]
        public bool CanInterpretReturnsTrueForOnlyGenericTypeParameterPythonExceptionType(Type exceptionType)
        {
            var exception = CreateExceptionFromType(exceptionType);
            return new GenericTypeParameterPythonExceptionInterpreter().CanInterpret(exception);
        }

        [Test]
        public void InterpretedMessagePointsAtNetTypesAndUntypedRollingWindow()
        {
            var interpreted = new GenericTypeParameterPythonExceptionInterpreter().Interpret(_pythonException, NullExceptionInterpreter.Instance);
            Assert.True(interpreted.Message.Contains("RollingWindow[DateTime](10)"), interpreted.Message);
            Assert.True(interpreted.Message.Contains("RollingWindow(10)"), interpreted.Message);
            // The stack trace should point at the offending line
            Assert.True(interpreted.Message.Contains("RollingWindow[datetime](10)"), interpreted.Message);
        }

        private Exception CreateExceptionFromType(Type type) => type == typeof(PythonException) ? _pythonException : (Exception)Activator.CreateInstance(type);
    }
}
