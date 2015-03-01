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
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Provides a base type for all indicators
    /// </summary>
    /// <typeparam name="T">The type of data input into this indicator</typeparam>
    public abstract class IndicatorBase<T> : IDataConsolidator
        where T : BaseData
    {
        /// <summary>the most recent input that was given to this indicator</summary>
        private T _previousInput;

        /// <summary>
        /// Initializes a new instance of the Indicator class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        protected IndicatorBase(string name)
        {
            Name = name;
            Current = new IndicatorDataPoint(DateTime.MinValue, 0m);
        }

        /// <summary>
        /// Gets a name for this indicator
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public abstract bool IsReady { get; }

        /// <summary>
        /// Gets the current state of this indicator. If the state has not been updated
        /// then the time on the value will equal DateTime.MinValue.
        /// </summary>
        public IndicatorDataPoint Current { get; private set; }

        /// <summary>
        /// Gets the number of samples processed by this indicator
        /// </summary>
        public long Samples { get; private set; }

        /// <summary>
        /// Updates the state of this indicator with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="input">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        public bool Update(T input)
        {
            if (_previousInput != null && input.Time < _previousInput.Time)
            {
                // if we receive a time in the past, throw
                throw new ArgumentException("This is a forward only indicator.");
            }
            if (!ReferenceEquals(input, _previousInput))
            {
                // compute a new value and update our previous time
                Samples++;
                _previousInput = input;
                var nextValue = ComputeNextValue(input);
                Current = new IndicatorDataPoint(input.Time, nextValue);

                // let others know we've produced a new data point
                OnDataConsolidated(Current);
            }
            return IsReady;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public virtual void Reset()
        {
            Samples = 0;
            Current = new IndicatorDataPoint(DateTime.MinValue, default(decimal));
        }

        /// <summary>
        /// Returns the current value of this instance
        /// </summary>
        /// <param name="instance">The indicator instance</param>
        /// <returns>The current value of the indicator</returns>
        public static implicit operator decimal(IndicatorBase<T> instance)
        {
            return instance.Current;
        }

        /// <summary>
        /// ToString Overload for Indicator Base
        /// </summary>
        /// <returns>String representation of the indicator</returns>
        public override string ToString()
        {
            return Current.Value.ToString("#######0.0####");
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected abstract decimal ComputeNextValue(T input);

        #region IDataConsolidator implementation

        /// <summary>
        /// Event handler that fires after this indicator is updated
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated;

        /// <summary>
        /// Gets the most recently produced piece of data output by this indicator
        /// </summary>
        public BaseData Consolidated
        {
            get { return Current; }
        }

        /// <summary>
        /// Gets the type consumed by this indicator
        /// </summary>
        public Type InputType
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// Gets the IndicatorDataPoint type
        /// </summary>
        public Type OutputType
        {
            get { return typeof(IndicatorDataPoint); }
        }

        /// <summary>
        /// Updates the state of this indicator with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="data">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        public void Update(BaseData data)
        {
            var typed = data as T;
            if (typed == null)
            {
                throw new ArgumentException("Expected input of type " + typeof(T).Name + " but received " + data.GetType().Name);
            }

            Update(typed);
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event
        /// </summary>
        /// <param name="consolidated">This is the new piece of data produced by this indicator</param>
        protected virtual void OnDataConsolidated(BaseData consolidated)
        {
            var handler = DataConsolidated;
            if (handler != null) handler(this, consolidated);
        }

        #endregion
    }
}