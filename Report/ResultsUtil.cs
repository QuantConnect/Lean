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
        /// <param name="seriesName">Series name from which the points will be extracted. By default is "Equity"</param>
        /// <returns></returns>
        public static SortedList<DateTime, double> EquityPoints(Result result, string seriesName = "Equity")
        {
            var points = new SortedList<DateTime, double>();

            if (result == null || result.Charts == null ||
                !result.Charts.ContainsKey("Strategy Equity") ||
                result.Charts["Strategy Equity"].Series == null ||
                !result.Charts["Strategy Equity"].Series.ContainsKey(seriesName))
            {
                return points;
            }

            foreach (var point in result.Charts["Strategy Equity"].Series[seriesName].Values)
            {
                points[Time.UnixTimeStampToDateTime(point.x)] = Convert.ToDouble(point.y);
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
                !result.Charts.ContainsKey("Benchmark") ||
                result.Charts["Benchmark"].Series == null ||
                !result.Charts["Benchmark"].Series.ContainsKey("Benchmark"))
            {
                return points;
            }

            if (!result.Charts.ContainsKey("Benchmark"))
            {
                return new SortedList<DateTime, double>();
            }
            if (!result.Charts["Benchmark"].Series.ContainsKey("Benchmark"))
            {
                return new SortedList<DateTime, double>();
            }

            foreach (var point in result.Charts["Benchmark"].Series["Benchmark"].Values)
            {
                points[Time.UnixTimeStampToDateTime(point.x)] = Convert.ToDouble(point.y);
            }

            return points;
        }
    }
}
