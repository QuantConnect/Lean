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
    /// Benchmark Algorithm: The minimalist basic template algorithm benchmark strategy.
    /// </summary>
    /// <remarks>
    /// All new projects in the cloud are created with the basic template algorithm. It uses a minute algorithm
    /// over a long period of time to establish a baseline.
    /// </remarks>
    public class BasicTemplateBenchmark : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2000, 01, 01);
            SetEndDate(2017, 01, 01);
            SetBenchmark(dt => 1m);
            AddEquity("SPY");
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
                Debug("Purchased Stock");
            }
        }
    }
}