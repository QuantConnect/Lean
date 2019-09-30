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
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using QuantConnect.Exceptions;
using QuantConnect.Scheduling;

namespace QuantConnect.Tests.Common.Exceptions
{
    [TestFixture]
    public class ScheduledEventExceptionInterpreterTests
    {
        [Test]
        [TestCase(typeof(Exception), ExpectedResult = false)]
        [TestCase(typeof(KeyNotFoundException), ExpectedResult = false)]
        [TestCase(typeof(DivideByZeroException), ExpectedResult = false)]
        [TestCase(typeof(InvalidOperationException), ExpectedResult = false)]
        [TestCase(typeof(ScheduledEventException), ExpectedResult = true)]
        public bool CanProjectReturnsTrueForOnlyScheduledEventExceptionType(Type exceptionType)
        {
            var exception = CreateExceptionFromType(exceptionType);
            return new ScheduledEventExceptionInterpreter().CanInterpret(exception);
        }

        [Test]
        [TestCase(typeof(Exception), true)]
        [TestCase(typeof(KeyNotFoundException), true)]
        [TestCase(typeof(DivideByZeroException), true)]
        [TestCase(typeof(InvalidOperationException), true)]
        [TestCase(typeof(ScheduledEventException), false)]
        public void ProjectThrowsForNonScheduledEventExceptionTypes(Type exceptionType, bool expectThrow)
        {
            var exception = CreateExceptionFromType(exceptionType);
            var interpreter = new ScheduledEventExceptionInterpreter();
            var constraint = expectThrow ? (IResolveConstraint)Throws.Exception : Throws.Nothing;
            Assert.That(() => interpreter.Interpret(exception, NullExceptionInterpreter.Instance), constraint);
        }

        [Test]
        public void ReformsMessageToIncludeScheduledEventName()
        {
            var id = Guid.NewGuid();
            var name = id.ToStringInvariant("D");
            var message = id.ToStringInvariant("N");
            var exception = new ScheduledEventException(name, message, null);
            var interpreted = new ScheduledEventExceptionInterpreter().Interpret(exception, NullExceptionInterpreter.Instance);

            var expectedInterpretedMessage = $"In Scheduled Event '{name}',";
            Assert.AreEqual(expectedInterpretedMessage, interpreted.Message);
        }

        [Test]
        public void WrapsScheduledEventExceptionInnerException()
        {
            var inner = new Exception();
            var exception = new ScheduledEventException("name", "message", inner);
            var interpreted = new ScheduledEventExceptionInterpreter().Interpret(exception, NullExceptionInterpreter.Instance);
            Assert.AreEqual(inner, interpreted.InnerException);
        }

        [Test]
        public void InvokesInnerExceptionProjectionOnInnerException()
        {
            var inner = new Exception("inner");
            var exception = new ScheduledEventException("name", "message", inner);
            var mockInnerInterpreter = new Mock<IExceptionInterpreter>();
            mockInnerInterpreter.Setup(iep => iep.Interpret(inner, mockInnerInterpreter.Object))
                .Returns(new Exception("Projected " + inner.Message))
                .Verifiable();

            var interpreter = new ScheduledEventExceptionInterpreter();

            interpreter.Interpret(exception, mockInnerInterpreter.Object);

            mockInnerInterpreter.Verify(iep => iep.Interpret(inner, mockInnerInterpreter.Object), Times.Exactly(1));
        }

        private Exception CreateExceptionFromType(Type type)
        {
            if (type == typeof(ScheduledEventException))
            {
                var inner = new Exception("Sample inner message");
                return new ScheduledEventException(Guid.NewGuid().ToStringInvariant(null), "Sample error message", inner);
            }

            return (Exception)Activator.CreateInstance(type);
        }
    }
}
