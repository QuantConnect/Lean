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
 *
*/

using System;
using System.IO;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Base downloader implementation with some helper methods
    /// </summary>
    public abstract class BaseDownloaderDataProvider : DefaultDataProvider
    {
        /// <summary>
        /// Synchronizer in charge of guaranteeing a single download per path request
        /// </summary>
        private readonly KeyStringSynchronizer _singleDownloadSynchronizer = new();

        /// <summary>
        /// Helper method which guarantees each requested key is downloaded only once concurrently if required based on <see cref="NeedToDownload"/>
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <param name="download">The download operation we want to perform once concurrently per key</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        protected Stream DownloadOnce(string key, Action<string> download)
        {
            // If we don't already have this file or its out of date, download it
            if (NeedToDownload(key))
            {
                // only the first thread will download the rest will wait for him to finish
                _singleDownloadSynchronizer.Execute(
                    key,
                    singleExecution: true,
                    () => download(key)
                );
                // single download finished, let's get the stream!
                return GetStream(key);
            }

            // even if we are not downloading the file because it exists we need to synchronize because the download might still be updating the file on disk
            return _singleDownloadSynchronizer.Execute(key, () => GetStream(key));
        }

        /// <summary>
        /// Get's the stream for a given file path
        /// </summary>
        protected virtual Stream GetStream(string key)
        {
            return base.Fetch(key);
        }

        /// <summary>
        /// Main filter to determine if this file needs to be downloaded
        /// </summary>
        /// <param name="filePath">File we are looking at</param>
        /// <returns>True if should download</returns>
        protected abstract bool NeedToDownload(string filePath);
    }
}
