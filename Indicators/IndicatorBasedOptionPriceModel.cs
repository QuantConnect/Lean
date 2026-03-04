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
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Provides an implementation of <see cref="IOptionPriceModel"/> that uses QuantConnect indicators
    /// to provide a theoretical price for the option contract.
    /// </summary>
    public class IndicatorBasedOptionPriceModel : OptionPriceModel
    {
        private readonly OptionPricingModelType? _optionPricingModelType;
        private readonly OptionPricingModelType? _ivModelType;
        private IDividendYieldModel _dividendYieldModel;
        private readonly IRiskFreeInterestRateModel _riskFreeInterestRateModel;
        private readonly bool _userSpecifiedDividendYieldModel;
        private readonly bool _useMirrorContract;
        private readonly SecurityManager _securityProvider;

        /// <summary>
        /// Creates a new instance of the <see cref="IndicatorBasedOptionPriceModel"/> class
        /// </summary>
        /// <param name="optionModel">The option pricing model type to be used by the indicators</param>
        /// <param name="ivModel">The option pricing model type to be used by the implied volatility indicator</param>
        /// <param name="dividendYieldModel">The dividend yield model to be used by the indicators</param>
        /// <param name="riskFreeInterestRateModel">The risk free interest rate model to be used by the indicators</param>
        /// <param name="useMirrorContract">Whether to use the mirror contract when possible</param>
        /// <param name="securityProvider">The security provider used to fetch the mirror contract</param>
        public IndicatorBasedOptionPriceModel(OptionPricingModelType? optionModel = null,
            OptionPricingModelType? ivModel = null, IDividendYieldModel dividendYieldModel = null,
            IRiskFreeInterestRateModel riskFreeInterestRateModel = null, bool useMirrorContract = true,
            SecurityManager securityProvider = null)
        {
            _optionPricingModelType = optionModel;
            _ivModelType = ivModel;
            _dividendYieldModel = dividendYieldModel;
            _riskFreeInterestRateModel = riskFreeInterestRateModel;
            _useMirrorContract = useMirrorContract;
            _userSpecifiedDividendYieldModel = dividendYieldModel != null;
            _securityProvider = securityProvider;
        }

        /// <summary>
        /// Creates a new <see cref="OptionPriceModelResult"/> containing the theoretical price based on
        /// QuantConnect indicators.
        /// </summary>
        /// <param name="parameters">The evaluation parameters</param>
        /// <returns>
        /// An instance of <see cref="OptionPriceModelResult"/> containing the theoretical
        /// price of the specified option contract.
        /// </returns>
        public override OptionPriceModelResult Evaluate(OptionPriceModelParameters parameters)
        {
            var contract = parameters.Contract;
            // expired options have no price
            if (contract.Time.Date > contract.Expiry.Date)
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Expired {contract.Symbol}. Time > Expiry: {contract.Time.Date} > {contract.Expiry.Date}");
                }
                return OptionPriceModelResult.None;
            }

            var option = parameters.Security as Option;
            var underlying = option.Underlying;

            if (option.Price == 0)
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Missing data for the option security {option.Symbol}.");
                }
                return OptionPriceModelResult.None;
            }

            if (underlying.Price == 0)
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Missing data for the underlying security {underlying.Symbol}.");
                }
                return OptionPriceModelResult.None;
            }

            var contractSymbol = contract.Symbol;
            Symbol mirrorContractSymbol = null;

            if (!_userSpecifiedDividendYieldModel)
            {
                _dividendYieldModel = GreeksIndicators.GetDividendYieldModel(contractSymbol);
            }

            if (_useMirrorContract)
            {
                mirrorContractSymbol = contractSymbol.GetMirrorOptionSymbol();
            }

            if (!_securityProvider.TryGetValue(mirrorContractSymbol, out var mirrorOption) || mirrorOption.Price == 0)
            {
                if (Log.DebuggingEnabled)
                {
                    if (mirrorOption == null)
                    {
                        Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Mirror contract {mirrorContractSymbol} not found. Using contract symbol only.");
                    }
                    else
                    {
                        Log.Debug($"IndicatorBasedOptionPriceModel.Evaluate(). Missing data for the mirror option contract {mirrorContractSymbol}. Using contract symbol only.");
                    }
                }

                // Null so that the indicators don't consider the mirror option and don't expect data for it
                mirrorContractSymbol = null;
                mirrorOption = null;
            }

            var indicators = new GreeksIndicators(contractSymbol, mirrorContractSymbol, _optionPricingModelType, _ivModelType,
                    _dividendYieldModel, _riskFreeInterestRateModel);

            var time = option.LocalTime;
            indicators.Update(new IndicatorDataPoint(underlying.Symbol, time, underlying.Price));
            indicators.Update(new IndicatorDataPoint(option.Symbol, time, option.Price));
            if (mirrorOption != null)
            {
                indicators.Update(new IndicatorDataPoint(mirrorOption.Symbol, time, mirrorOption.Price));
            }

            return indicators.CurrentResult;
        }
    }
}
