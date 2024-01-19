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
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Python;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// To provide a base class for option greeks indicator
    /// </summary>
    public abstract class OptionGreeksIndicatorBase : IndicatorBase<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Option's symbol object
        /// </summary>
        protected readonly Symbol _optionSymbol;

        /// <summary>
        /// Underlying security's symbol object
        /// </summary>
        protected Symbol _underlyingSymbol => _optionSymbol.Underlying;

        /// <summary>
        /// Cache of the current value of the greek
        /// </summary>
        protected decimal _greekValue;

        /// <summary>
        /// Option pricing model used to calculate greeks
        /// </summary>
        protected OptionPricingModelType _optionModel;

        /// <summary>
        /// Risk-free rate model
        /// </summary>
        protected readonly IRiskFreeInterestRateModel _riskFreeInterestRateModel;

        /// <summary>
        /// Gets the expiration time of the option
        /// </summary>
        public DateTime Expiry => _optionSymbol.ID.Date;

        /// <summary>
        /// Gets the option right (call/put) of the option
        /// </summary>
        public OptionRight Right => _optionSymbol.ID.OptionRight;

        /// <summary>
        /// Gets the strike price of the option
        /// </summary>
        public decimal Strike => _optionSymbol.ID.StrikePrice;

        /// <summary>
        /// Gets the option style (European/American) of the option
        /// </summary>
        public OptionStyle Style => _optionSymbol.ID.OptionStyle;

        /// <summary>
        /// Risk Free Rate
        /// </summary>
        public Identity RiskFreeRate { get; set; }

        /// <summary>
        /// Gets the implied volatility of the option
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ImpliedVolatility { get; }

        /// <summary>
        /// Gets the option price level
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Price { get; }

        /// <summary>
        /// Gets the underlying's price level
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> UnderlyingPrice { get; }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, int period = 2,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name)
        {
            var sid = option.ID;
            if (!sid.SecurityType.IsOption())
            {
                throw new ArgumentException("ImpliedVolatility only support SecurityType.Option.");
            }

            _optionSymbol = option;
            _riskFreeInterestRateModel = riskFreeRateModel;
            _optionModel = optionModel;
            ivModel = ivModel ?? optionModel;

            RiskFreeRate = new Identity(name + "_RiskFreeRate");
            ImpliedVolatility = new ImpliedVolatility(name + "_IV", option, riskFreeRateModel, period, (OptionPricingModelType)ivModel);
            Price = new Identity(name + "_Close");
            UnderlyingPrice = new Identity(name + "_UnderlyingClose");

            WarmUpPeriod = period;
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, PyObject riskFreeRateModel, int period = 2,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel), period, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, decimal riskFreeRate = 0.05m, int period = 2,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this(name, option, new ConstantRiskFreeRateInterestRateModel(riskFreeRate), period, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= 2 && Price.Current.Time == UnderlyingPrice.Current.Time;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value of the following sub-indicators from the given state:
        /// StandardDeviation, MiddleBand, UpperBand, LowerBand, BandWidth, %B
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            var time = input.EndTime;
            var inputSymbol = input.Symbol;

            if (inputSymbol == _optionSymbol)
            {
                ImpliedVolatility.Update(input);
                Price.Update(time, input.Price);
            }
            else if (inputSymbol == _underlyingSymbol)
            {
                ImpliedVolatility.Update(input);
                UnderlyingPrice.Update(time, input.Price);
            }
            else
            {
                throw new ArgumentException("The given symbol was not target or reference symbol");
            }

            if (Price.Current.Time == UnderlyingPrice.Current.Time)
            {
                RiskFreeRate.Update(time, _riskFreeInterestRateModel.GetInterestRate(time));
                _greekValue = CalculateGreek(time);
            }
            return _greekValue;
        }

        // Calculate the Delta of the option
        protected virtual decimal CalculateGreek(DateTime time)
        {
            throw new NotImplementedException("'CalculateGreek' method must be implemented");
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators (StandardDeviation, LowerBand, MiddleBand, UpperBand, BandWidth, %B)
        /// </summary>
        public override void Reset()
        {
            RiskFreeRate.Reset();
            ImpliedVolatility.Reset();
            Price.Reset();
            UnderlyingPrice.Reset();
            base.Reset();
        }
    }
}
