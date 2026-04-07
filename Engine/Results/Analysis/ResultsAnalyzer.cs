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
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Runs the full suite of backtest diagnostic tests against a single backtest.
    /// </summary>
    public class ResultsAnalyzer
    {
        private readonly QCAlgorithm _algorithm;
        private readonly Language _language;
        private readonly IReadOnlyList<string> _logs;
        private SortedList<DateTime, decimal> _equityCurve;
        private SortedList<DateTime, decimal> _benchmarkEquityCurve;
        private Result _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultsAnalyzer"/> class.
        /// </summary>
        /// <param name="result">The backtest result to analyze.</param>
        /// <param name="algorithm">The algorithm instance used for history requests and settings.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <param name="logs">The full list of log lines produced by the backtest.</param>
        public ResultsAnalyzer(Result result, QCAlgorithm algorithm, Language language, IReadOnlyList<string> logs)
        {
            _result = result;
            _algorithm = algorithm;
            _language = language;
            _logs = logs;
        }

        // ── Test chain ────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs all registered diagnostic checks against the backtest in weight order,
        /// stopping early when the time limit or maximum failure count is reached.
        /// </summary>
        /// <param name="timeLimitSeconds">Wall-clock seconds allowed for the full chain before early exit.</param>
        /// <param name="maxFailedAnalyses">Maximum number of failing analyses to collect before stopping; also the max returned.</param>
        /// <returns>Up to <paramref name="maxFailedAnalyses"/> <see cref="QuantConnect.Analysis"/> entries with solutions, ranked by weight.</returns>
        public IReadOnlyList<QuantConnect.Analysis> Run(int timeLimitSeconds = 5, int maxFailedAnalyses = 10)
        {
            (_equityCurve, _benchmarkEquityCurve) = ReadEquityCurve(_result, _algorithm);

            var parameters = new ResultsAnalysisRunParameters(_result, _algorithm, _language, _logs, _equityCurve, _benchmarkEquityCurve);

            // Instances are sorted by their own Weight — changing a weight automatically reorders execution.
            var analyses = new BaseResultsAnalysis[]
            {
                new PortfolioValueIsNotPositiveAnalysis(),
                new FlatEquityCurveAnalysis(),
                new InsufficientBuyingPowerOrderResponseErrorAnalysis(),
                new MarginCallsAnalysis(),
                new ExceedsShortableQuantityOrderResponseErrorAnalysis(),
                new SecurityPriceZeroOrderResponseErrorAnalysis(),
                new OrderQuantityZeroOrderResponseErrorAnalysis(),
                new NonTradableSecurityOrderResponseErrorAnalysis(),
                new BrokerageModelRefusedToSubmitOrderOrderResponseErrorAnalysis(),
                new BrokerageModelRefusedToUpdateOrderOrderResponseErrorAnalysis(),
                new TakeProfitAndStopLossOrdersAnalysis(),
                new StaleOrderFillsAnalysis(),
                new AlgorithmWarmingUpOrderResponseErrorAnalysis(),
                new ExchangeNotOpenOrderResponseErrorAnalysis(),
                new ForexConversionRateZeroOrderResponseErrorAnalysis(),
                new OrderFillsDuringExtendedMarketHoursAnalysis(),
                new ExceededMaximumOrdersOrderResponseErrorAnalysis(),
                new UnsupportedOptionShortPositionExerciseAnalysis(),
                new UnsupportedOptionExerciseQuantityAnalysis(),
                new EuropeanOptionNotExpiredOnExerciseOrderResponseErrorAnalysis(),
                new OptionOrderOnStockSplitOrderResponseErrorAnalysis(),
                new MarketOnOpenNotAllowedDuringRegularHoursOrderResponseErrorAnalysis(),
                new OrderQuantityLessThanLotSizeOrderResponseErrorAnalysis(),
                new InsightsEmittedForDelistedSecuritiesAnalysis(),
                new StatisticalSignificanceOfDailyReturnsAnalysis(),
                new PerformanceRelativeToBenchmarkAnalysis(),
                new CrisisEventsAnalysis(),
                new ExecutionSpeedAnalysis(),
                new PortfolioMarginUsageAnalysis(),
                new ParameterCountAnalysis(),
                new MonteCarloPercentileAnalysis(),
            }.OrderByDescending(a => a.Weight);

            var responses = new List<QuantConnect.Analysis>();
            var timer = Stopwatch.StartNew();
            var timeLimit = TimeSpan.FromSeconds(timeLimitSeconds);

            foreach (var analysis in analyses)
            {
                if (responses.Count >= maxFailedAnalyses)
                {
                    Log.Trace($"ResultsAnalyzer: reached maximum failed analyses count of {maxFailedAnalyses}, stopping analysis chain.");
                    break;
                }
                if (timer.Elapsed >= timeLimit)
                {
                    Log.Trace($"ResultsAnalyzer: reached time limit of {timeLimitSeconds} seconds (current running time: {timer.Elapsed.TotalSeconds}s), stopping analysis chain.");
                    break;
                }

                foreach (var result in analysis.Run(parameters))
                {
                    if (result.Solutions.Count > 0)
                    {
                        responses.Add(result);
                    }
                }
            }

            return responses;
        }

        /// <summary>
        /// Reads the backtest's "Strategy Equity" chart and fetches SPY daily history to build
        /// two time-aligned equity curves: one for the backtest and one for the benchmark.
        /// </summary>
        /// <param name="result">The backtest result containing the charts.</param>
        /// <param name="algorithm">The algorithm instance used to retrieve SPY history.</param>
        /// <returns>
        /// A tuple of two <see cref="SortedList{TKey,TValue}"/> instances sharing the same timestamp keys:
        /// the first is the backtest equity curve, the second is the SPY benchmark curve.
        /// </returns>
        private static (SortedList<DateTime, decimal> BacktestEquity, SortedList<DateTime, decimal> BenchmarkEquity) ReadEquityCurve(Result result, QCAlgorithm algorithm)
        {
            // ── 1. backtest equity from "Strategy Equity" chart ──────────────────
            BaseSeries equitySeries;
            if (result.Charts.TryGetValue("Strategy Equity", out var chart) &&
                chart.Series.TryGetValue("Equity", out var series))
            {
                equitySeries = series;
            }
            else
            {
                return (new(), new());
            }

            // ── 2. Benchmark from SPY history ─────────────────────────────────────
            var spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var exchangeTimeZone = MarketHoursDatabase.FromDataFolder()
                .GetExchangeHours(spy.ID.Market, spy, spy.ID.SecurityType)
                .TimeZone;

            var benchmarkSeries = new SortedList<DateTime, decimal>();
            var historyStart = algorithm.StartDate - TimeSpan.FromDays(3);
            var historyEnd = algorithm.EndDate + TimeSpan.FromDays(1);
            foreach (var bar in algorithm.History(spy, historyStart, historyEnd, Resolution.Daily))
            {
                var time = algorithm.Settings.DailyPreciseEndTime ? bar.EndTime.AddDays(1).Date : bar.EndTime;
                benchmarkSeries.Add(time.Date.ConvertToUtc(exchangeTimeZone), bar.Close);
            }

            // ── 3. Resample both to daily data points ────────────────────────────
            var sampler = new SeriesSampler(TimeSpan.FromDays(1));
            var start = new DateTime(
                Math.Max(equitySeries.Values.First().Time.Ticks, benchmarkSeries.Keys.First().Ticks));
            var stop = new DateTime(
                Math.Min(equitySeries.Values.Last().Time.Ticks, benchmarkSeries.Keys.Last().Ticks));

            var sampledEquity = sampler.Sample(equitySeries, start, stop);

            // ── 4. Align the two curves on the same timestamps ───────────────────
            var equityByTime = new SortedList<DateTime, decimal>(
                sampledEquity.Values.ToDictionary(
                    p => p.Time,
                    p => p is Candlestick c ? c.Close ?? 0m : ((ChartPoint)p).Y ?? 0m));

            var commonKeys = equityByTime.Keys.Intersect(benchmarkSeries.Keys).ToHashSet();
            var alignedEquity = new SortedList<DateTime, decimal>(
                equityByTime.Where(kv => commonKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
            var alignedBenchmark = new SortedList<DateTime, decimal>(
                benchmarkSeries.Where(kv => commonKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

            return (alignedEquity, alignedBenchmark);
        }
    }
}
