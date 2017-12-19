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
using Newtonsoft.Json;
using QuantConnect.Logging;
using System.Drawing;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Single Parent Chart Object for Custom Charting
    /// </summary>
    [JsonObject]
    public class Chart
    {
        /// Name of the Chart:
        public string Name = "";

        /// Type of the Chart, Overlayed or Stacked.
        [Obsolete("ChartType is now obsolete. Please use Series indexes instead by setting index in the series constructor.")]
        public ChartType ChartType = ChartType.Overlay;

        /// List of Series Objects for this Chart:
        public Dictionary<string, Series> Series = new Dictionary<string, Series>();

        /// <summary>
        /// Default constructor for chart:
        /// </summary>
        public Chart() { }

        /// <summary>
        /// Chart Constructor:
        /// </summary>
        /// <param name="name">Name of the Chart</param>
        /// <param name="type"> Type of the chart</param>
        [Obsolete("ChartType is now obsolete and ignored in charting. Please use Series indexes instead by setting index in the series constructor.")]
        public Chart(string name, ChartType type = ChartType.Overlay)
        {
            Name = name;
            Series = new Dictionary<string, Series>();
            ChartType = type;
        }

        /// <summary>
        /// Constructor for a chart
        /// </summary>
        /// <param name="name">String name of the chart</param>
        public Chart(string name)
        {
            Name = name;
            Series = new Dictionary<string, Series>();
        }

        /// <summary>
        /// Add a reference to this chart series:
        /// </summary>
        /// <param name="series">Chart series class object</param>
        public void AddSeries(Series series)
        {
            //If we dont already have this series, add to the chrt:
            if (!Series.ContainsKey(series.Name))
            {
                Series.Add(series.Name, series);
            }
            else
            {
                throw new Exception("Chart.AddSeries(): Chart series name already exists");
            }
        }

        /// <summary>
        /// Fetch the updates of the chart, and save the index position.
        /// </summary>
        /// <returns></returns>
        public Chart GetUpdates()
        {
            var copy = new Chart(Name);
            try
            {
                foreach (var series in Series.Values)
                {
                    copy.AddSeries(series.GetUpdates());
                }
            }
            catch (Exception err) {
                Log.Error(err);
            }
            return copy;
        }

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        /// <returns></returns>
        public Chart Clone()
        {
            var chart = new Chart(Name);

            foreach (var kvp in Series)
            {
                chart.Series.Add(kvp.Key, kvp.Value.Clone());
            }

            return chart;
        }
    }


    /// <summary>
    /// Chart Series Object - Series data and properties for a chart:
    /// </summary>
    [JsonObject]
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
        /// Add a new point to this series:
        /// </summary>
        /// <param name="time">Time of the chart point</param>
        /// <param name="value">Value of the chart point</param>
        /// <param name="liveMode">This is a live mode point</param>
        public void AddPoint(DateTime time, decimal value, bool liveMode = false)
        {
            if (Values.Count >= 4000 && !liveMode)
            {
                // perform rate limiting in backtest mode
                return;
            }

            var chartPoint = new ChartPoint(time, value);
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
            catch (Exception err) {
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
    /// Single Chart Point Value Type for QCAlgorithm.Plot();
    /// </summary>
    [JsonObject]
    public class ChartPoint
    {
        /// Time of this chart point: lower case for javascript encoding simplicty
        public long x;

        /// Value of this chart point:  lower case for javascript encoding simplicty
        public decimal y;

        /// <summary>
        /// Default constructor. Using in SeriesSampler.
        /// </summary>
        public ChartPoint() {}

        /// <summary>
        /// Constructor that takes both x, y value paris
        /// </summary>
        /// <param name="xValue">X value often representing a time in seconds</param>
        /// <param name="yValue">Y value</param>
        public ChartPoint(long xValue, decimal yValue)
        {
            x = xValue;
            y = yValue;
        }

        ///Constructor for datetime-value arguements:
        public ChartPoint(DateTime time, decimal value)
        {
            x = Convert.ToInt64(Time.DateTimeToUnixTimeStamp(time.ToUniversalTime()));
            y = value.SmartRounding();
        }

        ///Cloner Constructor:
        public ChartPoint(ChartPoint point)
        {
            x = point.x;
            y = point.y.SmartRounding();
        }

        /// <summary>
        /// Provides a readable string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return Time.UnixTimeStampToDateTime(x).ToString("o") + " - " + y;
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
        Pie
    }

    /// <summary>
    /// Type of chart - should we draw the series as overlayed or stacked
    /// </summary>
    public enum ChartType
    {
        /// Overlayed stacked
        Overlay,
        /// Stacked series on top of each other.
        Stacked
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
