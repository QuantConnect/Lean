using System;
using System.IO;
using Ionic.Zip;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Default implementation of the <see cref="IDataCacheProvider"/>
    /// Does not cache data.  If the data is a zip, the first entry is returned
    /// </summary>
    public class SingleEntryDataCacheProvider : IDataCacheProvider
    {
        private readonly IDataProvider _dataProvider;
        private ZipFile _zipFile;

        /// <summary>
        /// Constructor that takes the <see cref="IDataProvider"/> to be used to retrieve data
        /// </summary>
        public SingleEntryDataCacheProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Fetch data from the cache
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <returns>An <see cref="Stream"/> of the cached data</returns>
        public Stream Fetch(string key)
        {
            var stream = _dataProvider.Fetch(key);

            if (key.EndsWith(".zip") && stream != null)
            {
                // get the first entry from the zip file
                return Compression.UnzipStream(stream, out _zipFile);
            }

            return stream;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data to cache as a byte array</param>
        public void Store(string key, byte[] data)
        {
            //
        }

        public void Dispose()
        {
            if (_zipFile != null)
            {
                _zipFile.Dispose();
            }
        }
    }
}
