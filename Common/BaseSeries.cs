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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Chart Series Object - Series data and properties for a chart:
    /// </summary>
    [JsonConverter(typeof(SeriesJsonConverter))]
    public abstract class BaseSeries
    {
        /// The index of the last fetch update request to only retrieve the "delta" of the previous request.
        private int _updatePosition;

        /// <summary>
        /// Name of the series.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Axis for the chart series.
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Index/position of the series on the chart.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Axis name for the chart series.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string IndexName { get; set; }

        /// <summary>
        /// Defines the visual Z index of the series on the chart.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ZIndex { get; set; }

        /// <summary>
        /// Chart type for the series:
        /// </summary>
        public SeriesType SeriesType { get; set; }

        /// <summary>
        /// An optional tooltip template
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Tooltip { get; set; }

        /// <summary>
        /// The series list of values.
        /// These values are assumed to be in ascending time order (first points earliest, last points latest)
        /// </summary>
        public List<ISeriesPoint> Values { get; set; }

        /// <summary>
        /// Default constructor for chart series
        /// </summary>
        protected BaseSeries()
        {
            Unit = "$";
            Values = new List<ISeriesPoint>();
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        /// <param name="type">Type of the series</param>
        protected BaseSeries(string name, SeriesType type)
            : this()
        {
            Name = name;
            SeriesType = type;
        }

        /// <summary>
        /// Foundational constructor on the series class
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="type">Type of the series</param>
        /// <param name="index">Series index position on the chart</param>
        protected BaseSeries(string name, SeriesType type, int index)
            : this(name, type)
        {
            Index = index;
        }

        /// <summary>
        /// Foundational constructor on the series class
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="type">Type of the series</param>
        /// <param name="index">Series index position on the chart</param>
        /// <param name="unit">Unit for the series axis</param>
        protected BaseSeries(string name, SeriesType type, int index, string unit)
            : this(name, type, index)
        {
            Unit = unit;
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        /// <param name="type">Type of the chart series</param>
        /// <param name="unit">Unit of the series</param>
        protected BaseSeries(string name, SeriesType type, string unit)
            : this(name, type, 0, unit)
        {
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        /// <param name="point">The data point to add</param>
        public virtual void AddPoint(ISeriesPoint point)
        {
            if (Values.Count > 0 && Values[Values.Count - 1].Time == point.Time)
            {
                // duplicate points at the same time, overwrite the value
                Values[Values.Count - 1] = point;
            }
            else
            {
                Values.Add(point);
            }
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        /// <param name="time">The time of the data point</param>
        /// <param name="values">The values of the data point</param>
        public abstract void AddPoint(DateTime time, List<decimal> values);

        /// <summary>
        /// Get the updates since the last call to this function.
        /// </summary>
        /// <returns>List of the updates from the series</returns>
        public BaseSeries GetUpdates()
        {
            var copy = Clone(empty: true);

            try
            {
                //Add the updates since the last
                for (var i = _updatePosition; i < Values.Count; i++)
                {
                    copy.Values.Add(Values[i]);
                }
                //Shuffle the update point to now:
                _updatePosition = Values.Count;
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return copy;
        }

        /// <summary>
        /// Removes the data from this series and resets the update position to 0
        /// </summary>
        public void Purge()
        {
            Values.Clear();
            _updatePosition = 0;
        }

        /// <summary>
        /// Will sum up all chart points into a new single value, using the time of latest point
        /// </summary>
        /// <returns>The new chart point</returns>
        public abstract ISeriesPoint ConsolidateChartPoints();

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        /// <returns></returns>
        public abstract BaseSeries Clone(bool empty = false);

        /// <summary>
        /// Return a list of cloned values
        /// </summary>
        /// <returns></returns>
        protected List<ISeriesPoint> CloneValues()
        {
            var clone = new List<ISeriesPoint>(Values.Count);
            foreach (var point in Values)
            {
                clone.Add(point.Clone());
            }
            return clone;
        }

        /// <summary>
        /// Returns an enumerable of the values of the series cast to the specified type
        /// </summary>
        /// <returns>An enumerable of the values of the series cast to the specified type</returns>
        public IEnumerable<T> GetValues<T>()
            where T : ISeriesPoint
        {
            return Values.Cast<T>();
        }

        /// <summary>
        /// Creates a series according to the specified type.
        /// </summary>
        /// <param name="seriesType">The series type</param>
        /// <param name="name">The name of the series</param>
        /// <param name="index">Series index position on the chart</param>
        /// <param name="unit">Unit for the series axis</param>
        /// <returns>
        /// A <see cref="CandlestickSeries"/> if <paramref name="seriesType"/> is <see cref="SeriesType.Candle"/>.
        /// A <see cref="Series"/> otherwise.
        /// </returns>
        public static BaseSeries Create(SeriesType seriesType, string name, int index = 0, string unit = "$")
        {
            if (!Enum.IsDefined(typeof(SeriesType), seriesType))
            {
                throw new ArgumentOutOfRangeException(nameof(seriesType), "Series type out of range");
            }

            if (seriesType == SeriesType.Candle)
            {
                return new CandlestickSeries(name, index, unit);
            }

            return new Series(name, seriesType, index, unit);
        }
    }

    /// <summary>
    /// Available types of chart series
    /// </summary>
    public enum SeriesType
    {
        /// Line Plot for Value Types (0)
        Line,
        /// Scatter Plot for Chart Distinct Types (1)
        Scatter,
        /// Charts (2)
        Candle,
        /// Bar chart (3)
        Bar,
        /// Flag indicators (4)
        Flag,
        /// 100% area chart showing relative proportions of series values at each time index (5)
        StackedArea,
        /// Pie chart (6)
        Pie,
        /// Treemap Plot (7)
        Treemap,
        /// Heatmap Plot (9) -- NOTE: 8 is reserved
        Heatmap = 9,
        /// Scatter 3D Plot (10)
        Scatter3d
    }
}
