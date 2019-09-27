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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides a factory method for creating <see cref="ISubscriptionDataSourceReader"/> instances
    /// </summary>
    public static class SubscriptionDataSourceReader
    {
        /// <summary>
        /// Creates a new <see cref="ISubscriptionDataSourceReader"/> capable of handling the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The subscription data source to create a factory for</param>
        /// <param name="dataCacheProvider">Used to cache data</param>
        /// <param name="config">The configuration of the subscription</param>
        /// <param name="date">The date to be processed</param>
        /// <param name="isLiveMode">True for live mode, false otherwise</param>
        /// <returns>A new <see cref="ISubscriptionDataSourceReader"/> that can read the specified <paramref name="source"/></returns>
        public static ISubscriptionDataSourceReader ForSource(SubscriptionDataSource source, IDataCacheProvider dataCacheProvider, SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            ISubscriptionDataSourceReader reader;
            TextSubscriptionDataSourceReader textReader = null;
            switch (source.Format)
            {
                case FileFormat.Csv:
                    reader = textReader = new TextSubscriptionDataSourceReader(dataCacheProvider, config, date, isLiveMode);
                    break;

                case FileFormat.Collection:
                    reader = new CollectionSubscriptionDataSourceReader(dataCacheProvider, config, date, isLiveMode);
                    break;

                case FileFormat.ZipEntryName:
                    reader = new ZipEntryNameSubscriptionDataSourceReader(config, date, isLiveMode);
                    break;

                case FileFormat.Index:
                    return new IndexSubscriptionDataSourceReader(dataCacheProvider, config, date, isLiveMode);

                default:
                    throw new NotImplementedException("SubscriptionFactory.ForSource(" + source + ") has not been implemented yet.");
            }

            // wire up event handlers for logging missing files
            if (source.TransportMedium == SubscriptionTransportMedium.LocalFile)
            {
                var factory = config.GetBaseDataInstance();
                if (!factory.IsSparseData())
                {
                    reader.InvalidSource += (sender, args) => Log.Error($"SubscriptionDataSourceReader.InvalidSource(): File not found: {args.Source.Source}");
                    if (textReader != null)
                    {
                        textReader.CreateStreamReaderError += (sender, args) => Log.Error($"SubscriptionDataSourceReader.CreateStreamReaderError(): File not found: {args.Source.Source}");
                    }
                }
            }

            return reader;
        }

        /// <summary>
        /// Creates cache directory if not existing and deletes old files from the cache
        /// </summary>
        public static void CheckRemoteFileCache()
        {
            // create cache directory if not existing
            if (!Directory.Exists(Globals.Cache)) Directory.CreateDirectory(Globals.Cache);

            // clean old files out of the cache
            foreach (var file in Directory.EnumerateFiles(Globals.Cache))
            {
                if (File.GetCreationTime(file) < DateTime.Now.AddHours(-24)) File.Delete(file);
            }
        }
    }
}
