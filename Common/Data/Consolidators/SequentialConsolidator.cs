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

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// This consolidator wires up the events on its First and Second consolidators
    /// such that data flows from the First to Second consolidator. It's output comes
    /// from the Second.
    /// </summary>
    public class SequentialConsolidator : IDataConsolidator
    {
        /// <summary>
        /// Gets the first consolidator to receive data
        /// </summary>
        public IDataConsolidator First
        {
            get; private set;
        }

        /// <summary>
        /// Gets the second consolidator that ends up receiving data produced
        /// by the first
        /// </summary>
        public IDataConsolidator Second
        {
            get; private set;
        }

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        ///
        /// For a SequentialConsolidator, this is the output from the 'Second' consolidator.
        /// </summary>
        public IBaseData Consolidated
        {
            get { return Second.Consolidated; }
        }

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public IBaseData WorkingData
        {
            get { return Second.WorkingData; }
        }

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public Type InputType
        {
            get { return First.InputType; }
        }

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public Type OutputType
        {
            get { return Second.OutputType; }
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(IBaseData data)
        {
            First.Update(data);
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public void Scan(DateTime currentLocalTime)
        {
            First.Scan(currentLocalTime);
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated;

        /// <summary>
        /// Creates a new consolidator that will pump date through the first, and then the output
        /// of the first into the second. This enables 'wrapping' or 'composing' of consolidators
        /// </summary>
        /// <param name="first">The first consolidator to receive data</param>
        /// <param name="second">The consolidator to receive first's output</param>
        public SequentialConsolidator(IDataConsolidator first, IDataConsolidator second)
        {
            if (!second.InputType.IsAssignableFrom(first.OutputType))
            {
                throw new ArgumentException("first.OutputType must equal second.OutputType!");
            }
            First = first;
            Second = second;

            // wire up the second one to get data from the first
            first.DataConsolidated += (sender, consolidated) => second.Update(consolidated);

            // wire up the second one's events to also fire this consolidator's event so consumers
            // can attach
            second.DataConsolidated += (sender, consolidated) => OnDataConsolidated(consolidated);
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected virtual void OnDataConsolidated(IBaseData consolidated)
        {
            var handler = DataConsolidated;
            if (handler != null) handler(this, consolidated);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            First.Dispose();
            Second.Dispose();
            DataConsolidated = null;
        }
    }
}
