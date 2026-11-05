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
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Algorithm;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Storage;
using QuantConnect.Research;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.Storage;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using System.Reflection;
using Python.Runtime;

namespace QuantConnect.Tests.Common.Storage
{
    [TestFixture]
    public class LocalObjectStoreTests
    {
        private static readonly string TestStorageRoot = $"{Directory.GetCurrentDirectory()}/{nameof(LocalObjectStoreTests)}";
        private static readonly string StorageRootConfigurationValue = LocalObjectStore.DefaultObjectStore;

        // the Python store so the Python tests share it, the C# tests only reach the base members
        private PythonObjectStore _store;
        private ILogHandler _logHandler;

        [OneTimeSetUp]
        public void Setup()
        {
            LocalObjectStore.DefaultObjectStore = TestStorageRoot;
#pragma warning disable CA2000
            _store = new PythonObjectStore(new TestLocalObjectStore());
#pragma warning restore CA2000
            _store.Initialize(0, 0, "", new Controls() { StorageLimit = 5 * 1024 * 1024, StorageFileCount = 100 }, AlgorithmMode.Backtesting);

            // Store initial Log Handler
            _logHandler = Log.LogHandler;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _store.DisposeSafely();
            LocalObjectStore.DefaultObjectStore = StorageRootConfigurationValue;
            try
            {
                Directory.Delete(TestStorageRoot, true);
            }
            catch
            {
            }

            // Restore initial Log Handler
            Log.LogHandler = _logHandler;
        }

        [Test]
        public void DoesNotYieldTwiceSameFile()
        {
            using var store = new ObjectStore(new TestLocalObjectStore());
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            store.SaveBytes("DoesNotYieldTwiceSameFile", [1, 1]);

            var enumerator = store.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());

            Assert.IsFalse(enumerator.MoveNext());
            store.Delete("DoesNotYieldTwiceSameFile");
        }

        [Test]
        public void DisposeDoesNotHang()
        {
            var store = new ObjectStore(new TestLocalObjectStore());
            store.Initialize(0, 0, "", new Controls(), AlgorithmMode.Backtesting);
            Assert.IsTrue(store.SaveBytes("DisposeDoesNotHang", [1, 1]));

            using var testEvent = new AutoResetEvent(false);
            _ = Task.Factory.StartNew(() =>
            {
                var enumerator = store.GetEnumerator();
                Assert.IsTrue(enumerator.MoveNext());
                testEvent.Set();
                Thread.Sleep(Time.OneSecond);
            }, TaskCreationOptions.LongRunning);
            testEvent.WaitOne();

            store.Delete("DisposeDoesNotHang");
            _ = Task.Factory.StartNew(() =>
            {
                store.Dispose();
                testEvent.Set();
            }, TaskCreationOptions.LongRunning);
            Assert.IsTrue(testEvent.WaitOne(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void ExistingFilesLoadedCorretly()
        {
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                var dir = Path.Combine(TestStorageRoot, "location-pepe", "test");
                Directory.CreateDirectory(dir);

                var filename = "Jose";
                var filename2 = "rootFile";
                File.WriteAllText(Path.Combine(dir, filename), "pinocho the movie");
                File.WriteAllText(Path.Combine(TestStorageRoot, filename2), "jiji");

                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);

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

        [Test]
        public void GetFilePathPermissions([Values] bool read, [Values] bool write, [Values] bool delete)
        {
            var permissions = new QuantConnect.Packets.StoragePermissions()
            {
                Read = read,
                Write = write,
                Delete = delete
            };
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StorageAccess = permissions });

            if (!read || !write)
            {
                Assert.Throws<InvalidOperationException>(() => store.GetFilePath("Jose"));
            }
            else
            {
                Assert.DoesNotThrow(() => store.GetFilePath("Jose"));
            }
        }

        [Test]
        public void ReadBytesPermissions([Values] bool read, [Values] bool write, [Values] bool delete)
        {
            var permissions = new QuantConnect.Packets.StoragePermissions()
            {
                Read = read,
                Write = write,
                Delete = delete
            };
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StorageAccess = permissions });

