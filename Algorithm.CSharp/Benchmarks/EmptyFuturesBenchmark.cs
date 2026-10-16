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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    /// <summary>
    /// Benchmark Algorithm that adds the S&amp;P 500 E-mini futures chain but does not trade it.
    /// Measures the performance of futures universe selection, chain loading, and data synchronization.
    /// </summary>
    public class EmptyFuturesBenchmark : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2023, 1, 1);

            var future = AddFuture(Futures.Indices.SP500EMini);
            future.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));
        }
    }
}
