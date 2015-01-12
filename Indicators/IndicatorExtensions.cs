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
        public static void Update(this IndicatorBase<IndicatorDataPoint> indicator, DateTime time, decimal value)
        {
            indicator.Update(new IndicatorDataPoint(time, value));
        }

        /// <summary>
        /// Creates a new SequentialIndicator such that data from 'first' is piped into 'second'
        /// </summary>
        /// <param name="second">The indicator that wraps the first</param>
        /// <param name="first">The indicator to be wrapped</param>
        /// <returns>A new SequentialIndicator that pipes data from first to second</returns>
        public static SequentialIndicator<T> Of<T>(this IndicatorBase<IndicatorDataPoint> second, IndicatorBase<T> first) 
            where T : BaseData
        {
            return new SequentialIndicator<T>(first, second);
        }

        /// <summary>
        /// Creates a new SequentialIndicator such that data from 'first' is piped into 'second'
        /// </summary>
        /// <param name="second">The indicator that wraps the first</param>
        /// <param name="first">The indicator to be wrapped</param>
        /// <param name="name">The name of the new indicator</param>
        /// <returns>A new SequentialIndicator that pipes data from first to second</returns>
        public static SequentialIndicator<T> Of<T>(this IndicatorBase<IndicatorDataPoint> second, IndicatorBase<T> first, string name)
            where T : BaseData
        {
            return new SequentialIndicator<T>(first, second);
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
