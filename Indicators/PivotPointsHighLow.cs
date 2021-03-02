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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Pivot Points (High/Low), also known as Bar Count Reversals, indicator.
    /// https://www.fidelity.com/learning-center/trading-investing/technical-analysis/technical-indicator-guide/pivot-points-high-low
    /// </summary>
    public class PivotPointsHighLow : WindowIndicator<IBaseDataBar>
    {
        private readonly int _length;

        /// <summary>
        /// Array of high pivot points, such that first element is nearest to the present date
        /// </summary>
        public PivotPoint[] HighPivotPoints;

        /// <summary>
        /// Array of low pivot points, such that first element is nearest to the present date
        /// </summary>
        public PivotPoint[] LowPivotPoints;

        /// <summary>
        /// Array of both high and low pivot points, such that first element is nearest to the present date
        /// </summary>
        public PivotPoint[] PivotPoints;

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator
        /// </summary>
        /// <param name="length"></param>
        public PivotPointsHighLow(int length)
            : this ($"PivotPointsHighLow({length})", length)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator
        /// </summary>
        /// <param name="name"></param>
        /// <param name="period"></param>
        public PivotPointsHighLow(string name, int period) : 
            base(name, 2 * period + 1)
        {
            _length = period;
        }

        /// <inheritdoc />
        protected override decimal ComputeNextValue(IReadOnlyWindow<IBaseDataBar> window, IBaseDataBar input)
        {
            if (!IsReady) return 0m;

            var middlePoint = window[_length];

            var isHigh = false;
            for (var k = 0; k < window.Count; k++)
            {
                // Skip the middle point
                if (k == _length) continue;

                // Check if current high is below middle point high
                isHigh = window[k].High < middlePoint.High;

                // If any high is not below middle point high -> this is not a pivot point
                if (!isHigh) break;
            }

            var isLow = false;
            for (var k = 0; k < window.Count; k++)
            {
                if (k == _length) continue;

                isLow = window[k].Low > middlePoint.Low;

                if (!isLow) break;
            }

            // It's possible case when the bar forms both low and high points at the same time
            if (isHigh && isLow)
            {
                var pointHigh = new PivotPoint(PivotPointType.High, middlePoint.High, middlePoint.EndTime);
                Push(pointHigh, ref HighPivotPoints);
                Push(pointHigh, ref PivotPoints);

                var pointLow = new PivotPoint(PivotPointType.Low, middlePoint.Low, middlePoint.EndTime);
                Push(pointLow, ref LowPivotPoints);
                Push(pointLow, ref PivotPoints);
                return 2m;
            }

            if (isHigh)
            {
                var point = new PivotPoint(PivotPointType.High, middlePoint.High, middlePoint.EndTime);
                Push(point, ref HighPivotPoints);
                Push(point, ref PivotPoints);
                return 1m;
            }

            if (isLow)
            {
                var point = new PivotPoint(PivotPointType.Low, middlePoint.Low, middlePoint.EndTime);
                Push(point, ref LowPivotPoints);
                Push(point, ref PivotPoints);
                return -1m;
            }

            return 0m;
        }

        // Will replace an array passed by reference with a new one, containing new pivot point,
        // where the the order of the elements will be from nearest to farthest in time
        private void Push(PivotPoint point, ref PivotPoint[] array)
        {
            PivotPoint[] tempArray;
            int size;
            if (array == null)
            {
                size = 1;
                tempArray = new PivotPoint[size];
            }
            else
            {
                size = array.Length + 1;
                tempArray = new PivotPoint[size];
                for (var n = 0; n < array.Length; n++)
                {
                    tempArray[n + 1] = array[n];
                }
            }

            tempArray[0] = point;
            array = tempArray;
        }
    }

    /// <summary>
    /// Represents the points identified by Pivot Point High/Low Indicator.
    /// </summary>
    public class PivotPoint : BaseData
    {
        /// <summary>
        /// Represents pivot point type : High or Low
        /// </summary>
        public PivotPointType PivotPointType { get; set; }

        /// <summary>
        /// Peak value
        /// </summary>
        public sealed override decimal Value { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="PivotPoint"/>
        /// </summary>
        public PivotPoint(PivotPointType type, decimal price, DateTime time)
        {
            PivotPointType = type;
            Value = price;
            Time = time;
        }
    }

    /// <summary>
    /// Pivot point direction
    /// </summary>
    public enum PivotPointType
    {
        /// <summary>
        /// High pivot point
        /// </summary>
        High = 1,

        /// <summary>
        /// Low pivot point
        /// </summary>
        Low = -1
    }
}
