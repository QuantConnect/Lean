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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataProcessor"/> that simply forwards all
    /// received data to other attached processors
    /// </summary>
    public class PipeDataProcessor : IDataProcessor
    {
        private readonly HashSet<IDataProcessor> _processors;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeDataProcessor"/> class
        /// </summary>
        /// <param name="processors">The processors to pipe the data to</param>
        public PipeDataProcessor(IEnumerable<IDataProcessor> processors)
        {
            _processors = processors.ToHashSet();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeDataProcessor"/> class
        /// </summary>
        /// <param name="processors">The processors to pipe the data to</param>
        public PipeDataProcessor(params IDataProcessor[] processors)
            : this((IEnumerable<IDataProcessor>)processors)
        {
        }

        /// <summary>
        /// Adds the specified processor to the output pipe
        /// </summary>
        /// <param name="processor">Processor to receive data from this pipe</param>
        public void PipeTo(IDataProcessor processor)
        {
            _processors.Add(processor);
        }

        /// <summary>
        /// Invoked for each piece of data from the source file
        /// </summary>
        /// <param name="data">The data to be processed</param>
        public void Process(IBaseData data)
        {
            foreach (var processor in _processors)
            {
                processor.Process(data);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var processor in _processors)
            {
                processor.Dispose();
            }
        }
    }
}