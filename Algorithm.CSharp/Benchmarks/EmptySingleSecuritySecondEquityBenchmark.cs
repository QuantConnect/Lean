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
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    /// <summary>
    /// Benchmark Algorithm: Pure processing of 1 equity second resolution with the same benchmark.
    /// </summary>
    /// <remarks>
    /// This should eliminate the synchronization part of LEAN and focus on measuring the performance of a single datafeed and event handling system.
    /// </remarks>
    public class EmptySingleSecuritySecondEquityBenchmark : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2008, 01, 01);
            SetEndDate(2009, 01, 01);
            SetBenchmark(dt => 1m);
            AddEquity("SPY", Resolution.Second);
        }

        public override void OnData(Slice data)
        {
        }
    }
}