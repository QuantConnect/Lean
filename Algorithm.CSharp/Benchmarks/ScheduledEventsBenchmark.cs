/*
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

using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    public class ScheduledEventsBenchmark : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2011, 1, 1);
            SetEndDate(2018, 1, 1);
            SetCash(100000);
            AddEquity("SPY");
            foreach (int period in Enumerable.Range(0, 300))
            {
                Schedule.On(DateRules.EveryDay("SPY"), TimeRules.AfterMarketOpen("SPY", period), Rebalance);
                Schedule.On(DateRules.EveryDay("SPY"), TimeRules.BeforeMarketClose("SPY", period), Rebalance);
            }
        }

        public override void OnData(Slice data) { }
        private void Rebalance() { }
    }
}