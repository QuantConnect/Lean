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

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis.Providers
{
    /// <summary>
    /// Computes population average scores
    /// </summary>
    public class AlphaRuntimeStatisticsUpdater : IAlphaRuntimeStatisticsUpdater
    {
        // online averaging algorithm requires n=1 to start
        private int _populationMeanSamples = 1;
        private decimal _longCount;
        private decimal _shortCount;

        private readonly double _smoothingFactor;
        private readonly decimal _tradablePercentOfVolume;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaRuntimeStatisticsUpdater"/> class
        /// </summary>
        /// <param name="tradablePercentOfVolume">Percent of volume of first bar used to estimate the maximum number of tradable shares. Defaults to 1%</param>
        /// <param name="period">The period used for exponential smoothing of scores - this is a number of alphas. Defaults to 100 alpha predictions</param>
        public AlphaRuntimeStatisticsUpdater(decimal tradablePercentOfVolume = 0.01m, int period = 100)
        {
            _tradablePercentOfVolume = tradablePercentOfVolume;
            _smoothingFactor = 2.0 / (period + 1.0);
        }

        /// <summary>
        /// Updates statistics when a new alpha signal is received by the alpha manager
        /// </summary>
        /// <param name="statistics">Statistics to be updated</param>
        /// <param name="context">Context whose alpha was just generated</param>
        public void OnAlphaReceived(AlphaRuntimeStatistics statistics, AlphaAnalysisContext context)
        {
            // incremement total alpha counter
            statistics.TotalAlphasGenerated++;

            // update long/short ratio statistics
            if (context.Alpha.Direction == AlphaDirection.Up)
            {
                _longCount++;
            }
            else if (context.Alpha.Direction == AlphaDirection.Down)
            {
                _shortCount++;
            }

            statistics.LongShortRatio = _shortCount == 0 ? 1m : _longCount / _shortCount;
        }

        /// <summary>
        /// Computes an estimated value for the alpha. This is intended to be invoked at the end of the
        /// alpha period, i.e, when now == alpha.GeneratedTimeUtc + alpha.Period;
        /// </summary>
        /// <param name="statistics">Statistics to be updated</param>
        /// <param name="context">Context whose alpha has just closed</param>
        public void OnAlphaClosed(AlphaRuntimeStatistics statistics, AlphaAnalysisContext context)
        {
            // tradable volume (purposefully includes fractional shares)
            var volume =  _tradablePercentOfVolume * context.InitialValues.Volume;

            // value of the entering the trade in the account currency
            var enterValue = volume * context.InitialValues.Price * context.InitialValues.QuoteCurrencyConversionRate;

            // value of exiting the trade in the account currency
            var exitValue = volume * context.CurrentValues.Price * context.CurrentValues.QuoteCurrencyConversionRate;

            // total value delta between enter and exit values
            var alphaValue = (int) context.Alpha.Direction * (exitValue - enterValue);

            context.Alpha.EstimatedValue = alphaValue;
            statistics.TotalEstimatedAlphaValue += alphaValue;
        }

        /// <summary>
        /// Updates the specified statistics with the new scores
        /// </summary>
        /// <param name="statistics">Statistics to be updated</param>
        /// <param name="context">Context whose alpha has just completed analysis</param>
        public void OnAlphaAnalysisCompleted(AlphaRuntimeStatistics statistics, AlphaAnalysisContext context)
        {
            foreach (var scoreType in AlphaManager.ScoreTypes)
            {
                var score = context.Score.GetScore(scoreType);
                var currentTime = context.CurrentValues.TimeUtc;

                var mean = statistics.MeanPopulationScore.GetScore(scoreType);
                var newMean = mean + (score - mean) / _populationMeanSamples;
                statistics.MeanPopulationScore.SetScore(scoreType, newMean, currentTime);

                var newEma = score;
                if (_populationMeanSamples > 1)
                {
                    var ema = statistics.RollingAveragedPopulationScore.GetScore(scoreType);
                    newEma = score * _smoothingFactor + ema * (1 - _smoothingFactor);
                }
                statistics.RollingAveragedPopulationScore.SetScore(scoreType, newEma, currentTime);
            }

            _populationMeanSamples++;
        }
    }
}