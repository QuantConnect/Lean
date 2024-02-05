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
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Report;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class BacktestingResultHandlerTests
    {
        public BacktestingResultHandler GetResults(string algorithm, DateTime algoStart, DateTime algoEnd)
        {
            // Required, otherwise LocalObjectStoreTests overwrites the "object-store-root" config value
            // and causes the algorithm to error out.
            Config.Reset();

            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(algorithm,
               new Dictionary<string, string>(),
               Language.CSharp,
               AlgorithmStatus.Completed);

            // The AlgorithmRunner uses the `RegressionResultHandler` but doesn't do any sampling.
            // It defaults to the behavior of the `BacktestingResultHandler` class in `results.ProcessSynchronousEvents()`
            var backtestResults = AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                startDate: algoStart,
                endDate: algoEnd);

            return backtestResults.Results;
        }

        [TestCase(nameof(BasicTemplateAlgorithm), true)]
        [TestCase(nameof(BasicTemplateDailyAlgorithm), true)]
        [TestCase(nameof(ResolutionSwitchingAlgorithm), false)]
        public void SamplesNotMisalignedRelative(string algorithm, bool shouldSucceed)
        {
            // After PR #4003 is merged (https://github.com/QuantConnect/Lean/pull/4003),
            // this test will fail with any daily algorithm, such as BasicTemplateDailyAlgorithm.

            var backtestResults = GetResults(algorithm, new DateTime(2013, 10, 7), new DateTime(2013, 10, 11));
            var benchmarkSeries = backtestResults.Charts[BaseResultsHandler.BenchmarkKey].Series[BaseResultsHandler.BenchmarkKey];
            var equitySeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.EquityKey];
            var performanceSeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.ReturnKey];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.FirstValue()).PercentChange();
            var benchmarkPerformance = benchmark.ResampleEquivalence(dt => dt.Date, s => s.FirstValue()).PercentChange();
            performance = performance.ResampleEquivalence(dt => dt.Date, s => s.FirstValue());

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
            // | 10/9/2013 12:00:00 AM  -> -0.0114689260000001  -0.0117646820298673  -0.01145515  |
            // | 10/10/2013 12:00:00 AM -> 0.000602494970229067 0.000604537450206063 0.000601821 |
            // | 10/11/2013 12:00:00 AM -> 0.0215322302204622   0.0216210268330373   0.02153223   |
            // | 10/12/2013 12:00:00 AM -> 0.0063588373516552   0.00638415728187288  0.006358837  |
            // ====================================================================================
            //
            // And it produces this Frame in master for `BasicTemplateAlgorithm` (minute resolution):
            // ====================================================================================
            // |                             equity               bench                perf       |
            // | 10/7/2013 12:00:00 AM  -> <missing>            <missing>            2.69427E-05 |
            // | 10/8/2013 12:00:00 AM  -> -0.0117197546154655  -0.00835040609378598 -0.01171975  |
            // | 10/9/2013 12:00:00 AM  -> 0.000602640205305941 -0.0117646820298673  0.0006019659 |
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
            //  [0.02162102, 0.02153223],
            //  [0.00638415, 0.00636580]]
            //
            // If we manually calculate the beta with the series put above,  we get the beta: 0.8757695
            // If we manually calculate the beta without the invalid values, we get the beta: 0.9892104

            TestSampleAlignmentsRelative(equityPerformance, benchmarkPerformance, performance, shouldSucceed);
        }

        [Test]
        public void BasicTemplateAlgorithmSamplesNotMisalignedAbsolute()
        {
            var backtestResults = GetResults(nameof(BasicTemplateAlgorithm), new DateTime(2013, 10, 7), new DateTime(2013, 10, 11));
            var benchmarkSeries = backtestResults.Charts[BaseResultsHandler.BenchmarkKey].Series[BaseResultsHandler.BenchmarkKey];
            var equitySeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.EquityKey];
            var performanceSeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.ReturnKey];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            // Lineup all our samples
            // Equity and benchmark will need to be converted into performance values
            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.FirstValue()).PercentChange();
            var benchmarkPerformance = benchmark.PercentChange();

            // Before 2020-9-30 on master, the following Frame is created:
            // ====================================================================================
            // |                           equity               bench                perf         |
            // | 2013-10-07 20:00:00 -> <missing>            <missing>            -9.20427E-05    |
            // | 2013-10-08 00:00:00 -> -0.0116913921108040  -0.0116470501350577  <missing>       |
            // | 2013-10-08 20:00:00 -> <missing>            <missing>            -0.01169139     |
            // | 2013-10-09 00:00:00 -> 0.000602020218337738 0.000604391026446547 <missing>       |
            // | 2013-10-09 20:00:00 -> <missing>            <missing>            0.0006020202    |
            // | 2013-10-10 00:00:00 -> 0.02156944065055612  0.0216814919573348   <missing>       |
            // | 2013-10-10 20:00:00 -> <missing>            <missing>            0.02156944      |
            // | 2013-10-11 00:00:00 -> 0.00641960569172269  0.00644312892467460  <missing>       |
            // | 2013-10-11 20:00:00 -> <missing>            <missing>            0.006419605     |
            // ===================================================================================|
            //
            // After new adjustments
            // ====================================================================================
            // |                           equity           bench                 perf            |
            // | 10/7/2013 12:00:00 AM  -> <missing>        <missing>             0               |
            // | 10/8/2013 12:00:00 AM  -> -0.0002128590    -0.00870388175321252  -0.0002128589   |
            // | 10/9/2013 12:00:00 AM  -> -0.0115428009    -0.01158779130093582  -0.0115427999   |
            // | 10/10/2013 12:00:00 AM -> 0.00054174422    0.000543757827876358  0.00054174399   |
            // | 10/11/2013 12:00:00 AM -> 0.02207915613    0.022165997700413814  0.02207916      |
            // | 10/11/2013 08:00:00 PM -> <missing>        0.006263266301918913  0.006239327     | <- Only missing equity because converted to daily values, doesn't have an 8pm point
            // ====================================================================================

            // Verify Benchmark values
            Assert.AreEqual(new DateTime(2013, 10, 8), benchmarkPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), benchmarkPerformance.LastKey().Date);
            Assert.AreEqual(6, benchmarkPerformance.KeyCount);
            Assert.AreEqual(5, benchmarkPerformance.ValueCount); // Is 5 because first point is missing

            var expectedBenchmarkPerformance = new List<double>
            {
                0,                          // First sample at start, seen as missing since percent change won't exists for that day
                -0.0087038817532125255,     // 10/7 - 10/8
                -0.011587791300935823,      // 10/8 - 10/9
                0.00054375782787635836,     // 10/9 - 10/10
                0.022165997700413814,       // 10/10 - 10/11
                0.0062632663019189135       // 10/11 12AM - 10/11 8PM
            };

            Assert.AreEqual(expectedBenchmarkPerformance, benchmarkPerformance.ValuesAll.ToList());

            // Verify Daily Performance values
            Assert.AreEqual(new DateTime(2013, 10, 7), performance.FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), performance.LastKey().Date);
            Assert.AreEqual(6, performance.ValueCount);
            Assert.AreEqual(6, performance.KeyCount);

            var expectedPerformance = new List<double>
            {
                0,                      // First sample at start, zero because no change has occurred yet
                -0.0002128589,          // 10/7 - 10/8 <- we buy at open here
                -0.011542799999999999,  // 10/8 - 10/9
                0.00054174399999999993, // 10/9 - 10/10
                0.02207916,             // 10/10 - 10/11
                0.006239327             // 10/11 12AM - 10/11 8PM
            };

            Assert.AreEqual(expectedPerformance, performance.Values.ToList());


            // Verify equity performance
            Assert.AreEqual(4, equityPerformance.ValueCount);
            Assert.AreEqual(new DateTime(2013, 10, 8), equityPerformance.DropMissing().FirstKey());
            Assert.AreEqual(new DateTime(2013, 10, 11), equityPerformance.LastKey());

            var expectedEquityPerformance = new List<double>
            {
                0,                          // First sample at start, seen as missing since percent change won't exists for that day
                -0.00021285900000002583,    // 10/7 - 10/8 <- we buy at open here
                -0.011542800989075775,      // 10/8 - 10/9
                0.00054174422990819744,     // 10/9 - 10/10
                0.022079156131712498        // 10/10 - 10/11
            };

            Assert.AreEqual(expectedEquityPerformance, equityPerformance.ValuesAll.ToList());
        }

        [Test]
        public void BasicTemplateDailyAlgorithmSamplesNotMisalignedAbsolute()
        {
            // This test will produce incorrect results, but is here to detect if any changes occur to the Sampling.
            var backtestResults = GetResults(nameof(BasicTemplateDailyAlgorithm), new DateTime(2013, 10, 7), new DateTime(2013, 10, 11));
            var benchmarkSeries = backtestResults.Charts[BaseResultsHandler.BenchmarkKey].Series[BaseResultsHandler.BenchmarkKey];
            var equitySeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.EquityKey];
            var performanceSeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.ReturnKey];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            // Lineup all our samples
            // Equity and benchmark will need to be converted into performance values
            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.FirstValue()).PercentChange();
            var benchmarkPerformance = benchmark.PercentChange();

            // Before 2020-01-10 on master, the following Frame is created:
            // ====================================================================================
            // |                           equity               bench                perf         |
            // | 10/7/2013 12:00:00 AM  -> <missing>            <missing>            0            |
            // | 10/8/2013 12:00:00 AM  -> 0                    -0.00835040609378598 0            |
            // | 10/9/2013 12:00:00 AM  -> -0.0114689260000001  -0.0117646820298673  -0.01145515  |
            // | 10/10/2013 12:00:00 AM -> 0.000602494970229067 0.000604537450206063 0.000601821  |
            // | 10/11/2013 12:00:00 AM -> 0.0215322302204622   0.0216210268330373   0.02153223   |
            // | 10/12/2013 12:00:00 AM -> 0.0063588373516552   0.00638415728187288  0.006358837  |
            // ====================================================================================
            //
            // Samples were aligned since both benchmark and strategy only use daily data.

            Assert.AreEqual(new DateTime(2013, 10, 8), benchmarkPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 12), benchmarkPerformance.LastKey().Date);
            Assert.AreEqual(6, benchmarkPerformance.KeyCount);
            Assert.AreEqual(5, benchmarkPerformance.ValueCount);

            var expectedBenchmarkPerformance = new List<double>
            {
                0,                          // First sample at start 10/7 12AM, seen as missing since percent change won't exists for that day
                -0.0087038817532125255,     // 10/7 - 10/8
                -0.011587791300935823,      // 10/8 - 10/9
                0.00054375782787635836,     // 10/9 - 10/10
                0.022165997700413814,       // 10/10 - 10/11
                0.0062632663019189135       // 10/11 12AM - 10/11 8PM
            };

            Assert.AreEqual(expectedBenchmarkPerformance, benchmarkPerformance.ValuesAll.ToList());

            // Verify daily performance
            Assert.AreEqual(new DateTime(2013, 10, 7), performance.FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 12), performance.LastKey().Date);
            Assert.AreEqual(6, performance.ValueCount);
            Assert.AreEqual(6, performance.KeyCount);

            var expectedPerformance = new List<double>
            {
                0,                      // First sample at start 10/7 12AM, zero because no change has occurred yet
                0,                      // 10/7 - 10/8 <- We get first data at 12AM on 10/8
                -0.011770290000000001,  // 10/8 - 10/9 <- We buy at with OnMarketOpen order
                0.0005425408,           // 10/9 - 10/10
                0.02211161,             // 10/10 - 10/11
                0.0062483               // 10/11 12AM - 10/11 8PM
            };

            Assert.AreEqual(expectedPerformance, performance.Values.ToList());

            // Verify equity performance
            Assert.AreEqual(5, equityPerformance.ValueCount);
            Assert.AreEqual(new DateTime(2013, 10, 8), equityPerformance.DropMissing().FirstKey());
            Assert.AreEqual(new DateTime(2013, 10, 12), equityPerformance.LastKey());

            var expectedEquityPerformance = new List<double>
            {
                0,                          // First sample at start 10/7 12AM, seen as missing since percent change won't exists for that day
                0,                          // 10/7 - 10/8 <- We get first data at 12AM on 10/8
                -0.011770286000000052,      // 10/8 - 10/9 <- We buy at with OnMarketOpen order
                0.000542540861101854,       // 10/9 - 10/10
                0.022111611742941455,       // 10/10 - 10/11
                0.0062483003408066885       // 10/11 - 10/12
            };

            Assert.AreEqual(expectedEquityPerformance, equityPerformance.ValuesAll.ToList());
        }

        [Test]
        public void BasicTemplateFrameworkAlgorithmSamplesNotMisalignedAbsolute()
        {
            var backtestResults = GetResults(nameof(BasicTemplateFrameworkAlgorithm), new DateTime(2013, 10, 7), new DateTime(2013, 10, 11));
            var benchmarkSeries = backtestResults.Charts[BaseResultsHandler.BenchmarkKey].Series[BaseResultsHandler.BenchmarkKey];
            var equitySeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.EquityKey];
            var performanceSeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.ReturnKey];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.FirstValue()).PercentChange();
            var benchmarkPerformance = benchmark.PercentChange();

            // Before 2020-01-10 on master, the following Frame is created:
            // ====================================================================================
            // |                           equity               bench                perf         |
            // | 10/7/2013 12:00:00 AM  -> <missing>            <missing>            2.69427E-05  |
            // | 10/8/2013 12:00:00 AM  -> -0.012379760         -0.00835040609378598 -0.01237976  |
            // | 10/9/2013 12:00:00 AM  -> 0.0006023682         -0.0117646820298673  0.0006023682 |
            // | 10/10/2013 12:00:00 AM -> 0.0215518000         0.000604537450206063 0.02155180   |
            // | 10/11/2013 12:00:00 AM -> 0.0063644940         0.0216210268330373   0.006364494  |
            // | 10/12/2013 12:00:00 AM -> 0                    0.00638415728187288  0            |
            // ====================================================================================
            //
            // Since benchmark is set at daily resolution, the benchmark data gets shifted forward by one day, causing misalignment.

            // Verify benchmark performance
            Assert.AreEqual(new DateTime(2013, 10, 8), benchmarkPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), benchmarkPerformance.LastKey().Date);
            Assert.AreEqual(6, benchmarkPerformance.KeyCount);
            Assert.AreEqual(5, benchmarkPerformance.ValueCount);

            var expectedBenchmarkPerformance = new List<double>
            {
                0,                          // First sample at start 10/7 12AM, seen as missing since percent change won't exists for that day
                -0.0087038817532125255,     // 10/7 - 10/8
                -0.011587791300935823,      // 10/8 - 10/9
                0.00054375782787635836,     // 10/9 - 10/10
                0.022165997700413814,       // 10/10 - 10/11
                0.0062632663019189135       // 10/11 12AM - 10/11 8PM
            };

            Assert.AreEqual(expectedBenchmarkPerformance, benchmarkPerformance.ValuesAll.ToList());

            // Verify daily performance
            Assert.AreEqual(new DateTime(2013, 10, 7), performance.FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), performance.LastKey().Date);
            Assert.AreEqual(6, performance.ValueCount);
            Assert.AreEqual(6, performance.KeyCount);

            var expectedPerformance = new List<double>
            {
                0,                      // First sample at start, zero because no change has occurred yet
                -0.0002128589,          // 10/7 - 10/8 <- we buy at open here
                -0.01190911,            // 10/8 - 10/9
                0.0005419449,           // 10/9 - 10/10
                0.02208734,             // 10/10 - 10/11
                0.006241589             // 10/11 12AM - 10/11 8PM
            };

            Assert.AreEqual(expectedPerformance, performance.Values.ToList());


            // Verify equity performance
            Assert.AreEqual(4, equityPerformance.ValueCount);
            Assert.AreEqual(new DateTime(2013, 10, 8), equityPerformance.DropMissing().FirstKey());
            Assert.AreEqual(new DateTime(2013, 10, 11), equityPerformance.LastKey());

            var expectedEquityPerformance = new List<double>
            {
                0,                          // First sample at start, seen as missing since percent change won't exists for that day
                -0.00021285900000002583,    // 10/7 - 10/8 <- we buy at open here
                -0.011909110961450057,      // 10/8 - 10/9
                0.00054194506802551345,     // 10/9 - 10/10
                0.022087336992789988        // 10/10 - 10/11
            };

            Assert.AreEqual(expectedEquityPerformance, equityPerformance.ValuesAll.ToList());
        }

        [Test]
        public void ResolutionSwitchingAlgorithmSamplesNotMisalignedAbsolute()
        {
            // This test will produce incorrect results, but is here to detect if any changes occur to the Sampling.
            var backtestResults = GetResults(nameof(ResolutionSwitchingAlgorithm), new DateTime(2013, 10, 7), new DateTime(2013, 10, 11));
            var benchmarkSeries = backtestResults.Charts[BaseResultsHandler.BenchmarkKey].Series[BaseResultsHandler.BenchmarkKey];
            var equitySeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.EquityKey];
            var performanceSeries = backtestResults.Charts[BaseResultsHandler.StrategyEquityKey].Series[BaseResultsHandler.ReturnKey];
            var benchmark = ToDeedleSeries(benchmarkSeries);
            var equity = ToDeedleSeries(equitySeries);
            var performance = ToDeedleSeries(performanceSeries).SelectValues(x => x / 100);

            var equityPerformance = equity.ResampleEquivalence(dt => dt.Date, s => s.FirstValue()).PercentChange();
            var benchmarkPerformance = benchmark.PercentChange();

            // Before 2020-01-10, the following Frame is created:
            // ==================================================================================
            // |                           equity              bench                perf        |
            // | 10/7/2013 12:00:00 AM  -> <missing>           <missing>            0           |
            // | 10/8/2013 12:00:00 AM  -> 0                   -0.00835040609378598 0           |
            // | 10/9/2013 12:00:00 AM  -> -0.0114877029999999 -0.0117646820298673  -0.0114877  |
            // | 10/10/2013 12:00:00 AM -> 0.0111613132728369  0.000604537450206063 0.01116131  | <- (perf): This is the date we changed to minutely data.
            // | 10/11/2013 12:00:00 AM -> 0.00642813429004656 0.0216210268330373   0.006428134 | <- If we were still using daily data, this value would be here: |
            // | 10/12/2013 12:00:00 AM -> 0                   0.00638415728187288  0           | <----------------------------------------------------------------
            // ==================================================================================
            //
            // On 2013-10-10, we switch from Daily Resolution to Minute. From here onwards, our performance values
            // are shifted backwards by 1 with the exception of the value on 10/10/2013 12:00:00 AM.
            Frame.CreateEmpty<DateTime, string>().Join("equity", equityPerformance).Join("bench", benchmarkPerformance).Join("perf", performance).Print();

            // Verify benchmark performance
            Assert.AreEqual(new DateTime(2013, 10, 8), benchmarkPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), benchmarkPerformance.LastKey().Date);
            Assert.AreEqual(6, benchmarkPerformance.KeyCount);
            Assert.AreEqual(5, benchmarkPerformance.ValueCount);

            var expectedBenchmarkPerformance = new List<double>
            {
                0,                          // First sample at start 10/7 12AM, seen as missing since percent change won't exists for that day
                -0.0087038817532125255,     // 10/7 - 10/8
                -0.011587791300935823,      // 10/8 - 10/9
                0.00054375782787635836,     // 10/9 - 10/10
                0.022165997700413814,       // 10/10 - 10/11
                0.0062632663019189135       // 10/11 12AM - 10/11 8PM
            };

            Assert.AreEqual(expectedBenchmarkPerformance, benchmarkPerformance.ValuesAll.ToList());

            // Verify daily performance
            Assert.AreEqual(new DateTime(2013, 10, 7), performance.FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), performance.LastKey().Date);
            Assert.AreEqual(6, performance.ValueCount);
            Assert.AreEqual(6, performance.KeyCount);

            var expectedPerformance = new List<double>
            {
                0,                      // First sample at start 10/7 12AM, zero because no change has occurred yet
                0,                      // 10/7 - 10/8 <- We get first data at 12AM on 10/8
                -0.01112113,            // 10/8 - 10/9 <- We buy at with OnMarketOpen order
                -3.291606E-05,          // 10/9 - 10/10
                0.01100997,             // 10/10 - 10/11
                0.005968033             // 10/11 12AM - 10/11 8PM
            };

            Assert.AreEqual(expectedPerformance, performance.Values.ToList());

            // Verify equity performance
            Assert.AreEqual(4, equityPerformance.ValueCount);
            Assert.AreEqual(new DateTime(2013, 10, 8), equityPerformance.DropMissing().FirstKey().Date);
            Assert.AreEqual(new DateTime(2013, 10, 11), equityPerformance.LastKey().Date);

            var expectedEquityPerformance = new List<double>
            {
                0,                          // First sample at start 10/7 12AM, seen as missing since percent change won't exists for that day
                0,                          // 10/7 - 10/8 <- We get first data at 12AM on 10/8
                -0.011121126999999979,      // 10/8 - 10/9 <- We buy at with OnMarketOpen order
                -3.29160637250732E-05,      // 10/9 - 10/10
                0.011009966611364035,       // 10/10 - 10/11
            };

            Assert.AreEqual(expectedEquityPerformance, equityPerformance.ValuesAll.ToList());
        }

        private void TestSampleAlignmentsRelative(
            Series<DateTime, double> equityPerformance,
            Series<DateTime, double> benchmarkPerformance,
            Series<DateTime, double> performance,
            bool shouldSucceed)
        {
            // Fill our missing values before comparing
            equityPerformance = equityPerformance.FillMissing(0.0d);
            benchmarkPerformance = benchmarkPerformance.FillMissing(0.0d);
            performance = performance.FillMissing(0.0d);

            var equityPerformanceContainsExpectedCount = equityPerformance.ValueCount == performance.ValueCount;
            var equityPerformanceContainsExpectedCountMessage = "Calculated equity performance series or performance series contains more values than expected";
            var equityPerformanceMatchesPerformance = equityPerformance.Values.Select(x => Math.Round(x, 5)).ToList().SequenceEqual(performance.Values.Select(x => Math.Round(x, 5)).ToList());
            var equityPerformanceMatchesPerformanceMessage = "Calculated equity performance value does not match performance series value. This most likely means that the performance series has been sampled more than it should have and is misaligned as a result.";
            var benchmarkPerformanceAndPerformanceAreAligned = benchmarkPerformance.ValueCount == performance.ValueCount;
            var benchmarkPerformanceAndPerformanceAreAlignedMessage = "Performance and benchmark performance series are misaligned";

            // Skip the first 3 because [1] = start of algorithm samples always 0, [2] = first data comes in during this first day, we place our orders for open
            var benchmarkPerformanceAndPerformanceDoNotDiverge = (performance - benchmarkPerformance).Values.Skip(2).All(x => x <= 0.0005 && x >= -0.0005);
            var benchmarkPerformanceAndPerformanceDoNotDivergeMessage = "Equity performance and benchmark performance have diverging values. This most likely means that the performance and calculated benchmark performance series are misaligned.";

            if (!shouldSucceed)
            {
                // All tests are passing, though we aren't expecting that.
                if (equityPerformanceContainsExpectedCount && equityPerformanceMatchesPerformance &&
                    benchmarkPerformanceAndPerformanceAreAligned && benchmarkPerformanceAndPerformanceDoNotDiverge)
                {
                    Assert.Fail("All checks are passing on a test that should be failing.");
                }

                if (!equityPerformanceContainsExpectedCount)
                {
                    Log.Trace($"TestSampleAlignmentsRelative(): Test failed, but it was expected. Message: {equityPerformanceContainsExpectedCountMessage}");
                }
                if (!equityPerformanceMatchesPerformance)
                {
                    Log.Trace($"TestSampleAlignmentsRelative(): Test failed, but it was expected. Message: {equityPerformanceMatchesPerformanceMessage}");
                }
                if (!benchmarkPerformanceAndPerformanceAreAligned)
                {
                    Log.Trace($"TestSampleAlignmentsRelative(): Test failed, but it was expected. Message: {benchmarkPerformanceAndPerformanceAreAlignedMessage}");
                }
                if (!benchmarkPerformanceAndPerformanceDoNotDiverge)
                {
                    Log.Trace($"TestSampleAlignmentsRelative(): Test failed, but it was expected. Message: {benchmarkPerformanceAndPerformanceDoNotDivergeMessage}");
                }

                return;
            }

            Assert.IsTrue(equityPerformanceContainsExpectedCount, equityPerformanceContainsExpectedCountMessage);
            Assert.IsTrue(equityPerformanceMatchesPerformance, equityPerformanceMatchesPerformanceMessage);
            Assert.IsTrue(benchmarkPerformanceAndPerformanceAreAligned, benchmarkPerformanceAndPerformanceAreAlignedMessage);
            Assert.IsTrue(benchmarkPerformanceAndPerformanceDoNotDiverge, benchmarkPerformanceAndPerformanceDoNotDivergeMessage);
        }

        private static Series<DateTime, double> ToDeedleSeries(BaseSeries series)
        {
            return new Series<DateTime, double>(series.Values.Select(x =>
            {
                var value = 0d;
                switch (x)
                {
                    case ChartPoint chartPoint:
                        value = (double)chartPoint.y;
                        break;
                    case Candlestick candlestick:
                        value = (double)candlestick.Close;
                        break;
                }

                return new KeyValuePair<DateTime, double>(x.Time, value);
            }));
        }
    }
}
