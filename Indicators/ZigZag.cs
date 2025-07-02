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
using QuantConnect.Data.Market;
using QuantConnect.Python;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The ZigZag indicator identifies significant turning points in price movements,
    /// filtering out noise using a sensitivity threshold and a minimum trend length.
    /// It alternates between high and low pivots to determine market trends.
    /// </summary>
    public class ZigZag : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// The most recent pivot point, represented as a bar of market data.
        /// Used as a reference for calculating subsequent pivots.
        /// </summary>
        private IBaseDataBar _lastPivot;

        /// <summary>
        /// The minimum number of bars required to confirm a valid trend.
        /// Ensures that minor fluctuations in price do not create false pivots.
        /// </summary>
        private readonly int _minTrendLength;

        /// <summary>
        /// The sensitivity threshold for detecting significant price movements.
        /// A decimal value between 0 and 1 that determines the percentage change required
        /// to recognize a new pivot.
        /// </summary>
        private readonly decimal _sensitivity;

        /// <summary>
        /// A counter to track the number of bars since the last pivot was identified.
        /// Used to enforce the minimum trend length requirement.
        /// </summary>
        private int _count;

        /// <summary>
        /// Tracks whether the most recent pivot was a low pivot.
        /// Used to alternate between identifying high and low pivots.
        /// </summary>
        private bool _lastPivotWasLow;

        /// <summary>
        /// Stores the most recent high pivot value in the ZigZag calculation.
        /// Updated whenever a valid high pivot is identified.
        /// </summary>
        [PandasIgnore]
        public Identity HighPivot { get; }

        /// <summary>
        /// Stores the most recent low pivot value in the ZigZag calculation.
        /// Updated whenever a valid low pivot is identified.
        /// </summary>
        [PandasIgnore]
        public Identity LowPivot { get; }

        /// <summary>
        /// Represents the current type of pivot (High or Low) in the ZigZag calculation.
        /// The value is updated based on the most recent pivot identified: 
        /// </summary>
        public PivotPointType PivotType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZigZag"/> class with the specified parameters.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="sensitivity">The sensitivity threshold as a decimal value between 0 and 1.</param>
        /// <param name="minTrendLength">The minimum number of bars required to form a valid trend.</param>
        public ZigZag(string name, decimal sensitivity = 0.05m, int minTrendLength = 1) : base(name)
        {
            if (sensitivity <= 0 || sensitivity >= 1)
            {
                throw new ArgumentException("Sensitivity must be between 0 and 1.", nameof(sensitivity));
            }

            if (minTrendLength < 1)
            {
                throw new ArgumentException("Minimum trend length must be greater than 0.", nameof(minTrendLength));
            }
            HighPivot = new Identity(name + "_HighPivot");
            LowPivot = new Identity(name + "_LowPivot");
            _sensitivity = sensitivity;
            _minTrendLength = minTrendLength;
            PivotType = PivotPointType.Low;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZigZag"/> class using default parameters.
        /// </summary>
        /// <param name="sensitivity">The sensitivity threshold as a decimal value between 0 and 1.</param>
        /// <param name="minTrendLength">The minimum number of bars required to form a valid trend.</param>
        public ZigZag(decimal sensitivity = 0.05m, int minTrendLength = 1)
            : this($"ZZ({sensitivity},{minTrendLength})", sensitivity, minTrendLength)
        {
        }

        /// <summary>
        /// Indicates whether the indicator has enough data to produce meaningful output.
        /// The indicator is considered "ready" when the number of samples exceeds the minimum trend length.
        /// </summary>
        public override bool IsReady => Samples > _minTrendLength;

        /// <summary>
        /// Gets the number of periods required for the indicator to warm up.
        /// This is equal to the minimum trend length plus one additional bar for initialization.
        /// </summary>
        public int WarmUpPeriod => _minTrendLength + 1;

        /// <summary>
        /// Computes the next value of the ZigZag indicator based on the input bar.
        /// Determines whether the input bar forms a new pivot or updates the current trend.
        /// </summary>
        /// <param name="input">The current bar of market data used for the calculation.</param>
        /// <returns>
        /// The value of the most recent pivot, either a high or low, depending on the current trend.
        /// </returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (_lastPivot == null)
            {
                UpdatePivot(input, true);
                return decimal.Zero;
            }

            var currentPivot = _lastPivotWasLow ? _lastPivot.Low : _lastPivot.High;

            if (_lastPivotWasLow)
            {
                if (input.High >= _lastPivot.Low * (1m + _sensitivity) && _count >= _minTrendLength)
                {
                    UpdatePivot(input, false);
                    currentPivot = HighPivot;
                }
                else if (input.Low <= _lastPivot.Low)
                {
                    UpdatePivot(input, true);
                    currentPivot = LowPivot;
                }
            }
            else
            {
                if (input.Low <= _lastPivot.High * (1m - _sensitivity) && _count >= _minTrendLength)
                {
                    UpdatePivot(input, true);
                    currentPivot = LowPivot;
                }
                else if (input.High >= _lastPivot.High)
                {
                    UpdatePivot(input, false);
                    currentPivot = HighPivot;
                }
            }
            _count++;
            return currentPivot;
        }

        /// <summary>
        /// Updates the pivot point based on the given input bar. 
        /// If a change in trend is detected, the pivot type is switched and the corresponding pivot (high or low) is updated.
        /// </summary>
        /// <param name="input">The current bar of market data used for the update.</param>
        /// <param name="pivotDirection">Indicates whether the trend has reversed.</param>
        private void UpdatePivot(IBaseDataBar input, bool pivotDirection)
        {
            _lastPivot = input;
            _count = 0;
            if (_lastPivotWasLow == pivotDirection)
            {
                //Update previous pivot
                (_lastPivotWasLow ? LowPivot : HighPivot).Update(input.EndTime, _lastPivotWasLow ? input.Low : input.High);
            }
            else
            {
                //Create new pivot
                (_lastPivotWasLow ? HighPivot : LowPivot).Update(input.EndTime, _lastPivotWasLow ? input.High : input.Low);
                PivotType = _lastPivotWasLow ? PivotPointType.High : PivotPointType.Low;
            }
            _lastPivotWasLow = pivotDirection;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _lastPivot = null;
            PivotType = PivotPointType.Low;
            HighPivot.Reset();
            LowPivot.Reset();
            base.Reset();
        }
    }
}
