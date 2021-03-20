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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Storage
{
    /// <summary>
    /// A local disk implementation of <see cref="IObjectStore"/>.
    /// </summary>
    public class LocalObjectStore : IObjectStore
    {
        /// <summary>
        /// No read permissions error message
        /// </summary>
        protected const string NoReadPermissionsError = "The current user does not have permission to read from the organization Object Store." +
                                                        " Please contact your organization administrator to request permission.";

        /// <summary>
        /// No write permissions error message
        /// </summary>
        protected const string NoWritePermissionsError = "The current user does not have permission to write to the organization Object Store." +
                                                         " Please contact your organization administrator to request permission.";

        /// <summary>
        /// Event raised each time there's an error
        /// </summary>
        public event EventHandler<ObjectStoreErrorRaisedEventArgs> ErrorRaised;

        /// <summary>
        /// Flag indicating the state of this object storage has changed since the last <seealso cref="Persist"/> invocation
        /// </summary>
        private volatile bool _dirty;

        private Timer _persistenceTimer;
        private TimeSpan _persistenceInterval;
        private readonly string _storageRoot = Path.GetFullPath(Config.Get("object-store-root", "./storage"));
        private readonly ConcurrentDictionary<string, byte[]> _storage = new ConcurrentDictionary<string, byte[]>();
        private readonly object _persistLock = new object();

        /// <summary>
        /// Provides access to the controls governing behavior of this instance, such as the persistence interval
        /// </summary>
        protected Controls Controls { get; private set; }

        /// <summary>
        /// The root storage folder for the algorithm
        /// </summary>
        protected string AlgorithmStorageRoot { get; private set; }

        /// <summary>
        /// Initializes the object store
        /// </summary>
        /// <param name="algorithmName">The algorithm name</param>
        /// <param name="userId">The user id</param>
        /// <param name="projectId">The project id</param>
        /// <param name="userToken">The user token</param>
        /// <param name="controls">The job controls instance</param>
        public virtual void Initialize(string algorithmName, int userId, int projectId, string userToken, Controls controls)
        {
            // absolute path including algorithm name
            AlgorithmStorageRoot = Path.Combine(_storageRoot, algorithmName);

            // create the root path if it does not exist
            Directory.CreateDirectory(AlgorithmStorageRoot);

            Log.Trace($"LocalObjectStore.Initialize(): Storage Root: {new FileInfo(AlgorithmStorageRoot).FullName}");

            Controls = controls;

            // Load in any already existing objects in the storage directory
            LoadExistingObjects();

            // if <= 0 we disable periodic persistence and make it synchronous
            if (Controls.PersistenceIntervalSeconds > 0)
            {
                _persistenceInterval = TimeSpan.FromSeconds(Controls.PersistenceIntervalSeconds);
                _persistenceTimer = new Timer(_ => Persist(), null, _persistenceInterval, _persistenceInterval);
            }
        }

        /// <summary>
        /// Loads objects from the AlgorithmStorageRoot into the ObjectStore
        /// </summary>
        protected virtual void LoadExistingObjects()
        {
            if (Controls.StoragePermissions.HasFlag(FileAccess.Read))
            {
                foreach (var file in Directory.EnumerateFiles(AlgorithmStorageRoot))
                {
                    // Read the contents of the file and decode the filename to get our key
                    var contents = File.ReadAllBytes(file);
                    var key = Base64ToKey(Path.GetFileName(file));
                    _storage[key] = contents;
                }
            }
        }

        /// <summary>
        /// Determines whether the store contains data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>True if the key was found</returns>
        public bool ContainsKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (!Controls.StoragePermissions.HasFlag(FileAccess.Read))
            {
                throw new InvalidOperationException($"LocalObjectStore.ContainsKey(): {NoReadPermissionsError}");
            }

            return _storage.ContainsKey(key);
        }

        /// <summary>
        /// Returns the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>A byte array containing the data</returns>
        public byte[] ReadBytes(string key)
        {
            // Ensure we have the key, also takes care of null or improper access
            if (!ContainsKey(key))
            {
                throw new KeyNotFoundException($"Object with key '{key}' was not found in the current project. " +
                    "Please use ObjectStore.ContainsKey(key) to check if an object exists before attempting to read."
                );
            }

            byte[] data;
            _storage.TryGetValue(key, out data);

            return data;
        }

        /// <summary>
        /// Saves the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="contents">The object data</param>
        /// <returns>True if the save operation was successful</returns>
        public bool SaveBytes(string key, byte[] contents)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (key.Contains("?"))
            {
                throw new ArgumentException($"LocalObjectStore.SaveBytes(): char '?' is not supported in the key {key}");
            }
            if (!Controls.StoragePermissions.HasFlag(FileAccess.Write))
            {
                throw new InvalidOperationException($"LocalObjectStore.SaveBytes(): {NoWritePermissionsError}");
            }

            if (InternalSaveBytes(key, contents))
            {
                _dirty = true;
                // if <= 0 we disable periodic persistence and make it synchronous
                if (Controls.PersistenceIntervalSeconds <= 0)
                {
                    Persist();
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Won't trigger persist nor will check storage write permissions, useful on initialization since it allows read only permissions to load the object store
        /// </summary>
        protected bool InternalSaveBytes(string key, byte[] contents)
        {
            // Before saving confirm we are abiding by the control rules
            // Start by counting our file and its length
            var fileCount = 1;
            var expectedStorageSizeBytes = contents.Length;
            foreach (var kvp in _storage)
            {
                if (key.Equals(kvp.Key))
                {
                    // Skip we have already counted this above
                    // If this key was already in storage it will be replaced.
                }
                else
                {
                    fileCount++;
                    expectedStorageSizeBytes += kvp.Value.Length;
                }
            }

            // Verify we are within FileCount limit
            if (fileCount > Controls.StorageFileCount)
            {
                var message = $"LocalObjectStore.InternalSaveBytes(): at file capacity: {fileCount}. Unable to save: '{key}'";
                Log.Error(message);
                OnErrorRaised(new StorageLimitExceededException(message));
                return false;
            }

            // Verify we are within Storage limit
            var expectedStorageSizeMb = BytesToMb(expectedStorageSizeBytes);
            if (expectedStorageSizeMb > Controls.StorageLimitMB)
            {
                var message = $"LocalObjectStore.InternalSaveBytes(): at storage capacity: {expectedStorageSizeMb}MB/{Controls.StorageLimitMB}MB. Unable to save: '{key}'";
                Log.Error(message);
                OnErrorRaised(new StorageLimitExceededException(message));
                return false;
            }

            // Add the entry
            _storage.AddOrUpdate(key, k => contents, (k, v) => contents);
            return true;
        }

        /// <summary>
        /// Deletes the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>True if the delete operation was successful</returns>
        public bool Delete(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (!Controls.StoragePermissions.HasFlag(FileAccess.Write))
            {
                throw new InvalidOperationException($"LocalObjectStore.Delete(): {NoWritePermissionsError}");
            }

            byte[] _;
            if (_storage.TryRemove(key, out _))
            {
                _dirty = true;

                // if <= 0 we disable periodic persistence and make it synchronous
                if (Controls.PersistenceIntervalSeconds <= 0)
                {
                    Persist();
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the file path for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>The path for the file</returns>
        public virtual string GetFilePath(string key)
        {
            // Ensure we have an object for that key
            if (!ContainsKey(key))
            {
                throw new KeyNotFoundException($"Object with key '{key}' was not found in the current project. " +
                    "Please use ObjectStore.ContainsKey(key) to check if an object exists before attempting to read."
                );
            }

            // Persist to ensure pur files are up to date
            Persist();

            // Fetch the path to file and return it
            return PathForKey(key);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                if (_persistenceTimer != null)
                {
                    _persistenceTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    Persist();

                    _persistenceTimer.DisposeSafely();
                }

                // if the object store was not used, delete the empty storage directory created in Initialize.
                if (AlgorithmStorageRoot != null && !Directory.GetFileSystemEntries(AlgorithmStorageRoot).Any())
                {
                    Directory.Delete(AlgorithmStorageRoot);
                }
            }
            catch (Exception err)
            {
                Log.Error(err, "Error deleting storage directory.");
            }
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator()
        {
            return _storage.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get's a file path for a given key.
        /// Internal use only because it does not guarantee the existence of the file.
        /// </summary>
        protected string PathForKey(string key)
        {
            // We use an encoded filename because certain keys will cause problems with persisting
            // data to a file; we use Base64 because it allow us to use all chars except '?' in our
            // key, this is because '?' in the right place will output '/' which will break during
            // persist
            return Path.Combine(AlgorithmStorageRoot, $"{KeyToBase64(key)}");
        }

        /// <summary>
        /// Invoked periodically to persist the object store's contents
        /// </summary>
        private void Persist()
        {
            // Acquire the persist lock
            lock (_persistLock)
            {
                // If there are no changes we are fine
                if (!_dirty)
                {
                    return;
                }

                try
                {
                    // Pause timer while persisting
                    _persistenceTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                    if (PersistData(this))
                    {
                        _dirty = false;
                    }
                }
                catch (Exception err)
                {
                    Log.Error("LocalObjectStore.Persist()", err);
                    OnErrorRaised(err);
                }
                finally
                {
                    // restart timer following end of persistence
                    _persistenceTimer?.Change(_persistenceInterval, _persistenceInterval);
                }
            }
        }

        /// <summary>
        /// Overridable persistence function
        /// </summary>
        /// <param name="data">The data to be persisted</param>
        /// <returns>True if persistence was successful, otherwise false</returns>
        protected virtual bool PersistData(IEnumerable<KeyValuePair<string, byte[]>> data)
        {
            try
            {
                // Delete any files that are no longer saved in the store
                foreach (var filepath in Directory.EnumerateFiles(AlgorithmStorageRoot))
                {
                    var filename = Path.GetFileName(filepath);
                    if (!_storage.ContainsKey(Base64ToKey(filename)))
                    {
                        File.Delete(filepath);
                    }
                }

                // Write all our store data to disk
                foreach (var kvp in data)
                {
                    // Get a path for this key and write to it
                    var path = PathForKey(kvp.Key);
                    File.WriteAllBytes(path, kvp.Value);
                }

                return true;
            }
            catch (Exception err) 
            {
                Log.Error("LocalObjectStore.PersistData()", err);
                OnErrorRaised(err);
                return false;
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="ErrorRaised"/> event
        /// </summary>
        protected virtual void OnErrorRaised(Exception error)
        {
            ErrorRaised?.Invoke(this, new ObjectStoreErrorRaisedEventArgs(error));
        }

        /// <summary>
        /// Converts a number of bytes to megabytes as it's more human legible
        /// </summary>
        private static double BytesToMb(long bytes)
        {
            return bytes / 1024.0 / 1024.0;
        }

        /// <summary>
        /// Convert a given key to a Base64 string
        /// </summary>
        /// <param name="key">Key to be encoded</param>
        /// <returns>Base64 hash string</returns>
        private static string KeyToBase64(string key)
        {
            var textAsBytes = Encoding.UTF8.GetBytes(key);
            return Convert.ToBase64String(textAsBytes);
        }

        /// <summary>
        /// Convert a given Base64 string back to a key
        /// </summary>
        /// <param name="hash">Hash to be decoded</param>
        /// <returns>Key string</returns>
        private static string Base64ToKey(string hash)
        {
            var textAsBytes = Convert.FromBase64String(hash);
            return Encoding.UTF8.GetString(textAsBytes);
        }
    }
}
