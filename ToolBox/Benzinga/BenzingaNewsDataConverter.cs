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
using QuantConnect.Data.Custom.Benzinga;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;

namespace QuantConnect.ToolBox.Benzinga
{
    /// <summary>
    /// Converts data sourced from Benzinga's API
    /// </summary>
    public class BenzingaNewsDataConverter
    {
        private readonly DirectoryInfo _sourceDirectory;
        private readonly DirectoryInfo _destinationDirectory;
        private readonly DirectoryInfo _processedFilesDirectory;
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
        public BenzingaNewsDataConverter(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, DirectoryInfo processedFilesDirectory)
        {
            _sourceDirectory = sourceDirectory;
            _destinationDirectory = destinationDirectory;
            _processedFilesDirectory = processedFilesDirectory;
            _destinationDirectory.Create();
            _isWindows = OS.IsWindows;
        }

        public bool Convert(DateTime date)
        {
            var newsArticles = new DirectoryInfo(
                Path.Combine(
                    _sourceDirectory.FullName,
                    $"{date:yyyyMMdd}"
                )
            );

            if (!newsArticles.Exists)
            {
                Log.Error($"BenzingaDataConverter.Convert(): Data not found for date {date:yyyy-MM-dd}");
                return false;
            }

            try
            {
                WriteToFile(Process(newsArticles));
            }
            catch (Exception error)
            {
                Log.Error(error, $"Failed to complete processing of Benzinga news data for {date:yyyy-MM-dd}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Convert the raw data to BenzingaNews instances.
        /// </summary>
        /// <param name="dateDirectory">Directory for a given date, e.g. _sourceDirectory/20181227/</param>
        /// <returns>Enumerable of <see cref="BenzingaNews"/> instances</returns>
        private IEnumerable<BenzingaNews> Process(DirectoryInfo dateDirectory)
        {
            foreach (var publication in dateDirectory.GetFiles("*.json", SearchOption.TopDirectoryOnly))
            {
                foreach (var article in JsonConvert.DeserializeObject<JArray>(File.ReadAllText(publication.FullName)))
                {
                    yield return BenzingaNewsJsonConverter.DeserializeNews(article, enableLogging: true);
                }
            }
        }

        /// <summary>
        /// Writes the processed data into their respective files
        /// </summary>
        /// <param name="news">Enumerable containing news articles</param>
        /// <param name="date">Date to convert for</param>
        private void WriteToFile(IEnumerable<BenzingaNews> news)
        {
            // Send off articles and indexes to be loaded with the necessary data to write to file
            var groupedFilteredContents = FilterArticlesAndIndexes(news);
            // Write the index file contents to disk in batches by date
            foreach (var kvp in groupedFilteredContents)
            {
                Log.Trace($"BenzingaNewsDataConverter.WriteToFile(): Begin writing and compressing indexes for date {kvp.Key:yyyy-MM-dd}");
                var filteredContents = kvp.Value;

                WriteIndexesToFile(filteredContents);
                // Compress the data and write to disk under the `content` folder in the destination directory
                CompressData(filteredContents);
            }
        }

        /// <summary>
        /// Filters and batches the articles so that they can be written to disk in one go
        /// </summary>
        /// <param name="news">Enumerable of BenzingaNews</param>
        /// <param name="date">Date to convert</param>
        private Dictionary<DateTime, BenzingaNewsFiltered> FilterArticlesAndIndexes(IEnumerable<BenzingaNews> news)
        {
            // Create a local cache of indexes and articles in order to speed up the conversion process.
            // We will write files from memory to disk without the need of temporary files.
            var filteredCollection = new Dictionary<DateTime, BenzingaNewsFiltered>();
            var contentsZip = new Dictionary<DateTime, Dictionary<string, string>>();

            foreach (var article in news)
            {
                var createdReference = false;
                var symbolsToRemove = new HashSet<Symbol>();

                BenzingaNewsFiltered filtered;
                if (!filteredCollection.TryGetValue(article.UpdatedAt.Date, out filtered))
                {
                    filtered = new BenzingaNewsFiltered(article.UpdatedAt.Date);
                    filteredCollection[article.UpdatedAt.Date] = filtered;
                }

                // For each ticker, add a reference to the article we're going to store under the "content" folder
                foreach (var symbol in article.Symbols)
                {
                    // Invalid character in path
                    if (symbol.Value.Contains(":"))
                    {
                        symbolsToRemove.Add(symbol);
                        Log.Error($"BenzingaDataConverter.SerializeArticlesAndIndexes(): Ticker {symbol.Value} contains invalid character ':'. Skipping");
                        continue;
                    }

                    // We can't write these tickers in Windows, so we'll skip them
                    if (_isWindows && _forbiddenTickers.Contains(symbol.Value))
                    {
                        symbolsToRemove.Add(symbol);
                        Log.Error($"BenzingaDataConverter.SerializeArticlesAndIndexes(): Ticker {symbol.Value} is invalid in Windows. Skipping");
                        continue;
                    }

                    BenzingaIndexCollection indexCollection;
                    if (!filtered.SymbolIndexes.TryGetValue(symbol, out indexCollection))
                    {
                        indexCollection = new BenzingaIndexCollection(article.UpdatedAt.Date);
                        filtered.SymbolIndexes[symbol] = indexCollection;

                        // The reference file path is where the indexes pointing to an article live
                        var referenceFile = new FileInfo(Path.Combine(_processedFilesDirectory.FullName, symbol.Value.ToLowerInvariant(), $"{article.UpdatedAt.Date:yyyyMMdd}.csv"));

                        if (referenceFile.Exists)
                        {
                            Dictionary<string, string> compressedData;
                            if (!contentsZip.TryGetValue(article.UpdatedAt.Date, out compressedData))
                            {
                                var zipFile = new FileInfo(Path.Combine(_processedFilesDirectory.FullName, "content", $"{article.UpdatedAt.Date:yyyyMMdd}.zip"));
                                using (var reader = zipFile.OpenRead())
                                {
                                    var ms = new MemoryStream();
                                    reader.CopyTo(ms);

                                    Log.Trace($"BenzingaNewsDataConverter.FilterArticlesAndIndexes(): Opening {zipFile.FullName} to get existing index UpdatedAt time");

                                    compressedData = Compression.UnzipData(ms.ToArray());
                                    contentsZip[article.UpdatedAt.Date] = compressedData;
                                }
                            }

                            var articlesUpdatedAt = compressedData
                                .ToDictionary(kvp => kvp.Key, kvp => JsonConvert.DeserializeObject<BenzingaNews>(kvp.Value, new BenzingaNewsJsonConverter()).UpdatedAt);

                            foreach (var reference in File.ReadLines(referenceFile.FullName))
                            {
                                // If we have an index, we should also have an existing article for it.
                                // Purposely allowing it to throw an OutOfBounds exception in the case
                                // the article doesn't exist although we have the index for it
                                indexCollection.Indexes[reference] = articlesUpdatedAt[reference];
                            }
                        }
                    }

                    // Add the article ID to the index collection so that we can batch
                    // write all of the indexes in one go per day
                    indexCollection.Indexes[$"{article.Id}.json"] = article.UpdatedAt;

                    createdReference = true;
                }

                // Skip if we didn't create any references for an article
                if (!createdReference)
                {
                    Log.Error($"BenzingaDataConverter.SerializeArticlesAndIndexes(): Skipping news article {article.Id} because no tickers are associated with it");
                    continue;
                }

                // Remove unwanted symbols from the article before we write to disk
                foreach (var symbolToRemove in symbolsToRemove)
                {
                    article.Symbols.Remove(symbolToRemove);
                }

                // We could potentially have an article with no Symbols and still write it without this check
                if (article.Symbols.Count == 0)
                {
                    Log.Error($"BenzingaDataConverter.FilterArticlesAndIndexes(): Skipping news article {article.Id} because there are no Symbols associated with this article");
                    continue;
                }

                // Batch all the articles so that we can write it all in one shot
                filtered.ArticleContents[$"{article.Id}.json"] = JsonConvert.SerializeObject(article, Formatting.None, new BenzingaNewsJsonConverter());
            }

            return filteredCollection;
        }

        /// <summary>
        /// Writes all the filtered indexes from memory to disk
        /// </summary>
        private void WriteIndexesToFile(BenzingaNewsFiltered filtered)
        {
            foreach (var kvp in filtered.SymbolIndexes)
            {
                var symbol = kvp.Key;
                var indexCollection = kvp.Value;

                var referenceFileDirectory = new DirectoryInfo(Path.Combine(_destinationDirectory.FullName, symbol.Value.ToLowerInvariant()));
                var referenceFile = new FileInfo(Path.Combine(referenceFileDirectory.FullName, $"{indexCollection.Date.ToStringInvariant(DateFormat.EightCharacter)}.csv"));

                // Make sure to create the directory, otherwise we won't be able to move files to it.
                referenceFileDirectory.Create();

                var referenceFileExisted = referenceFile.Exists;
                var referenceFileTemp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
                var existingReferenceFileBackupPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

                Log.Trace($"BenzingaDataConverter.WriteToFile(): Creating reference temp file: {referenceFileTemp}");
                // Order before writing since we need to order the indexes by the UpdatedAt date
                File.WriteAllLines(referenceFileTemp, indexCollection.Indexes.OrderBy(x => x.Value).Select(x => x.Key));

                if (referenceFileExisted)
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
                if (referenceFileExisted)
                {
                    File.Delete(existingReferenceFileBackupPath);
                }
            }
        }

        /// <summary>
        /// Compresses the data to ZIP format and stores them in the <code>Path.Combine(_destinationDirectory.FullName, "content")</code> folder
        /// </summary>
        private void CompressData(BenzingaNewsFiltered filtered)
        {
            // Now compress all of the data we have for a given day
            var compressedDirectory = new DirectoryInfo(Path.Combine(_destinationDirectory.FullName, "content"));
            var compressedFinal = new FileInfo(Path.Combine(compressedDirectory.FullName, $"{filtered.Date:yyyyMMdd}.zip"));
            var compressedTemp = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip"));
            var compressedFinalExists = compressedFinal.Exists;
            var compressedFinalBackup = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip"));

            compressedDirectory.Create();

            if (compressedFinalExists)
            {
                // First, get the manifest of existing articles contained within the ZIP file
                // to determine what articles we need to write to the new ZIP file
                var existingArticles = Compression.GetZipEntryFileNames(compressedFinal.FullName);
                var missingArticles = filtered.ArticleContents.Keys.Except(existingArticles).ToList();

                // nop, all the articles we have are already included inside the zip file
                if (missingArticles.Count == 0)
                {
                    Log.Trace($"BenzingaNewsDataConverter.CompressData(): ZIP file already contains all contents we planned to write. Skipping.");
                    return;
                }

                Log.Trace($"BenzingaNewsDataConverter.CompressData(): Loading data from existing ZIP file {compressedFinal.FullName}");

                // Takes all articles that are missing from the existing articles contained within the ZIP file
                // and creates a new dictionary only containing the files that are missing in the ZIP file.
                var excludedArticles = filtered.ArticleContents.Where(kvp => missingArticles.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                // Create a new instance to preserve the original data in the collection.
                // Since we are not passing by `ref` we are not mutating the original collection
                var instance = new BenzingaNewsFiltered(filtered.Date);

                // Dispose of the handle before attempting to move the file, otherwise File.Move will throw
                using (var zipFile = compressedFinal.OpenRead())
                {
                    var ms = new MemoryStream();
                    zipFile.CopyTo(ms);
                    instance.ArticleContents = Compression.UnzipData(ms.ToArray());
                }

                instance.SymbolIndexes = filtered.SymbolIndexes;

                // Add each missing article into the instance article contents dictionary
                foreach (var kvp in excludedArticles)
                {
                    instance.ArticleContents[kvp.Key] = kvp.Value;
                }

                // Guaranteed to preserve original data, i.e. not mutate the original collection
                filtered = instance;

                Log.Trace($"BenzingaDataConverter.WriteToFile(): Moving existing zip file for {filtered.Date:yyyyMMdd} to temp directory as: {compressedFinalBackup.Name}");
                File.Move(compressedFinal.FullName, compressedFinalBackup.FullName);
                compressedFinal.Refresh();
            }

            try
            {
                Log.Trace($"BenzingaDataConverter.WriteToFile(): Compressing to temp file: {compressedTemp.FullName}");
                Compression.ZipData(compressedTemp.FullName, filtered.ArticleContents);

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
            /// Date that the indexes apply to
            /// </summary>
            public DateTime Date { get; private set; }

            /// <summary>
            /// Indexes keyed by reference, with value as UpdatedAt time
            /// </summary>
            public Dictionary<string, DateTime> Indexes { get; private set; }

            /// <summary>
            /// Creates an instance of <see cref="BenzingaIndexCollection"/>
            /// </summary>
            /// <param name="date">Date that the indexes will apply to</param>
            public BenzingaIndexCollection(DateTime date)
            {
                Date = date;
                Indexes = new Dictionary<string, DateTime>();
            }
        }

        /// <summary>
        /// For filtered data before writing to file
        /// </summary>
        /// <remarks>
        /// This was created to encapsulate the contents we plan on writing to files.
        /// Another reason is to prevent pollution of the method signatures.
        /// </remarks>
        private class BenzingaNewsFiltered
        {
            /// <summary>
            /// Date associated with the filtered contents
            /// </summary>
            public DateTime Date;

            /// <summary>
            /// Contents we want to write to file. Keyed by filename and value is contents to write
            /// </summary>
            public Dictionary<string, string> ArticleContents;

            /// <summary>
            /// Indexes to write to file per symbol
            /// </summary>
            public Dictionary<Symbol, BenzingaIndexCollection> SymbolIndexes;

            /// <summary>
            /// Creates an instance of BenzingaNewsFiltered.
            /// </summary>
            public BenzingaNewsFiltered(DateTime date)
            {
                Date = date;
                ArticleContents = new Dictionary<string, string>();
                SymbolIndexes = new Dictionary<Symbol, BenzingaIndexCollection>();
            }
        }
    }
}
