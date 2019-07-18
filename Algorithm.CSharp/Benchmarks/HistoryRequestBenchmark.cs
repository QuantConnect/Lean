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

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    public class HistoryRequestBenchmark : QCAlgorithm
    {
        private Symbol _symbol;
        public override void Initialize()
        {
            SetStartDate(2010, 01, 01);
            SetEndDate(2018, 01, 01);
            SetCash(10000);
            _symbol = AddEquity("SPY").Symbol;
        }

        public override void OnEndOfDay()
        {
            var minuteHistory = History(_symbol, 60, Resolution.Minute);
            var lastHourHigh = minuteHistory.Select(minuteBar => minuteBar.High).DefaultIfEmpty(0).Max();
            var dailyHistory = History(_symbol, 1, Resolution.Daily).First();
            var dailyHigh = dailyHistory.High;
            var dailyLow = dailyHistory.Low;
            var dailyOpen = dailyHistory.Open;
        }
    }
}