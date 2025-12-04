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

using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The New Highs - New Lows indicator displays the daily difference between 
    /// the number of stocks reaching new highs and the number of stocks reaching new lows
    /// in defined time period.
    /// </summary>
    public abstract class NewHighsNewLowsIndicator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly IDictionary<SecurityIdentifier, TradeBar> _currentPeriod = new Dictionary<SecurityIdentifier, TradeBar>();
        private readonly IDictionary<SecurityIdentifier, Maximum> _rollingPreviousHighs = new Dictionary<SecurityIdentifier, Maximum>();
        private readonly IDictionary<SecurityIdentifier, Minimum> _rollingPreviousLows = new Dictionary<SecurityIdentifier, Minimum>();
        private readonly Func<IEnumerable<TradeBar>, decimal> _computeSubValue;
        private readonly Func<decimal, decimal, decimal> _computeMainValue;
        private readonly int _period;

        private DateTime? _currentPeriodTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvanceDeclineRatio"/> class
        /// </summary>
        public NewHighsNewLowsIndicator(
            string name,
            int period,
            Func<IEnumerable<TradeBar>, decimal> computeSubValue,
            Func<decimal, decimal, decimal> computeMainValue)
            : base(name)
        {
            _period = period;
            _computeSubValue = computeSubValue;
            _computeMainValue = computeMainValue;
        }

        /// <summary>
        /// Add tracking asset issue
        /// </summary>
        /// <param name="asset">tracking asset issue</param>
        public virtual void Add(Symbol asset)
        {
            if (!_currentPeriod.ContainsKey(asset.ID))
            {
                _currentPeriod.Add(asset.ID, null);
            }

            if (!_rollingPreviousHighs.ContainsKey(asset.ID))
            {
                _rollingPreviousHighs.Add(asset.ID, new Maximum(_period));
            }

            if (!_rollingPreviousLows.ContainsKey(asset.ID))
            {
                _rollingPreviousLows.Add(asset.ID, new Minimum(_period));
            }
        }

        /// <summary>
        /// Remove tracking asset issue
        /// </summary>
        /// <param name="asset">tracking asset issue</param>
        public virtual void Remove(Symbol asset)
        {
            _currentPeriod.Remove(asset.ID);
            _rollingPreviousHighs.Remove(asset.ID);
            _rollingPreviousLows.Remove(asset.ID);
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
            foreach (SecurityIdentifier key in _currentPeriod.Keys.ToList())
            {
                _currentPeriod[key] = null;
            }

            foreach (SecurityIdentifier key in _rollingPreviousHighs.Keys.ToList())
            {
                _rollingPreviousHighs[key].Reset();
            }

            foreach (SecurityIdentifier key in _rollingPreviousLows.Keys.ToList())
            {
                _rollingPreviousLows[key].Reset();
            }

            _currentPeriodTime = null;

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
        protected override decimal ComputeNextValue(TradeBar input)
        {
            List<TradeBar> newHighStocks = new List<TradeBar>();
            List<TradeBar> newLowStocks = new List<TradeBar>();

            foreach (KeyValuePair<SecurityIdentifier, TradeBar> stock in _currentPeriod)
            {
                if (!_rollingPreviousHighs.TryGetValue(stock.Key, out Maximum rollingHigh))
                {
                    continue;
                }

                if (!_rollingPreviousLows.TryGetValue(stock.Key, out Minimum rollingLow))
                {
                    continue;
                }

                if (stock.Value.High > rollingHigh.Current.Value)
                {
                    newHighStocks.Add(stock.Value);
                }
                if (stock.Value.Low < rollingLow.Current.Value)
                {
                    newLowStocks.Add(stock.Value);
                }
            }

            return _computeMainValue(_computeSubValue(newHighStocks), _computeSubValue(newLowStocks));
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override IndicatorResult ValidateAndComputeNextValue(TradeBar input)
        {
            Enqueue(input);

            if (HasMissingCurrentPeriodValue() || !HasSufficientPreviousDataForComputation())
            {
                return new IndicatorResult(0, IndicatorStatus.ValueNotReady);
            }

            decimal vNext = ComputeNextValue(input);
            return new IndicatorResult(vNext);
        }

        private void Enqueue(TradeBar input)
        {
            // In case of unordered input
            if (input.EndTime == _currentPeriodTime)
            {
                // Update the previous values in rolling window
                UpdatePreviousValues(input.Symbol.ID, input);
                return;
            }

            // In case of new period
            if (input.Time > _currentPeriodTime)
            {
                foreach (SecurityIdentifier key in _currentPeriod.Keys.ToList())
                {
                    // We got new period and therefore we need to update
                    // the previous value in rolling window so we can use them for calculation
                    UpdatePreviousValues(key, _currentPeriod[key]);
                    _currentPeriod[key] = null;
                }
                _currentPeriodTime = input.Time;
            }

            if (_currentPeriod.ContainsKey(input.Symbol.ID)
                && (!_currentPeriodTime.HasValue || input.Time == _currentPeriodTime))
            {
                _currentPeriod[input.Symbol.ID] = input;

                if (!_currentPeriodTime.HasValue)
                {
                    _currentPeriodTime = input.Time;
                }
            }
        }

        private bool HasMissingCurrentPeriodValue()
        {
            return _currentPeriod.Any(x => x.Value == null);
        }

        private bool HasSufficientPreviousDataForComputation()
        {
            return _currentPeriod.All(period =>
                _rollingPreviousHighs.TryGetValue(period.Key, out Maximum max) && max.IsReady
                && _rollingPreviousLows.TryGetValue(period.Key, out Minimum min) && min.IsReady);
        }

        private void UpdatePreviousValues(SecurityIdentifier symbol, TradeBar bar)
        {
            if (bar == null)
                return;

            if (!_rollingPreviousHighs.TryGetValue(symbol, out Maximum max)
                || !_rollingPreviousLows.TryGetValue(symbol, out Minimum min))
                return;

            max.Update(_currentPeriodTime.Value, bar.High);
            min.Update(_currentPeriodTime.Value, bar.Low);
        }
    }
}
