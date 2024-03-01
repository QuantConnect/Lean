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
using NUnit.Framework;
using System.Threading;
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class UserDefinedUniverseTests
    {
        [Test]
        public void ThreadSafety()
        {
            // allow the system to stabilize
            Thread.Sleep(1000);
            var results = AlgorithmRunner.RunLocalBacktest(nameof(TestUserDefinedUniverseAlgorithm),
                new Dictionary<string, string> { { PerformanceMetrics.TotalOrders, "1" } },
                Language.CSharp,
                AlgorithmStatus.Completed,
                algorithmLocation: "QuantConnect.Tests.dll");

            Assert.GreaterOrEqual(TestUserDefinedUniverseAlgorithm.AdditionCount, 50, $"We added {TestUserDefinedUniverseAlgorithm.AdditionCount} times");
        }
    }

    public class TestUserDefinedUniverseAlgorithm : BasicTemplateAlgorithm
    {
        public static long AdditionCount;

        private Thread _thread;
        private CancellationTokenSource _cancellationTokenSource = new();
        private ManualResetEvent _threadStarted = new (false);
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            var spy = AddEquity("SPY", Resolution.Minute, dataNormalizationMode: DataNormalizationMode.Raw).Symbol;

            _thread = new Thread(() =>
            {
                _threadStarted.Set();
                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested && AdditionCount < 250)
                    {
                        var currentCount = Interlocked.Increment(ref AdditionCount);
                        var contract = QuantConnect.Symbol.CreateOption(spy, QuantConnect.Market.USA, OptionStyle.American, OptionRight.Call, currentCount, new DateTime(2022, 10, 10));
                        AddOptionContract(contract);


                        if (currentCount % 2 == 0)
                        {
                            RemoveSecurity("AAPL");
                        }
                        else
                        {
                            AddEquity("AAPL");
                        }
                        if (currentCount % 25 == 0)
                        {
                            Thread.Sleep(10);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Error(ex);
                    SetStatus(AlgorithmStatus.RuntimeError);
                }
            }) { IsBackground = true };
        }

        public override void OnData(Slice data)
        {
            if (!_threadStarted.WaitOne(0))
            {
                _thread.Start();
                _threadStarted.WaitOne();
            }
            base.OnData(data);
        }
        public override void OnEndOfAlgorithm()
        {
            _thread.StopSafely(TimeSpan.FromSeconds(2), _cancellationTokenSource);
            base.OnEndOfAlgorithm();
        }
    }
}
