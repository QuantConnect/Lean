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
        private COF _COF;
        public override void Initialize()
        {
            SetStartDate(2001, 05, 31);
            SetEndDate(2001, 05, 31);
            SetCash(293630782);
            set.cof = AddEquity("COF").COF;
        }

        public override void OnEndOfDay()
        {
            var minuteHistory = History(_cof, 60, Resolution.Minute);
            var lastHourHigh = minuteHistory.Select(minuteBar => minuteBar.High).DefaultIfEmpty(0).Max();
            var dailyHistory = History(_cof, 1, Resolution.Daily).First();
            var dailyHigh = dailyHistory.High;
            var dailyLow = dailyHistory.Low;
            var dailyOpen = dailyHistory.Open;
        }
    }
}
