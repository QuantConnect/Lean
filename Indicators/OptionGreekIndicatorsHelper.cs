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
using QuantConnect.Logging;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Helper clas for option greeks related indicators
    /// </summary>
    public class OptionGreekIndicatorsHelper
    {
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

        private static decimal CalculateD1(decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, decimal volatility)
        {
            var numerator = DecimalMath(Math.Log, spotPrice / strikePrice) + (riskFreeRate + 0.5m * volatility * volatility) * timeToExpiration;
            var denominator = volatility * DecimalMath(Math.Sqrt, timeToExpiration);
            return numerator / denominator;
        }

        private static decimal CalculateD2(decimal d1, decimal volatility, decimal timeToExpiration)
        {
            return d1 - volatility * DecimalMath(Math.Sqrt, timeToExpiration);
        }

        // Reference: https://en.wikipedia.org/wiki/Binomial_options_pricing_model#Step_1:_Create_the_binomial_price_tree
        internal static decimal CRRTheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice,
            decimal timeToExpiration, decimal riskFreeRate, OptionRight optionType, int steps = 200)
        {
            var deltaTime = timeToExpiration / steps;
            var upFactor = DecimalMath(Math.Exp, volatility * DecimalMath(Math.Sqrt, deltaTime));
            var discount = DecimalMath(Math.Exp, -riskFreeRate * deltaTime);
            var probUp = (upFactor - discount) / (upFactor * upFactor - 1);
            var probDown = discount - probUp;

            var values = new decimal[steps + 1];
            var exerciseValues = new decimal[steps + 1];

            for (int i = 0; i <= steps; i++)
            {
                var nextPrice = spotPrice * Convert.ToDecimal(Math.Pow((double)upFactor, 2 * i - steps));
                values[i] = PayOff(optionType, nextPrice, strikePrice);
            }

            for (int period = steps - 1; period >= 0; period--)
            {
                for (int i = 0; i <= period; i++)
                {
                    var binomialValue = values[i] * probDown + values[i + 1] * probUp;
                    var exerciseValue = PayOff(optionType, values[i], strikePrice);
                    values[i] = Math.Max(binomialValue, exerciseValue);
                }
            }

            return values[0];
        }

        private static decimal PayOff(OptionRight optionType, decimal price, decimal strike)
        {
            return Math.Max(optionType == OptionRight.Call ? price - strike : strike - price, 0);
        }

        private static decimal DecimalMath(Func<double, double> function, decimal input)
        {
            return Convert.ToDecimal(function((double)input));
        }

        // Reference: https://en.wikipedia.org/wiki/Brent%27s_method#Algorithm
        internal static decimal BrentApproximation(Func<decimal, decimal> function, 
                                                   decimal referenceLevel, 
                                                   decimal lowerBound, 
                                                   decimal upperBound,
                                                   decimal tolerance = 1e-5m,
                                                   int maxIterations = 20)
        {
            var a = lowerBound;
            var b = upperBound;
            var fA = function(a) - referenceLevel;
            var fB = function(b) - referenceLevel;

            if (fA * fB >= 0m)
            {
                Log.Error("Root is not bracketed, returning suboptimal result");
                return Math.Abs(fA) > Math.Abs(fB) ? b : a;
            }

            if (Math.Abs(fA) < Math.Abs(fB))
            {
                var temp = a;
                a = b;
                b = temp;
            }

            var c = a;
            var d = c;
            var s = decimal.MinValue;
            var fS = decimal.MinValue;
            var mFlag = true;
            var iter = 0;

            while (fS != 0 && Math.Abs(b - a) > tolerance && iter < maxIterations)
            {
                fA = function(a) - referenceLevel;
                fB = function(b) - referenceLevel;
                var fC = function(c) - referenceLevel;

                if (fA != fC && fB != fC)
                {
                    // inverse quadratic interpolation
                    s = a * fB * fC / (fA - fB) / (fA - fC) + b * fA * fC / (fB - fA) / (fB - fC) + c * fA * fB / (fC - fA) / (fC - fB);
                }
                else
                {
                    // secant method
                    s = b - fB * (b - a) / (fB - fA);
                }

                var bound1 = (3 * a + b) / 4;
                var minBound = Math.Min(bound1, b);
                var maxBound = Math.Max(bound1, b);
                if (s < minBound || s > maxBound ||
                    (mFlag && Math.Abs(s - b) >= Math.Abs(b - c) / 2) ||
                    (!mFlag && Math.Abs(s - b) >= Math.Abs(c - d) / 2) ||
                    (mFlag && Math.Abs(b - c) < tolerance) ||
                    (!mFlag && Math.Abs(c - d) < tolerance))
                {
                    // bisection method
                    s = (a + b) / 2;
                    mFlag = true;
                }
                else
                {
                    mFlag = false;
                }

                fS = function(s) - referenceLevel;
                d = c;
                c = b;

                if (fA * fS < 0)
                {
                    b = s;
                }
                else
                {
                    a = s;
                }

                if (Math.Abs(fA) < Math.Abs(fB))
                {
                    var temp = a;
                    a = b;
                    b = temp;
                }

                iter++;
            }

            return s;
        }
    }
}
