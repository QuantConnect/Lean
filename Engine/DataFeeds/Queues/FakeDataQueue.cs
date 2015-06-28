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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.DataFeeds.Queues
{
    /// <summary>
    /// This is an implementation of <see cref="IDataQueueHandler"/> used for testing
    /// </summary>
    public class FakeDataQueue : IDataQueueHandler
    {
        private readonly Random _random = new Random();

        private readonly Timer _timer;
        private readonly ConcurrentQueue<BaseData> _ticks; 
        private readonly Dictionary<SecurityType, List<string>> _symbols;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeDataQueue"/> class to randomly emit data for each symbol
        /// </summary>
        public FakeDataQueue()
        {
            _ticks = new ConcurrentQueue<BaseData>();
            _symbols = new Dictionary<SecurityType, List<string>>();
            _timer = new Timer
            {
                AutoReset = true,
                Enabled = true
            };
            _timer.Elapsed += (sender, args) =>
            {
                _timer.Interval = _random.Next(15, 2500); // around each second
                foreach (var symbol in _symbols.SelectMany(x => x.Value))
                {
                    // 50/50 repeating chance of emitting each symbol
                    while (_random.NextDouble() > 0.75)
                    {
                        _ticks.Enqueue(new Tick
                        {
                            Time = DateTime.Now,
                            Symbol = symbol,
                            Value = 10 + (decimal) Math.Abs(Math.Sin(DateTime.Now.TimeOfDay.TotalMinutes)),
                            TickType = TickType.Trade,
                            Quantity = _random.Next(10, (int) _timer.Interval)
                        });
                    }
                }
            };
        }

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            BaseData tick;
            var timeout = DateTime.UtcNow + Time.OneMillisecond;
            while (_ticks.TryDequeue(out tick) && DateTime.UtcNow < timeout)
            {
                yield return tick;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IDictionary<SecurityType, List<string>> symbols)
        {
            foreach (var securityType in symbols)
            {
                List<string> securities;
                if (!_symbols.TryGetValue(securityType.Key, out securities))
                {
                    securities = new List<string>();
                    _symbols[securityType.Key] = securities;
                }
                securities.AddRange(securityType.Value);
            }
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IDictionary<SecurityType, List<string>> symbols)
        {
            foreach (var securityType in symbols)
            {
                List<string> securities;
                if (_symbols.TryGetValue(securityType.Key, out securities))
                {
                    securities.RemoveAll(x => securityType.Value.Contains(x));
                }
            }
        }
    }
}
