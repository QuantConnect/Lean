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
using QuantConnect.Indicators;
using QuantConnect.Data.Common;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents a daily trading session with OHLCV data and a list of rolling windows for different subscriptions.
    /// </summary>
    public class Session : IBar
    {
        private readonly List<RollingWindow<SessionBar>> _sessionWindows;
        private readonly List<MarketHourAwareConsolidator> _consolidators;
        private readonly List<TickType> _tickTypes;
        private IAlgorithmSettings _algorithmSettings;
        public List<RollingWindow<SessionBar>> SessionWindows => _sessionWindows;

        /// <summary>
        /// True if we have at least one trading day data
        /// </summary>
        public bool IsTradingDayDataReady => SessionWindows.Any(x => x.Count > 0);

        /// <summary>
        /// Opening price of the session
        /// </summary>
        public decimal Open => GetSessionValue(sb => sb.Open);

        /// <summary>
        /// High price of the session
        /// </summary>
        public decimal High => GetSessionValue(sb => sb.High);

        /// <summary>
        /// Low price of the session
        /// </summary>
        public decimal Low => GetSessionValue(sb => sb.Low);

        /// <summary>
        /// Closing price of the session
        /// </summary>
        public decimal Close => GetSessionValue(sb => sb.Close);

        /// <summary>
        /// Volume traded during the session
        /// </summary>
        public decimal Volume => GetSessionValue(sb => sb.Volume);

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class
        /// </summary>
        /// <param name="tickTypes">The tick types to use</param>
        /// <param name="algorithmSettings">The algorithm settings</param>
        public Session(IEnumerable<TickType> tickTypes, IAlgorithmSettings algorithmSettings = null)
        {
            _algorithmSettings = algorithmSettings ?? new AlgorithmSettings();
            _tickTypes = tickTypes.ToList();

            // Index 0 = Tick -> TickType.Trade
            // Index 1 = Tick -> TickType.Quote
            // Index 2 = TradeBar
            // Index 3 = QuoteBar
            _consolidators = new List<MarketHourAwareConsolidator>()
            {
                null,
                null,
                null,
                null
            };
            _sessionWindows = new List<RollingWindow<SessionBar>>()
            {
                new RollingWindow<SessionBar>(2),
                new RollingWindow<SessionBar>(2),
                new RollingWindow<SessionBar>(2),
                new RollingWindow<SessionBar>(2)
            };
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="Session"/> class
        /// </summary>
        /// <param name="tickType">The tick type to use</param>
        /// <param name="algorithmSettings">The algorithm settings</param>
        public Session(TickType tickType, IAlgorithmSettings algorithmSettings = null)
            : this([tickType], algorithmSettings)
        {
        }

        /// <summary>
        /// Updates the session with new price data
        /// </summary>
        public void Update(BaseData data)
        {
            if (data is Tick tick)
            {
                if (!_tickTypes.Contains(tick.TickType))
                {
                    throw new ArgumentException($"Unsupported tick type: {tick.TickType}");
                }

                if (tick.TickType == TickType.Trade)
                {
                    UpdateConsolidator(data, 0, typeof(Tick), tick.TickType);
                }
                else if (tick.TickType == TickType.Quote)
                {
                    UpdateConsolidator(data, 1, typeof(Tick), tick.TickType);
                }
            }
            else if (data is TradeBar)
            {
                UpdateConsolidator(data, 2, typeof(TradeBar));
            }
            else if (data is QuoteBar)
            {
                UpdateConsolidator(data, 3, typeof(QuoteBar));
            }
        }

        private void UpdateConsolidator(BaseData data, int index, Type type, TickType? tickType = null)
        {
            if (_consolidators[index] == null)
            {
                _consolidators[index] = new MarketHourAwareConsolidator(_algorithmSettings.DailyPreciseEndTime, Resolution.Daily, type, tickType ?? TickType.Trade, false);
                _consolidators[index].DataConsolidated += (sender, bar) =>
                {
                    if (bar is TradeBar tradeBar)
                    {
                        var sessionBar = new SessionBar(tradeBar.Time, tradeBar.Open, tradeBar.High, tradeBar.Low, tradeBar.Close, tradeBar.Volume);
                        _sessionWindows[index].Add(sessionBar);
                    }
                    else if (bar is QuoteBar quoteBar)
                    {
                        var sessionBar = new SessionBar(quoteBar.Time, quoteBar.Open, quoteBar.High, quoteBar.Low, quoteBar.Close, 0);
                        _sessionWindows[index].Add(sessionBar);
                    }
                };
            }
            _consolidators[index].Update(data);
        }

        private decimal GetSessionValue(Func<SessionBar, decimal> selector)
        {
            // First try to use QuoteBar (index 3)
            if (_sessionWindows[3].Count > 0)
            {
                return selector(_sessionWindows[3][0]);
            }

            // If there is not QuoteBar, try to use TradeBar (index 2)
            if (_sessionWindows[2].Count > 0)
            {
                return selector(_sessionWindows[2][0]);
            }

            // If there is not TradeBar, try to use Tick with Quote type (index 1)
            if (_sessionWindows[1].Count > 0)
            {
                return selector(_sessionWindows[1][0]);
            }

            // If there is not Tick with Quote type, try to use Tick with Trade type (index 0)
            if (_sessionWindows[0].Count > 0)
            {
                return selector(_sessionWindows[0][0]);
            }

            // If ther is no data, return 0
            return 0m;
        }

        /// <summary>
        /// Resets the session
        /// </summary>
        public void Reset()
        {
            _sessionWindows.Clear();
            _consolidators.Clear();
            _tickTypes.Clear();
            _algorithmSettings = null;
        }
    }

    /// <summary>
    /// Contains OHLCV data for a single session
    /// </summary>
    public class SessionBar : IBar
    {
        /// <summary>
        /// Current time marker.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public decimal Open { get; private set; }

        /// <summary>
        /// High price of the bar during the time period.
        /// </summary>
        public decimal High { get; private set; }

        /// <summary>
        /// Low price of the bar during the time period.
        /// </summary>
        public decimal Low { get; private set; }

        /// <summary>
        /// Closing price of the bar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close { get; private set; }

        /// <summary>
        /// Volume:
        /// </summary>
        public decimal Volume { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionBar"/> class
        /// </summary>
        public SessionBar(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionBar"/> class with default values
        /// </summary>
        public SessionBar()
        {
            Time = DateTime.MinValue;
            Open = High = Low = Close = Volume = 0;
        }
    }
}
