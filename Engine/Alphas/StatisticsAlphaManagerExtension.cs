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

namespace QuantConnect.Lean.Engine.Alphas
{
    /// <summary>
    /// Manages alpha statistics responsbilities
    /// </summary>
    public class StatisticsAlphaManagerExtension : IAlphaManagerExtension
    {
        private readonly double _smoothingFactor;
        private readonly int _rollingAverageIsReadyCount;
        private readonly decimal _tradablePercentOfVolume;

        /// <summary>
        /// Gets the current statistics. The values are current as of the time specified
        /// in <see cref="AlphaRuntimeStatistics.MeanPopulationScore"/> and <see cref="AlphaRuntimeStatistics.RollingAveragedPopulationScore"/>
        /// </summary>
        public AlphaRuntimeStatistics Statistics { get; }

        /// <summary>
        /// Gets whether or not the rolling average statistics is ready
        /// </summary>
        public bool RollingAverageIsReady => Statistics.TotalAlphasAnalysisCompleted >= _rollingAverageIsReadyCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatisticsAlphaManagerExtension"/> class
        /// </summary>
        /// <param name="tradablePercentOfVolume">Percent of volume of first bar used to estimate the maximum number of tradable shares. Defaults to 1%</param>
        /// <param name="period">The period used for exponential smoothing of scores - this is a number of alphas. Defaults to 100 alpha predictions</param>
        public StatisticsAlphaManagerExtension(decimal tradablePercentOfVolume = 0.01m, int period = 100)
        {
            Statistics = new AlphaRuntimeStatistics();
            _tradablePercentOfVolume = tradablePercentOfVolume;
            _smoothingFactor = 2.0 / (period + 1.0);

            // use normal ema warmup period
            _rollingAverageIsReadyCount = period;
        }

        /// <summary>
        /// Handles the <see cref="IAlgorithm.AlphasGenerated"/> event
        /// Increments total, long and short counters. Updates long/short ratio
        /// </summary>
        /// <param name="context">The newly generated alpha context</param>
        public void OnAlphaGenerated(AlphaAnalysisContext context)
        {
            // incremement total alpha counter
            Statistics.TotalAlphasGenerated++;

            // update long/short ratio statistics
            if (context.Alpha.Direction == AlphaDirection.Up)
            {
                Statistics.LongCount++;
            }
            else if (context.Alpha.Direction == AlphaDirection.Down)
            {
                Statistics.ShortCount++;
            }
        }

        /// <summary>
        /// Computes an estimated value for the alpha. This is intended to be invoked at the end of the
        /// alpha period, i.e, when now == alpha.GeneratedTimeUtc + alpha.Period;
        /// </summary>
        /// <param name="context">Context whose alpha has just closed</param>
        public void OnAlphaClosed(AlphaAnalysisContext context)
        {
            // increment closed alpha counter
            Statistics.TotalAlphasClosed += 1;

            // tradable volume (purposefully includes fractional shares)
            var volume = _tradablePercentOfVolume * context.InitialValues.Volume;

            // value of the entering the trade in the account currency
            var enterValue = volume * context.InitialValues.Price * context.InitialValues.QuoteCurrencyConversionRate;

            // value of exiting the trade in the account currency
            var exitValue = volume * context.CurrentValues.Price * context.CurrentValues.QuoteCurrencyConversionRate;

            // total value delta between enter and exit values
            var alphaValue = (int)context.Alpha.Direction * (exitValue - enterValue);

            context.Alpha.EstimatedValue = alphaValue;
            Statistics.TotalEstimatedAlphaValue += alphaValue;
        }

        /// <summary>
        /// Updates the specified statistics with the new scores
        /// </summary>
        /// <param name="context">Context whose alpha has just completed analysis</param>
        public void OnAlphaAnalysisCompleted(AlphaAnalysisContext context)
        {
            // increment analysis completed counter
            Statistics.TotalAlphasAnalysisCompleted += 1;

            foreach (var scoreType in AlphaManager.ScoreTypes)
            {
                var score = context.Score.GetScore(scoreType);
                var currentTime = context.CurrentValues.TimeUtc;

                // online population average
                var mean = Statistics.MeanPopulationScore.GetScore(scoreType);
                var newMean = mean + (score - mean) / Statistics.TotalAlphasAnalysisCompleted;
                Statistics.MeanPopulationScore.SetScore(scoreType, newMean, currentTime);

                var newEma = score;
                if (Statistics.TotalAlphasAnalysisCompleted > 1)
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
            //NOP - Statistics are updated in line as alpha is generated, closed, and analyzed
        }

        /// <summary>
        /// Allows the extension to initialize itself over the expected range
        /// </summary>
        /// <param name="algorithmStartDate">The start date of the algorithm</param>
        /// <param name="algorithmEndDate">The end date of the algorithm</param>
        /// <param name="algorithmUtcTime">The algorithm's current utc time</param>
        public void InitializeForRange(DateTime algorithmStartDate, DateTime algorithmEndDate, DateTime algorithmUtcTime)
        {
            //NOP - All statistics are streaming and don't require knowledge of the range
        }
    }
}