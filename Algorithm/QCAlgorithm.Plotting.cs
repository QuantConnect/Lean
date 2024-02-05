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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        private bool _isEmitWarmupPlotWarningSet;
        private readonly ConcurrentDictionary<string, Chart> _charts = new ConcurrentDictionary<string, Chart>();

        private static readonly Dictionary<string, List<string>> ReservedChartSeriesNames = new Dictionary<string, List<string>>
        {
            { "Strategy Equity", new List<string> { "Equity", "Return" } },
            { "Capacity", new List<string> { "Strategy Capacity" } },
            { "Drawdown", new List<string> { "Equity Drawdown" } },
            { "Benchmark", new List<string>() { "Benchmark" } },
            { "Assets Sales Volume", new List<string>() },
            { "Exposure", new List<string>() },
            { "Portfolio Margin", new List<string>() },
            { "Portfolio Turnover", new List<string> { "Portfolio Turnover" } }
        };

        /// <summary>
        /// Access to the runtime statistics property. User provided statistics.
        /// </summary>
        /// <remarks> RuntimeStatistics are displayed in the head banner in live trading</remarks>
        [DocumentationAttribute(Charting)]
        public ConcurrentDictionary<string, string> RuntimeStatistics { get; } = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Add a Chart object to algorithm collection
        /// </summary>
        /// <param name="chart">Chart object to add to collection.</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        [DocumentationAttribute(Charting)]
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
        [DocumentationAttribute(Charting)]
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
        [DocumentationAttribute(Charting)]
        public void Record(string series, int value)
        {
            Plot(series, value);
        }

        /// <summary>
        /// Plot a chart using string series name, with double value. Alias of Plot();
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        [DocumentationAttribute(Charting)]
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
        [DocumentationAttribute(Charting)]
        public void Record(string series, decimal value)
        {
            //By default plot to the primary chart:
            Plot(series, value);
        }

        /// <summary>
        /// Plot a chart using string series name, with double value.
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string series, double value) {
            Plot(series, value.SafeDecimalCast());
        }

        /// <summary>
        /// Plot a chart using string series name, with int value.
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string series, int value)
        {
            Plot(series, (decimal)value);
        }

        /// <summary>
        ///Plot a chart using string series name, with float value.
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string series, float value)
        {
            Plot(series, (double)value);
        }

        /// <summary>
        /// Plot a chart to string chart name, using string series name, with double value.
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, string series, double value)
        {
            Plot(chart, series, value.SafeDecimalCast());
        }

        /// <summary>
        /// Plot a chart to string chart name, using string series name, with int value
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, string series, int value)
        {
            Plot(chart, series, (decimal)value);
        }

        /// <summary>
        /// Plot a chart to string chart name, using string series name, with float value
        /// </summary>
        /// <seealso cref="Plot(string,string,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, string series, float value)
        {
            Plot(chart, series, (double)value);
        }

        /// <summary>
        /// Plot a value to a chart of string-chart name, with string series name, and decimal value. If chart does not exist, create it.
        /// </summary>
        /// <param name="chart">Chart name</param>
        /// <param name="series">Series name</param>
        /// <param name="value">Value of the point</param>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, string series, decimal value)
        {
            if (TryGetChartSeries(chart, series, out Series chartSeries))
            {
                chartSeries.AddPoint(UtcTime, value);
            }
        }

        /// <summary>
        /// Plot a candlestick to the default/primary chart series by the given series name.
        /// </summary>
        /// <param name="series">Series name</param>
        /// <param name="open">The candlestick open value</param>
        /// <param name="high">The candlestick high value</param>
        /// <param name="low">The candlestick low value</param>
        /// <param name="close">The candlestick close value</param>
        /// <seealso cref="Plot(string,string,decimal,decimal,decimal,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string series, double open, double high, double low, double close)
        {
            Plot(series, open.SafeDecimalCast(), high.SafeDecimalCast(), low.SafeDecimalCast(), close.SafeDecimalCast());
        }

        /// <summary>
        /// Plot a candlestick to the default/primary chart series by the given series name.
        /// </summary>
        /// <param name="series">Series name</param>
        /// <param name="open">The candlestick open value</param>
        /// <param name="high">The candlestick high value</param>
        /// <param name="low">The candlestick low value</param>
        /// <param name="close">The candlestick close value</param>
        /// <seealso cref="Plot(string,string,decimal,decimal,decimal,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string series, float open, float high, float low, float close)
        {
            Plot(series, (double)open, (double)high, (double)low, (double)close);
        }

        /// <summary>
        /// Plot a candlestick to the default/primary chart series by the given series name.
        /// </summary>
        /// <param name="series">Series name</param>
        /// <param name="open">The candlestick open value</param>
        /// <param name="high">The candlestick high value</param>
        /// <param name="low">The candlestick low value</param>
        /// <param name="close">The candlestick close value</param>
        /// <seealso cref="Plot(string,string,decimal,decimal,decimal,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string series, int open, int high, int low, int close)
        {
            Plot(series, (decimal)open, (decimal)high, (decimal)low, (decimal)close);
        }

        /// <summary>
        /// Plot a candlestick to the default/primary chart series by the given series name.
        /// </summary>
        /// <param name="series">Name of the plot series</param>
        /// <param name="open">The candlestick open value</param>
        /// <param name="high">The candlestick high value</param>
        /// <param name="low">The candlestick low value</param>
        /// <param name="close">The candlestick close value</param>
        /// <seealso cref="Plot(string,string,decimal,decimal,decimal,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string series, decimal open, decimal high, decimal low, decimal close)
        {
            //By default plot to the primary chart:
            Plot("Strategy Equity", series, open, high, low, close);
        }

        /// <summary>
        /// Plot a candlestick to the given series of the given chart.
        /// </summary>
        /// <param name="chart">Chart name</param>
        /// <param name="series">Series name</param>
        /// <param name="open">The candlestick open value</param>
        /// <param name="high">The candlestick high value</param>
        /// <param name="low">The candlestick low value</param>
        /// <param name="close">The candlestick close value</param>
        /// <seealso cref="Plot(string,string,decimal,decimal,decimal,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, string series, double open, double high, double low, double close)
        {
            Plot(chart, series, open.SafeDecimalCast(), high.SafeDecimalCast(), low.SafeDecimalCast(), close.SafeDecimalCast());
        }

        /// <summary>
        /// Plot a candlestick to the given series of the given chart.
        /// </summary>
        /// <param name="chart">Chart name</param>
        /// <param name="series">Series name</param>
        /// <param name="open">The candlestick open value</param>
        /// <param name="high">The candlestick high value</param>
        /// <param name="low">The candlestick low value</param>
        /// <param name="close">The candlestick close value</param>
        /// <seealso cref="Plot(string,string,decimal,decimal,decimal,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, string series, float open, float high, float low, float close)
        {
            Plot(chart, series, (double)open, (double)high, (double)low, (double)close);
        }

        /// <summary>
        /// Plot a candlestick to the given series of the given chart.
        /// </summary>
        /// <param name="chart">Chart name</param>
        /// <param name="series">Series name</param>
        /// <param name="open">The candlestick open value</param>
        /// <param name="high">The candlestick high value</param>
        /// <param name="low">The candlestick low value</param>
        /// <param name="close">The candlestick close value</param>
        /// <seealso cref="Plot(string,string,decimal,decimal,decimal,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, string series, int open, int high, int low, int close)
        {
            Plot(chart, series, (decimal)open, (decimal)high, (decimal)low, (decimal)close);
        }

        /// <summary>
        /// Plot a candlestick to a chart of string-chart name, with string series name, and decimal value. If chart does not exist, create it.
        /// </summary>
        /// <param name="chart">Chart name</param>
        /// <param name="series">Series name</param>
        /// <param name="open">The candlestick open value</param>
        /// <param name="high">The candlestick high value</param>
        /// <param name="low">The candlestick low value</param>
        /// <param name="close">The candlestick close value</param>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, string series, decimal open, decimal high, decimal low, decimal close)
        {
            if (TryGetChartSeries(chart, series, out CandlestickSeries candlestickSeries))
            {
                candlestickSeries.AddPoint(UtcTime, open, high, low, close);
            }
        }

        /// <summary>
        /// Plot a candlestick to the given series of the given chart.
        /// </summary>
        /// <param name="series">Name of the plot series</param>
        /// <param name="bar">The trade bar to be plotted to the candlestick series</param>
        /// <seealso cref="Plot(string,string,decimal,decimal,decimal,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string series, TradeBar bar)
        {
            Plot(series, bar.Open, bar.High, bar.Low, bar.Close);
        }

        /// <summary>
        /// Plot a candlestick to the given series of the given chart.
        /// </summary>
        /// <param name="chart">Chart name</param>
        /// <param name="series">Name of the plot series</param>
        /// <param name="bar">The trade bar to be plotted to the candlestick series</param>
        /// <seealso cref="Plot(string,string,decimal,decimal,decimal,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, string series, TradeBar bar)
        {
            Plot(chart, series, bar.Open, bar.High, bar.Low, bar.Close);
        }

        private bool TryGetChartSeries<T>(string chartName, string seriesName, out T series)
            where T : BaseSeries, new()
        {
            series = null;

            // Check if chart/series names are reserved
            if (ReservedChartSeriesNames.TryGetValue(chartName, out var reservedSeriesNames))
            {
                if (reservedSeriesNames.Count == 0)
                {
                    throw new ArgumentException($"'{chartName}' is a reserved chart name.");
                }
                if (reservedSeriesNames.Contains(seriesName))
                {
                    throw new ArgumentException($"'{seriesName}' is a reserved series name for chart '{chartName}'.");
                }
            }

            if(!_charts.TryGetValue(chartName, out var chart))
            {
                // If we don't have the chart, create it
                _charts[chartName] = chart = new Chart(chartName);
            }

            if (!chart.Series.TryGetValue(seriesName, out var chartSeries))
            {
                chartSeries = new T() { Name = seriesName };
                chart.AddSeries(chartSeries);
            }

            if (LiveMode && IsWarmingUp)
            {
                if (!_isEmitWarmupPlotWarningSet)
                {
                    _isEmitWarmupPlotWarningSet = true;
                    Debug("Plotting is disabled during algorithm warmup in live trading.");
                }
                return false;
            }

            series = (T)chartSeries;
            return true;
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
        [DocumentationAttribute(Charting)]
        public void AddSeries(string chart, string series, SeriesType seriesType, string unit = "$")
        {
            Chart c;
            if (!_charts.TryGetValue(chart, out c))
            {
                _charts[chart] = c = new Chart(chart);
            }

            c.Series[series] = BaseSeries.Create(seriesType, series, unit: unit);
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="indicators">The indicators to plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, params IndicatorBase[] indicators)
        {
            foreach (var indicator in indicators)
            {
                Plot(chart, indicator.Name, indicator.Current.Value);
            }
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        [DocumentationAttribute(Charting)]
        [DocumentationAttribute(Indicators)]
        public void PlotIndicator(string chart, params IndicatorBase[] indicators)
        {
            PlotIndicator(chart, false, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        [DocumentationAttribute(Charting)]
        [DocumentationAttribute(Indicators)]
        public void PlotIndicator(string chart, bool waitForReady, params IndicatorBase[] indicators)
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
        [DocumentationAttribute(Charting)]
        public void SetRuntimeStatistic(string name, string value)
        {
            RuntimeStatistics.AddOrUpdate(name, value);
        }

        /// <summary>
        /// Set a runtime statistic for the algorithm. Runtime statistics are shown in the top banner of a live algorithm GUI.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">Decimal value of your runtime statistic</param>
        [DocumentationAttribute(Charting)]
        public void SetRuntimeStatistic(string name, decimal value)
        {
            SetRuntimeStatistic(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Set a runtime statistic for the algorithm. Runtime statistics are shown in the top banner of a live algorithm GUI.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">Int value of your runtime statistic</param>
        [DocumentationAttribute(Charting)]
        public void SetRuntimeStatistic(string name, int value)
        {
            SetRuntimeStatistic(name, value.ToStringInvariant());
        }

        /// <summary>
        /// Set a runtime statistic for the algorithm. Runtime statistics are shown in the top banner of a live algorithm GUI.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">Double value of your runtime statistic</param>
        [DocumentationAttribute(Charting)]
        public void SetRuntimeStatistic(string name, double value)
        {
            SetRuntimeStatistic(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Set a custom summary statistic for the algorithm.
        /// </summary>
        /// <param name="name">Name of the custom summary statistic</param>
        /// <param name="value">Value of the custom summary statistic</param>
        [DocumentationAttribute(StatisticsTag)]
        public void SetSummaryStatistic(string name, string value)
        {
            _statisticsService.SetSummaryStatistic(name, value);
        }

        /// <summary>
        /// Set a custom summary statistic for the algorithm.
        /// </summary>
        /// <param name="name">Name of the custom summary statistic</param>
        /// <param name="value">Value of the custom summary statistic</param>
        [DocumentationAttribute(StatisticsTag)]
        public void SetSummaryStatistic(string name, int value)
        {
            _statisticsService.SetSummaryStatistic(name, value.ToStringInvariant());
        }

        /// <summary>
        /// Set a custom summary statistic for the algorithm.
        /// </summary>
        /// <param name="name">Name of the custom summary statistic</param>
        /// <param name="value">Value of the custom summary statistic</param>
        [DocumentationAttribute(StatisticsTag)]
        public void SetSummaryStatistic(string name, double value)
        {
            _statisticsService.SetSummaryStatistic(name, value.ToStringInvariant());
        }

        /// <summary>
        /// Set a custom summary statistic for the algorithm.
        /// </summary>
        /// <param name="name">Name of the custom summary statistic</param>
        /// <param name="value">Value of the custom summary statistic</param>
        [DocumentationAttribute(StatisticsTag)]
        public void SetSummaryStatistic(string name, decimal value)
        {
            _statisticsService.SetSummaryStatistic(name, value.ToStringInvariant());
        }

        /// <summary>
        /// Get the chart updates by fetch the recent points added and return for dynamic Charting.
        /// </summary>
        /// <param name="clearChartData"></param>
        /// <returns>List of chart updates since the last request</returns>
        /// <remarks>GetChartUpdates returns the latest updates since previous request.</remarks>
        [DocumentationAttribute(Charting)]
        public IEnumerable<Chart> GetChartUpdates(bool clearChartData = false)
        {
            foreach (var chart in _charts.Values)
            {
                yield return chart.GetUpdates();
                if (clearChartData)
                {
                    // we can clear this data out after getting updates to prevent unnecessary memory usage
                    foreach (var series in chart.Series)
                    {
                        series.Value.Purge();
                    }
                }
            }
        }
    }
}
