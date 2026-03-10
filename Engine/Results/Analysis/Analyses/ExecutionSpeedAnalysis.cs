//using System;
//using System.Collections.Generic;

//namespace BacktestAnalyzerrr.Tests;

///// <summary>
///// Detects slow execution by parsing the last log line.
///// Benchmark speeds: https://www.quantconnect.com/performance
///// </summary>
//public class ExecutionSpeedAnalysis : BacktestResultAnalysis
//{
//    public IReadOnlyList<TestResult> Run(List<string> logs)
//    {
//        string? result = null;

//        var parts = logs[^1].Split(' ');
//        int idx = Array.IndexOf(parts, "Algorithm");
//        if (idx >= 0)
//        {
//            int seconds = (int)double.Parse(parts[idx + 4]);
//            if (seconds >= 10)
//            {
//                // Remove trailing comma from e.g. "123K,"
//                string kStr = parts[idx + 7].TrimEnd(',');
//                if (int.TryParse(kStr, out int dataPointsPerSecond) && dataPointsPerSecond < 40)
//                    result = $"The algorithm is slowly executing at only {dataPointsPerSecond}K data points per second";
//            }
//        }

//        var potentialSolutions = result is not null ? PotentialSolutions() : [];
//        return SingleResponse(result, potentialSolutions);
//    }

//    private static List<string> PotentialSolutions() =>
//    [
//        "Review the algorithm code for inefficiencies.",
//        "If there is a universe, reduce its size.",
//        "Reduce the data resolution.",
//        "If the algorithm is training a model, reduce the amount of training data or reduce the number of epochs in the training process.",
//    ];
//}
