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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using QuantConnect.Data.Market;

namespace QuantConnect.Queues.Test
{
    /// <summary>
    /// Provides a fake queue handler to provider live data to run tests locally
    /// </summary>
    public class FakeLiveQueueHandler : Queue
    {
        private readonly Random _random = new Random();
        private const int _tickCount = 1000;
        private const int _createDataGapsEvery = 2;
        private readonly TimeSpan _maxDataGap = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan _emitEvery = TimeSpan.FromSeconds(1.0);

        private static readonly DateTime _start = DateTime.Now;
        private readonly HashSet<Subscription> _subscriptions = new HashSet<Subscription>();

        private readonly Stopwatch _lastEmit = Stopwatch.StartNew();

        /// <inheritdoc />
        public override IEnumerable<Tick> GetNextTicks()
        {
            // instead of sleeping, model real world where we just don't have data to pull from the queue
            if (_lastEmit.ElapsedTicks < _emitEvery.Ticks * _random.NextDouble())
            {
                yield break;
            }

            // reset our last emit to now
            _lastEmit.Restart();

            // spread them out
            for (int i = 0; i < _tickCount; i++)
            {
                foreach (var subscription in _subscriptions)
                {
                    // model delays in dequeuing
                    if (_random.NextDouble() < 0.0001)
                    {
                        Thread.Sleep(10);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }

                    var time = DateTime.Now.Subtract(TimeSpan.FromMilliseconds(4200 * _random.NextDouble()));
                    var sine = ComputeNextSineValue(_start, time, TimeSpan.FromMinutes(1));
                    yield return new Tick(time, subscription.Symbol, sine * 1.025m, sine * .975m);
                }
            }
        }

        /// <inheritdoc />
        public override void Subscribe(IDictionary<SecurityType, List<string>> symbols)
        {
            foreach (var type in symbols)
            {
                foreach (var symbol in type.Value)
                {
                    TimeSpan gap = TimeSpan.Zero;
                    if (_subscriptions.Count%_createDataGapsEvery == 0)
                    {
                        double value = _random.NextDouble();
                        double milliseconds = _maxDataGap.TotalMilliseconds*value;
                        gap = TimeSpan.FromMilliseconds(milliseconds);
                    }
                    Console.WriteLine("SYMBOL: " + symbol + " GAP: " + gap.TotalSeconds.ToString("0.00"));
                    _subscriptions.Add(new Subscription(symbol, gap));
                }
            }
        }

        /// <inheritdoc />
        public override void Unsubscribe(IDictionary<SecurityType, List<string>> symbols)
        {
            foreach (var type in symbols)
            {
                foreach (var symbol in type.Value)
                {
                    _subscriptions.Remove(new Subscription(symbol, TimeSpan.Zero));
                }
            }
        }

        /// <summary>
        /// Calculate the next fake value for our fake data:
        /// </summary>
        /// <param name="start">Start of the fake data period</param>
        /// <param name="current">Current time for the fake data period</param>
        /// <param name="period">Period we want the sine to run over</param>
        /// <returns></returns>
        private decimal ComputeNextSineValue(DateTime start, DateTime current, TimeSpan period)
        {
            var percentage = ((current - start).TotalHours/period.TotalHours);

            return ((decimal) Math.Sin(percentage)*100) + 1000;
        }

        private class Subscription
        {
            public readonly TimeSpan DataInterval;
            public readonly string Symbol;

            public Subscription(string symbol, TimeSpan dataInterval)
            {
                Symbol = symbol.ToUpper();
                DataInterval = dataInterval;
            }

            public override int GetHashCode()
            {
                return Symbol.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }
                return ((Subscription) obj).Symbol.ToUpper() == Symbol;
            }
        }
    }
}
