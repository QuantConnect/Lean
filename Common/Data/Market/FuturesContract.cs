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

using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Defines a single futures contract at a specific expiration
    /// </summary>
    public class FuturesContract : BaseContract
    {
        private FutureUniverse _universeData;
        private TradeBar _tradeBar;
        private QuoteBar _quoteBar;
        private Tick _tradeTick;
        private Tick _quoteTick;
        private Tick _openInterest;

        /// <summary>
        /// Gets the open interest
        /// </summary>
        public override decimal OpenInterest
        {
            get
            {
                // Contract universe data is prioritized
                if (_universeData != null)
                {
                    return _universeData.OpenInterest;
                }
                return _openInterest?.Value ?? decimal.Zero;
            }
        }

        /// <summary>
        /// Gets the last price this contract traded at
        /// </summary>
        public override decimal LastPrice
        {
            get
            {
                if (_universeData != null)
                {
                    return _universeData.Close;
                }

                if (_tradeBar == null && _tradeTick == null)
                {
                    return decimal.Zero;
                }
                if (_tradeBar != null)
                {
                    return _tradeTick != null && _tradeTick.EndTime > _tradeBar.EndTime ? _tradeTick.Price : _tradeBar.Close;
                }
                return _tradeTick.Price;
            }
        }

        /// <summary>
        /// Gets the last volume this contract traded at
        /// </summary>
        public override long Volume
        {
            get
            {
                if (_universeData != null)
                {
                    return (long)_universeData.Volume;
                }
                return (long)(_tradeBar?.Volume ?? 0);
            }
        }

        /// <summary>
        /// Get the current bid price
        /// </summary>
        public override decimal BidPrice
        {
            get
            {
                if (_universeData != null)
                {
                    return _universeData.Close;
                }
                if (_quoteBar == null && _quoteTick == null)
                {
                    return decimal.Zero;
                }
                if (_quoteBar != null)
                {
                    return _quoteTick != null && _quoteTick.EndTime > _quoteBar.EndTime ? _quoteTick.BidPrice : _quoteBar.Bid.Close;
                }
                return _quoteTick.BidPrice;
            }
        }

        /// <summary>
        /// Get the current bid size
        /// </summary>
        public override long BidSize
        {
            get
            {
                if (_quoteBar == null && _quoteTick == null)
                {
                    return 0;
                }
                if (_quoteBar != null)
                {
                    return (long)(_quoteTick != null && _quoteTick.EndTime > _quoteBar.EndTime ? _quoteTick.BidSize : _quoteBar.LastBidSize);
                }
                return (long)_quoteTick.BidSize;
            }
        }

        /// <summary>
        /// Gets the current ask price
        /// </summary>
        public override decimal AskPrice
        {
            get
            {
                if (_universeData != null)
                {
                    return _universeData.Close;
                }
                if (_quoteBar == null && _quoteTick == null)
                {
                    return decimal.Zero;
                }
                if (_quoteBar != null)
                {
                    return _quoteTick != null && _quoteTick.EndTime > _quoteBar.EndTime ? _quoteTick.AskPrice : _quoteBar.Ask.Close;
                }
                return _quoteTick.AskPrice;
            }
        }

        /// <summary>
        /// Get the current ask size
        /// </summary>
        public override long AskSize
        {
            get
            {
                if (_quoteBar == null && _quoteTick == null)
                {
                    return 0;
                }
                if (_quoteBar != null)
                {
                    return (long)(_quoteTick != null && _quoteTick.EndTime > _quoteBar.EndTime ? _quoteTick.AskSize : _quoteBar.LastAskSize);
                }
                return (long)_quoteTick.AskSize;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesContract"/> class
        /// </summary>
        /// <param name="symbol">The futures contract symbol</param>
        public FuturesContract(Symbol symbol)
            : base(symbol)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesContract"/> class
        /// </summary>
        /// <param name="contractData">The contract universe data</param>
        public FuturesContract(FutureUniverse contractData)
            : base(contractData.Symbol)
        {
            _universeData = contractData;
        }

        /// <summary>
        /// Implicit conversion into <see cref="Symbol"/>
        /// </summary>
        /// <param name="contract">The option contract to be converted</param>
        public static implicit operator Symbol(FuturesContract contract)
        {
            return contract.Symbol;
        }

        /// <summary>
        /// Updates the future contract with the new data, which can be a <see cref="Tick"/> or <see cref="TradeBar"/> or <see cref="QuoteBar"/>
        /// </summary>
        internal override void Update(BaseData data)
        {
            switch (data)
            {
                case TradeBar tradeBar:
                    _tradeBar = tradeBar;
                    break;

                case QuoteBar quoteBar:
                    _quoteBar = quoteBar;
                    break;

                case Tick tick when tick.TickType == TickType.Trade:
                    _tradeTick = tick;
                    break;

                case Tick tick when tick.TickType == TickType.Quote:
                    _quoteTick = tick;
                    break;

                case Tick tick when tick.TickType == TickType.OpenInterest:
                    _openInterest = tick;
                    break;
            }
        }
    }
}
