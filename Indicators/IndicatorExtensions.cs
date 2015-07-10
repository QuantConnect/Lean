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
        public static TSecond Of<T, TSecond>(this TSecond second, IndicatorBase<T> first, bool waitForFirstToReady = true)
            where T : BaseData
            where TSecond : IndicatorBase<IndicatorDataPoint>
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
        /// Creates a new CompositeIndicator such that the result will be the sum of the left and the constant
        /// </summary>
        /// <remarks>
        /// value = left + constant
        /// </remarks>
        /// <param name="left">The left indicator</param>
        /// <param name="constant">The addend</param>
        /// <returns>The sum of the left and right indicators</returns>
        public static CompositeIndicator<IndicatorDataPoint> Plus(this IndicatorBase<IndicatorDataPoint> left, decimal constant)
        {
            var constantIndicator = new ConstantIndicator<IndicatorDataPoint>(constant.ToString(CultureInfo.InvariantCulture), constant);
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
        public static CompositeIndicator<IndicatorDataPoint> Plus(this IndicatorBase<IndicatorDataPoint> left, IndicatorBase<IndicatorDataPoint> right)
        {
            return new CompositeIndicator<IndicatorDataPoint>(left, right, (l, r) => l + r);
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
        public static CompositeIndicator<IndicatorDataPoint> Plus(this IndicatorBase<IndicatorDataPoint> left, IndicatorBase<IndicatorDataPoint> right, string name)
        {
            return new CompositeIndicator<IndicatorDataPoint>(name, left, right, (l, r) => l + r);
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
        public static CompositeIndicator<IndicatorDataPoint> Minus(this IndicatorBase<IndicatorDataPoint> left, decimal constant)
        {
            var constantIndicator = new ConstantIndicator<IndicatorDataPoint>(constant.ToString(CultureInfo.InvariantCulture), constant);
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
        public static CompositeIndicator<IndicatorDataPoint> Minus(this IndicatorBase<IndicatorDataPoint> left, IndicatorBase<IndicatorDataPoint> right)
        {
            return new CompositeIndicator<IndicatorDataPoint>(left, right, (l, r) => l - r);
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
        public static CompositeIndicator<IndicatorDataPoint> Minus(this IndicatorBase<IndicatorDataPoint> left, IndicatorBase<IndicatorDataPoint> right, string name)
        {
            return new CompositeIndicator<IndicatorDataPoint>(name, left, right, (l, r) => l - r);
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
        public static CompositeIndicator<IndicatorDataPoint> Over(this IndicatorBase<IndicatorDataPoint> left, decimal constant)
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
        public static CompositeIndicator<IndicatorDataPoint> Over(this IndicatorBase<IndicatorDataPoint> left, IndicatorBase<IndicatorDataPoint> right)
        {
            return new CompositeIndicator<IndicatorDataPoint>(left, right, (l, r) => l / r);
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
        public static CompositeIndicator<IndicatorDataPoint> Over(this IndicatorBase<IndicatorDataPoint> left, IndicatorBase<IndicatorDataPoint> right, string name)
        {
            return new CompositeIndicator<IndicatorDataPoint>(name, left, right, (l, r) => l / r);
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
        public static CompositeIndicator<IndicatorDataPoint> Times(this IndicatorBase<IndicatorDataPoint> left, decimal constant)
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
        public static CompositeIndicator<IndicatorDataPoint> Times(this IndicatorBase<IndicatorDataPoint> left, IndicatorBase<IndicatorDataPoint> right)
        {
            return new CompositeIndicator<IndicatorDataPoint>(left, right, (l, r) => l * r);
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
        public static CompositeIndicator<IndicatorDataPoint> Times(this IndicatorBase<IndicatorDataPoint> left, IndicatorBase<IndicatorDataPoint> right, string name)
        {
            return new CompositeIndicator<IndicatorDataPoint>(name, left, right, (l, r) => l * r);
        }
    }
}
