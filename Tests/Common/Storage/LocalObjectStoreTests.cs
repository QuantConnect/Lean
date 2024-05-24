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
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Storage;
using QuantConnect.Research;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.Storage;
using System.Threading;

namespace QuantConnect.Tests.Common.Storage
{
    [TestFixture]
    public class LocalObjectStoreTests
    {
        private static readonly string TestStorageRoot = $"{Directory.GetCurrentDirectory()}/{nameof(LocalObjectStoreTests)}";
        private static readonly string StorageRootConfigurationValue = Config.Get("object-store-root");

        private ObjectStore _store;
        private ILogHandler _logHandler;

        [OneTimeSetUp]
        public void Setup()
        {
            Config.Set("object-store-root", TestStorageRoot);

            _store = new ObjectStore(new TestLocalObjectStore());
            _store.Initialize(0, 0, "", new Controls() { StorageLimit = 5 * 1024 * 1024, StorageFileCount = 100 });

            // Store initial Log Handler
            _logHandler = Log.LogHandler;
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

            // Restore initial Log Handler
            Log.LogHandler = _logHandler;
        }

        [Test]
        public void ExistingFilesLoadedCorretly()
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                var dir = Path.Combine(TestStorageRoot, "location-pepe", "test");
                Directory.CreateDirectory(dir);

