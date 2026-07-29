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
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    /// <summary>
    /// Benchmark Algorithm: Loading and synchronization of an S&amp;P 500 E-mini futures chain.
    /// </summary>
    public class EmptyFuturesBenchmark : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);

            var future = AddFuture(Futures.Indices.SP500EMini);
            future.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));
        }
    }
}
