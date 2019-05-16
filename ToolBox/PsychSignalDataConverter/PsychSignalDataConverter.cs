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
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.PsychSignalDataConverter
{
    public class PsychSignalDataConverter
    {
        /// <summary>
        /// Iterate over the data, processing each data point before finally zipping the directories
        /// </summary>
        public void Convert(string sourceFilePath, SecurityType securityType, string market)
        {
            if (securityType != SecurityType.Equity)
            {
                throw new ArgumentException("Only equity data is supported for the psychsignal converter. Exiting...");
            }

            var tickerFileHandlers = new Dictionary<string, TickerData>();
            var tickerFolders = new List<string>();

            var dataFolder = Path.Combine(Globals.DataFolder, securityType.SecurityTypeToLower(), market);
            var sentimentFolder = Path.Combine(dataFolder, "alternative", "psychsignal");
            var knownTickerFolder = Path.Combine(dataFolder, "daily");

            var knownTickers = (from zipFile in Directory.GetFiles(knownTickerFolder, "*.zip")
                               select Path.GetFileNameWithoutExtension(zipFile)).ToList();

            var previousTicker = string.Empty;

            foreach (var currentLine in File.ReadLines(sourceFilePath))
            {
                if (currentLine.StartsWith("SOURCE"))
                {
                    continue;
                }

                var csv = currentLine.Split(',');

                var ticker = csv[1].ToLower();
                if (!knownTickers.Contains(ticker))
                {
                    continue;
                }

                DateTime timestamp;
                if (!DateTime.TryParse(csv[2], out timestamp))
                {
                    Log.Error($"Failed to parse timestamp: {csv[2]}");
                    continue;
                }

                var dataFilePath = Path.Combine(sentimentFolder, ticker, $"{timestamp:yyyyMMdd}.csv");

                // Avoids having to re-open the file every time we want to write to it
                TickerData handler;
                if (!tickerFileHandlers.TryGetValue(ticker, out handler))
                {
                    if (!tickerFolders.Contains(ticker))
                    {
                        Directory.CreateDirectory(Path.Combine(sentimentFolder, ticker));
                        tickerFolders.Add(ticker);
                    }
                    tickerFileHandlers[ticker] = new TickerData(dataFilePath);
                    handler = tickerFileHandlers[ticker];
                }
                if (handler.DataPath != dataFilePath)
                {
                    tickerFileHandlers[ticker].UpdateWriter(dataFilePath);
                }
                // We need to flush the previous data if our ticker has changed in order to completely write all data to disk.
                // Previously, the last day's data would not be written because the file was never closed. 
                if (previousTicker != ticker && !string.IsNullOrEmpty(previousTicker))
                {
                    tickerFileHandlers[previousTicker].Writer.Close();
                    tickerFileHandlers.Remove(previousTicker);
                }

                // SOURCE[0],SYMBOL[1],TIMESTAMP_UTC[2],BULLISH_INTENSITY[3],BEARISH_INTENSITY[4],BULL_MINUS_BEAR[5],BULL_SCORED_MESSAGES[6],BEAR_SCORED_MESSAGES[7],BULL_BEAR_MSG_RATIO[8],TOTAL_SCANNED_MESSAGES[9]
                handler.Writer.WriteLine(ToCsv(timestamp, csv.Skip(3)));
                previousTicker = ticker;
            }
            
            // Free final writer so that we don't get an IOException due to the final file being open
            tickerFileHandlers[previousTicker].Writer.Close();

            foreach (var tickerDataFolder in Directory.GetDirectories(sentimentFolder))
            {
                Compression.ZipDirectory(tickerDataFolder, $"{tickerDataFolder}.zip", false);
                Directory.Delete(tickerDataFolder, true);
            }
        }

        /// <summary>
        /// Converts line of psychsignal data to LEAN's csv format
        /// </summary>
        /// <param name="timestamp">Timestamp as a string to use for filename</param>
        /// <param name="csvData">Data as it comes from data vendor</param>
        /// <returns></returns>
        public string ToCsv(DateTime timestamp, IEnumerable<string> csvData)
        {
            return $"{timestamp.Subtract(new DateTime(timestamp.Year, timestamp.Month, timestamp.Day)).TotalMilliseconds},{string.Join(",", csvData)}";
        }

        private class TickerData
        {
            /// <summary>
            /// File writer as stream
            /// </summary>
            public StreamWriter Writer { get; private set; }

            /// <summary>
            /// Path to the data (csv file)
            /// </summary>
            public string DataPath { get; private set; }

            /// <summary>
            /// Creates writer instances and saves file path.
            /// Used to keep file open until the path changes for the symbol
            /// </summary>
            /// <param name="dataFilePath">Path to the file we want to write</param>
            public TickerData(string dataFilePath)
            {
                DataPath = dataFilePath;
                Writer = new StreamWriter(DataPath);
            }

            /// <summary>
            /// Closes and updates the writer to a new file path
            /// </summary>
            /// <param name="dataFilePath">Path to the file we want to write</param>
            public void UpdateWriter(string dataFilePath)
            {
                DataPath = dataFilePath;
                Writer.Close();
                Writer = new StreamWriter(DataPath);
            }
        }
    }
}
