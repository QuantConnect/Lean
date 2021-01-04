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

using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    ///     The base class for any Time Series-type indicator, containing methods common to most of such models.
    /// </summary>
    public abstract class TimeSeriesIndicator : IndicatorBase<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        private double[] _diffHeads;

        /// <summary>
        ///     A constructor for a basic Time Series indicator.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        protected TimeSeriesIndicator(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady { get; }

        /// <summary>
        ///     Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        ///     Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            return 0m;
        }


        /// <summary>
        ///     Differences a time series d times.
        /// </summary>
        /// <param name="series">Series to difference</param>
        /// <param name="d">The differencing order</param>
        protected double[]
            DifferenceSeries(int d, double[] series)
        {
            _diffHeads = new double[series.Length - d];
            if (d == 0)
            {
                return null;
            }

            var localSeries = series;
            for (var j = 1; j <= d; j++)
            {
                var result = new double[localSeries.Length - 1];
                _diffHeads[j - 1] = localSeries.Last();

                for (var i = 1; i <= localSeries.Length - 1; i++)
                {
                    result[i - 1] = localSeries[i] - localSeries[i - 1];
                }

                localSeries = result;
            }

            return localSeries;
        }

        /// <summary>
        ///     Returns a series where each spot is taken by the cumulative sum of all points up to and including
        ///     the value at that spot in the original series.
        /// </summary>
        /// <param name="series">Series to cumulatively sum over.</param>
        /// <returns>Cumulatively summed series.</returns>
        protected double[] CumulativeSum(double[] series)
        {
            var sums = 0d;
            var outSeries = new double[series.Length];
            for (var i = 0; i < series.Length; i++)
            {
                sums += series[i];
                outSeries[i] = sums;
            }

            return outSeries;
        }


        /// <summary>
        ///     Undoes the differencing of a time series which has been differenced using <see cref="DifferenceSeries" />..
        ///     https://github.com/statsmodels/statsmodels/blob/04f00006a7aeb1c93d6894caa420698400da6c33/statsmodels/tsa/tsatools.py#L758
        /// </summary>
        /// <param name="series">Series to un-difference</param>
        protected double[]
            InverseDifferencedSeries(double[] series)
        {
            if (_diffHeads == null)
            {
                return null;
            }

            var diffHeads = _diffHeads;
            while (diffHeads.Length > 0)
            {
                var first = diffHeads.Last();
                diffHeads = diffHeads.Take(diffHeads.Length - 1) as double[]; // removes from original
                series[series.Length + 1] = first;
                series = CumulativeSum(series);
            }

            return series;
        }

        /// <summary>
        ///     Returns an array of lagged series for each of {1,...,p} lags.
        /// </summary>
        /// <param name="p">Max lag order</param>
        /// <param name="series">Series to calculate the lags of</param>
        /// <param name="includeT">Whether or not to include t with its lags in the output array</param>
        /// <returns>A list such that index i returns the series for i+1 lags</returns>
        protected double[][] LaggedSeries(int p, double[] series, bool includeT = false)
        {
            // P-defined lagging - for each X_t, return double[] of the relevant lagged terms
            var toArray = new List<double[]>();
            for (var t = p; t < series.Length; t++)
            {
                var localLag = new List<double>();
                for (var j = includeT ? 0 : 1; j <= p; j++)
                {
                    localLag.Add(series[t - j]);
                }

                toArray.Add(localLag.ToArray());
            }

            return toArray.ToArray();
        }
    }
}
