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
using System.Text.RegularExpressions;
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
        /// Gets the default object store location
        /// </summary>
        public static string DefaultObjectStore => Path.GetFullPath(Config.Get("object-store-root", "./storage"));

        /// <summary>
        /// Flag indicating the state of this object storage has changed since the last <seealso cref="Persist"/> invocation
        /// </summary>
        private volatile bool _dirty;

        private Timer _persistenceTimer;
        private readonly string _storageRoot = DefaultObjectStore;
        private Regex _pathRegex = new (@"^\.?[a-zA-Z0-9\\/_#\-\$= ]+\.?[a-zA-Z0-9]*$", RegexOptions.Compiled);
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
        /// <param name="userId">The user id</param>
        /// <param name="projectId">The project id</param>
        /// <param name="userToken">The user token</param>
        /// <param name="controls">The job controls instance</param>
        public virtual void Initialize(int userId, int projectId, string userToken, Controls controls)
        {
            // absolute path including algorithm name
            AlgorithmStorageRoot = _storageRoot;

            // create the root path if it does not exist
            Directory.CreateDirectory(AlgorithmStorageRoot);

            Log.Trace($"LocalObjectStore.Initialize(): Storage Root: {new FileInfo(AlgorithmStorageRoot).FullName}. StorageFileCount {controls.StorageFileCount}. StorageLimit {BytesToMb(controls.StorageLimit)}MB");

            Controls = controls;

            // Load in any already existing objects in the storage directory
            LoadExistingObjects(loadContent: false);

            // if <= 0 we disable periodic persistence and make it synchronous
            if (Controls.PersistenceIntervalSeconds > 0)
            {
                _persistenceTimer = new Timer(_ => Persist(), null, Controls.PersistenceIntervalSeconds * 1000, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Loads objects from the AlgorithmStorageRoot into the ObjectStore
        /// </summary>
        private void LoadExistingObjects(bool loadContent)
        {
            if (Controls.StoragePermissions.HasFlag(FileAccess.Read))
            {
                var rootFolder = new DirectoryInfo(AlgorithmStorageRoot);
                foreach (var file in rootFolder.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    var path = NormalizePath(file.FullName.RemoveFromStart(rootFolder.FullName));

                    if (loadContent)
                    {
                        if (!_storage.TryGetValue(path, out var content) || content == null)
                        {
                            // load file if content is null or not present, we prioritize the version we have in memory
                            _storage[path] = File.ReadAllBytes(file.FullName);
                        }
                    }
                    else
                    {
                        // we do not read the file contents yet, just the name. We read the contents on demand
                        _storage[path] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the store contains data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>True if the key was found</returns>
        public bool ContainsKey(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!Controls.StoragePermissions.HasFlag(FileAccess.Read))
            {
                throw new InvalidOperationException($"LocalObjectStore.ContainsKey(): {NoReadPermissionsError}");
            }

            return _storage.ContainsKey(NormalizePath(path));
        }

        /// <summary>
        /// Returns the object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>A byte array containing the data</returns>
        public byte[] ReadBytes(string path)
        {
            // Ensure we have the key, also takes care of null or improper access
            if (!ContainsKey(path))
            {
                throw new KeyNotFoundException($"Object with path '{path}' was not found in the current project. " +
                    "Please use ObjectStore.ContainsKey(key) to check if an object exists before attempting to read."
                );
            }
            path = NormalizePath(path);

            _storage.TryGetValue(path, out var data);

            if(data == null)
            {
                var filePath = PathForKey(path);
                if (File.Exists(filePath))
                {
                    // if there is no data in the cache and the file exists on disk let's load it
                    data = _storage[path] = File.ReadAllBytes(filePath);
                }
            }
            return data;
        }

        /// <summary>
        /// Saves the object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="contents">The object data</param>
        /// <returns>True if the save operation was successful</returns>
        public bool SaveBytes(string path, byte[] contents)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            else if (!Controls.StoragePermissions.HasFlag(FileAccess.Write))
            {
                throw new InvalidOperationException($"LocalObjectStore.SaveBytes(): {NoWritePermissionsError}");
            }
            else if (!_pathRegex.IsMatch(path))
            {
                throw new ArgumentException($"LocalObjectStore: path is not supported: '{path}'");
            }
            else if (path.Count(c => c == '/') > 100 || path.Count(c => c == '\\') > 100)
            {
                // just in case
                throw new ArgumentException($"LocalObjectStore: path is not supported: '{path}'");
            }

            // after we check the regex
            path = NormalizePath(path);

            if (InternalSaveBytes(path, contents)
                // only persist if we actually stored some new data, else can skip
                && contents != null)
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
        protected bool InternalSaveBytes(string path, byte[] contents)
        {
            // Before saving confirm we are abiding by the control rules
            // Start by counting our file and its length
            var fileCount = 1;
            var expectedStorageSizeBytes = contents?.Length ?? 0L;
            foreach (var kvp in _storage)
            {
                if (path.Equals(kvp.Key))
                {
                    // Skip we have already counted this above
                    // If this key was already in storage it will be replaced.
                }
                else
                {
                    fileCount++;
                    var fileInfo = new FileInfo(PathForKey(kvp.Key));
                    if (fileInfo.Exists)
                    {
                        expectedStorageSizeBytes += fileInfo.Length;
                    }
                }
            }

            // Verify we are within FileCount limit
            if (fileCount > Controls.StorageFileCount)
            {
                var message = $"LocalObjectStore.InternalSaveBytes(): You have reached the ObjectStore limit for files it can save: {fileCount}. Unable to save the new file: '{path}'";
                Log.Error(message);
                OnErrorRaised(new StorageLimitExceededException(message));
                return false;
            }

            // Verify we are within Storage limit
            if (expectedStorageSizeBytes > Controls.StorageLimit)
            {
                var message = $"LocalObjectStore.InternalSaveBytes(): at storage capacity: {BytesToMb(expectedStorageSizeBytes)}MB/{BytesToMb(Controls.StorageLimit)}MB. Unable to save: '{path}'";
                Log.Error(message);
                OnErrorRaised(new StorageLimitExceededException(message));
                return false;
            }

            // Add the entry
            _storage.AddOrUpdate(path, k => contents, (k, v) => contents);
            return true;
        }

        /// <summary>
        /// Deletes the object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>True if the delete operation was successful</returns>
        public bool Delete(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!Controls.StoragePermissions.HasFlag(FileAccess.Write))
            {
                throw new InvalidOperationException($"LocalObjectStore.Delete(): {NoWritePermissionsError}");
            }

            path = NormalizePath(path);
            if (_storage.TryRemove(path, out var _))
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
        /// Returns the file path for the specified path
        /// </summary>
        /// <remarks>If the key is not already inserted it will just return a path associated with it
        /// and add the key with null value</remarks>
        /// <param name="path">The object path</param>
        /// <returns>The path for the file</returns>
        public virtual string GetFilePath(string path)
        {
            // Ensure we have an object for that key
            if (!ContainsKey(path))
            {
                // Add a key with null value to tell Persist() not to delete the file created in the path associated
                // with this key and not update it with the value associated with the key(null)
                SaveBytes(path, null);
            }
            else
            {
                // Persist to ensure pur files are up to date
                Persist();
            }

            // Fetch the path to file and return it
            return PathForKey(path);
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
            if(_storage.Any(kvp => kvp.Value == null))
            {
                // we need to load the data
                LoadExistingObjects(loadContent: true);
            }
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
        /// Get's a file path for a given path.
        /// Internal use only because it does not guarantee the existence of the file.
        /// </summary>
        protected string PathForKey(string path)
        {
            return Path.Combine(AlgorithmStorageRoot, NormalizePath(path));
        }

        /// <summary>
        /// Invoked periodically to persist the object store's contents
        /// </summary>
        private void Persist()
        {
            // Acquire the persist lock
            lock (_persistLock)
            {
                try
                {
                    // If there are no changes we are fine
                    if (!_dirty)
                    {
                        return;
                    }

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
                    try
                    {
                        if(_persistenceTimer != null)
                        {
                            // restart timer following end of persistence
                            _persistenceTimer.Change(Time.GetSecondUnevenWait(Controls.PersistenceIntervalSeconds * 1000), Timeout.Infinite);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // ignored disposed
                    }
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
                var rootFolder = new DirectoryInfo(AlgorithmStorageRoot);
                foreach (var file in rootFolder.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    var path = NormalizePath(file.FullName.RemoveFromStart(rootFolder.FullName));

                    if (!_storage.ContainsKey(path))
                    {
                        file.Delete();
                    }
                }

                // Write all our store data to disk
                foreach (var kvp in data)
                {
                    // Skip the key associated with null values. They are not linked to a file yet or not loaded
                    if (kvp.Value != null)
                    {
                        // Get a path for this key and write to it
                        var path = PathForKey(kvp.Key);

                        // directory might not exist for custom prefix
                        var parent = Path.GetDirectoryName(path);
                        if (!Directory.Exists(parent))
                        {
                            Directory.CreateDirectory(parent);
                        }
                        File.WriteAllBytes(path, kvp.Value);
                    }
                }

                return true;
            }
            catch (Exception err) 
            {
                Log.Error(err, "LocalObjectStore.PersistData()");
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

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }
            return path.TrimStart('.').TrimStart('/', '\\');
        }
    }
}
