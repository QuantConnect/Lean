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
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Chart Series Object - Series data and properties for a chart:
    /// </summary>
    [JsonConverter(typeof(SeriesJsonConverter))]
    public class Series
    {
        /// <summary>
        /// Name of the Series:
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Axis for the chart series.
        /// </summary>
        public string Unit = "$";

        /// <summary>
        /// Index/position of the series on the chart.
        /// </summary>
        public int Index;

        /// <summary>
        ///  Values for the series plot:
        /// These values are assumed to be in ascending time order (first points earliest, last points latest)
        /// </summary>
        public List<ChartPoint> Values = new List<ChartPoint>();

        /// <summary>
        /// Chart type for the series:
        /// </summary>
        public SeriesType SeriesType = SeriesType.Line;

        /// <summary>
        /// Color the series
        /// </summary>
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color Color = Color.Empty;

        /// <summary>
        /// Shape or symbol for the marker in a scatter plot
        /// </summary>
        public ScatterMarkerSymbol ScatterMarkerSymbol = ScatterMarkerSymbol.None;

        /// Get the index of the last fetch update request to only retrieve the "delta" of the previous request.
        private int _updatePosition;

        /// <summary>
        /// Default constructor for chart series
        /// </summary>
        public Series() { }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        public Series(string name)
        {
            Name = name;
            SeriesType = SeriesType.Line;
            Unit = "$";
            Index = 0;
            Color = Color.Empty;
            ScatterMarkerSymbol = ScatterMarkerSymbol.None;
        }

        /// <summary>
        /// Foundational constructor on the series class
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="type">Type of the series</param>
        public Series(string name, SeriesType type)
        {
            Name = name;
            SeriesType = type;
            Index = 0;
            Unit = "$";
            Color = Color.Empty;
            ScatterMarkerSymbol = ScatterMarkerSymbol.None;
        }

        /// <summary>
        /// Foundational constructor on the series class
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="type">Type of the series</param>
        /// <param name="index">Index position on the chart of the series</param>
        public Series(string name, SeriesType type, int index)
        {
            Name = name;
            SeriesType = type;
            Index = index;
            Unit = "$";
            Color = Color.Empty;
            ScatterMarkerSymbol = ScatterMarkerSymbol.None;
        }

        /// <summary>
        /// Foundational constructor on the series class
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="type">Type of the series</param>
        /// <param name="index">Index position on the chart of the series</param>
        /// <param name="unit">Unit for the series axis</param>
        public Series(string name, SeriesType type, int index, string unit)
        {
            Name = name;
            SeriesType = type;
            Index = index;
            Unit = unit;
            Color = Color.Empty;
            ScatterMarkerSymbol = ScatterMarkerSymbol.None;
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        /// <param name="type">Type of the chart series</param>
        /// <param name="unit">Unit of the serier</param>
        public Series(string name, SeriesType type = SeriesType.Line, string unit = "$")
        {
            Name = name;
            Values = new List<ChartPoint>();
            SeriesType = type;
            Unit = unit;
            Index = 0;
            Color = Color.Empty;
            ScatterMarkerSymbol = ScatterMarkerSymbol.None;
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        /// <param name="type">Type of the chart series</param>
        /// <param name="unit">Unit of the serier</param>
        /// <param name="color">Color of the series</param>
        public Series(string name, SeriesType type, string unit, Color color)
        {
            Name = name;
            Values = new List<ChartPoint>();
            SeriesType = type;
            Unit = unit;
            Index = 0;
            Color = color;
            ScatterMarkerSymbol = ScatterMarkerSymbol.None;
        }

        /// <summary>
        /// Constructor method for Chart Series
        /// </summary>
        /// <param name="name">Name of the chart series</param>
        /// <param name="type">Type of the chart series</param>
        /// <param name="unit">Unit of the serier</param>
        /// <param name="color">Color of the series</param>
        /// <param name="symbol">Symbol for the marker in a scatter plot series</param>
        public Series(string name, SeriesType type, string unit, Color color, ScatterMarkerSymbol symbol = ScatterMarkerSymbol.None)
        {
            Name = name;
            Values = new List<ChartPoint>();
            SeriesType = type;
            Unit = unit;
            Index = 0;
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
            var chartPoint = new ChartPoint(time, value);
            AddPoint(chartPoint);
        }

        /// <summary>
        /// Add a new point to this series
        /// </summary>
        /// <param name="chartPoint">The data point to add</param>
        public void AddPoint(ChartPoint chartPoint)
        {
            if (Values.Count > 0 && Values[Values.Count - 1].x == chartPoint.x)
            {
                // duplicate points at the same time, overwrite the value
                Values[Values.Count - 1] = chartPoint;
            }
            else
            {
                Values.Add(chartPoint);
            }
        }

        /// <summary>
        /// Get the updates since the last call to this function.
        /// </summary>
        /// <returns>List of the updates from the series</returns>
        public Series GetUpdates()
        {
            var copy = new Series(Name, SeriesType, Index, Unit)
            {
                Color = Color,
                ScatterMarkerSymbol = ScatterMarkerSymbol
            };

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
        /// Will sum up all chart points into a new single value, using the time of lastest point
        /// </summary>
        /// <returns>The new chart point</returns>
        public ChartPoint ConsolidateChartPoints()
        {
            if (Values.Count <= 0) return null;

            var sum = 0m;
            foreach (var point in Values)
            {
                sum += point.y;
            }

            var lastPoint = Values.Last();
            return new ChartPoint(lastPoint.x, sum);
        }

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        /// <returns></returns>
        public Series Clone()
        {
            var series = new Series
            {
                Name = Name,
                Values = new List<ChartPoint>(),
                SeriesType = SeriesType,
                Unit = Unit,
                Index = Index,
                Color = Color,
                ScatterMarkerSymbol = ScatterMarkerSymbol
            };

            foreach (var point in Values)
            {
                series.Values.Add(new ChartPoint(point.x, point.y));
            }

            return series;
        }
    }

    /// <summary>
    /// Available types of charts
    /// </summary>
    public enum SeriesType
    {
        /// Line Plot for Value Types
        Line,
        /// Scatter Plot for Chart Distinct Types
        Scatter,
        /// Charts
        Candle,
        /// Bar chart.
        Bar,
        /// Flag indicators
        Flag,
        /// 100% area chart showing relative proportions of series values at each time index
        StackedArea,
        /// Pie chart
        Pie,
        /// Treemap Plot
        Treemap
    }

    /// <summary>
    /// Shape or symbol for the marker in a scatter plot
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ScatterMarkerSymbol
    {
        /// Circle symbol
        [EnumMember(Value = "none")]
        None,
        /// Circle symbol
        [EnumMember(Value = "circle")]
        Circle,
        /// Square symbol
        [EnumMember(Value = "square")]
        Square,
        /// Diamond symbol
        [EnumMember(Value = "diamond")]
        Diamond,
        /// Triangle symbol
        [EnumMember(Value = "triangle")]
        Triangle,
        /// Triangle-down symbol
        [EnumMember(Value = "triangle-down")]
        TriangleDown,
    }
}
