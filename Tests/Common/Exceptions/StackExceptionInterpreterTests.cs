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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Exceptions;

namespace QuantConnect.Tests.Common.Exceptions
{
    [TestFixture]
    public class StackExceptionInterpretersTests
    {
        [Test]
        public void CreatesFromAssemblies()
        {
            var assembly = typeof(ClrBubbledExceptionInterpreter).Assembly;
            var interpreter = StackExceptionInterpreter.CreateFromAssemblies(new[] {assembly});
            Assert.AreEqual(1, interpreter.Interpreters.Count(p => p.GetType() == typeof(ClrBubbledExceptionInterpreter)));
        }

        [Test]
        public void CallsInterpretOnFirstProjectionThatCanInterpret()
        {
            var canInterpretCalled = new List<int>();
            var interpretCalled = new List<int>();
            var interpreters = new[]
            {
                new FakeExceptionInterpreter(e =>
                {
                    canInterpretCalled.Add(0);
                    return false;
                }, e =>
                {
                    interpretCalled.Add(0);
                    return e;
                },
                order : 2),
                new FakeExceptionInterpreter(e =>
                {
                    canInterpretCalled.Add(1);
                    return true;
                }, e =>
                {
                    interpretCalled.Add(1);
                    return e;
                },
                order : 1),
                new FakeExceptionInterpreter(e =>
                {
                    canInterpretCalled.Add(2);
                    return false;
                }, e =>
                {
                    interpretCalled.Add(2);
                    return e;
                },
                order : 0)
            };

            var interpreter = new StackExceptionInterpreter(interpreters);
            interpreter.Interpret(new Exception(), null);

            // can interpret called for 3nd and 2rd entry
            Assert.Contains(2, canInterpretCalled);
            Assert.Contains(1, canInterpretCalled);
            Assert.That(canInterpretCalled, Is.Not.Contains(0));

            // interpret only called on second entry
            Assert.That(interpretCalled, Is.Not.Contains(0));
            Assert.Contains(1, interpretCalled);
            Assert.That(interpretCalled, Is.Not.Contains(2));

            // interpreter called 3rd before 2nd
            Assert.Greater(canInterpretCalled.First(), canInterpretCalled.Last());
        }

        [Test]
        public void RecursivelyProjectsInnerExceptions()
        {
            var inner = new Exception("inner");
            var middle = new Exception("middle", inner);
            var outter = new Exception("outter", middle);
            var interpreter = new StackExceptionInterpreter(new[]
            {
                new FakeExceptionInterpreter()
            });

            var interpreted = interpreter.Interpret(outter, null);
            Assert.AreEqual("Projected 1: outter", interpreted.Message);
            Assert.AreEqual("Projected 2: middle", interpreted.InnerException.Message);
            Assert.AreEqual("Projected 3: inner", interpreted.InnerException.InnerException.Message);
        }

        [Test]
        public void GetsExceptionMessageHeaderAsAllInnersJoinedBySpace()
        {
            var inner = new Exception("inner");
            var middle = new Exception("middle", inner);
            var outter = new Exception("outter", middle);
            var message = new StackExceptionInterpreter(Enumerable.Empty<IExceptionInterpreter>()).GetExceptionMessageHeader(outter);

            // header line w/ exception message and then the full detail on a new line
            var expectedMessage = "outter middle inner";
            Assert.AreEqual(expectedMessage, message);
        }
    }
}
