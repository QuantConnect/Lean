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
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.Alphas
{
    /// <summary>
    /// Manages alpha charting responsibilities.
    /// </summary>
    public class ChartingInsightManagerExtension : IInsightManagerExtension
    {
        /// <summary>
        /// The string name used for the Alpha Assets chart
        /// </summary>
        public const string AlphaAssets = "Alpha Assets";

        private readonly bool _liveMode;
        private readonly StatisticsInsightManagerExtension _statisticsManager;

        private const int BacktestChartSamples = 1000;
        private DateTime _lastInsightCountSampleDateUtc;
        private DateTime _nextChartSampleAlgorithmTimeUtc;

        private readonly Chart _totalInsightCountPerSymbolChart = new Chart(AlphaAssets);          // Heatmap chart
        private readonly Series _totalInsightCountSeries = new Series("Count", SeriesType.Bar, "#");

        private int _dailyCount;
        private readonly Dictionary<Symbol, int> _totalInsightCountPerSymbol = new Dictionary<Symbol, int>();
        private readonly Dictionary<InsightScoreType, Series> _insightScoreSeriesByScoreType = new Dictionary<InsightScoreType, Series>();

        /// <summary>
        /// Gets or sets the interval at which alpha charts are updated. This is in realtion to algorithm time.
        /// </summary>
        protected TimeSpan SampleInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartingInsightManagerExtension"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance. This is only used for adding the charts
        /// to the algorithm. We purposefully do not save a reference to avoid potentially inconsistent reads</param>
        /// <param name="statisticsManager">Statistics manager used to access mean population scores for charting</param>
        public ChartingInsightManagerExtension(IAlgorithm algorithm, StatisticsInsightManagerExtension statisticsManager)
        {
            _statisticsManager = statisticsManager;
            _liveMode = algorithm.LiveMode;

            // chart for average scores over sample period
            var scoreChart = new Chart("Alpha");
            foreach (var scoreType in InsightManager.ScoreTypes)
            {
                var series = new Series($"{scoreType} Score", SeriesType.Line, "%");
                scoreChart.AddSeries(series);
                _insightScoreSeriesByScoreType[scoreType] = series;
            }

            // chart for insight count over sample period
            var insightCount = new Chart("Insight Count");
            insightCount.AddSeries(_totalInsightCountSeries);

            algorithm.AddChart(scoreChart);
            algorithm.AddChart(insightCount);
            algorithm.AddChart(_totalInsightCountPerSymbolChart);
        }

        /// <summary>
        /// Invokes the manager at the end of the time step.
        /// Samples and plots insight counts and population score.
        /// </summary>
        /// <param name="frontierTimeUtc">The current frontier time utc</param>
        public void Step(DateTime frontierTimeUtc)
        {
            // sample insight/symbol counts each utc day change
            if (frontierTimeUtc.Date > _lastInsightCountSampleDateUtc)
            {
                _lastInsightCountSampleDateUtc = frontierTimeUtc.Date;

                // add sum of daily insight counts to the total insight count series
                _totalInsightCountSeries.AddPoint(frontierTimeUtc.Date, _dailyCount);

                // Create the pie chart every minute or so
                PopulateChartWithSeriesPerSymbol(_totalInsightCountPerSymbol, _totalInsightCountPerSymbolChart, SeriesType.Treemap, frontierTimeUtc);

                // Resetting our storage
                _dailyCount = 0;
            }

            // sample average population scores
            if (frontierTimeUtc >= _nextChartSampleAlgorithmTimeUtc)
            {
                try
                {
                    // verify these scores have been computed before taking the first sample
                    if (_statisticsManager.RollingAverageIsReady)
                    {
                        // sample the rolling averaged population scores
                        foreach (var scoreType in InsightManager.ScoreTypes)
                        {
                            var score = 100 * _statisticsManager.Statistics.RollingAveragedPopulationScore.GetScore(scoreType);
                            _insightScoreSeriesByScoreType[scoreType].AddPoint(frontierTimeUtc, score.SafeDecimalCast());
                        }
                        _nextChartSampleAlgorithmTimeUtc = frontierTimeUtc + SampleInterval;
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
            }
        }

        /// <summary>
        /// Invoked after <see cref="IAlgorithm.Initialize"/> has been called.
        /// Determines chart sample interval and initial sample times
        /// </summary>
        /// <remarks>
        /// While the algorithm instance is provided, it's highly recommended to not maintain
        /// a direct reference to it as there is no way to guarantee consistence reads.
        /// </remarks>
        /// <param name="algorithmStartDate">The start date of the algorithm</param>
        /// <param name="algorithmEndDate">The end date of the algorithm</param>
        /// <param name="algorithmUtcTime">The algorithm's current utc time</param>
        public void InitializeForRange(DateTime algorithmStartDate, DateTime algorithmEndDate, DateTime algorithmUtcTime)
        {
            if (_liveMode)
            {
                // live mode we'll sample each minute
                SampleInterval = Time.OneMinute;
            }
            else
            {
                // space out backtesting samples evenly
                var backtestPeriod = algorithmEndDate - algorithmStartDate;
                SampleInterval = TimeSpan.FromTicks(backtestPeriod.Ticks / BacktestChartSamples);
            }

            _nextChartSampleAlgorithmTimeUtc = algorithmUtcTime + SampleInterval;
            _lastInsightCountSampleDateUtc = algorithmUtcTime.RoundDown(Time.OneDay);
        }

        /// <summary>
        /// Handles the <see cref="IAlgorithm.InsightsGenerated"/> event.
        /// Keep daily and total count of insights by symbol
        /// </summary>
        /// <param name="context">The newly generated insight analysis context</param>
        public void OnInsightGenerated(InsightAnalysisContext context)
        {
            if (!_totalInsightCountPerSymbol.ContainsKey(context.Symbol))
            {
                _totalInsightCountPerSymbol[context.Symbol] = 1;
            }
            else
            {
                // track total count per symbol
                _totalInsightCountPerSymbol[context.Symbol] += 1;
            }

            _dailyCount++;
        }

        /// <summary>
        /// NOP - Charting is more concerned with population vs individual insights
        /// </summary>
        /// <param name="context">Context whose insight has just completed analysis</param>
        public void OnInsightClosed(InsightAnalysisContext context)
        {
        }

        /// <summary>
        /// NOP - Charting is more concerned with population vs individual insights
        /// </summary>
        /// <param name="context">Context whose insight has just completed analysis</param>
        public void OnInsightAnalysisCompleted(InsightAnalysisContext context)
        {
        }

        /// <summary>
        /// Creates series for each symbol and adds a value corresponding to the specified data
        /// </summary>
        private void PopulateChartWithSeriesPerSymbol(Dictionary<Symbol, int> data, Chart chart, SeriesType seriesType, DateTime frontierTimeUtc)
        {
            foreach (var kvp in data)
            {
                var symbol = kvp.Key;
                var count = kvp.Value;

                Series series;
                if (!chart.Series.TryGetValue(symbol.Value, out series))
                {
                    series = new Series(symbol.Value, seriesType, null);
                    chart.Series.Add(series.Name, series);
                }
                series.AddPoint(frontierTimeUtc, count);
            }
        }
    }
}
