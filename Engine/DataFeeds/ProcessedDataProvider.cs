/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
*/

using System.IO;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using System;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// A data provider that will check the processed data folder first
    /// </summary>
    public class ProcessedDataProvider : IDataProvider, IDisposable
    {
        private readonly DefaultDataProvider _defaultDataProvider;
        private readonly string _processedDataDirectory;

        /// <summary>
        /// Ignored
        /// </summary>
        public event EventHandler<DataProviderNewDataRequestEventArgs> NewDataRequest;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public ProcessedDataProvider()
        {
            _defaultDataProvider = new();
            _processedDataDirectory = Config.Get("processed-data-directory") ?? string.Empty;
            Log.Trace($"ProcessedDataProvider(): processed data directory to use {_processedDataDirectory}, exists: {Directory.Exists(_processedDataDirectory)}");
        }

        /// <summary>
        /// Retrieves data from disc to be used in an algorithm
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public Stream Fetch(string key)
        {
            Stream result = null;

            // we will try the processed data folder first
            if (_processedDataDirectory.Length != 0 && key.StartsWith(Globals.DataFolder, StringComparison.OrdinalIgnoreCase))
            {
                result = _defaultDataProvider.Fetch(Path.Combine(_processedDataDirectory, key.Remove(0, Globals.DataFolder.Length).TrimStart('/', '\\')));
                if (result != null)
                {
                    Log.Trace($"ProcessedDataProvider.Fetch({key}): fetched from processed data directory");
                }
            }

            // fall back to existing data folder path
            return result ?? _defaultDataProvider.Fetch(key);
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the internal data provider
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _defaultDataProvider.Dispose();
            }
        }
    }
}
