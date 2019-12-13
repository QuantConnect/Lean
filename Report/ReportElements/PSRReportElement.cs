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
    internal sealed class PSRReportElement : ReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Estimate the PSR of the strategy.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public PSRReportElement(string name, string key, BacktestResult backtest, LiveResult live)
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
            var backtestPoints = Calculations.EquityPoints(_backtest);
            var benchmarkPoints = Calculations.BenchmarkPoints(_backtest);
            var backtestSeries = new Series<DateTime, double>(backtestPoints.Keys, backtestPoints.Values)
                .PercentChange()
                .ResampleEquivalence(date => date.Date)
                .Select(kvp => kvp.Value.Sum());

            var benchmarkSeries = new Series<DateTime, double>(benchmarkPoints.Keys, benchmarkPoints.Values)
                .PercentChange()
                .ResampleEquivalence(date => date.Date)
                .Select(kvp => kvp.Value.Sum());

            var benchmarkSharpe = Statistics.Statistics.ObservedSharpeRatio(benchmarkSeries.Values.ToList());
            var psr = Statistics.Statistics.ProbabilisticSharpeRatio(backtestSeries.Values.ToList(), benchmarkSharpe);

            return $"{psr:P0}";
        }
    }
}