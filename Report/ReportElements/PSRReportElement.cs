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

using System;
using System.Linq;
using Deedle;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class PSRReportElement : ReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// The number of trading days per year to get better result of statistics
        /// </summary>
        private int _tradingDaysPerYear;

        /// <summary>
        /// Estimate the PSR of the strategy.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        /// <param name="tradingDaysPerYear">The number of trading days per year to get better result of statistics</param>
        public PSRReportElement(string name, string key, BacktestResult backtest, LiveResult live, int tradingDaysPerYear)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
            _tradingDaysPerYear = tradingDaysPerYear;
        }

        /// <summary>
        /// The generated output string to be injected
        /// </summary>
        public override string Render()
        {
            decimal? psr;
            if (_live == null)
            {
                psr = _backtest?.TotalPerformance?.PortfolioStatistics?.ProbabilisticSharpeRatio;
                Result = psr;
                if (psr == null)
                {
                    return "-";
                }
                
                return $"{psr:P0}";
            }

            var equityCurvePerformance = DrawdownCollection.NormalizeResults(_backtest, _live)
                .ResampleEquivalence(date => date.Date, s => s.LastValue())
                .PercentChange();

            if (equityCurvePerformance.IsEmpty || equityCurvePerformance.KeyCount < 180)
            {
                return "-";
            }

            var sixMonthsBefore = equityCurvePerformance.LastKey() - TimeSpan.FromDays(180);

            var benchmarkSharpeRatio = 1.0d / Math.Sqrt(_tradingDaysPerYear);
            psr = Statistics.Statistics.ProbabilisticSharpeRatio(
                equityCurvePerformance
                    .Where(kvp => kvp.Key >= sixMonthsBefore)
                    .Values
                    .ToList(), 
                benchmarkSharpeRatio)
                .SafeDecimalCast();
            
            Result = psr;
            return $"{psr:P0}";
        }
    }
}
