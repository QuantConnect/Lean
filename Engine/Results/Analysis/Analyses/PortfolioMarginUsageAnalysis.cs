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
//using QuantConnect.Api;
//using BacktestAnalyzerrr.Utils;

//namespace BacktestAnalyzerrr.Tests;

///// <summary>
///// Detects periods where the portfolio under-utilises available margin
///// (3-day SMA of margin usage drops below 50 %).
///// </summary>
//public class PortfolioMarginUsageAnalysis : BacktestResultAnalysis
//{
//    public IReadOnlyList<TestResult> Run(IApi api, backtest backtest)
//    {
//        // 1 – Get the Portfolio Margin chart.
//        var chart = api.ReadBacktestChart(
//            backtest.ProjectId,
//            "Portfolio Margin",
//            (long)backtest.BacktestStart.Subtract(DateTime.UnixEpoch).TotalSeconds,
//            (long)backtest.BacktestEnd.Subtract(DateTime.UnixEpoch).TotalSeconds,
//            99999999,
//            backtest.BacktestId);

//        // 2 – Build one Series per asset series in the chart, then combine into a Frame.
//        var columns = new Dictionary<string, Series<DateTime, double>>();
//        foreach (var kvp in chart.Chart.Series)
//        {
//            var seriesData = new Dictionary<DateTime, double>();
//            foreach (var point in kvp.Value.Values)
//            {
//                var utcTime = DateTimeOffset.FromUnixTimeSeconds((long)point.X).UtcDateTime;
//                var eastern = Timestamps.EasternTime(utcTime);
//                seriesData[eastern] = (double)point.Y;
//            }
//            columns[kvp.Key] = seriesData.ToSeries();
//        }

//        var frame = Frame.ofColumns(columns);

//        // 3 – Fill missing, sum columns → total portfolio margin usage.
//        var filled    = frame.FillMissing(0.0);
//        var portfolio = filled.SumRows();

//        // 4 – 3-day SMA; count days below 50 %.
//        var rolling3 = portfolio.RollingMean(3);
//        int belowCount = rolling3.Values.Count(v => v < 50);

//        object? result = belowCount > 0
//            ? $"Number of days when the 3-day SMA of the margin usage drops below 50%: {belowCount}"
//            : null;

//        var potentialSolutions = result is not null ? PotentialSolutions() : [];
//        return SingleResponse(result, potentialSolutions);
//    }

//    private static List<string> PotentialSolutions() =>
//    [
//        "The algorithm sometimes only utilizes a small proportion of the margin available. " +
//        "Adjust the strategy logic or position sizing to utilize more margin.",

//        "If the algorithm logic leads to periods of time when the portfolio sits in cash, " +
//        "consider holding a \"risk-free\" asset during these periods.",
//    ];
//}
