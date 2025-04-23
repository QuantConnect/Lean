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

// ReSharper disable InconsistentNaming
using System;
using Python.Runtime;
using QuantConnect.Data;
using System.Globalization;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Provides extension methods for Indicator
    /// </summary>
    public static class IndicatorExtensions
    {
        /// <summary>
        /// Updates the state of this indicator with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="indicator">The indicator to be updated</param>
        /// <param name="time">The time associated with the value</param>
        /// <param name="value">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        public static bool Update(this IndicatorBase<IndicatorDataPoint> indicator, DateTime time, decimal value)
        {
            return indicator.Update(new IndicatorDataPoint(time, value));
        }

        /// <summary>
        /// Configures the second indicator to receive automatic updates from the first by attaching an event handler
        /// to first.DataConsolidated
        /// </summary>
        /// <param name="second">The indicator that receives data from the first</param>
        /// <param name="first">The indicator that sends data via DataConsolidated even to the second</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if first.IsReady returns true, false to always send updates to second</param>
        /// <returns>The reference to the second indicator to allow for method chaining</returns>
        public static T Of<T>(this T second, IIndicator first, bool waitForFirstToReady = true)
            where T : IIndicator
        {
            first.Updated += (sender, consolidated) =>
            {
                // only send the data along if we're ready
                if (!waitForFirstToReady || first.IsReady)
                {
                    second.Update(consolidated);
                }
            };

            return second;
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be average of a first indicator weighted by a second one
        /// </summary>
        /// <param name="value">Indicator that will be averaged</param>
        /// <param name="weight">Indicator that provides the average weights</param>
        /// <param name="period">Average period</param>
        /// <returns>Indicator that results of the average of first by weights given by second</returns>
        public static CompositeIndicator WeightedBy(this IndicatorBase value, IndicatorBase weight, int period)
        {
            var x = new WindowIdentity(period);
            var y = new WindowIdentity(period);
            var numerator = new Sum("Sum_xy", period);
            var denominator = new Sum("Sum_y", period);

            value.Updated += (sender, consolidated) =>
            {
                x.Update(consolidated);
                if (x.Samples == y.Samples)
                {
                    numerator.Update(consolidated.Time, consolidated.Value * y.Current.Value);
                }
            };

            weight.Updated += (sender, consolidated) =>
            {
                y.Update(consolidated);
                if (x.Samples == y.Samples)
                {
                    numerator.Update(consolidated.Time, consolidated.Value * x.Current.Value);
                }
                denominator.Update(consolidated);
            };

            var resetCompositeIndicator = new ResetCompositeIndicator(numerator, denominator, GetOverIndicatorComposer(), () =>
            {
                x.Reset();
                y.Reset();
            });

            return resetCompositeIndicator;
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the sum of the left and the constant
        /// </summary>
        /// <remarks>
        /// value = left + constant
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="constant">The addend</param>
        /// <returns>The sum of the left and right indicators</returns>
        public static CompositeIndicator Plus(this IndicatorBase left, decimal constant)
        {
            var constantIndicator = new ConstantIndicator<IBaseData>(constant.ToString(CultureInfo.InvariantCulture), constant);
            return left.Plus(constantIndicator);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the sum of the left and right
        /// </summary>
        /// <remarks>
        /// value = left + right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <returns>The sum of the left and right indicators</returns>
        public static CompositeIndicator Plus(this IndicatorBase left, IndicatorBase right)
        {
            return new(left, right, (l, r) => l.Current.Value + r.Current.Value);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the sum of the left and right
        /// </summary>
        /// <remarks>
        /// value = left + right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <param name="name">The name of this indicator</param>
        /// <returns>The sum of the left and right indicators</returns>
        public static CompositeIndicator Plus(this IndicatorBase left, IndicatorBase right, string name)
        {
            return new(name, left, right, (l, r) => l.Current.Value + r.Current.Value);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the difference of the left and constant
        /// </summary>
        /// <remarks>
        /// value = left - constant
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="constant">The subtrahend</param>
        /// <returns>The difference of the left and right indicators</returns>
        public static CompositeIndicator Minus(this IndicatorBase left, decimal constant)
        {
            var constantIndicator = new ConstantIndicator<IBaseData>(constant.ToString(CultureInfo.InvariantCulture), constant);
            return left.Minus(constantIndicator);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the difference of the left and right
        /// </summary>
        /// <remarks>
        /// value = left - right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <returns>The difference of the left and right indicators</returns>
        public static CompositeIndicator Minus(this IndicatorBase left, IndicatorBase right)
        {
            return new(left, right, (l, r) => l.Current.Value - r.Current.Value);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the difference of the left and right
        /// </summary>
        /// <remarks>
        /// value = left - right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <param name="name">The name of this indicator</param>
        /// <returns>The difference of the left and right indicators</returns>
        public static CompositeIndicator Minus(this IndicatorBase left, IndicatorBase right, string name)
        {
            return new(name, left, right, (l, r) => l.Current.Value - r.Current.Value);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the ratio of the left to the constant
        /// </summary>
        /// <remarks>
        /// value = left/constant
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="constant">The constant value denominator</param>
        /// <returns>The ratio of the left to the right indicator</returns>
        public static CompositeIndicator Over(this IndicatorBase left, decimal constant)
        {
            var constantIndicator = new ConstantIndicator<IndicatorDataPoint>(constant.ToString(CultureInfo.InvariantCulture), constant);
            return left.Over(constantIndicator);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the ratio of the left to the right
        /// </summary>
        /// <remarks>
        /// value = left/right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <returns>The ratio of the left to the right indicator</returns>
        public static CompositeIndicator Over(this IndicatorBase left, IndicatorBase right)
        {
            return new(left, right, GetOverIndicatorComposer());
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the ratio of the left to the right
        /// </summary>
        /// <remarks>
        /// value = left/right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <param name="name">The name of this indicator</param>
        /// <returns>The ratio of the left to the right indicator</returns>
        public static CompositeIndicator Over(this IndicatorBase left, IndicatorBase right, string name)
        {
            return new(name, left, right, GetOverIndicatorComposer());
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the product of the left and the constant
        /// </summary>
        /// <remarks>
        /// value = left*constant
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="constant">The constant value to multiple by</param>
        /// <returns>The product of the left to the right indicators</returns>
        public static CompositeIndicator Times(this IndicatorBase left, decimal constant)
        {
            var constantIndicator = new ConstantIndicator<IndicatorDataPoint>(constant.ToString(CultureInfo.InvariantCulture), constant);
            return left.Times(constantIndicator);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the product of the left to the right
        /// </summary>
        /// <remarks>
        /// value = left*right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <returns>The product of the left to the right indicators</returns>
        public static CompositeIndicator Times(this IndicatorBase left, IndicatorBase right)
        {
            return new(left, right, (l, r) => l.Current.Value * r.Current.Value);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the product of the left to the right
        /// </summary>
        /// <remarks>
        /// value = left*right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <param name="name">The name of this indicator</param>
        /// <returns>The product of the left to the right indicators</returns>
        public static CompositeIndicator Times(this IndicatorBase left, IndicatorBase right, string name)
        {
            return new(name, left, right, (l, r) => l.Current.Value * r.Current.Value);
        }

        /// <summary>Creates a new ExponentialMovingAverage indicator with the specified period and smoothingFactor from the left indicator
        /// </summary>
        /// <param name="left">The ExponentialMovingAverage indicator will be created using the data from left</param>
        /// <param name="period">The period of the ExponentialMovingAverage indicators</param>
        /// <param name="smoothingFactor">The percentage of data from the previous value to be carried into the next value</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to always send updates</param>
        /// <returns>A reference to the ExponentialMovingAverage indicator to allow for method chaining</returns>
        public static ExponentialMovingAverage EMA(this IndicatorBase left, int period, decimal? smoothingFactor = null, bool waitForFirstToReady = true)
        {
            var k = smoothingFactor ?? ExponentialMovingAverage.SmoothingFactorDefault(period);
            return new ExponentialMovingAverage($"EMA{period}_Of_{left.Name}", period, k).Of(left, waitForFirstToReady);
        }

        /// <summary>Creates a new Maximum indicator with the specified period from the left indicator
        /// </summary>
        /// <param name="left">The Maximum indicator will be created using the data from left</param>
        /// <param name="period">The period of the Maximum indicator</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to always send updates</param>
        /// <returns>A reference to the Maximum indicator to allow for method chaining</returns>
        public static Maximum MAX(this IIndicator left, int period, bool waitForFirstToReady = true)
        {
            return new Maximum($"MAX{period}_Of_{left.Name}", period).Of(left, waitForFirstToReady);
        }

        /// <summary>Creates a new Minimum indicator with the specified period from the left indicator
        /// </summary>
        /// <param name="left">The Minimum indicator will be created using the data from left</param>
        /// <param name="period">The period of the Minimum indicator</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to always send updates</param>
        /// <returns>A reference to the Minimum indicator to allow for method chaining</returns>
        public static Minimum MIN(this IndicatorBase left, int period, bool waitForFirstToReady = true)
        {
            return new Minimum($"MIN{period}_Of_{left.Name}", period).Of(left, waitForFirstToReady);
        }

        /// <summary>Initializes a new instance of the SimpleMovingAverage class with the specified name and period from the left indicator
        /// </summary>
        /// <param name="left">The SimpleMovingAverage indicator will be created using the data from left</param>
        /// <param name="period">The period of the SMA</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if first.IsReady returns true, false to always send updates to second</param>
        /// <returns>The reference to the SimpleMovingAverage indicator to allow for method chaining</returns>
        public static SimpleMovingAverage SMA(this IndicatorBase left, int period, bool waitForFirstToReady = true)
        {
            return new SimpleMovingAverage($"SMA{period}_Of_{left.Name}", period).Of(left, waitForFirstToReady);
        }



        /// The methods overloads bellow are due to python.net not being able to correctly solve generic methods overload

        /// <summary>
        /// Configures the second indicator to receive automatic updates from the first by attaching an event handler
        /// to first.DataConsolidated
        /// </summary>
        /// <param name="second">The indicator that receives data from the first</param>
        /// <param name="first">The indicator that sends data via DataConsolidated even to the second</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if first.IsReady returns true, false to always send updates to second</param>
        /// <returns>The reference to the second indicator to allow for method chaining</returns>
        public static IndicatorBase Of(PyObject second, PyObject first, bool waitForFirstToReady = true)
        {
            var indicator1 = GetIndicatorAsManagedObject(first);
            var indicator2 = GetIndicatorAsManagedObject(second);
            return Of(indicator2, indicator1, waitForFirstToReady);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be average of a first indicator weighted by a second one
        /// </summary>
        /// <param name="value">Indicator that will be averaged</param>
        /// <param name="weight">Indicator that provides the average weights</param>
        /// <param name="period">Average period</param>
        /// <returns>Indicator that results of the average of first by weights given by second</returns>
        // ReSharper disable once UnusedMember.Global
        public static CompositeIndicator WeightedBy(PyObject value, PyObject weight, int period)
        {
            var indicator1 = GetIndicatorAsManagedObject(value);
            var indicator2 = GetIndicatorAsManagedObject(weight);
            return WeightedBy(indicator1, indicator2, period);
        }

        /// <summary>
        /// Creates a new ExponentialMovingAverage indicator with the specified period and smoothingFactor from the left indicator
        /// </summary>
        /// <param name="left">The ExponentialMovingAverage indicator will be created using the data from left</param>
        /// <param name="period">The period of the ExponentialMovingAverage indicators</param>
        /// <param name="smoothingFactor">The percentage of data from the previous value to be carried into the next value</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to always send updates</param>
        /// <returns>A reference to the ExponentialMovingAverage indicator to allow for method chaining</returns>
        public static ExponentialMovingAverage EMA(PyObject left, int period, decimal? smoothingFactor = null, bool waitForFirstToReady = true)
        {
            var indicator = GetIndicatorAsManagedObject(left);
            return EMA(indicator, period, smoothingFactor, waitForFirstToReady);
        }

        /// <summary>
        /// Creates a new Maximum indicator with the specified period from the left indicator
        /// </summary>
        /// <param name="left">The Maximum indicator will be created using the data from left</param>
        /// <param name="period">The period of the Maximum indicator</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to always send updates</param>
        /// <returns>A reference to the Maximum indicator to allow for method chaining</returns>
        public static Maximum MAX(PyObject left, int period, bool waitForFirstToReady = true)
        {
            var indicator = GetIndicatorAsManagedObject(left);
            return MAX(indicator, period, waitForFirstToReady);
        }

        /// <summary>
        /// Creates a new Minimum indicator with the specified period from the left indicator
        /// </summary>
        /// <param name="left">The Minimum indicator will be created using the data from left</param>
        /// <param name="period">The period of the Minimum indicator</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to always send updates</param>
        /// <returns>A reference to the Minimum indicator to allow for method chaining</returns>
        public static Minimum MIN(PyObject left, int period, bool waitForFirstToReady = true)
        {
            var indicator = GetIndicatorAsManagedObject(left);
            return MIN(indicator, period, waitForFirstToReady);
        }

        /// <summary>
        /// Initializes a new instance of the SimpleMovingAverage class with the specified name and period from the left indicator
        /// </summary>
        /// <param name="left">The SimpleMovingAverage indicator will be created using the data from left</param>
        /// <param name="period">The period of the SMA</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if first.IsReady returns true, false to always send updates to second</param>
        /// <returns>The reference to the SimpleMovingAverage indicator to allow for method chaining</returns>
        public static SimpleMovingAverage SMA(PyObject left, int period, bool waitForFirstToReady = true)
        {
            var indicator = GetIndicatorAsManagedObject(left);
            return SMA(indicator, period, waitForFirstToReady);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the ratio of the left to the constant
        /// </summary>
        /// <remarks>
        /// value = left/constant
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="constant">The constant value denominator</param>
        /// <returns>The ratio of the left to the right indicator</returns>
        public static CompositeIndicator Over(PyObject left, decimal constant)
        {
            var indicatorLeft = GetIndicatorAsManagedObject(left);
            return Over(indicatorLeft, constant);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the ratio of the left to the right
        /// </summary>
        /// <remarks>
        /// value = left/right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <param name="name">The name of this indicator</param>
        /// <returns>The ratio of the left to the right indicator</returns>
        public static CompositeIndicator Over(PyObject left, PyObject right, string name = null)
        {
            var indicatorLeft = GetIndicatorAsManagedObject(left);
            var indicatorRight = GetIndicatorAsManagedObject(right);
            return Over(indicatorLeft, indicatorRight, name);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the difference of the left and constant
        /// </summary>
        /// <remarks>
        /// value = left - constant
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="constant">The subtrahend</param>
        /// <returns>The difference of the left and right indicators</returns>
        public static CompositeIndicator Minus(PyObject left, decimal constant)
        {
            var indicatorLeft = GetIndicatorAsManagedObject(left);
            return Minus(indicatorLeft, constant);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the difference of the left and right
        /// </summary>
        /// <remarks>
        /// value = left - right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <param name="name">The name of this indicator</param>
        /// <returns>The difference of the left and right indicators</returns>
        public static CompositeIndicator Minus(PyObject left, PyObject right, string name = null)
        {
            var indicatorLeft = GetIndicatorAsManagedObject(left);
            var indicatorRight = GetIndicatorAsManagedObject(right);
            return Minus(indicatorLeft, indicatorRight, name);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the product of the left and the constant
        /// </summary>
        /// <remarks>
        /// value = left*constant
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="constant">The constant value to multiple by</param>
        /// <returns>The product of the left to the right indicators</returns>
        public static CompositeIndicator Times(PyObject left, decimal constant)
        {
            var indicatorLeft = GetIndicatorAsManagedObject(left);
            return Times(indicatorLeft, constant);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the product of the left to the right
        /// </summary>
        /// <remarks>
        /// value = left*right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <param name="name">The name of this indicator</param>
        /// <returns>The product of the left to the right indicators</returns>
        public static CompositeIndicator Times(PyObject left, PyObject right, string name = null)
        {
            var indicatorLeft = GetIndicatorAsManagedObject(left);
            var indicatorRight = GetIndicatorAsManagedObject(right);
            return Times(indicatorLeft, indicatorRight, name);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the sum of the left and the constant
        /// </summary>
        /// <remarks>
        /// value = left + constant
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="constant">The addend</param>
        /// <returns>The sum of the left and right indicators</returns>
        public static CompositeIndicator Plus(PyObject left, decimal constant)
        {
            var indicatorLeft = GetIndicatorAsManagedObject(left);
            return Plus(indicatorLeft, constant);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be the sum of the left and right
        /// </summary>
        /// <remarks>
        /// value = left + right
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="right">The right indicator</param>
        /// <param name="name">The name of this indicator</param>
        /// <returns>The sum of the left and right indicators</returns>
        public static CompositeIndicator Plus(PyObject left, PyObject right, string name = null)
        {
            var indicatorLeft = GetIndicatorAsManagedObject(left);
            var indicatorRight = GetIndicatorAsManagedObject(right);
            return Plus(indicatorLeft, indicatorRight, name);
        }

        internal static IndicatorBase GetIndicatorAsManagedObject(this PyObject indicator)
        {
            if (indicator.TryConvert(out PythonIndicator pythonIndicator, true))
            {
                pythonIndicator.SetIndicator(indicator);
                return pythonIndicator;
            }

            return indicator.SafeAsManagedObject();
        }

        /// <summary>
        /// Gets the IndicatorComposer for a CompositeIndicator whose result is the ratio of the left to the right
        /// </summary>
        /// <returns>The IndicatorComposer for a CompositeIndicator whose result is the ratio of the left to the right</returns>
        private static CompositeIndicator.IndicatorComposer GetOverIndicatorComposer()
        {
            return (l, r) => r.Current.Value == 0m ? new IndicatorResult(0m, IndicatorStatus.MathError) : new IndicatorResult(l.Current.Value / r.Current.Value);
        }
    }
}
