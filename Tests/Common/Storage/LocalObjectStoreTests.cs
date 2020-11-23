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
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.Storage;
using QuantConnect.Packets;
using QuantConnect.Research;
using QuantConnect.Storage;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Storage
{
    [TestFixture]
    public class LocalObjectStoreTests
    {
        private static readonly string TestStorageRoot = $"./{nameof(LocalObjectStoreTests)}";
        private static readonly string StorageRootConfigurationValue = Config.Get("object-store-root");

        private ObjectStore _store;

        [OneTimeSetUp]
        public void Setup()
        {
            Config.Set("object-store-root", TestStorageRoot);

            _store = new ObjectStore(new LocalObjectStore());
            _store.Initialize("CSharp-TestAlgorithm", 0, 0, "", new Controls());
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _store.DisposeSafely();
            Config.Set("object-store-root", StorageRootConfigurationValue);
            try
            {
                Directory.Delete(TestStorageRoot, true);
            }
            catch
            {
            }
            Config.Reset();
        }

        [TestCase(FileAccess.Read, false)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, true)]
        public void GetFilePathPermissions(FileAccess permissions, bool shouldThrow)
        {
            var store = new TestLocalObjectStore();
            store.Initialize($"CSharp-TestAlgorithm-{permissions}", 0, 0, "", new Controls { StoragePermissions = permissions });

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.GetFilePath("Jose"));
            }
            else
            {
                Assert.Throws<KeyNotFoundException>(() => store.GetFilePath("Jose"));
            }
        }

        [TestCase(FileAccess.Read, false)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, true)]
        public void ReadBytesPermissions(FileAccess permissions, bool shouldThrow)
        {
            var store = new TestLocalObjectStore();
            store.Initialize($"CSharp-TestAlgorithm-{permissions}", 0, 0, "", new Controls { StoragePermissions = permissions });

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.ReadBytes("Jose"));
            }
            else
            {
                Assert.Throws<KeyNotFoundException>(() => store.ReadBytes("Jose"));
            }
        }

        [TestCase(FileAccess.Read, true)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, false)]
        public void SaveBytesPermissions(FileAccess permissions, bool shouldThrow)
        {
            var store = new TestLocalObjectStore();
            store.Initialize($"CSharp-TestAlgorithm-{permissions}", 0, 0, "", new Controls { StoragePermissions = permissions });

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.SaveBytes("Jose", new byte[] { 0 }));
            }
            else
            {
                Assert.IsTrue(store.SaveBytes("Jose", new byte[] { 0 }));
            }
        }

        [TestCase(FileAccess.Read, true)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, false)]
        public void DeletePermissions(FileAccess permissions, bool shouldThrow)
        {
            var store = new TestLocalObjectStore();
            store.Initialize($"CSharp-TestAlgorithm-{permissions}", 0, 0, "", new Controls { StoragePermissions = permissions });

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.Delete("Jose"));
            }
            else
            {
                Assert.IsFalse(store.Delete("Jose"));
            }
        }

        [Test]
        public void GetFilePathAndDelete()
        {
            using (var store = new ObjectStore(new LocalObjectStore()))
            {
                store.Initialize("test", 0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests/test"));

                var key = "ILove";
                store.SaveString(key, "Pizza");
                var path = store.GetFilePath(key);

                Assert.IsTrue(File.Exists(path));
                store.Delete(key);

                Assert.IsFalse(File.Exists(path));
            }
        }

        [TestCase(FileAccess.Read, false)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, true)]
        public void ContainsKeyPermissions(FileAccess permissions, bool shouldThrow)
        {
            var store = new TestLocalObjectStore();
            store.Initialize($"CSharp-TestAlgorithm-{permissions}", 0, 0, "", new Controls {StoragePermissions = permissions});

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.ContainsKey("Jose"));
            }
            else
            {
                Assert.IsFalse(store.ContainsKey("Jose"));
            }
        }

        [TestCase(FileAccess.Read, false)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, true)]
        public void InitializationPermissions(FileAccess permissions, bool shouldThrow)
        {
            var store = new TestLocalObjectStore();
            var dir = Path.Combine(TestStorageRoot, $"CSharp-TestAlgorithm-8");
            Directory.CreateDirectory(dir);

            //Determine filename for key "Jose" using Base64
            var filename = Convert.ToBase64String(Encoding.UTF8.GetBytes("Jose"));
            File.WriteAllText(Path.Combine(dir, filename), "Pepe");

            store.Initialize($"CSharp-TestAlgorithm-8", 0, 0, "", new Controls { StoragePermissions = permissions });

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.ContainsKey("Jose"));
            }
            else
            {
                Assert.IsTrue(store.ContainsKey("Jose"));
            }

            Directory.Delete(dir, true);
        }

        [Test]
        public void PersistCalledSynchronously()
        {
            var store = new TestLocalObjectStore();
            store.Initialize("CSharp-TestAlgorithm2", 0, 0, "", new Controls
            {
                PersistenceIntervalSeconds = -1
            });

            store.SaveBytes("Pepe", new byte[] {1});
            Assert.AreEqual(1, store.ReadBytes("Pepe").Single());
            Assert.IsTrue(store.PersistDataCalled);

            store.PersistDataCalled = false;

            store.Delete("Pepe");
            Assert.IsTrue(store.PersistDataCalled);
            Assert.IsFalse(store.ContainsKey("Pepe"));

            store.DisposeSafely();
        }

        [Test]
        public void ThrowsKeyNotFoundException_WhenObjectStoreDoesNotContainKey()
        {
            var error = Assert.Throws<KeyNotFoundException>(
                () => _store.ReadBytes("missing.missing")
            );

            Assert.IsTrue(error.Message.Contains("Please use ObjectStore.ContainsKey(key)"));
        }

        [TestCase("my_key", "./LocalObjectStoreTests/CSharp-TestAlgorithm/bXlfa2V5")]
        [TestCase("test/123", "./LocalObjectStoreTests/CSharp-TestAlgorithm/dGVzdC8xMjM=")]
        [TestCase("**abc**", "./LocalObjectStoreTests/CSharp-TestAlgorithm/KiphYmMqKg==")]
        [TestCase("<random>", "./LocalObjectStoreTests/CSharp-TestAlgorithm/PHJhbmRvbT4=")]
        [TestCase("|", "./LocalObjectStoreTests/CSharp-TestAlgorithm/fA==")]
        public void GetFilePathReturnsFileName(string key, string expectedRelativePath)
        {
            var expectedPath = Path.GetFullPath(expectedRelativePath).Replace("\\", "/");
            _store.SaveString(key, "data");
            Assert.AreEqual(expectedPath, _store.GetFilePath(key).Replace("\\", "/"));
        }

        [Test]
        public void SavesAndLoadsText()
        {
            const string expectedText = "12;26";

            Assert.IsTrue(_store.SaveString("my_settings_text", expectedText));

            var actualText = _store.Read("my_settings_text");

            Assert.AreEqual(expectedText, actualText);
        }

        [Test]
        public void SizeLimitIsRespected()
        {
            {
                var validData = new byte[1024 * 1024 * 4];
                Assert.IsTrue(_store.SaveBytes("my_settings_text", validData));
            }
            {
                var invalidData = new byte[1024 * 1024 * 6];
                Assert.IsFalse(_store.SaveBytes("my_settings_text", invalidData));
            }
            _store.Delete("my_settings_text");
        }

        [Test]
        public void SavesAndLoadsJson()
        {
            var expected = new TestSettings { EmaFastPeriod = 12, EmaSlowPeriod = 26 };

            Assert.IsTrue(_store.SaveJson("my_settings_json", expected));

            var actual = _store.ReadJson<TestSettings>("my_settings_json");

            Assert.AreEqual(expected.EmaFastPeriod, actual.EmaFastPeriod);
            Assert.AreEqual(expected.EmaSlowPeriod, actual.EmaSlowPeriod);
        }

        [Test]
        public void SavesAndLoadsXml()
        {
            var expected = new TestSettings { EmaFastPeriod = 12, EmaSlowPeriod = 26 };

            Assert.IsTrue(_store.SaveXml("my_settings_xml", expected));

            var actual = _store.ReadXml<TestSettings>("my_settings_xml");

            Assert.AreEqual(expected.EmaFastPeriod, actual.EmaFastPeriod);
            Assert.AreEqual(expected.EmaSlowPeriod, actual.EmaSlowPeriod);
        }

        [Test]
        public void ThrowsIfKeyIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _store.ContainsKey(null));
            Assert.Throws<ArgumentNullException>(() => _store.ReadBytes(null));
            Assert.Throws<ArgumentNullException>(() => _store.SaveBytes(null, null));
            Assert.Throws<ArgumentNullException>(() => _store.Delete(null));
            Assert.Throws<ArgumentNullException>(() => _store.GetFilePath(null));
        }

        [Test]
        public void DisposeRemovesEmptyStorageFolder()
        {
            using (var store = new LocalObjectStore())
            {
                store.Initialize("unused", 0, 0, "", new Controls());

                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests/unused"));
            }

            Assert.IsFalse(Directory.Exists("./LocalObjectStoreTests/unused"));
        }

        [Test]
        public void DisposeDoesNotDeleteStoreFiles()
        {
            string path;
            using (var store = new LocalObjectStore())
            {
                store.Initialize("test", 0, 0, "", new Controls() {PersistenceIntervalSeconds = -1});
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests/test"));

                var validData = new byte[1024 * 1024 * 4];
                var saved = store.SaveBytes("a.txt", validData);
                Assert.IsTrue(saved);

                path = store.GetFilePath("a.txt");
                Assert.IsTrue(File.Exists(path));
            }

            // Check that it still exists
            Assert.IsTrue(File.Exists(path));
        }

        [Test]
        public void QuantBookObjectStoreBehavior()
        {
            // Test for issue #4811, on loop store objects would duplicate
            for (int i = 0; i < 3; i++)
            {
                // Create a QuantBook and save some data
                var qb = new QuantBook();
                qb.ObjectStore.Save("a.txt", "1010101010101010101010");
                Assert.IsTrue(qb.ObjectStore.ContainsKey("a.txt"));

                // Assert the store has only a.txt
                var store = qb.ObjectStore.GetEnumerator().AsEnumerable().ToList();
                Assert.IsTrue(store.Count == 1);
                Assert.IsTrue(store[0].Key == "a.txt");

                // Get the file path and verify it exists
                var path = qb.ObjectStore.GetFilePath("a.txt");
                Assert.IsTrue(File.Exists(path));
            }
        }

        [Test]
        public void OversizedObject()
        {
            // Create a big byte array
            var bytesToWrite = new byte[7000000];

            // Attempt to save it to local store with 5MB cap
            Assert.IsFalse(_store.SaveBytes("test", bytesToWrite));
        }

        [Test]
        public void TooManyObjects()
        {
            // Write 100 Files first, should not throw
            for (int i = _store.Count(); i < 100; i++)
            {
                Assert.IsTrue(_store.SaveString($"{i}", $"{i}"));
            }

            // Write 1 more; should throw
            Assert.IsFalse(_store.SaveString("breaker", "gotem"));
        }

        [Test]
        public void DeletedObjectIsNotReloaded()
        {
            using (var store = new LocalObjectStore())
            {
                store.Initialize("test", 0, 0, "", new Controls());
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests/test"));

                var validData = new byte[1024 * 4];
                store.SaveBytes("a.txt", validData);
                Assert.IsTrue(store.ContainsKey("a.txt"));

                store.SaveBytes("b.txt", validData);
                Assert.IsTrue(store.ContainsKey("b.txt"));

                // Assert the store has our two objects
                var storedObj = store.GetEnumerator().AsEnumerable().ToList();
                Assert.IsTrue(storedObj.Count == 2);

                // Delete a.txt and close this store down
                store.Delete("a.txt");
                Assert.IsFalse(store.ContainsKey("a.txt"));
            }

            using (var store = new LocalObjectStore())
            {
                // Check that the dir still exists, it had files so it shouldn't have deleted
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests/test"));
                store.Initialize("test", 0, 0, "", new Controls());

                // Check our files; a should be gone, b should be there
                Assert.IsFalse(store.ContainsKey("a.txt"));
                Assert.IsTrue(store.ContainsKey("b.txt"));
            }
        }

        public class TestSettings
        {
            public int EmaFastPeriod { get; set; }
            public int EmaSlowPeriod { get; set; }
        }

        private class TestLocalObjectStore : LocalObjectStore
        {
            public bool PersistDataCalled { get; set; }
            protected override bool PersistData(IEnumerable<KeyValuePair<string, byte[]>> data)
            {
                PersistDataCalled = true;
                return base.PersistData(data);
            }
        }
    }
}
