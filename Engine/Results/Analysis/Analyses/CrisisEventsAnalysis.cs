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
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Util;
using MathNet.Numerics.Statistics;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Compares the strategy's Sharpe ratio to the benchmark's across known
    /// crisis / market-stress periods.
    /// Source: https://github.com/QuantConnect/Lean/blob/master/Report/Crisis.cs
    /// </summary>
    public class CrisisEventsAnalysis : BaseResultsAnalysis
    {
        public override string Issue { get; } = "The strategy underperformed the benchmark during some crisis events in terms of risk-adjusted returns.";

        public override int Weight { get; } = 22;
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Algorithm, parameters.EquityCurve, parameters.BenchmarkEquityCurve);

        private static readonly (string Name, DateTime Start, DateTime End)[] CrisisEvents =
        [
            ("DotCom Bubble 2000",                   new(2000,  2, 26), new(2000,  9, 10)),
            ("September 11, 2001",                   new(2001,  9,  5), new(2001, 10, 10)),
            ("U.S. Housing Bubble 2003",             new(2003,  1,  1), new(2003,  2, 20)),
            ("Global Financial Crisis 2007",         new(2007, 10,  1), new(2011, 12,  1)),
            ("Flash Crash 2010",                     new(2010,  5,  1), new(2010,  5, 22)),
            ("Fukushima Meltdown 2011",              new(2011,  3,  1), new(2011,  4, 22)),
            ("U.S. Credit Downgrade 2011",           new(2011,  8,  5), new(2011,  9,  1)),
            ("ECB IR Event 2012",                    new(2012,  9,  5), new(2012, 10, 12)),
            ("European Debt Crisis 2014",            new(2014, 10,  1), new(2014, 10, 29)),
            ("Market Sell-Off 2015",                 new(2015,  8, 10), new(2015, 10, 10)),
            ("Recovery 2010-2012",                   new(2010,  1,  1), new(2012, 10,  1)),
            ("New Normal 2014-2019",                 new(2014,  1,  1), new(2019,  1,  1)),
            ("COVID-19 Pandemic 2020",               new(2020,  2, 10), new(2020,  9, 20)),
            ("Post-COVID Run-up 2020-2021",          new(2020,  4,  1), new(2022,  1,  1)),
            ("Meme Season 2021",                     new(2021,  1,  1), new(2021,  5, 15)),
            ("Russia Invades Ukraine 2022-2023",     new(2022,  2,  1), new(2024,  1,  1)),
            ("AI Boom 2022-Present",                 new(2022, 11, 30), DateTime.Now),
        ];

        /// <summary>
        /// Compares the strategy's Sharpe ratio to the benchmark's across all crisis events
        /// that fall entirely within the backtest period.
        /// </summary>
        /// <param name="algorithm">The algorithm instance used to obtain the risk-free rate model.</param>
        /// <param name="backtestEquity">Daily equity values for the strategy, keyed by date.</param>
        /// <param name="benchmarkEquity">Daily equity values for the benchmark (SPY), keyed by date.</param>
        /// <returns>Analysis results listing crisis periods where the strategy underperformed the benchmark.</returns>
        public IReadOnlyList<AnalysisResult> Run(QCAlgorithm algorithm,
            SortedList<DateTime, decimal> backtestEquity,
            SortedList<DateTime, decimal> benchmarkEquity)
        {
            if (backtestEquity.Count == 0 || benchmarkEquity.Count == 0)
            {
                return SingleResponse(new ResultsAnalysisRepeatedContext([]));
            }

            var backtestStart = backtestEquity.Keys[0];
            var backtestEnd = backtestEquity.Keys[backtestEquity.Count - 1];

            var result = new List<object>();

            foreach (var (name, startDate, endDate) in CrisisEvents)
            {
                // Only include crisis events fully inside the backtest period.
                if (startDate < backtestStart || endDate > backtestEnd)
                {
                    continue;
                }

                var filteredBacktest = FilterByDate(backtestEquity, startDate, endDate);
                var filteredBenchmark = FilterByDate(benchmarkEquity, startDate, endDate);

                var (backtestSharpe, benchmarkSharpe) = CalculateSharpeRatio(filteredBacktest, filteredBenchmark, algorithm.RiskFreeInterestRateModel);

                if (backtestSharpe < benchmarkSharpe)
                {
                    result.Add(new
                    {
                        CrisisEvent = name,
                        BacktestSharpe = backtestSharpe,
                        BenchmarkSharpe = benchmarkSharpe,
                    });
                }
            }

            var potentialSolutions = result.Count > 0 ? Solutions() : [];
            return SingleResponse(new ResultsAnalysisRepeatedContext(result), potentialSolutions);
        }

        /// <summary>
        /// Keeps only entries whose key falls in [<paramref name="from"/>, <paramref name="to"/>].
        /// </summary>
        private static SortedList<DateTime, decimal> FilterByDate(SortedList<DateTime, decimal> series, DateTime from, DateTime to)
        {
            var result = new SortedList<DateTime, decimal>();
            foreach (var kvp in series)
            {
                if (kvp.Key < from)
                {
                    continue;
                }
                if (kvp.Key > to)
                {
                    break;
                }
                result.Add(kvp.Key, kvp.Value);
            }

            return result;
        }

        /// <summary>
        /// Calculates annualised Sharpe ratios for the backtest and benchmark equity series
        /// using the supplied risk-free rate model.
        /// </summary>
        /// <param name="backtest">Daily equity values for the strategy.</param>
        /// <param name="benchmark">Daily equity values for the benchmark.</param>
        /// <param name="riskFreeInterestRateModel">Model used to obtain the risk-free rate over the period.</param>
        /// <returns>A tuple of (backtestSharpe, benchmarkSharpe); both are 0 when a series is empty.</returns>
        internal static (double, double) CalculateSharpeRatio(SortedList<DateTime, decimal> backtest, SortedList<DateTime, decimal> benchmark,
            IRiskFreeInterestRateModel riskFreeInterestRateModel)
        {
            if (backtest.Count == 0 || benchmark.Count == 0)
            {
                return (0, 0);
            }

            var backtestReturns = backtest.PercentChange().Values.Select(x => (double)x).ToArray();
            var benchmarkReturns = benchmark.PercentChange().Values.Select(x => (double)x).ToArray();

            var riskFreeRate = (double)riskFreeInterestRateModel.GetRiskFreeRate(backtest.First().Key, backtest.Last().Key);

            var backtestSharpe = Statistics.Statistics.SharpeRatio(backtestReturns.Mean(), backtestReturns.StandardDeviation(), riskFreeRate);
            var benchmarkSharpe = Statistics.Statistics.SharpeRatio(benchmarkReturns.Mean(), benchmarkReturns.StandardDeviation(), riskFreeRate);

            return (backtestSharpe, benchmarkSharpe);
        }

        private static List<string> Solutions() =>
        [
            "Consider adding risk management techniques such as stop-loss orders, position sizing, " +
            "Option hedging, and diversification to mitigate losses during turbulent periods.",
        ];
    }
}
