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

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QuantConnect.Util
{
    /// <summary>
    /// Helper class to keep track of wall time, an efficient stop watch implementation
    /// </summary>
    public class PerformanceTimer
    {
        private static readonly double _frequency = Stopwatch.Frequency;
        private long _start;
        private long _currentTicks;
        private double _totalSeconds;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start()
        {
            _start = Stopwatch.GetTimestamp();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            _currentTicks += Stopwatch.GetTimestamp() - _start;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal GetAndReset()
        {
            var currentSeconds = _currentTicks / _frequency;
            _currentTicks = 0;
            _totalSeconds += currentSeconds;
            return (decimal)Math.Round(currentSeconds, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal GetTotalTime()
        {
            return (decimal)Math.Round(_totalSeconds, 1);
        }
    }
}
