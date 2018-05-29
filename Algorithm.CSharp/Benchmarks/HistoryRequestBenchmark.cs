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
    public class HistoryRequestBenchmark : QCAlgorithm
    {

        public override void Initialize()
        {
            SetStartDate(2015, 01, 01);
            SetEndDate(2018, 01, 01);
            SetCash(10000);
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Hour);

        }

        public override void OnData(Slice data)
        {
            var history = History("SPY", 2, Resolution.Daily);
        }
    }
}