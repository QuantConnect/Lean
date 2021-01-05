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
    /// The advance-decline indicator compares the number of stocks 
    /// that closed higher against the number of stocks 
    /// that closed lower than their previous day's closing prices.
    /// </summary>
    public abstract class AdvanceDeclineIndicator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private IDictionary<SecurityIdentifier, TradeBar> _previousPeriod = new Dictionary<SecurityIdentifier, TradeBar>();
        private IDictionary<SecurityIdentifier, TradeBar> _currentPeriod = new Dictionary<SecurityIdentifier, TradeBar>();
        private readonly Func<IEnumerable<TradeBar>, decimal> _computeValue;
        private DateTime? _currentPeriodTime = null;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AdvanceDeclineRatio"/> class
        /// </summary>
        public AdvanceDeclineIndicator(string name, Func<IEnumerable<TradeBar>, decimal> compute) : base(name)
        {
            _computeValue = compute;
        }

        /// <summary>
        /// Add tracking stock issue
        /// </summary>
        /// <param name="symbol">tracking stock issue</param>
        public void AddStock(Symbol symbol)
        {
            if (!_currentPeriod.ContainsKey(symbol.ID))
            {
                _currentPeriod.Add(symbol.ID, null);
            }
        }

        /// <summary>
        /// Remove tracking stock issue
        /// </summary>
        /// <param name="symbol">tracking stock issue</param>
        public void RemoveStock(Symbol symbol)
        {
            _currentPeriod.Remove(symbol.ID);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _previousPeriod.Keys.Any();

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 2;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            var advStocks = new List<TradeBar>();
            var dclStocks = new List<TradeBar>();

            TradeBar tradeBar;
            foreach (var stock in _currentPeriod)
            {
                if (!_previousPeriod.TryGetValue(stock.Key, out tradeBar) || tradeBar == null)
                {
                    continue;
                }
                else if (stock.Value.Close < tradeBar.Close)
                {
                    dclStocks.Add(stock.Value);
                }
                else if (stock.Value.Close > tradeBar.Close)
                {
                    advStocks.Add(stock.Value);
                }
            }

            if (!dclStocks.Any())
            {
                return 0;
            }

            return _computeValue(advStocks) / _computeValue(dclStocks);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override IndicatorResult ValidateAndComputeNextValue(TradeBar input)
        {
            Enqueue(input);

            if (!_previousPeriod.Keys.Any() || _currentPeriod.Any(p => p.Value == null))
            {
                return new IndicatorResult(0, IndicatorStatus.ValueNotReady);
            }

            var vNext = ComputeNextValue(input);
            return new IndicatorResult(vNext);
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _previousPeriod.Clear();
            foreach (var key in _currentPeriod.Keys.ToList())
            {
                _currentPeriod[key] = null;
            }

            base.Reset();
        }

        private void Enqueue(TradeBar input)
        {
            if (input.EndTime == _currentPeriodTime)
            {
                _previousPeriod[input.Symbol.ID] = input;
                return;
            }

            if (input.Time > _currentPeriodTime)
            {
                _previousPeriod.Clear();
                foreach (var key in _currentPeriod.Keys.ToList())
                {
                    _previousPeriod[key] = _currentPeriod[key];
                    _currentPeriod[key] = null;
                }
                _currentPeriodTime = input.Time;
            }

            if (_currentPeriod.ContainsKey(input.Symbol.ID) && (!_currentPeriodTime.HasValue || input.Time == _currentPeriodTime))
            {
                _currentPeriod[input.Symbol.ID] = input;
                if (!_currentPeriodTime.HasValue)
                {
                    _currentPeriodTime = input.Time;
                }
            }
        }
    }
}
