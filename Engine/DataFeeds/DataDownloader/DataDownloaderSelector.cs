/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
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
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.DataDownloader
{
    /// <summary>Selects the appropriate data downloader based on the data type.</summary>
    public class DataDownloaderSelector : IDisposable
    {
        private readonly IDataDownloader _baseDataDownloader;
        private readonly Lazy<CanonicalDataDownloaderDecorator> _canonicalDataDownloaderDecorator;

        /// <summary>Initializes a new instance of the <see cref="DataDownloaderSelector"/> class.</summary>
        /// <param name="baseDataDownloader">The base data downloader instance.</param>
        public DataDownloaderSelector(IDataDownloader baseDataDownloader)
        {
            _baseDataDownloader = baseDataDownloader;
            _canonicalDataDownloaderDecorator =
                new Lazy<CanonicalDataDownloaderDecorator>(() => new CanonicalDataDownloaderDecorator(_baseDataDownloader));
        }

        /// <summary>Disposes the base downloader and the decorator if it was initialized.</summary>
        public void Dispose()
        {
            (_baseDataDownloader as IDisposable)?.DisposeSafely();
            if (_canonicalDataDownloaderDecorator.IsValueCreated)
            {
                (_canonicalDataDownloaderDecorator.Value as IDisposable)?.DisposeSafely();
            }
        }

        /// <summary>Returns the appropriate downloader for the given data type.</summary>
        /// <param name="dataType">The type of data to download.</param>
        /// <returns>The base downloader for common lean data types, otherwise the canonical decorator.</returns>
        public IDataDownloader GetDataDownloader(Type dataType)
        {
            return LeanData.IsCommonLeanDataType(dataType) ? _baseDataDownloader : _canonicalDataDownloaderDecorator.Value;
        }
    }
}
