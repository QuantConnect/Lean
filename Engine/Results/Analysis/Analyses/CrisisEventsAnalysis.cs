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
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Deedle;
//using QuantConnect;
//using QuantConnect.Research;
//using BacktestAnalyzerrr.Utils;

//namespace BacktestAnalyzerrr.Tests;

///// <summary>
///// Compares the strategy's Sharpe ratio to the benchmark's across known
///// crisis / market-stress periods.
///// Source: https://github.com/QuantConnect/Lean/blob/master/Report/Crisis.cs
///// </summary>
//public class CrisisEventsAnalysis : BacktestResultAnalysis
//{
//    private static readonly (string Name, DateTime Start, DateTime End)[] CrisisEvents =
//    [
//        ("DotCom Bubble 2000",                   new(2000,  2, 26), new(2000,  9, 10)),
//        ("September 11, 2001",                   new(2001,  9,  5), new(2001, 10, 10)),
//        ("U.S. Housing Bubble 2003",             new(2003,  1,  1), new(2003,  2, 20)),
//        ("Global Financial Crisis 2007",         new(2007, 10,  1), new(2011, 12,  1)),
//        ("Flash Crash 2010",                     new(2010,  5,  1), new(2010,  5, 22)),
//        ("Fukushima Meltdown 2011",              new(2011,  3,  1), new(2011,  4, 22)),
//        ("U.S. Credit Downgrade 2011",           new(2011,  8,  5), new(2011,  9,  1)),
//        ("ECB IR Event 2012",                    new(2012,  9,  5), new(2012, 10, 12)),
//        ("European Debt Crisis 2014",            new(2014, 10,  1), new(2014, 10, 29)),
//        ("Market Sell-Off 2015",                 new(2015,  8, 10), new(2015, 10, 10)),
//        ("Recovery 2010-2012",                   new(2010,  1,  1), new(2012, 10,  1)),
//        ("New Normal 2014-2019",                 new(2014,  1,  1), new(2019,  1,  1)),
//        ("COVID-19 Pandemic 2020",               new(2020,  2, 10), new(2020,  9, 20)),
//        ("Post-COVID Run-up 2020-2021",          new(2020,  4,  1), new(2022,  1,  1)),
//        ("Meme Season 2021",                     new(2021,  1,  1), new(2021,  5, 15)),
//        ("Russia Invades Ukraine 2022-2023",     new(2022,  2,  1), new(2024,  1,  1)),
//        ("AI Boom 2022-Present",                 new(2022, 11, 30), DateTime.Now),
//    ];

//    public IReadOnlyList<TestResult> Run(
//        QCAlgorithm qb,
//        Series<DateTime, double> backtestEquity,
//        Series<DateTime, double> benchmarkEquity)
//    {
//        var backtestStart = backtestEquity.Keys.First();
//        var backtestEnd   = backtestEquity.Keys.Last();

//        var result = new List<object>();

//        foreach (var (name, startDate, endDate) in CrisisEvents)
//        {
//            // Only include crisis events fully inside the backtest period.
//            if (startDate < backtestStart || endDate > backtestEnd)
//                continue;

//            var filteredBacktest  = backtestEquity.FilterByDate(startDate, endDate);
//            var filteredBenchmark = benchmarkEquity.FilterByDate(startDate, endDate);

//            var backtestReturns  = filteredBacktest.PctChange();
//            var benchmarkReturns = filteredBenchmark.PctChange();

//            double rfr = RiskFreeInterestRateModelExtensions.GetRiskFreeRate(
//                qb.RiskFreeInterestRateModel, startDate, endDate);

//            double backtestSharpe  = Statistics.SharpeRatio(backtestReturns.Mean(),  backtestReturns.StdDev(),  rfr);
//            double benchmarkSharpe = Statistics.SharpeRatio(benchmarkReturns.Mean(), benchmarkReturns.StdDev(), rfr);

//            if (backtestSharpe < benchmarkSharpe)
//            {
//                result.Add(new
//                {
//                    crisis_event      = name,
//                    backtest_sharpe   = backtestSharpe,
//                    benchmark_sharpe  = benchmarkSharpe,
//                });
//            }
//        }

//        var potentialSolutions = result.Count > 0 ? PotentialSolutions() : [];
//        return SingleResponse(result.Count > 0 ? (object)result : null, potentialSolutions);
//    }

//    private static List<string> PotentialSolutions() =>
//    [
//        "The strategy underperformed the benchmark during some crisis events. " +
//        "Consider adding risk management techniques such as stop-loss orders, position sizing, " +
//        "Option hedging, and diversification to mitigate losses during turbulent periods.",
//    ];
//}
