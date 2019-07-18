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
using System.Globalization;
using QuantConnect.Data;
using Python.Runtime;
using QuantConnect.Util;

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
        /// <param name="waitForFirstToReady">True to only send updates to the second if first.IsReady returns true, false to alway send updates to second</param>
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
        public static CompositeIndicator<IndicatorDataPoint> WeightedBy<T, TWeight>(this IndicatorBase<T> value, TWeight weight, int period)
            where T : IBaseData
            where TWeight : IndicatorBase<IndicatorDataPoint>
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

            return numerator.Over(denominator);
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
        public static CompositeIndicator<T> Plus<T>(this IndicatorBase<T> left, decimal constant)
            where T : IBaseData
        {
            var constantIndicator = new ConstantIndicator<T>(constant.ToString(CultureInfo.InvariantCulture), constant);
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
        public static CompositeIndicator<T> Plus<T>(this IndicatorBase<T> left, IndicatorBase<T> right)
            where T : IBaseData
        {
            return new CompositeIndicator<T>(left, right, (l, r) => l + r);
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
        public static CompositeIndicator<T> Plus<T>(this IndicatorBase<T> left, IndicatorBase<T> right, string name)
            where T : IBaseData
        {
            return new CompositeIndicator<T>(name, left, right, (l, r) => l + r);
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
        public static CompositeIndicator<T> Minus<T>(this IndicatorBase<T> left, decimal constant)
            where T : IBaseData
        {
            var constantIndicator = new ConstantIndicator<T>(constant.ToString(CultureInfo.InvariantCulture), constant);
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
        public static CompositeIndicator<T> Minus<T>(this IndicatorBase<T> left, IndicatorBase<T> right)
            where T : IBaseData
        {
            return new CompositeIndicator<T>(left, right, (l, r) => l - r);
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
        public static CompositeIndicator<T> Minus<T>(this IndicatorBase<T> left, IndicatorBase<T> right, string name)
            where T : IBaseData
        {
            return new CompositeIndicator<T>(name, left, right, (l, r) => l - r);
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
        public static CompositeIndicator<T> Over<T>(this IndicatorBase<T> left, decimal constant)
            where T : IBaseData
        {
            var constantIndicator = new ConstantIndicator<T>(constant.ToString(CultureInfo.InvariantCulture), constant);
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
        public static CompositeIndicator<T> Over<T>(this IndicatorBase<T> left, IndicatorBase<T> right)
            where T : IBaseData
        {
            return new CompositeIndicator<T>(left, right, (l, r) => r == 0m ? new IndicatorResult(0m, IndicatorStatus.MathError) : new IndicatorResult(l / r));
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
        public static CompositeIndicator<T> Over<T>(this IndicatorBase<T> left, IndicatorBase<T> right, string name)
            where T : IBaseData
        {
            return new CompositeIndicator<T>(name, left, right, (l, r) => r == 0m ? new IndicatorResult(0m, IndicatorStatus.MathError) : new IndicatorResult(l / r));
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
        public static CompositeIndicator<T> Times<T>(this IndicatorBase<T> left, decimal constant)
            where T : IBaseData
        {
            var constantIndicator = new ConstantIndicator<T>(constant.ToString(CultureInfo.InvariantCulture), constant);
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
        public static CompositeIndicator<T> Times<T>(this IndicatorBase<T> left, IndicatorBase<T> right)
            where T : IBaseData
        {
            return new CompositeIndicator<T>(left, right, (l, r) => l * r);
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
        public static CompositeIndicator<T> Times<T>(this IndicatorBase<T> left, IndicatorBase<T> right, string name)
            where T : IBaseData
        {
            return new CompositeIndicator<T>(name, left, right, (l, r) => l * r);
        }

        /// <summary>Creates a new ExponentialMovingAverage indicator with the specified period and smoothingFactor from the left indicator
        /// </summary>
        /// <param name="left">The ExponentialMovingAverage indicator will be created using the data from left</param>
        /// <param name="period">The period of the ExponentialMovingAverage indicators</param>
        /// <param name="smoothingFactor">The percentage of data from the previous value to be carried into the next value</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to alway send updates</param>
        /// <returns>A reference to the ExponentialMovingAverage indicator to allow for method chaining</returns>
        public static ExponentialMovingAverage EMA<T>(this IndicatorBase<T> left, int period, decimal? smoothingFactor = null, bool waitForFirstToReady = true)
            where T : IBaseData
        {
            decimal k = smoothingFactor.HasValue ? k = smoothingFactor.Value : ExponentialMovingAverage.SmoothingFactorDefault(period);
            return new ExponentialMovingAverage($"EMA{period}_Of_{left.Name}", period, k).Of(left, waitForFirstToReady);
        }

        /// <summary>Creates a new Maximum indicator with the specified period from the left indicator
        /// </summary>
        /// <param name="left">The Maximum indicator will be created using the data from left</param>
        /// <param name="period">The period of the Maximum indicator</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to alway send updates</param>
        /// <returns>A reference to the Maximum indicator to allow for method chaining</returns>
        public static Maximum MAX(this IIndicator left, int period, bool waitForFirstToReady = true)
        {
            return new Maximum($"MAX{period}_Of_{left.Name}", period).Of(left, waitForFirstToReady);
        }

        /// <summary>Creates a new Minimum indicator with the specified period from the left indicator
        /// </summary>
        /// <param name="left">The Minimum indicator will be created using the data from left</param>
        /// <param name="period">The period of the Minimum indicator</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to alway send updates</param>
        /// <returns>A reference to the Minimum indicator to allow for method chaining</returns>
        public static Minimum MIN<T>(this IndicatorBase<T> left, int period, bool waitForFirstToReady = true)
            where T : IBaseData
        {
            return new Minimum($"MIN{period}_Of_{left.Name}", period).Of(left, waitForFirstToReady);
        }

        /// <summary>Initializes a new instance of the SimpleMovingAverage class with the specified name and period from the left indicator
        /// </summary>
        /// <param name="left">The SimpleMovingAverage indicator will be created using the data from left</param>
        /// <param name="period">The period of the SMA</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if first.IsReady returns true, false to alway send updates to second</param>
        /// <returns>The reference to the SimpleMovingAverage indicator to allow for method chaining</returns>
        public static SimpleMovingAverage SMA<T>(this IndicatorBase<T> left, int period, bool waitForFirstToReady = true)
            where T : IBaseData
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
        /// <param name="waitForFirstToReady">True to only send updates to the second if first.IsReady returns true, false to alway send updates to second</param>
        /// <returns>The reference to the second indicator to allow for method chaining</returns>
        public static object Of(PyObject second, PyObject first, bool waitForFirstToReady = true)
        {
            dynamic indicator1 = first.AsManagedObject((Type)first.GetPythonType().AsManagedObject(typeof(Type)));
            dynamic indicator2 = second.AsManagedObject((Type)second.GetPythonType().AsManagedObject(typeof(Type)));
            return Of(indicator2, indicator1, waitForFirstToReady);
        }

        /// <summary>
        /// Creates a new CompositeIndicator such that the result will be average of a first indicator weighted by a second one
        /// </summary>
        /// <param name="value">Indicator that will be averaged</param>
        /// <param name="weight">Indicator that provides the average weights</param>
        /// <param name="period">Average period</param>
        /// <returns>Indicator that results of the average of first by weights given by second</returns>
        public static CompositeIndicator<IndicatorDataPoint> WeightedBy(PyObject value, PyObject weight, int period)
        {
            dynamic indicator1 = value.AsManagedObject((Type)value.GetPythonType().AsManagedObject(typeof(Type)));
            dynamic indicator2 = weight.AsManagedObject((Type)weight.GetPythonType().AsManagedObject(typeof(Type)));
            return WeightedBy(indicator1, indicator2, period);
        }

        /// <summary>
        /// Creates a new ExponentialMovingAverage indicator with the specified period and smoothingFactor from the left indicator
        /// </summary>
        /// <param name="left">The ExponentialMovingAverage indicator will be created using the data from left</param>
        /// <param name="period">The period of the ExponentialMovingAverage indicators</param>
        /// <param name="smoothingFactor">The percentage of data from the previous value to be carried into the next value</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to alway send updates</param>
        /// <returns>A reference to the ExponentialMovingAverage indicator to allow for method chaining</returns>
        public static ExponentialMovingAverage EMA(PyObject left, int period, decimal? smoothingFactor = null, bool waitForFirstToReady = true)
        {
            dynamic indicator = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
            return EMA(indicator, period, smoothingFactor, waitForFirstToReady);
        }

        /// <summary>
        /// Creates a new Maximum indicator with the specified period from the left indicator
        /// </summary>
        /// <param name="left">The Maximum indicator will be created using the data from left</param>
        /// <param name="period">The period of the Maximum indicator</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to alway send updates</param>
        /// <returns>A reference to the Maximum indicator to allow for method chaining</returns>
        public static Maximum MAX(PyObject left, int period, bool waitForFirstToReady = true)
        {
            dynamic indicator = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
            return MAX(indicator, period, waitForFirstToReady);
        }

        /// <summary>
        /// Creates a new Minimum indicator with the specified period from the left indicator
        /// </summary>
        /// <param name="left">The Minimum indicator will be created using the data from left</param>
        /// <param name="period">The period of the Minimum indicator</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if left.IsReady returns true, false to alway send updates</param>
        /// <returns>A reference to the Minimum indicator to allow for method chaining</returns>
        public static Minimum MIN(PyObject left, int period, bool waitForFirstToReady = true)
        {
            dynamic indicator = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
            return MIN(indicator, period, waitForFirstToReady);
        }

        /// <summary>
        /// Initializes a new instance of the SimpleMovingAverage class with the specified name and period from the left indicator
        /// </summary>
        /// <param name="left">The SimpleMovingAverage indicator will be created using the data from left</param>
        /// <param name="period">The period of the SMA</param>
        /// <param name="waitForFirstToReady">True to only send updates to the second if first.IsReady returns true, false to alway send updates to second</param>
        /// <returns>The reference to the SimpleMovingAverage indicator to allow for method chaining</returns>
        public static SimpleMovingAverage SMA(PyObject left, int period, bool waitForFirstToReady = true)
        {
            dynamic indicator = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
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
        public static object Over(PyObject left, decimal constant)
        {
            dynamic indicatorLeft = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
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
        public static object Over(PyObject left, PyObject right, string name = "")
        {
            dynamic indicatorLeft = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
            dynamic indicatorRight = right.AsManagedObject((Type)right.GetPythonType().AsManagedObject(typeof(Type)));
            if (name.IsNullOrEmpty())
            {
                return Over(indicatorLeft, indicatorRight);
            }
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
        public static object Minus(PyObject left, decimal constant)
        {
            dynamic indicatorLeft = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
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
        public static object Minus(PyObject left, PyObject right, string name = "")
        {
            dynamic indicatorLeft = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
            dynamic indicatorRight = right.AsManagedObject((Type)right.GetPythonType().AsManagedObject(typeof(Type)));
            if (name.IsNullOrEmpty())
            {
                return Minus(indicatorLeft, indicatorRight);
            }
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
        public static object Times(PyObject left, decimal constant)
        {
            dynamic indicatorLeft = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
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
        public static object Times(PyObject left, PyObject right, string name = "")
        {
            dynamic indicatorLeft = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
            dynamic indicatorRight = right.AsManagedObject((Type)right.GetPythonType().AsManagedObject(typeof(Type)));
            if (name.IsNullOrEmpty())
            {
                return Times(indicatorLeft, indicatorRight);
            }
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
        public static object Plus(PyObject left, decimal constant)
        {
            dynamic indicatorLeft = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
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
        public static object Plus(PyObject left, PyObject right, string name = "")
        {
            dynamic indicatorLeft = left.AsManagedObject((Type)left.GetPythonType().AsManagedObject(typeof(Type)));
            dynamic indicatorRight = right.AsManagedObject((Type)right.GetPythonType().AsManagedObject(typeof(Type)));
            if (name.IsNullOrEmpty())
            {
                return Plus(indicatorLeft, indicatorRight);
            }
            return Plus(indicatorLeft, indicatorRight, name);
        }
    }
}