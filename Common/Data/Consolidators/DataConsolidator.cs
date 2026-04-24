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
 *
*/

using System;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Represents a type that consumes BaseData instances and fires an event with consolidated
    /// and/or aggregated data.
    /// </summary>
    /// <typeparam name="TInput">The type consumed by the consolidator</typeparam>
    public abstract class DataConsolidator<TInput> : ConsolidatorBase
        where TInput : IBaseData
    {
        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
            if (!(data is TInput))
            {
                throw new ArgumentNullException(nameof(data),
                    $"Received type of {data.GetType().Name} but expected {typeof(TInput).Name}"
                );
            }
            Update((TInput)data);
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public abstract override void Scan(DateTime currentLocalTime);

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated;

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public abstract override IBaseData WorkingData { get; }

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public override Type InputType => typeof(TInput);

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public abstract override Type OutputType { get; }

        /// <summary>
        /// Updates this consolidator with the specified data. This method is
        /// responsible for raising the DataConsolidated event
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public abstract void Update(TInput data);

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public override void Dispose()
        {
            DataConsolidated = null;
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. Fires the event and updates the rolling window.
        /// </summary>
        protected override void OnDataConsolidated(IBaseData consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);
            base.OnDataConsolidated(consolidated);
        }
    }
}