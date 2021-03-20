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
using System.Linq;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class SharpeRatioReportElement : ReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Estimate the sharpe ratio of the strategy.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public SharpeRatioReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// The generated output string to be injected
        /// </summary>
        public override string Render()
        {
            if (_live == null)
            {
                var backtestSharpe = _backtest?.TotalPerformance?.PortfolioStatistics?.SharpeRatio;
                Result = backtestSharpe;
                return backtestSharpe?.ToString("F1") ?? "-";
            }

            var equityPoints = ResultsUtil.EquityPoints(_live);
            var performance = DeedleUtil.PercentChange(new Series<DateTime, double>(equityPoints).ResampleEquivalence(date => date.Date, s => s.LastValue()));
            if (performance.ValueCount == 0)
            {
                return "-";
            }

            var sixMonthsAgo = performance.LastKey().AddDays(-180);
            var trailingPerformance = performance.Where(series => series.Key >= sixMonthsAgo && series.Key.DayOfWeek != DayOfWeek.Saturday && series.Key.DayOfWeek != DayOfWeek.Sunday)
                .Values
                .ToList();

            if (trailingPerformance.Count < 7 || Statistics.Statistics.AnnualStandardDeviation(trailingPerformance) == 0)
            {
                return "-";
            }

            var sharpe = Statistics.Statistics.SharpeRatio(trailingPerformance, 0.0);
            Result = sharpe;
            return sharpe.ToString("F2");
        }
    }
}
