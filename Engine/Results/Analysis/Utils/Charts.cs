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
 *
*/
using QuantConnect;
using QuantConnect.Algorithm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis.Utils
{

    /// <summary>
    /// Loads and aligns the backtest equity curve and the SPY benchmark curve.
    /// </summary>
    public static class Charts
    {
        public static (SortedList<DateTime, decimal> BacktestEquity, SortedList<DateTime, decimal> BenchmarkEquity) ReadEquityCurve(Result result, QCAlgorithm algorithm, ref List<string> timingLogs)
        {
            // ── 1. backtest equity from "Strategy Equity" chart ──────────────────
            var timer = Stopwatch.StartNew();

            SortedList<DateTime, decimal> equitySeries;
            if (result.Charts.TryGetValue("Strategy Equity", out var chart) &&
                chart.Series.TryGetValue("Equity", out var series))
            {
                equitySeries = new SortedList<DateTime, decimal>(
                    series.Values.Cast<Candlestick>()
                        .ToDictionary(
                            candle => candle.Time.ConvertFromUtc(TimeZones.EasternStandard),
                            candle => candle.Close ?? 0m));
            }
            else
            {
                equitySeries = new SortedList<DateTime, decimal>();
            }

            timingLogs.Add($"{timer.Elapsed} - Loading equity curve");

            // ── 2. Benchmark from SPY history ─────────────────────────────────────
            timer.Restart();
            var spy = algorithm.Symbol("SPY");

            timingLogs.Add($"{timer.Elapsed} - Creating SPY symbol");

            algorithm.Settings.DailyPreciseEndTime = false; // ensures history bars are aligned to midnight Eastern
            var historyStart = algorithm.StartDate - TimeSpan.FromDays(3);
            var historyEnd = algorithm.EndDate + TimeSpan.FromDays(1);
            var benchmarkSeries = new SortedList<DateTime, decimal>(
                algorithm.History(spy, historyStart, historyEnd, Resolution.Daily)
                    .ToDictionary(x => x.EndTime, x => x.Close));

            timingLogs.Add($"{timer.Elapsed} - Fetching SPY history");

            // ── 3. Align the two curves on the same timestamps ───────────────────
            var commonKeys = equitySeries.Keys.Intersect(benchmarkSeries.Keys).ToHashSet();
            var alignedEquity = new SortedList<DateTime, decimal>(equitySeries.Where(kv => commonKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
            var alignedBenchmark = new SortedList<DateTime, decimal>(benchmarkSeries.Where(kv => commonKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
            return (alignedEquity, alignedBenchmark);
        }
    }
}
