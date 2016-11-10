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
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataProcessor"/> that writes the incoming
    /// stream of data to a csv file.
    /// </summary>
    public class CsvDataProcessor : IDataProcessor
    {
        private const int TicksPerFlush = 50;
        private static readonly object DirectoryCreateSync = new object();
        
        private readonly string _dataDirectory;
        private readonly Resolution _resolution;
        private readonly TickType _tickType;
        private readonly Dictionary<Symbol, Writer> _writers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvDataProcessor"/> class
        /// </summary>
        /// <param name="dataDirectory">The root data directory, /Data</param>
        /// <param name="resolution">The resolution being sent into the Process method</param>
        /// <param name="tickType">The tick type, trade or quote</param>
        public CsvDataProcessor(string dataDirectory, Resolution resolution, TickType tickType)
        {
            _dataDirectory = dataDirectory;
            _resolution = resolution;
            _tickType = tickType;
            _writers = new Dictionary<Symbol, Writer>();
        }

        /// <summary>
        /// Invoked for each piece of data from the source file
        /// </summary>
        /// <param name="data">The data to be processed</param>
        public void Process(IBaseData data)
        {
            Writer writer;
            if (!_writers.TryGetValue(data.Symbol, out writer))
            {
                writer = CreateTextWriter(data);
                _writers[data.Symbol] = writer;
            }

            // flush every so often
            if (++writer.ProcessCount%TicksPerFlush == 0)
            {
                writer.TextWriter.Flush();
            }

            var line = LeanData.GenerateLine(data, data.Symbol.ID.SecurityType, _resolution);
            writer.TextWriter.WriteLine(line);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in _writers)
            {
                kvp.Value.TextWriter.Dispose();
            }
        }

        /// <summary>
        /// Creates the <see cref="TextWriter"/> that writes data to csv files
        /// </summary>
        private Writer CreateTextWriter(IBaseData data)
        {
            var entry = LeanData.GenerateZipEntryName(data.Symbol, data.Time.Date, _resolution, _tickType);
            var relativePath = LeanData.GenerateRelativeZipFilePath(data.Symbol, data.Time.Date, _resolution, _tickType)
                .Replace(".zip", string.Empty);
            var path = Path.Combine(Path.Combine(_dataDirectory, relativePath), entry);
            var directory = new FileInfo(path).Directory.FullName;
            if (!Directory.Exists(directory))
            {
                // lock before checking again
                lock (DirectoryCreateSync) if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            }

            return new Writer(path, new StreamWriter(path));
        }


        private sealed class Writer
        {
            public readonly string Path;
            public readonly TextWriter TextWriter;
            public int ProcessCount;
            public Writer(string path, TextWriter textWriter)
            {
                Path = path;
                TextWriter = textWriter;
            }
        }
    }
}