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

using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data.Custom.Benzinga;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using QuantConnect.Interfaces;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.ToolBox.Benzinga
{
    /// <summary>
    /// Converts Historical data in RSS format to
    /// </summary>
    public class BenzingaNewsDataConverter
    {
        private readonly DirectoryInfo _sourceDirectory;
        private readonly DirectoryInfo _destinationDirectory;
        private readonly IMapFileProvider _mapFileProvider;
        private readonly MapFileResolver _mapFileResolver;
        private readonly bool _isWindows;

        // FIXME: Associated issue: https://github.com/QuantConnect/Lean/issues/3849
        private readonly HashSet<string> _forbiddenTickers = new HashSet<string>()
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        };

        /// <summary>
        /// Creates a new instance of the Benzinga converter
        /// </summary>
        /// <param name="sourceDirectory">Directory to read data from. This should be the Benzinga data folder (e.g. /Data/alternative/benzinga)</param>
        /// <param name="destinationDirectory">Directory to write data to. This should be the final Benzinga data folder (e.g. /Data/alternative/benzinga)</param>
        public BenzingaNewsDataConverter(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
        {
            _sourceDirectory = sourceDirectory;
            _destinationDirectory = destinationDirectory;
            _destinationDirectory.Create();
            _isWindows = OS.IsWindows;

            _mapFileProvider = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"));
            _mapFileResolver = _mapFileProvider.Get(Market.USA);
        }

        public bool Convert(DateTime date)
        {
            var newsArticles = new DirectoryInfo(Path.Combine(
                _sourceDirectory.FullName,
                $"{date.Year}",
                date.Month.ToStringInvariant().PadLeft(2, '0'),
                date.Day.ToStringInvariant().PadLeft(2, '0')));

            if (!newsArticles.Exists)
            {
                Log.Error($"BenzingaDataConverter.Convert(): Data not found for date {date:yyyy-MM-dd}");
                return false;
            }

            WriteToFile(Process(newsArticles), date);
            return true;
        }

        /// <summary>
        /// Process the data to BenzingaNews instances. This will process both historical and API retrieved news data.
        /// </summary>
        /// <param name="dateDirectory">Directory for a given date, e.g. ./root/2018/12/27/</param>
        /// <returns></returns>
        private IEnumerable<BenzingaNews> Process(DirectoryInfo dateDirectory)
        {
            foreach (var publication in dateDirectory.GetFiles("*.xml", SearchOption.TopDirectoryOnly))
            {
                var document = new XmlDocument();
                document.LoadXml(File.ReadAllText(publication.FullName));

                var news = BenzingaNewsFactory.CreateBenzingaNewsFromRSS(JsonConvert.SerializeXmlNode(document), _mapFileProvider, _mapFileResolver);
                if (news == null)
                {
                    continue;
                }

                yield return news;
            }

            foreach (var publication in dateDirectory.GetFiles("*.json", SearchOption.TopDirectoryOnly))
            {
                var news = BenzingaNewsFactory.CreateBenzingaNewsFromJSON(File.ReadAllText(publication.FullName), _mapFileResolver);
                foreach (var article in news)
                {
                    yield return article;
                }
            }
        }

        /// <summary>
        /// Writes the processed data into their respective files
        /// </summary>
        /// <param name="news">Enumerable containing news articles</param>
        /// <param name="date">Date to convert for</param>
        private void WriteToFile(IEnumerable<BenzingaNews> news, DateTime date)
        {
            // Create a local cache of indexes and articles in order to speed up the conversion process.
            // We will write files from memory to disk without the need of temporary files.
            var articles = new Dictionary<string, string>();
            var indexes = new Dictionary<Symbol, BenzingaIndexCollection>();

            foreach (var article in news)
            {
                var createdReference = false;
                var symbolsToRemove = new HashSet<Symbol>();

                // For each ticker, add a reference to the article we're going to store under the "content" folder
                foreach (var symbol in article.Symbols)
                {
                    // Invalid character in path
                    if (symbol.Value.Contains(":"))
                    {
                        symbolsToRemove.Add(symbol);
                        Log.Error($"BenzingaDataConverter.WriteToFile(): Ticker {symbol.Value} contains invalid character ':'. Skipping");
                        continue;
                    }

                    // We can't write these tickers in Windows, so we'll skip them
                    if (_isWindows && _forbiddenTickers.Contains(symbol.Value))
                    {
                        symbolsToRemove.Add(symbol);
                        Log.Error($"BenzingaDataConverter.WriteToFile(): Ticker {symbol.Value} is invalid in Windows. Skipping");
                        continue;
                    }

                    BenzingaIndexCollection indexCollection;
                    if (!indexes.TryGetValue(symbol, out indexCollection))
                    {
                        indexCollection = new BenzingaIndexCollection(date);
                        indexes[symbol] = indexCollection;
                        // The reference file path is where the indexes pointing to an article live
                        var referenceFile = new FileInfo(Path.Combine(_destinationDirectory.FullName, symbol.Value.ToLowerInvariant(), $"{date:yyyMMdd}.csv"));

                        if (referenceFile.Exists)
                        {
                            foreach (var reference in File.ReadLines(referenceFile.FullName))
                            {
                                // Because `indexCollection.Indexes` is a HashSet, we're guaranteed
                                // to never have duplicate values after running the converter, even
                                // if we somehow previously had duplicate values in the index file.
                                indexCollection.Indexes.Add(reference);
                            }
                        }
                    }

                    // Add the article ID to the index collection so that we can batch
                    // write all of the indexes in one go per day
                    indexCollection.Indexes.Add($"{article.Id}.json");

                    createdReference = true;
                }

                // Skip if we didn't create any references for an article
                if (!createdReference)
                {
                    Log.Error($"BenzingaDataConverter.WriteToFile(): Skipping news article {article.Id} because no tickers are associated with it");
                    continue;
                }

                // Remove unwanted symbols from the article before we write to disk
                foreach (var symbolToRemove in symbolsToRemove)
                {
                    article.Symbols = article.Symbols.Where(s => s != symbolToRemove).ToList();
                }

                // Batch all the articles so that we can parallelize and write all in one shot
                articles[$"{article.Id}.json"] = JsonConvert.SerializeObject(article, Newtonsoft.Json.Formatting.None, new BenzingaNewsJsonConverter());
            }

            // Write all the batched indexes from memory to disk
            foreach (var kvp in indexes)
            {
                var symbol = kvp.Key;
                var indexCollection = kvp.Value;

                var referenceFileDirectory = new DirectoryInfo(Path.Combine(_destinationDirectory.FullName, symbol.Value.ToLowerInvariant()));
                var referenceFile = new FileInfo(Path.Combine(referenceFileDirectory.FullName, $"{indexCollection.Date.ToStringInvariant(DateFormat.EightCharacter)}.csv"));

                // Make sure to create the directory, otherwise we won't be able to move files to it.
                referenceFileDirectory.Create();

                var referenceFileExists = referenceFile.Exists;
                var referenceFileTemp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
                var existingReferenceFileBackupPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

                Log.Trace($"BenzingaDataConverter.WriteToFile(): Creating reference temp file: {referenceFileTemp}");
                // Order before writing since HashSet doesn't guarantee order of elements
                File.WriteAllLines(referenceFileTemp, indexCollection.Indexes.OrderBy(x => x));

                if (referenceFileExists)
                {
                    Log.Trace($"BenzingaDataConverter.WriteToFile(): Moving existing reference file to backup location: {existingReferenceFileBackupPath}");
                    File.Move(referenceFile.FullName, existingReferenceFileBackupPath);
                }

                try
                {
                    Log.Trace($"BenzingaDataConverter.WriteToFile(): Writing reference to {referenceFile.FullName}");
                    File.Move(referenceFileTemp, referenceFile.FullName);
                }
                catch (Exception error)
                {
                    Log.Error(error, $"BenzingaDataConverter.WriteToFile(): Failed to move file to: {referenceFile.FullName} - Skipping...");
                    referenceFile.Refresh();

                    if (referenceFile.Exists)
                    {
                        Log.Error($"BenzingaDataConverter.WriteToFile(): Moving backup file: {existingReferenceFileBackupPath} back to original location: {referenceFile.FullName}");
                        File.Move(existingReferenceFileBackupPath, referenceFile.FullName);
                    }

                    continue;
                }

                // We moved the existing reference file to a temporary location if this is true.
                // We don't need it anymore, so delete it, otherwise we risk running out of disk space.
                if (referenceFileExists)
                {
                    File.Delete(existingReferenceFileBackupPath);
                }
            }

            // Now compress all of the data we have for a given day
            var compressedDirectory = new DirectoryInfo(Path.Combine(_destinationDirectory.FullName, "content"));
            var compressedFinal = new FileInfo(Path.Combine(compressedDirectory.FullName, $"{date:yyyyMMdd}.zip"));
            var compressedTemp = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip"));
            var compressedFinalExists = compressedFinal.Exists;
            var compressedFinalBackup = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip"));

            compressedDirectory.Create();

            if (compressedFinalExists)
            {
                Log.Trace($"BenzingaDataConverter.WriteToFile(): Moving existing zip file for {date:yyyyMMdd} to temp directory as: {compressedFinalBackup.Name}");
                File.Move(compressedFinal.FullName, compressedFinalBackup.FullName);
                compressedFinal.Refresh();
            }

            try
            {
                Log.Trace($"BenzingaDataConverter.WriteToFile(): Compressing to temp file: {compressedTemp.FullName}");
                Compression.ZipData(compressedTemp.FullName, articles);

                Log.Trace($"BenzingaDataConverter.WriteToFile(): Moving compressed file to final location: {compressedFinal.FullName}");
                File.Move(compressedTemp.FullName, compressedFinal.FullName);
            }
            catch (Exception e)
            {
                compressedFinal.Refresh();
                // This differs from compressedFinalExists since
                // we want to know if we actually wrote a file to that location.
                if (compressedFinal.Exists)
                {
                    compressedFinal.Delete();
                }

                Log.Error(e, $"Failed to compress to {compressedFinal.FullName}. Restoring backup from temp: {compressedFinalBackup.Name}");

                // Move the original file back to its original location if we failed to write to it
                if (compressedFinalExists)
                {
                    File.Move(compressedFinalBackup.FullName, compressedFinal.FullName);
                }
            }
        }

        /// <summary>
        /// Provides a collection to store Benzinga indexes keyed by Date
        /// </summary>
        private class BenzingaIndexCollection
        {
            /// <summary>
            /// Date that the index applies to
            /// </summary>
            public DateTime Date { get; private set; }

            /// <summary>
            /// Indexes for the given `Date`
            /// </summary>
            public HashSet<string> Indexes { get; private set; }

            /// <summary>
            /// Creates an instance of <see cref="BenzingaIndexCollection"/>
            /// </summary>
            /// <param name="date">Date that the indexes will apply to</param>
            public BenzingaIndexCollection(DateTime date)
            {
                Date = date;
                Indexes = new HashSet<string>();
            }
        }
    }
}
