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
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Runs a reduced suite of backtest diagnostic tests periodically while the backtest is still running,
    /// against a snapshot of the current intermediate results.
    /// </summary>
    public class InRunResultsAnalyzer : ResultsAnalyzer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InRunResultsAnalyzer"/> class.
        /// </summary>
        /// <param name="result">A snapshot of the current intermediate backtest result to analyze.</param>
        /// <param name="algorithm">The algorithm instance used for history requests and settings.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <param name="logs">The list of log lines produced by the backtest so far.</param>
        public InRunResultsAnalyzer(Result result, QCAlgorithm algorithm, Language language, IReadOnlyList<string> logs)
            : base(result, algorithm, language, logs)
        {
        }

        /// <summary>
        /// The equity and benchmark curves are not built for in-run analysis:
        /// none of the in-run analyses read them, and building them would issue
        /// a benchmark history request on every run.
        /// </summary>
        protected override bool RequiresEquityCurves => false;

        /// <summary>
        /// Creates the set of diagnostic analyses to run while the backtest is in progress.
        /// Only analyses that read the result snapshot (logs, orders, order events, charts) are
        /// included: they are cheap, thread-safe, and detect error conditions whose findings
        /// don't depend on the backtest being complete. Curve and statistics based analyses are
        /// left to the final analysis, since partial-period statistics are noisy and require
        /// history requests.
        /// </summary>
        protected override IReadOnlyCollection<BaseResultsAnalysis> GetAnalyses() => new BaseResultsAnalysis[]
        {
            new PortfolioValueIsNotPositiveAnalysis(),
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
            new ExceededMaximumOrdersOrderResponseErrorAnalysis(),
            new UnsupportedOptionShortPositionExerciseAnalysis(),
            new UnsupportedOptionExerciseQuantityAnalysis(),
            new EuropeanOptionNotExpiredOnExerciseOrderResponseErrorAnalysis(),
            new OptionOrderOnStockSplitOrderResponseErrorAnalysis(),
            new MarketOnOpenNotAllowedDuringRegularHoursOrderResponseErrorAnalysis(),
            new OrderQuantityLessThanLotSizeOrderResponseErrorAnalysis(),
            new InsightsEmittedForDelistedSecuritiesAnalysis(),
            new PortfolioMarginUsageAnalysis(),
        };
    }
}
