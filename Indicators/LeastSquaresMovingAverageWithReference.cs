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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Least Squares Moving Average (LSMA) of a target in relation with a reference fits a least
    /// squares regression line of the target close prices on the reference close prices over the given
    /// period, instead of on the time index used by <see cref="LeastSquaresMovingAverage"/>. It then
    /// returns the value the regression line takes for the most recent reference price, which is the
    /// price the target is expected to have given where the reference is trading.
    ///
    /// It is common practice to use the SPX index as the reference, so that the indicator describes
    /// the target price in terms of the overall market level.
    ///
    /// The indicator only updates when both assets have a price for a time step. When a bar is missing
    /// for one of the assets, the indicator value fills forward to improve the accuracy of the indicator.
    /// </summary>
    public class LeastSquaresMovingAverageWithReference : DualSymbolIndicator<IBaseDataBar>
    {
        /// <summary>
        /// The point where the regression line crosses the y-axis (target price axis)
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Intercept { get; }

        /// <summary>
        /// The regression line slope, the target price change per unit of reference price change
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Slope { get; }

        /// <summary>
        /// Creates a new LeastSquaresMovingAverageWithReference indicator with the specified name,
        /// target, reference and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public LeastSquaresMovingAverageWithReference(string name, Symbol targetSymbol, Symbol referenceSymbol, int period)
            : base(name, targetSymbol, referenceSymbol, period)
        {
            // Assert the period is greater than one, otherwise the regression line can not be fitted
            if (period < 2)
            {
                throw new ArgumentException($"Period parameter for LeastSquaresMovingAverageWithReference indicator must be greater than 1 but was {period}.");
            }

            Intercept = new Identity(name + "_Intercept");
            Slope = new Identity(name + "_Slope");
        }

        /// <summary>
        /// Creates a new LeastSquaresMovingAverageWithReference indicator with the specified target,
        /// reference and period values
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public LeastSquaresMovingAverageWithReference(Symbol targetSymbol, Symbol referenceSymbol, int period)
            : this($"LSMA({period})", targetSymbol, referenceSymbol, period)
        {
        }

        /// <summary>
        /// Computes the value the regression line of the target on the reference takes for the
        /// most recent reference price
        /// </summary>
        protected override decimal ComputeIndicator()
        {
            // Until both windows are full, the indicator returns the target price, like the LSMA does
            if (!IsReady)
            {
                return TargetDataPoints[0].Close;
            }

            // Both windows only hold the data points of the time steps both symbols have a price for,
            // so the target and the reference prices pair up by index
            var referencePrices = ReferenceDataPoints.Select(x => (double)x.Close).ToArray();
            var targetPrices = TargetDataPoints.Select(x => (double)x.Close).ToArray();
            var (intercept, slope) = Fit.Line(x: referencePrices, y: targetPrices);

            // The regression line is undefined when the reference price does not change over the period
            if (intercept.IsNaNOrInfinity() || slope.IsNaNOrInfinity())
            {
                return TargetDataPoints[0].Close;
            }

            var endTime = TargetDataPoints[0].EndTime;
            Intercept.Update(endTime, intercept.SafeDecimalCast());
            Slope.Update(endTime, slope.SafeDecimalCast());

            return Intercept.Current.Value + Slope.Current.Value * ReferenceDataPoints[0].Close;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators (Intercept, Slope)
        /// </summary>
        public override void Reset()
        {
            Intercept.Reset();
            Slope.Reset();
            base.Reset();
        }
    }
}
