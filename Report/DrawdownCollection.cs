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

using Deedle;
using QuantConnect.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Report
{
    /// <summary>
    /// Collection of drawdowns for the given period marked by start and end date
    /// </summary>
    public class DrawdownCollection
    {
        /// <summary>
        /// Starting time of the drawdown collection
        /// </summary>
        public DateTime Start { get; private set; }

        /// <summary>
        /// Ending time of the drawdown collection
        /// </summary>
        public DateTime End { get; private set; }

        /// <summary>
        /// Number of periods to take into consideration for the top N drawdown periods.
        /// This will be the number of items contained in the <see cref="Drawdowns"/> collection.
        /// </summary>
        public int Periods { get; private set; }

        /// <summary>
        /// Worst drawdowns encountered
        /// </summary>
        public List<DrawdownPeriod> Drawdowns { get; private set; }

        /// <summary>
        /// Creates an instance with a default collection (no items) and the top N worst drawdowns
        /// </summary>
        /// <param name="periods"></param>
        public DrawdownCollection(int periods)
        {
            Drawdowns = new List<DrawdownPeriod>();
            Periods = periods;
        }

        /// <summary>
        /// Creates an instance from the given drawdowns and the top N worst drawdowns
        /// </summary>
        /// <param name="drawdowns">Drawdown groups</param>
        /// <param name="periods">Periods this collection contains</param>
        public DrawdownCollection(List<DrawdownPeriod> drawdowns, int periods)
        {
            Periods = periods;
            Start = drawdowns.Select(x => x.Start).Min();
            End = drawdowns.Select(x => x.End).Max();
            Drawdowns = drawdowns.OrderByDescending(x => x.PeakToTrough)
                .Take(Periods)
                .ToList();
        }

        /// <summary>
        /// Generate a new instance of DrawdownCollection from backtest and live <see cref="Result"/> derived instances
        /// </summary>
        /// <param name="backtestResult">Backtest result packet</param>
        /// <param name="liveResult">Live result packet</param>
        /// <param name="periods">Top N drawdown periods to get</param>
        /// <returns>DrawdownCollection instance</returns>
        public static DrawdownCollection FromResult(BacktestResult backtestResult = null, LiveResult liveResult = null, int periods = 5)
        {
            if (backtestResult == null && liveResult == null)
            {
                throw new ArgumentException("backtestResult and liveResult can not be null at the same time");
            }

            var backtestPoints = Calculations.EquityPoints(backtestResult);
            var livePoints = Calculations.EquityPoints(liveResult);

            // Cache the last point for performance
            var lastBacktestPoint = backtestPoints.Last().Value;

            var points = backtestPoints.Concat(
                livePoints
                    .Where(kvp => !backtestPoints.ContainsKey(kvp.Key))
                    .Select(kvp => new KeyValuePair<DateTime, double>(kvp.Key, kvp.Value + lastBacktestPoint))
                )
                .ToList();

            var strategySeries = new Series<DateTime, double>(points.Select(x => x.Key), points.Select(x => x.Value));

            return new DrawdownCollection(GetDrawdownPeriods(strategySeries, periods).ToList(), periods);
        }

        /// <summary>
        /// Gets the underwater plot for the provided curve.
        /// Data is expected to be the concatenated output of <see cref="Calculations.EquityPoints"/>.
        /// </summary>
        /// <param name="curve">Equity curve</param>
        /// <returns></returns>
        public static Series<DateTime, double> GetUnderwater(Series<DateTime, double> curve)
        {
            var returns = curve / curve.FirstValue();
            var cumulativeMax = Calculations.CumulativeMax(returns);

            return (1 - (returns / cumulativeMax)) * -1;
        }

        /// <summary>
        /// Gets all the data associated with the underwater plot and everything used to generate it.
        /// Note that you should instead use <see cref="GetUnderwater(Series{DateTime, double})"/> if you
        /// want to just generate an underwater plot. This is internally used to get the top N worst drawdown periods.
        /// </summary>
        /// <param name="curve">Equity curve</param>
        /// <returns>Frame containing the following keys: "returns", "cumulativeMax", "drawdown"</returns>
        public static Frame<DateTime, string> GetUnderwaterFrame(Series<DateTime, double> curve)
        {
            var returns = curve / curve.FirstValue();
            var cumulativeMax = Calculations.CumulativeMax(returns);
            var drawdown = 1 - (returns / cumulativeMax);

            var frame = Frame.CreateEmpty<DateTime, string>();

            frame.AddColumn("returns", returns);
            frame.AddColumn("cumulativeMax", cumulativeMax);
            frame.AddColumn("drawdown", drawdown);

            return frame;
        }

        /// <summary>
        /// Gets the top N worst drawdowns and associated statistics.
        /// Returns a Frame with the following keys: "duration", "cumulativeMax", "drawdown"
        /// </summary>
        /// <param name="curve">Equity curve</param>
        /// <param name="periods">Top N worst periods. If this is greater than the results, we retrieve all the items instead</param>
        /// <returns>Frame with the following keys: "duration", "cumulativeMax", "drawdown"</returns>
        public static Frame<DateTime, string> GetTopWorstDrawdowns(Series<DateTime, double> curve, int periods)
        {
            var returns = curve / curve.FirstValue();
            var cumulativeMax = Calculations.CumulativeMax(returns);
            var drawdown = 1 - (returns / cumulativeMax);

            var groups = cumulativeMax.GroupBy(kvp => kvp.Value);
            // In order, the items are: date, duration, cumulative max, max drawdown
            var drawdownGroups = new List<Tuple<DateTime, double, double, double>>();

            foreach (var group in groups.Values)
            {

                var firstDate = group.SortByKey().FirstKey();
                var lastDate = group.SortByKey().LastKey();

                var cumulativeMaxGroup = cumulativeMax.Between(firstDate, lastDate);
                var drawdownGroup = drawdown.Between(firstDate, lastDate);
                var drawdownGroupMax = drawdownGroup.Values.Max();

                var drawdownMax = drawdownGroup.Where(kvp => kvp.Value == drawdownGroupMax);

                drawdownGroups.Add(new Tuple<DateTime, double, double, double>(
                    drawdownMax.FirstKey(),
                    group.ValueCount,
                    cumulativeMaxGroup.FirstValue(),
                    drawdownMax.FirstValue()
                ));
            }

            var drawdowns = new Series<DateTime, double>(drawdownGroups.Select(x => x.Item1), drawdownGroups.Select(x => x.Item4));
            // Sort by negative drawdown value (in ascending order), which leaves it sorted in descending order 😮
            var sortedDrawdowns = drawdowns.SortBy(x => -x);
            // Only get the most we're allowed to take so that we don't overflow trying to get more drawdown items than exist
            var periodsToTake = periods < sortedDrawdowns.ValueCount ? periods : sortedDrawdowns.ValueCount;

            // Again, in order, the items are: date (Item1), duration (Item2), cumulative max (Item3), max drawdown (Item4).
            var topDrawdowns = new Series<DateTime, double>(sortedDrawdowns.Keys.Take(periodsToTake), sortedDrawdowns.Values.Take(periodsToTake));
            var topDurations = new Series<DateTime, double>(topDrawdowns.Keys.OrderBy(x => x), drawdownGroups.Where(t => topDrawdowns.Keys.Contains(t.Item1)).OrderBy(x => x.Item1).Select(x => x.Item2));
            var topCumulativeMax = new Series<DateTime, double>(topDrawdowns.Keys.OrderBy(x => x), drawdownGroups.Where(t => topDrawdowns.Keys.Contains(t.Item1)).OrderBy(x => x.Item1).Select(x => x.Item3));

            var frame = Frame.CreateEmpty<DateTime, string>();

            frame.AddColumn("duration", topDurations);
            frame.AddColumn("cumulativeMax", topCumulativeMax);
            frame.AddColumn("drawdown", topDrawdowns);

            return frame;
        }

        /// <summary>
        /// Gets the given drawdown periods from the equity curve and the set periods
        /// </summary>
        /// <param name="curve">Equity curve</param>
        /// <param name="periods">Top N drawdown periods to get</param>
        /// <returns>Enumerable of DrawdownPeriod</returns>
        public static IEnumerable<DrawdownPeriod> GetDrawdownPeriods(Series<DateTime, double> curve, int periods = 5)
        {
            var frame = GetUnderwaterFrame(curve);
            var topDrawdowns = GetTopWorstDrawdowns(curve, periods);

            for (var i = 1; i <= topDrawdowns.RowCount; i++)
            {
                var data = DrawdownGroup(frame, topDrawdowns["cumulativeMax"].GetAt(i - 1));

                // Tuple is as follows: Start (Item1: DateTime), End (Item2: DateTime), Max Drawdown (Item3: double)
                yield return new DrawdownPeriod(data.Item1, data.Item2, data.Item3);
            }
        }

        private static Tuple<DateTime, DateTime, double> DrawdownGroup(Frame<DateTime, string> frame, double groupMax)
        {
            var drawdownGroup = frame["cumulativeMax"].Where(kvp => kvp.Value == groupMax);
            var groupDrawdown = frame["drawdown"].Where(kvp => drawdownGroup.Keys.Contains(kvp.Key)).Max();

            var groupStart = drawdownGroup.FirstKey();
            var groupEnd = drawdownGroup.LastKey();

            return new Tuple<DateTime, DateTime, double>(groupStart, groupEnd, groupDrawdown);
        }
    }
}
