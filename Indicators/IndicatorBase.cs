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
using System.Diagnostics;
using QuantConnect.Logging;
using System.Collections.Generic;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Abstract Indicator base, meant to contain non-generic fields of indicator base to support non-typed inputs
    /// </summary>
    public abstract partial class IndicatorBase : WindowBase<IndicatorDataPoint>, IIndicator
    {
        /// <summary>
        /// Tracks whether any indicator on the current thread is being updated. Indicators routinely read
        /// each other's 'Current' while computing, e.g. MACD reads its EMAs before they are ready, so the
        /// not-ready read warning only applies to reads happening outside of any indicator update
        /// </summary>
        [ThreadStatic]
        private static int _updateScopeDepth;

        /// <summary>
        /// True once the not-ready 'Current' read warning is no longer applicable for this indicator,
        /// either because it was already logged or because the indicator got ready. Keeps the hot path
        /// to a single boolean check
        /// </summary>
        private bool _notReadyCurrentWarningDisabled;

        /// <summary>
        /// The data consolidators associated with this indicator if any
        /// </summary>
        /// <remarks>These references allow us to unregister an indicator from getting future data updates through it's consolidators.
        /// We need multiple consolitadors because some indicators consume data from multiple different symbols</remarks>
        public ISet<IDataConsolidator> Consolidators { get; } = new HashSet<IDataConsolidator>();

        /// <summary>
        /// Gets the current state of this indicator. If the state has not been updated
        /// then the time on the value will equal DateTime.MinValue.
        /// </summary>
        /// <remarks>Reading 'Current' while the indicator is not ready yields a meaningless value, a common
        /// silent logic bug, so the first such user read logs a warning naming the samples needed vs received</remarks>
        public override IndicatorDataPoint Current
        {
            get
            {
                if (!_notReadyCurrentWarningDisabled)
                {
                    ValidateCurrentAccess();
                }
                return base.Current;
            }
            protected set
            {
                base.Current = value;
            }
        }

        /// <summary>
        /// Gets the previous state of this indicator. If the state has not been updated
        /// then the time on the value will equal DateTime.MinValue.
        /// </summary>
        public override IndicatorDataPoint Previous => base.Previous ?? new IndicatorDataPoint(DateTime.MinValue, 0);

        /// <summary>
        /// Gets a name for this indicator
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the number of samples processed by this indicator
        /// </summary>
        public long Samples { get; internal set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public abstract bool IsReady { get; }

        /// <summary>
        /// Event handler that fires after this indicator is updated
        /// </summary>
        public event IndicatorUpdatedHandler Updated;

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Initializes a new instance of the Indicator class.
        /// </summary>
        protected IndicatorBase() : base(DefaultWindowSize)
        {
            Current = new IndicatorDataPoint(DateTime.MinValue, 0m);
        }

        /// <summary>
        /// Initializes a new instance of the Indicator class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        protected IndicatorBase(string name)
            : this()
        {
            Name = name;
        }

        /// <summary>
        /// Event invocator for the Updated event
        /// </summary>
        /// <param name="consolidated">This is the new piece of data produced by this indicator</param>
        protected virtual void OnUpdated(IndicatorDataPoint consolidated)
        {
            Updated?.Invoke(this, consolidated);
        }

        /// <summary>
        /// Updates the state of this indicator with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="input">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        public abstract bool Update(IBaseData input);

        /// <summary>
        /// Updating an indicator with a raw value is not supported: an update always requires a time.
        /// This overload exists to turn a common misuse, e.g. 'indicator.update(bar.close)' in Python, which
        /// otherwise surfaces as a hard to understand runtime binding error, into a prescriptive error naming
        /// the valid update forms. Python numeric values bind to it through their double conversion.
        /// It takes a double on purpose: a decimal overload would make existing 'Update(IndicatorDataPoint)'
        /// C# call sites ambiguous through the data point's implicit decimal conversion
        /// </summary>
        /// <param name="value">The value the indicator was incorrectly updated with</param>
        public bool Update(double value)
        {
            if (this is IndicatorBase<IndicatorDataPoint>)
            {
                throw new NotSupportedException($"{GetType().Name}.Update() requires a time for each new value." +
                    " Use one of the following methods instead: Update(DateTime, decimal), e.g. 'indicator.Update(bar.EndTime, bar.Close)', or Update(IndicatorDataPoint)");
            }

            var suggestions = new List<string>
            {
                "Update(TradeBar)",
                "Update(QuoteBar)"
            };

            if (this is IndicatorBase<IBaseData>)
            {
                suggestions.Add("Update(Tick)");
            }

            throw new NotSupportedException($"{GetType().Name} does not support being updated with just a value: it requires a full data bar." +
                $" Use one of the following methods instead: {string.Join(", ", suggestions)}");
        }

        /// <summary>
        /// Flags the current thread as updating an indicator, see <see cref="_updateScopeDepth"/>
        /// </summary>
        protected static void EnterUpdateScope()
        {
            _updateScopeDepth++;
        }

        /// <summary>
        /// Removes the indicator updating flag from the current thread, see <see cref="_updateScopeDepth"/>
        /// </summary>
        protected static void ExitUpdateScope()
        {
            _updateScopeDepth--;
        }

        /// <summary>
        /// Logs a warning, once per indicator, when 'Current' is read outside of any indicator update
        /// while this indicator is not ready yet
        /// </summary>
        private void ValidateCurrentAccess()
        {
            if (IsReady)
            {
                // once ready the warning no longer applies, disable the check so reads only cost a boolean check
                _notReadyCurrentWarningDisabled = true;
                return;
            }

            if (_updateScopeDepth != 0)
            {
                // internal read from another indicator's update, not a user read
                return;
            }

            _notReadyCurrentWarningDisabled = true;
            var samplesNeeded = (this as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;
            var samplesReceived = $"received {Samples} sample{(Samples == 1 ? string.Empty : "s")}";
            var requirement = samplesNeeded > 0 ? $"{samplesReceived} but requires {samplesNeeded}" : samplesReceived;
            Log.Error($"IndicatorBase.Current: The indicator '{Name}' is not ready yet, it has {requirement}." +
                " Its value is not meaningful until 'IsReady' is true. Either check 'IsReady' before reading 'Current', or warm the indicator up," +
                " e.g. with 'Settings.AutomaticIndicatorWarmUp = true' or 'algorithm.WarmUpIndicator()'. This message is only logged once per indicator.");
        }

        /// <summary>
        /// ToString Overload for Indicator Base
        /// </summary>
        /// <returns>String representation of the indicator</returns>
        public override string ToString()
        {
            return Current.Value.ToStringInvariant("#######0.0####");
        }

        /// <summary>
        /// Provides a more detailed string of this indicator in the form of {Name} - {Value}
        /// </summary>
        /// <returns>A detailed string of this indicator's current state</returns>
        public string ToDetailedString()
        {
            return $"{Name} - {this}";
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(IIndicator other)
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
            var other = obj as IndicatorBase;
            if (other == null)
            {
                throw new ArgumentException("Object must be of type " + GetType().GetBetterTypeName());
            }

            return CompareTo(other);
        }

    }

    /// <summary>
    /// Provides a base type for all indicators
    /// </summary>
    /// <typeparam name="T">The type of data input into this indicator</typeparam>
    [DebuggerDisplay("{ToDetailedString()}")]
    public abstract class IndicatorBase<T> : IndicatorBase
        where T : IBaseData
    {
        private bool _loggedForwardOnlyIndicatorError;

        /// <summary>the most recent input that was given to this indicator</summary>
        private Dictionary<SecurityIdentifier, T> _previousInput = new Dictionary<SecurityIdentifier, T>();

        /// <summary>
        /// Initializes a new instance of the Indicator class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        protected IndicatorBase(string name)
            : base(name)
        { }

        /// <summary>
        /// Updates the state of this indicator with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="input">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        public override bool Update(IBaseData input)
        {
            EnterUpdateScope();
            try
            {
                T _previousSymbolInput = default(T);
                if (_previousInput.TryGetValue(input.Symbol.ID, out _previousSymbolInput) && input.EndTime < _previousSymbolInput.EndTime)
                {
                    if (!_loggedForwardOnlyIndicatorError)
                    {
                        _loggedForwardOnlyIndicatorError = true;
                        // if we receive a time in the past, log once and return
                        Log.Error($"IndicatorBase.Update(): This is a forward only indicator: {Name} Input: {input.EndTime:u} Previous: {_previousSymbolInput.EndTime:u}. It will not be updated with this input.");
                    }
                    return IsReady;
                }
                if (!ReferenceEquals(input, _previousSymbolInput))
                {
                    // compute a new value and update our previous time
                    Samples++;

                    if (!(input is T))
                    {
                        if (typeof(T) == typeof(IndicatorDataPoint))
                        {
                            input = new IndicatorDataPoint(input.Symbol, input.EndTime, input.Value);
                        }
                        else
                        {
                            throw new ArgumentException($"IndicatorBase.Update() 'input' expected to be of type {typeof(T)} but is of type {input.GetType()}");
                        }
                    }
                    _previousInput[input.Symbol.ID] = (T)input;

                    var nextResult = ValidateAndComputeNextValue((T)input);
                    if (nextResult.Status == IndicatorStatus.Success)
                    {
                        Current = new IndicatorDataPoint(input.Symbol, input.EndTime, nextResult.Value);

                        // let others know we've produced a new data point
                        OnUpdated(Current);
                    }
                }
                return IsReady;
            }
            finally
            {
                ExitUpdateScope();
            }
        }

        /// <summary>
        /// Updates the state of this indicator with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="time">The time associated with the value</param>
        /// <param name="value">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        public bool Update(DateTime time, decimal value)
        {
            if (typeof(T) == typeof(IndicatorDataPoint))
            {
                return Update((T)(object)new IndicatorDataPoint(time, value));
            }

            var suggestions = new List<string>
            {
                "Update(TradeBar)",
                "Update(QuoteBar)"
            };

            if (typeof(T) == typeof(IBaseData))
            {
                suggestions.Add("Update(Tick)");
            }

            throw new NotSupportedException($"{GetType().Name} does not support the `Update(DateTime, decimal)` method. Use one of the following methods instead: {string.Join(", ", suggestions)}");
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            Samples = 0;
            _previousInput.Clear();
            Window.Reset();
            Current = new IndicatorDataPoint(DateTime.MinValue, default(decimal));
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected abstract decimal ComputeNextValue(T input);

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// and returns an instance of the <see cref="IndicatorResult"/> class
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>An IndicatorResult object including the status of the indicator</returns>
        protected virtual IndicatorResult ValidateAndComputeNextValue(T input)
        {
            // default implementation always returns IndicatorStatus.Success
            return new IndicatorResult(ComputeNextValue(input));
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            // this implementation acts as a liason to prevent inconsistency between the operators
            // == and != against primitive types. the core impl for equals between two indicators
            // is still reference equality, however, when comparing value types (floats/int, ect..)
            // we'll use value type semantics on Current.Value
            // because of this, we shouldn't need to override GetHashCode as well since we're still
            // solely relying on reference semantics (think hashset/dictionary impls)

            if (ReferenceEquals(obj, null)) return false;
            var type = obj.GetType();

            while (type != null && type != typeof(object))
            {
                var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (typeof(IndicatorBase<>) == cur)
                {
                    return ReferenceEquals(this, obj);
                }
                type = type.BaseType;
            }

            try
            {
                // the obj is not an indicator, so let's check for value types, try converting to decimal
                var converted = obj.ConvertInvariant<decimal>();
                return Current.Value == converted;
            }
            catch (InvalidCastException)
            {
                // conversion failed, return false
                return false;
            }
        }

        /// <summary>
        /// Get Hash Code for this Object
        /// </summary>
        /// <returns>Integer Hash Code</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
