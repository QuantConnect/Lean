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
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Least Squares Moving Average (LSMA) first calculates a least squares regression line
    /// over the preceding time periods, and then projects it forward to the current period. In
    /// essence, it calculates what the value would be if the regression line continued.
    /// When a reference symbol is provided, the regression is performed against the reference
    /// values instead of time.
    /// Source: https://rtmath.net/assets/docs/finanalysis/html/b3fab79c-f4b2-40fb-8709-fdba43cdb363.htm
    /// </summary>
    public class LeastSquaresMovingAverage : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Array representing the time.
        /// </summary>
        private readonly double[] _t;

        /// <summary>
        /// The reference symbol to regress against.
        /// </summary>
        private readonly Symbol _referenceSymbol = Symbol.None;

        /// <summary>
        /// Rolling window of reference symbol data points.
        /// </summary>
        private readonly RollingWindow<IndicatorDataPoint> _referenceWindow = new(0);

        /// <summary>
        /// The point where the regression line crosses the y-axis (price-axis)
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Intercept { get; }

        /// <summary>
        /// The regression line slope
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Slope { get; }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => Period;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => base.IsReady && _referenceWindow.IsReady;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeastSquaresMovingAverage"/> class.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The number of data points to hold in the window</param>
        public LeastSquaresMovingAverage(string name, int period)
            : base(name, period)
        {
            _t = Vector<double>.Build.Dense(period, i => i + 1).ToArray();
            Intercept = new Identity(name + "_Intercept");
            Slope = new Identity(name + "_Slope");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeastSquaresMovingAverage"/> class.
        /// </summary>
        /// <param name="period">The number of data points to hold in the window.</param>
        public LeastSquaresMovingAverage(int period)
            : this($"LSMA({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeastSquaresMovingAverage"/> class
        /// with a reference symbol for regression.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol to regress against</param>
        /// <param name="period">The number of data points to hold in the window</param>
        public LeastSquaresMovingAverage(string name, Symbol referenceSymbol, int period)
            : this(name, period)
        {
            _referenceSymbol = referenceSymbol;
            _referenceWindow = new RollingWindow<IndicatorDataPoint>(period);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeastSquaresMovingAverage"/> class
        /// with a reference symbol for regression.
        /// </summary>
        /// <param name="referenceSymbol">The reference symbol to regress against</param>
        /// <param name="period">The number of data points to hold in the window</param>
        public LeastSquaresMovingAverage(Symbol referenceSymbol, int period)
            : this($"LSMA({period},{referenceSymbol})", referenceSymbol, period)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            if (input.Symbol == _referenceSymbol)
            {
                _referenceWindow.Add(input);
                return Current.Value;
            }

            return base.ComputeNextValue(input);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="window"></param>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>
        /// A new value for this indicator
        /// </returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            // Until the window is ready, the indicator returns the input value.
            if (!window.IsReady) return input.Value;

            // Sort the window by time, convert the observations to double and transform it to an array
            var series = window
                .OrderBy(i => i.EndTime)
                .Select(i => Convert.ToDouble(i.Value))
                .ToArray();

            var x = (decimal)Period;
            double intercept, slope;
            if (_referenceWindow.Size != 0 && _referenceWindow.IsReady)
            {
                var xValues = _referenceWindow
                    .OrderBy(i => i.EndTime)
                    .Select(i => Convert.ToDouble(i.Value))
                    .ToArray();
                x = _referenceWindow[0].Value;
                (intercept, slope) = Fit.Line(x: xValues, y: series);
            }
            else
            {
                (intercept, slope) = Fit.Line(x: _t, y: series);
            }

            Intercept.Update(input.EndTime, intercept.SafeDecimalCast());
            Slope.Update(input.EndTime, slope.SafeDecimalCast());

            // Calculate the fitted value corresponding to the input
            return Intercept.Current.Value + Slope.Current.Value * x;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators (Intercept, Slope)
        /// </summary>
        public override void Reset()
        {
            Intercept.Reset();
            Slope.Reset();
            _referenceWindow.Reset();
            base.Reset();
        }
    }
}
