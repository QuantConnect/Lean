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
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataProcessor"/> that writes the incoming
    /// stream of data to a zip file. A intermediate csv is created during processing and the zip
    /// is performed upon a call to <see cref="Dispose"/>
    /// </summary>
    public class ZipCsvDataProcessor : IDataProcessor
    {
        private readonly string _dataDirectory;
        private readonly Resolution _resolution;
        private readonly TickType _tickType;
        private readonly Dictionary<string, Tuple<string, TextWriter>> _writers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipCsvDataProcessor"/> class
        /// </summary>
        /// <param name="dataDirectory">The root data directory, /Data</param>
        /// <param name="resolution">The resolution being sent into the Process method</param>
        /// <param name="tickType">The tick type, trade or quote</param>
        public ZipCsvDataProcessor(string dataDirectory, Resolution resolution, TickType tickType)
        {
            _dataDirectory = dataDirectory;
            _resolution = resolution;
            _tickType = tickType;
            _writers = new Dictionary<string, Tuple<string, TextWriter>>();
        }

        /// <summary>
        /// Invoked for each piece of data from the source file
        /// </summary>
        /// <param name="data">The data to be processed</param>
        public void Process(BaseData data)
        {
            Tuple<string, TextWriter> tuple;
            var entry = LeanData.GenerateZipEntryName(data.Symbol, data.Time.Date, _resolution, _tickType);
            if (!_writers.TryGetValue(entry, out tuple))
            {
                tuple = CreateTextWriter(data, entry);
                _writers[entry] = tuple;
            }

            var line = LeanData.GenerateLine(data, data.Symbol.ID.SecurityType, _resolution);
            tuple.Item2.WriteLine(line);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in _writers)
            {
                kvp.Value.Item2.Dispose();
            }

            // group by the directory where the csv's are stored
            foreach (var grouping in _writers.GroupBy(x => new FileInfo(x.Value.Item1).Directory.FullName))
            {
                // zip the contents of the directory and then delete the directory
                Compression.ZipDirectory(grouping.Key, grouping.Key + ".zip", false);
                Directory.Delete(grouping.Key, true);
            }

            _writers.Clear();
        }

        /// <summary>
        /// Creates the <see cref="TextWriter"/> that writes data to csv files
        /// </summary>
        private Tuple<string, TextWriter> CreateTextWriter(BaseData data, string entry)
        {
            var relativePath = LeanData.GenerateRelativeZipFilePath(data.Symbol, data.Time.Date, _resolution, _tickType)
                .Replace(".zip", string.Empty);
            var path = Path.Combine(Path.Combine(_dataDirectory, relativePath), entry);
            var directory = new FileInfo(path).Directory.FullName;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            return Tuple.Create(path, (TextWriter)new StreamWriter(path));
        }
    }
}