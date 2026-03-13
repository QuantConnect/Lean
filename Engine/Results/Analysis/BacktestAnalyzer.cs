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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Runs the full suite of backtest diagnostic tests against a single backtest.
    /// </summary>
    public class BacktestAnalyzer
    {
        private readonly QCAlgorithm _algorithm;
        private readonly Language _language;
        private readonly IReadOnlyList<string> _logs;
        private SortedList<DateTime, decimal> _equityCurve;
        private SortedList<DateTime, decimal> _benchmarkEquityCurve;
        private Result _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="BacktestAnalyzer"/> class.
        /// </summary>
        /// <param name="result">The backtest result to analyze.</param>
        /// <param name="algorithm">The algorithm instance used for history requests and settings.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <param name="logs">The full list of log lines produced by the backtest.</param>
        public BacktestAnalyzer(Result result, QCAlgorithm algorithm, Language language, IReadOnlyList<string> logs)
        {
            _result = result;
            _algorithm = algorithm;
            _language = language;
            _logs = logs;
        }

        // ── Test chain ────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs all registered diagnostic checks against the backtest in order,
        /// stopping early when the time limit or maximum failure count is reached.
        /// </summary>
        /// <param name="timeLimitSeconds">Wall-clock seconds allowed for the full chain before early exit.</param>
        /// <param name="maxFailedTests">Maximum number of analyses with solutions before early exit.</param>
        /// <returns>A list of <see cref="BacktestAnalysisResult"/> entries that have at least one potential solution.</returns>
        public IReadOnlyList<BacktestAnalysisResult> RunTestChain(int timeLimitSeconds = 5, int maxFailedTests = 3)
        {
            var timingLogs = new List<string>();
            (_equityCurve, _benchmarkEquityCurve) = ReadEquityCurve(_result, _algorithm, timingLogs);

            var analyses = new List<Func<IReadOnlyList<BacktestAnalysisResult>>>
            {
                CheckFlatEquityCurve,
                CheckPortfolioValueIsNotPositive,
                CheckInsufficientBuyingPowerOrderResponseError,
                CheckAlgorithmWarmingUpOrderResponseError,
                CheckBrokerageModelRefusedToSubmitOrderOrderResponseError,
                CheckExceedsShortableQuantityOrderResponseError,
                CheckNonTradableSecurityOrderResponseError,
                CheckOrderQuantityZeroOrderResponseError,
                CheckSecurityPriceZeroOrderResponseError,
                CheckUnsupportedRequestTypeOrderResponseError,
                CheckExchangeNotOpenOrderResponseError,
                CheckForexConversionRateZeroOrderResponseError,
                CheckExceededMaximumOrdersOrderResponseError,
                CheckBrokerageModelRefusedToUpdateOrderOrderResponseError,
                CheckOrderQuantityLessThanLotSizeOrderResponseError,
                CheckEuropeanOptionNotExpiredOnExerciseOrderResponseError,
                CheckOptionOrderOnStockSplitOrderResponseError,
                CheckMarketOnOpenNotAllowedDuringRegularHoursOrderResponseError,
                CheckInsightsEmittedForDelistedSecurities,
                CheckTakeProfitAndStopLossOrders,
                CheckMarginCalls,
                CheckExecutionSpeed,
                CheckStaleOrderFills,
                CheckForOrderFillsDuringExtendedMarketHours,
                CheckPortfolioMarginUsage,
                CheckParameterCount,
                CheckCrisisEvents,
                CheckStatisticalSignificanceOfDailyReturns,
                CheckPerformanceRelativeToBenchmark,
                CheckMonteCarloPercentile,
            };

            // TODO: REMOVE THIS!
            timeLimitSeconds = 100;
            maxFailedTests = 100;

            var responses = new ConcurrentBag<BacktestAnalysisResult>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeLimitSeconds));
            var timer = Stopwatch.StartNew();

            try
            {
                Parallel.ForEach(analyses, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cts.Token }, analysis =>
                {
                    foreach (var result in analysis())
                    {
                        if (result.PotentialSolutions.Count > 0)
                        {
                            responses.Add(result);
                            if (responses.Count >= maxFailedTests || timer.Elapsed.TotalSeconds >= timeLimitSeconds)
                            {
                                cts.Cancel();
                            }
                        }
                    }
                });
            }
            catch (OperationCanceledException) { }

            return [.. responses.Take(maxFailedTests)];
        }

        private IReadOnlyList<BacktestAnalysisResult> CheckFlatEquityCurve()
            => new FlatEquityCurveAnalysis().Run(_equityCurve);

        private IReadOnlyList<BacktestAnalysisResult> CheckPortfolioValueIsNotPositive()
            => new PortfolioValueIsNotPositiveAnalysis().Run(_result);

        private IReadOnlyList<BacktestAnalysisResult> CheckInsufficientBuyingPowerOrderResponseError()
            => new InsufficientBuyingPowerOrderResponseErrorAnalysis().Run(_result.OrderEvents, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckAlgorithmWarmingUpOrderResponseError()
            => new AlgorithmWarmingUpOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckBrokerageModelRefusedToSubmitOrderOrderResponseError()
            => new BrokerageModelRefusedToSubmitOrderOrderResponseErrorAnalysis().Run(_result.OrderEvents, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckExceedsShortableQuantityOrderResponseError()
            => new ExceedsShortableQuantityOrderResponseErrorAnalysis().Run(_result.OrderEvents, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckNonTradableSecurityOrderResponseError()
            => new NonTradableSecurityOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckOrderQuantityZeroOrderResponseError()
            => new OrderQuantityZeroOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckSecurityPriceZeroOrderResponseError()
            => new SecurityPriceZeroOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckUnsupportedRequestTypeOrderResponseError()
            => new UnsupportedRequestTypeOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckExchangeNotOpenOrderResponseError()
            => new ExchangeNotOpenOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckForexConversionRateZeroOrderResponseError()
            => new ForexConversionRateZeroOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckExceededMaximumOrdersOrderResponseError()
            => new ExceededMaximumOrdersOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckBrokerageModelRefusedToUpdateOrderOrderResponseError()
            => new BrokerageModelRefusedToUpdateOrderOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckOrderQuantityLessThanLotSizeOrderResponseError()
            => new OrderQuantityLessThanLotSizeOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckEuropeanOptionNotExpiredOnExerciseOrderResponseError()
            => new EuropeanOptionNotExpiredOnExerciseOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckOptionOrderOnStockSplitOrderResponseError()
            => new OptionOrderOnStockSplitOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckMarketOnOpenNotAllowedDuringRegularHoursOrderResponseError()
            => new MarketOnOpenNotAllowedDuringRegularHoursOrderResponseErrorAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckInsightsEmittedForDelistedSecurities()
            => new InsightsEmittedForDelistedSecuritiesAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckTakeProfitAndStopLossOrders()
            => new TakeProfitAndStopLossOrdersAnalysis().Run(_result.Orders.Values, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckMarginCalls()
            => new MarginCallsAnalysis().Run(_logs, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckExecutionSpeed()
            => new ExecutionSpeedAnalysis().Run(_logs);

        private IReadOnlyList<BacktestAnalysisResult> CheckStaleOrderFills()
            => new StaleOrderFillsAnalysis().Run(_result.OrderEvents, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckForOrderFillsDuringExtendedMarketHours()
            => new OrderFillsDuringExtendedMarketHoursAnalysis().Run(_algorithm, _result.OrderEvents, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckPortfolioMarginUsage()
            => new PortfolioMarginUsageAnalysis().Run(_result);

        private IReadOnlyList<BacktestAnalysisResult> CheckParameterCount()
            => new ParameterCountAnalysis().Run(_algorithm, _language);

        private IReadOnlyList<BacktestAnalysisResult> CheckCrisisEvents()
            => new CrisisEventsAnalysis().Run(_algorithm, _equityCurve, _benchmarkEquityCurve);

        private IReadOnlyList<BacktestAnalysisResult> CheckStatisticalSignificanceOfDailyReturns()
            => new StatisticalSignificanceOfDailyReturnsAnalysis().Run(_equityCurve, _benchmarkEquityCurve);

        private IReadOnlyList<BacktestAnalysisResult> CheckPerformanceRelativeToBenchmark()
            => new PerformanceRelativeToBenchmark().Run(_algorithm, _equityCurve, _benchmarkEquityCurve);

        private IReadOnlyList<BacktestAnalysisResult> CheckMonteCarloPercentile()
            => new MonteCarloPercentile().Run(_equityCurve);

        /// <summary>
        /// Reads the backtest's "Strategy Equity" chart and fetches SPY daily history to build
        /// two time-aligned equity curves: one for the backtest and one for the benchmark.
        /// </summary>
        /// <param name="result">The backtest result containing the charts.</param>
        /// <param name="algorithm">The algorithm instance used to retrieve SPY history.</param>
        /// <param name="timingLogs">List that receives timing log messages for each step.</param>
        /// <returns>
        /// A tuple of two <see cref="SortedList{TKey,TValue}"/> instances sharing the same timestamp keys:
        /// the first is the backtest equity curve, the second is the SPY benchmark curve.
        /// </returns>
        private static (SortedList<DateTime, decimal> BacktestEquity, SortedList<DateTime, decimal> BenchmarkEquity) ReadEquityCurve(Result result, QCAlgorithm algorithm, List<string> timingLogs)
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
