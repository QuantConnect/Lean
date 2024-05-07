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
     
        public static decimal BlackTheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, decimal dividendYield, OptionRight optionType)
        {
            var d1 = CalculateD1(spotPrice, strikePrice, timeToExpiration, riskFreeRate, dividendYield, volatility);
            var d2 = CalculateD2(d1, volatility, timeToExpiration);
            var norm = new Normal();

            var optionPrice = 0.0m;

            if (optionType == OptionRight.Call)
            {
                optionPrice = spotPrice * DecimalMath(Math.Exp, -dividendYield * timeToExpiration) * DecimalMath(norm.CumulativeDistribution, d1)
                    - strikePrice * DecimalMath(Math.Exp, -riskFreeRate * timeToExpiration) * DecimalMath(norm.CumulativeDistribution, d2);
            }
            else if (optionType == OptionRight.Put)
            {
                optionPrice = strikePrice * DecimalMath(Math.Exp, -riskFreeRate * timeToExpiration) * DecimalMath(norm.CumulativeDistribution, -d2)
                    - spotPrice * DecimalMath(Math.Exp, -dividendYield * timeToExpiration) * DecimalMath(norm.CumulativeDistribution, -d1);
            }
            else
            {
                throw new ArgumentException("Invalid option right.");
            }

            return optionPrice;
        }

        internal static decimal CalculateD1(decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, decimal dividendYield, decimal volatility)
        {
            var numerator = DecimalMath(Math.Log, spotPrice / strikePrice) + (riskFreeRate - dividendYield + 0.5m * volatility * volatility) * timeToExpiration;
            var denominator = volatility * DecimalMath(Math.Sqrt, Math.Max(0m, timeToExpiration));
            if (denominator == 0m)
            {
                // return a random variable large enough to produce normal probability density close to 1
                return 10;
            }
            return numerator / denominator;
        }

        internal static decimal CalculateD2(decimal d1, decimal volatility, decimal timeToExpiration)
        {
            return d1 - volatility * DecimalMath(Math.Sqrt, Math.Max(0m, timeToExpiration));
        }

        // Reference: https://en.wikipedia.org/wiki/Binomial_options_pricing_model#Step_1:_Create_the_binomial_price_tree
        public static decimal CRRTheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice,
            decimal timeToExpiration, decimal riskFreeRate, decimal dividendYield, OptionRight optionType, int steps = Steps)
        {
            var deltaTime = timeToExpiration / steps;
            var upFactor = DecimalMath(Math.Exp, volatility * DecimalMath(Math.Sqrt, deltaTime));
            if (upFactor == 1m)
            {
                // Introduce a very small factor to avoid constant tree while staying low volatility
                upFactor = 1.0001m;
            }
            var downFactor = 1m / upFactor;
            var probUp = (DecimalMath(Math.Exp, (riskFreeRate - dividendYield) * deltaTime) - downFactor) / (upFactor - downFactor);

            return BinomialTheoreticalPrice(deltaTime, probUp, upFactor, riskFreeRate, spotPrice, strikePrice, optionType, steps);
        }

        public static decimal ForwardTreeTheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice,
            decimal timeToExpiration, decimal riskFreeRate, decimal dividendYield, OptionRight optionType, int steps = Steps)
        {
            var deltaTime = timeToExpiration / steps;
            var discount = DecimalMath(Math.Exp, (riskFreeRate - dividendYield) * deltaTime);
            var upFactor = DecimalMath(Math.Exp, volatility * DecimalMath(Math.Sqrt, deltaTime)) * discount;
            var downFactor = DecimalMath(Math.Exp, -volatility * DecimalMath(Math.Sqrt, deltaTime)) * discount;
            if (upFactor - downFactor == 0m)
            {
                // Introduce a very small factor
                // to avoid constant tree while staying low volatility
                upFactor = 1.0001m;
                downFactor = 0.9999m;
            }
            var probUp = (discount - downFactor) / (upFactor - downFactor);

            return BinomialTheoreticalPrice(deltaTime, probUp, upFactor, riskFreeRate, spotPrice, strikePrice, optionType, steps);
        }

        private static decimal BinomialTheoreticalPrice(decimal deltaTime, decimal probUp, decimal upFactor, decimal riskFreeRate,
            decimal spotPrice, decimal strikePrice, OptionRight optionType, int steps = Steps)
        {
            var probDown = 1m - probUp;
            var values = new decimal[steps + 1];

            for (int i = 0; i <= steps; i++)
            {
                var nextPrice = spotPrice * Convert.ToDecimal(Math.Pow((double)upFactor, 2 * i - steps));
                values[i] = OptionPayoff.GetIntrinsicValue(nextPrice, strikePrice, optionType);
            }

            for (int period = steps - 1; period >= 0; period--)
            {
                for (int i = 0; i <= period; i++)
                {
                    var binomialValue = DecimalMath(Math.Exp, -riskFreeRate * deltaTime) * (values[i] * probDown + values[i + 1] * probUp);
                    // No advantage for American put option to exercise early in risk-neutral setting
                    if (optionType == OptionRight.Put)
                    {
                        values[i] = binomialValue;
                        continue;
                    }
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
