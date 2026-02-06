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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Util;
using System;
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Provides an implementation of <see cref="IOptionPriceModel"/> that uses QuantConnect indicators
    /// to provide a theoretical price for the option contract.
    /// </summary>
    public class IndicatorBasedOptionPriceModel : IOptionPriceModel
    {
        private Symbol _contractSymbol;
        private Symbol _mirrorContractSymbol;
        private IDividendYieldModel _dividendYieldModel;
        private GreeksIndicators _indicators;

        /// <summary>
        /// Creates a new <see cref="OptionPriceModelResult"/> containing the theoretical price based on
        /// QuantConnect indicators.
        /// </summary>
        /// <param name="security">The option security object</param>
        /// <param name="slice">
        /// The current data slice. This can be used to access other information
        /// available to the algorithm
        /// </param>
        /// <param name="contract">The option contract to evaluate</param>
        /// <returns>
        /// An instance of <see cref="OptionPriceModelResult"/> containing the theoretical
        /// price of the specified option contract.
        /// </returns>
        public OptionPriceModelResult Evaluate(Security security, Slice slice, OptionContract contract)
        {
            // expired options have no price
            if (contract.Time.Date > contract.Expiry.Date)
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Expired {contract.Symbol}. Time > Expiry: {contract.Time.Date} > {contract.Expiry.Date}");
                }
                return OptionPriceModelResult.None;
            }

            var contractSymbol = _contractSymbol;
            var mirrorContractSymbol = _mirrorContractSymbol;
            var symbolsChanged = false;
            // These models are supposed to be one per contract (security instance), so we cache the symbols to avoid calling 
            // GetMirrorOptionSymbol multiple times. If the contract changes by any reason, we just update the cached symbols.
            if (contractSymbol != contract.Symbol)
            {
                contractSymbol = _contractSymbol = contract.Symbol;
                mirrorContractSymbol = _mirrorContractSymbol = contractSymbol.GetMirrorOptionSymbol();
                _dividendYieldModel = GreeksIndicators.GetDividendYieldModel(contractSymbol);
                symbolsChanged = true;
            }

            BaseData underlyingData = null;
            BaseData optionData = null;
            BaseData mirrorOptionData = null;

            foreach (var useBars in new[] { true, false })
            {
                if (useBars)
                {
                    TradeBar underlyingTradeBar = null;
                    QuoteBar underlyingQuoteBar = null;
                    if ((slice.Bars.TryGetValue(contractSymbol.Underlying, out underlyingTradeBar) || 
                         slice.QuoteBars.TryGetValue(contractSymbol.Underlying, out underlyingQuoteBar)) &&
                        slice.QuoteBars.TryGetValue(contractSymbol, out var optionQuoteBar))
                    {
                        underlyingData = (BaseData)underlyingTradeBar ?? underlyingQuoteBar;
                        optionData = optionQuoteBar;

                        if (slice.QuoteBars.TryGetValue(mirrorContractSymbol, out var mirrorOptionQuoteBar))
                        {
                            mirrorOptionData = mirrorOptionQuoteBar;
                        }

                        break;
                    }
                }
                else
                {
                    if (slice.Ticks.TryGetValue(contractSymbol.Underlying, out var underlyingTicks) &&
                        slice.Ticks.TryGetValue(contractSymbol, out var optionTicks))
                    {
                        // Get last underlying trade tick
                        underlyingData = underlyingTicks
                            .Where(x => x.TickType == TickType.Trade)
                            .LastOrDefault();
                        if (underlyingData == null)
                        {
                            continue;
                        }

                        // Get last option quote tick
                        optionData = optionTicks
                            .Where(x => x.TickType == TickType.Quote)
                            .LastOrDefault();
                        if (optionData == null)
                        {
                            underlyingData = null;
                            continue;
                        }

                        // Try to get last mirror option quote tick
                        if (slice.Ticks.TryGetValue(_mirrorContractSymbol, out var mirrorOptionTicks))
                        {
                            mirrorOptionData = mirrorOptionTicks
                                .Where(x => x.TickType == TickType.Quote)
                                .LastOrDefault();
                        }

                        break;
                    }
                }
            }

            if (underlyingData == null || optionData == null)
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Missing data for {contractSymbol} or {contractSymbol.Underlying}.");
                }
                return OptionPriceModelResult.None;
            }

            if (mirrorOptionData == null)
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Missing data for mirror option {mirrorContractSymbol}. Using contract symbol only.");
                }
                // Null so that the indicators don't consider the mirror option and don't expect data for it
                mirrorContractSymbol = null;
            }

            var greeksIndicators = new Lazy<GreeksIndicatorsResult>(() =>
            {
                if (_indicators == null || symbolsChanged ||
                    // The mirror contract can go from null to non-null and vice versa, so we need to check if the symbol has changed in that case as well
                    (_indicators.UseMirrorOption && mirrorContractSymbol == null) || (!_indicators.UseMirrorOption && mirrorContractSymbol != null))
                {
                    // We'll try to reuse the indicators instance whenever possible
                    _indicators = new GreeksIndicators(contractSymbol, mirrorContractSymbol, dividendYieldModel: _dividendYieldModel);
                }

                if (underlyingData != null)
                {
                    _indicators.Update(underlyingData);
                }
                if (optionData != null)
                {
                    _indicators.Update(optionData);
                }
                if (mirrorOptionData != null)
                {
                    _indicators.Update(mirrorOptionData);
                }

                var result = _indicators.CurrentResult;
                _indicators.Reset();

                return result;
            }, isThreadSafe: false);

            return new OptionPriceModelResult(
                () => greeksIndicators.Value.TheoreticalPrice, 
                () => greeksIndicators.Value.ImpliedVolatility, 
                () => greeksIndicators.Value.Greeks);
        }
    }
}
