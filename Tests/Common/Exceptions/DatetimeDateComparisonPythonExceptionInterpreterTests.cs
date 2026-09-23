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
    public class DatetimeDateComparisonPythonExceptionInterpreterTests
    {
        private PythonException _comparisonException;
        private PythonException _unsupportedOperandException;

        [OneTimeSetUp]
        public void Setup()
        {
            using (Py.GIL())
            {
                var module = Py.Import("Test_PythonExceptionInterpreter");
                dynamic algorithm = module.GetAttr("Test_PythonExceptionInterpreter").Invoke();

                try
                {
                    // x = datetime(2020, 1, 2) < date(2020, 1, 1)
                    algorithm.datetime_date_comparison();
                }
                catch (PythonException pythonException)
                {
                    _comparisonException = pythonException;
                }

                try
                {
                    // x = None + "Pepe Grillo"
                    algorithm.unsupported_operand();
                }
                catch (PythonException pythonException)
                {
                    _unsupportedOperandException = pythonException;
                }
            }
        }

        [Test]
        public void StackInterpreterRewritesComparisonErrorWithDateHint()
        {
            var interpreter = StackExceptionInterpreter.CreateFromAssemblies();
            var interpreted = interpreter.Interpret(_comparisonException, NullExceptionInterpreter.Instance);
            Assert.IsTrue(interpreted.Message.Contains(".date()"), interpreted.Message);
            Assert.IsTrue(interpreted.Message.Contains("x = datetime(2020, 1, 2) < date(2020, 1, 1)"), interpreted.Message);
        }

        [Test]
        [TestCase(typeof(Exception), ExpectedResult = false)]
        [TestCase(typeof(KeyNotFoundException), ExpectedResult = false)]
        [TestCase(typeof(DivideByZeroException), ExpectedResult = false)]
        [TestCase(typeof(InvalidOperationException), ExpectedResult = false)]
        [TestCase(typeof(PythonException), ExpectedResult = true)]
        public bool CanInterpretReturnsTrueForOnlyDatetimeDateComparisonPythonExceptionType(Type exceptionType)
        {
            var exception = CreateExceptionFromType(exceptionType);
            return new DatetimeDateComparisonPythonExceptionInterpreter().CanInterpret(exception);
        }

        [Test]
        public void CanInterpretReturnsFalseForOtherTypeErrors()
        {
            Assert.IsFalse(new DatetimeDateComparisonPythonExceptionInterpreter().CanInterpret(_unsupportedOperandException));
            Assert.IsFalse(new UnsupportedOperandPythonExceptionInterpreter().CanInterpret(_comparisonException));
        }

        [Test]
        [TestCase(typeof(Exception), true)]
        [TestCase(typeof(KeyNotFoundException), true)]
        [TestCase(typeof(DivideByZeroException), true)]
        [TestCase(typeof(InvalidOperationException), true)]
        [TestCase(typeof(PythonException), false)]
        public void InterpretThrowsForNonDatetimeDateComparisonPythonExceptionTypes(Type exceptionType, bool expectThrow)
        {
            var exception = CreateExceptionFromType(exceptionType);
            var interpreter = new DatetimeDateComparisonPythonExceptionInterpreter();
            var constraint = expectThrow ? (IResolveConstraint)Throws.Exception : Throws.Nothing;
            Assert.That(() => interpreter.Interpret(exception, NullExceptionInterpreter.Instance), constraint);
        }

        [Test]
        public void InterpretedMessageContainsGuidanceAndStackInformation()
        {
            var interpreted = new DatetimeDateComparisonPythonExceptionInterpreter().Interpret(_comparisonException, NullExceptionInterpreter.Instance);
            Assert.IsTrue(interpreted.Message.Contains(
                Messages.DatetimeDateComparisonPythonExceptionInterpreter.InvalidDatetimeDateComparison), interpreted.Message);
            Assert.IsTrue(interpreted.Message.Contains("x = datetime(2020, 1, 2) < date(2020, 1, 1)"), interpreted.Message);
        }

        private Exception CreateExceptionFromType(Type type) => type == typeof(PythonException) ? _comparisonException : (Exception)Activator.CreateInstance(type);
    }
}
