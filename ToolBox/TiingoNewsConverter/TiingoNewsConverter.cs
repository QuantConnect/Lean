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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Util;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Logging;
using Newtonsoft.Json.Linq;

namespace QuantConnect.ToolBox.TiingoNewsConverter
{
    public class TiingoNewsConverter
    {
        private const int TaskCountLimit = 200;
        private readonly DirectoryInfo _sourceDirectory;
        private readonly DirectoryInfo _rootDestinationDirectory;
        private readonly DirectoryInfo _contentDirectory;

        /// <summary>
        /// Creates an instance of the converter
        /// </summary>
        /// <param name="sourceDirectory">Directory to read raw data from</param>
        /// <param name="destinationDirectory">Directory to write processed data to</param>
        public TiingoNewsConverter(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
        {
            _sourceDirectory = sourceDirectory;
            _rootDestinationDirectory = new DirectoryInfo(Path.Combine(destinationDirectory.FullName, "alternative\\tiingo"));
            _contentDirectory = Directory.CreateDirectory(Path.Combine(_rootDestinationDirectory.FullName, "content"));
        }

        public void Convert()
        {
            // supposing sourceFiles are the different daily files, eg: bulkfile_2014-01-11_2014-01-12.tar.gz
            var sourceFiles = _sourceDirectory.EnumerateFiles()
                .OrderBy(info => info.Name)
                .ToList(info => info);

            var ioTasks = new Queue<Task>();
            var indexesPerTicker = new Dictionary<TickerIndex, List<Article>>();
            var newsPerDateCollection = new Dictionary<DateTime, List<Article>>();
            var currentDate = DateTime.MinValue;
            var tempPaths = new Queue<string>();

            Log.Trace($"TiingoNewsConverter.Convert(): untar daily files. Count {sourceFiles.Count}...");
            foreach (var bulkFilePerDate in sourceFiles)
            {
                Log.Trace($"TiingoNewsConverter.Convert(): file: {bulkFilePerDate.Name}...");
                var tempPath = GetTempPath();
                tempPaths.Enqueue(tempPath);
                Compression.UnTarGzFiles(bulkFilePerDate.FullName, tempPath);

                // we expect 1 bulk json file for each date
                var newsForDateFile = Directory.EnumerateFiles(tempPath).Single();

                var jsonNews2 = JsonConvert.DeserializeObject(File.ReadAllText(newsForDateFile)) as JArray;
                if (jsonNews2 == null)
                {
                    Log.Error($"TiingoNewsConverter.Convert(): Failed to deserialize file: {bulkFilePerDate.Name}");
                    continue;
                }

                // this is required else memory grows for ever
                if (ioTasks.Count > TaskCountLimit)
                {
                    WaitForTasksToFinish(ioTasks);
                    ioTasks.Enqueue(StartDirectoryCleaner(tempPaths));
                }

                Log.Trace("TiingoNewsConverter.Convert(): processing news...");
                foreach (var jNews in jsonNews2)
                {
                    var singleNewsData = TiingoNewsJsonConverter.DeserializeNews(jNews);
                    var newsPublishDate = singleNewsData.PublishedDate.Date;

                    // we store the data after 1 day difference
                    // raw data is not really ordered and can have jumps +-1 day
                    if (singleNewsData.PublishedDate.Date > (currentDate + Time.OneDay))
                    {
                        var keysToRemove = new List<DateTime>();
                        foreach (var news in newsPerDateCollection
                            .Where(kvp => kvp.Key < currentDate))
                        {
                            Log.Trace($"TiingoNewsConverter.Convert(): StoreDataForDate {news.Key}...");
                            keysToRemove.Add(news.Key);

                            StoreDataForDate(
                                news.Key,
                                indexesPerTicker,
                                news.Value,
                                ioTasks);
                        }

                        foreach (var key in keysToRemove)
                        {
                            newsPerDateCollection.Remove(key);
                        }
                        currentDate = singleNewsData.PublishedDate.Date;
                    }

                    // just in case...
                    if (singleNewsData.PublishedDate.Date < (currentDate - Time.OneDay))
                    {
                        throw new Exception(
                            $"Unexpected date {singleNewsData.PublishedDate.Date} current at {currentDate} file {bulkFilePerDate.Name}");
                    }

                    if (singleNewsData.Symbols.Count == 0)
                    {
                        // skip articles with not symbols
                        continue;
                    }

                    var article = new Article(singleNewsData.ArticleID + ".json",
                        singleNewsData.PublishedDate,
                        // Formatting.None -> 1 line
                        jNews.ToString(Formatting.None));

                    // store article by ID
                    List<Article> newsForDate;
                    if (!newsPerDateCollection.TryGetValue(newsPublishDate, out newsForDate))
                    {
                        newsPerDateCollection[newsPublishDate] = newsForDate = new List<Article>();
                    }
                    newsForDate.Add(article);

                    // update tickers indexes adding the new article id
                    foreach (var newsDataSymbol in singleNewsData.Symbols)
                    {
                        if (newsDataSymbol.Value.All(char.IsDigit))
                        {
                            // skip symbols which only have numbers as value
                            continue;
                        }

                        var indexCacheKey = new TickerIndex(newsDataSymbol.Value, newsPublishDate);

                        List<Article> articles;
                        if (!indexesPerTicker.TryGetValue(indexCacheKey, out articles))
                        {
                            indexesPerTicker[indexCacheKey] = articles = new List<Article>();
                        }
                        articles.Add(article);
                    }
                }
            }

            foreach (var news in newsPerDateCollection)
            {
                Log.Trace("TiingoNewsConverter.Convert(): store remaining data...");
                StoreDataForDate(
                    news.Key,
                    indexesPerTicker,
                    news.Value,
                    ioTasks);
            }

            ioTasks.Enqueue(StartDirectoryCleaner(tempPaths));
            WaitForTasksToFinish(ioTasks);
        }

        private void StoreDataForDate(DateTime date,
            Dictionary<TickerIndex, List<Article>> indexesPerTicker,
            List<Article> newsForDate,
            Queue<Task> ioTasks)
        {
            var toRemove = new List<TickerIndex>();
            var newsDateStr = date.ToStringInvariant(DateFormat.EightCharacter);

            foreach (var kvp in indexesPerTicker
                .Where(index => index.Key.Date == date))
            {
                var indexKey = kvp.Key;
                toRemove.Add(indexKey);

                // Store index: this is slow so send it to a task
                ioTasks.Enqueue(Task.Run(() =>
                {
                    try
                    {
                        // we have to order the articles here when we are about to store them
                        // by publish date
                        var orderedArticles = kvp.Value.OrderBy(article => article.PublishDate).ToList();
                        var data = string.Join(Environment.NewLine, orderedArticles.Select(article => article.Name));

                        // the ticker directory
                        var tickerDir = Directory.CreateDirectory(
                                Path.Combine(_rootDestinationDirectory.FullName, indexKey.Ticker.ToLowerInvariant()));

                        // the index file for that ticker for that date
                        var indexFile = Path.Combine(tickerDir.FullName, $"{newsDateStr}.csv");

                        if (File.Exists(indexFile))
                        {
                            Log.Error($"TiingoNewsConverter.Convert(): Warning index file already exists: {indexFile}. Will overwrite...");
                        }

                        File.WriteAllText(indexFile, data);
                    }
                    catch (Exception exception)
                    {
                        Log.Error($"TiingoNewsConverter.Convert(): Failed to store index: {indexKey}", exception);
                    }
                }));
            }

            if (newsForDate.Count > 0)
            {
                // Store news for date: this is slow so send it to a task too
                ioTasks.Enqueue(Task.Run(() =>
                {
                    var data = newsForDate.ToDictionary(article => article.Name, article => article.RawData);
                    var contentPath = Path.Combine(_contentDirectory.FullName, $"{newsDateStr}.zip");
                    if (!Compression.ZipData(contentPath, data))
                    {
                        Log.Error($"TiingoNewsConverter.Convert(): Failed to store news: {contentPath}");
                    }
                }));
            }

            foreach (var index in toRemove)
            {
                indexesPerTicker.Remove(index);
            }
        }

        private class Article
        {
            public string Name { get; }
            public string RawData { get; }
            public DateTime PublishDate { get; }

            public Article(string name, DateTime date, string rawData)
            {
                Name = name;
                PublishDate = date;
                RawData = rawData;
            }
        }

        private class TickerIndex
        {
            public string Ticker { get; }
            public DateTime Date { get; }

            public TickerIndex(string ticker, DateTime date)
            {
                Ticker = ticker;
                Date = date;
            }

            /// <summary>
            /// 
            /// </summary>
            public override int GetHashCode()
            {
                return Ticker.GetHashCode() + Date.GetHashCode();
            }

            /// <summary>
            /// 
            /// </summary>
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                var objectAsType = obj as TickerIndex;
                if (objectAsType == null) return false;
                return Ticker == objectAsType.Ticker
                       && Date == objectAsType.Date;
            }
        }

        private string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToStringInvariant(null));
        }

        private void WaitForTasksToFinish(Queue<Task> tasks)
        {
            if (tasks.Count > TaskCountLimit)
            {
                Log.Trace("TiingoNewsConverter.WaitForTasksToFinish(): start...");
                while (tasks.Count > 0)
                {
                    var task = tasks.Dequeue();
                    task.Wait();
                }
            }
        }

        private Task StartDirectoryCleaner(Queue<string> paths)
        {
            var copy = new Queue<string>(paths);
            paths.Clear();
            return Task.Run(() =>
            {
                while (copy.Count > 0)
                {
                    var path = copy.Dequeue();
                    try
                    {
                        Directory.Delete(path, recursive: true);
                    }
                    catch
                    {
                        // pass
                    }
                }
            });
        }
    }
}