            if (!read)
            {
                Assert.Throws<InvalidOperationException>(() => store.ReadBytes("Jose"));
            }
            else
            {
                Assert.Throws<KeyNotFoundException>(() => store.ReadBytes("Jose"));
            }
        }

        [Test]
        public void SaveBytesPermissions([Values] bool read, [Values] bool write)
        {
            var permissions = new QuantConnect.Packets.StoragePermissions()
            {
                Read = read,
                Write = write,
                Delete = true
            };
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StorageAccess = permissions });

            if (!write)
            {
                Assert.Throws<InvalidOperationException>(() => store.SaveBytes("Jose", new byte[] { 0 }));
            }
            else
            {
                Assert.IsTrue(store.SaveBytes("Jose", new byte[] { 0 }));
                Assert.IsTrue(store.Delete("Jose"));
            }
        }

        [Test]
        public void DeletePermissions([Values] bool read, [Values] bool write, [Values] bool delete)
        {
            var permissions = new QuantConnect.Packets.StoragePermissions()
            {
                Read = read,
                Write = write,
                Delete = delete
            };
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StorageAccess = permissions });

            if (!delete)
            {
                Assert.Throws<InvalidOperationException>(() => store.Delete("Jose"));
            }
            else
            {
                Assert.IsFalse(store.Delete("Jose"));
            }
        }

        [TestCase("../prefix/", "__/prefix/")]
        [TestCase("..\\prefix/", "__/prefix/")]
        public void InvalidCustomPathsStore(string path, string sanitizedPath)
        {
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
                Assert.AreEqual(0, store.Count());

                // '..' cannot leave the storage root: it is sanitized away
                Assert.IsTrue(store.SaveString($"{path}ILove", "Pizza"));
                Assert.AreEqual(new[] { $"{sanitizedPath}ILove", $"{path}ILove" }, store.Keys);
                Assert.That(Path.GetFullPath(store.GetFilePath($"{path}ILove")), Does.StartWith(Path.GetFullPath(TestStorageRoot)));

                store.Delete($"{path}ILove");
            }
        }

        [Test]
        public void ValidPaths()
        {
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);

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
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
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
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
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
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
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
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
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

        [Test]
        public void ContainsKeyPermissions([Values] bool read, [Values] bool write, [Values] bool delete)
        {
            var permissions = new QuantConnect.Packets.StoragePermissions()
            {
                Read = read,
                Write = write,
                Delete = delete
            };
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls { StorageAccess = permissions });

            if (!read)
            {
                Assert.Throws<InvalidOperationException>(() => store.ContainsKey("Jose"));
            }
            else
            {
                Assert.IsFalse(store.ContainsKey("Jose"));
            }
        }

        [Test]
        public void InitializationPermissions([Values] bool read, [Values] bool write, [Values] bool delete)
        {
            var permissions = new QuantConnect.Packets.StoragePermissions()
            {
                Read = read,
                Write = write,
                Delete = delete
            };
            using var store = new TestLocalObjectStore();
            var dir = Path.Combine(TestStorageRoot);
            Directory.CreateDirectory(dir);

            //Determine filename for key "Jose" using Base64
            var filename = "Jose";
            File.WriteAllText(Path.Combine(dir, filename), "Pepe");

            store.Initialize(0, 0, "", new Controls { StorageAccess = permissions });

            if (!read)
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

        [Test]
        public void KeyNotFoundErrorListsTheAvailableKeys()
        {
            // dedicated store instance: its in-memory keys are listed first in the error
            using var store = new ObjectStore(new TestLocalObjectStore());
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            Assert.IsTrue(store.SaveBytes("available_key_a.txt", new byte[] { 1 }));
            Assert.IsTrue(store.SaveBytes("available_key_b.txt", new byte[] { 1 }));

            var error = Assert.Throws<KeyNotFoundException>(() => store.ReadBytes("missing.missing"));

            Assert.IsTrue(error.Message.Contains("Keys: ["), error.Message);
            Assert.IsTrue(error.Message.Contains("'available_key_a.txt'"), error.Message);
            Assert.IsTrue(error.Message.Contains("'available_key_b.txt'"), error.Message);

            store.Delete("available_key_a.txt");
            store.Delete("available_key_b.txt");
        }

        [Test]
        public void KeyNotFoundErrorTruncatesTheAvailableKeysListAtTen()
        {
            using var store = new ObjectStore(new TestLocalObjectStore());
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            for (var i = 0; i < 11; i++)
            {
                Assert.IsTrue(store.SaveBytes($"truncation_key_{i}.txt", new byte[] { 1 }));
            }

            var error = Assert.Throws<KeyNotFoundException>(() => store.ReadBytes("missing.missing"));

            Assert.IsTrue(error.Message.Contains(", ...]"), error.Message);

            for (var i = 0; i < 11; i++)
            {
                store.Delete($"truncation_key_{i}.txt");
            }
        }

        [Test]
        public void QuantBookUsesThePythonObjectStoreInPythonNotebooks()
        {
            // the notebook kernel is detected once, in the static constructor
            var isPythonNotebook = typeof(QuantBook).GetField("_isPythonNotebook", BindingFlags.NonPublic | BindingFlags.Static);
            isPythonNotebook.SetValue(null, true);
            try
            {
                var quantBook = new QuantBook();

                Assert.IsInstanceOf<PythonObjectStore>(quantBook.ObjectStore);
            }
            finally
            {
                isPythonNotebook.SetValue(null, false);
            }
        }

        [Test]
        public void AlgorithmKeepsAnAlreadyWrappedStore()
        {
            using var pythonStore = new PythonObjectStore(new TestLocalObjectStore());
            var algorithm = new QCAlgorithm();

            algorithm.SetObjectStore(pythonStore);

            Assert.AreSame(pythonStore, algorithm.ObjectStore);
        }

        [Test]
        public void SaveTextAndReadTextAreAliasesOfSaveAndRead()
        {
            Assert.IsTrue(_store.SaveText("save_text_alias.txt", "abc"));
            Assert.AreEqual("abc", _store.Read("save_text_alias.txt"));
            Assert.AreEqual("abc", _store.ReadText("save_text_alias.txt"));

            _store.Delete("save_text_alias.txt");
        }

        [TestCase("my_key/file.csv", "my_key/file.csv")]
        [TestCase("folder.v1/file.tar.gz", "folder.v1/file.tar.gz")]
        [TestCase("my.folder/file.name_v2", "my.folder/file.name_v2")]
        [TestCase("trade_log_ai_hardware_&_cloud.csv", "trade_log_ai_hardware___cloud.csv")]
        [TestCase("trade log: AI & Cloud.csv", "trade log_ AI _ Cloud.csv")]
        [TestCase("résumé.pdf", "r_sum_.pdf")]
        [TestCase("a..", "a__")]
        [TestCase("100%.json", "100_.json")]
        [TestCase("..model.pkl", "__model.pkl")]
        [TestCase("a/../b.csv", "a/__/b.csv")]
        [TestCase("a...b.csv", "a___b.csv")]
        [TestCase("file.csv\n", "file.csv_")]
        public void UnsupportedKeysAreSanitizedOnSaveAndFoundByTheKeyTheyWereSavedWith(string key, string sanitizedKey)
        {
            Assert.IsTrue(_store.Save(key, "abc"));

            Assert.IsTrue(_store.Keys.Contains(sanitizedKey), string.Join(", ", _store.Keys));
            Assert.That(_store.Keys.ToList(), Does.Contain(key));
            Assert.IsTrue(_store.ContainsKey(key));
            Assert.IsTrue(_store.ContainsKey(sanitizedKey));
            Assert.AreEqual("abc", _store.Read(key));
            Assert.AreEqual("abc", _store.Read(sanitizedKey));

            Assert.IsTrue(_store.Delete(key));
            Assert.IsFalse(_store.ContainsKey(sanitizedKey));
            Assert.That(_store.Keys.ToList(), Does.Not.Contain(key));
        }

        [TestCase("../secrets.json", "__/secrets.json")]
        [TestCase("../../secrets.json", "__/__/secrets.json")]
        [TestCase("..\\secrets.json", "__\\secrets.json")]
        [TestCase("x/../secrets.json", "x/__/secrets.json")]
        [TestCase("x/../../secrets.json", "x/__/__/secrets.json")]
        [TestCase("x\\..\\..\\secrets.json", "x\\__\\__\\secrets.json")]
        [TestCase("x/.../secrets.json", "x/___/secrets.json")]
        [TestCase("x/..", "x/__")]
        [TestCase("x/....", "x/____")]
        public void TraversalKeysAreSanitizedAndStayInsideTheStorageRoot(string key, string sanitizedKey)
        {
            // '..' never reaches the file system, so a key cannot point at another folder's or organization's objects
            var warnings = new List<string>();
            using var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add);
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            var root = Path.GetFullPath(TestStorageRoot) + Path.DirectorySeparatorChar;

            Assert.That(Path.GetFullPath(store.GetFilePath(key)), Does.StartWith(root));
            Assert.IsTrue(store.Save(key, "abc"));

            Assert.IsTrue(store.Keys.Contains(sanitizedKey.Replace('\\', '/')), string.Join(", ", store.Keys));
            // the keys list the key as saved too, the objects are only stored under names without '..'
            Assert.That(store.Keys.ToList(), Does.Contain(key));
            Assert.IsFalse(store.Any(entry => entry.Key.Contains("..")), string.Join(", ", store.Select(entry => entry.Key)));
            Assert.That(Path.GetFullPath(store.GetFilePath(sanitizedKey)), Does.StartWith(root));
            Assert.AreEqual("abc", store.Read(key));
            Assert.That(warnings[0], Does.Contain("it contains '..'"));

            Assert.IsTrue(store.Delete(key));
            Assert.IsFalse(store.ContainsKey(sanitizedKey));
        }

        [Test]
        public void GetFilePathOfAnUnsupportedKeyIsTheFileTheSaveThatFollowsReads()
        {
            var warnings = new List<string>();
            using var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add);
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);

            var filePath = store.GetFilePath("model (1).pkl");
            Assert.AreEqual(Path.GetFullPath(Path.Combine(TestStorageRoot, "model _1_.pkl")), Path.GetFullPath(filePath));
            File.WriteAllText(filePath, "abc");

            Assert.IsTrue(store.Save("model (1).pkl"));

            Assert.AreEqual("abc", store.Read("model (1).pkl"));
            Assert.AreEqual("abc", store.Read("model _1_.pkl"));
            Assert.AreEqual(1, warnings.Count, string.Join("\n", warnings));
            Assert.IsTrue(store.Delete("model (1).pkl"));
            Assert.IsFalse(store.ContainsKey("model _1_.pkl"));
        }

        [Test]
        public void UnsupportedKeysAreWarnedAboutOnceStatingTheIssueAndTheKeyUsed()
        {
            var warnings = new List<string>();
            using var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add);
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);

            Assert.IsTrue(store.Save("trade log: AI & Cloud.csv", "abc"));
            Assert.IsTrue(store.ContainsKey("trade log: AI & Cloud.csv"));
            Assert.AreEqual("abc", store.Read("trade log: AI & Cloud.csv"));
            Assert.IsTrue(store.Save("trade log: AI & Cloud.csv", "abcd"));
            Assert.IsTrue(store.Save("supported.csv", "abc"));

            Assert.AreEqual(1, warnings.Count, string.Join("\n", warnings));
            Assert.AreEqual("ObjectStore: key 'trade log: AI & Cloud.csv' is not supported: unsupported characters ':', '&'. " +
                "Keys may only contain english letters, numbers, spaces and the characters '.', '/', '\\', '_', '#', '-', '$' and '=', " +
                "never two '.' in a row, e.g. 'folder/trade_log-2024.v2.csv'. " +
                "Using 'trade log_ AI _ Cloud.csv' instead. Any other unsupported key is sanitized the same way", warnings[0]);

            // the next unsupported key is sanitized silently
            Assert.IsFalse(store.ContainsKey("trade log: AI.v2.csv"));
            Assert.AreEqual(1, warnings.Count, string.Join("\n", warnings));

            store.Delete("trade log: AI & Cloud.csv");
            store.Delete("supported.csv");
        }

        [TestCase("trade log: AI & Cloud.csv", "unsupported characters ':', '&'")]
        [TestCase("résumé.pdf", "unsupported character 'é'")]
        [TestCase("..model.pkl", "it contains '..'")]
        [TestCase("a/../b.csv", "it contains '..'")]
        [TestCase("trade log: AI..v2.csv", "unsupported character ':'; it contains '..'")]
        public void UnsupportedKeyWarningStatesTheIssue(string key, string expectedIssue)
        {
            var warnings = new List<string>();
            using var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add);
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);

            Assert.IsFalse(store.ContainsKey(key));

            Assert.AreEqual(1, warnings.Count);
            Assert.That(warnings[0], Does.StartWith($"ObjectStore: key '{key}' is not supported: {expectedIssue}."));
        }

        [TestCase("", "the key is empty")]
        [TestCase(null, null)]
        [TestCase(".", "it has no name, only '.' and separators")]
        [TestCase("./", "it has no name, only '.' and separators")]
        [TestCase("/", "it has no name, only '.' and separators")]
        public void EmptyAndNamelessKeysAreRejected(string key, string expectedIssue)
        {
            // nothing to sanitize these into: they throw like the store itself would, without a warning
            var warnings = new List<string>();
            using var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add);
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);

            var error = Assert.Catch<ArgumentException>(() => store.Save(key, "abc"));

            Assert.IsEmpty(warnings);
            if (expectedIssue != null)
            {
                Assert.That(error.Message, Does.Contain($"path is not supported: '{key}': {expectedIssue}."));
            }
        }

        [TestCase("model (1).tar.gz")]
        [TestCase("100%.json")]
        [TestCase("BRK&B.csv")]
        public void ExistingUnsupportedKeysAreUsedAsGiven(string key)
        {
            // an object put in the store by other means (uploaded, copied into the folder) under an unsupported name
            var warnings = new List<string>();
            using var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add);
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            var filePath = Path.Combine(TestStorageRoot, key);
            File.WriteAllText(filePath, "abc");

            Assert.IsTrue(store.ContainsKey(key));
            Assert.AreEqual("abc", store.Read(key));
            Assert.IsTrue(store.Keys.Contains(key), string.Join(", ", store.Keys));
            Assert.AreEqual(Path.GetFullPath(filePath), Path.GetFullPath(store.GetFilePath(key)));
            Assert.IsEmpty(warnings, string.Join("\n", warnings));
            Assert.IsTrue(store.Delete(key));
            Assert.IsFalse(File.Exists(filePath));

            // gone from the store, the key falls back to its sanitized name like any other unsupported key
            Assert.IsFalse(store.ContainsKey(key));
            Assert.AreEqual(1, warnings.Count, string.Join("\n", warnings));
            Assert.That(warnings[0], Does.StartWith($"ObjectStore: key '{key}' is not supported:"));
        }

        [Test]
        public void SavingToAnExistingUnsupportedKeyGoesToTheSanitizedNameWhichReadsThenReturn()
        {
            // an object put in the store by other means under an unsupported name keeps it: saves through that name go to the sanitized one
            var filePath = Path.Combine(TestStorageRoot, "model (1).tar.gz");
            File.WriteAllText(filePath, "old");
            var warnings = new List<string>();
            using (var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
                Assert.AreEqual("old", store.Read("model (1).tar.gz"));
                Assert.IsEmpty(warnings, string.Join("\n", warnings));

                Assert.IsTrue(store.Save("model (1).tar.gz", "new"));

                Assert.AreEqual(1, warnings.Count, string.Join("\n", warnings));
                Assert.That(warnings[0], Does.StartWith("ObjectStore: key 'model (1).tar.gz' is not supported: unsupported characters '(', ')'."));
                Assert.AreEqual("new", store.Read("model (1).tar.gz"));
                Assert.AreEqual("new", store.Read("model _1_.tar.gz"));
                Assert.AreEqual("old", File.ReadAllText(filePath));
            }

            // a later run reads and replaces the sanitized object through the same name
            using (var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add))
            {
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
                Assert.AreEqual("new", store.Read("model (1).tar.gz"));
                Assert.IsTrue(store.Save("model (1).tar.gz", "newer"));
                Assert.AreEqual("newer", store.Read("model _1_.tar.gz"));

                // with the sanitized object gone, the one stored under the name as given is read again
                Assert.IsTrue(store.Delete("model (1).tar.gz"));
                Assert.IsFalse(store.ContainsKey("model _1_.tar.gz"));
                Assert.AreEqual("old", store.Read("model (1).tar.gz"));
                Assert.IsTrue(store.Delete("model (1).tar.gz"));
                Assert.IsFalse(store.ContainsKey("model (1).tar.gz"));
            }
        }

        [Test]
        public void DeletingASavedUnsupportedKeyRemovesTheSavedObjectThenTheOriginal()
        {
            var filePath = Path.Combine(TestStorageRoot, "model (1).tar.gz");
            File.WriteAllText(filePath, "old");
            using var store = new ObjectStore(new TestLocalObjectStore(), _ => { });
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            Assert.IsTrue(store.Save("model (1).tar.gz", "new"));
            Assert.AreEqual("new", store.Read("model (1).tar.gz"));

            // the first delete removes what this store saved, and the key resolves like a fresh store's from then on
            Assert.IsTrue(store.Delete("model (1).tar.gz"));
            Assert.IsFalse(File.Exists(Path.Combine(TestStorageRoot, "model _1_.tar.gz")));
            Assert.IsTrue(store.ContainsKey("model (1).tar.gz"));
            Assert.AreEqual("old", store.Read("model (1).tar.gz"));

            Assert.IsTrue(store.Delete("model (1).tar.gz"));
            Assert.IsFalse(store.ContainsKey("model (1).tar.gz"));
            Assert.IsFalse(File.Exists(filePath));
        }

        [Test]
        public void AFailedSaveToAnUnsupportedKeyDoesNotRedirectItsReads()
        {
            var filePath = Path.Combine(TestStorageRoot, "model (1).tar.gz");
            File.WriteAllText(filePath, "old");
            using var store = new ObjectStore(new TestLocalObjectStore(), _ => { });
            // the 3 bytes stored fill the storage: the save is rejected, not thrown, in backtesting
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1, StorageLimit = 3 }, AlgorithmMode.Backtesting);

            Assert.IsFalse(store.Save("model (1).tar.gz", "new"));

            Assert.IsTrue(store.ContainsKey("model (1).tar.gz"));
            Assert.AreEqual("old", store.Read("model (1).tar.gz"));
            Assert.IsFalse(store.ContainsKey("model _1_.tar.gz"));

            Assert.IsTrue(store.Delete("model (1).tar.gz"));
            Assert.IsFalse(File.Exists(filePath));
        }

        [Test]
        public void CustomDataIsReadFromAnExistingUnsupportedKey()
        {
            using var store = new ObjectStore(new TestLocalObjectStore());
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            File.WriteAllText(Path.Combine(TestStorageRoot, "BRK&B.csv"), "2013-10-07,167.42\n2013-10-08,165.48\n");

            // the data feed reads custom data through the algorithm's store
            using var reader = new ObjectStoreSubscriptionStreamReader(store, "BRK&B.csv");

            Assert.IsFalse(reader.EndOfStream);
            Assert.AreEqual("2013-10-07,167.42", reader.ReadLine());
            Assert.AreEqual("2013-10-08,165.48", reader.ReadLine());
            store.Delete("BRK&B.csv");
        }

        [Test]
        public void SavingAnUnsupportedKeyReplacesTheObjectUnderItsSanitizedName()
        {
            var warnings = new List<string>();
            using var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add);
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            Assert.IsTrue(store.Save("p_q.csv", "direct"));

            Assert.IsTrue(store.Save("p&q.csv", "ampersand"));
            Assert.AreEqual("ampersand", store.Read("p_q.csv"));

            // distinct unsupported keys can share a sanitized name: the last save wins
            Assert.IsTrue(store.Save("p:q.csv", "colon"));
            Assert.AreEqual("colon", store.Read("p&q.csv"));
            Assert.AreEqual("colon", store.Read("p_q.csv"));
            Assert.AreEqual(1, warnings.Count, string.Join("\n", warnings));

            Assert.IsTrue(store.Delete("p&q.csv"));
            Assert.IsFalse(store.ContainsKey("p_q.csv"));
        }

        [Test]
        public void ANewRunSavesThroughTheSameUnsupportedKeyAgain()
        {
            for (var run = 1; run <= 2; run++)
            {
                using var store = new ObjectStore(new TestLocalObjectStore(), _ => { });
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);

                Assert.IsTrue(store.Save("trade log: AI & Cloud.csv", $"run{run}"));

                Assert.AreEqual($"run{run}", store.Read("trade log: AI & Cloud.csv"));
                Assert.AreEqual($"run{run}", store.Read("trade log_ AI _ Cloud.csv"));
            }

            using var lastRun = new ObjectStore(new TestLocalObjectStore(), _ => { });
            lastRun.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            Assert.IsTrue(lastRun.Delete("trade log: AI & Cloud.csv"));
        }

        [Test]
        public void ABackslashKeyIsSavedInEveryRunAsOnTheStoreItAlwaysReadAsASeparator()
        {
            var warnings = new List<string>();
            for (var run = 1; run <= 2; run++)
            {
                using var store = new ObjectStore(new TestLocalObjectStore(), warnings.Add);
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);

                Assert.IsTrue(store.Save("backslash_folder\\results.csv", $"run{run}"));

                Assert.AreEqual($"run{run}", store.Read("backslash_folder\\results.csv"));
                Assert.AreEqual($"run{run}", store.Read("backslash_folder/results.csv"));
                Assert.IsTrue(store.Keys.Contains("backslash_folder/results.csv"), string.Join(", ", store.Keys));
                Assert.IsTrue(store.ContainsKey("backslash_folder\\results.csv"));
            }
            Assert.IsEmpty(warnings, string.Join("\n", warnings));

            using var lastRun = new ObjectStore(new TestLocalObjectStore());
            lastRun.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            Assert.IsTrue(lastRun.Delete("backslash_folder\\results.csv"));
            Assert.IsFalse(lastRun.ContainsKey("backslash_folder/results.csv"));
        }

        [Test]
        public void KeysHoldAnUnsupportedKeySavedInThisRunAndItsSanitizedName()
        {
            using var store = new ObjectStore(new TestLocalObjectStore(), _ => { });
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            Assert.IsTrue(store.Save("keys check: A & B.csv", "abc"));
            Assert.IsTrue(store.Save("keys check: A & B.csv", "def"));
            Assert.IsTrue(store.Save("keys_folder\\file.csv", "abc"));

            var keys = store.Keys;

            var listed = keys.ToList();
            Assert.AreEqual(1, listed.Count(key => key == "keys check: A & B.csv"), string.Join(", ", listed));
            Assert.AreEqual(1, listed.Count(key => key == "keys check_ A _ B.csv"), string.Join(", ", listed));
            Assert.That(listed, Does.Contain("keys_folder/file.csv"));
            Assert.AreEqual(listed.Count, keys.Count);
            var copy = new string[keys.Count];
            keys.CopyTo(copy, 0);
            Assert.AreEqual(listed, copy);

            Assert.IsTrue(keys.Contains("keys check: A & B.csv"));
            Assert.IsTrue(keys.Contains("keys check_ A _ B.csv"));
            Assert.IsTrue(keys.Contains("keys_folder\\file.csv"));
            Assert.IsTrue(keys.Contains("keys_folder/file.csv"));
            Assert.IsFalse(keys.Contains("keys_missing.csv"));
            Assert.IsFalse(keys.Contains(null));

            // a run that did not save through the unsupported key lists the stored name only, and still finds the key
            using (var otherRun = new ObjectStore(new TestLocalObjectStore(), _ => { }))
            {
                otherRun.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
                Assert.That(otherRun.Keys.ToList(), Does.Contain("keys check_ A _ B.csv"));
                Assert.That(otherRun.Keys.ToList(), Does.Not.Contain("keys check: A & B.csv"));
                Assert.IsTrue(otherRun.Keys.Contains("keys check: A & B.csv"));
            }

            // the unsupported key is listed while the object it was saved to exists
            Assert.IsTrue(store.Delete("keys check_ A _ B.csv"));
            Assert.That(store.Keys.ToList(), Does.Not.Contain("keys check: A & B.csv"));
            Assert.IsFalse(store.Keys.Contains("keys check: A & B.csv"));
            Assert.IsTrue(store.Delete("keys_folder\\file.csv"));
        }

        [Test]
        public void KeysListAnExistingUnsupportedNameOnceAfterASaveThroughIt()
        {
            using var store = new ObjectStore(new TestLocalObjectStore(), _ => { });
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
            File.WriteAllText(Path.Combine(TestStorageRoot, "keys legacy (1).csv"), "old");

            Assert.IsTrue(store.Save("keys legacy (1).csv", "new"));

            var listed = store.Keys.ToList();
            Assert.AreEqual(1, listed.Count(key => key == "keys legacy (1).csv"), string.Join(", ", listed));
            Assert.AreEqual(1, listed.Count(key => key == "keys legacy _1_.csv"), string.Join(", ", listed));

            Assert.IsTrue(store.Delete("keys legacy _1_.csv"));
            Assert.IsTrue(store.Delete("keys legacy (1).csv"));
            Assert.That(store.Keys.ToList(), Does.Not.Contain("keys legacy (1).csv"));
        }

        [Test]
        public void KeysListTheStoreOnFirstUseOnlyAndContainsNeverDoes()
        {
            var fileHandler = new ListingCountFileHandler();
            var localStore = new TestLocalObjectStore();
            using var store = new ObjectStore(localStore, _ => { });
            localStore.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, fileHandler);
            Assert.IsTrue(store.Save("lazy keys: A & B.csv", "abc"));

            var listings = fileHandler.Listings;
            var keys = store.Keys;
            Assert.IsTrue(keys.Contains("lazy keys: A & B.csv"));
            Assert.IsTrue(keys.Contains("lazy keys_ A _ B.csv"));
            Assert.IsFalse(keys.Contains("lazy keys missing.csv"));
            using (Py.GIL())
            {
                var module = PyModule.FromString("TestLazyObjectStoreKeys", "def contains(keys, key):\n    return key in keys\n");
                dynamic contains = module.GetAttr("contains");
                PyObject found = contains(store.Keys, "lazy keys: A & B.csv");
                PyObject missing = contains(store.Keys, "lazy keys missing.csv");
                Assert.IsTrue(found.As<bool>());
                Assert.IsFalse(missing.As<bool>());
            }
            Assert.AreEqual(listings, fileHandler.Listings);

            // the listing is built on first use, once per collection
            var listed = keys.ToList();
            Assert.AreEqual(listed.Count, keys.Count);
            Assert.AreEqual(listed, keys.ToList());
            Assert.AreEqual(listings + 1, fileHandler.Listings);
            Assert.That(listed, Does.Contain("lazy keys: A & B.csv"));
            Assert.That(listed, Does.Contain("lazy keys_ A _ B.csv"));

            // a new access lists again, so it sees what changed since
            Assert.IsTrue(store.Save("lazy_keys_later.csv", "abc"));
            listings = fileHandler.Listings;
            Assert.That(store.Keys.ToList(), Does.Contain("lazy_keys_later.csv"));
            Assert.AreEqual(listings + 1, fileHandler.Listings);

            Assert.IsTrue(store.Delete("lazy keys: A & B.csv"));
            Assert.IsTrue(store.Delete("lazy_keys_later.csv"));
        }

        [Test]
        public void QuantBookWarnsThroughTheEngineLog()
        {
            var traces = new List<string>();
            Log.LogHandler = new FunctionalLogHandler(null, traces.Add, null);
            var quantBook = new QuantBook();
            Assert.AreEqual(typeof(ObjectStore), quantBook.ObjectStore.GetType());

            Assert.IsTrue(quantBook.ObjectStore.Save("trade log: AI & Cloud.csv", "abc"));

            Assert.AreEqual(1, traces.Count(trace => trace.Contains("ObjectStore: key 'trade log: AI & Cloud.csv' is not supported: unsupported characters ':', '&'.")),
                string.Join("\n", traces));
            quantBook.ObjectStore.Delete("trade log: AI & Cloud.csv");
        }

        [Test]
        public void LocalObjectStoreRejectsUnsupportedKeysStatingTheIssue()
        {
            // the implementation is the last line of defense, only the wrapper sanitizes
            using var store = new TestLocalObjectStore();
            store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });

            var error = Assert.Throws<ArgumentException>(() => store.SaveBytes("trade_log_ai_hardware_&_cloud.csv", new byte[] { 1 }));

            Assert.AreEqual("ObjectStore: path is not supported: 'trade_log_ai_hardware_&_cloud.csv': unsupported character '&'. " +
                "Keys may only contain english letters, numbers, spaces and the characters '.', '/', '\\', '_', '#', '-', '$' and '=', " +
                "never two '.' in a row, e.g. 'folder/trade_log-2024.v2.csv'", error.Message);
        }

        [Test]
        public void AlgorithmWrapsTheStoreAndWarnsThroughDebug()
        {
            using var localStore = new TestLocalObjectStore();
            localStore.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 });
            var algorithm = new QCAlgorithm();

            algorithm.SetObjectStore(localStore);

            Assert.AreEqual(typeof(ObjectStore), algorithm.ObjectStore.GetType());
            Assert.IsTrue(algorithm.ObjectStore.Save("trade_log_ai_hardware_&_cloud.csv", "abc"));
            Assert.IsTrue(algorithm.DebugMessages.TryDequeue(out var warning));
            Assert.That(warning, Does.Contain("ObjectStore: key 'trade_log_ai_hardware_&_cloud.csv' is not supported: unsupported character '&'."));
            Assert.That(warning, Does.Contain("Using 'trade_log_ai_hardware___cloud.csv' instead"));

            algorithm.ObjectStore.Delete("trade_log_ai_hardware_&_cloud.csv");
        }

        [TestCase("my_key", "./LocalObjectStoreTests/my_key")]
        [TestCase("test/123", "./LocalObjectStoreTests/test/123")]
        [TestCase("**abc**", "./LocalObjectStoreTests/__abc__")]
        [TestCase("<random>", "./LocalObjectStoreTests/_random_")]
        [TestCase("|", "./LocalObjectStoreTests/_")]
        public void GetFilePathReturnsFileName(string key, string expectedRelativePath)
        {
            var expectedPath = Path.GetFullPath(expectedRelativePath).Replace("\\", "/");
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
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
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
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
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
#pragma warning disable CA2000
            using (var store = new ObjectStore(new TestLocalObjectStore()))
            {
#pragma warning restore CA2000
                store.Initialize(0, 0, "", new Controls() { PersistenceIntervalSeconds = -1 }, AlgorithmMode.Backtesting);
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

        [Test]
        public void StoragePropertiesAreAccessible()
        {
            var controls = new Controls { StorageLimit = 536870912, StorageFileCount = 500 };
            using var store = new TestLocalObjectStore();
            store.Initialize(1, 2, "token", controls);

            Assert.AreEqual(536870912, store.MaxSize);
            Assert.AreEqual(500, store.MaxFiles);
        }

        [Test]
        public void StoragePropertiesUseDefaultsFromControls()
        {
            using var store = new TestLocalObjectStore();
            store.Initialize(1, 2, "token", new Controls());

            Assert.AreEqual(10737418240, store.MaxSize);
            Assert.AreEqual(10000, store.MaxFiles);
        }

        [TestCase(1, false)]
        [TestCase(2, true)]
        public void SavingFilesRespectsStorageFileCountLimit(int filesToAdd, bool expectedLimitReached)
        {
            var controls = new Controls { StorageFileCount = 2, StorageLimit = long.MaxValue };
            using var store = new TestLocalObjectStore();
            store.Initialize(1, 2, "token", controls);

            for (int i = 0; i < filesToAdd; i++)
            {
                store.SaveBytes($"/file{i}.txt", new byte[] { 1 });
            }

            Assert.AreEqual(expectedLimitReached, store.Count() == controls.StorageFileCount);

            // Delete all files
            for (int i = 0; i < filesToAdd; i++)
            {
                store.Delete($"/file{i}.txt");
            }
        }

        [Test]
        public void ObjectStoreDelegatesToImplementation()
        {
            using var localStore = new TestLocalObjectStore();
            localStore.Initialize(1, 2, "token", new Controls());
            using var objectStore = new ObjectStore(localStore);

            Assert.AreEqual(localStore.MaxSize, objectStore.MaxSize);
            Assert.AreEqual(localStore.MaxFiles, objectStore.MaxFiles);
            Assert.AreEqual(localStore.Count(), objectStore.Count());
            Assert.AreEqual(localStore.Count() == localStore.MaxFiles, objectStore.Count() == objectStore.MaxFiles);
        }

        [Test]
        public void ResearchModeThrowsExceptionAndCapturesErrorWhenObjectStoreLimitIsReached()
        {
            var testObjectStore = new LocalObjectStore();
            testObjectStore.Initialize(1, 1, "test", new Controls
            {
                StorageFileCount = 2,
                StorageLimit = 100,
                PersistenceIntervalSeconds = -1,
                StorageAccess = new QuantConnect.Packets.StoragePermissions
                {
                    Read = true,
                    Write = true,
                    Delete = true
                }
            }, AlgorithmMode.Research);
            using (Py.GIL())
            {


                var testModule = PyModule.FromString("PythonCustomDataHistoryCanBeFetchedUsingCSharpApi",
                    @"
from AlgorithmImports import *

def clean_data():
    qb = QuantBook()
    for i in range(3):
        qb.object_store.delete(f'file_{i}.txt')

def add_data(object_store):
    qb = QuantBook()
    qb.SetObjectStore(object_store)
    
    max_files = qb.object_store.max_files
    for i in range(max_files):
        result = qb.object_store.save(f'file_{i}.txt', f'data_{i}')

    result = qb.object_store.save(f'file_{max_files}.txt', 'should_fail')
    return result
");

                dynamic result = testModule.GetAttr("add_data");
                var exception = Assert.Catch<Exception>(() => result(testObjectStore));
                Assert.That(exception.Message, Does.Contain("ObjectStore limit"));

                // Reset
                testModule.GetAttr("clean_data").Invoke();
                testObjectStore.Dispose();
            }
        }

        [Test]
        public void PythonSaveJsonAndReadJsonRoundTripTolerantTypes()
        {
            RunPythonTest(@"
def test(object_store):
    symbol = Symbol.create('SPY', SecurityType.EQUITY, Market.USA)

    # datetime/date/Decimal/Symbol values and non-string dictionary keys, which json.dumps rejects
    object_store.save_json('helpers/log.json', {
        'time': datetime(2024, 1, 2, 3, 4, 5),
        'date': datetime(2024, 1, 1).date(),
        'qty': Decimal('1.5'),
        'symbol': symbol,
        'fills': [{symbol: 10}]
    })
    data = object_store.read_json('helpers/log.json')
    # a Symbol value is written like the C# SaveJson<T> does, a Symbol key as its string like a .NET dictionary key
    assert data == {
        'time': '2024-01-02T03:04:05',
        'date': '2024-01-01',
        'qty': 1.5,
        'symbol': {'value': 'SPY', 'id': str(symbol.id), 'permtick': 'SPY'},
        'fills': [{str(symbol): 10}]
    }, data
    # both forms resolve back to the Symbol through the symbol cache, which is what QCAlgorithm.symbol(str) does
    assert SymbolCache.get_symbol(data['symbol']['id']) == symbol
    assert SymbolCache.get_symbol(next(iter(data['fills'][0]))) == symbol

    object_store.delete('helpers/log.json')
");
        }

        [Test]
        public void PythonSaveJsonSerializesDotNetObjectsLikeTheCSharpOverload()
        {
            RunPythonTest(@"
from System.Collections.Generic import List, Dictionary
import Newtonsoft.Json as newtonsoft

def test(object_store):
    numbers = List[int]([1, 2])
    weights = Dictionary[str, float]()
    weights['a'] = 1.5
    point = IndicatorDataPoint(datetime(2020, 1, 1), 5)

    object_store.save_json('helpers/dotnet.json', {'numbers': numbers, 'weights': weights, 'point': point})
    data = object_store.read_json('helpers/dotnet.json')
    assert data == {
        'numbers': [1, 2],
        'weights': {'a': 1.5},
        'point': json.loads(newtonsoft.JsonConvert.serialize_object(point))
    }, data

    object_store.delete('helpers/dotnet.json')
");
        }

        [Test]
        public void PythonSaveJsonConvertsNumpyArraysAndScalars()
        {
            RunPythonTest(@"
import numpy as np

def test(object_store):
    object_store.save_json('helpers/numpy.json', {
        'weights': np.array([1.5, 2.5]),
        'grid': np.array([[1, 2], [3, 4]]),
        'count': np.int64(3),
        'ratio': np.float32(0.5)
    })
    data = object_store.read_json('helpers/numpy.json')
    assert data == {'weights': [1.5, 2.5], 'grid': [[1, 2], [3, 4]], 'count': 3, 'ratio': 0.5}, data

    object_store.delete('helpers/numpy.json')
");
        }

        [Test]
        public void PythonSaveJsonWritesNanAndInfinityAsNull()
        {
            RunPythonTest(@"
import numpy as np

def test(object_store):
    object_store.save_json('helpers/nan.json', {
        'pnl': float('nan'),
        'ratio': float('inf'),
        'drawdown': -float('inf'),
        'series': np.array([1.0, np.nan]),
        'scalar': np.float64('nan')
    })
    text = object_store.read('helpers/nan.json')
    assert 'NaN' not in text and 'Infinity' not in text, text
    data = object_store.read_json('helpers/nan.json')
    assert data == {'pnl': None, 'ratio': None, 'drawdown': None, 'series': [1.0, None], 'scalar': None}, data

    object_store.delete('helpers/nan.json')
");
        }

        [Test]
        public void PythonSaveJsonWritesNumpyDatetimesAsIso8601AndNatAsNull()
        {
            RunPythonTest(@"
import numpy as np

def test(object_store):
    object_store.save_json('helpers/datetime64.json', {
        'index': pd.DatetimeIndex([pd.Timestamp('2020-01-01'), pd.Timestamp('2020-01-02 03:04:05.000000006')]).values,
        'scalar': np.datetime64('2020-01-01T00:00:00.000000000'),
        'day': np.datetime64('2020-01-01'),
        'timestamp': pd.Timestamp('2020-01-01 10:00'),
        'nat': pd.NaT,
        'nat64': np.datetime64('NaT'),
        'with_nat': pd.to_datetime(['2020-01-01', None]).values,
        'grid': np.array([['2020-01-01', '2020-01-02']], dtype='datetime64[D]')
    })
    data = object_store.read_json('helpers/datetime64.json')
    assert data == {
        'index': ['2020-01-01T00:00:00', '2020-01-02T03:04:05.000000006'],
        'scalar': '2020-01-01T00:00:00',
        'day': '2020-01-01T00:00:00',
        'timestamp': '2020-01-01T10:00:00',
        'nat': None,
        'nat64': None,
        'with_nat': ['2020-01-01T00:00:00', None],
        'grid': [['2020-01-01T00:00:00', '2020-01-02T00:00:00']]
    }, data

    object_store.delete('helpers/datetime64.json')
");
        }

        [Test]
        public void PythonSaveJsonWritesTimedeltasAsIso8601Durations()
        {
            RunPythonTest(@"
import numpy as np
from datetime import timedelta

def test(object_store):
    object_store.save_json('helpers/timedelta.json', {
        'holding': timedelta(days=1, hours=2),
        'latency': np.timedelta64(1500, 'ms'),
        'spans': (pd.to_datetime(['2020-01-02', '2020-01-03']) - pd.Timestamp('2020-01-01')).values,
        'pandas': pd.Timedelta(seconds=1.5),
        'nat': np.timedelta64('NaT')
    })
    data = object_store.read_json('helpers/timedelta.json')
    assert data == {
        'holding': 'P1DT2H0M0S',
        'latency': 'P0DT0H0M1.5S',
        'spans': ['P1DT0H0M0S', 'P2DT0H0M0S'],
        'pandas': 'P0DT0H0M1.5S',
        'nat': None
    }, data
    assert pd.Timedelta(data['holding']) == timedelta(days=1, hours=2)

    object_store.delete('helpers/timedelta.json')
");
        }

        [Test]
        public void PythonSaveJsonRejectsNanDictionaryKeysWithTypeError()
        {
            RunPythonTest(@"
def test(object_store):
    error = None
    try:
        object_store.save_json('helpers/nan_key.json', {float('nan'): 1})
    except TypeError as e:
        error = str(e)
    assert error is not None and 'nan' in error and 'key' in error, error
    assert not object_store.contains_key('helpers/nan_key.json')
");
        }

        [Test]
        public void PythonSaveJsonRejectsPandasObjectsPointingToSaveDataframe()
        {
            RunPythonTest(@"
def test(object_store):
    for value in [pd.DataFrame({'a': [1, 2]}), pd.Series([1, 2])]:
        error = None
        try:
            object_store.save_json('helpers/pandas.json', {'data': value})
        except TypeError as e:
            error = str(e)
        assert error is not None and 'save_dataframe' in error and type(value).__name__ in error, error
        assert not object_store.contains_key('helpers/pandas.json')
");
        }

        [Test]
        public void PythonSaveJsonRejectsUnsupportedTypesNamingTheType()
        {
            RunPythonTest(@"
class TradeRecord:
    pass

def test(object_store):
    for value in [TradeRecord(), object(), (x for x in [1])]:
        error = None
        try:
            object_store.save_json('helpers/unsupported.json', {'data': value})
        except TypeError as e:
            error = str(e)
        assert error is not None and type(value).__name__ in error, error
        assert not object_store.contains_key('helpers/unsupported.json')
");
        }

        [Test]
        public void PythonSaveJsonRoundTripsTopLevelContainersAndRejectsTopLevelScalars()
        {
            RunPythonTest(@"
def test(object_store):
    failures = []
    try:
        for value in [None, [1, 'a'], (1, 2)]:
            expected = list(value) if isinstance(value, tuple) else value
            object_store.save_json('helpers/container.json', value)
            data = object_store.read_json('helpers/container.json')
            if data != expected:
                failures.append(f'{value!r} -> {data!r}')

        # a top-level str/int/float/bool does not bind to the PyObject overload: the error names the mismatch
        for value in ['text', 42, 1.5, True]:
            try:
                object_store.save_json('helpers/scalar.json', value)
                failures.append(f'{value!r} was accepted')
            except TypeError as e:
                if 'No method matches' not in str(e):
                    failures.append(f'{value!r} -> {e}')
        if object_store.contains_key('helpers/scalar.json'):
            failures.append('a rejected scalar was saved')
    finally:
        for key in ['helpers/container.json', 'helpers/scalar.json']:
            if object_store.contains_key(key):
                object_store.delete(key)
    assert not failures, failures
");
        }

        [Test]
        public void PythonReadJsonThrowsOnMissingKey()
        {
            RunPythonTest(@"
def test(object_store):
    assert_raises_key_not_found(lambda: object_store.read_json('helpers/missing.json'))
");
        }

        [Test]
        public void PythonSaveTextAndReadTextAreAliasesOfSaveAndRead()
        {
            RunPythonTest(@"
def test(object_store):
    object_store.save_text('helpers/report.txt', 'hello')
    assert object_store.read('helpers/report.txt') == 'hello'
    assert object_store.read_text('helpers/report.txt') == 'hello'

    object_store.delete('helpers/report.txt')
");
        }

        [Test]
        public void PythonSaveDataframeAndReadDataframeRoundTripAsCsv()
        {
            RunPythonTest(@"
def test(object_store):
    frame = pd.DataFrame({'a': [1, 2], 'b': [3.5, 4.5]})
    object_store.save_dataframe('helpers/frame.csv', frame)
    assert 'a,b' in object_store.read('helpers/frame.csv')
    pd.testing.assert_frame_equal(object_store.read_dataframe('helpers/frame.csv'), frame)

    object_store.delete('helpers/frame.csv')
");
        }

        [Test]
        public void PythonSaveDataframeAndReadDataframeRoundTripAsJson()
        {
            RunPythonTest(@"
def test(object_store):
    frame = pd.DataFrame({'a': [1, 2], 'b': [3.5, 4.5]})
    object_store.save_dataframe('helpers/frame.json', frame)
    assert object_store.read('helpers/frame.json') == frame.to_json()
    pd.testing.assert_frame_equal(object_store.read_dataframe('helpers/frame.json'), frame)

    object_store.delete('helpers/frame.json')
");
        }

        [Test]
        public void PythonReadDataframeRestoresATimeIndex()
        {
            RunPythonTest(@"
def test(object_store):
    frame = pd.DataFrame({'close': [167.42, 168.0]}, index=pd.to_datetime(['2013-10-10', '2013-10-11']))
    object_store.save_dataframe('helpers/history.csv', frame)
    pd.testing.assert_frame_equal(object_store.read_dataframe('helpers/history.csv'), frame)

    object_store.delete('helpers/history.csv')
");
        }

        [Test]
        public void PythonReadDataframeThrowsOnMissingKey()
        {
            RunPythonTest(@"
def test(object_store):
    assert_raises_key_not_found(lambda: object_store.read_dataframe('helpers/missing.csv'))
");
        }

        [Test]
        public void PythonHelpersSanitizeUnsupportedKeys()
        {
            RunPythonTest(@"
def test(object_store):
    object_store.save_json('helpers/trade log: AI & Cloud.json', {'a': 1})
    assert 'helpers/trade log_ AI _ Cloud.json' in list(object_store.keys), list(object_store.keys)
    assert object_store.read_json('helpers/trade log: AI & Cloud.json') == {'a': 1}
    object_store.delete('helpers/trade log: AI & Cloud.json')
    assert not object_store.contains_key('helpers/trade log_ AI _ Cloud.json')
");
        }

        [Test]
        public void PythonKeysHoldAnUnsupportedKeySavedInThisRunAndItsSanitizedName()
        {
            RunPythonTest(@"
def test(object_store):
    key = 'helpers/in check: A & B.csv'
    object_store.save(key, 'abc')

    assert key in object_store.keys
    assert 'helpers/in check_ A _ B.csv' in object_store.keys
    assert 'helpers/missing.csv' not in object_store.keys
    # the keys list the key as saved next to the name the object is stored under
    assert list(object_store.keys).count('helpers/in check_ A _ B.csv') == 1
    assert list(object_store.keys).count(key) == 1
    assert len(object_store.keys) == len(list(object_store.keys))

    object_store.delete(key)
    assert key not in object_store.keys
    assert key not in list(object_store.keys)
");
        }

        /// <summary>
        /// Runs the 'test(object_store)' function of the given Python source against the test store
        /// </summary>
        private void RunPythonTest(string source)
        {
            using (Py.GIL())
            {
                var testModule = PyModule.FromString("TestObjectStoreHelpers", @"
from AlgorithmImports import *
from System.Collections.Generic import KeyNotFoundException
from datetime import datetime
from decimal import Decimal
import json
import pandas as pd

def assert_raises_key_not_found(action):
    try:
        action()
    except KeyNotFoundException:
        return
    raise AssertionError('Expected a KeyNotFoundException')
" + source);

                dynamic test = testModule.GetAttr("test");
                Assert.DoesNotThrow(() => test(_store));
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

            public override void Initialize(int userId, int projectId, string userToken, Controls controls, AlgorithmMode algorithmMode = AlgorithmMode.Backtesting)
            {
                base.Initialize(userId, projectId, userToken, controls, algorithmMode);
            }

            public void Initialize(int userId, int projectId, string userToken, Controls controls, FileHandler fileHandler)
            {
                FileHandler = fileHandler;
                base.Initialize(userId, projectId, userToken, controls, AlgorithmMode.Backtesting);
            }
            protected override bool PersistData()
            {
                PersistDataCalled = true;
                return base.PersistData();
            }
            protected override string StorageRoot() => TestStorageRoot;
        }

        private sealed class ListingCountFileHandler : FileHandler
        {
            public int Listings { get; private set; }

            public override IEnumerable<FileInfo> EnumerateFiles(string path, string pattern, SearchOption searchOption, out string rootfolder)
            {
                Listings++;
                return base.EnumerateFiles(path, pattern, searchOption, out rootfolder);
            }
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
