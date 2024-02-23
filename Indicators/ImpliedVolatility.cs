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
using MathNet.Numerics.RootFinding;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Logging;
using QuantConnect.Python;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Implied Volatility indicator that calculate the IV of an option using Black-Scholes Model
    /// </summary>
    public class ImpliedVolatility : OptionIndicatorBase
    {
        private BaseDataConsolidator _consolidator;
        private RateOfChange _roc;
        private decimal _impliedVolatility;

        /// <summary>
        /// Gets the historical volatility of the underlying
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> HistoricalVolatility { get; }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            int period = 252, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : base(name, option, riskFreeRateModel, dividendYieldModel, period, optionModel)
        {
            _roc = new(1);
            HistoricalVolatility = IndicatorExtensions.Times(
                IndicatorExtensions.Of(
                    new StandardDeviation(period),
                    _roc
                ),
                Convert.ToDecimal(Math.Sqrt(252))
            );
            
            _consolidator = new(TimeSpan.FromDays(1));
            _consolidator.DataConsolidated += (_, bar) => {
                _roc.Update(bar.EndTime, bar.Price);
            };
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            int period = 252, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this($"IV({option.Value},{riskFreeRateModel},{dividendYieldModel},{period},{optionModel})", option, riskFreeRateModel, dividendYieldModel, period, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, int period = 252,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel), 
                DividendYieldModelPythonWrapper.FromPyObject(dividendYieldModel), period, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, int period = 252,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this($"IV({option.Value},{period},{riskFreeRateModel},{dividendYieldModel},{optionModel})", option, riskFreeRateModel, dividendYieldModel, period, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
            int period = 252, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this(name, option, riskFreeRateModel, new ConstantDividendYieldModel(dividendYield), period, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, int period = 252,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this($"IV({option.Value},{period},{riskFreeRateModel},{dividendYield},{optionModel})", option, riskFreeRateModel, dividendYield, period, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m,
            int period = 252, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel), 
                new ConstantDividendYieldModel(dividendYield), period, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, int period = 252,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this($"IV({option.Value},{period},{riskFreeRateModel},{dividendYield},{optionModel})", option, riskFreeRateModel, dividendYield, period, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m,
            int period = 252, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this(name, option, new ConstantRiskFreeRateInterestRateModel(riskFreeRate), new ConstantDividendYieldModel(dividendYield), period, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, int period = 252,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this($"IV({option.Value},{period},{riskFreeRate},{dividendYield},{optionModel})", option, riskFreeRate, dividendYield, period, optionModel)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => HistoricalVolatility.Samples >= 2 && Price.Current.Time == UnderlyingPrice.Current.Time;

        /// <summary>
        /// Computes the next value
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            var inputSymbol = input.Symbol;
            if (inputSymbol == _optionSymbol)
            {
                Price.Update(input.EndTime, input.Price);
            }
            else if (inputSymbol == _underlyingSymbol)
            {
                _consolidator.Update(input);
                UnderlyingPrice.Update(input.EndTime, input.Price);
            }
            else
            {
                throw new ArgumentException("The given symbol was not target or reference symbol");
            }

            var time = Price.Current.Time;
            if (time == UnderlyingPrice.Current.Time && Price.IsReady && UnderlyingPrice.IsReady)
            {
                _impliedVolatility = CalculateIV(time);
            }
            return _impliedVolatility;
        }

        // Calculate the theoretical option price
        private decimal TheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, 
            decimal dividendYield, OptionRight optionType, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
        {
            switch (optionModel)
            {
                // Binomial model also follows BSM process (log-normal)
                case OptionPricingModelType.BinomialCoxRossRubinstein:
                    return OptionGreekIndicatorsHelper.CRRTheoreticalPrice(volatility, spotPrice, strikePrice, timeToExpiration, riskFreeRate, dividendYield, optionType);
                case OptionPricingModelType.BlackScholes:
                default:
                    return OptionGreekIndicatorsHelper.BlackTheoreticalPrice(volatility, spotPrice, strikePrice, timeToExpiration, riskFreeRate, dividendYield, optionType);
            }
        }

        // Calculate the IV of the option
        private decimal CalculateIV(DateTime time)
        {
            RiskFreeRate.Update(time, _riskFreeInterestRateModel.GetInterestRate(time));
            DividendYield.Update(time, _dividendYieldModel.GetDividendYield(_underlyingSymbol, time));

            var price = Price.Current.Value;
            var spotPrice = UnderlyingPrice.Current.Value;
            var timeToExpiration = Convert.ToDecimal((Expiry - time).TotalDays) / 365m;

            Func<double, double> f = (vol) => (double)(price - TheoreticalPrice(
                Convert.ToDecimal(vol), spotPrice, Strike, timeToExpiration, RiskFreeRate.Current.Value, DividendYield.Current.Value, Right, _optionModel));
            try
            {
                return Convert.ToDecimal(Brent.FindRoot(f, 1e-7d, 2.0d, 1e-4d, 100));
            }
            catch
            {
                Log.Error("ImpliedVolatility.CalculateIV(): Fail to converge, returning 0.");
                return 0m;
            }
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators
        /// </summary>
        public override void Reset()
        {
            _consolidator.Dispose();
            _consolidator = new(TimeSpan.FromDays(1));
            _consolidator.DataConsolidated += (_, bar) => {
                _roc.Update(bar.EndTime, bar.Price);
            };

            _roc.Reset();
            HistoricalVolatility.Reset();
            base.Reset();
        }
    }
}
