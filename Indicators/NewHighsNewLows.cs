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

using NodaTime;
using QuantConnect.Data.Market;
using QuantConnect.Python;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The NewHighsNewLows is a New Highs / New Lows indicator that accepts IBaseDataBar data as its input.
    /// 
    /// This type is more of a shim/typedef to reduce the need to refer to things as NewHighsNewLows&lt;IBaseDataBar&gt;
    /// </summary>
    public class NewHighsNewLows : NewHighsNewLows<IBaseDataBar>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewHighsNewLows"/> class
        /// </summary>
        public NewHighsNewLows(string name, int period) : base(name, period)
        {
        }
    }

    /// <summary>
    /// The New Highs - New Lows indicator displays the daily difference or ratio between 
    /// the number of assets reaching new highs and the number of stocks reaching new lows
    /// in defined time period.
    /// </summary>
    public abstract class NewHighsNewLows<T> : IndicatorBase<T>, IIndicatorWarmUpPeriodProvider
        where T : class, IBaseDataBar
    {
        private readonly TrackedAssets _trackedAssets;
        private readonly int _period;
        private readonly AssetTimeZones _assetTimeZones = new();

        private DateTime? _currentPeriodStartTimeUtc;
        private DateTime? _currentPeriodEndTimeUtc;

        /// <summary>
        /// The time zone of this indicator's values, so that the values of assets of different exchange time zones share
        /// one clock. By default, the exchange time zone of the first asset added. That time zone can repeat an hour when
        /// daylight saving time ends: for assets that trade then, such as crypto with US equities, set
        /// <see cref="TimeZones.Utc"/>, which never does
        /// </summary>
        [PandasIgnore]
        public DateTimeZone TimeZone
        {
            get => _assetTimeZones.TimeZone;
            set => _assetTimeZones.TimeZone = value;
        }

        /// <summary>
        /// Difference between the number of assets reaching new highs and the number of assets
        /// reaching new lows in defined time period.
        /// </summary>
        public IndicatorBase<IBaseDataBar> Difference { get; }

        /// <summary>
        /// Ratio between the number of assets reaching new highs and the number of assets
        /// reaching new lows in defined time period.
        /// </summary>
        public IndicatorBase<IBaseDataBar> Ratio { get; }

        /// <summary>
        /// List of assets that reached new high
        /// </summary>
        protected ICollection<T> NewHighs { get; private set; } = [];

        /// <summary>
        /// List of assets that reached new high 
        /// </summary>
        protected ICollection<T> NewLows { get; private set; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="NewHighsNewLows{T}"/> class
        /// </summary>
        protected NewHighsNewLows(string name, int period) : base(name)
        {
            _period = period;
            _trackedAssets = new TrackedAssets(period);

            Difference = new FunctionalIndicator<IBaseDataBar>(
                $"{name}_Difference",
                (input) =>
                {
                    return NewHighs.Count - NewLows.Count;
                },
                _ => IsReady);

            Ratio = new FunctionalIndicator<IBaseDataBar>(
                $"{name}_Ratio",
                (input) =>
                {
                    return NewLows.Count == 0m ? NewHighs.Count : (decimal)NewHighs.Count / NewLows.Count;
                },
                _ => IsReady);
        }

        /// <summary>
        /// Add tracking asset issue
        /// </summary>
        /// <param name="asset">tracking asset issue</param>
        public virtual void Add(Symbol asset)
        {
            _assetTimeZones.Add(asset);
            _trackedAssets.Add(asset);
        }

        /// <summary>
        /// Remove tracking asset issue
        /// </summary>
        /// <param name="asset">tracking asset issue</param>
        public virtual void Remove(Symbol asset)
        {
            _trackedAssets.Remove(asset);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => HasSufficientPreviousDataForComputation();

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _trackedAssets.Reset();

            _currentPeriodStartTimeUtc = null;
            _currentPeriodEndTimeUtc = null;
            _assetTimeZones.Reset();

            Difference.Reset();
            Ratio.Reset();

            base.Reset();
        }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period + 1;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(T input)
        {
            NewHighs.Clear();
            NewLows.Clear();

            foreach (TrackedAsset asset in _trackedAssets)
            {
                if (asset.CurrentPeriodValue.High > asset.RollingPreviousHigh.Current.Value)
                {
                    NewHighs.Add(asset.CurrentPeriodValue);
                }

                if (asset.CurrentPeriodValue.Low < asset.RollingPreviousLow.Current.Value)
                {
                    NewLows.Add(asset.CurrentPeriodValue);
                }
            }

            Difference.Update(input);
            Ratio.Update(input);

            return Difference.Current.Value;
        }

        /// <summary>
        /// Gets the end time of the value this indicator computes from the given input, in <see cref="TimeZone"/>
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The end time of the indicator's value</returns>
        protected override DateTime GetValueEndTime(T input)
        {
            return _assetTimeZones.GetEndTime(input);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override IndicatorResult ValidateAndComputeNextValue(T input)
        {
            Enqueue(input);

            if (HasMissingCurrentPeriodValue() || !HasSufficientPreviousDataForComputation())
            {
                return new IndicatorResult(0, IndicatorStatus.ValueNotReady);
            }

            // The sub-indicators take the times of their values from the input they're given
            var endTime = GetValueEndTime(input);
            if (endTime != input.EndTime)
            {
                var valueInput = (T)(object)input.Clone();
                valueInput.Time = endTime - (input.EndTime - input.Time);
                valueInput.EndTime = endTime;
                input = valueInput;
            }

            var vNext = ComputeNextValue(input);
            return new IndicatorResult(vNext);
        }

        private void Enqueue(T input)
        {
            TrackedAsset trackedAsset;
            // Inputs of the same period share their UTC times, whatever their exchange time zones
            var (startTimeUtc, endTimeUtc) = _assetTimeZones.GetUtcPeriod(input);

            // In case of unordered input: an input of the period that ended when the current one started
            if (endTimeUtc == _currentPeriodStartTimeUtc && endTimeUtc < _currentPeriodEndTimeUtc)
            {
                // Update the previous values in rolling window
                if (_trackedAssets.TryGetAsset(input.Symbol, out trackedAsset))
                {
                    UpdatePreviousValues(trackedAsset, input);
                }
                return;
            }

            // In case of new period
            if (endTimeUtc > _currentPeriodEndTimeUtc)
            {
                foreach (TrackedAsset asset in _trackedAssets)
                {
                    // We got new period and therefore we need to update
                    // the previous value in rolling window so we can use them for calculation
                    UpdatePreviousValues(asset, asset.CurrentPeriodValue);
                    asset.CurrentPeriodValue = default;
                }
                _currentPeriodStartTimeUtc = startTimeUtc;
                _currentPeriodEndTimeUtc = endTimeUtc;
            }

            if (_trackedAssets.TryGetAsset(input.Symbol, out trackedAsset)
                && (!_currentPeriodEndTimeUtc.HasValue || endTimeUtc == _currentPeriodEndTimeUtc))
            {
                trackedAsset.CurrentPeriodValue = input;

                if (!_currentPeriodEndTimeUtc.HasValue)
                {
                    _currentPeriodStartTimeUtc = startTimeUtc;
                    _currentPeriodEndTimeUtc = endTimeUtc;
                }
            }
        }

        private bool HasMissingCurrentPeriodValue()
        {
            return _trackedAssets.Any(x => x.CurrentPeriodValue == null);
        }

        private bool HasSufficientPreviousDataForComputation()
        {
            // All() holds over an empty set, so without this the indicator reports itself
            // ready before any asset is tracked
            return _trackedAssets.Any()
                && _trackedAssets.All(asset =>
                    asset.RollingPreviousHigh.IsReady
                    && asset.RollingPreviousLow.IsReady);
        }

        private void UpdatePreviousValues(TrackedAsset asset, IBaseDataBar bar)
        {
            if (bar == null)
            {
                return;
            }

            asset.RollingPreviousHigh.Update(_currentPeriodEndTimeUtc.Value, bar.High);
            asset.RollingPreviousLow.Update(_currentPeriodEndTimeUtc.Value, bar.Low);
        }

        /// <summary>
        /// Assets tracked by NewHighsNewLows indicator
        /// </summary>
        private class TrackedAssets : IEnumerable<TrackedAsset>
        {
            private readonly int _period;

            private readonly IDictionary<SecurityIdentifier, TrackedAsset> _assets = new Dictionary<SecurityIdentifier, TrackedAsset>();

            public TrackedAssets(int period)
            {
                _period = period;
            }

            /// <summary>
            /// Add tracking asset issue
            /// </summary>
            /// <param name="asset">tracking asset issue</param>
            public void Add(Symbol asset)
            {
                if (!_assets.ContainsKey(asset.ID))
                {
                    _assets.Add(asset.ID, new TrackedAsset(_period));
                }
            }

            /// <summary>
            /// Remove tracking asset issue
            /// </summary>
            /// <param name="asset">tracking asset issue</param>
            public void Remove(Symbol asset)
            {
                _assets.Remove(asset.ID);
            }

            /// <summary>
            /// Resets tracked assets to its initial state
            /// </summary>
            public void Reset()
            {
                foreach (TrackedAsset asset in this)
                {
                    asset.Reset();
                }
            }

            public bool TryGetAsset(Symbol asset, out TrackedAsset trackedAsset)
            {
                return _assets.TryGetValue(asset.ID, out trackedAsset);
            }

            public IEnumerator<TrackedAsset> GetEnumerator()
            {
                return _assets.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Asset tracked by NewHighsNewLows indicator
        /// </summary>
        private class TrackedAsset
        {
            public T CurrentPeriodValue { get; set; }
            public Maximum RollingPreviousHigh { get; init; }
            public Minimum RollingPreviousLow { get; init; }

            public TrackedAsset(int period)
            {
                CurrentPeriodValue = default;
                RollingPreviousHigh = new Maximum(period);
                RollingPreviousLow = new Minimum(period);
            }
            /// <summary>
            /// Resets tracked asset to its initial state
            /// </summary>
            public void Reset()
            {
                CurrentPeriodValue = default;
                RollingPreviousHigh.Reset();
                RollingPreviousLow.Reset();
            }
        }
    }
}
