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
using QuantConnect.Data;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataProcessor"/> that filters the incoming
    /// stream of data before passing it along to the wrapped processor
    /// </summary>
    public class FilteredDataProcessor : IDataProcessor
    {
        private readonly Func<IBaseData, bool> _predicate;
        private readonly IDataProcessor _processor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredDataProcessor"/> class
        /// </summary>
        /// <param name="processor">The processor to filter data for</param>
        /// <param name="predicate">The filtering predicate to be applied</param>
        public FilteredDataProcessor(IDataProcessor processor, Func<IBaseData, bool> predicate)
        {
            _predicate = predicate;
            _processor = processor;
        }

        /// <summary>
        /// Invoked for each piece of data from the source file
        /// </summary>
        /// <param name="data">The data to be processed</param>
        public void Process(IBaseData data)
        {
            if (_predicate(data))
            {
                _processor.Process(data);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _processor.Dispose();
        }
    }
}