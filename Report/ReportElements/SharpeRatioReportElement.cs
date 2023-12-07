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

namespace QuantConnect.Report.ReportElements
{
    public class SharpeRatioReportElement : ReportElement
    {
        /// <summary>
        /// Live result object
        /// </summary>
        protected LiveResult LiveResult { get; }

        /// <summary>
        /// Backtest result object
        /// </summary>
        protected BacktestResult BacktestResult { get; }

        /// <summary>
        /// Sharpe Ratio from a backtest
        /// </summary>
        public virtual decimal? BacktestResultValue => BacktestResult?.TotalPerformance?.PortfolioStatistics?.SharpeRatio;

        /// <summary>
        /// Estimate the sharpe ratio of the strategy.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public SharpeRatioReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            LiveResult = live;
            BacktestResult = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// The generated output string to be injected
        /// </summary>
        public override string Render()
        {
            if (LiveResult == null)
            {
                Result = BacktestResultValue;
                return BacktestResultValue?.ToString("F1") ?? "-";
            }

            var equityPoints = ResultsUtil.EquityPoints(LiveResult);
            var performance = DeedleUtil.PercentChange(new Series<DateTime, double>(equityPoints).ResampleEquivalence(date => date.Date, s => s.LastValue()));
            if (performance.ValueCount == 0)
            {
                return "-";
            }

            var sixMonthsAgo = performance.LastKey().AddDays(-180);
            var trailingPerformance = performance.Where(series => series.Key >= sixMonthsAgo && series.Key.DayOfWeek != DayOfWeek.Saturday && series.Key.DayOfWeek != DayOfWeek.Sunday)
                .Values
                .ToList();

            var liveResultValue = GetLiveResultValue(trailingPerformance);
            Result = liveResultValue;
            return liveResultValue?.ToString("F2") ?? "-";
        }

        /// <summary>
        /// Get the live result value
        /// </summary>
        /// <param name="trailingPerformance">The performance for the last period</param>
        /// <returns>The desired metric. Sharpe Ratio in this class.</returns>
        public virtual double? GetLiveResultValue(List<double> trailingPerformance)
        {
            if (trailingPerformance.Count < 7 || Statistics.Statistics.AnnualStandardDeviation(trailingPerformance) == 0)
            {
                return null;
            }

            return Statistics.Statistics.SharpeRatio(trailingPerformance, 0.0);
        }
    }
}
