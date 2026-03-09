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
using QuantConnect.Lean.Engine.Results;

namespace QuantConnect.Report
{
    /// <summary>
    /// Utility methods for dealing with the <see cref="Result"/> objects
    /// </summary>
    public static class ResultsUtil
    {
        /// <summary>
        /// Get the points, from the Series name given, in Strategy Equity chart
        /// </summary>
        /// <param name="result">Result object to extract the chart points</param>
        /// <param name="seriesName">Series name from which the points will be extracted. By default is Equity series</param>
        /// <returns></returns>
        public static SortedList<DateTime, double> EquityPoints(Result result, string seriesName = null)
        {
            var points = new SortedList<DateTime, double>();

            seriesName ??= BaseResultsHandler.EquityKey;
            if (result == null || result.Charts == null ||
                !result.Charts.ContainsKey(BaseResultsHandler.StrategyEquityKey) ||
                result.Charts[BaseResultsHandler.StrategyEquityKey].Series == null ||
                !result.Charts[BaseResultsHandler.StrategyEquityKey].Series.ContainsKey(seriesName))
            {
                return points;
            }

            var series = result.Charts[BaseResultsHandler.StrategyEquityKey].Series[seriesName];
            switch (series)
            {
                case Series s:
                    foreach (ChartPoint point in s.Values)
                    {
                        points[point.Time] = Convert.ToDouble(point.y);
                    }
                    break;

                case CandlestickSeries candlestickSeries:
                    foreach (Candlestick candlestick in candlestickSeries.Values)
                    {
                        points[candlestick.Time] = Convert.ToDouble(candlestick.Close);
                    }
                    break;
            }

            return points;
        }

        /// <summary>
        /// Gets the points of the benchmark
        /// </summary>
        /// <param name="result">Backtesting or live results</param>
        /// <returns>Sorted list keyed by date and value</returns>
        public static SortedList<DateTime, double> BenchmarkPoints(Result result)
        {
            var points = new SortedList<DateTime, double>();

            if (result == null || result.Charts == null ||
                !result.Charts.ContainsKey(BaseResultsHandler.BenchmarkKey) ||
                result.Charts[BaseResultsHandler.BenchmarkKey].Series == null ||
                !result.Charts[BaseResultsHandler.BenchmarkKey].Series.ContainsKey(BaseResultsHandler.BenchmarkKey))
            {
                return points;
            }

            if (!result.Charts.ContainsKey(BaseResultsHandler.BenchmarkKey))
            {
                return new SortedList<DateTime, double>();
            }
            if (!result.Charts[BaseResultsHandler.BenchmarkKey].Series.ContainsKey(BaseResultsHandler.BenchmarkKey))
            {
                return new SortedList<DateTime, double>();
            }

            // Benchmark should be a Series, so we cast the points directly to ChartPoint
            foreach (ChartPoint point in result.Charts[BaseResultsHandler.BenchmarkKey].Series[BaseResultsHandler.BenchmarkKey].Values)
            {
                points[Time.UnixTimeStampToDateTime(point.x)] = Convert.ToDouble(point.y);
            }

            return points;
        }
    }
}
