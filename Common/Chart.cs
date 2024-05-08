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
using System.Data;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuantConnect.Logging;

namespace QuantConnect
{
    /// <summary>
    /// Single Parent Chart Object for Custom Charting
    /// </summary>
    public class Chart
    {
        /// <summary>
        /// Name of the Chart
        /// </summary>
        public string Name = "";

        /// Type of the Chart, Overlayed or Stacked.
        [Obsolete("ChartType is now obsolete. Please use Series indexes instead by setting index in the series constructor.")]
        public ChartType ChartType = ChartType.Overlay;

        /// List of Series Objects for this Chart:
        [JsonConverter(typeof(ChartSeriesJsonConverter))]
        public Dictionary<string, BaseSeries> Series = new Dictionary<string, BaseSeries>();

        /// <summary>
        /// Associated symbol if any, making this an asset plot
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Symbol Symbol { get; set; }

        /// <summary>
        /// True to hide this series legend from the chart
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool LegendDisabled { get; set; }

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
            Series = new Dictionary<string, BaseSeries>();
            ChartType = type;
        }

        /// <summary>
        /// Constructor for a chart
        /// </summary>
        /// <param name="name">String name of the chart</param>
        public Chart(string name) : this(name, null)
        {
        }

        /// <summary>
        /// Constructor for a chart
        /// </summary>
        /// <param name="name">String name of the chart</param>
        /// <param name="symbol">Associated symbol if any</param>
        public Chart(string name, Symbol symbol)
        {
            Name = name;
            Symbol = symbol;
            Series = new Dictionary<string, BaseSeries>();
        }

        /// <summary>
        /// Add a reference to this chart series:
        /// </summary>
        /// <param name="series">Chart series class object</param>
        public void AddSeries(BaseSeries series)
        {
            //If we dont already have this series, add to the chrt:
            if (!Series.ContainsKey(series.Name))
            {
                Series.Add(series.Name, series);
            }
            else
            {
                throw new DuplicateNameException($"Chart.AddSeries(): ${Messages.Chart.ChartSeriesAlreadyExists}");
            }
        }

        /// <summary>
        /// Gets Series if already present in chart, else will add a new series and return it
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="type">Type of the series</param>
        /// <param name="index">Index position on the chart of the series</param>
        /// <param name="unit">Unit for the series axis</param>
        /// <param name="color">Color of the series</param>
        /// <param name="symbol">Symbol for the marker in a scatter plot series</param>
        /// <param name="forceAddNew">True will always add a new Series instance, stepping on existing if any</param>
        public Series TryAddAndGetSeries(string name, SeriesType type, int index, string unit,
                                      Color color, ScatterMarkerSymbol symbol, bool forceAddNew = false)
        {
            BaseSeries series;
            if (forceAddNew || !Series.TryGetValue(name, out series))
            {
                series = new Series(name, type, index, unit)
                {
                    Color = color,
                    ScatterMarkerSymbol = symbol
                };
                Series[name] = series;
            }

            return (Series)series;
        }

        /// <summary>
        /// Gets Series if already present in chart, else will add a new series and return it
        /// </summary>
        /// <param name="name">Name of the series</param>
        /// <param name="templateSeries">Series to be used as a template. It will be clone without values if the series is added to the chart</param>
        /// <param name="forceAddNew">True will always add a new Series instance, stepping on existing if any</param>
        public BaseSeries TryAddAndGetSeries(string name, BaseSeries templateSeries, bool forceAddNew = false)
        {
            BaseSeries chartSeries;
            if (forceAddNew || !Series.TryGetValue(name, out chartSeries))
            {
                Series[name] = chartSeries = templateSeries.Clone(empty: true);
            }

            return chartSeries;
        }

        /// <summary>
        /// Fetch a chart with only the updates since the last request,
        /// Underlying series will save the index position.
        /// </summary>
        /// <returns></returns>
        public Chart GetUpdates()
        {
            var copy = CloneEmpty();
            try
            {
                foreach (var series in Series.Values)
                {
                    copy.AddSeries(series.GetUpdates());
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return copy;
        }

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        /// <returns></returns>
        public virtual Chart Clone()
        {
            var chart = CloneEmpty();

            foreach (var kvp in Series)
            {
                chart.Series.Add(kvp.Key, kvp.Value.Clone());
            }

            return chart;
        }

        /// <summary>
        /// Return a new empty instance clone of this object
        /// </summary>
        public virtual Chart CloneEmpty()
        {
            return new Chart(Name) { LegendDisabled = LegendDisabled, Symbol = Symbol };
        }
    }

    /// <summary>
    /// Type of chart - should we draw the series as overlayed or stacked
    /// </summary>
    public enum ChartType
    {
        /// Overlayed stacked (0)
        Overlay,
        /// Stacked series on top of each other. (1)
        Stacked
    }
}
