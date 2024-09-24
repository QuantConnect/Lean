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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Defines a single option contract at a specific expiration and strike price
    /// </summary>
    public class OptionContract : ISymbolProvider
    {
        private static readonly SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

        private Lazy<OptionPriceModelResult> _optionPriceModelResult = new(() => OptionPriceModelResult.None);

        private readonly List<BaseData> _data = new();

        /// <summary>
        /// Gets the option contract's symbol
        /// </summary>
        public Symbol Symbol
        {
            get; set;
        }

        /// <summary>
        /// Gets the underlying security's symbol
        /// </summary>
        public Symbol UnderlyingSymbol
        {
            get; private set;
        }

        /// <summary>
        /// Gets the strike price
        /// </summary>
        public decimal Strike => Symbol.ID.StrikePrice;

        /// <summary>
        /// Gets the strike price multiplied by the strike multiplier
        /// </summary>
        public decimal ScaledStrike
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the expiration date
        /// </summary>
        public DateTime Expiry => Symbol.ID.Date;

        /// <summary>
        /// Gets the right being purchased (call [right to buy] or put [right to sell])
        /// </summary>
        public OptionRight Right => Symbol.ID.OptionRight;

        /// <summary>
        /// Gets the option style
        /// </summary>
        public OptionStyle Style => Symbol.ID.OptionStyle;

        /// <summary>
        /// Gets the theoretical price of this option contract as computed by the <see cref="IOptionPriceModel"/>
        /// </summary>
        public decimal TheoreticalPrice => _optionPriceModelResult.Value.TheoreticalPrice;

        /// <summary>
        /// Gets the implied volatility of the option contract as computed by the <see cref="IOptionPriceModel"/>
        /// </summary>
        public decimal ImpliedVolatility => _optionPriceModelResult.Value.ImpliedVolatility;

        /// <summary>
        /// Gets the greeks for this contract
        /// </summary>
        public Greeks Greeks => _optionPriceModelResult.Value.Greeks;

        /// <summary>
        /// Gets the local date time this contract's data was last updated
        /// </summary>
        public DateTime Time
        {
            get; set;
        }

        /// <summary>
        /// Gets the open interest
        /// </summary>
        public decimal OpenInterest => GetLastOpenInterest()?.Value ?? decimal.Zero;

        /// <summary>
        /// Gets the last price this contract traded at
        /// </summary>
        public decimal LastPrice => GetLastTrades().LastOrDefault() switch
        {
            Tick tick => tick.LastPrice,
            TradeBar tradeBar => tradeBar.Close,
            _ => decimal.Zero
        };

        /// <summary>
        /// Gets the last volume this contract traded at
        /// </summary>
        public long Volume => (long)(GetLastTradeBar()?.Volume ?? 0L);

        /// <summary>
        /// Gets the current bid price
        /// </summary>
        public decimal BidPrice
        {
            get
            {
                foreach (var data in GetLastQuotes())
                {
                    if (data is Tick tick && tick.BidPrice != 0)
                    {
                        return tick.BidPrice;
                    }

                    if (data is QuoteBar quoteBar && quoteBar.Bid != null && quoteBar.Bid.Close != 0)
                    {
                        return quoteBar.Bid.Close;
                    }
                }

                return decimal.Zero;
            }
        }

        /// <summary>
        /// Get the current bid size
        /// </summary>
        public long BidSize
        {
            get
            {
                foreach (var data in GetLastQuotes())
                {
                    if (data is Tick tick && tick.BidPrice != 0)
                    {
                        return (long)tick.BidSize;
                    }

                    if (data is QuoteBar quoteBar && quoteBar.Bid != null && quoteBar.Bid.Close != 0)
                    {
                        return (long)quoteBar.LastBidSize;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the ask price
        /// </summary>
        public decimal AskPrice
        {
            get
            {
                foreach (var data in GetLastQuotes())
                {
                    if (data is Tick tick && tick.AskPrice != 0)
                    {
                        return tick.AskPrice;
                    }

                    if (data is QuoteBar quoteBar && quoteBar.Ask != null && quoteBar.Ask.Close != 0)
                    {
                        return quoteBar.Ask.Close;
                    }
                }

                return decimal.Zero;
            }
        }

        /// <summary>
        /// Gets the current ask size
        /// </summary>
        public long AskSize
        {
            get
            {
                foreach (var data in GetLastQuotes())
                {
                    if (data is Tick tick && tick.AskPrice != 0)
                    {
                        return (long)tick.AskSize;
                    }

                    if (data is QuoteBar quoteBar && quoteBar.Ask != null && quoteBar.Ask.Close != 0)
                    {
                        return (long)quoteBar.LastAskSize;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the last price the underlying security traded at
        /// </summary>
        public decimal UnderlyingLastPrice
        {
            get; set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionContract"/> class
        /// </summary>
        /// <param name="security">The option contract security</param>
        /// <param name="underlyingSymbol">The symbol of the underlying security</param>
        public OptionContract(ISecurityPrice security, Symbol underlyingSymbol)
        {
            Symbol = security.Symbol;
            UnderlyingSymbol = underlyingSymbol;
            ScaledStrike = Strike * security.SymbolProperties.StrikeMultiplier;
        }

        /// <summary>
        /// Initializes a new option contract from a given <see cref="OptionUniverse"/> instance
        /// </summary>
        /// <param name="contractData">The option universe contract data to use as source for this contract</param>
        public OptionContract(OptionUniverse contractData)
        {
            Symbol = contractData.Symbol;
            UnderlyingSymbol = contractData.Symbol.Underlying;

            // TODO: What about the strike multiplier if no security is provided? Should we access the spdb directly?
            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(
                contractData.Symbol.ID.Market,
                contractData.Symbol,
                contractData.Symbol.SecurityType,
                // What should the default be? We don't have access to the account currency here
                Currencies.USD);
            ScaledStrike = Strike * symbolProperties.StrikeMultiplier;
        }

        /// <summary>
        /// Sets the option price model evaluator function to be used for this contract
        /// </summary>
        /// <param name="optionPriceModelEvaluator">Function delegate used to evaluate the option price model</param>
        internal void SetOptionPriceModel(Func<OptionPriceModelResult> optionPriceModelEvaluator)
        {
            _optionPriceModelResult = new Lazy<OptionPriceModelResult>(optionPriceModelEvaluator);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString() => Symbol.Value;

        /// <summary>
        /// Creates a <see cref="OptionContract"/>
        /// </summary>
        /// <param name="baseData"></param>
        /// <param name="security">provides price properties for a <see cref="Security"/></param>
        /// <param name="underlyingLastPrice">last price the underlying security traded at</param>
        /// <returns>Option contract</returns>
        public static OptionContract Create(BaseData baseData, ISecurityPrice security, decimal underlyingLastPrice)
            => Create(baseData.Symbol.Underlying, baseData.EndTime, security, underlyingLastPrice);

        /// <summary>
        /// Creates a <see cref="OptionContract"/>
        /// </summary>
        /// <param name="underlyingSymbol">The symbol of the underlying security</param>
        /// <param name="endTime">local date time this contract's data was last updated</param>
        /// <param name="security">provides price properties for a <see cref="Security"/></param>
        /// <param name="underlyingLastPrice">last price the underlying security traded at</param>
        /// <returns>Option contract</returns>
        public static OptionContract Create(Symbol underlyingSymbol, DateTime endTime, ISecurityPrice security, decimal underlyingLastPrice)
        {
            return new OptionContract(security, underlyingSymbol)
            {
                Time = endTime,
                UnderlyingLastPrice = underlyingLastPrice
            };
        }

        /// <summary>
        /// Creates a new option contract from a given <see cref="OptionUniverse"/> instance,
        /// using its data to form a quote bar to source pricing data
        /// </summary>
        /// <param name="contractData">The option universe contract data to use as source for this contract</param>
        public static OptionContract Create(OptionUniverse contractData)
        {
            var contract = new OptionContract(contractData)
            {
                Time = contractData.EndTime,
            };

            var bar = new Bar(contractData.Open, contractData.High, contractData.Low, contractData.Close);
            var quoteBar = new QuoteBar(contractData.Time, contractData.Symbol, bar, 0, bar, 0)
            {
                EndTime = contractData.EndTime,
            };

            contract.Update(quoteBar);

            return contract;
        }

        /// <summary>
        /// Updates the option contract with the new data, which can be a <see cref="Tick"/> or <see cref="TradeBar"/> or <see cref="QuoteBar"/>
        /// </summary>
        internal void Update(BaseData data)
        {
            _data.Add(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<BaseData> GetLastTrades()
        {
            return _data.Where(x => x is TradeBar || (x is Tick tick && tick.TickType == TickType.Trade));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TradeBar GetLastTradeBar()
        {
            return _data.LastOrDefault(x => x is TradeBar) as TradeBar;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<BaseData> GetLastQuotes()
        {
            return _data.Where(x => x is QuoteBar || (x is Tick tick && tick.TickType == TickType.Quote));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BaseData GetLastOpenInterest()
        {
            return _data.LastOrDefault(x => x is Tick tick && tick.TickType == TickType.OpenInterest);
        }
    }
}