                var filename = "Jose";
                var filename2 = "rootFile";
                File.WriteAllText(Path.Combine(dir, filename), "pinocho the movie");
                File.WriteAllText(Path.Combine(TestStorageRoot, filename2), "jiji");

                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });

                var storeContent = store.ToList();

                Assert.IsTrue(storeContent.All(kvp => kvp.Value != null));

                Assert.AreEqual(2, storeContent.Count);
                Assert.AreEqual("location-pepe/test/Jose", storeContent.Single(s => s.Key.Contains("location")).Key.Replace('\\', '/'));
                Assert.AreEqual("rootFile", storeContent.Single(s => s.Key.Contains("rootFile")).Key);

                Assert.IsTrue(File.Exists(store.GetFilePath("location-pepe/test/Jose")));
                Assert.IsTrue(File.Exists(store.GetFilePath("rootFile")));

                Assert.IsTrue(store.Delete("location-pepe/test/Jose"));
                Assert.IsTrue(store.Delete("rootFile"));
            }
        }

        [TestCase(FileAccess.Read, true)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, true)]
        public void GetFilePathPermissions(FileAccess permissions, bool shouldThrow)
        {
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StoragePermissions = permissions });

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.GetFilePath("Jose"));
            }
            else
            {
                Assert.DoesNotThrow(() => store.GetFilePath("Jose"));
            }
        }

        [TestCase(FileAccess.Read, false)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, true)]
        public void ReadBytesPermissions(FileAccess permissions, bool shouldThrow)
        {
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StoragePermissions = permissions });

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
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StoragePermissions = permissions });

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.SaveBytes("Jose", new byte[] { 0 }));
            }
            else
            {
                Assert.IsTrue(store.SaveBytes("Jose", new byte[] { 0 }));
                Assert.IsTrue(store.Delete("Jose"));
            }
        }

        [TestCase(FileAccess.Read, true)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, false)]
        public void DeletePermissions(FileAccess permissions, bool shouldThrow)
        {
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StoragePermissions = permissions });

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.Delete("Jose"));
            }
            else
            {
                Assert.IsFalse(store.Delete("Jose"));
            }
        }

        [TestCase("../prefix/")]
        [TestCase("..\\prefix/")]
        public void InvalidCustomPathsStore(string path)
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                Assert.AreEqual(0, store.Count());

                Assert.Throws<ArgumentException>(() => store.SaveString($"{path}ILove", "Pizza"));
            }
        }

        [Test]
        public void ValidPaths()
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });

                store.SaveString("jose-something/pepe/ILove", "Pizza");
                Assert.AreEqual(1, store.Count());
                Assert.AreEqual(1, Directory.EnumerateFiles(Path.Combine(TestStorageRoot, "jose-something", "pepe")).Count());

                store.Delete("jose-something/pepe/ILove");
                Assert.AreEqual(0, store.Count());
                Assert.AreEqual(0, Directory.EnumerateFiles(TestStorageRoot, "*", SearchOption.AllDirectories).Count());
            }
        }

        [TestCase("prefix/")]
        [TestCase("/prefix/")]
        [TestCase("/prefix")]
        [TestCase("prefix")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("a/prefix/")]
        [TestCase("a/super/prefix/")]
        [TestCase("/a/super/prefix/")]
        [TestCase("/a/super/prefix")]
        [TestCase("./a/su-p_er\\pr##efi$x")]
        [TestCase("./a/super/prefix")]
        [TestCase("./a/su-p_er\\pr x=")]
        public void CustomPrefixStore(string prefix)
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                Assert.AreEqual(0, store.Count());

                var key = "ILove";
                if (prefix != null)
                {
                    key = Path.Combine(prefix, key);
                }
                store.SaveString(key, "Pizza");
                Assert.AreEqual(1, store.Count());
                Assert.AreEqual(1, Directory.EnumerateFiles(TestStorageRoot, "*", SearchOption.AllDirectories).Count());

                var data = store.Read(key);
                Assert.AreEqual("Pizza", data);

                var path = store.GetFilePath(key);

                Assert.IsTrue(File.Exists(path));
                Assert.IsTrue(store.Delete(key));
                Assert.IsFalse(File.Exists(path));
            }
        }

        [TestCase(2)]
        [TestCase(1)]
        [TestCase(0)]
        public void KeysBehavior(int useCase)
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                var key = "ILove";
                store.SaveString(key, "Pizza");
                var path = store.GetFilePath(key);

                if (useCase == 0)
                {
                    // delete
                    Assert.IsTrue(store.Delete(key));
                    Assert.IsFalse(File.Exists(path));
                    Assert.AreEqual(0, store.Keys.Count);
                }
                else if (useCase == 1)
                {
                    // read
                    Assert.AreEqual(key, store.Keys.Single());
                }
                else if (useCase == 2)
                {
                    // new file
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), "some other-file"), "Pepe");

                    Assert.AreEqual(2, store.Keys.Count);
                    Assert.AreEqual(1, store.Keys.Count(k => k == key));
                    Assert.AreEqual(1, store.Keys.Count(k => k == "some other-file"));

                    Assert.IsTrue(store.Delete("some other-file"));
                }

                // clean up
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [TestCase(5)]
        [TestCase(4)]
        [TestCase(3)]
        [TestCase(2)]
        [TestCase(1)]
        [TestCase(0)]
        public void AfterClearState(int useCase)
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                var key = "ILove";
                store.SaveString(key, "Pizza");
                var path = store.GetFilePath(key);
                // CLEAR the state
                store.Clear();

                if (useCase == 0)
                {
                    // delete
                    Assert.IsTrue(store.Delete(key));
                    Assert.IsFalse(File.Exists(path));
                }
                else if (useCase == 1)
                {
                    // read
                    Assert.AreEqual("Pizza", store.ReadString(key));
                }
                else if (useCase == 2)
                {
                    // enumeration
                    Assert.AreEqual("Pizza", store.Single().Value);
                }
                else if (useCase == 3)
                {
                    // keys
                    Assert.AreEqual(key, store.Keys.Single());
                }
                else if (useCase == 4)
                {
                    // get file path
                    Assert.AreEqual(path, store.GetFilePath(key));
                }
                else if (useCase == 5)
                {
                    // new file
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), "some other-file"), "Pepe");

                    // read new file
                    Assert.AreEqual("Pepe", store.ReadString("some other-file"));
                    Assert.IsTrue(store.Delete("some other-file"));
                }

                // clean up
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void GetFilePathAndDelete()
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));

                var key = "ILove";
                store.SaveString(key, "Pizza");
                var path = store.GetFilePath(key);

                Assert.IsTrue(File.Exists(path));
                store.Delete(key);

                Assert.IsFalse(File.Exists(path));
            }
        }

        [Test]
        public void SaveAndDelete()
        {
            string path;
            using (var store = new TestLocalObjectStore())
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = 1 }, new TestFileHandler());
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));
                var key = "ILove";
                path = store.GetFilePath(key);
                store.SaveBytes(key, new byte[] { 1 });
                Thread.Sleep(2000);
                store.Delete(key);

                Assert.IsTrue(store.PersistDataCalled, "PersistData() was never called!");
            }
            Assert.IsFalse(File.Exists(path));
        }

        [TestCase(FileAccess.Read, false)]
        [TestCase(FileAccess.ReadWrite, false)]
        [TestCase(0, true)]
        [TestCase(FileAccess.Write, true)]
        public void ContainsKeyPermissions(FileAccess permissions, bool shouldThrow)
        {
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StoragePermissions = permissions });

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
            using var store = new TestLocalObjectStore();
            var dir = Path.Combine(TestStorageRoot);
            Directory.CreateDirectory(dir);

            //Determine filename for key "Jose" using Base64
            var filename = "Jose";
            File.WriteAllText(Path.Combine(dir, filename), "Pepe");

            store.Initialize(0, 0, "", new Controls { StoragePermissions = permissions });

            if (shouldThrow)
            {
                Assert.Throws<InvalidOperationException>(() => store.ContainsKey(filename));
            }
            else
            {
                Assert.IsTrue(store.ContainsKey(filename));
            }

            Directory.Delete(dir, true);
        }

        [Test]
        public void PersistCalledSynchronously()
        {
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls
            {
                PersistenceIntervalSeconds = -1
            });

            store.SaveBytes("Pepe", new byte[] { 1 });
            Assert.AreEqual(1, store.ReadBytes("Pepe").Single());
            Assert.IsTrue(store.PersistDataCalled);

            store.PersistDataCalled = false;

            store.Delete("Pepe");
            Assert.IsFalse(File.Exists(Path.Combine(TestStorageRoot, "Pepe")));
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

        [TestCase("my_key", "./LocalObjectStoreTests/my_key")]
        [TestCase("test/123", "./LocalObjectStoreTests/test/123")]
        [TestCase("**abc**", null)]
        [TestCase("<random>", null)]
        [TestCase("|", null)]
        public void GetFilePathReturnsFileName(string key, string expectedRelativePath)
        {
            if (expectedRelativePath == null)
            {
                Assert.Throws<ArgumentException>(() => _store.GetFilePath(key));
            }
            else
            {
                var expectedPath = Path.GetFullPath(expectedRelativePath).Replace("\\", "/");
                Assert.AreEqual(expectedPath, _store.GetFilePath(key).Replace("\\", "/"));
            }
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
        public void DisposeDoesNotRemovesEmptyStorageFolder()
        {
            using (var store = new TestLocalObjectStore())
            {
                store.Initialize(0, 0, "", new Controls());

                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));
            }

            Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));
        }

        [Test]
        public void DisposeDoesNotErrorWhenStorageFolderAlreadyDeleted()
        {
            var testHandler = new QueueLogHandler();
            Log.LogHandler = testHandler;

            using (var store = new TestLocalObjectStore())
            {
                store.Initialize(0, 0, "", new Controls());

                Directory.Delete("./LocalObjectStoreTests/", true);
            }

            Assert.IsFalse(testHandler.Logs.Any(message =>
                message.Message.Contains("Error deleting storage directory.")));
        }

        [Test]
        public void DisposeDoesNotDeleteStoreFiles()
        {
            string path;
            using (var store = new TestLocalObjectStore())
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));

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
                Assert.AreEqual(1, store.Count);
                // 0 being the project id, default prefix
                Assert.AreEqual(Path.Combine("a.txt"), store[0].Key);

                // Get the file path and verify it exists
                var path = qb.ObjectStore.GetFilePath("a.txt");
                Assert.IsTrue(File.Exists(path));

                Assert.IsTrue(qb.ObjectStore.Delete("a.txt"));
                Assert.IsFalse(File.Exists(path));
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

        [TestCase(true)]
        [TestCase(false)]
        public void TooManyObjects(bool usingObjectStore)
        {
            using (var store = new TestLocalObjectStore())
            {
                store.Initialize(0, 0, "", new Controls() { StorageLimit = 5 * 1024 * 1024, StorageFileCount = 100 });
                // Write 100 Files first, should not throw
                var start = store.Count();
                for (var i = start; i < 100; i++)
                {
                    if (usingObjectStore)
                    {
                        Assert.IsTrue(store.SaveBytes($"{i}", new byte[1]));
                    }
                    else
                    {
                        File.WriteAllBytes(Path.Combine(TestStorageRoot, $"{i}"), new byte[1]);
                    }
                }

                // Write 1 more; should throw
                Assert.IsFalse(store.SaveBytes("breaker", new byte[1]));

                // cleaup
                for (var i = start; i < 100; i++)
                {
                    Assert.IsTrue(store.Delete($"{i}"));
                }
            }
        }

        [Test]
        public void WriteFromExternalMethodAndSaveFromSource()
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));

                var key = "Test";
                var content = "Example text";

                var path = store.GetFilePath(key);

                DummyMachineLearning(path, content);
                store.Save(key);

                var storeContent = store.Read(key);
                Assert.AreEqual(content, storeContent);
            }
        }

        [TestCase("/test/", "test")]
        [TestCase("test\\", "test")]
        [TestCase("test", "LocalObjectStoreTests")]
        [TestCase("abc/12 3/test", "12 3")]
        [TestCase("abc\\1 23\\test", "1 23")]
        [TestCase("/abc\\1 23\\test", "1 23")]
        [TestCase("\\abc\\1 23\\test", "1 23")]
        public void GetFilePathMethodWorksProperly(string key, string expectedParentName)
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));

                var path = store.GetFilePath(key);
                // paths are always under the object store root path
                Assert.IsTrue(path.Contains("LocalObjectStoreTests", StringComparison.InvariantCultureIgnoreCase));
                Assert.IsFalse(File.Exists(path));
                Assert.IsNull(store.Read(key));
                // the parent of the path requested will be created
                var parent = Directory.GetParent(path);
                Assert.AreEqual(expectedParentName, parent.Name);
                Assert.IsTrue(parent.Exists);
            }
        }

        [Test]
        public void TrySaveKeyWithNotFileAssociated()
        {
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));

                var key = "test";
                Assert.Throws<ArgumentException>(() => store.Save(key));
            }
        }

        [TestCase(1)]
        [TestCase(0)]
        public void NewUnregisteredFileIsAvailable(int useCase)
        {
            using (var store = new TestLocalObjectStore())
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });

                // create 'Jose' file in the object store After initialize
                var joseFile = Path.Combine(TestStorageRoot, "JoseNew2");
                File.WriteAllText(joseFile, "Pepe");

                if (useCase == 0)
                {
                    Assert.IsTrue(store.ContainsKey("JoseNew2"));
                }
                else if (useCase == 1)
                {
                    Assert.IsNotNull(store.ReadBytes("JoseNew2"));
                }

                // clean up
                File.Delete(joseFile);
            }
        }

        [Test]
        public void NewUnregisteredFileIsNotDeleted()
        {
            using (var store = new TestLocalObjectStore())
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });

                // create 'Jose' file in the object store After initialize
                var joseFile = Path.Combine(TestStorageRoot, "JoseNew");
                File.WriteAllText(joseFile, "Pepe");

                store.SaveBytes("a.txt", new byte[1024 * 4]);
                Assert.IsTrue(store.ContainsKey("a.txt"));

                Assert.IsTrue(File.Exists(joseFile));

                // clean up
                store.Delete("a.txt");
                File.Delete(joseFile);
            }
        }

        [Test]
        public void NewUnregisteredFileCanBeDeleted()
        {
            using (var store = new TestLocalObjectStore())
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });

                // create 'Jose' file in the object store After initialize
                var joseFile = Path.Combine(TestStorageRoot, "JoseNew77");
                File.WriteAllText(joseFile, "Pepe");

                Assert.IsTrue(File.Exists(joseFile));

                Assert.IsTrue(store.Delete("JoseNew77"));

                Assert.IsFalse(File.Exists(joseFile));
            }
        }

        [Test]
        public void DeletedObjectIsNotReloaded()
        {
            using (var store = new TestLocalObjectStore())
            {
                store.Initialize(0, 0, "", new Controls());
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));

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

            using (var store = new TestLocalObjectStore())
            {
                // Check that the dir still exists, it had files so it shouldn't have deleted
                Assert.IsTrue(Directory.Exists("./LocalObjectStoreTests"));
                store.Initialize(0, 0, "", new Controls());

                // Check our files; a should be gone, b should be there
                Assert.IsFalse(store.ContainsKey("a.txt"));
                Assert.IsTrue(store.ContainsKey("b.txt"));
            }
        }

        private static void DummyMachineLearning(string outputFile, string content)
        {
            try
            {
                var sw = new StreamWriter(outputFile);
                sw.Write(content);
                sw.Close();
            }
            catch (Exception e)
            {
                throw e;
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

            public override void Initialize(int userId, int projectId, string userToken, Controls controls)
            {
                base.Initialize(userId, projectId, userToken, controls);
            }

            public void Initialize(int userId, int projectId, string userToken, Controls controls, FileHandler fileHandler)
            {
                FileHandler = fileHandler;
                base.Initialize(userId, projectId, userToken, controls);
            }
            protected override bool PersistData()
            {
                PersistDataCalled = true;
                return base.PersistData();
            }
            protected override string StorageRoot() => TestStorageRoot;
        }

        public class TestFileHandler : FileHandler
        {
            public override void WriteAllBytes(string path, byte[] data)
            {
                // The thread sleeps for 1 second in order to align with the
                // other thread that will try to delete this file (see SaveAndDelete()
                // unit test)
                Thread.Sleep(1000);
                base.WriteAllBytes(path, data);
            }
        }
    }
}
