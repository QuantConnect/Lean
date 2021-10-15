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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Pivot Points (High/Low), also known as Bar Count Reversals, indicator.
    /// https://www.fidelity.com/learning-center/trading-investing/technical-analysis/technical-indicator-guide/pivot-points-high-low
    /// </summary>
    public class PivotPointsHighLow : BasePivotPointsHighLow
    {
        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator with an equal high and low length
        /// </summary>
        /// <param name="surroundingBarsCount">The length parameter here defines the number of surrounding bars that we compare against the current bar high and lows for the max/min </param>
        /// <param name="lastStoredValues">The number of last stored indicator values</param>
        public PivotPointsHighLow(int surroundingBarsCount, int lastStoredValues = 100)
            : base($"PivotPointsHighLow({surroundingBarsCount})", surroundingBarsCount, surroundingBarsCount, lastStoredValues)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator
        /// </summary>
        /// <param name="surroundingBarsCountForHighPoint">The number of surrounding bars whose high values should be less than the current bar's for the bar high to be marked as high pivot point</param>
        /// <param name="surroundingBarsCountForLowPoint">The number of surrounding bars whose low values should be more than the current bar's for the bar low to be marked as low pivot point</param>
        /// <param name="lastStoredValues">The number of last stored indicator values</param>
        public PivotPointsHighLow(int surroundingBarsCountForHighPoint, int surroundingBarsCountForLowPoint, int lastStoredValues = 100)
            : base($"PivotPointsHighLow({surroundingBarsCountForHighPoint},{surroundingBarsCountForLowPoint})", surroundingBarsCountForHighPoint, surroundingBarsCountForLowPoint, lastStoredValues)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator
        /// </summary>
        /// <param name="name">The name of an indicator</param>
        /// <param name="surroundingBarsCountForHighPoint">The number of surrounding bars whose high values should be less than the current bar's for the bar high to be marked as high pivot point</param>
        /// <param name="surroundingBarsCountForLowPoint">The number of surrounding bars whose low values should be more than the current bar's for the bar low to be marked as low pivot point</param>
        /// <param name="lastStoredValues">The number of last stored indicator values</param>
        public PivotPointsHighLow(string name, int surroundingBarsCountForHighPoint, int surroundingBarsCountForLowPoint, int lastStoredValues = 100)
            : base(name, surroundingBarsCountForHighPoint, surroundingBarsCountForLowPoint, lastStoredValues)
        { }

        /// <inheritdoc/>
        protected override PivotPoint FindNextLowPivotPoint(RollingWindow<IBaseDataBar> windowLows, int midPointIndexOrSurroundingBarsCount)
        {
            var isLow = true;
            var middlePoint = windowLows[midPointIndexOrSurroundingBarsCount];
            for (var k = 0; k < windowLows.Size && isLow; k++)
            {
                if (k == midPointIndexOrSurroundingBarsCount)
                {
                    continue;
                }

                isLow = windowLows[k].Low > middlePoint.Low;
            }

            PivotPoint low = null;
            if (isLow)
            {
                low = new PivotPoint(PivotPointType.Low, middlePoint.Low, middlePoint.EndTime);
            }

            return low;
        }

        /// <inheritdoc/>
        protected override PivotPoint FindNextHighPivotPoint(RollingWindow<IBaseDataBar> windowHighs, int midPointIndexOrSurroundingBarsCount)
        {
            var isHigh = true;
            var middlePoint = windowHighs[midPointIndexOrSurroundingBarsCount];
            for (var k = 0; k < windowHighs.Size && isHigh; k++)
            {
                // Skip the middle point
                if (k == midPointIndexOrSurroundingBarsCount)
                {
                    continue;
                }

                // Check if current high is below middle point high
                isHigh = windowHighs[k].High < middlePoint.High;
            }

            PivotPoint high = null;
            if (isHigh)
            {
                high = new PivotPoint(PivotPointType.High, middlePoint.High, middlePoint.EndTime);
            }

            return high;
        }

        /// <inheritdoc/>
        protected override decimal ConvertToComputedValue(PivotPoint highPoint, PivotPoint lowPoint)
        {
            if (highPoint != null)
            {
                if (lowPoint != null)
                {
                    // Can be the bar forms both high and low pivot points at the same time
                    return (decimal)PivotPointType.Both;
                }
                return (decimal)PivotPointType.High;
            }

            if (lowPoint != null)
            {
                return (decimal)PivotPointType.Low;
            }

            return (decimal)PivotPointType.None;
        }
    }
}
