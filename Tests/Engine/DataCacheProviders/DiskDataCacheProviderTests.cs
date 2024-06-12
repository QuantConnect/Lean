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
 *
*/

using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataCacheProviders
{
    [TestFixture]
    public class DiskDataCacheProviderTests : DataCacheProviderTests
    {
        public override IDataCacheProvider CreateDataCacheProvider()
        {
            return new DiskDataCacheProvider();
        }

        [Test]
        public void WriteTest()
        {
            var entryName = "testEntry.csv";
            var filePath = Path.GetTempFileName().Replace(".tmp", ".zip", StringComparison.InvariantCulture);
            var key = filePath + "#" + entryName;

            var random = new Random();
            var data = new byte[10];
            random.NextBytes(data);

            // Write this data to our entry in the temp zip
            DataCacheProvider.Store(key, data);

            // Verify it exists
            Assert.IsTrue(File.Exists(filePath));

            // Open the file are verify we have the expected results
            using var zip = new ZipFile(filePath);
            Assert.AreEqual(1, zip.Count);
            Assert.IsNotNull(zip.GetEntry(entryName));

            zip.DisposeSafely();
        }

        [Test]
        public void OverrideEntry()
        {
            var entryName = "testEntry.csv";
            var filePath = Path.GetTempFileName().Replace(".tmp", ".zip", StringComparison.InvariantCulture);
            var key = filePath + "#" + entryName;

            var random = new Random();
            var data = new byte[10];
            random.NextBytes(data);

            // Write this data to our entry in the temp zip
            DataCacheProvider.Store(key, data);

            // Verify it exists
            Assert.IsTrue(File.Exists(filePath));

            random = new Random();
            data = new byte[10];
            random.NextBytes(data);

            // Write this data to our entry in the temp zip
            DataCacheProvider.Store(key, data);
        }
    }
}
