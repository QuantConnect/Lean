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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes Average Directional Index which measures trend strength without regard to trend direction.
    /// Firstly, it calculates the Directional Movement and the True Range value, and then the values are accumulated and smoothed
    /// using a custom smoothing method proposed by Wilder. For an n period smoothing, 1/n of each period's value is added to the total period.
    /// From these accumulated values we are therefore able to derived the 'Positive Directional Index' (+DI) and 'Negative Directional Index' (-DI)
    /// which is used to calculate the Average Directional Index.
    /// </summary>
    public class AverageDirectionalIndex : IndicatorBase<TradeBar>
    {
        private TradeBar _previousInput;

        private readonly int _period;

        private IndicatorBase<TradeBar> TrueRange { get; set; }

        private IndicatorBase<TradeBar> DirectionalMovementPlus { get; set; }

        private IndicatorBase<TradeBar> DirectionalMovementMinus { get; set; }

        private IndicatorBase<IndicatorDataPoint> SmoothedDirectionalMovementPlus { get; set; }

        private IndicatorBase<IndicatorDataPoint> SmoothedDirectionalMovementMinus { get; set; }

        private IndicatorBase<IndicatorDataPoint> SmoothedTrueRange { get; set; }

        /// <summary>
        /// Gets or sets the index of the Plus Directional Indicator
        /// </summary>
        /// <value>
        /// The  index of the Plus Directional Indicator.
        /// </value>
        public IndicatorBase<IndicatorDataPoint> PositiveDirectionalIndex { get; private set; }

        /// <summary>
        /// Gets or sets the index of the Minus Directional Indicator
        /// </summary>
        /// <value>
        /// The index of the Minus Directional Indicator.
        /// </value>
        public IndicatorBase<IndicatorDataPoint> NegativeDirectionalIndex { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AverageDirectionalIndex"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="period">The period.</param>
        public AverageDirectionalIndex(string name, int period)
            : base(name)
        {
            _period = period;

            TrueRange = new FunctionalIndicator<TradeBar>(name + "_TrueRange",
                currentBar =>
                {
                    var value = ComputeTrueRange(currentBar);
                    return value;
                },
                isReady => _previousInput != null
                );

            DirectionalMovementPlus = new FunctionalIndicator<TradeBar>(name + "_PositiveDirectionalMovement",
                currentBar =>
                {
                    var value = ComputePositiveDirectionalMovement(currentBar);
                    return value;
                },
                isReady => _previousInput != null
                );


            DirectionalMovementMinus = new FunctionalIndicator<TradeBar>(name + "_NegativeDirectionalMovement",
                currentBar =>
                {
                    var value = ComputeNegativeDirectionalMovement(currentBar);
                    return value;
                },
                isReady => _previousInput != null
                );

            PositiveDirectionalIndex = new FunctionalIndicator<IndicatorDataPoint>(name + "_PositiveDirectionalIndex",
                input => ComputePositiveDirectionalIndex(),
                positiveDirectionalIndex => DirectionalMovementPlus.IsReady && TrueRange.IsReady,
                () =>
                {
                    DirectionalMovementPlus.Reset();
                    TrueRange.Reset();
                }
                );

            NegativeDirectionalIndex = new FunctionalIndicator<IndicatorDataPoint>(name + "_NegativeDirectionalIndex",
                input => ComputeNegativeDirectionalIndex(),
                negativeDirectionalIndex => DirectionalMovementMinus.IsReady && TrueRange.IsReady,
                () =>
                {
                    DirectionalMovementMinus.Reset();
                    TrueRange.Reset();
                }
                );

            SmoothedTrueRange = new FunctionalIndicator<IndicatorDataPoint>(name + "_SmoothedTrueRange",
                    currentBar => ComputeSmoothedTrueRange(period),
                    isReady => _previousInput != null
                );


            SmoothedDirectionalMovementPlus = new FunctionalIndicator<IndicatorDataPoint>(name + "_SmoothedDirectionalMovementPlus",
                    currentBar => ComputeSmoothedDirectionalMovementPlus(period),
                    isReady => _previousInput != null
                );

            SmoothedDirectionalMovementMinus = new FunctionalIndicator<IndicatorDataPoint>(name + "_SmoothedDirectionalMovementMinus",
                    currentBar => ComputeSmoothedDirectionalMovementMinus(period),
                    isReady => _previousInput != null
                );
        }

        /// <summary>
        /// Computes the Smoothed Directional Movement Plus value.
        /// </summary>
        /// <param name="period">The period.</param>
        /// <returns></returns>
        private decimal ComputeSmoothedDirectionalMovementPlus(int period)
        {

            decimal value;

            if (Samples < period)
            {
                value = SmoothedDirectionalMovementPlus.Current + DirectionalMovementPlus.Current;
            }
            else
            {
                value = SmoothedDirectionalMovementPlus.Current - (SmoothedDirectionalMovementPlus.Current / period) + DirectionalMovementPlus.Current;
            }

            return value;
        }

        /// <summary>
        /// Computes the Smoothed Directional Movement Minus value.
        /// </summary>
        /// <param name="period">The period.</param>
        /// <returns></returns>
        private decimal ComputeSmoothedDirectionalMovementMinus(int period)
        {
            decimal value;

            if (Samples < period)
            {
                value = SmoothedDirectionalMovementMinus.Current + DirectionalMovementMinus.Current;
            }
            else
            {
                value = SmoothedDirectionalMovementMinus.Current - (SmoothedDirectionalMovementMinus.Current / 14) + DirectionalMovementMinus.Current;
            }

            return value;
        }

        /// <summary>
        /// Computes the Smoothed True Range value.
        /// </summary>
        /// <param name="period">The period.</param>
        /// <returns></returns>
        private decimal ComputeSmoothedTrueRange(int period)
        {
            decimal value;

            if (Samples < period)
            {
                value = SmoothedTrueRange.Current + TrueRange.Current;
            }
            else
            {
                value = SmoothedTrueRange.Current - (SmoothedTrueRange.Current / period) + TrueRange.Current;
            }
            return value;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return Samples >= _period; }
        }

        /// <summary>
        /// Computes the True Range value.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        private decimal ComputeTrueRange(TradeBar input)
        {
            var trueRange = new decimal(0.0);

            if (_previousInput == null) return trueRange;

            trueRange = (Math.Max(Math.Abs(input.Low - _previousInput.Close), Math.Max(TrueRange.Current, Math.Abs(input.High - _previousInput.Close))));

            return trueRange;
        }

        /// <summary>
        /// Computes the positive directional movement.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        private decimal ComputePositiveDirectionalMovement(TradeBar input)
        {
            var postiveDirectionalMovement = new decimal(0.0);

            if (_previousInput == null) return postiveDirectionalMovement;

            if ((input.High - _previousInput.High) >= (_previousInput.Low - input.Low))
            {
                if ((input.High - _previousInput.High) > 0)
                {
                    postiveDirectionalMovement = input.High - _previousInput.High;
                }
            }

            return postiveDirectionalMovement;
        }

        /// <summary>
        /// Computes the negative directional movement.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        private decimal ComputeNegativeDirectionalMovement(TradeBar input)
        {
            var negativeDirectionalMovement = new decimal(0.0);

            if (_previousInput == null) return negativeDirectionalMovement;

            if ((_previousInput.Low - input.Low) > (input.High - _previousInput.High))
            {
                if ((_previousInput.Low - input.Low) > 0)
                {
                    negativeDirectionalMovement = _previousInput.Low - input.Low;
                }
            }

            return negativeDirectionalMovement;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            TrueRange.Update(input);
            DirectionalMovementPlus.Update(input);
            DirectionalMovementMinus.Update(input);
            SmoothedTrueRange.Update(Current);
            SmoothedDirectionalMovementMinus.Update(Current);
            SmoothedDirectionalMovementPlus.Update(Current);
            if (_previousInput != null)
            {
                PositiveDirectionalIndex.Update(Current);
                NegativeDirectionalIndex.Update(Current);
            }
            var diff = Math.Abs(PositiveDirectionalIndex - NegativeDirectionalIndex);
            var sum = PositiveDirectionalIndex + NegativeDirectionalIndex;
            var value = sum == 0 ? 50 : ((_period - 1) * Current.Value + 100 * diff / sum ) / _period;
            _previousInput = input;
            return value;
        }

        /// <summary>
        /// Computes the Plus Directional Indicator (+DI period).
        /// </summary>
        /// <returns></returns>
        private decimal ComputePositiveDirectionalIndex()
        {
            if (SmoothedTrueRange == 0) return new decimal(0.0);

            var positiveDirectionalIndex = (SmoothedDirectionalMovementPlus.Current.Value / SmoothedTrueRange.Current.Value) * 100;

            return positiveDirectionalIndex;
        }

        /// <summary>
        /// Computes the Minus Directional Indicator (-DI period).
        /// </summary>
        /// <returns></returns>
        private decimal ComputeNegativeDirectionalIndex()
        {
            if (SmoothedTrueRange == 0) return new decimal(0.0);

            var negativeDirectionalIndex = (SmoothedDirectionalMovementMinus.Current.Value / SmoothedTrueRange.Current.Value) * 100;

            return negativeDirectionalIndex;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _previousInput = null;
            TrueRange.Reset();
            DirectionalMovementPlus.Reset();
            DirectionalMovementMinus.Reset();
            SmoothedTrueRange.Reset();
            SmoothedDirectionalMovementMinus.Reset();
            SmoothedDirectionalMovementPlus.Reset();
            PositiveDirectionalIndex.Reset();
            NegativeDirectionalIndex.Reset();
        }
    }
}
