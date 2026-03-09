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
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataProcessor"/> that consolidates the data
    /// stream and forwards the consolidated data to other processors
    /// </summary>
    public class ConsolidatorDataProcessor : IDataProcessor
    {
        private DateTime _frontier;
        private readonly IDataProcessor _destination;
        private readonly Func<IBaseData, IDataConsolidator> _createConsolidator;
        private readonly Dictionary<Symbol, IDataConsolidator> _consolidators;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsolidatorDataProcessor"/> class
        /// </summary>
        /// <param name="destination">The receiver of the consolidated data</param>
        /// <param name="createConsolidator">Function used to create consolidators</param>
        public ConsolidatorDataProcessor(IDataProcessor destination, Func<IBaseData, IDataConsolidator> createConsolidator)
        {
            _destination = destination;
            _createConsolidator = createConsolidator;
            _consolidators = new Dictionary<Symbol, IDataConsolidator>();
        }

        /// <summary>
        /// Invoked for each piece of data from the source file
        /// </summary>
        /// <param name="data">The data to be processed</param>
        public void Process(IBaseData data)
        {
            // grab the correct consolidator for this symbol
            IDataConsolidator consolidator;
            if (!_consolidators.TryGetValue(data.Symbol, out consolidator))
            {
                consolidator = _createConsolidator(data);
                consolidator.DataConsolidated += OnDataConsolidated;
                _consolidators[data.Symbol] = consolidator;
            }

            consolidator.Update(data);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _frontier = DateTime.MaxValue;

            // check the other consolidators to see if they also need to emit their working bars
            foreach (var consolidator in _consolidators.Values)
            {
                consolidator.Scan(_frontier);
            }

            _destination.Dispose();
            _consolidators.Clear();
        }

        /// <summary>
        /// Handles the <see cref="IDataConsolidator.DataConsolidated"/> event
        /// </summary>
        private void OnDataConsolidated(object sender, IBaseData args)
        {
            _destination.Process(args);

            // we've already checked this frontier time, so don't scan the consolidators
            if (_frontier >= args.EndTime) return;
            _frontier = args.EndTime;

            // check the other consolidators to see if they also need to emit
            foreach (var consolidator in _consolidators.Values)
            {
                // back up the time a single instance, this allows data at exact same
                // time to still come through
                consolidator.Scan(args.EndTime.AddTicks(-1));
            }
        }
    }
}