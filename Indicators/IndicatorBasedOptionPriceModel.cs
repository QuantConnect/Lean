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

            var contractSymbol = contract.Symbol;
            var underlyingData = slice.AllData
                // We use trades for the underlying (see how Greeks indicators are registered to algorithms)
                .Where(x => x.Symbol == contractSymbol.Underlying && (x is TradeBar || (x is Tick tick && tick.TickType == TickType.Trade)))
                // Order by resolution
                .OrderBy(x => x.EndTime - x.Time)
                // Let's use the lowest resolution available, trying to match our pre calculated daily greeks (using daily bars if possible).
                // If ticks, use the last tick in the slice
                .LastOrDefault();

            var period = TimeSpan.Zero;
            BaseData optionData = null;
            if (underlyingData != null)
            {
                period = underlyingData.EndTime - underlyingData.Time;
                optionData = slice.AllData
                    .Where(x => x.Symbol == contractSymbol && 
                        // Use the same resolution data
                        x.EndTime - x.Time == period &&
                        // We use quotes for the options (see how Greeks indicators are registered to algorithms)
                        (x is QuoteBar || (x is Tick tick && tick.TickType == TickType.Quote)))
                    .LastOrDefault();
            }

            if (underlyingData == null || optionData == null)
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Missing data for {contractSymbol} or {contractSymbol.Underlying}.");
                }
                return OptionPriceModelResult.None;
            }

            var mirrorContractSymbol = Symbol.CreateOption(contractSymbol.Underlying,
                contractSymbol.ID.Symbol,
                contractSymbol.ID.Market,
                contractSymbol.ID.OptionStyle,
                contractSymbol.ID.OptionRight == OptionRight.Call ? OptionRight.Put : OptionRight.Call,
                contractSymbol.ID.StrikePrice,
                contractSymbol.ID.Date);
            var mirrorOptionData = slice.AllData
                .Where(x => x.Symbol == mirrorContractSymbol &&
                    // Use the same resolution data
                    x.EndTime - x.Time == period &&
                    (x is QuoteBar || (x is Tick tick && tick.TickType == TickType.Quote)))
                .LastOrDefault();

            if (mirrorOptionData == null)
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Missing data for mirror option {mirrorContractSymbol}. Using contract symbol only.");
                }
                mirrorContractSymbol = null;
            }

            var greeksIndicators = new Lazy<GreeksIndicators>(() =>
            {
                var indicators = new GreeksIndicators(contractSymbol, mirrorContractSymbol);

                if (underlyingData != null)
                {
                    indicators.Update(underlyingData);
                }
                if (optionData != null)
                {
                    indicators.Update(optionData);
                }
                if (mirrorOptionData != null)
                {
                    indicators.Update(mirrorOptionData);
                }

                return indicators;
            }, isThreadSafe: false);

            return new OptionPriceModelResult(
                () => greeksIndicators.Value.ImpliedVolatility.TheoreticalPrice, 
                () => greeksIndicators.Value.ImpliedVolatility, 
                () => greeksIndicators.Value.Greeks);
        }
    }
}
