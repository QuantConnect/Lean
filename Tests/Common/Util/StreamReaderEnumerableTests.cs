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
using System.Threading;
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

        [Test]
        public void DisposesWhenEnumerationIsCompleted()
        {
            var disposable = new TestDisposable();
            var memoryStream = new TestMemoryStream(Encoding.Default.GetBytes("line1\r\nline2\r\nline3"));
            var streamReader = new TestStreamReader(memoryStream);
            var enumerable = new StreamReaderEnumerable(streamReader, disposable);

            // complete enumeration
            var lines = enumerable.ToList();

            Assert.IsTrue(streamReader.DisposeCalled);
            Assert.IsTrue(streamReader.DisposeCalledDisposingValue);

            Assert.IsTrue(memoryStream.DisposeCalled);
            Assert.IsTrue(memoryStream.DisposeCalledDisposingValue);

            Assert.IsTrue(disposable.DisposeCalled);
        }

        class TestMemoryStream : MemoryStream
        {
            public bool DisposeCalled { get; private set; }
            public bool DisposeCalledDisposingValue { get; private set; }
            public TestMemoryStream(byte[] bytes) : base(bytes) { }
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                DisposeCalled = true;
                DisposeCalledDisposingValue = disposing;
            }
        }

        class TestStreamReader : StreamReader
        {
            public bool DisposeCalled { get; private set; }
            public bool DisposeCalledDisposingValue { get; private set; }
            public TestStreamReader(Stream stream) : base(stream) { }
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                DisposeCalled = true;
                DisposeCalledDisposingValue = disposing;
            }
        }

        class TestDisposable : IDisposable
        {
            public bool DisposeCalled { get; private set; }
            public void Dispose() { DisposeCalled = true; }
        }
    }
}
