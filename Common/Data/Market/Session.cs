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
using Common.Securities;
using QuantConnect.Statistics;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents a daily trading session with OHLCV data and rolling window functionality.
    /// </summary>
    public class Session : IBar
    {
        private readonly RollingWindow<TradeBar> _window;
        private readonly MarketHourAwareConsolidator _consolidator;

        /// <summary>
        /// Rolling window of session bars (default size: 2)
        /// </summary>
        public RollingWindow<TradeBar> Window => _window;

        /// <summary>
        /// True if we have at least one trading day data
        /// </summary>
        public bool IsTradingDayDataReady => _window.Count > 0;

        /// <summary>
        /// Opening price of the session
        /// </summary>
        public decimal Open
        {
            get { return _window[0].Open; }
        }

        /// <summary>
        /// High price of the session
        /// </summary>
        public decimal High
        {
            get { return _window[0].High; }
        }

        /// <summary>
        /// Low price of the session
        /// </summary>
        public decimal Low
        {
            get { return _window[0].Low; }
        }

        /// <summary>
        /// Closing price of the session
        /// </summary>
        public decimal Close
        {
            get { return _window[0].Close; }
        }

        /// <summary>
        /// Volume traded during the session
        /// </summary>
        public decimal Volume
        {
            get { return _window[0].Volume; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class
        /// </summary>
        /// <param name="sessionConfig">The session configuration</param>
        public Session(SecurityCacheSessionConfig sessionConfig = null)
        {
            if (sessionConfig != null)
            {
                _consolidator = new MarketHourAwareConsolidator(sessionConfig.DailyStrictEndTimeEnabled, Resolution.Daily, sessionConfig.DataType, sessionConfig.TickType, sessionConfig.ExtendedMarketHours);
                _consolidator.DataConsolidated += (sender, bar) =>
                {
                    // Reuse the TradeBar constructor to populate the session bar
                    // Only expose Open, High, Low, Close, Volume
                    if (bar is TradeBar tradeBar)
                    {
                        var sessionBar = new TradeBar(tradeBar.Time, tradeBar.Symbol, tradeBar.Open, tradeBar.High, tradeBar.Low, tradeBar.Close, tradeBar.Volume);
                        _window.Add(sessionBar);
                    }
                    else if (bar is QuoteBar quoteBar)
                    {
                        var sessionBar = new TradeBar(quoteBar.Time, quoteBar.Symbol, quoteBar.Open, quoteBar.High, quoteBar.Low, quoteBar.Close, 0);
                        _window.Add(sessionBar);
                    }
                };
            }

            _window = new RollingWindow<TradeBar>(2);
        }

        /// <summary>
        /// Updates the session with new price data
        /// </summary>
        public void Update(BaseData data)
        {
            if (_consolidator != null)
            {
                _consolidator.Update(data);
            }
        }
    }
}
