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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        private readonly ConcurrentDictionary<string, Chart> _charts = new ConcurrentDictionary<string, Chart>();

        private static readonly Dictionary<string, List<string>> ReservedChartSeriesNames = new Dictionary<string, List<string>>
        {
            { "Strategy Equity", new List<string> { "Equity", "Daily Performance" } },
            { "Meta", new List<string>() },
            { "Alpha", new List<string> { "Direction Score", "Magnitude Score" } },
            { "Alpha Count", new List<string> { "Count" } },
            { "Alpha Assets", new List<string>() },
            { "Alpha Asset Breakdown", new List<string>() }
        };

        /// <summary>
        /// Access to the runtime statistics property. User provided statistics.
        /// </summary>
        /// <remarks> RuntimeStatistics are displayed in the head banner in live trading</remarks>
        public ConcurrentDictionary<string, string> RuntimeStatistics { get; } = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Add a Chart object to algorithm collection
        /// </summary>
        /// <param name="chart">Chart object to add to collection.</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void AddChart(Chart chart)
        {
            _charts.TryAdd(chart.Name, chart);
        }

        /// <summary>
        /// Plot a chart using string series name, with value.
        /// </summary>
        /// <param name="series">Name of the plot series</param>
        /// <param name="value">Value to plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string series, decimal value)
        {
            //By default plot to the primary chart:
            Plot("Strategy Equity", series, value);
        }

        /// <summary>
        /// Plot a chart using string series name, with int value. Alias of Plot();
        /// </summary>
        /// <remarks> Record(string series, int value)</remarks>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Record(string series, int value)
        {
            Plot(series, value);
        }

        /// <summary>
        /// Plot a chart using string series name, with double value. Alias of Plot();
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Record(string series, double value)
        {
            Plot(series, value);
        }

        /// <summary>
        /// Plot a chart using string series name, with decimal value. Alias of Plot();
        /// </summary>
        /// <param name="series"></param>
        /// <param name="value"></param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Record(string series, decimal value)
        {
            //By default plot to the primary chart:
            Plot(series, value);
        }

        /// <summary>
        /// Plot a chart using string series name, with double value.
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string series, double value) {
            Plot(series, value.SafeDecimalCast());
        }

        /// <summary>
        /// Plot a chart using string series name, with int value.
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string series, int value)
        {
            Plot(series, (decimal)value);
        }

        /// <summary>
        ///Plot a chart using string series name, with float value.
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string series, float value)
        {
            Plot(series, (decimal)value);
        }

        /// <summary>
        /// Plot a chart to string chart name, using string series name, with double value.
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, string series, double value)
        {
            Plot(chart, series, value.SafeDecimalCast());
        }

        /// <summary>
        /// Plot a chart to string chart name, using string series name, with int value
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, string series, int value)
        {
            Plot(chart, series, (decimal)value);
        }

        /// <summary>
        /// Plot a chart to string chart name, using string series name, with float value
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, string series, float value)
        {
            Plot(chart, series, (decimal)value);
        }

        /// <summary>
        /// Plot a value to a chart of string-chart name, with string series name, and decimal value. If chart does not exist, create it.
        /// </summary>
        /// <param name="chart">Chart name</param>
        /// <param name="series">Series name</param>
        /// <param name="value">Value of the point</param>
        public void Plot(string chart, string series, decimal value)
        {
            // Check if chart/series names are reserved
            List<string> reservedSeriesNames;
            if (ReservedChartSeriesNames.TryGetValue(chart, out reservedSeriesNames))
            {
                if (reservedSeriesNames.Count == 0)
                {
                    throw new Exception($"Algorithm.Plot(): '{chart}' is a reserved chart name.");
                }
                if (reservedSeriesNames.Contains(series))
                {
                    throw new Exception($"Algorithm.Plot(): '{series}' is a reserved series name for chart '{chart}'.");
                }
            }

            // If we don't have the chart, create it:
            _charts.TryAdd(chart, new Chart(chart));

            var thisChart = _charts[chart];
            if (!thisChart.Series.ContainsKey(series))
            {
                //Number of series in total, excluding reserved charts
                var seriesCount = _charts.Select(x => x.Value)
                    .Aggregate(0, (i, c) => ReservedChartSeriesNames.TryGetValue(c.Name, out reservedSeriesNames)
                    ? i + c.Series.Values.Count(s => reservedSeriesNames.Count > 0 && !reservedSeriesNames.Contains(s.Name))
                    : i + c.Series.Count);

                if (seriesCount > 10)
                {
                    Error("Exceeded maximum series count: Each backtest can have up to 10 series in total.");
                    return;
                }

                //If we don't have the series, create it:
                thisChart.AddSeries(new Series(series, SeriesType.Line, 0, "$"));
            }

            thisChart.Series[series].AddPoint(UtcTime, value);
        }

        /// <summary>
        /// Add a series object for charting. This is useful when initializing charts with
        /// series other than type = line. If a series exists in the chart with the same name,
        /// then it is replaced.
        /// </summary>
        /// <param name="chart">The chart name</param>
        /// <param name="series">The series name</param>
        /// <param name="seriesType">The type of series, i.e, Scatter</param>
        /// <param name="unit">The unit of the y axis, usually $</param>
        public void AddSeries(string chart, string series, SeriesType seriesType, string unit = "$")
        {
            Chart c;
            if (!_charts.TryGetValue(chart, out c))
            {
                _charts[chart] = c = new Chart(chart);
            }

            c.Series[series] = new Series(series, seriesType, unit);
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="indicators">The indicatorsto plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot<T>(string chart, params IndicatorBase<T>[] indicators)
            where T : IBaseData
        {
            foreach (var indicator in indicators)
            {
                Plot(chart, indicator.Name, indicator.Current.Value);
            }
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator<T>(string chart, params IndicatorBase<T>[] indicators)
            where T : IBaseData
        {
            foreach (var i in indicators)
            {
                if (i == null) continue;

                // copy loop variable for usage in closure
                var ilocal = i;
                i.Updated += (sender, args) =>
                {
                    Plot(chart, ilocal);
                };
            }
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator<T>(string chart, bool waitForReady, params IndicatorBase<T>[] indicators)
            where T : IBaseData
        {
            foreach (var i in indicators)
            {
                if (i == null) continue;

                // copy loop variable for usage in closure
                var ilocal = i;
                i.Updated += (sender, args) =>
                {
                    if (!waitForReady || ilocal.IsReady)
                    {
                        Plot(chart, ilocal);
                    }
                };
            }
        }

        /// <summary>
        /// Set a runtime statistic for the algorithm. Runtime statistics are shown in the top banner of a live algorithm GUI.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">String value of your runtime statistic</param>
        /// <seealso cref="LiveMode"/>
        public void SetRuntimeStatistic(string name, string value)
        {
            RuntimeStatistics.AddOrUpdate(name, value);
        }

        /// <summary>
        /// Set a runtime statistic for the algorithm. Runtime statistics are shown in the top banner of a live algorithm GUI.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">Decimal value of your runtime statistic</param>
        public void SetRuntimeStatistic(string name, decimal value)
        {
            SetRuntimeStatistic(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Set a runtime statistic for the algorithm. Runtime statistics are shown in the top banner of a live algorithm GUI.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">Int value of your runtime statistic</param>
        public void SetRuntimeStatistic(string name, int value)
        {
            SetRuntimeStatistic(name, value.ToStringInvariant());
        }

        /// <summary>
        /// Set a runtime statistic for the algorithm. Runtime statistics are shown in the top banner of a live algorithm GUI.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">Double value of your runtime statistic</param>
        public void SetRuntimeStatistic(string name, double value)
        {
            SetRuntimeStatistic(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Get the chart updates by fetch the recent points added and return for dynamic plotting.
        /// </summary>
        /// <param name="clearChartData"></param>
        /// <returns>List of chart updates since the last request</returns>
        /// <remarks>GetChartUpdates returns the latest updates since previous request.</remarks>
        public List<Chart> GetChartUpdates(bool clearChartData = false)
        {
            var updates = _charts.Select(x => x.Value).Select(chart => chart.GetUpdates()).ToList();

            if (clearChartData)
            {
                // we can clear this data out after getting updates to prevent unnecessary memory usage
                foreach (var chart in _charts)
                {
                    foreach (var series in chart.Value.Series)
                    {
                        series.Value.Purge();
                    }
                }
            }
            return updates;
        }
    }
}