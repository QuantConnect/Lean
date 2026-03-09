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
using QuantConnect.Data;
using QuantConnect.Python;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// To provide a base class for option indicator
    /// </summary>
    public abstract class OptionIndicatorBase : MultiSymbolIndicator<IBaseData>
    {
        private DateTime _expiry;

        /// <summary>
        /// Option's symbol object
        /// </summary>
        [PandasIgnore]
        public Symbol OptionSymbol { get; init; }

        /// <summary>
        /// Mirror option symbol (by option right), for implied volatility
        /// </summary>
        protected Symbol _oppositeOptionSymbol { get; private set; }

        /// <summary>
        /// Underlying security's symbol object
        /// </summary>
        protected Symbol _underlyingSymbol => OptionSymbol.Underlying;

        /// <summary>
        /// Option pricing model used to calculate indicator
        /// </summary>
        protected OptionPricingModelType _optionModel { get; set; }

        /// <summary>
        /// Risk-free rate model
        /// </summary>
        protected IRiskFreeInterestRateModel _riskFreeInterestRateModel { get; init; }

        /// <summary>
        /// Dividend yield model, for continuous dividend yield
        /// </summary>
        protected IDividendYieldModel _dividendYieldModel { get; init; }

        /// <summary>
        /// Gets the expiration time of the option
        /// </summary>
        [PandasIgnore]
        public DateTime Expiry
        {
            get
            {
                if (_expiry == default)
                {
                    _expiry = Securities.Option.OptionSymbol.GetSettlementDateTime(OptionSymbol);
                }
                return _expiry;
            }
        }

        /// <summary>
        /// Gets the option right (call/put) of the option
        /// </summary>
        [PandasIgnore]
        public OptionRight Right => OptionSymbol.ID.OptionRight;

        /// <summary>
        /// Gets the strike price of the option
        /// </summary>
        [PandasIgnore]
        public decimal Strike => OptionSymbol.ID.StrikePrice;

        /// <summary>
        /// Gets the option style (European/American) of the option
        /// </summary>
        [PandasIgnore]
        public OptionStyle Style => OptionSymbol.ID.OptionStyle;

        /// <summary>
        /// Risk Free Rate
        /// </summary>
        [PandasIgnore]
        public Identity RiskFreeRate { get; set; }

        /// <summary>
        /// Dividend Yield
        /// </summary>
        [PandasIgnore]
        public Identity DividendYield { get; set; }

        /// <summary>
        /// Gets the option price level
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Price { get; }

        /// <summary>
        /// Gets the mirror option price level, for implied volatility
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> OppositePrice { get; private set; }

        /// <summary>
        /// Gets the underlying's price level
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> UnderlyingPrice { get; }

        /// <summary>
        /// Flag if mirror option is implemented for parity type calculation
        /// </summary>
        [PandasIgnore]
        public bool UseMirrorContract => _oppositeOptionSymbol != null;

        /// <summary>
        /// Initializes a new instance of the OptionIndicatorBase class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="period">The lookback period of volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate the Greek/IV</param>
        protected OptionIndicatorBase(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            Symbol mirrorOption = null, OptionPricingModelType? optionModel = null, int period = 1)
            : base(name, mirrorOption == null ? [option, option.Underlying] : [option, option.Underlying, mirrorOption], period)
        {
            var sid = option.ID;
            if (!sid.SecurityType.IsOption())
            {
                throw new ArgumentException("OptionIndicatorBase only support SecurityType.Option.");
            }

            OptionSymbol = option;
            _riskFreeInterestRateModel = riskFreeRateModel;
            _dividendYieldModel = dividendYieldModel;
            _optionModel = optionModel ?? GetOptionModel(optionModel, sid.OptionStyle);

            RiskFreeRate = new Identity(name + "_RiskFreeRate");
            DividendYield = new Identity(name + "_DividendYield");
            Price = new Identity(name + "_Close");
            UnderlyingPrice = new Identity(name + "_UnderlyingClose");

            DataBySymbol[OptionSymbol].NewInput += (sender, input) => Price.Update(input);
            DataBySymbol[_underlyingSymbol].NewInput += (sender, input) => UnderlyingPrice.Update(input);

            if (mirrorOption != null)
            {
                _oppositeOptionSymbol = mirrorOption;
                OppositePrice = new Identity(Name + "_OppositeClose");
                DataBySymbol[_oppositeOptionSymbol].NewInput += (sender, input) => OppositePrice.Update(input);
            }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// This will round the result to 7 decimal places.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseData input)
        {
            return Math.Round(base.ComputeNextValue(input), 7);
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators
        /// </summary>
        public override void Reset()
        {
            RiskFreeRate.Reset();
            DividendYield.Reset();
            Price.Reset();
            UnderlyingPrice.Reset();
            base.Reset();

            if (UseMirrorContract)
            {
                OppositePrice.Reset();
            }
        }

        /// <summary>
        /// Gets the option pricing model based on the option style, if not specified
        /// </summary>
        /// <param name="optionModel">The optional option pricing model, which will be returned if not null</param>
        /// <param name="optionStyle">The option style</param>
        /// <returns>The option pricing model based on the option style, if not specified</returns>
        public static OptionPricingModelType GetOptionModel(OptionPricingModelType? optionModel, OptionStyle optionStyle)
        {
            if (optionModel.HasValue)
            {
                return optionModel.Value;
            }

            // Default values depend on the option style
            return optionStyle switch
            {
                OptionStyle.European => OptionPricingModelType.BlackScholes,
                OptionStyle.American => OptionPricingModelType.ForwardTree,
                _ => throw new ArgumentOutOfRangeException(nameof(optionStyle), optionStyle, null)
            };
        }
    }
}
