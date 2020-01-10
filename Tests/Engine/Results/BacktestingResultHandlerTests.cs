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
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Report;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class BacktestingResultHandlerTests
    {
        private Dictionary<string, BacktestingResultHandler> _resultsCache = new Dictionary<string, BacktestingResultHandler>();

        public BacktestingResultHandler GetResults(string algorithm, DateTime algoStart, DateTime algoEnd)
        {
            BacktestingResultHandler backtestingResult;
            if (!_resultsCache.TryGetValue(algorithm, out backtestingResult))
            {
                var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(algorithm,
                   new Dictionary<string, string>(),
                   Language.CSharp,
                   AlgorithmStatus.Completed);


                // The AlgorithmRunner uses the `RegressionResultHandler` but doesn't do any sampling.
                // It defaults to the behavior of the `BacktestingResultHandler` class in `results.ProcessSynchronousEvents()`
                AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                    parameter.Statistics,
                    parameter.AlphaStatistics,
                    parameter.Language,
                    parameter.ExpectedFinalStatus,
                    startDate: algoStart,
                    endDate: algoEnd,
                    storeResult: true);

                _resultsCache[algorithm] = AlgorithmRunner.AlgorithmResults[Language.CSharp][algorithm];
                AlgorithmRunner.AlgorithmResults[Language.CSharp].Remove(algorithm);
            }

            return _resultsCache[algorithm];
        }

        [TestCase(nameof(BasicTemplateAlgorithm))]
        [TestCase(nameof(BasicTemplateDailyAlgorithm))]
        [TestCase(nameof(ResolutionSwitchingAlgorithm))]
        public void SamplesNotMisalignedRelative(string algorithm)
        {
            var backtestResults = GetResults(algorithm, new DateTime(2013, 10, 7), new DateTime(2013, 10, 11));
            var benchmarkSeries = backtestResults.Charts["Benchmark"].Series["Benchmark"];
            var equitySeries = backtestResults.Charts["Strategy Equity"].Series["Equity"];
            var performanceSeries = backtestResults.Charts["Strategy Equity"].Series["Daily Performance"];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();
            var benchmarkPerformance = benchmark.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();
            performance = performance.ResampleEquivalence(dt => dt.Date, s => s.LastValue());

            // Uncomment the lines below to simulate a (naive) data cleaning attempt.
            // Remarks: during the development of PR #3979, this was thought to be a solution for misaligned values.
            // however, because the lowest resolution of an algorithm can affect the result of the performance series, we quickly
            // discovered that this was not an apt solution. You can view the misalignment with non-daily data
            // by uncommenting the lines below. The test should fail on the "diverging values" test for any algorithm that makes use of minutely data.
            // However, the test will pass for algorithms that only make use of daily resolution because the benchmark
            // is added in daily resolution in 'Engine/DataFeeds/UniverseSelection.cs#L384', which causes the sampling
            // of the two series to be aligned with each other (sampling at the previous close, which is 2 days ago w/ daily data vs. yesterday's close w/ non-daily).
            // --------------------------------------------------------------
            //performanceSeries.Values.RemoveAt(0);
            //performanceSeries.Values.RemoveAt(0);
            //equityPerformance = equityPerformance.After(equityPerformance.GetKeyAt(1));
            //benchmarkPerformance = benchmarkPerformance.After(benchmarkPerformance.GetKeyAt(1));
            //---------------------------------------------------------------

            //Frame.CreateEmpty<DateTime, string>().Join("equity", equityPerformance).Join("bench", benchmarkPerformance).Join("perf", performance).Print();
            // Before 2020-01-10, by uncommenting the line above, it produces this Frame in master for `BasicTemplateDailyAlgorithm`:
            // ====================================================================================
            // |                           equity               bench                perf         |
            // | 10/7/2013 12:00:00 AM  -> <missing>            <missing>            0            |
            // | 10/8/2013 12:00:00 AM  -> 0                    -0.00835040609378598 0            |
            // | 10/9/2013 12:00:00 AM  -> -0.0114689260000001  -0.0117646820298673  -0.01146893  |
            // | 10/10/2013 12:00:00 AM -> 0.000602494970229067 0.000604537450206063 0.0006024946 |
            // | 10/11/2013 12:00:00 AM -> 0.0215563202204622   0.0216210268330373   0.02155632   |
            // | 10/12/2013 12:00:00 AM -> 0.0063658013516552   0.00638415728187288  0.006365801  |
            // ====================================================================================
            //
            // And it produces this Frame in master for `BasicTemplateAlgorithm` (minute resolution):
            // ====================================================================================
            // |                             equity               bench                perf       |
            // | 10/7/2013 12:00:00 AM  -> <missing>            <missing>            2.585847E-05 |
            // | 10/8/2013 12:00:00 AM  -> -0.0117327146154655  -0.00835040609378598 -0.01173271  |
            // | 10/9/2013 12:00:00 AM  -> 0.000602640205305941 -0.0117646820298673  0.0006026399 |
            // | 10/10/2013 12:00:00 AM -> 0.0215615143841935   0.000604537450206063 0.02156151   |
            // | 10/11/2013 12:00:00 AM -> 0.0063673015777644   0.0216210268330373   0.006367302  |
            // | 10/12/2013 12:00:00 AM -> 0                    0.00638415728187288  0            |
            // ====================================================================================
            //
            // Note: The `<missing>` values at the start of the "bench" series (10/7/2013 12:00:00 AM) would
            // be represented as a `0` due to the `CreateBenchmarkDifferences(...)` method in StatisticsBuilder.
            // The `EnsureSameLength(...)` method in StatisticsBuilder pads the result with
            // additional zeroes by appending if the length of the two series are not equal, but that is not the case here.
            //
            // We'll be calculating statistics for the daily algorithm using these two series:
            //        Bench, Performance
            // [[         0,          0], // Invalid, this is the first time step of the algorithm. No data has been pumped in yet nor have the securities' prices been initialized. This value shouldn't exist.
            //  [-0.0083504,          0], // Invalid, no data should exist for this day in "Bench" because we don't calculate the percentage change from open to close. This value should exist, but we should drop it for the time being.
            //  [-0.0117646, -0.0114689],
            //  [0.00060453, 0.00060249],
            //  [0.02162102, 0.02155632],
            //  [0.00638415, 0.00636580]]
            //
            // If we manually calculate the beta with the series put above,  we get the beta: 0.8757695
            // If we manually calculate the beta without the invalid values, we get the beta: 0.9892104

            TestSampleAlignmentsRelative(equityPerformance, benchmarkPerformance, performance);
        }

        [Test]
        public void BasicTemplateAlgorithmSamplesNotMisalignedAbsolute()
        {
            var backtestResults = GetResults(nameof(BasicTemplateAlgorithm), new DateTime(2013, 10, 7), new DateTime(2013, 10 ,11));
            var benchmarkSeries = backtestResults.Charts["Benchmark"].Series["Benchmark"];
            var equitySeries = backtestResults.Charts["Strategy Equity"].Series["Equity"];
            var performanceSeries = backtestResults.Charts["Strategy Equity"].Series["Daily Performance"];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();
            var benchmarkPerformance = benchmark.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();

            // Before 2020-01-10 on master, the following Frame is created:
            // ====================================================================================
            // |                           equity               bench                perf         |
            // | 10/7/2013 12:00:00 AM  -> <missing>            <missing>            2.585847E-05 |
            // | 10/8/2013 12:00:00 AM  -> -0.0117327146154655  -0.00835040609378598 -0.01173271  |
            // | 10/9/2013 12:00:00 AM  -> 0.000602640205305941 -0.0117646820298673  0.0006026399 |
            // | 10/10/2013 12:00:00 AM -> 0.0215615143841935   0.000604537450206063 0.02156151   |
            // | 10/11/2013 12:00:00 AM -> 0.0063673015777644   0.0216210268330373   0.006367302  |
            // | 10/12/2013 12:00:00 AM -> 0                    0.00638415728187288  0            |
            // ====================================================================================
            //
            // With some fixes applied, we get the following series:
            // ====================================================================================
            // |                           equity               bench                perf         |
            // | 10/7/2013 12:00:00 PM  -> <missing>            <missing>            2.585847E-05 |
            // | 10/8/2013 12:00:00 AM  -> -0.0117327146154655  -0.0117646820298673  -0.01173271  |
            // | 10/9/2013 12:00:00 AM  -> 0.000602640205305941 0.000604537450206063 0.0006026399 |
            // | 10/10/2013 12:00:00 AM -> 0.0215615143841935   0.0216210268330373   0.02156151   |
            // | 10/11/2013 12:00:00 AM -> 0.0063673015777644   0.00638415728187288  0.006367302  |
            // ===================================================================================|

            Assert.AreEqual(new DateTime(2013, 10, 8), benchmarkPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), benchmarkPerformance.LastKey().Date);
            Assert.AreEqual(5, benchmarkPerformance.KeyCount);
            Assert.AreEqual(4, benchmarkPerformance.ValueCount);
            Assert.AreEqual(Math.Round(-0.01176468202986730, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(0), 6));
            Assert.AreEqual(Math.Round(0.000604537450206063, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(1), 6));
            Assert.AreEqual(Math.Round(0.021621026833037300, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(2), 6));
            Assert.AreEqual(Math.Round(0.006384157281872880, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(3), 6));

            Assert.AreEqual(new DateTime(2013, 10, 7), performance.FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), performance.LastKey().Date);
            Assert.AreEqual(5, performance.ValueCount);
            Assert.AreEqual(5, performance.KeyCount);
            Assert.AreEqual(Math.Round(2.585847E-05, 6), Math.Round(performance.GetAt(0), 6));
            Assert.AreEqual(Math.Round(-0.011732710, 6), Math.Round(performance.GetAt(1), 6));
            Assert.AreEqual(Math.Round(0.0006026399, 6), Math.Round(performance.GetAt(2), 6));
            Assert.AreEqual(Math.Round(0.0215615100, 6), Math.Round(performance.GetAt(3), 6));
            Assert.AreEqual(Math.Round(0.0063673020, 6), Math.Round(performance.GetAt(4), 6));

            // This is a side-effect of how we calculate performance from the equity series in this test.
            Assert.AreEqual(4, equityPerformance.ValueCount);
            Assert.AreEqual(new DateTime(2013, 10, 8), equityPerformance.DropMissing().FirstKey());
            Assert.AreEqual(new DateTime(2013, 10, 11), equityPerformance.LastKey());

            // No need to run TestSampleAlignmentsRelative(...) here, since this algorithm will already have that ran
            // in the test SamplesNotMisalignedRelative().
        }

        [Test]
        public void BasicTemplateDailyAlgorithmSamplesNotMisalignedAbsolute()
        {
            var backtestResults = GetResults(nameof(BasicTemplateDailyAlgorithm), new DateTime(2013, 10, 7), new DateTime(2013, 10, 11));
            var benchmarkSeries = backtestResults.Charts["Benchmark"].Series["Benchmark"];
            var equitySeries = backtestResults.Charts["Strategy Equity"].Series["Equity"];
            var performanceSeries = backtestResults.Charts["Strategy Equity"].Series["Daily Performance"];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();
            var benchmarkPerformance = benchmark.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();

            // Before 2020-01-10 on master, the following Frame is created:
            // ====================================================================================
            // |                           equity               bench                perf         |
            // | 10/7/2013 12:00:00 AM  -> <missing>            <missing>            0            |
            // | 10/8/2013 12:00:00 AM  -> 0                    -0.00835040609378598 0            |
            // | 10/9/2013 12:00:00 AM  -> -0.0114689260000001  -0.0117646820298673  -0.01146893  |
            // | 10/10/2013 12:00:00 AM -> 0.000602494970229067 0.000604537450206063 0.0006024946 |
            // | 10/11/2013 12:00:00 AM -> 0.0215563202204622   0.0216210268330373   0.02155632   |
            // | 10/12/2013 12:00:00 AM -> 0.0063658013516552   0.00638415728187288  0.006365801  |
            // ====================================================================================
            //
            // Samples are aligned since both benchmark and strategy only use daily data.

            Assert.AreEqual(new DateTime(2013, 10, 8), benchmarkPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), benchmarkPerformance.LastKey().Date);
            Assert.AreEqual(5, benchmarkPerformance.KeyCount);
            Assert.AreEqual(4, benchmarkPerformance.ValueCount);
            Assert.AreEqual(Math.Round(-0.01176468202986730, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(0), 6));
            Assert.AreEqual(Math.Round(0.000604537450206063, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(1), 6));
            Assert.AreEqual(Math.Round(0.021621026833037300, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(2), 6));
            Assert.AreEqual(Math.Round(0.006384157281872880, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(3), 6));

            Assert.AreEqual(new DateTime(2013, 10, 7), performance.FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), performance.LastKey().Date);
            Assert.AreEqual(5, performance.ValueCount);
            Assert.AreEqual(5, performance.KeyCount);
            Assert.AreEqual(0.0, performance.GetAt(0));
            Assert.AreEqual(Math.Round(-0.011468930, 6), Math.Round(performance.GetAt(1), 6));
            Assert.AreEqual(Math.Round(0.0006024946, 6), Math.Round(performance.GetAt(2), 6));
            Assert.AreEqual(Math.Round(0.0215563200, 6), Math.Round(performance.GetAt(3), 6));
            Assert.AreEqual(Math.Round(0.0063658010, 6), Math.Round(performance.GetAt(4), 6));

            // This is a side-effect of how we calculate performance from the equity series in this test.
            Assert.AreEqual(4, equityPerformance.ValueCount);
            Assert.AreEqual(new DateTime(2013, 10, 8), equityPerformance.DropMissing().FirstKey());
            Assert.AreEqual(new DateTime(2013, 10, 11), equityPerformance.LastKey());

            // No need to run TestSampleAlignmentsRelative(...) here, since this algorithm will already have that ran
            // in the test SamplesNotMisalignedRelative().
        }

        [Test]
        public void BasicTemplateFrameworkAlgorithmSamplesNotMisalignedAbsolute()
        {
            var backtestResults = GetResults(nameof(BasicTemplateFrameworkAlgorithm), new DateTime(2013, 10, 7), new DateTime(2013, 10, 11));
            var benchmarkSeries = backtestResults.Charts["Benchmark"].Series["Benchmark"];
            var equitySeries = backtestResults.Charts["Strategy Equity"].Series["Equity"];
            var performanceSeries = backtestResults.Charts["Strategy Equity"].Series["Daily Performance"];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();
            var benchmarkPerformance = benchmark.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();

            // Before 2020-01-10 on master, the following Frame is created:
            // ====================================================================================
            // |                           equity               bench                perf         |
            // | 10/7/2013 12:00:00 AM  -> <missing>            <missing>            2.585847E-05 |
            // | 10/8/2013 12:00:00 AM  -> -0.0123926275514368  -0.00835040609378598 -0.01239263  |
            // | 10/9/2013 12:00:00 AM  -> 0.000602147816236763 -0.0117646820298673  0.0006021478 |
            // | 10/10/2013 12:00:00 AM -> 0.0215439204116712   0.000604537450206063 0.02154392   |
            // | 10/11/2013 12:00:00 AM -> 0.00636221601330912  0.0216210268330373   0.006362216  |
            // | 10/12/2013 12:00:00 AM -> 0                    0.00638415728187288  0            |
            // ====================================================================================
            //
            // Since benchmark is set at daily resolution, the benchmark data gets shifted forward by one day, causing misalignment.

            Assert.AreEqual(new DateTime(2013, 10, 8), benchmarkPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), benchmarkPerformance.LastKey().Date);
            Assert.AreEqual(5, benchmarkPerformance.KeyCount);
            Assert.AreEqual(4, benchmarkPerformance.ValueCount);
            Assert.AreEqual(Math.Round(-0.01176468202986730, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(0), 6));
            Assert.AreEqual(Math.Round(0.000604537450206063, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(1), 6));
            Assert.AreEqual(Math.Round(0.021621026833037300, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(2), 6));
            Assert.AreEqual(Math.Round(0.006384157281872880, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(3), 6));

            Assert.AreEqual(new DateTime(2013, 10, 7), performance.FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), performance.LastKey().Date);
            Assert.AreEqual(5, performance.ValueCount);
            Assert.AreEqual(5, performance.KeyCount);
            Assert.AreEqual(Math.Round(2.585847E-05, 6), Math.Round(performance.GetAt(0), 6));
            Assert.AreEqual(Math.Round(-0.012392630, 6), Math.Round(performance.GetAt(1), 6));
            Assert.AreEqual(Math.Round(0.0006021478, 6), Math.Round(performance.GetAt(2), 6));
            Assert.AreEqual(Math.Round(0.0215439200, 6), Math.Round(performance.GetAt(3), 6));
            Assert.AreEqual(Math.Round(0.0063622160, 6), Math.Round(performance.GetAt(4), 6));

            // This is a side-effect of how we calculate performance from the equity series in this test.
            Assert.AreEqual(4, equityPerformance.ValueCount);
            Assert.AreEqual(new DateTime(2013, 10, 8), equityPerformance.DropMissing().FirstKey());
            Assert.AreEqual(new DateTime(2013, 10, 11), equityPerformance.LastKey());

            // No need to run TestSampleAlignmentsRelative(...) here, since this algorithm will already have that ran
            // in the SamplesNotMisalignedRelative() test.
        }

        [Test]
        public void ResolutionSwitchingAlgorithmSamplesNotMisalignedAbsolute()
        {
            var backtestResults = GetResults(nameof(ResolutionSwitchingAlgorithm), new DateTime(2013, 10, 7), new DateTime(2013, 10, 11));
            var benchmarkSeries = backtestResults.Charts["Benchmark"].Series["Benchmark"];
            var equitySeries = backtestResults.Charts["Strategy Equity"].Series["Equity"];
            var performanceSeries = backtestResults.Charts["Strategy Equity"].Series["Daily Performance"];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();
            var benchmarkPerformance = benchmark.ResampleEquivalence(dt => dt.Date, s => s.LastValue()).PercentChange();

            // Before 2020-01-10, the following Frame is created:
            // ==================================================================================
            // |                           equity              bench                perf        |
            // | 10/7/2013 12:00:00 AM  -> <missing>           <missing>            0           |
            // | 10/8/2013 12:00:00 AM  -> 0                   -0.00835040609378598 0           |
            // | 10/9/2013 12:00:00 AM  -> -0.0111101029999999 -0.0117646820298673  -0.0111101  |
            // | 10/10/2013 12:00:00 AM -> 0.0107871432728369  0.000604537450206063 0.01078714  | <- (perf): This is the date we changed to minutely data.
            // | 10/11/2013 12:00:00 AM -> 0.00621556629004656 0.0216210268330373   0.006215567 | <- If we were still using daily data, this value would be here: |
            // | 10/12/2013 12:00:00 AM -> 0                   0.00638415728187288  0           | <----------------------------------------------------------------
            // ==================================================================================
            //
            // On 2013-10-10, we switch from Daily Resolution to Minute. From here onwards, our performance values
            // are shifted backwards by 1 with the exception of the value on 10/10/2013 12:00:00 AM.

            Assert.AreEqual(new DateTime(2013, 10, 8), benchmarkPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), benchmarkPerformance.LastKey().Date);
            Assert.AreEqual(5, benchmarkPerformance.KeyCount);
            Assert.AreEqual(4, benchmarkPerformance.ValueCount);
            Assert.AreEqual(Math.Round(-0.01176468202986730, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(0), 6));
            Assert.AreEqual(Math.Round(0.000604537450206063, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(1), 6));
            Assert.AreEqual(Math.Round(0.021621026833037300, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(2), 6));
            Assert.AreEqual(Math.Round(0.006384157281872880, 6), Math.Round(benchmarkPerformance.DropMissing().GetAt(3), 6));

            Assert.AreEqual(new DateTime(2013, 10, 7), performance.FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), performance.LastKey().Date);
            Assert.AreEqual(5, performance.ValueCount);
            Assert.AreEqual(5, performance.KeyCount);
            Assert.AreEqual(Math.Round(0.0, 6), Math.Round(performance.GetAt(0), 6));
            Assert.AreEqual(Math.Round(-0.01111010, 6), Math.Round(performance.GetAt(1), 6));
            Assert.AreEqual(Math.Round(0.010787140, 6), Math.Round(performance.GetAt(2), 6));
            // Unknown value should go here.
            Assert.AreEqual(Math.Round(0.006215567, 6), Math.Round(performance.GetAt(4), 6));

            Assert.AreEqual(4, equityPerformance.ValueCount);
            Assert.AreEqual(new DateTime(2013, 10, 8), equityPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), equityPerformance.LastKey().Date);
        }

        private void TestSampleAlignmentsRelative(
            Series<DateTime, double> equityPerformance,
            Series<DateTime, double> benchmarkPerformance,
            Series<DateTime, double> performance)
        {
            Assert.AreEqual(
                equityPerformance.ValueCount,
                performance.ValueCount - 1,
                "Calculated equity performance series or performance series contains more values than expected"
            );
            Assert.AreEqual(
                equityPerformance.Values.Select(x => Math.Round(x, 5)).ToList(), performance.Values.Skip(1).Select(x => Math.Round(x, 5)).ToList(),
                "Calculated equity performance value does not match performance series value. This most likely means that the performance series has been sampled more than it should have and is misaligned as a result."
            );
            Assert.AreEqual(
                performance.ValueCount - 1,
                benchmarkPerformance.ValueCount,
                "Performance and benchmark performance series are misaligned"
            );
            Assert.IsTrue(
                (performance - benchmarkPerformance).Values.All(x => x <= 0.0005 && x >= -0.0005),
                "Equity performance and benchmark performance have diverging values. This most likely means that the performance and calculated benchmark performance series are misaligned."
            );
        }

        private static Series<DateTime, double> ToDeedleSeries(Series series)
        {
            return new Series<DateTime, double>(series.Values.Select(x => new KeyValuePair<DateTime, double>(Time.UnixTimeStampToDateTime(x.x), (double)x.y)));
        }
    }
}
