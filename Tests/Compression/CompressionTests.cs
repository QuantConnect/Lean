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
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
using Newtonsoft.Json;
using NUnit.Framework;

namespace QuantConnect.Tests.Compression
{
    [TestFixture]
    public class CompressionTests
    {
        [Test]
        public void ReadLinesCountMatchesLineCount()
        {
            const string file = "../../../Data/equity/usa/minute/spy/20131008_trade.zip";

            const int expected = 828;
            int actual = QuantConnect.Compression.ReadLines(file).Count();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ZipBytes()
        {
            const string fileContents = "this is the contents of a file!";
            var fileBytes = Encoding.ASCII.GetBytes(fileContents); // using asci because UnzipData uses 1byte=1char
            var zippedBytes = QuantConnect.Compression.ZipBytes(fileBytes, "entry");
            File.WriteAllBytes("entry.zip", zippedBytes);

            using (var streamReader = QuantConnect.Compression.UnzipStreamToStreamReader(File.OpenRead("entry.zip")))
            {
                var contents = streamReader.ReadToEnd();
                Assert.AreEqual(fileContents, contents);
            }
        }

        [Test]
        public void ZipBytesReturnsByteArrayWithCorrectLength()
        {
            const string file = "../../../Data/equity/usa/tick/spy/20131007_trade.zip";
            var fileBytes = File.ReadAllBytes(file);
            var zippedBytes = QuantConnect.Compression.ZipBytes(fileBytes, "entry");

            Assert.AreEqual(OS.IsWindows ? 905921 : 906121, zippedBytes.Length);
        }

        [Test]
        public void ExtractsZipEntryByName()
        {
            var zip = Path.Combine("TestData", "multizip.zip");
            ZipFile zipFile;
            using (var entryStream = QuantConnect.Compression.Unzip(zip, "multizip/two.txt", out zipFile))
            using (zipFile)
            {
                var text = entryStream.ReadToEnd();
                Assert.AreEqual("2", text);
            }
        }

        [Test]
        public void ReadsZipEntryFileNames()
        {
            var zipFileName = Path.Combine("TestData", "20151224_quote_american.zip");
            var entryFileNames = QuantConnect.Compression.GetZipEntryFileNames(zipFileName).ToList();

            var expectedFileNames = new[]
            {
                "20151224_xlre_tick_quote_american_call_210000_20160819.csv",
                "20151224_xlre_tick_quote_american_call_220000_20160819.csv",
                "20151224_xlre_tick_quote_american_put_370000_20160819.csv"
            };

            Assert.AreEqual(expectedFileNames.Length, entryFileNames.Count);

            for (var i = 0; i < entryFileNames.Count; i++)
            {
                Assert.AreEqual(expectedFileNames[i], entryFileNames[i]);
            }
        }

        [Test]
        public void UnzipByteArray()
        {
            var name = nameof(UnzipByteArray);
            var root = new DirectoryInfo(name);
            var testPath = Path.Combine(root.FullName, "test.txt");
            var test2Path = Path.Combine(Path.Combine(root.FullName, "sub"), "test2.txt");
            var zipFile = $"./{name}.zip";
            var files = new List<string>();
            try
            {
                root.Create();
                File.WriteAllText(testPath, "string contents");
                var sub = root.CreateSubdirectory("sub");
                File.WriteAllText(test2Path, "string contents 2");
                QuantConnect.Compression.ZipDirectory(root.FullName, zipFile);
                Directory.Delete(root.FullName, true);

                var data = File.ReadAllBytes(zipFile);
                files = QuantConnect.Compression.UnzipToFolder(data,  Directory.GetCurrentDirectory());

                Assert.AreEqual(2, files.Count);
                Assert.IsTrue(File.Exists(testPath));
                Assert.IsTrue(File.Exists(test2Path));
            }
            finally
            {
                File.Delete(zipFile);
                files.ForEach(File.Delete);
            }
        }

        [Test]
        public void UnzipToFolderDoesNotStripSubDirectories()
        {
            var name = nameof(UnzipToFolderDoesNotStripSubDirectories);
            var root = new DirectoryInfo(name);
            var testPath = Path.Combine(root.FullName, "test.txt");
            var test2Path = Path.Combine(Path.Combine(root.FullName, "sub"), "test2.txt");
            var zipFile = $"./{name}.zip";
            var files = new List<string>();
            try
            {
                root.Create();
                File.WriteAllText(testPath, "string contents");
                var sub = root.CreateSubdirectory("sub");
                File.WriteAllText(test2Path, "string contents 2");
                QuantConnect.Compression.ZipDirectory(root.FullName, zipFile);
                Directory.Delete(root.FullName, true);
                files = QuantConnect.Compression.UnzipToFolder(zipFile);

                Assert.AreEqual(2, files.Count);
                Assert.IsTrue(File.Exists(testPath));
                Assert.IsTrue(File.Exists(test2Path));
            }
            finally
            {
                File.Delete(zipFile);
                files.ForEach(File.Delete);
            }
        }

        [Test]
        public void UnzipToFolderUsesCorrectOutputFolder()
        {
            var name = nameof(UnzipToFolderUsesCorrectOutputFolder);
            var root = new DirectoryInfo(name);
            var testPath = Path.Combine(root.FullName, "test.txt");
            var test2Path = Path.Combine(Path.Combine(root.FullName, "sub"), "test2.txt");
            var zipFile = $"./jo\\se/{name}.zip";
            var files = new List<string>();
            try
            {
                Directory.CreateDirectory("./jo\\se");
                root.Create();
                File.WriteAllText(testPath, "string contents");
                var sub = root.CreateSubdirectory("sub");
                File.WriteAllText(test2Path, "string contents 2");
                QuantConnect.Compression.ZipDirectory(root.FullName, zipFile);
                Directory.Delete(root.FullName, true);
                files = QuantConnect.Compression.UnzipToFolder(zipFile);

                Assert.IsTrue(File.Exists(Path.Combine(root.Parent.FullName, "jo\\se", name, "test.txt")));
                Assert.IsTrue(File.Exists(Path.Combine(root.Parent.FullName, "jo\\se", name, "sub", "test2.txt")));
            }
            finally
            {
                File.Delete(zipFile);
                files.ForEach(File.Delete);
            }
        }

        [Test]
        public void ZipUnzipDataToFile()
        {
            var data = new Dictionary<string, string>
            {
                {"Ł", "The key is unicode"},
                {"2", "something"}
            };

            var fileName = Guid.NewGuid().ToString();
            var compressed = QuantConnect.Compression.ZipData(fileName, data);

            Assert.IsTrue(compressed);

            using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var result = QuantConnect.Compression.UnzipDataAsync(fileStream).Result;

            CollectionAssert.AreEqual(data.OrderBy(kv => kv.Key).ToList(), result.OrderBy(kv => kv.Key).ToList());
        }

        [Test]
        public void UnzipDataSupportsEncoding()
        {
            var data = new Dictionary<string, string>
            {
                {"Ł", "The key is unicode"}
            };

            var encoding = Encoding.UTF8;
            var bytes = encoding.GetBytes(JsonConvert.SerializeObject(data));
            var compressed = QuantConnect.Compression.ZipBytes(bytes, "entry.json");
            var decompressed = QuantConnect.Compression.UnzipData(compressed, encoding);
            var redata = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                decompressed.Single().Value
            );

            var expected = data.Single();
            var actual = redata.Single();
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);
        }

