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
    public class PivotPointsHighLow : WindowIndicator<IBaseDataBar>
    {
        private readonly int _lengthHigh;
        private readonly int _lengthLow;

        // Indicator will keep information of the last 100 pivot points
        private readonly RollingWindow<PivotPoint> _pivotPoints = new RollingWindow<PivotPoint>(100);

        /// <summary>
        /// Event informs of new pivot point formed with new data update
        /// </summary>
        public event EventHandler<PivotPointsEventArgs> NewPivotPointFormed;

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator with an equal high and low length
        /// </summary>
        /// <param name="length">The length parameter here defines the number of surround bars that we compare against the current bar high and lows for the max/min </param>
        public PivotPointsHighLow(int length)
            : this($"PivotPointsHighLow({length})", length, length)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator
        /// </summary>
        /// <param name="lengthHigh">The number of surrounding bars whose high values should be less than the current one's for the high pivot point be assigned</param>
        /// <param name="lengthLow">The number of surrounding bars whose lows values should be less than the current one's for the low pivot point be assigned</param>
        public PivotPointsHighLow(int lengthHigh, int lengthLow)
            : this($"PivotPointsHighLow({lengthHigh},{lengthLow})", lengthHigh, lengthLow)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator
        /// </summary>
        /// <param name="name">The name of an indicator</param>
        /// <param name="lengthHigh">The number of surrounding bars whose high values should be less than the current one's for the high pivot point be assigned</param>
        /// <param name="lengthLow">The number of surrounding bars whose lows values should be less than the current one's for the low pivot point be assigned</param>
        public PivotPointsHighLow(string name, int lengthHigh, int lengthLow) :
            base(name, 2 * Math.Max(lengthLow, lengthHigh) + 1)
        {
            _lengthHigh = lengthHigh;
            _lengthLow = lengthLow;
        }

        /// <inheritdoc />
        protected override decimal ComputeNextValue(IReadOnlyWindow<IBaseDataBar> window, IBaseDataBar input)
        {
            if (!IsReady) return 0m;

            var middlePoint1 = window[_lengthHigh];
            var isHigh = false;
            for (var k = 0; k < (2 * _lengthHigh + 1); k++)
            {
                // Skip the middle point
                if (k == _lengthHigh) continue;

                // Check if current high is below middle point high
                isHigh = window[k].High < middlePoint1.High;

                // If any high is not below middle point high -> this is not a pivot point
                if (!isHigh) break;
            }

            var middlePoint2 = window[_lengthLow];
            var isLow = false;
            for (var k = 0; k < (2 * _lengthLow + 1); k++)
            {
                if (k == _lengthLow) continue;

                isLow = window[k].Low > middlePoint2.Low;

                if (!isLow) break;
            }

            // It's possible case when the bar forms both low and high points at the same time
            if (isHigh && isLow)
            {
                var pointHigh = new PivotPoint(PivotPointType.High, middlePoint1.High, middlePoint1.EndTime);
                _pivotPoints.Add(pointHigh);
                OnNewPivotPointFormed(new PivotPointsEventArgs(pointHigh));

                var pointLow = new PivotPoint(PivotPointType.Low, middlePoint2.Low, middlePoint2.EndTime);
                _pivotPoints.Add(pointLow);
                OnNewPivotPointFormed(new PivotPointsEventArgs(pointLow));

                return 2m;
            }

            if (isHigh)
            {
                var point = new PivotPoint(PivotPointType.High, middlePoint1.High, middlePoint1.EndTime);
                _pivotPoints.Add(point);
                OnNewPivotPointFormed(new PivotPointsEventArgs(point));

                return 1m;
            }

            if (isLow)
            {
                var point = new PivotPoint(PivotPointType.Low, middlePoint2.Low, middlePoint2.EndTime);
                _pivotPoints.Add(point);
                OnNewPivotPointFormed(new PivotPointsEventArgs(point));

                return -1m;
            }

            return 0m;
        }

        /// <summary>
        /// Get current high pivot points, in the order such that first element in collection is the nearest to the present date
        /// </summary>
        /// <returns>An array of high pivot points.</returns>
        /// <remarks>Returned array can be empty if no points have been registered yet/</remarks>
        public PivotPoint[] GetHighPivotPointsArray()
        {
            return _pivotPoints?.Where(p => p.PivotPointType == PivotPointType.High).ToArray();
        }

        /// <summary>
        /// Get current low pivot points, in the order such that first element in collection is the nearest to the present date
        /// </summary>
        /// <returns>An array of low pivot points.</returns>
        /// <remarks>Returned array can be empty if no points have been registered yet/</remarks>
        public PivotPoint[] GetLowPivotPointsArray()
        {
            return _pivotPoints?.Where(p => p.PivotPointType == PivotPointType.Low).ToArray();
        }

        /// <summary>
        /// Get all pivot points, in the order such that first element in collection is the nearest to the present date
        /// </summary>
        /// <returns>An array of low and high pivot points. Ordered by time in descending order.</returns>
        /// <remarks>Returned array can be empty if no points have been registered yet/</remarks>
        public PivotPoint[] GetAllPivotPointsArray()
        {
            // Get all pivot points within rolling wind. collection as an array
            return _pivotPoints?.Take(_pivotPoints.Count).ToArray();
        }

        /// <summary>
        /// Invokes NewPivotPointFormed event
        /// </summary>
        /// <param name="pivotPointsEventArgs"></param>
        protected virtual void OnNewPivotPointFormed(PivotPointsEventArgs pivotPointsEventArgs)
        {
            NewPivotPointFormed?.Invoke(this, pivotPointsEventArgs);
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
        /// Low pivot point
        /// </summary>
        Low = -1,

        /// <summary>
        /// No pivot point
        /// </summary>
        None = 0,

        /// <summary>
        /// High pivot point
        /// </summary>
        High = 1,

        /// <summary>
        /// Both high and low pivot point
        /// </summary>
        Both = 2
    }

    /// <summary>
    /// Event arguments class for the <see cref="PivotPointsHighLow.NewPivotPointFormed"/> event
    /// </summary>
    public class PivotPointsEventArgs : EventArgs
    {
        /// <summary>
        /// New pivot point
        /// </summary>
        public PivotPoint PivotPoint { get; }

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsEventArgs"/>
        /// </summary>
        /// <param name="pivotPoint"></param>
        public PivotPointsEventArgs(PivotPoint pivotPoint)
        {
            PivotPoint = pivotPoint;
        }
    }
}
