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
        /// <param name="strategySeries">Equity curve with both live and backtesting merged</param>
        /// <param name="periods">Periods this collection contains</param>
        public DrawdownCollection(Series<DateTime, double> strategySeries, int periods)
        {
            var drawdowns = GetDrawdownPeriods(strategySeries, periods).ToList();

            Periods = periods;
            Start = strategySeries.IsEmpty ? DateTime.MinValue : strategySeries.FirstKey();
            End = strategySeries.IsEmpty ? DateTime.MaxValue : strategySeries.LastKey();
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
            return new DrawdownCollection(NormalizeResults(backtestResult, liveResult), periods);
        }

        /// <summary>
        /// Normalizes the Series used to calculate the drawdown plots and charts
        /// </summary>
        /// <param name="backtestResult">Backtest result packet</param>
        /// <param name="liveResult">Live result packet</param>
        /// <returns></returns>
        public static Series<DateTime, double> NormalizeResults(BacktestResult backtestResult, LiveResult liveResult)
        {
            var backtestPoints = ResultsUtil.EquityPoints(backtestResult);
            var livePoints = ResultsUtil.EquityPoints(liveResult);

            if (backtestPoints.Count == 0 && livePoints.Count == 0)
            {
                return new Series<DateTime, double>(new DateTime[] { }, new double[] { });
            }

            var startingEquity = backtestPoints.Count == 0 ? livePoints.First().Value : backtestPoints.First().Value;
            var backtestSeries = new Series<DateTime, double>(backtestPoints)
                .PercentChange()
                .Where(kvp => kvp.Value != 0)
                .CumulativeSum();

            var liveSeries = new Series<DateTime, double>(livePoints)
                .PercentChange()
                .CumulativeSum();

            // Get the last key of the backtest series if our series is empty to avoid issues with empty frames
            var firstLiveKey = liveSeries.IsEmpty ? backtestSeries.LastKey().AddDays(1) : liveSeries.FirstKey();

            // Add the final non-overlapping point of the backtest equity curve to the entire live series to keep continuity.
            if (!backtestSeries.IsEmpty)
            {
                var filtered = backtestSeries.Where(kvp => kvp.Key < firstLiveKey);
                liveSeries = filtered.IsEmpty ? liveSeries : liveSeries + filtered.LastValue();
            }

            // Prefer the live values as we don't care about backtest once we've deployed into live.
            // All in all, this is a normalized equity curve, though it's been normalized
            // so that there are no discontinuous jumps in equity value if we only used equity cash
            // to add the last value of the backtest series to the live series.
            //
            // Pandas equivalent:
            //
            // ```
            // pd.concat([backtestSeries, liveSeries], axis=1).fillna(method='ffill').dropna().diff().add(1).cumprod().mul(startingEquity)
            // ```
            return backtestSeries.Merge(liveSeries, UnionBehavior.PreferRight)
                .FillMissing(Direction.Forward)
                .DropMissing()
                .Diff(1)
                .SelectValues(x => x + 1)
                .CumulativeProduct()
                .SelectValues(x => x * startingEquity);
        }

        /// <summary>
        /// Gets the underwater plot for the provided curve.
        /// Data is expected to be the concatenated output of <see cref="ResultsUtil.EquityPoints"/>.
        /// </summary>
        /// <param name="curve">Equity curve</param>
        /// <returns></returns>
        public static Series<DateTime, double> GetUnderwater(Series<DateTime, double> curve)
        {
            if (curve.IsEmpty)
            {
                return curve;
            }

            var returns = curve / curve.FirstValue();
            var cumulativeMax = returns.CumulativeMax();

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
            var frame = Frame.CreateEmpty<DateTime, string>();
            if (curve.IsEmpty)
            {
                return frame;
            }

            var returns = curve / curve.FirstValue();
            var cumulativeMax = returns.CumulativeMax();
            var drawdown = 1 - (returns / cumulativeMax);

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
            var frame = Frame.CreateEmpty<DateTime, string>();
            if (curve.IsEmpty)
            {
                return frame;
            }

            var returns = curve / curve.FirstValue();
            var cumulativeMax = returns.CumulativeMax();
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
            var drawdownAfter = frame["cumulativeMax"].Where(kvp => kvp.Value > groupMax);
            var drawdownGroup = frame["cumulativeMax"].Where(kvp => kvp.Value == groupMax);
            var groupDrawdown = frame["drawdown"].Realign(drawdownGroup.Keys).Max();

            var groupStart = drawdownGroup.FirstKey();
            // Get the start of the next period if it exists. That is when the drawdown period has officially ended.
            // We do this to extend the drawdown period enough so that missing values don't stop it early.
            var groupEnd = drawdownAfter.IsEmpty ? drawdownGroup.LastKey() : drawdownAfter.FirstKey();

            return new Tuple<DateTime, DateTime, double>(groupStart, groupEnd, groupDrawdown);
        }
    }
}
