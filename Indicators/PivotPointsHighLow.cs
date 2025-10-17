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
    public class PivotPointsHighLow : IndicatorBase<IBaseDataBar>, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _surroundingBarsCountForHighPoint;
        private readonly int _surroundingBarsCountForLowPoint;
        private readonly RollingWindow<IBaseDataBar> _windowHighs;
        private readonly RollingWindow<IBaseDataBar> _windowLows;
        // Stores information of that last N pivot points
        private readonly RollingWindow<PivotPoint> _windowPivotPoints;
        private readonly bool _strict;

        /// <summary>
        /// Event informs of new pivot point formed with new data update
        /// </summary>
        public event EventHandler<PivotPointsEventArgs> NewPivotPointFormed;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _windowHighs.IsReady && _windowLows.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; protected set; }

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator with an equal high and low length
        /// </summary>
        /// <param name="surroundingBarsCount">The length parameter here defines the number of surrounding bars that we compare against the current bar high and lows for the max/min </param>
        /// <param name="lastStoredValues">The number of last stored indicator values</param>
        public PivotPointsHighLow(int surroundingBarsCount, int lastStoredValues = 100)
            : this($"PivotPointsHighLow({surroundingBarsCount})", surroundingBarsCount, surroundingBarsCount, lastStoredValues)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator
        /// </summary>
        /// <param name="surroundingBarsCountForHighPoint">The number of surrounding bars whose high values should be less than the current bar's for the bar high to be marked as high pivot point</param>
        /// <param name="surroundingBarsCountForLowPoint">The number of surrounding bars whose low values should be more than the current bar's for the bar low to be marked as low pivot point</param>
        /// <param name="lastStoredValues">The number of last stored indicator values</param>
        /// <param name="strict">When true (default), uses strict inequalities (&gt; and &lt;). When false, uses relaxed inequalities (&gt;= and &lt;=) allowing equal values to be detected as pivot points.</param>
        public PivotPointsHighLow(int surroundingBarsCountForHighPoint, int surroundingBarsCountForLowPoint, int lastStoredValues = 100, bool strict = true)
            : this($"PivotPointsHighLow({surroundingBarsCountForHighPoint},{surroundingBarsCountForLowPoint})", surroundingBarsCountForHighPoint, surroundingBarsCountForLowPoint, lastStoredValues, strict)
        { }


        /// <summary>
        /// Creates a new instance of <see cref="PivotPointsHighLow"/> indicator
        /// </summary>
        /// <param name="name">The name of an indicator</param>
        /// <param name="surroundingBarsCountForHighPoint">The number of surrounding bars whose high values should be less than the current bar's for the bar high to be marked as high pivot point</param>
        /// <param name="surroundingBarsCountForLowPoint">The number of surrounding bars whose low values should be more than the current bar's for the bar low to be marked as low pivot point</param>
        /// <param name="lastStoredValues">The number of last stored indicator values</param>
        /// <param name="strict">When true (default), uses strict inequalities (&gt; and &lt;). When false, uses relaxed inequalities (&gt;= and &lt;=) allowing equal values to be detected as pivot points.</param>
        public PivotPointsHighLow(string name, int surroundingBarsCountForHighPoint, int surroundingBarsCountForLowPoint, int lastStoredValues = 100, bool strict = true)
            : base(name)
        {
            _surroundingBarsCountForHighPoint = surroundingBarsCountForHighPoint;
            _surroundingBarsCountForLowPoint = surroundingBarsCountForLowPoint;
            _strict = strict;
            _windowHighs = new RollingWindow<IBaseDataBar>(2 * surroundingBarsCountForHighPoint + 1);
            _windowLows = new RollingWindow<IBaseDataBar>(2 * _surroundingBarsCountForLowPoint + 1);
            _windowPivotPoints = new RollingWindow<PivotPoint>(lastStoredValues);
            WarmUpPeriod = Math.Max(_windowHighs.Size, _windowLows.Size);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _windowHighs.Add(input);
            _windowLows.Add(input);

            PivotPoint highPoint = null, lowPoint = null;

            if (_windowHighs.IsReady)
            {
                highPoint = FindNextHighPivotPoint(_windowHighs, _surroundingBarsCountForHighPoint);
            }

            if (_windowLows.IsReady)
            {
                lowPoint = FindNextLowPivotPoint(_windowLows, _surroundingBarsCountForLowPoint);
            }

            OnNewPivotPointFormed(highPoint);
            OnNewPivotPointFormed(lowPoint);

            return ConvertToComputedValue(highPoint, lowPoint);
        }

        /// <summary>
        /// Looks for the next low pivot point.
        /// </summary>
        /// <param name="windowLows">rolling window that tracks the lows</param>
        /// <param name="midPointIndexOrSurroundingBarsCount">The midpoint index or surrounding bars count for lows</param>
        /// <returns>pivot point if found else null</returns>
        protected virtual PivotPoint FindNextLowPivotPoint(RollingWindow<IBaseDataBar> windowLows, int midPointIndexOrSurroundingBarsCount)
        {
            var isLow = true;
            var middlePoint = windowLows[midPointIndexOrSurroundingBarsCount];
            for (var k = 0; k < windowLows.Size && isLow; k++)
            {
                if (k == midPointIndexOrSurroundingBarsCount)
                {
                    continue;
                }

                isLow = _strict
                    ? windowLows[k].Low > middlePoint.Low
                    : windowLows[k].Low >= middlePoint.Low;
            }

            PivotPoint low = null;
            if (isLow)
            {
                low = new PivotPoint(PivotPointType.Low, middlePoint.Low, middlePoint.EndTime);
            }

            return low;
        }

        /// <summary>
        /// Looks for the next high pivot point.
        /// </summary>
        /// <param name="windowHighs">rolling window that tracks the highs</param>
        /// <param name="midPointIndexOrSurroundingBarsCount">The midpoint index or surrounding bars count for highs</param>
        /// <returns>pivot point if found else null</returns>
        protected virtual PivotPoint FindNextHighPivotPoint(RollingWindow<IBaseDataBar> windowHighs, int midPointIndexOrSurroundingBarsCount)
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
                isHigh = _strict
                    ? windowHighs[k].High < middlePoint.High
                    : windowHighs[k].High <= middlePoint.High;
            }

            PivotPoint high = null;
            if (isHigh)
            {
                high = new PivotPoint(PivotPointType.High, middlePoint.High, middlePoint.EndTime);
            }

            return high;
        }

        /// <summary>
        /// Method for converting high and low pivot points to a decimal value.
        /// </summary>
        /// <param name="highPoint">new high point or null</param>
        /// <param name="lowPoint">new low point or null</param>
        /// <returns>a decimal value representing the values of high and low pivot points</returns>
        protected virtual decimal ConvertToComputedValue(PivotPoint highPoint, PivotPoint lowPoint)
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

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _windowHighs.Reset();
            _windowLows.Reset();
            _windowPivotPoints.Reset();
            base.Reset();
        }

        /// <summary>
        /// Get current high pivot points, in the order such that first element in collection is the nearest to the present date
        /// </summary>
        /// <returns>An array of high pivot points.</returns>
        /// <remarks>Returned array can be empty if no points have been registered yet/</remarks>
        public PivotPoint[] GetHighPivotPointsArray()
        {
            return _windowPivotPoints.Where(p => p.PivotPointType == PivotPointType.High).ToArray();
        }

        /// <summary>
        /// Get current low pivot points, in the order such that first element in collection is the nearest to the present date
        /// </summary>
        /// <returns>An array of low pivot points.</returns>
        /// <remarks>Returned array can be empty if no points have been registered yet/</remarks>
        public PivotPoint[] GetLowPivotPointsArray()
        {
            return _windowPivotPoints.Where(p => p.PivotPointType == PivotPointType.Low).ToArray();
        }

        /// <summary>
        /// Get all pivot points, in the order such that first element in collection is the nearest to the present date
        /// </summary>
        /// <returns>An array of low and high pivot points. Ordered by time in descending order.</returns>
        /// <remarks>Returned array can be empty if no points have been registered yet/</remarks>
        public PivotPoint[] GetAllPivotPointsArray()
        {
            // Get all pivot points within rolling wind. collection as an array
            return _windowPivotPoints.ToArray();
        }

        /// <summary>
        /// Invokes NewPivotPointFormed event
        /// </summary>
        private void OnNewPivotPointFormed(PivotPoint pivotPoint)
        {
            if (pivotPoint != null)
            {
                _windowPivotPoints.Add(pivotPoint);
                NewPivotPointFormed?.Invoke(this, new PivotPointsEventArgs(pivotPoint));
            }
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
        /// Low pivot point (-1)
        /// </summary>
        Low = -1,

        /// <summary>
        /// No pivot point (0)
        /// </summary>
        None = 0,

        /// <summary>
        /// High pivot point (1)
        /// </summary>
        High = 1,

        /// <summary>
        /// Both high and low pivot point (2)
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
