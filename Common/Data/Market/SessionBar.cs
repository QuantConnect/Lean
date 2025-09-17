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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Contains OHLCV data for a single session
    /// </summary>
    public class SessionBar : TradeBar
    {
        private DateTime _lastVolumeTime = DateTime.MinValue;
        private bool _initialized;
        private DateTime _currentTime;
        private QuoteBar _bar;
        private readonly TickType _sourceTickType;
        internal QuoteBar BarInstance => _bar;

        /// <summary>
        /// Open Interest:
        /// </summary>
        public decimal OpenInterest { get; set; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public override decimal Open => _bar?.Open ?? 0m;

        /// <summary>
        /// High price of the TradeBar during the time period.
        /// </summary>
        public override decimal High => _bar?.High ?? 0m;

        /// <summary>
        /// Low price of the TradeBar during the time period.
        /// </summary>
        public override decimal Low => _bar?.Low ?? 0m;

        /// <summary>
        /// Closing price of the TradeBar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public override decimal Close => _bar?.Close ?? 0m;

        /// <summary>
        /// The period of this session bar
        /// </summary>
        public override TimeSpan Period { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Initializes a new instance of SessionBar with default values
        /// </summary>
        public SessionBar() { }

        /// <summary>
        /// Initializes a new instance of SessionBar with a specific tick type
        /// </summary>
        public SessionBar(TickType sourceTickType)
        {
            _sourceTickType = sourceTickType;
        }

        public void Update(BaseData data, SessionBar consolidated)
        {
            if (data.Time < _currentTime)
            {
                // This will prevent overlapping
                return;
            }

            InitializeBar(data, consolidated);
            if (data.DataType == MarketDataType.TradeBar && data is TradeBar tradeBar)
            {
                if (_lastVolumeTime <= tradeBar.Time)
                {
                    _lastVolumeTime = tradeBar.EndTime;
                    Volume += tradeBar.Volume;
                }

                if (_sourceTickType == TickType.Trade)
                {
                    if (!_initialized)
                    {
                        _initialized = true;
                        _bar.Bid.Open = tradeBar.Open;
                    }
                    _bar.Bid.Close = tradeBar.Close;
                    if (tradeBar.Low < _bar.Bid.Low) _bar.Bid.Low = tradeBar.Low;
                    if (tradeBar.High > _bar.Bid.High) _bar.Bid.High = tradeBar.High;

                    _currentTime = tradeBar.EndTime;
                }
            }
            else if (_sourceTickType == TickType.Quote && data.DataType == MarketDataType.QuoteBar)
            {
                var quoteBar = data as QuoteBar;
                var bid = quoteBar.Bid;
                var ask = quoteBar.Ask;

                // update the bid and ask
                if (bid != null)
                {
                    _bar.LastBidSize = quoteBar.LastBidSize;
                    if (_bar.Bid == null)
                    {
                        _bar.Bid = new Bar(bid.Open, bid.High, bid.Low, bid.Close);
                    }
                    else
                    {
                        _bar.Bid.Close = bid.Close;
                        if (_bar.Bid.High < bid.High) _bar.Bid.High = bid.High;
                        if (_bar.Bid.Low > bid.Low) _bar.Bid.Low = bid.Low;
                    }
                }
                if (ask != null)
                {
                    _bar.LastAskSize = quoteBar.LastAskSize;
                    if (_bar.Ask == null)
                    {
                        _bar.Ask = new Bar(ask.Open, ask.High, ask.Low, ask.Close);
                    }
                    else
                    {
                        _bar.Ask.Close = ask.Close;
                        if (_bar.Ask.High < ask.High) _bar.Ask.High = ask.High;
                        if (_bar.Ask.Low > ask.Low) _bar.Ask.Low = ask.Low;
                    }
                }

                _bar.Value = data.Value;
            }
            else if (data.DataType == MarketDataType.Tick)
            {
                var tick = data as Tick;
                if (_lastVolumeTime <= data.Time)
                {
                    _lastVolumeTime = data.EndTime;
                    Volume += tick.Quantity;
                }

                // update the bid and ask
                if (_sourceTickType == tick.TickType)
                {
                    _bar.Update(decimal.Zero, tick.BidPrice, tick.AskPrice, decimal.Zero, tick.BidSize, tick.AskSize);
                    _currentTime = data.EndTime;
                }
            }
        }

        private void InitializeBar(BaseData data, SessionBar consolidated)
        {
            if (_bar == null)
            {
                _bar = new QuoteBar(data.Time.Date, data.Symbol, null, 0, null, 0, Period);
                if (_sourceTickType == TickType.Trade)
                {
                    _bar.Bid = new Bar(0, 0, decimal.MaxValue, 0);
                }
                else if (consolidated != null)
                {
                    var previousBar = consolidated.BarInstance;
                    _bar.Update(decimal.Zero, previousBar?.Bid?.Close ?? decimal.Zero, previousBar?.Ask?.Close ?? decimal.Zero, decimal.Zero, previousBar?.LastBidSize ?? 0, previousBar?.LastAskSize ?? 0);
                }
            }
        }

        /// <summary>
        /// Returns a string representation of the session bar with OHLCV and OpenInterest values formatted.
        /// Example: "O: 101.00 H: 112.00 L: 95.00 C: 110.00 V: 1005.00 OI: 12"
        /// </summary>
        public override string ToString()
        {
            return $"O: {Open.SmartRounding()} " +
                   $"H: {High.SmartRounding()} " +
                   $"L: {Low.SmartRounding()} " +
                   $"C: {Close.SmartRounding()} " +
                   $"V: {Volume.SmartRounding()} " +
                   $"OI: {OpenInterest}";
        }
    }
}
