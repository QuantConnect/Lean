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
using System.Linq;
using System.Text;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class StreamReaderEnumerableTests
    {
        [Test]
        public void EnumeratesLines()
        {
            var lines = Enumerable.Range(0, 10).Select(i => $"line {i}").ToArray();
            var content = string.Join(Environment.NewLine, lines);
            using (var streamReader = new StreamReader(new MemoryStream(Encoding.Default.GetBytes(content))))
            {
                var enumerable = new StreamReaderEnumerable(streamReader);
                var actualLines = enumerable.ToList();
                CollectionAssert.AreEqual(lines, actualLines);
            }
        }
    }
}
