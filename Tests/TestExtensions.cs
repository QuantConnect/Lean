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

using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace QuantConnect.Tests
{
    /// <summary>
    /// Provides extension methods to make test code easier to read/write
    /// </summary>
    public static class TestExtensions
    {
        /// <summary>
        /// Calls <see cref="WaitHandle.WaitOne(int)"/> on the specified <see cref="WaitHandle"></see> and then
        /// call <see cref="Assert.Fail(string)"/> if <paramref name="wait"/> was not set.
        /// </summary>
        /// <param name="wait">The <see cref="WaitHandle"/></param> instance to wait on
        /// <param name="milliseconds">The timeout, in milliseconds</param>
        /// <param name="message">The message to fail with, null to fail with no message</param>
        public static void WaitOneAssertFail(this WaitHandle wait, int milliseconds, string message = null)
        {
            if (!wait.WaitOne(milliseconds))
            {
                Assert.Fail(message);
            }
        }

        /// <summary>
        /// Asserts that the two JObject instances are equal. This uses <see cref="JToken.DeepEquals"/> wrapped w/ better failure messages
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="property">The property to assert</param>
        public static void IsEqualTo(this JObject expected, JObject actual, string property)
        {
            var e = expected.SelectToken(property);
            var a = actual.SelectToken(property);
            Assert.IsTrue(JToken.DeepEquals(e, a), $"{property}: Expected {e}. Actual {a}");
        }
    }
}
