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

        internal static decimal LeisenReimerTheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, OptionRight optionType, int numSteps = 500)
        {
            var upFactor = CalculateUpFactor(volatility, timeToExpiration, numSteps);
            var downFactor = 1.0m / upFactor;
            var upProbability = CalculateUpProbability(riskFreeRate, volatility, timeToExpiration, numSteps);
            var downProbability = 1.0m - upProbability;

            decimal[,] stockPrices = new decimal[numSteps + 1, numSteps + 1];
            decimal[,] optionPrices = new decimal[numSteps + 1, numSteps + 1];

            // Calculate stock prices at each node
            for (int i = 0; i <= numSteps; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    stockPrices[j, i] = spotPrice * Convert.ToDecimal(Math.Pow((double)upFactor, i - j) * Math.Pow((double)downFactor, j));
                }
            }

            // Calculate option prices at expiration
            for (int j = 0; j <= numSteps; j++)
            {
                optionPrices[j, numSteps] = CalculateOptionPayoff(stockPrices[j, numSteps], strikePrice, optionType);
            }

            // Calculate option prices at previous nodes using backward induction
            for (int i = numSteps - 1; i >= 0; i--)
            {
                for (int j = 0; j <= i; j++)
                {
                    optionPrices[j, i] = (upProbability * optionPrices[j, i + 1] + downProbability * optionPrices[j + 1, i + 1]) * DecimalMath(Math.Exp, -riskFreeRate * timeToExpiration / numSteps);
                }
            }

            return optionPrices[0, 0];
        }

        private static decimal CalculateUpFactor(decimal volatility, decimal timeToExpiration, int numSteps)
        {
            return DecimalMath(Math.Exp, volatility * DecimalMath(Math.Sqrt, timeToExpiration / numSteps));
        }

        private static decimal CalculateUpProbability(decimal riskFreeRate, decimal volatility, decimal timeToExpiration, int numSteps)
        {
            var upFactor = CalculateUpFactor(volatility, timeToExpiration, numSteps);
            var r = DecimalMath(Math.Exp, riskFreeRate * timeToExpiration / numSteps);
            return (r - (1.0m / upFactor)) / (upFactor - (1.0m / upFactor));
        }

        private static decimal CalculateOptionPayoff(decimal stockPrice, decimal strikePrice, OptionRight optionType)
        {
            if (optionType == OptionRight.Call)
            {
                return Math.Max(stockPrice - strikePrice, 0.0m);
            }
            else if (optionType == OptionRight.Put)
            {
                return Math.Max(strikePrice - stockPrice, 0.0m);
            }
            else
            {
                throw new ArgumentException("Invalid option right.");
            }
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
                                                   int maxIterations = 100)
        {
            var a = function(lowerBound) - referenceLevel;
            var b = function(upperBound) - referenceLevel;

            if (a * b >= 0m)
            {
                throw new Exception("Root is not bracketed");
            }

            if (Math.Abs(a) < Math.Abs(b))
            {
                var temp = a;
                a = b;
                b = temp;
            }

            var c = a;
            var d = c;
            var fS = decimal.MinValue;
            var mFlag = true;
            var iter = 0;

            while (fS != 0 || Math.Abs(b - a) > tolerance || iter > maxIterations)
            {
                var fA = function(a) - referenceLevel;
                var fB = function(b) - referenceLevel;
                var fC = function(c) - referenceLevel;

                decimal s;
                if (fA != fC && fB != fC)
                {
                    // inverse quadratic interpolation
                    s = a * fB * fC / (fA - fB) / (fA - fC) + b * fA * fC / (fB - fA) / (fB - fC) + c * fA * fB / (fC - fA) / (fC - fB);
                }
                else
                {
                    s = b - fB * (b - a) / (fB - fA);
                }

                if (s < (3 * a + b) / 4 || s > b ||
                    (mFlag && Math.Abs(s - b) >= Math.Abs(b - c) / 2) ||
                    (!mFlag && Math.Abs(s - b) >= Math.Abs(c - d) / 2) ||
                    (mFlag && Math.Abs(b - c) < tolerance) ||
                    (!mFlag && Math.Abs(c - d) < tolerance))
                {
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

            return b;
        }
    }
}
