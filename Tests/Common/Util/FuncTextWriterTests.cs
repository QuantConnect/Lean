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
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class FuncTextWriterTests
    {
        [SetUp]
        public void Setup()
        {
            // clear any existing output
            var standardOut = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetError(standardOut);
            Console.SetOut(standardOut);
        }
        
        [Test]
        public void RedirectsWriteAndWriteLine()
        {
            var messages = new List<string>();
            Action<string> redirector = s => messages.Add(s);
            var writer = new FuncTextWriter(redirector);

            writer.Write("message");
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("message", messages[0]);

            writer.WriteLine("message2");
            Assert.AreEqual(2, messages.Count);
            Assert.AreEqual("message2", messages[1]);
        }

        [Test]
        public void RedirectsConsoleOutAndError()
        {
            var messages = new List<string>();
            Action<string> redirector = s => messages.Add(s);
            var writer = new FuncTextWriter(redirector);

            Console.SetOut(writer);
            Console.SetError(writer);

            Console.WriteLine("message");
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("message", messages[0]);

            Console.Error.WriteLine("message2");
            Assert.AreEqual(2, messages.Count);
            Assert.AreEqual("message2", messages[1]);
        }
    }
}
