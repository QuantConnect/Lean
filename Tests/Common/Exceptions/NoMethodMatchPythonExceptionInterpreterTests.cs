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

        [OneTimeSetUp]
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
            var interpreter = StackExceptionInterpreter.CreateFromAssemblies();
            exception = interpreter.Interpret(exception, NullExceptionInterpreter.Instance);
            Assert.True(exception.Message.Contains("self.set_cash('SPY')"));
        }

        [Test]
        public void VerifyMessageContainsTheMethodName()
        {
            var exception = CreateExceptionFromType(typeof(PythonException));
            var interpreter = new NoMethodMatchPythonExceptionInterpreter();
            exception = interpreter.Interpret(exception, NullExceptionInterpreter.Instance);

            // The interpreter should reference the actual method name that failed to resolve
            // (set_cash, the snake_case name Python callers use), not a fragment of the argument
            // type list (e.g. "'str'>)").
            Assert.That(exception.Message, Does.Contain("set_cash"));
            Assert.That(exception.Message, Does.Not.Contain(">)"));
        }

        [Test]
        public void VerifyMessageContainsTheMethodNameForOverloadedMethod()
        {
            PythonException pythonException;
            using (Py.GIL())
            {
                var module = Py.Import("Test_PythonExceptionInterpreter");
                dynamic algorithm = module.GetAttr("Test_PythonExceptionInterpreter").Invoke();

                // self.rsi(symbol, 15, Resolution.DAILY) -- the third argument should be a
                // MovingAverageType, so no RSI overload matches the given arguments.
                pythonException = Assert.Throws<PythonException>(() => algorithm.no_method_match_rsi());
            }

            var interpreter = new NoMethodMatchPythonExceptionInterpreter();
            var exception = interpreter.Interpret(pythonException, NullExceptionInterpreter.Instance);

            // The interpreter should reference the rsi method name (snake_case, as Python callers
            // use it), not a fragment of the argument type list (e.g. "'QuantConnect.Resolution'>)").
            Assert.That(exception.Message, Does.Contain("rsi"));
            Assert.That(exception.Message, Does.Not.Contain(">)"));
        }

        [Test]
        public void VerifyMessageKeepsTheOverloadsHint()
        {
            // pythonnet appends the candidate signatures to the binding-failure message.
            // set_cash('SPY') has multiple candidate overloads.
            var pythonException = (PythonException)CreateExceptionFromType(typeof(PythonException));
            StringAssert.Contains("The following overloads are available:", pythonException.Message);

            var interpreter = new NoMethodMatchPythonExceptionInterpreter();
            var exception = interpreter.Interpret(pythonException, NullExceptionInterpreter.Instance);

            // The interpreted message must keep the overloads hint so the user can see
            // what the method expects, not just that no overload matched.
            Assert.That(exception.Message, Does.Contain("The following overloads are available:"));
            Assert.That(exception.Message, Does.Contain("set_cash("));
        }

        [Test]
        public void VerifyMessageKeepsTheExpectedSignatureHint()
        {
            PythonException pythonException;
            using (Py.GIL())
            {
                var module = Py.Import("Test_PythonExceptionInterpreter");
                dynamic algorithm = module.GetAttr("Test_PythonExceptionInterpreter").Invoke();
                pythonException = Assert.Throws<PythonException>(() => algorithm.no_method_match_rsi());
            }

            // rsi's candidate overloads collapse into a single snake-case signature,
            // so pythonnet words the hint as "The expected signature is:"
            StringAssert.Contains("The expected signature is:", pythonException.Message);

            var interpreter = new NoMethodMatchPythonExceptionInterpreter();
            var exception = interpreter.Interpret(pythonException, NullExceptionInterpreter.Instance);

            Assert.That(exception.Message, Does.Contain("The expected signature is:"));
            Assert.That(exception.Message, Does.Contain("rsi("));
        }

        private Exception CreateExceptionFromType(Type type) => type == typeof(PythonException) ? _pythonException : (Exception)Activator.CreateInstance(type);
    }
}
