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

using System.Linq;
using QuantConnect.Algorithm;

namespace QuantConnect.Tests.Engine
{
    public static class PerformanceBenchmarkAlgorithms
    {
        public static QCAlgorithm SingleSecurity_Second => new SingleSecurity_Second_BenchmarkTest();
        public static QCAlgorithm FiveHundredSecurity_Second => new FiveHundredSecurity_Second_BenchmarkTest();

        private class SingleSecurity_Second_BenchmarkTest : QCAlgorithm
        {
            public override void Initialize()
            {
                SetStartDate(2008, 01, 01);
                SetEndDate(2009, 01, 01);
                SetCash(100000);
                SetBenchmark(time => 0m);
                AddEquity("SPY", Resolution.Second, "usa", true);
            }
        }

        private class FiveHundredSecurity_Second_BenchmarkTest : QCAlgorithm
        {
            public override void Initialize()
            {
                SetStartDate(2018, 02, 01);
                SetEndDate(2018, 02, 01);
                SetCash(100000);
                SetBenchmark(time => 0m);
                foreach (var symbol in QuantConnect.Algorithm.CSharp.Benchmarks.Symbols.Equity.All.Take(500))
                {
                    AddEquity(symbol, Resolution.Second);
                }
            }
        }
    }
}