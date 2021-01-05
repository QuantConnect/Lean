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
using QuantConnect.Logging;

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
                throw new DuplicateNameException("Chart.AddSeries(): Chart series name already exists");
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
            Series series;
            if (forceAddNew || !Series.TryGetValue(name, out series))
            {
                series = new Series(name, type, index, unit)
                {
                    Color = color,
                    ScatterMarkerSymbol = symbol
                };
                Series[name] = series;
            }

            return series;
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
    /// Type of chart - should we draw the series as overlayed or stacked
    /// </summary>
    public enum ChartType
    {
        /// Overlayed stacked
        Overlay,
        /// Stacked series on top of each other.
        Stacked
    }
}
