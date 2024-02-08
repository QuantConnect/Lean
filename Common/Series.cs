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
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Chart Series Object - Series data and properties for a chart:
    /// </summary>
    [JsonConverter(typeof(SeriesJsonConverter))]
    public class Series : BaseSeries
    {
        /// <summary>
        /// Color the series
        /// </summary>
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color Color { get; set; } = Color.Empty;

        /// <summary>
        /// Shape or symbol for the marker in a scatter plot
        /// </summary>
        public ScatterMarkerSymbol ScatterMarkerSymbol { get; set; } = ScatterMarkerSymbol.None;

        /// <summary>
        /// Default constructor for chart series
        /// </summary>
        public Series() : base() { }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        public Series(string name)
            : base(name, SeriesType.Line)
        {
        }

        /// <summary>
        /// Foundational constructor on the series class
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="type">Type of the series</param>
        /// <param name="index">Index position on the chart of the series</param>
        public Series(string name, SeriesType type, int index)
            : this(name, type, index, "$")
        {
        }

        /// <summary>
        /// Foundational constructor on the series class
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="type">Type of the series</param>
        /// <param name="index">Index position on the chart of the series</param>
        /// <param name="unit">Unit for the series axis</param>
        public Series(string name, SeriesType type, int index, string unit)
            : this(name, type, unit, Color.Empty, ScatterMarkerSymbol.None)
        {
            Index = index;
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        /// <param name="type">Type of the chart series</param>
        /// <param name="unit">Unit of the series</param>
        public Series(string name, SeriesType type = SeriesType.Line, string unit = "$")
            : this(name, type, unit, Color.Empty)
        {
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        /// <param name="type">Type of the chart series</param>
        /// <param name="unit">Unit of the series</param>
        /// <param name="color">Color of the series</param>
        public Series(string name, SeriesType type, string unit, Color color)
            : this(name, type, unit, color, ScatterMarkerSymbol.None)
        {
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        /// <param name="type">Type of the chart series</param>
        /// <param name="unit">Unit of the series</param>
        /// <param name="color">Color of the series</param>
        /// <param name="symbol">Symbol for the marker in a scatter plot series</param>
        public Series(string name, SeriesType type, string unit, Color color, ScatterMarkerSymbol symbol = ScatterMarkerSymbol.None)
            : base(name, type, 0, unit)
        {
            Color = color;
            ScatterMarkerSymbol = symbol;
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        /// <param name="time">Time of the chart point</param>
        /// <param name="value">Value of the chart point</param>
        public void AddPoint(DateTime time, decimal value)
        {
            ISeriesPoint point;
            if (SeriesType == SeriesType.Scatter)
            {
                point = new ScatterChartPoint(time, value);
            }
            else
            {
                point = new ChartPoint(time, value);
            }
            AddPoint(point);
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        /// <param name="point">The data point to add</param>
        public override void AddPoint(ISeriesPoint point)
        {
            if (point as ChartPoint == null)
            {
                throw new ArgumentException("Series.AddPoint requires a ChartPoint object");
            }

            base.AddPoint(point);
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        /// <param name="time">The time of the data point</param>
        /// <param name="values">The values of the data point</param>
        public override void AddPoint(DateTime time, List<decimal> values)
        {
            if (values.Count > 1)
            {
                throw new ArgumentException("Series.AddPoint requires a single value");
            }

            AddPoint(time, values.Count > 0 ? values[0] : 0);
        }

        /// <summary>
        /// Will sum up all chart points into a new single value, using the time of latest point
        /// </summary>
        /// <returns>The new chart point</returns>
        public override ISeriesPoint ConsolidateChartPoints()
        {
            if (Values.Count <= 0) return null;

            var sum = 0m;
            foreach (ChartPoint point in Values)
            {
                if(point.y.HasValue)
                {
                    sum += point.y.Value;
                }
            }

            var lastPoint = (ChartPoint)Values.Last();
            return new ChartPoint(lastPoint.x, sum);
        }

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        /// <returns></returns>
        public override BaseSeries Clone(bool empty = false)
        {
            var series = new Series(Name, SeriesType, Index, Unit)
            {
                Color = Color,
                ZIndex = ZIndex,
                Tooltip = Tooltip,
                IndexName = IndexName,
                ScatterMarkerSymbol = ScatterMarkerSymbol,
            };

            if (!empty)
            {
                series.Values = CloneValues();
            }

            return series;
        }
    }

    /// <summary>
    /// Shape or symbol for the marker in a scatter plot
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ScatterMarkerSymbol
    {
        /// Circle symbol (0)
        [EnumMember(Value = "none")]
        None,
        /// Circle symbol (1)
        [EnumMember(Value = "circle")]
        Circle,
        /// Square symbol (2)
        [EnumMember(Value = "square")]
        Square,
        /// Diamond symbol (3)
        [EnumMember(Value = "diamond")]
        Diamond,
        /// Triangle symbol (4)
        [EnumMember(Value = "triangle")]
        Triangle,
        /// Triangle-down symbol (5)
        [EnumMember(Value = "triangle-down")]
        TriangleDown
    }
}
