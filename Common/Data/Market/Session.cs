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
        private readonly Dictionary<(Type DataType, TickType? TickType), MarketHourAwareConsolidator> _consolidators;
        private readonly List<TickType> _supportedTickTypes;
        private IAlgorithmSettings _algorithmSettings;

        public RollingWindow<SessionBar> TradeBarWindow { get; private set; }
        public RollingWindow<SessionBar> QuoteBarWindow { get; private set; }

        /// <summary>
        /// True if we have at least one trading day data
        /// </summary>
        public bool IsTradingDayDataReady => TradeBarWindow.Count > 0 || QuoteBarWindow.Count > 0;

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
            _supportedTickTypes = tickTypes.ToList();

            _consolidators = new Dictionary<(Type, TickType?), MarketHourAwareConsolidator>
            {
                [(typeof(Tick), TickType.Trade)] = null,
                [(typeof(Tick), TickType.Quote)] = null,
                [(typeof(TradeBar), null)] = null,
                [(typeof(QuoteBar), null)] = null
            };

            TradeBarWindow = new RollingWindow<SessionBar>(2);
            QuoteBarWindow = new RollingWindow<SessionBar>(2);
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
            switch (data)
            {
                case Tick tick:
                    if (!_supportedTickTypes.Contains(tick.TickType))
                    {
                        throw new ArgumentException($"Unsupported tick type: {tick.TickType}");
                    }
                    UpdateConsolidator(data, (typeof(Tick), tick.TickType));
                    break;

                case TradeBar _:
                    UpdateConsolidator(data, (typeof(TradeBar), null));
                    break;

                case QuoteBar _:
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
                    true);

                _consolidators[key].DataConsolidated += (sender, consolidated) =>
                {
                    switch (consolidated)
                    {
                        case TradeBar tradeBar:
                            TradeBarWindow.Add(new SessionBar(
                                tradeBar.Time,
                                tradeBar.Open,
                                tradeBar.High,
                                tradeBar.Low,
                                tradeBar.Close,
                                tradeBar.Volume));
                            break;

                        case QuoteBar quoteBar:
                            QuoteBarWindow.Add(new SessionBar(
                                quoteBar.Time,
                                quoteBar.Open,
                                quoteBar.High,
                                quoteBar.Low,
                                quoteBar.Close,
                                0));
                            break;
                    }
                };
            }

            _consolidators[key].Update(data);
        }

        private decimal GetSessionValue(Func<SessionBar, decimal> selector)
        {
            if (QuoteBarWindow.Count > 0)
            {
                return selector(QuoteBarWindow[0]);
            }

            if (TradeBarWindow.Count > 0)
            {
                return selector(TradeBarWindow[0]);
            }

            // If ther is no data, return 0
            return 0m;
        }

        /// <summary>
        /// Resets the session
        /// </summary>
        public void Reset()
        {
            TradeBarWindow.Reset();
            QuoteBarWindow.Reset();
            _consolidators.Clear();
            _supportedTickTypes.Clear();
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
        }
    }
}
