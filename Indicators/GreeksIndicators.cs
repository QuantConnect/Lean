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
using QuantConnect.Securities.Option;
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Helper class that holds and updates the greeks indicators
    /// </summary>
    public class GreeksIndicators
    {
        private readonly static IRiskFreeInterestRateModel _interestRateProvider = new InterestRateProvider();
        private readonly static IDividendYieldModel _constantDividendYieldModel = new ConstantDividendYieldModel(0);

        private readonly Symbol _optionSymbol;
        private readonly Symbol _mirrorOptionSymbol;

        /// <summary>
        /// Gets the implied volatility indicator
        /// </summary>
        public ImpliedVolatility ImpliedVolatility { get; }

        /// <summary>
        /// Gets the delta indicator
        /// </summary>
        public Delta Delta { get; }

        /// <summary>
        /// Gets the gamma indicator
        /// </summary>
        public Gamma Gamma { get; }

        /// <summary>
        /// Gets the vega indicator
        /// </summary>
        public Vega Vega { get; }

        /// <summary>
        /// Gets the daily theta indicator
        /// </summary>
        public Theta ThetaPerDay { get; }

        /// <summary>
        /// Gets the rho indicator
        /// </summary>
        public Rho Rho { get; }

        /// <summary>
        /// Gets the interest rate used in the calculations
        /// </summary>
        public decimal InterestRate => Delta.RiskFreeRate;

        /// <summary>
        /// Gets the dividend yield used in the calculations
        /// </summary>
        public decimal DividendYield => Delta.DividendYield;

        /// <summary>
        /// Gets the current greeks values
        /// </summary>
        public Greeks Greeks
        {
            get
            {
                var theta = 0m;
                var thetaPerDay = ThetaPerDay.Current.Value;
                try
                {
                    theta = thetaPerDay * 365m;
                }
                catch (OverflowException)
                {
                    theta = thetaPerDay < 0 ? decimal.MinValue : decimal.MaxValue;
                }

                return new Greeks(Delta, Gamma, Vega, theta, Rho, 0m);
            }
        }

        /// <summary>
        /// Whether the mirror option is set and will be used in the calculations.
        /// </summary>
        public bool UseMirrorOption => _mirrorOptionSymbol != null;

        /// <summary>
        /// Gets the current result of the greeks indicators, including the implied volatility, theoretical price and greeks values
        /// </summary>
        public OptionPriceModelResult CurrentResult => new OptionPriceModelResult(ImpliedVolatility.TheoreticalPrice, ImpliedVolatility, Greeks);

        /// <summary>
        /// Gets the dividend yield model to be used in the calculations for the specified option symbol.
        /// </summary>
        public static IDividendYieldModel GetDividendYieldModel(Symbol optionSymbol)
        {
            return optionSymbol.SecurityType != SecurityType.IndexOption
                ? DividendYieldProvider.CreateForOption(optionSymbol)
                : _constantDividendYieldModel;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="GreeksIndicators"/> class
        /// </summary>
        public GreeksIndicators(Symbol optionSymbol, Symbol mirrorOptionSymbol, OptionPricingModelType? optionModel = null,
            OptionPricingModelType? ivModel = null, IDividendYieldModel dividendYieldModel = null,
            IRiskFreeInterestRateModel riskFreeInterestRateModel = null)
        {
            _optionSymbol = optionSymbol;
            _mirrorOptionSymbol = mirrorOptionSymbol;

            dividendYieldModel ??= GetDividendYieldModel(optionSymbol);
            riskFreeInterestRateModel ??= _interestRateProvider;

            ImpliedVolatility = new ImpliedVolatility(_optionSymbol, riskFreeInterestRateModel, dividendYieldModel, _mirrorOptionSymbol, ivModel);
            Delta = new Delta(_optionSymbol, riskFreeInterestRateModel, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);
            Gamma = new Gamma(_optionSymbol, riskFreeInterestRateModel, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);
            Vega = new Vega(_optionSymbol, riskFreeInterestRateModel, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);
            ThetaPerDay = new Theta(_optionSymbol, riskFreeInterestRateModel, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);
            Rho = new Rho(_optionSymbol, riskFreeInterestRateModel, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);

            Delta.ImpliedVolatility = ImpliedVolatility;
            Gamma.ImpliedVolatility = ImpliedVolatility;
            Vega.ImpliedVolatility = ImpliedVolatility;
            ThetaPerDay.ImpliedVolatility = ImpliedVolatility;
            Rho.ImpliedVolatility = ImpliedVolatility;
        }

        /// <summary>
        /// Feeds the specified data into the indicators
        /// </summary>
        public void Update(IBaseData data)
        {
            ImpliedVolatility.Update(data);
            Delta.Update(data);
            Gamma.Update(data);
            Vega.Update(data);
            ThetaPerDay.Update(data);
            Rho.Update(data);
        }

        /// <summary>
        /// Resets the indicators to their default state
        /// </summary>
        public void Reset()
        {
            ImpliedVolatility.Reset();
            Delta.Reset();
            Gamma.Reset();
            Vega.Reset();
            ThetaPerDay.Reset();
            Rho.Reset();
        }
    }
}
