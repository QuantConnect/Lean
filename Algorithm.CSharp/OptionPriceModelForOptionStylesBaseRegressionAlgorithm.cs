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
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Base regression algorithm exercising different style options with option price models that might
    /// or might not support them. Also, if the option style is supported, greeks are asserted to be accesible and have valid values.
    /// </summary>
    public abstract class OptionPriceModelForOptionStylesBaseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _optionStyleIsSupported;
        private Option _option;
        private bool _checkGreeks;
        private bool _triedGreeksCalculation;

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

                CheckGreeks(kvp.Value);
            }
        }

        public override void OnEndOfDay(Symbol symbol)
        {
            _checkGreeks = true;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_triedGreeksCalculation)
            {
                throw new RegressionTestException("Expected greeks to be accessed");
            }
        }

        protected void Init(Option option, bool optionStyleIsSupported)
        {
            _option = option;
            _optionStyleIsSupported = optionStyleIsSupported;
            _checkGreeks = true;
            _triedGreeksCalculation = false;
        }


        public void CheckGreeks(OptionChain contracts)
        {
            if (!_checkGreeks || !contracts.Any())
            {
                return;
            }

            _checkGreeks = false;
            _triedGreeksCalculation = true;

            foreach (var contract in contracts)
            {
                Greeks greeks = null;
                try
                {
                    greeks = contract.Greeks;

                    // Greeks should have not been successfully accessed if the option style is not supported
                    if (!_optionStyleIsSupported)
                    {
                        throw new RegressionTestException($"Expected greeks not to be calculated for {contract.Symbol.Value}, an {_option.Style} style option, using {_option?.PriceModel.GetType().Name}, which does not support them, but they were");

                    }
                }
                catch (ArgumentException)
                {
                    // ArgumentException is only expected if the option style is not supported
                    if (_optionStyleIsSupported)
                    {
                        throw new RegressionTestException($"Expected greeks to be calculated for {contract.Symbol.Value}, an {_option.Style} style option, using {_option?.PriceModel.GetType().Name}, which supports them, but they were not");
                    }
                }

                // Greeks should be valid if they were successfuly accessed for supported option style
                if (_optionStyleIsSupported)
                {
                    if (greeks == null)
                    {
                        greeks = new ModeledGreeks();
                    }

                    if (greeks.Delta == 0m && greeks.Gamma == 0m && greeks.Theta == 0m && greeks.Vega == 0m && greeks.Rho == 0m)
                    {
                        throw new RegressionTestException($"Expected greeks to not be zero simultaneously for {contract.Symbol.Value}, an {_option.Style} style option, using {_option?.PriceModel.GetType().Name}, but they were");
                    }

                    // Delta can be {-1, 0, 1} if the price is too wild, rho can be 0 if risk free rate is 0
                    // Vega can be 0 if the price is very off from theoretical price, Gamma = 0 if Delta belongs to {-1, 1}
                    if (((contract.Right == OptionRight.Call && (greeks.Delta < 0m || greeks.Delta > 1m || greeks.Rho < 0m))
                        || (contract.Right == OptionRight.Put && (greeks.Delta < -1m || greeks.Delta > 0m || greeks.Rho > 0m))
                        || greeks.Vega < 0m || greeks.Gamma < 0m))
                    {
                        throw new RegressionTestException($"Expected greeks to have valid values. Greeks were: Delta: {greeks.Delta}, Rho: {greeks.Rho}, Theta: {greeks.Theta}, Vega: {greeks.Vega}, Gamma: {greeks.Gamma}");
                    }
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        abstract public long DataPoints { get; }

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        abstract public int AlgorithmHistoryDataPoints { get; }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        abstract public AlgorithmStatus AlgorithmStatus { get; }

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        abstract public Dictionary<string, string> ExpectedStatistics { get; }
    }
}
