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
using Newtonsoft.Json.Linq;
using QuantConnect.Configuration;
using QuantConnect.Data.Custom.Benzinga;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using QuantConnect.Interfaces;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.ToolBox.Benzinga
{
    public class BenzingaDataConverter
    {
        private readonly DirectoryInfo _sourceDirectory;
        private readonly DirectoryInfo _destinationDirectory;
        private readonly IMapFileProvider _mapFileProvider;
        private readonly MapFileResolver _mapFileResolver;
        private readonly bool _isWindows;
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
        public BenzingaDataConverter(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
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
        /// Process the data to BenzingaNews instances
        /// </summary>
        /// <param name="dateDirectory">Directory for a given date, e.g. ./root/2018/12/27/</param>
        /// <returns></returns>
        private IEnumerable<BenzingaNews> Process(DirectoryInfo dateDirectory)
        {
            foreach (var publication in dateDirectory.GetFiles("*.xml", SearchOption.TopDirectoryOnly))
            {
                var document = new XmlDocument();
                document.LoadXml(File.ReadAllText(publication.FullName));

                var item = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeXmlNode(document))["rss"]["channel"]["item"];

                var tickers = new List<JToken>();

                // Only process articles that contain tickers to disk
                if (item["bz:ticker"] == null)
                {
                    continue;
                }

                // Check for only a single ticker before we try to iterate
                if (item["bz:ticker"].Type == JTokenType.Object)
                {
                    tickers.Add(item["bz:ticker"]);
                }
                else
                {
                    foreach (var tickerData in JArray.Parse(item["bz:ticker"].ToString()))
                    {
                        tickers.Add(tickerData);
                    }
                }

                var instance = JsonConvert.DeserializeObject<BenzingaNews>(item.ToString());

                // Strip all HTML tags from the article, then convert HTML entities to their string representation
                // e.g. "<html><p>Apple&#39;s Earnings</p></html>" would become "Apple's Earnings"
                instance.Contents = WebUtility.HtmlDecode(Regex.Replace(instance.Contents, @"<[^>]*>", " "));
                instance.Symbols = new List<BenzingaSymbolData>();
                instance.Metadata = new BenzingaMetadata()
                {
                    IsPro = item["bz:type"]["@pro"].ToString() == "1",
                    FirstRun = item["bz:type"]["@firstrun"].ToString() == "1",
                    Kind = item["bz:type"]["#text"].ToString()
                };

                foreach (var ticker in tickers)
                {
                    var sentiment = ticker["@sentiment"] == null ? (decimal?)null : Parse.Decimal(ticker["@sentiment"].ToString());
                    var exchange = ticker["@exchange"] == null ? string.Empty : ticker["@exchange"].ToString();

                    if (ticker["#text"] == null)
                    {
                        continue;
                    }

                    // Tickers with dots in them like BRK.A and BRK.B appear as BRK-A and BRK-B in Benzinga data.
                    var symbolTicker = ticker["#text"].ToString().Trim().Replace('-', '.');
                    var mappedSymbol = _mapFileResolver.ResolveMapFile(symbolTicker, instance.PublicationDate).GetMappedSymbol(instance.PublicationDate);

                    if (string.IsNullOrWhiteSpace(mappedSymbol))
                    {
                        Log.Error($"BenzingaDataConverter.Process(): Failed to map old ticker {symbolTicker}. New ticker is null");
                        continue;
                    }

                    instance.Symbols.Add(new BenzingaSymbolData()
                    {
                        Exchange = exchange,
                        Sentiment = sentiment,
                        Symbol = new Symbol(
                            SecurityIdentifier.GenerateEquity(symbolTicker, Market.USA, mapSymbol: true, mapFileProvider: _mapFileProvider, mappingResolveDate: instance.PublicationDate),
                            mappedSymbol
                        )
                    });
                }

                yield return instance;
            }
        }

        /// <summary>
        /// Writes the processed data into their respective files
        /// </summary>
        /// <param name="news">Enumerable containing news articles</param>
        /// <param name="date">Date to convert for</param>
        private void WriteToFile(IEnumerable<BenzingaNews> news, DateTime date)
        {
            var finalDirectory = new DirectoryInfo(Path.Combine(_destinationDirectory.FullName, "content", $"{date:yyyMMdd}"));
            finalDirectory.Create();

            foreach (var article in news)
            {
                var createdReference = false;
                var symbolsToRemove = new List<Symbol>();
                // For each ticker, add a reference to the article we're going to store under the "content" folder
                foreach (var ticker in article.Symbols)
                {
                    // Invalid character in path
                    if (ticker.Symbol.Value.Contains(":"))
                    {
                        symbolsToRemove.Add(ticker.Symbol);
                        Log.Trace($"BenzingaDataConverter.WriteToFile(): Ticker {ticker.Symbol.Value} contains invalid character ':'. Skipping");
                        continue;
                    }
                    // ETFs have a special exchange tag associated with them.
                    // Sometimes, we have an empty exchange for some tickers. Let's try and map them first before giving up on them.
                    // Example: SPOT (Spotify) on 2018-10-01 has a missing exchange even though it has already IPO'd
                    if (ticker.Exchange != "NASDAQ" && ticker.Exchange != "NYSE" && ticker.Exchange != "ETF" && !string.IsNullOrEmpty(ticker.Exchange))
                    {
                        symbolsToRemove.Add(ticker.Symbol);
                        Log.Trace($"BenzingaDataConverter.WriteToFile(): Ticker {ticker.Symbol.Value} is not in NYSE, NASDAQ, or is not an ETF. Skipping");
                        continue;
                    }
                    // We can't write these tickers in Windows, so we'll skip them
                    if (_isWindows && _forbiddenTickers.Contains(ticker.Symbol.Value))
                    {
                        symbolsToRemove.Add(ticker.Symbol);
                        Log.Trace($"BenzingaDataConverter.WriteToFile(): Ticker {ticker.Symbol.Value} is invalid in Windows. Skipping");
                        continue;
                    }

                    var tickerPath = new DirectoryInfo(Path.Combine(_destinationDirectory.FullName, ticker.Symbol.Value.ToLowerInvariant()));
                    tickerPath.Create();

                    var referenceFilePath = Path.Combine(tickerPath.FullName, $"{date:yyyMMdd}.csv");
                    var tickerReferences = new HashSet<string>();

                    if (File.Exists(referenceFilePath))
                    {
                        tickerReferences = File.ReadAllLines(referenceFilePath)
                            .ToHashSet();
                    }

                    tickerReferences.Add($"{article.Id}.json");

                    var referenceFileTemp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
                    var finalFileExists = File.Exists(referenceFilePath);
                    var existingReferenceFileBackupPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

                    Log.Trace($"BenzingaDataConverter.WriteToFile(): Creating reference temp file: {referenceFileTemp}");
                    File.WriteAllLines(referenceFileTemp, tickerReferences.OrderBy(x => x));

                    if (finalFileExists)
                    {
                        Log.Trace($"BenzingaDataConverter.WriteToFile(): Moving existing reference file to backup location: {existingReferenceFileBackupPath}");
                        File.Move(referenceFilePath, existingReferenceFileBackupPath);
                    }

                    try
                    {
                        Log.Trace($"BenzingaDataConverter.WriteToFile(): Writing reference to {referenceFilePath}");
                        File.Move(referenceFileTemp, referenceFilePath);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"BenzingaDataConverter.WriteToFile(): Failed to move file to: {referenceFilePath} - Skipping...");

                        if (finalFileExists)
                        {
                            Log.Error($"BenzingaDataConverter.WriteToFile(): Moving backup file: {existingReferenceFileBackupPath} back to original location: {referenceFilePath}");
                            File.Move(existingReferenceFileBackupPath, referenceFilePath);
                        }

                        continue;
                    }

                    // Clean up after ourselves so that we don't accidentally run out of storage space
                    File.Delete(existingReferenceFileBackupPath);

                    createdReference = true;
                }

                foreach (var symbolToRemove in symbolsToRemove)
                {
                    article.Symbols = article.Symbols.Where(x => x.Symbol != symbolToRemove).ToList();
                }

                if (!createdReference)
                {
                    Log.Trace($"BenzingaDataConverter.WriteToFile(): Skipping news article {article.Id} because no tickers are associated with it");
                    continue;
                }

                var finalArticleFile = Path.Combine(finalDirectory.FullName, $"{article.Id}.json");
                var tempArticleFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
                var backupArticleFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
                var articleExists = File.Exists(finalArticleFile);

                Log.Trace($"BenzingaDataConverter.WriteToFile(): Writing article contents to temp: {tempArticleFile}");
                File.WriteAllText(tempArticleFile, JsonConvert.SerializeObject(article, Newtonsoft.Json.Formatting.None));

                if (articleExists)
                {
                    Log.Trace($"BenzingaDataConverter.WriteToFile(): Article already exists. Moving from: {finalArticleFile} - to backup: {backupArticleFile}");
                    File.Move(finalArticleFile, backupArticleFile);
                }

                try
                {
                    Log.Trace($"BenzingaDataConverter.WriteToFile(): Moving from temp to: {finalArticleFile}");
                    File.Move(tempArticleFile, finalArticleFile);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"BenzingaDataConverter.WriteToFile(): Failed to move file to: {finalArticleFile}");

                    if (articleExists)
                    {
                        Log.Error($"BenzingaDataConverter.WriteToFile(): Restoring backup article file: {backupArticleFile} to original location: {finalArticleFile}");
                        File.Move(backupArticleFile, finalArticleFile);
                    }
                }
            }

            // Now compress all of the data we have for a given day
            var compressedFinal = new FileInfo(Path.Combine(_destinationDirectory.FullName, "content", $"{date:yyyyMMdd}.zip"));
            var compressedFinalBackup = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip"));

            if (compressedFinal.Exists)
            {
                Log.Trace($"BenzingaDataConverter.WriteToFile(): Moving existing zip file for {date:yyyyMMdd} to temp directory as: {compressedFinalBackup.Name}");
                compressedFinal.MoveTo(compressedFinalBackup.FullName);
                compressedFinal.Refresh();
            }

            try
            {
                Log.Trace($"BenzingaDataConverter.WriteToFile(): Compressing {finalDirectory.Name} to {compressedFinal.FullName}");
                Compression.ZipDirectory(finalDirectory.FullName, compressedFinal.FullName, includeRootInZip: false);
            }
            catch (Exception e)
            {
                compressedFinal.Refresh();
                compressedFinal.Delete();

                Log.Error(e, $"Failed to compress to {compressedFinal.FullName}. Restoring backup from temp: {compressedFinalBackup.Name}");
                File.Move(compressedFinalBackup.FullName, compressedFinal.FullName);

                return;
            }
            finally
            {
                Log.Trace($"BenzingaDataConverter.WriteTOFile(): Deleting temp data in directory: {finalDirectory.Name}");
                // Clean up after ourselves
                Directory.Delete(finalDirectory.FullName, true);
            }
        }
    }
}
