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
using System.Collections.Generic;
using System.Linq;
using Deedle;

namespace QuantConnect.Report
{
    /// <summary>
    /// Utility extension methods for Deedle series/frames
    /// </summary>
    public static class DeedleUtil
    {
        /// <summary>
        /// Calculates the cumulative sum for the given series
        /// </summary>
        /// <param name="input">Series to calculate cumulative sum for</param>
        /// <returns>Cumulative sum in series form</returns>
        public static Series<DateTime, double> CumulativeSum(this Series<DateTime, double> input)
        {
            if (input.IsEmpty)
            {
                return input;
            }

            var prev = 0.0;

            return input.SelectValues(current =>
            {
                var sum = prev + current;
                prev = sum;

                return sum;
            });
        }

        /// <summary>
        /// Calculates the cumulative product of the series. This is equal to the python pandas method: `df.cumprod()`
        /// </summary>
        /// <param name="input">Input series</param>
        /// <returns>Cumulative product</returns>
        public static Series<DateTime, double> CumulativeProduct(
            this Series<DateTime, double> input
        )
        {
            if (input.IsEmpty)
            {
                return input;
            }

            var prev = 1.0;

            return input.SelectValues(current =>
            {
                var product = prev * current;
                prev = product;

                return product;
            });
        }

        /// <summary>
        /// Calculates the cumulative max of the series. This is equal to the python pandas method: `df.cummax()`.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Series<DateTime, double> CumulativeMax(this Series<DateTime, double> input)
        {
            if (input.IsEmpty)
            {
                return input;
            }

            var prevMax = double.NegativeInfinity;
            var values = new List<double>();

            foreach (var point in input.Values)
            {
                if (point > prevMax)
                {
                    prevMax = point;
                }

                values.Add(prevMax);
            }

            return new Series<DateTime, double>(input.Keys, values);
        }

        /// <summary>
        /// Calculates the percentage change from the previous value to the current
        /// </summary>
        /// <param name="input">Series to calculate percentage change for</param>
        /// <returns>Percentage change in series form</returns>
        /// <remarks>Equivalent to `df.pct_change()`</remarks>
        public static Series<DateTime, double> PercentChange(this Series<DateTime, double> input)
        {
            if (input.IsEmpty)
            {
                return input;
            }

            var inputShifted = input.Shift(1);

            return (input - inputShifted) / inputShifted;
        }

        /// <summary>
        /// Calculates the cumulative returns series of the given input equity curve
        /// </summary>
        /// <param name="input">Equity curve series</param>
        /// <returns>Cumulative returns over time</returns>
        public static Series<DateTime, double> CumulativeReturns(
            this Series<DateTime, double> input
        )
        {
            if (input.IsEmpty)
            {
                return input;
            }

            return (
                    input.PercentChange().Where(kvp => !double.IsInfinity(kvp.Value)) + 1
                ).CumulativeProduct() - 1;
        }

        /// <summary>
        /// Calculates the total returns over a period of time for the given input
        /// </summary>
        /// <param name="input">Equity curve series</param>
        /// <returns>Total returns over time</returns>
        public static double TotalReturns(this Series<DateTime, double> input)
        {
            var returns = input.CumulativeReturns();

            if (returns.IsEmpty)
            {
                return double.NaN;
            }

            return returns.LastValue();
        }

        /// <summary>
        /// Drops sparse columns only if every value is `missing` in the column
        /// </summary>
        /// <typeparam name="TRowKey">Frame row key</typeparam>
        /// <typeparam name="TColumnKey">Frame column key</typeparam>
        /// <param name="frame">Data Frame</param>
        /// <returns>new Frame with sparse columns dropped</returns>
        /// <remarks>Equivalent to `df.dropna(axis=1, how='all')`</remarks>
        public static Frame<TRowKey, TColumnKey> DropSparseColumnsAll<TRowKey, TColumnKey>(
            this Frame<TRowKey, TColumnKey> frame
        )
        {
            var newFrame = frame.Clone();

            foreach (var key in frame.ColumnKeys)
            {
                if (newFrame[key].DropMissing().ValueCount == 0)
                {
                    newFrame.DropColumn(key);
                }
            }

            return newFrame;
        }

        /// <summary>
        /// Drops sparse rows if and only if every value is `missing` in the Frame
        /// </summary>
        /// <typeparam name="TRowKey">Frame row key</typeparam>
        /// <typeparam name="TColumnKey">Frame column key</typeparam>
        /// <param name="frame">Data Frame</param>
        /// <returns>new Frame with sparse rows dropped</returns>
        /// <remarks>Equivalent to `df.dropna(how='all')`</remarks>
        public static Frame<TRowKey, TColumnKey> DropSparseRowsAll<TRowKey, TColumnKey>(
            this Frame<TRowKey, TColumnKey> frame
        )
        {
            if (frame.ColumnKeys.Count() == 0)
            {
                return Frame.CreateEmpty<TRowKey, TColumnKey>();
            }

            var newFrame = frame.Clone().Transpose();

            foreach (var key in frame.RowKeys)
            {
                if (newFrame[key].DropMissing().ValueCount == 0)
                {
                    newFrame.DropColumn(key);
                }
            }

            return newFrame.Transpose();
        }
    }
}
