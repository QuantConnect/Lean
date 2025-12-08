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
using System.IO;
using System.Linq;
using QuantConnect.Securities;
using QuantConnect.Configuration;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Base tool for pulling data from a remote source and updating existing csv file.
    /// </summary>
    public class ExchangeInfoUpdater
    {
        private readonly IExchangeInfoDownloader _eidl;

        public ExchangeInfoUpdater(IExchangeInfoDownloader eidl)
        {
            _eidl = eidl;
        }

        /// <summary>
        /// Update existing symbol properties database
        /// </summary>
        public void Run()
        {
            var directory = Path.Combine(Globals.DataFolder, "symbol-properties");
            var file = Path.Combine(directory, "symbol-properties-database.csv");
            var baseOutputDirectory = Config.Get("temp-output-directory", "/temp-output-directory");
            var tempOutputDirectory = Directory.CreateDirectory(Path.Combine(baseOutputDirectory, "symbol-properties"));
            var tmp = Path.Combine(tempOutputDirectory.FullName, "symbol-properties-database.csv");
            if (File.Exists(tmp))
            {
                file = tmp;
            }
            else if (!File.Exists(file))
            {
                throw new FileNotFoundException("Unable to locate symbol properties file: " + file);
            }

            // Read file data before to escape from clash if file == tmp
            // Dispose off enumerator to free up resource
            var fileLines = File.ReadLines(file).ToList();

            using (var writer = new StreamWriter(tmp))
            {
                var fetch = false;
                var filter = $"{_eidl.Market},";
                foreach (var line in fileLines)
                {
                    if (!line.StartsWithInvariant(filter, true))
                    {
                        writer.WriteLine(line);
                    }
                    else if (!fetch)
                    {
                        WriteData(writer);
                        fetch = true;
                    }
                }

                if (!fetch)
                {
                    writer.WriteLine(Environment.NewLine);
                    WriteData(writer);
                }
            }
        }

        private void WriteData(StreamWriter writer)
        {
            var existingSymbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
            var entryPerSymbol = _eidl.Get().ToDictionary(newLine => {
                var splitted = newLine.Split(',');
                return new SecurityDatabaseKey(splitted[0], splitted[1], (SecurityType)Enum.Parse(typeof(SecurityType), splitted[2], true));
            });

            foreach (var existingEntry in existingSymbolPropertiesDatabase.GetSymbolPropertiesList(_eidl.Market))
            {
                if (!entryPerSymbol.ContainsKey(existingEntry.Key))
                {
                    // let's keep any existing which is no longer available, to take into account for delistings/removals
                    entryPerSymbol[existingEntry.Key] = $"{existingEntry.Key.Market},{existingEntry.Key.Symbol},{existingEntry.Key.SecurityType.ToLower()},{existingEntry.Value}";
                }
            }

            foreach (var upd in entryPerSymbol.OrderBy(x => x.Key.SecurityType).ThenBy(x => x.Key.Symbol))
            {
                writer.WriteLine(upd.Value);
            }
        }
    }
}
