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
using QuantConnect.Lean.Engine.Results.Analysis.Utils;
using System;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Runs the full suite of backtest diagnostic tests against a single backtest.
    /// Mirrors <c>process.py : BacktestAnalyzer</c>.
    /// </summary>
    public class BacktestAnalyzer
    {
        private readonly QCAlgorithm _algorithm;
        private readonly Language _language;
        private readonly IReadOnlyList<string> _logs;
        private SortedList<DateTime, decimal> _equityCurve;
        private SortedList<DateTime, decimal> _benchmarkEquityCurve;
        private Result _result;

        public BacktestAnalyzer(Result result, QCAlgorithm algorithm, Language language, IReadOnlyList<string> logs)
        {
            _result = result;
            _algorithm = algorithm;
            _language = language;
            _logs = logs;
        }

        // ── Test chain ────────────────────────────────────────────────────────────

        public List<BacktestAnalysisResult> RunTestChain(int timeLimitSeconds = 5, int maxFailedTests = 3)
        {
            var timingLogs = new List<string>();
            (_equityCurve, _benchmarkEquityCurve) = Charts.ReadEquityCurve(_result, _algorithm, ref timingLogs);

            var tests = new List<Func<IReadOnlyList<BacktestAnalysisResult>>>
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
                //CheckTakeProfitAndStopLossOrders,
                CheckMarginCalls,
                CheckExecutionSpeed,
                CheckStaleOrderFills,
                CheckForOrderFillsDuringExtendedMarketHours,
                //CheckPortfolioMarginUsage,
                CheckParameterCount,
                CheckCrisisEvents,
                CheckStatisticalSignificanceOfDailyReturns,
                //CheckPerformanceRelativeToBenchmark,
                //CheckMonteCarloPercentile,
            };
            //var tests = new List<Func<IReadOnlyList<BacktestAnalysisResult>>>();

            var responses = new List<BacktestAnalysisResult>();
            var startTime = DateTime.UtcNow;

            foreach (var test in tests)
            {
                var results = test();
                foreach (var r in results)
                {
                    if (r.PotentialSolutions.Count > 0)
                        responses.Add(r);

                    if ((DateTime.UtcNow - startTime).TotalSeconds >= timeLimitSeconds)
                        return responses;

                    if (responses.Count >= maxFailedTests)
                        return responses;
                }
            }

            timingLogs.Add($"{DateTime.UtcNow - startTime} - Total analysis time");
            return responses;
        }

        // ── Individual check methods ──────────────────────────────────────────────

        public IReadOnlyList<BacktestAnalysisResult> CheckFlatEquityCurve()
            => new FlatEquityCurveAnalysis().Run(_equityCurve);

        public IReadOnlyList<BacktestAnalysisResult> CheckPortfolioValueIsNotPositive()
            => new PortfolioValueIsNotPositiveAnalysis().Run(_result);

        public IReadOnlyList<BacktestAnalysisResult> CheckInsufficientBuyingPowerOrderResponseError()
            => new InsufficientBuyingPowerOrderResponseErrorAnalysis().Run(_result.OrderEvents, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckAlgorithmWarmingUpOrderResponseError()
            => new AlgorithmWarmingUpOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckBrokerageModelRefusedToSubmitOrderOrderResponseError()
            => new BrokerageModelRefusedToSubmitOrderOrderResponseErrorAnalysis().Run(_result.OrderEvents, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckExceedsShortableQuantityOrderResponseError()
            => new ExceedsShortableQuantityOrderResponseErrorAnalysis().Run(_result.OrderEvents, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckNonTradableSecurityOrderResponseError()
            => new NonTradableSecurityOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckOrderQuantityZeroOrderResponseError()
            => new OrderQuantityZeroOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckSecurityPriceZeroOrderResponseError()
            => new SecurityPriceZeroOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckUnsupportedRequestTypeOrderResponseError()
            => new UnsupportedRequestTypeOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckExchangeNotOpenOrderResponseError()
            => new ExchangeNotOpenOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckForexConversionRateZeroOrderResponseError()
            => new ForexConversionRateZeroOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckExceededMaximumOrdersOrderResponseError()
            => new ExceededMaximumOrdersOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckBrokerageModelRefusedToUpdateOrderOrderResponseError()
            => new BrokerageModelRefusedToUpdateOrderOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckOrderQuantityLessThanLotSizeOrderResponseError()
            => new OrderQuantityLessThanLotSizeOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckEuropeanOptionNotExpiredOnExerciseOrderResponseError()
            => new EuropeanOptionNotExpiredOnExerciseOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckOptionOrderOnStockSplitOrderResponseError()
            => new OptionOrderOnStockSplitOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckMarketOnOpenNotAllowedDuringRegularHoursOrderResponseError()
            => new MarketOnOpenNotAllowedDuringRegularHoursOrderResponseErrorAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckInsightsEmittedForDelistedSecurities()
            => new InsightsEmittedForDelistedSecuritiesAnalysis().Run(_logs, _language);

        //public IReadOnlyList<BacktestAnalysisResult> CheckTakeProfitAndStopLossOrders()
        //    => new TakeProfitAndStopLossOrdersAnalysis().Run(_result.Orders.Values.ToList(), _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckMarginCalls()
            => new MarginCallsAnalysis().Run(_logs, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckExecutionSpeed()
            => new ExecutionSpeedAnalysis().Run(_logs);

        public IReadOnlyList<BacktestAnalysisResult> CheckStaleOrderFills()
            => new StaleOrderFillsAnalysis().Run(_result.OrderEvents, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckForOrderFillsDuringExtendedMarketHours()
            => new OrderFillsDuringExtendedMarketHoursAnalysis().Run(_algorithm, _result.OrderEvents, _language);

        //public IReadOnlyList<BacktestAnalysisResult> CheckPortfolioMarginUsage()
        //    => new PortfolioMarginUsageAnalysis().Run(_api, _backtest);

        public IReadOnlyList<BacktestAnalysisResult> CheckParameterCount()
            => new ParameterCountAnalysis().Run(_algorithm, _language);

        public IReadOnlyList<BacktestAnalysisResult> CheckCrisisEvents()
            => new CrisisEventsAnalysis().Run(_algorithm, _equityCurve, _benchmarkEquityCurve);

        public IReadOnlyList<BacktestAnalysisResult> CheckStatisticalSignificanceOfDailyReturns()
            => new StatisticalSignificanceOfDailyReturnsAnalysis().Run(_equityCurve, _benchmarkEquityCurve);

        //public IReadOnlyList<BacktestAnalysisResult> CheckPerformanceRelativeToBenchmark()
        //    => new PerformanceRelativeToBenchmark().Run(_algorithm, _equityCurve, _benchmarkEquityCurve);

        //public IReadOnlyList<BacktestAnalysisResult> CheckMonteCarloPercentile()
        //    => new MonteCarloPercentile().Run(_equityCurve);
    }
}
