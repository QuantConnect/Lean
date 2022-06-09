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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Base regression algorithm excersizing for exercising different style options with option price models that migth
    /// or might not support them. Also, if the option style is supported, greeks are asserted to be accesible and have valid values.
    /// </summary>
    public abstract class OptionPriceModelForOptionStylesBaseRegressionAlgorithm : QCAlgorithm
    {
        protected OptionStyle _optionStyle = OptionStyle.European;
        protected bool _optionStyleIsSupported;
        protected bool _triedGreeksCalculation;
        protected Option _option;
        protected IEnumerable<OptionContract> _contracts;

        public override void OnData(Slice slice)
        {
            if (IsWarmingUp)
            {
                return;
            }

            foreach (var kvp in slice.OptionChains)
            {
                if (kvp.Key != _option?.Symbol)
                {
                    continue;
                }

                var chain = kvp.Value;
                _contracts = chain.Where(x => x.Right == OptionRight.Call);
            }
        }

        public override void OnEndOfDay(Symbol symbol)
        {
            if (!IsWarmingUp)
            {
                CheckGreeks();
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_triedGreeksCalculation)
            {
                throw new Exception("Expected greeks to be accessed");
            }
        }

        public void CheckGreeks()
        {
            if (_contracts == null || !_contracts.Any())
            {
                return;
            }

            _triedGreeksCalculation = true;

            foreach (var contract in _contracts)
            {
                Greeks greeks = new Greeks();
                try
                {
                    greeks = contract.Greeks;

                    // Greeks should have not been successfully accessed if the option style is not supported
                    if (!_optionStyleIsSupported)
                    {
                        throw new Exception($"Expected greeks not to be calculated for {contract.Symbol.Value}, an {_optionStyle} style option, using {_option?.PriceModel.GetType().Name}, which does not support them, but they were");

                    }
                }
                catch (ArgumentException)
                {
                    // ArgumentException is only expected if the option style is not supported
                    if (_optionStyleIsSupported)
                    {
                        throw new Exception($"Expected greeks to be calculated for {contract.Symbol.Value}, an {_optionStyle} style option, using {_option?.PriceModel.GetType().Name}, which supports them, but they were not");
                    }
                }

                // Greeks shpould be valid if they were successfuly accessed for supported option style
                if (_optionStyleIsSupported && (greeks.Delta < 0m || greeks.Delta > 1m || greeks.Theta >= 0m || greeks.Rho <= 0m || greeks.Vega <= 0m))
                {
                    throw new Exception($"Expected greeks to have valid values. Greeks were: Gamma: {greeks.Gamma}, Rho: {greeks.Rho}, Delta: {greeks.Delta}, Vega: {greeks.Vega}");
                }
            }
        }
    }
}
