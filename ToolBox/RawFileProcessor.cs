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

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Processing harness used to read files in, parse them, and process them.
    /// </summary>
    public class RawFileProcessor : IDisposable
    {
        private DateTime? _start;
        private readonly IStreamProvider _streamProvider;
        private readonly IStreamParser _parser;
        private readonly IDataProcessor[] _processors;

        /// <summary>
        /// Gets or sets a name used for logging
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RawFileProcessor"/> class
        /// </summary>
        public RawFileProcessor(IStreamProvider streamProvider, IStreamParser parser, params IDataProcessor[] processors)
        {
            _streamProvider = streamProvider;
            _parser = parser;
            _processors = processors;
        }

        /// <summary>
        /// Runs the raw file processor on the specified files
        /// </summary>
        /// <param name="name">A name for the processor used for logging</param>
        /// <param name="sources">The raw files to be processed</param>
        /// <param name="streamProvider">Instance capable of reading the sources into a stream</param>
        /// <param name="streamParser">Instance capable of parsing the provided stream</param>
        /// <param name="processors">The data processors to process the parsed data</param>
        /// <returns>True if the operation completed without error, otherwise false</returns>
        public static bool Run(string name, IEnumerable<string> sources, IStreamProvider streamProvider, IStreamParser streamParser, params IDataProcessor[] processors)
        {
            using (var processor = new RawFileProcessor(streamProvider, streamParser, processors) { Name = name })
            {
                foreach (var zip in sources)
                {
                    try
                    {
                        processor.Process(zip);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Perform processing on the specified source file
        /// </summary>
        /// <param name="source">The source file to be processed</param>
        public void Process(string source)
        {
            _start = _start ?? DateTime.UtcNow;

            // process the source file
            foreach (var stream in _streamProvider.Open(source))
            {
                using (stream)
                {
                    foreach (var data in _parser.Parse(source, stream))
                    {
                        foreach (var processor in _processors)
                        {
                            processor.Process(data);
                        }
                    }
                }
            }

            Log.Trace("RawFileProcessor.Process({0}): Finished.", source);
            _streamProvider.Close(source);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _streamProvider.Dispose();
            _parser.Dispose();
            foreach (var processor in _processors)
            {
                processor.Dispose();
            }

            if (_start.HasValue)
            {
                var stop = DateTime.UtcNow;
                Log.Trace("RawFileProcessor.Dispose({0}): Elapsed {1}", Name, stop - _start);
            }
        }
    }
}