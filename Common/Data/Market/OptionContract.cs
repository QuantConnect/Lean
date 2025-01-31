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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Defines a single option contract at a specific expiration and strike price
    /// </summary>
    public class OptionContract : BaseContract
    {
        private IOptionData _optionData = OptionPriceModelResultData.Null;
        private readonly SymbolProperties _symbolProperties;

        /// <summary>
        /// Gets the strike price
        /// </summary>
        public decimal Strike => Symbol.ID.StrikePrice;

        /// <summary>
        /// Gets the strike price multiplied by the strike multiplier
        /// </summary>
        public decimal ScaledStrike => Strike * _symbolProperties.StrikeMultiplier;

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
        public decimal TheoreticalPrice => _optionData.TheoreticalPrice;

        /// <summary>
        /// Gets the implied volatility of the option contract as computed by the <see cref="IOptionPriceModel"/>
        /// </summary>
        public decimal ImpliedVolatility => _optionData.ImpliedVolatility;

        /// <summary>
        /// Gets the greeks for this contract
        /// </summary>
        public Greeks Greeks => _optionData.Greeks;

        /// <summary>
        /// Gets the open interest
        /// </summary>
        public override decimal OpenInterest => _optionData.OpenInterest;

        /// <summary>
        /// Gets the last price this contract traded at
        /// </summary>
        public override decimal LastPrice => _optionData.LastPrice;

        /// <summary>
        /// Gets the last volume this contract traded at
        /// </summary>
        public override long Volume => _optionData.Volume;

        /// <summary>
        /// Gets the current bid price
        /// </summary>
        public override decimal BidPrice => _optionData.BidPrice;

        /// <summary>
        /// Get the current bid size
        /// </summary>
        public override long BidSize => _optionData.BidSize;

        /// <summary>
        /// Gets the ask price
        /// </summary>
        public override decimal AskPrice => _optionData.AskPrice;

        /// <summary>
        /// Gets the current ask size
        /// </summary>
        public override long AskSize => _optionData.AskSize;

        /// <summary>
        /// Gets the last price the underlying security traded at
        /// </summary>
        public decimal UnderlyingLastPrice => _optionData.UnderlyingLastPrice;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionContract"/> class
        /// </summary>
        /// <param name="security">The option contract security</param>
        public OptionContract(ISecurityPrice security)
            : base(security.Symbol)
        {
            _symbolProperties = security.SymbolProperties;
        }

        /// <summary>
        /// Initializes a new option contract from a given <see cref="OptionUniverse"/> instance
        /// </summary>
        /// <param name="contractData">The option universe contract data to use as source for this contract</param>
        /// <param name="symbolProperties">The contract symbol properties</param>
        public OptionContract(OptionUniverse contractData, SymbolProperties symbolProperties)
            : base(contractData.Symbol)
        {
            _symbolProperties = symbolProperties;
            _optionData = new OptionUniverseData(contractData);
        }

        /// <summary>
        /// Sets the option price model evaluator function to be used for this contract
        /// </summary>
        /// <param name="optionPriceModelEvaluator">Function delegate used to evaluate the option price model</param>
        internal void SetOptionPriceModel(Func<OptionPriceModelResult> optionPriceModelEvaluator)
        {
            _optionData = new OptionPriceModelResultData(optionPriceModelEvaluator, _optionData as OptionPriceModelResultData);
        }

        /// <summary>
        /// Creates a <see cref="OptionContract"/>
        /// </summary>
        /// <param name="baseData"></param>
        /// <param name="security">Provides price properties for a <see cref="Security"/></param>
        /// <param name="underlying">Last underlying security trade data</param>
        /// <returns>Option contract</returns>
        public static OptionContract Create(BaseData baseData, ISecurityPrice security, BaseData underlying)
            => Create(baseData.EndTime, security, underlying);

        /// <summary>
        /// Creates a <see cref="OptionContract"/>
        /// </summary>
        /// <param name="endTime">local date time this contract's data was last updated</param>
        /// <param name="security">provides price properties for a <see cref="Security"/></param>
        /// <param name="underlying">last underlying security trade data</param>
        /// <returns>Option contract</returns>
        public static OptionContract Create(DateTime endTime, ISecurityPrice security, BaseData underlying)
        {
            var contract = new OptionContract(security)
            {
                Time = endTime,
            };
            contract._optionData.SetUnderlying(underlying);

            return contract;
        }

        /// <summary>
        /// Creates a new option contract from a given <see cref="OptionUniverse"/> instance,
        /// using its data to form a quote bar to source pricing data
        /// </summary>
        /// <param name="contractData">The option universe contract data to use as source for this contract</param>
        /// <param name="symbolProperties">The contract symbol properties</param>
        public static OptionContract Create(OptionUniverse contractData, SymbolProperties symbolProperties)
        {
            var contract = new OptionContract(contractData, symbolProperties)
            {
                Time = contractData.EndTime,
            };

            return contract;
        }

        /// <summary>
        /// Implicit conversion into <see cref="Symbol"/>
        /// </summary>
        /// <param name="contract">The option contract to be converted</param>
        public static implicit operator Symbol(OptionContract contract)
        {
            return contract.Symbol;
        }

        /// <summary>
        /// Updates the option contract with the new data, which can be a <see cref="Tick"/> or <see cref="TradeBar"/> or <see cref="QuoteBar"/>
        /// </summary>
        internal override void Update(BaseData data)
        {
            if (data.Symbol.SecurityType.IsOption())
            {
                _optionData.Update(data);
            }
            else if (data.Symbol.SecurityType == Symbol.GetUnderlyingFromOptionType(Symbol.SecurityType))
            {
                _optionData.SetUnderlying(data);
            }
        }

        #region Option Contract Data Handlers

        private interface IOptionData
        {
            decimal LastPrice { get; }
            decimal UnderlyingLastPrice { get; }
            long Volume { get; }
            decimal BidPrice { get; }
            long BidSize { get; }
            decimal AskPrice { get; }
            long AskSize { get; }
            decimal OpenInterest { get; }
            decimal TheoreticalPrice { get; }
            decimal ImpliedVolatility { get; }
            Greeks Greeks { get; }

            void Update(BaseData data);

            void SetUnderlying(BaseData data);
        }

        /// <summary>
        /// Handles option data for a contract from actual price data (trade, quote, open interest) and theoretical price model results
        /// </summary>
        private class OptionPriceModelResultData : IOptionData
        {
            public static readonly OptionPriceModelResultData Null = new(() => OptionPriceModelResult.None);

            private readonly Lazy<OptionPriceModelResult> _optionPriceModelResult;
            private TradeBar _tradeBar;
            private QuoteBar _quoteBar;
            private OpenInterest _openInterest;
            private BaseData _underlying;

            public decimal LastPrice => _tradeBar?.Close ?? decimal.Zero;

            public decimal UnderlyingLastPrice => _underlying?.Price ?? decimal.Zero;

            public long Volume => (long)(_tradeBar?.Volume ?? 0L);

            public decimal BidPrice => _quoteBar?.Bid?.Close ?? decimal.Zero;

            public long BidSize => (long)(_quoteBar?.LastBidSize ?? 0L);

            public decimal AskPrice => _quoteBar?.Ask?.Close ?? decimal.Zero;

            public long AskSize => (long)(_quoteBar?.LastAskSize ?? 0L);

            public decimal OpenInterest => _openInterest?.Value ?? decimal.Zero;

            public decimal TheoreticalPrice => _optionPriceModelResult.Value.TheoreticalPrice;
            public decimal ImpliedVolatility => _optionPriceModelResult.Value.ImpliedVolatility;
            public Greeks Greeks => _optionPriceModelResult.Value.Greeks;

            public OptionPriceModelResultData(Func<OptionPriceModelResult> optionPriceModelEvaluator,
                OptionPriceModelResultData previousOptionData = null)
            {
                _optionPriceModelResult = new(optionPriceModelEvaluator, isThreadSafe: false);

                if (previousOptionData != null)
                {
                    _tradeBar = previousOptionData._tradeBar;
                    _quoteBar = previousOptionData._quoteBar;
                    _openInterest = previousOptionData._openInterest;
                    _underlying = previousOptionData._underlying;
                }
            }

            public void Update(BaseData data)
            {
                switch (data)
                {
                    case TradeBar tradeBar:
                        _tradeBar = tradeBar;
                        break;
                    case QuoteBar quoteBar:
                        _quoteBar = quoteBar;
                        break;
                    case OpenInterest openInterest:
                        _openInterest = openInterest;
                        break;
                }
            }

            public void SetUnderlying(BaseData data)
            {
                _underlying = data;
            }
        }

        /// <summary>
        /// Handles option data for a contract from a <see cref="OptionUniverse"/> instance
        /// </summary>
        private class OptionUniverseData : IOptionData
        {
            private readonly OptionUniverse _contractData;

            public decimal LastPrice => _contractData.Close;

            // TODO: Null check required for FOPs: since OptionUniverse does not support FOPs,
            // these instances will by "synthetic" and will not have underlying data.
            // Can be removed after FOPs are supported by OptionUniverse
            public decimal UnderlyingLastPrice => _contractData?.Underlying?.Price ?? decimal.Zero;

            public long Volume => (long)_contractData.Volume;

            public decimal BidPrice => _contractData.Close;

            public long BidSize => 0;

            public decimal AskPrice => _contractData.Close;

            public long AskSize => 0;

            public decimal OpenInterest => _contractData.OpenInterest;

            public decimal TheoreticalPrice => decimal.Zero;

            public decimal ImpliedVolatility => _contractData.ImpliedVolatility;

            public Greeks Greeks => _contractData.Greeks;

            public OptionUniverseData(OptionUniverse contractData)
            {
                _contractData = contractData;
            }

            public void Update(BaseData data)
            {
            }

            public void SetUnderlying(BaseData data)
            {
            }
        }

        #endregion
    }
}
