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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Helper class that holds and updates the greeks indicators
    /// </summary>
    public class GreeksIndicators
    {
        private readonly static IRiskFreeInterestRateModel _interestRateProvider = new InterestRateProvider();

        private readonly Symbol _optionSymbol;
        private readonly Symbol _mirrorOptionSymbol;

        /// <summary>
        /// Gets the implied volatility indicator
        /// </summary>
        public ImpliedVolatility ImpliedVolatility { get; }

        /// <summary>
        /// Gets the delta indicator
        /// </summary>
        public  Delta Delta { get; }

        /// <summary>
        /// Gets the gamma indicator
        /// </summary>
        public  Gamma Gamma { get; }

        /// <summary>
        /// Gets the vega indicator
        /// </summary>
        public Vega Vega { get; }

        /// <summary>
        /// Gets the theta indicator
        /// </summary>
        public Theta Theta { get; }

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
        public Greeks Greeks => new GreeksHolder(Delta, Gamma, Vega, Theta, Rho);

        /// <summary>
        /// Creates a new instance of the <see cref="GreeksIndicators"/> class
        /// </summary>
        public GreeksIndicators(Symbol optionSymbol, Symbol mirrorOptionSymbol, OptionPricingModelType? optionModel = null,
            OptionPricingModelType? ivModel = null)
        {
            _optionSymbol = optionSymbol;
            _mirrorOptionSymbol = mirrorOptionSymbol;

            IDividendYieldModel dividendYieldModel = optionSymbol.SecurityType != SecurityType.IndexOption
                ? DividendYieldProvider.CreateForOption(_optionSymbol)
                : new ConstantDividendYieldModel(0);

            ImpliedVolatility = new ImpliedVolatility(_optionSymbol, _interestRateProvider, dividendYieldModel, _mirrorOptionSymbol, ivModel);
            Delta = new Delta(_optionSymbol, _interestRateProvider, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);
            Gamma = new Gamma(_optionSymbol, _interestRateProvider, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);
            Vega = new Vega(_optionSymbol, _interestRateProvider, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);
            Theta = new Theta(_optionSymbol, _interestRateProvider, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);
            Rho = new Rho(_optionSymbol, _interestRateProvider, dividendYieldModel, _mirrorOptionSymbol, optionModel, ivModel);

            Delta.ImpliedVolatility = ImpliedVolatility;
            Gamma.ImpliedVolatility = ImpliedVolatility;
            Vega.ImpliedVolatility = ImpliedVolatility;
            Theta.ImpliedVolatility = ImpliedVolatility;
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
            Theta.Update(data);
            Rho.Update(data);
        }

        private class GreeksHolder : Greeks
        {
            public override decimal Delta { get; }

            public override decimal Gamma { get; }

            public override decimal Vega { get; }

            public override decimal Theta { get; }

            public override decimal Rho { get; }

            public override decimal Lambda { get; }

            public GreeksHolder(decimal delta, decimal gamma, decimal vega, decimal theta, decimal rho)
            {
                Delta = delta;
                Gamma = gamma;
                Vega = vega;
                Theta = theta;
                Rho = rho;
            }
        }
    }
}
