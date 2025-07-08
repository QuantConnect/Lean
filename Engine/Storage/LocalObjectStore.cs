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
        /// No delete permissions error message
        /// </summary>
        protected const string NoDeletePermissionsError = "The current user does not have permission to delete objects from the organization Object Store." +
                                                         " Please contact your organization administrator to request permission.";

        /// <summary>
        /// Event raised each time there's an error
        /// </summary>
        public event EventHandler<ObjectStoreErrorRaisedEventArgs> ErrorRaised;

        /// <summary>
        /// Gets the default object store location
        /// </summary>
        public static string DefaultObjectStore { get; set; } = Path.GetFullPath(Config.Get("object-store-root", "./storage"));

        /// <summary>
        /// Flag indicating the state of this object storage has changed since the last <seealso cref="Persist"/> invocation
        /// </summary>
        private volatile bool _dirty;

        private Timer _persistenceTimer;
        private Regex _pathRegex = new(@"^\.?[a-zA-Z0-9\\/_#\-\$= ]+\.?[a-zA-Z0-9]*$", RegexOptions.Compiled);
        private readonly ConcurrentDictionary<string, ObjectStoreEntry> _storage = new();
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
        /// The file handler instance to use
        /// </summary>
        protected FileHandler FileHandler { get; set; } = new();

        /// <summary>
        /// Initializes the object store
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="projectId">The project id</param>
        /// <param name="userToken">The user token</param>
        /// <param name="controls">The job controls instance</param>
        public virtual void Initialize(int userId, int projectId, string userToken, Controls controls)
        {
            AlgorithmStorageRoot = StorageRoot();

            // create the root path if it does not exist
            var directoryInfo = FileHandler.CreateDirectory(AlgorithmStorageRoot);
            // full name will return a normalized path which is later easier to compare
            AlgorithmStorageRoot = directoryInfo.FullName;

            Controls = controls;

            // if <= 0 we disable periodic persistence and make it synchronous
            if (Controls.PersistenceIntervalSeconds > 0)
            {
                _persistenceTimer = new Timer(_ => Persist(), null, Controls.PersistenceIntervalSeconds * 1000, Timeout.Infinite);
            }

            Log.Trace($"LocalObjectStore.Initialize(): Storage Root: {directoryInfo.FullName}. StorageFileCount {controls.StorageFileCount}. StorageLimit {BytesToMb(controls.StorageLimit)}MB. StoragePermissions {Controls.StorageAccess}");
        }

        /// <summary>
        /// Storage root path
        /// </summary>
        protected virtual string StorageRoot()
        {
            return DefaultObjectStore;
        }

        /// <summary>
        /// Loads objects from the AlgorithmStorageRoot into the ObjectStore
        /// </summary>
        private IEnumerable<ObjectStoreEntry> GetObjectStoreEntries(bool loadContent, bool takePersistLock = true)
        {
            if (Controls.StorageAccess.Read)
            {
                // Acquire the persist lock to avoid yielding twice the same value, just in case
                lock (takePersistLock ? _persistLock : new object())
                {
                    foreach (var kvp in _storage)
                    {
                        if (!loadContent || kvp.Value.Data != null)
                        {
                            // let's first serve what we already have in memory because it might include files which are not on disk yet
                            yield return kvp.Value;
                        }
                    }

                    foreach (var file in FileHandler.EnumerateFiles(AlgorithmStorageRoot, "*", SearchOption.AllDirectories, out var rootFolder))
                    {
                        var path = NormalizePath(file.FullName.RemoveFromStart(rootFolder));

                        ObjectStoreEntry objectStoreEntry;
                        if (loadContent)
                        {
                            if (!_storage.TryGetValue(path, out objectStoreEntry) || objectStoreEntry.Data == null)
                            {
                                if (TryCreateObjectStoreEntry(file.FullName, path, out objectStoreEntry))
                                {
                                    // load file if content is null or not present, we prioritize the version we have in memory
                                    yield return _storage[path] = objectStoreEntry;
                                }
                            }
                        }
                        else
                        {
                            if (!_storage.ContainsKey(path))
                            {
                                // we do not read the file contents yet, just the name. We read the contents on demand
                                yield return _storage[path] = new ObjectStoreEntry(path, null);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the file paths present in the object store. This is specially useful not to load the object store into memory
        /// </summary>
        public ICollection<string> Keys
        {
            get
            {
                return GetObjectStoreEntries(loadContent: false).Select(objectStoreEntry => objectStoreEntry.Path).ToList();
            }
        }

        /// <summary>
        /// Will clear the object store state cache. This is useful when the object store is used concurrently by nodes which want to share information
        /// </summary>
        public void Clear()
        {
            // write to disk anything pending first
            Persist();

            _storage.Clear();
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
            if (!Controls.StorageAccess.Read)
            {
                throw new InvalidOperationException($"LocalObjectStore.ContainsKey(): {NoReadPermissionsError}");
            }

            path = NormalizePath(path);
            if (_storage.ContainsKey(path))
            {
                return true;
            }

            // if we don't have the file but it exists, be friendly and register it
            var filePath = PathForKey(path);
            if (FileHandler.Exists(filePath))
            {
                _storage[path] = new ObjectStoreEntry(path, null);
                return true;
            }
            return false;
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

            if (!_storage.TryGetValue(path, out var objectStoreEntry) || objectStoreEntry.Data == null)
            {
                var filePath = PathForKey(path);
                if (TryCreateObjectStoreEntry(filePath, path, out objectStoreEntry))
                {
                    // if there is no data in the cache and the file exists on disk let's load it
                    _storage[path] = objectStoreEntry;
                }
            }
            return objectStoreEntry?.Data;
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
            else if (!Controls.StorageAccess.Write)
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
            if (!IsWithinStorageLimit(path, contents, takePersistLock: true))
            {
                return false;
            }

            // Add the dirty entry
            var entry = _storage[path] = new ObjectStoreEntry(path, contents);
            entry.SetDirty();
            return true;
        }

        /// <summary>
        /// Validates storage limits are respected on a new save operation
        /// </summary>
        protected virtual bool IsWithinStorageLimit(string path, byte[] contents, bool takePersistLock)
        {
            // Before saving confirm we are abiding by the control rules
            // Start by counting our file and its length
            var fileCount = 1;
            var expectedStorageSizeBytes = contents?.Length ?? 0L;
            foreach (var kvp in GetObjectStoreEntries(loadContent: false, takePersistLock: takePersistLock))
            {
                if (path.Equals(kvp.Path))
                {
                    // Skip we have already counted this above
                    // If this key was already in storage it will be replaced.
                }
                else
                {
                    fileCount++;
                    if (kvp.Data != null)
                    {
                        // if the data is in memory use it
                        expectedStorageSizeBytes += kvp.Data.Length;
                    }
                    else
                    {
                        expectedStorageSizeBytes += FileHandler.TryGetFileLength(PathForKey(kvp.Path));
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
            if (!Controls.StorageAccess.Delete)
            {
                throw new InvalidOperationException($"LocalObjectStore.Delete(): {NoDeletePermissionsError}");
            }

            path = NormalizePath(path);

            var wasInCache = _storage.TryRemove(path, out var _);

            var filePath = PathForKey(path);
            if (FileHandler.Exists(filePath))
            {
                try
                {
                    FileHandler.Delete(filePath);
                    return true;
                }
                catch
                {
                    // This try sentence is to prevent a race condition with the Delete within the PersisData() method
                }
            }

            return wasInCache;
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
            var normalizedPathKey = PathForKey(path);

            var parent = Directory.GetParent(normalizedPathKey);
            if (parent != null && parent.FullName != AlgorithmStorageRoot)
            {
                // let's create the parent folder if it's not the root storage and it does not exist
                if (!FileHandler.DirectoryExists(parent.FullName))
                {
                    FileHandler.CreateDirectory(parent.FullName);
                }
            }
            return normalizedPathKey;
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
            return GetObjectStoreEntries(loadContent: true).Select(objectStore => new KeyValuePair<string, byte[]>(objectStore.Path, objectStore.Data)).GetEnumerator();
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

                    if (PersistData())
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
                        if (_persistenceTimer != null)
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
        /// <returns>True if persistence was successful, otherwise false</returns>
        protected virtual bool PersistData()
        {
            try
            {
                // Write our store data to disk
                // Skip the key associated with null values. They are not linked to a file yet or not loaded
                // Also skip fails which are not flagged as dirty
                foreach (var kvp in _storage)
                {
                    if (kvp.Value.Data != null && kvp.Value.IsDirty)
                    {
                        var filePath = PathForKey(kvp.Key);
                        // directory might not exist for custom prefix
                        var parentDirectory = Path.GetDirectoryName(filePath);
                        if (!FileHandler.DirectoryExists(parentDirectory))
                        {
                            FileHandler.CreateDirectory(parentDirectory);
                        }
                        FileHandler.WriteAllBytes(filePath, kvp.Value.Data);

                        // clear the dirty flag
                        kvp.Value.SetClean();

                        // This kvp could have been deleted by the Delete() method
                        if (!_storage.Contains(kvp))
                        {
                            try
                            {
                                FileHandler.Delete(filePath);
                            }
                            catch
                            {
                                // This try sentence is to prevent a race condition with the Delete() method
                            }
                        }
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
            return path.TrimStart('.').TrimStart('/', '\\').Replace('\\', '/');
        }

        private bool TryCreateObjectStoreEntry(string filePath, string path, out ObjectStoreEntry objectStoreEntry)
        {
            var count = 0;
            do
            {
                count++;
                try
                {
                    if (FileHandler.Exists(filePath))
                    {
                        objectStoreEntry = new ObjectStoreEntry(path, FileHandler.ReadAllBytes(filePath));
                        return true;
                    }
                    objectStoreEntry = null;
                    return false;
                }
                catch (Exception)
                {
                    if (count > 3)
                    {
                        throw;
                    }
                    else
                    {
                        // let's be resilient and retry, avoid race conditions, someone updating it or just random io failure
                        Thread.Sleep(250);
                    }
                }
            } while (true);
        }

        /// <summary>
        /// Helper class to hold the state of an object store file
        /// </summary>
        private class ObjectStoreEntry
        {
            private long _isDirty;
            public byte[] Data { get; }
            public string Path { get; }
            public bool IsDirty => Interlocked.Read(ref _isDirty) != 0;
            public ObjectStoreEntry(string path, byte[] data)
            {
                Path = path;
                Data = data;
            }
            public void SetDirty()
            {
                // flag as dirty
                Interlocked.CompareExchange(ref _isDirty, 1, 0);
            }
            public void SetClean()
            {
                Interlocked.CompareExchange(ref _isDirty, 0, 1);
            }
        }
    }
}
