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
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Compression
{
    [TestFixture]
    public class ZipStreamWriterTests
    {
        #pragma warning disable CA2000
        [Test]
        public void Create()
        {
            var fileName = Guid.NewGuid().ToString();
            var file = new ZipStreamWriter(fileName, "pepe");
            file.WriteLine("grillo");
            file.WriteLine("the");
            file.WriteLine("best");
            file.DisposeSafely();

            var lines = QuantConnect.Compression.ReadLines(fileName).ToList();

            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual("grillo", lines[0]);
            Assert.AreEqual("the", lines[1]);
            Assert.AreEqual("best", lines[2]);
            File.Delete(fileName);
        }

        [Test]
        public void Update()
        {
            var fileName = Guid.NewGuid().ToString();
            var file = new ZipStreamWriter(fileName, "pepe");
            file.WriteLine("grillo");
            file.DisposeSafely();

            var fileBis = new ZipStreamWriter(fileName, "pepe");
            fileBis.WriteLine("the");
            fileBis.WriteLine("best");
            fileBis.DisposeSafely();

            var lines = QuantConnect.Compression.ReadLines(fileName).ToList();

            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual("grillo", lines[0]);
            Assert.AreEqual("the", lines[1]);
            Assert.AreEqual("best", lines[2]);
            File.Delete(fileName);
        }

        [Test]
        public void MultipleEntries()
        {
            var fileName = Guid.NewGuid().ToString();
            var file = new ZipStreamWriter(fileName, "pepe");
            file.WriteLine("grillo");
            file.DisposeSafely();

            var fileBis = new ZipStreamWriter(fileName, "pepeBis");
            fileBis.WriteLine("the");
            fileBis.WriteLine("best");
            fileBis.DisposeSafely();

            var lines = QuantConnect.Compression.Unzip(fileName).ToList();

            Assert.AreEqual(2, lines.Count);

            Assert.AreEqual("pepe", lines[0].Key);
            Assert.AreEqual("pepeBis", lines[1].Key);

            var entry1 = lines[0].Value.ToList();
            Assert.AreEqual(1, entry1.Count);
            Assert.AreEqual("grillo", entry1[0]);

            var entry2 = lines[1].Value.ToList();
            Assert.AreEqual(2, entry2.Count);
            Assert.AreEqual("the", entry2[0]);
            Assert.AreEqual("best", entry2[1]);

            File.Delete(fileName);
        }
    }
}
