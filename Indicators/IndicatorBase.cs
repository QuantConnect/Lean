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
using System.Diagnostics;
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Event handler type for the IndicatorBase.Updated event
    /// </summary>
    /// <param name="sender">The indicator that fired the event</param>
    /// <param name="updated">The new piece of data produced by the indicator</param>
    public delegate void IndicatorUpdatedHandler(object sender, IndicatorDataPoint updated);

    /// <summary>
    /// Provides a base type for all indicators
    /// </summary>
    /// <typeparam name="T">The type of data input into this indicator</typeparam>
    [DebuggerDisplay("{ToDetailedString()}")]
    public abstract class IndicatorBase<T> : IComparable<IndicatorBase<T>>, IComparable
        where T : BaseData
    {
        /// <summary>the most recent input that was given to this indicator</summary>
        private T _previousInput;

        /// <summary>
        /// Event handler that fires after this indicator is updated
        /// </summary>
        public event IndicatorUpdatedHandler Updated;

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
        public IndicatorDataPoint Current { get; protected set; }

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
                throw new ArgumentException(string.Format("This is a forward only indicator: {0} Input: {1} Previous: {2}", Name, input.Time.ToString("u"), _previousInput.Time.ToString("u")));
            }
            if (!ReferenceEquals(input, _previousInput))
            {
                // compute a new value and update our previous time
                Samples++;
                _previousInput = input;
                var nextValue = ComputeNextValue(input);
                Current = new IndicatorDataPoint(input.Time, nextValue);

                // let others know we've produced a new data point
                OnUpdated(Current);
            }
            return IsReady;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public virtual void Reset()
        {
            Samples = 0;
            _previousInput = null;
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
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(IndicatorBase<T> other)
        {
            if (ReferenceEquals(other, null))
            {
                // everything is greater than null via MSDN
                return 1;
            }

            return Current.CompareTo(other.Current);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj"/> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj"/>. Greater than zero This instance follows <paramref name="obj"/> in the sort order. 
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param><exception cref="T:System.ArgumentException"><paramref name="obj"/> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
        public int CompareTo(object obj)
        {
            var other = obj as IndicatorBase<T>;
            if (other == null)
            {
                throw new ArgumentException("Object must be of type " + GetType().GetBetterTypeName());
            }

            return CompareTo(other);
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
        /// Provides a more detailed string of this indicator in the form of {Name} - {Value}
        /// </summary>
        /// <returns>A detailed string of this indicator's current state</returns>
        public string ToDetailedString()
        {
            return string.Format("{0} - {1}", Name, this);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected abstract decimal ComputeNextValue(T input);

        /// <summary>
        /// Event invocator for the Updated event
        /// </summary>
        /// <param name="consolidated">This is the new piece of data produced by this indicator</param>
        protected virtual void OnUpdated(IndicatorDataPoint consolidated)
        {
            var handler = Updated;
            if (handler != null) handler(this, consolidated);
        }
    }
}