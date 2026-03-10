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

///// <summary>Compares the full-period Sharpe ratio of the strategy to the benchmark.</summary>
//public class PerformanceRelativeToBenchmark : BacktestResultAnalysis
//{
//    public IReadOnlyList<TestResult> Run(
//        QCAlgorithm qb,
//        Series<DateTime, double> backtestEquity,
//        Series<DateTime, double> benchmarkEquity)
//    {
//        var backtestReturns  = backtestEquity.PctChange();
//        var benchmarkReturns = benchmarkEquity.PctChange();

//        double rfr = RiskFreeInterestRateModelExtensions.GetRiskFreeRate(
//            qb.RiskFreeInterestRateModel,
//            backtestReturns.Keys.First(),
//            backtestReturns.Keys.Last());

//        double backtestSharpe  = Statistics.SharpeRatio(backtestReturns.Mean(),  backtestReturns.StdDev(),  rfr);
//        double benchmarkSharpe = Statistics.SharpeRatio(benchmarkReturns.Mean(), benchmarkReturns.StdDev(), rfr);

//        object? result = backtestSharpe < benchmarkSharpe
//            ? new { BacktestSharpe = backtestSharpe, BenchmarkSharpe = benchmarkSharpe }
//            : null;

//        var potentialSolutions = result is not null ? PotentialSolutions() : [];
//        return SingleResponse(result, potentialSolutions);
//    }

//    private static List<string> PotentialSolutions() =>
//    [
//        "The strategy has a lower Sharpe ratio than the benchmark. " +
//        "Try adjusting the trading rules and/or the universe to get a strategy that outperforms the benchmark.",
//    ];
//}
