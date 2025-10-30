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

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    /// <summary>
    /// Benchmark Algorithm that adds SPX option chain but does not trade it.
    /// This is an interesting benchmark because SPX option chains are large
    /// </summary>
    public class EmptySPXOptionChainBenchmark : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetEndDate(2020, 6, 1);

            var index = AddIndex("SPX");
            var option = AddOption(index);
            option.SetFilter(x => x.IncludeWeeklys().Strikes(-30, 30).Expiration(0, 7));
        }
    }
}