        [Test]
        public void UnzipDataStream()
        {
            var data = new Dictionary<string, string>
            {
                {"Ł", "The key is unicode"}
            };

            var encoding = Encoding.UTF8;
            var bytes = encoding.GetBytes(JsonConvert.SerializeObject(data));
            var compressed = QuantConnect.Compression.ZipBytes(bytes, "entry.json");

            using var stream = new MemoryStream(compressed);
            var decompressed = QuantConnect.Compression.UnzipDataAsync(stream, encoding).Result;
            var redata = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                decompressed.Single().Value
            );

            var expected = data.Single();
            var actual = redata.Single();
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ZipCreateAppendData(bool overrideEntry)
        {
            var name = $"PepeGrillo{overrideEntry}.zip";
            if (File.Exists(name))
            {
                File.Delete(name);
            }
            QuantConnect.Compression.Zip("Pinocho", name, "cow");

            Assert.AreEqual(overrideEntry, QuantConnect.Compression.ZipCreateAppendData(name, "cow", "jiji", overrideEntry));

            var result = QuantConnect.Compression.Unzip(name).ToList();
            Assert.AreEqual(1, result.Count);

            var kvp = result.Single();
            Assert.AreEqual("cow", kvp.Key);

            var data = kvp.Value.ToList();
            Assert.AreEqual(1, data.Count);
            Assert.AreEqual((overrideEntry ? "jiji" : "Pinocho"), data[0]);
        }
    }
}
