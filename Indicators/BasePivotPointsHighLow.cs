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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using System;
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Base class for creating PivotPointHighLow indicators
    /// </summary>
    public abstract class BasePivotPointsHighLow : IndicatorBase<IBaseDataBar>, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _surroundingBarsCountForHighPoint;
        private readonly int _surroundingBarsCountForLowPoint;
        private readonly RollingWindow<IBaseDataBar> _windowHighs;
        private readonly RollingWindow<IBaseDataBar> _windowLows;
        // Stores information of that last N pivot points
        private readonly RollingWindow<PivotPoint> _windowPivotPoints;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _windowHighs.IsReady && _windowLows.IsReady;

        /// <summary>
        /// Event informs of new pivot point formed with new data update
        /// </summary>
        public event EventHandler<PivotPointsEventArgs> NewPivotPointFormed;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; protected set; }

        /// <summary>
        /// Base class for creating pivot point indicators
        /// </summary>
        /// <param name="name">The name of an indicator</param>
        /// <param name="surroundingBarsCountForHighPoint">The number of surrounding bars whose high values should be less than the current bar's for the bar high to be marked as high pivot point</param>
        /// <param name="surroundingBarsCountForLowPoint">The number of surrounding bars whose low values should be more than the current bar's for the bar low to be marked as low pivot point</param>
        /// <param name="lastStoredValues">The number of last stored indicator values</param>
        protected BasePivotPointsHighLow(string name, int surroundingBarsCountForHighPoint, int surroundingBarsCountForLowPoint, int lastStoredValues = 100) 
            : base(name)
        {
            _surroundingBarsCountForHighPoint = surroundingBarsCountForHighPoint;
            _surroundingBarsCountForLowPoint = surroundingBarsCountForLowPoint;
            _windowHighs = new RollingWindow<IBaseDataBar>(2 * surroundingBarsCountForHighPoint + 1);
            _windowLows = new RollingWindow<IBaseDataBar>(2 * _surroundingBarsCountForLowPoint + 1);
            _windowPivotPoints = new RollingWindow<PivotPoint>(lastStoredValues);
            WarmUpPeriod = Math.Max(_windowHighs.Size, _windowLows.Size);
        }

        /// <inheritdoc/>
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
        protected abstract PivotPoint FindNextLowPivotPoint(RollingWindow<IBaseDataBar> windowLows, int midPointIndexOrSurroundingBarsCount);

        /// <summary>
        /// Looks for the next high pivot point.
        /// </summary>
        /// <param name="windowHighs">rolling window that tracks the highs</param>
        /// <param name="midPointIndexOrSurroundingBarsCount">The midpoint index or surrounding bars count for highs</param>
        /// <returns>pivot point if found else null</returns>
        protected abstract PivotPoint FindNextHighPivotPoint(RollingWindow<IBaseDataBar> windowHighs, int midPointIndexOrSurroundingBarsCount);

        /// <summary>
        /// Method for converting high and low pivot points to a decimal value.
        /// </summary>
        /// <param name="highPoint">new high point or null</param>
        /// <param name="lowPoint">new low point or null</param>
        /// <returns>a decimal value representing the values of high and low pivot points</returns>
        protected abstract decimal ConvertToComputedValue(PivotPoint highPoint, PivotPoint lowPoint);

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
    /// Event arguments class for the <see cref="BasePivotPointsHighLow.NewPivotPointFormed"/> event
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
