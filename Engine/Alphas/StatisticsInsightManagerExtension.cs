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

using System;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Interfaces;
using QuantConnect.Statistics;

namespace QuantConnect.Lean.Engine.Alphas
{
    /// <summary>
    /// Manages alpha statistics responsbilities
    /// </summary>
    public class StatisticsInsightManagerExtension : IInsightManagerExtension
    {
        private readonly double _smoothingFactor;
        private readonly int _rollingAverageIsReadyCount;
        private readonly bool _requireRollingAverageWarmup;
        private readonly decimal _tradablePercentOfVolume;
        private readonly KellyCriterionManager _kellyCriterionManager;
        private DateTime _lastKellyCriterionUpdate;

        /// <summary>
        /// Gets the current statistics. The values are current as of the time specified
        /// in <see cref="AlphaRuntimeStatistics.MeanPopulationScore"/> and <see cref="AlphaRuntimeStatistics.RollingAveragedPopulationScore"/>
        /// </summary>
        public AlphaRuntimeStatistics Statistics { get; }

        /// <summary>
        /// Gets whether or not the rolling average statistics is ready
        /// </summary>
        public bool RollingAverageIsReady => !_requireRollingAverageWarmup || Statistics.TotalInsightsAnalysisCompleted >= _rollingAverageIsReadyCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatisticsInsightManagerExtension"/> class
        /// </summary>
        /// <param name="accountCurrencyProvider">The account currency provider</param>
        /// <param name="tradablePercentOfVolume">Percent of volume of first bar used to estimate the maximum number of tradable shares. Defaults to 1%</param>
        /// <param name="period">The period used for exponential smoothing of scores - this is a number of insights. Defaults to 100 insight predictions.</param>
        /// <param name="requireRollingAverageWarmup">Specify true to force the population average scoring to warmup before plotting.</param>
        public StatisticsInsightManagerExtension(
            IAccountCurrencyProvider accountCurrencyProvider,
            decimal tradablePercentOfVolume = 0.01m,
            int period = 100,
            bool requireRollingAverageWarmup = false)
        {
            Statistics = new AlphaRuntimeStatistics(accountCurrencyProvider);
            _tradablePercentOfVolume = tradablePercentOfVolume;
            _smoothingFactor = 2.0 / (period + 1.0);

            // use normal ema warmup period
            _rollingAverageIsReadyCount = period;
            _requireRollingAverageWarmup = requireRollingAverageWarmup;

            _kellyCriterionManager = new KellyCriterionManager();
        }

        /// <summary>
        /// Handles the <see cref="IAlgorithm.InsightsGenerated"/> event
        /// Increments total, long and short counters. Updates long/short ratio
        /// </summary>
        /// <param name="context">The newly generated insight context</param>
        public void OnInsightGenerated(InsightAnalysisContext context)
        {
            // incremement total insight counter
            Statistics.TotalInsightsGenerated++;

            // update long/short ratio statistics
            if (context.Insight.Direction == InsightDirection.Up)
            {
                Statistics.LongCount++;
            }
            else if (context.Insight.Direction == InsightDirection.Down)
            {
                Statistics.ShortCount++;
            }
        }

        /// <summary>
        /// Computes an estimated value for the insight. This is intended to be invoked at the end of the
        /// insight period, i.e, when now == insight.GeneratedTimeUtc + insight.Period;
        /// </summary>
        /// <param name="context">Context whose insight has just closed</param>
        public void OnInsightClosed(InsightAnalysisContext context)
        {
            // increment closed insight counter
            Statistics.TotalInsightsClosed += 1;

            // tradable volume (purposefully includes fractional shares)
            var volume = _tradablePercentOfVolume * context.InitialValues.Volume;

            // value of the entering the trade in the account currency
            var enterValue = context.InitialValues.Price * context.InitialValues.QuoteCurrencyConversionRate;

            // value of exiting the trade in the account currency
            var exitValue = context.CurrentValues.Price * context.CurrentValues.QuoteCurrencyConversionRate;

            // total value delta between enter and exit values
            var insightValue = (int)context.Insight.Direction * (exitValue - enterValue);

            var insightValueFactoredByTradableVolume = insightValue * volume;

            context.Insight.EstimatedValue = insightValueFactoredByTradableVolume;
            Statistics.TotalAccumulatedEstimatedAlphaValue += insightValueFactoredByTradableVolume;

            // just in case..
            if (enterValue != 0)
            {
                _kellyCriterionManager.AddNewValue(
                    (int)context.Insight.Direction * (exitValue / enterValue - 1),
                    context.Insight.GeneratedTimeUtc);
            }
        }

        /// <summary>
        /// Updates the specified statistics with the new scores
        /// </summary>
        /// <param name="context">Context whose insight has just completed analysis</param>
        public void OnInsightAnalysisCompleted(InsightAnalysisContext context)
        {
            // increment analysis completed counter
            Statistics.TotalInsightsAnalysisCompleted += 1;

            foreach (var scoreType in InsightManager.ScoreTypes)
            {
                if (!context.ShouldAnalyze(scoreType))
                {
                    continue;
                }
                var score = context.Score.GetScore(scoreType);
                var currentTime = context.CurrentValues.TimeUtc;

                // online population average
                var mean = Statistics.MeanPopulationScore.GetScore(scoreType);
                var newMean = mean + (score - mean) / Statistics.TotalInsightsAnalysisCompleted;
                Statistics.MeanPopulationScore.SetScore(scoreType, newMean, currentTime);

                var newEma = newMean;
                if (Statistics.TotalInsightsAnalysisCompleted > 4)
                {
                    // compute the traditional ema
                    var ema = Statistics.RollingAveragedPopulationScore.GetScore(scoreType);
                    newEma = score * _smoothingFactor + ema * (1 - _smoothingFactor);
                }
                Statistics.RollingAveragedPopulationScore.SetScore(scoreType, newEma, currentTime);
            }
        }

        /// <summary>
        /// Invokes the manager at the end of the time step.
        /// </summary>
        /// <param name="frontierTimeUtc">The current frontier time utc</param>
        public void Step(DateTime frontierTimeUtc)
        {
            Statistics.SetDate(frontierTimeUtc);

            if (_lastKellyCriterionUpdate.Date != frontierTimeUtc)
            {
                _lastKellyCriterionUpdate = frontierTimeUtc;

                _kellyCriterionManager.UpdateScores();

                Statistics.KellyCriterionEstimate = _kellyCriterionManager.KellyCriterionEstimate;
                Statistics.KellyCriterionProbabilityValue = _kellyCriterionManager.KellyCriterionProbabilityValue;
            }
        }

        /// <summary>
        /// Allows the extension to initialize itself over the expected range
        /// </summary>
        /// <param name="algorithmStartDate">The start date of the algorithm</param>
        /// <param name="algorithmEndDate">The end date of the algorithm</param>
        /// <param name="algorithmUtcTime">The algorithm's current utc time</param>
        public void InitializeForRange(DateTime algorithmStartDate, DateTime algorithmEndDate, DateTime algorithmUtcTime)
        {
            Statistics.SetStartDate(algorithmStartDate);
        }
    }
}