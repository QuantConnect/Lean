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
using MathNet.Numerics.Distributions;
using QuantConnect.Util;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Helper clas for option greeks related indicators
    /// </summary>
    public class OptionGreekIndicatorsHelper
    {
        /// <summary>
        /// Number of steps in binomial tree simulation to obtain Greeks/IV
        /// </summary>
        public const int Steps = 200;
     
        internal static decimal BlackTheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, OptionRight optionType)
        {
            var d1 = CalculateD1(spotPrice, strikePrice, timeToExpiration, riskFreeRate, volatility);
            var d2 = CalculateD2(d1, volatility, timeToExpiration);
            var norm = new Normal();

            var optionPrice = 0.0m;

            if (optionType == OptionRight.Call)
            {
                optionPrice = spotPrice * DecimalMath(norm.CumulativeDistribution, d1) - strikePrice * DecimalMath(Math.Exp, -riskFreeRate * timeToExpiration) * DecimalMath(norm.CumulativeDistribution, d2);
            }
            else if (optionType == OptionRight.Put)
            {
                optionPrice = strikePrice * DecimalMath(Math.Exp, -riskFreeRate * timeToExpiration) * DecimalMath(norm.CumulativeDistribution, -d2) - spotPrice * DecimalMath(norm.CumulativeDistribution, -d1);
            }
            else
            {
                throw new ArgumentException("Invalid option right.");
            }

            return optionPrice;
        }

        internal static decimal CalculateD1(decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, decimal volatility)
        {
            var numerator = DecimalMath(Math.Log, spotPrice / strikePrice) + (riskFreeRate + 0.5m * volatility * volatility) * timeToExpiration;
            var denominator = volatility * DecimalMath(Math.Sqrt, timeToExpiration);
            if (denominator == 0m)
            {
                // return a random variable large enough to produce normal probability density close to 1
                return 10;
            }
            return numerator / denominator;
        }

        private static decimal CalculateD2(decimal d1, decimal volatility, decimal timeToExpiration)
        {
            return d1 - volatility * DecimalMath(Math.Sqrt, timeToExpiration);
        }

        // Reference: https://en.wikipedia.org/wiki/Binomial_options_pricing_model#Step_1:_Create_the_binomial_price_tree
        internal static decimal CRRTheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice,
            decimal timeToExpiration, decimal riskFreeRate, OptionRight optionType, int steps = Steps)
        {
            var deltaTime = timeToExpiration / steps;
            var upFactor = DecimalMath(Math.Exp, volatility * DecimalMath(Math.Sqrt, deltaTime));
            if (upFactor == 1m)
            {
                // Introduce a very small factor to avoid constant tree while staying low volatility
                upFactor = 1.0001m;
            }
            var discount = DecimalMath(Math.Exp, -riskFreeRate * deltaTime);
            var probUp = upFactor * (upFactor - discount) / (upFactor * upFactor - 1);
            var probDown = discount - probUp;

            var values = new decimal[steps + 1];
            var exerciseValues = new decimal[steps + 1];

            for (int i = 0; i <= steps; i++)
            {
                var nextPrice = spotPrice * Convert.ToDecimal(Math.Pow((double)upFactor, 2 * i - steps));
                values[i] = OptionPayoff.GetIntrinsicValue(nextPrice, strikePrice, optionType);
            }

            for (int period = steps - 1; period >= 0; period--)
            {
                for (int i = 0; i <= period; i++)
                {
                    var binomialValue = values[i] * probDown + values[i + 1] * probUp;
                    var nextPrice = spotPrice * Convert.ToDecimal(Math.Pow((double)upFactor, 2 * i - period));
                    var exerciseValue = OptionPayoff.GetIntrinsicValue(nextPrice, strikePrice, optionType);
                    values[i] = Math.Max(binomialValue, exerciseValue);
                }
            }

            return values[0];
        }

        internal static decimal DecimalMath(Func<double, double> function, decimal input)
        {
            return Convert.ToDecimal(function((double)input));
        }
    }
}
