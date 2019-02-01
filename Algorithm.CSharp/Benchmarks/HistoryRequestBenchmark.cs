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
using System.Diagnostics;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    public class HistoryRequestBenchmark : QCAlgorithm
    {
        private Symbol _symbol;
        private Stopwatch _timer;
        public override void Initialize()
        {
            SetStartDate(2010, 10, 07);
            SetEndDate(2013, 10, 10);
            SetCash(10000);
            _symbol = AddEquity("SPY", Resolution.Minute).Symbol;
            _timer = Stopwatch.StartNew();
        }

        public void OnData(Slice slice)
        {
            long start = _timer.ElapsedTicks;

            var minuteHistory = History(_symbol, 2, Resolution.Minute);
            var lastHourHigh = minuteHistory.Select(minuteBar => minuteBar.High).DefaultIfEmpty(0).Max();
            long minElapse = _timer.ElapsedTicks;
            Debug($"Minute: #{minuteHistory.Count()} in {minElapse - start} ticks");

            var dailyHistory = History(_symbol, 20, Resolution.Daily);
            long dayElapse = _timer.ElapsedTicks;
            Debug($"Daily: #{dailyHistory.Count()} in {dayElapse - minElapse} ticks");
        }
    }
}