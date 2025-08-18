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
using QuantConnect.Securities;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents a daily trading session with OHLCV data and a list of rolling windows for different subscriptions.
    /// </summary>
    public class Session : RollingWindow<SessionBar>
    {
        private readonly Dictionary<(Type DataType, TickType? TickType), MarketHourAwareConsolidator> _consolidators;
        private readonly List<TickType> _supportedTickTypes;
        private IAlgorithmSettings _algorithmSettings;
        private SecurityExchangeHours _exchangeHours;

        /// <summary>
        /// True if we have at least one trading day data
        /// </summary>
        public bool IsTradingDayDataReady => Count > 0;

        /// <summary>
        /// Opening price of the session
        /// </summary>
        public decimal Open => GetValue(x => x.Open);

        /// <summary>
        /// High price of the session
        /// </summary>
        public decimal High => GetValue(x => x.High);

        /// <summary>
        /// Low price of the session
        /// </summary>
        public decimal Low => GetValue(x => x.Low);

        /// <summary>
        /// Closing price of the session
        /// </summary>
        public decimal Close => GetValue(x => x.Close);

        /// <summary>
        /// Volume traded during the session
        /// </summary>
        public decimal Volume => GetValue(x => x.Volume);

        /// <summary>
        /// Gets the time of the bar
        /// </summary>
        public DateTime Time => this[0].Time;

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class
        /// </summary>
        /// <param name="tickTypes">The tick types to use</param>
        /// <param name="algorithmSettings">The algorithm settings</param>
        public Session(IEnumerable<TickType> tickTypes, IAlgorithmSettings algorithmSettings = null) : base(2)
        {
            _algorithmSettings = algorithmSettings ?? new AlgorithmSettings();
            _supportedTickTypes = tickTypes.ToList();

            _consolidators = new Dictionary<(Type, TickType?), MarketHourAwareConsolidator>
            {
                [(typeof(Tick), TickType.Trade)] = null,
                [(typeof(Tick), TickType.Quote)] = null,
                [(typeof(TradeBar), null)] = null,
                [(typeof(QuoteBar), null)] = null
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
            if (_exchangeHours == null)
            {
                // Get the exchange hours for the symbol
                var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
                _exchangeHours = marketHoursDatabase.GetExchangeHours(data.Symbol.ID.Market, data.Symbol, data.Symbol.SecurityType);
            }
            switch (data)
            {
                case Tick tick:
                    if (!_supportedTickTypes.Contains(tick.TickType))
                    {
                        throw new ArgumentException($"Unsupported tick type: {tick.TickType}");
                    }
                    UpdateConsolidator(data, (typeof(Tick), tick.TickType));
                    break;

                case TradeBar tradeBar:
                    if (tradeBar.Period == TimeSpan.FromDays(1))
                    {
                        // When the period is one day, we add it directly to the session
                        Add(new SessionBar(tradeBar.Time, tradeBar.Open, tradeBar.High, tradeBar.Low, tradeBar.Close, tradeBar.Volume));
                        return;
                    }
                    UpdateConsolidator(data, (typeof(TradeBar), null));
                    break;

                case QuoteBar quoteBar:
                    if (quoteBar.Period == TimeSpan.FromDays(1))
                    {
                        // When the period is one day, we add it directly to the session
                        Add(new SessionBar(quoteBar.Time, quoteBar.Open, quoteBar.High, quoteBar.Low, quoteBar.Close, 0));
                        return;
                    }
                    UpdateConsolidator(data, (typeof(QuoteBar), null));
                    break;
            }
        }

        private void UpdateConsolidator(BaseData data, (Type DataType, TickType? TickType) key)
        {
            if (_consolidators[key] == null)
            {
                _consolidators[key] = new MarketHourAwareConsolidator(
                    _algorithmSettings.DailyPreciseEndTime,
                    Resolution.Daily,
                    key.DataType,
                    key.TickType ?? TickType.Trade,
                    false);

                _consolidators[key].DataConsolidated += OnConsolidated;
            }

            if (_exchangeHours != null && _exchangeHours.IsOpen(data.Time, false))
            {
                // Only update in regular trading hours
                _consolidators[key].Update(data);
            }
        }

        private void OnConsolidated(object sender, IBaseData consolidated)
        {
            SessionBar sessionBar = consolidated switch
            {
                TradeBar t => new SessionBar(t.Time, t.Open, t.High, t.Low, t.Close, t.Volume),
                QuoteBar q => new SessionBar(q.Time, q.Open, q.High, q.Low, q.Close, 0),
                _ => null
            };

            if (sessionBar != null)
            {
                Add(sessionBar);
            }
        }

        private decimal GetValue(Func<SessionBar, decimal> selector)
        {
            if (Count > 0)
            {
                return selector(this[0]);
            }
            return 0;
        }

        /// <summary>
        /// Resets the session
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            var keys = _consolidators.Keys.ToList();
            foreach (var key in keys)
            {
                _consolidators[key] = null;
            }
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
    }
}
