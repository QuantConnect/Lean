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

using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Carrier of an indicator values
    /// </summary>
    public class IndicatorValues : IEnumerable<IndicatorDataPoint>
    {
        /// <summary>
        /// The name of the values associated to this dto
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The indicator values
        /// </summary>
        public List<IndicatorDataPoint> Values { get; }

        /// <summary>
        /// The target indicator
        /// </summary>
        protected IndicatorBase Indicator { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public IndicatorValues(IndicatorBase indicator, string name)
        {
            Name = name;
            Values = new();
            Indicator = indicator;
        }

        /// <summary>
        /// Update with a new indicator point
        /// </summary>
        public virtual void UpdateValue(IndicatorDataPoint point)
        {
            Values.Add(point);
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public static IndicatorValues Create(IndicatorBase indicator, string name)
        {
            return new IndicatorValues(indicator, name);
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public static IndicatorValues Create(IndicatorBase indicator, PropertyInfo propertyInfo)
        {
            return new IndicatorPropertyValues(indicator, propertyInfo);
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"{Name} {Values.Count} indicator values";
        }

        public IEnumerator<IndicatorDataPoint> GetEnumerator()
        {
            return ((IEnumerable<IndicatorDataPoint>)Values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Values).GetEnumerator();
        }

        private class IndicatorPropertyValues : IndicatorValues
        {
            private readonly PropertyInfo _currentInfo;
            private readonly PropertyInfo _propertyInfo;
            public IndicatorPropertyValues(IndicatorBase indicator, PropertyInfo propertyInfo) : base(indicator, propertyInfo.Name)
            {
                _propertyInfo = propertyInfo;
                _currentInfo = _propertyInfo.PropertyType.GetProperty("Current");
            }
            public override void UpdateValue(IndicatorDataPoint _)
            {
                var value = _propertyInfo.GetValue(Indicator);
                if (_currentInfo != null)
                {
                    value = _currentInfo.GetValue(value);
                }
                Values.Add(value as IndicatorDataPoint);
            }
        }
    }
}
