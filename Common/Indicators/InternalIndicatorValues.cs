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

using System.Linq;
using System.Reflection;
using QuantConnect.Data;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Collection of indicator data points for a given time
    /// </summary>
    public class IndicatorDataPoints : DynamicData
    {
        /// <summary>
        /// The indicator value at a given point
        /// </summary>
        public IndicatorDataPoint Current => (IndicatorDataPoint)GetProperty("Current");

        /// <summary>
        /// The indicator value at a given point
        /// </summary>
        public override decimal Value => Current.Value;

        /// <summary>
        /// Access the historical indicator values per indicator property name
        /// </summary>
        public IndicatorDataPoint this[string name]
        {
            get
            {
                return GetProperty(name) as IndicatorDataPoint;
            }
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"{EndTime} {string.Join(", ", GetStorageDictionary().OrderBy(x => x.Key).Select(x => $"{x.Key}: {HandleObjectStorage(x.Value)}"))}";
        }

        /// <summary>
        /// Returns the current data value held within the instance
        /// </summary>
        /// <param name="instance">The DataPoint instance</param>
        /// <returns>The current data value held within the instance</returns>
        public static implicit operator decimal(IndicatorDataPoints instance)
        {
            return instance.Value;
        }

        private static string HandleObjectStorage(object storedObject)
        {
            if (storedObject is IndicatorDataPoint point)
            {
                return point.Value.SmartRounding().ToStringInvariant();
            }
            return storedObject?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Internal carrier of an indicator values by property name
    /// </summary>
    public class InternalIndicatorValues : IEnumerable<IndicatorDataPoint>
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
        protected IIndicator Indicator { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public InternalIndicatorValues(IIndicator indicator, string name)
        {
            Name = name;
            Values = new();
            Indicator = indicator;
        }

        /// <summary>
        /// Update with a new indicator point
        /// </summary>
        public virtual IndicatorDataPoint UpdateValue()
        {
            Values.Add(Indicator.Current);
            return Indicator.Current;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public static InternalIndicatorValues Create(IIndicator indicator, string name)
        {
            return new InternalIndicatorValues(indicator, name);
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public static InternalIndicatorValues Create(IIndicator indicator, PropertyInfo propertyInfo)
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

        /// <summary>
        /// Returns an enumerator for the indicator values
        /// </summary>
        public IEnumerator<IndicatorDataPoint> GetEnumerator()
        {
            return ((IEnumerable<IndicatorDataPoint>)Values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Values).GetEnumerator();
        }

        private class IndicatorPropertyValues : InternalIndicatorValues
        {
            private readonly PropertyInfo _currentInfo;
            private readonly PropertyInfo _propertyInfo;
            public IndicatorPropertyValues(IIndicator indicator, PropertyInfo propertyInfo) : base(indicator, propertyInfo.Name)
            {
                _propertyInfo = propertyInfo;
                _currentInfo = _propertyInfo.PropertyType.GetProperty("Current");
            }
            public override IndicatorDataPoint UpdateValue()
            {
                var value = _propertyInfo.GetValue(Indicator);
                if (value == null)
                {
                    return null;
                }

                if (_currentInfo != null)
                {
                    value = _currentInfo.GetValue(value);
                }
                var point = value as IndicatorDataPoint;

                if (Values.Count == 0 || point.EndTime != Values[^1].EndTime)
                {
                    // If the list is empty or the new point has a different EndTime, add it to the list
                    Values.Add(point);
                }
                else
                {
                    // If the new point has the same EndTime as the last point, update the last point
                    Values[^1] = point;
                }

                return point;
            }
        }
    }
}
