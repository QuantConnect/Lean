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
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearRegression;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// An Autoregressive Intergrated Moving Average (ARIMA) is a time series model which can be used to describe a set of data.
    /// In particular,with Xₜ representing the series, the model assumes the data are of form
    /// (after differencing <see cref="_diffOrder" /> times):
    /// <para>
    ///     Xₜ = c + εₜ + ΣᵢφᵢXₜ₋ᵢ +  Σᵢθᵢεₜ₋ᵢ
    /// </para>
    /// where the first sum has an upper limit of <see cref="_arOrder" /> and the second <see cref="_maOrder" />.
    /// </summary>
    public class AutoRegressiveIntegratedMovingAverage : TimeSeriesIndicator
    {
        private List<double> _residuals;
        private readonly bool _intercept;
        private readonly RollingWindow<double> _rollingData;

        /// <summary>
        /// Differencing coefficient (d). Determines how many times the series should be differenced before fitting the
        /// model.
        /// </summary>
        private readonly int _diffOrder;

        /// <summary>
        /// AR coefficient -- p
        /// </summary>
        private readonly int _arOrder;

        /// <summary>
        /// MA Coefficient -- q
        /// </summary>
        private readonly int _maOrder;

        /// <summary>
        /// Fitted AR parameters (φ terms).
        /// </summary>
        public double[] ArParameters;

        /// <summary>
        /// Fitted MA parameters (θ terms).
        /// </summary>
        public double[] MaParameters;

        /// <summary>
        /// Fitted intercept (c term).
        /// </summary>
        public double Intercept;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _rollingData.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public override int WarmUpPeriod { get; }

        /// <summary>
        /// The variance of the residuals (Var(ε)) from the first step of <see cref="TwoStepFit" />.
        /// </summary>
        public double ArResidualError { get; private set; }

        /// <summary>
        /// The variance of the residuals (Var(ε)) from the second step of <see cref="TwoStepFit" />.
        /// </summary>
        public double MaResidualError { get; private set; }

        /// <summary>
        /// Fits an ARIMA(arOrder,diffOrder,maOrder) model of form (after differencing it <see cref="_diffOrder" /> times):
        /// <para>
        ///     Xₜ = c + εₜ + ΣᵢφᵢXₜ₋ᵢ +  Σᵢθᵢεₜ₋ᵢ
        /// </para>
        /// where the first sum has an upper limit of <see cref="_arOrder" /> and the second <see cref="_maOrder" />.
        /// This particular constructor fits the model by means of <see cref="TwoStepFit" /> for a specified name.
        /// </summary>
        /// <param name="name">The name of the indicator</param>
        /// <param name="arOrder">AR order (p) -- defines the number of past values to consider in the AR component of the model.</param>
        /// <param name="diffOrder">Difference order (d) -- defines how many times to difference the model before fitting parameters.</param>
        /// <param name="maOrder">MA order -- defines the number of past values to consider in the MA component of the model.</param>
        /// <param name="period">Size of the rolling series to fit onto</param>
        /// <param name="intercept">Whether ot not to include the intercept term</param>
        public AutoRegressiveIntegratedMovingAverage(
            string name,
            int arOrder,
            int diffOrder,
            int maOrder,
            int period,
            bool intercept = true
            )
            : base(name)
        {
            if (arOrder < 0 || maOrder < 0)
            {
                throw new ArgumentException("AR/MA orders cannot be negative.");
            }

            if (arOrder == 0)
            {
                throw new ArgumentException("arOrder (p) must be greater than zero for all " +
                    "currently available fitting methods.");
            }

            if (period < Math.Max(arOrder, maOrder))
            {
                throw new ArgumentException("Period must exceed both arOrder and maOrder");
            }

            _arOrder = arOrder;
            _maOrder = maOrder;
            _diffOrder = diffOrder;
            WarmUpPeriod = period;
            _rollingData = new RollingWindow<double>(period);
            _intercept = intercept;
        }

        /// <summary>
        /// Fits an ARIMA(arOrder,diffOrder,maOrder) model of form (after differencing it <see cref="_diffOrder" /> times):
        /// <para>
        ///     Xₜ = c + εₜ + ΣᵢφᵢXₜ₋ᵢ +  Σᵢθᵢεₜ₋ᵢ
        /// </para>
        /// where the first sum has an upper limit of <see cref="_arOrder" /> and the second <see cref="_maOrder" />.
        /// This particular constructor fits the model by means of <see cref="TwoStepFit" /> using ordinary least squares.
        /// </summary>
        /// <param name="arOrder">AR order (p) -- defines the number of past values to consider in the AR component of the model.</param>
        /// <param name="diffOrder">Difference order (d) -- defines how many times to difference the model before fitting parameters.</param>
        /// <param name="maOrder">MA order (q) -- defines the number of past values to consider in the MA component of the model.</param>
        /// <param name="period">Size of the rolling series to fit onto</param>
        /// <param name="intercept">Whether to include an intercept term (c)</param>
        public AutoRegressiveIntegratedMovingAverage(
            int arOrder,
            int diffOrder,
            int maOrder,
            int period,
            bool intercept
            )
            : this($"ARIMA(({arOrder}, {diffOrder}, {maOrder}), {period}, {intercept})", arOrder, diffOrder, maOrder,
                period, intercept)
        {
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _rollingData.Reset();
        }

        /// <summary>
        /// Forecasts the series of the fitted model one point ahead.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _rollingData.Add((double)input.Value);
            if (_rollingData.IsReady)
            {
                var arrayData = _rollingData.ToArray();
                arrayData = _diffOrder > 0 ? DifferenceSeries(_diffOrder, arrayData, out _diffHeads) : arrayData;
                TwoStepFit(arrayData);
                double summants = 0;
                if (_arOrder > 0)
                {
                    for (var i = 0; i < _arOrder; i++) // AR Parameters
                    {
                        summants += ArParameters[i] * arrayData[i];
                    }
                }

                if (_maOrder > 0)
                {
                    for (var i = 0; i < _maOrder; i++) // MA Parameters
                    {
                        summants += MaParameters[i] * _residuals[_maOrder + i + 1];
                    }
                }

                summants += Intercept; // By default equals 0

                if (_diffOrder > 0)
                {
                    var dataCast = arrayData.ToList();
                    dataCast.Insert(0, summants); // Prepends
                    summants = InverseDifferencedSeries(dataCast.ToArray(), _diffHeads).First(); // Returns disintegrated series
                }

                return (decimal)summants;
            }

            return 0m;
        }

        /// <summary>
        /// Fits the model by means of implementing the following pseudo-code algorithm (in the form of "if{then}"):
        /// <code>
        /// if diffOrder > 0 {Difference data diffOrder times}
        /// if arOrder > 0 {Fit the AR model Xₜ = ΣᵢφᵢXₜ; ε's are set to residuals from fitting this.}
        /// if maOrder > 0 {Fit the MA parameters left over  Xₜ = c + εₜ + ΣᵢφᵢXₜ₋ᵢ +  Σᵢθᵢεₜ₋ᵢ}
        /// Return: φ and θ estimates.
        /// </code>
        /// http://mbhauser.com/informal-notes/two-step-arma-estimation.pdf
        /// </summary>
        private void TwoStepFit(double[] series) // Protected for any future inheritors (e.g., SARIMA)
        {
            _residuals = new List<double>();
            double errorAr = 0;
            double errorMa = 0;
            var lags = _arOrder > 0 ? LaggedSeries(_arOrder, series) : new[] {series};

            AutoRegressiveStep(lags, series, errorAr);

            if (_maOrder <= 0)
            {
                return;
            }

            MovingAverageStep(lags, series, errorMa);
        }

        /// <summary>
        /// Fits the moving average component in the <see cref="TwoStepFit"/> method.
        /// </summary>
        /// <param name="lags">An array of lagged data (<see cref="TimeSeriesIndicator.LaggedSeries"/>).</param>
        /// <param name="data">The input series, differenced <see cref="_diffOrder"/> times.</param>
        /// <param name="errorMa">The summed residuals (by default 0) associated with the MA component.</param>
        private void MovingAverageStep(double[][] lags, double[] data, double errorMa)
        {
            var appendedData = new List<double[]>();
            var laggedErrors = LaggedSeries(_maOrder, _residuals.ToArray());
            for (var i = 0; i < laggedErrors.Length; i++)
            {
                var doubles = lags[i].ToList();
                doubles.AddRange(laggedErrors[i]);
                appendedData.Add(doubles.ToArray());
            }

            var maFits = Fit.MultiDim(appendedData.ToArray(), data.Skip(_maOrder).ToArray(),
                method: DirectRegressionMethod.NormalEquations, intercept: _intercept);
            for (var i = _maOrder; i < data.Length; i++) // Calculate the error assoc. with model.
            {
                var paramVector = _intercept
                    ? Vector.Build.Dense(maFits.Skip(1).ToArray())
                    : Vector.Build.Dense(maFits);
                var residual = data[i] - Vector.Build.Dense(appendedData[i - _maOrder]).DotProduct(paramVector);
                errorMa += Math.Pow(residual, 2);
            }

            switch (_intercept)
            {
                case true:
                    MaResidualError = errorMa / (data.Length - Math.Max(_arOrder, _maOrder) - 1);
                    MaParameters = maFits.Skip(1 + _arOrder).ToArray();
                    ArParameters = maFits.Skip(1).Take(_arOrder).ToArray();
                    Intercept = maFits[0];
                    break;
                default:
                    MaResidualError = errorMa / (data.Length - Math.Max(_arOrder, _maOrder) - 1);
                    MaParameters = maFits.Skip(_arOrder).ToArray();
                    ArParameters = maFits.Take(_arOrder).ToArray();
                    break;
            }
        }

        /// <summary>
        /// Fits the autoregressive component in the <see cref="TwoStepFit"/> method.
        /// </summary>
        /// <param name="lags">An array of lagged data (<see cref="TimeSeriesIndicator.LaggedSeries"/>).</param>
        /// <param name="data">The input series, differenced <see cref="_diffOrder"/> times.</param>
        /// <param name="errorAr">The summed residuals (by default 0) associated with the AR component.</param>
        private void AutoRegressiveStep(double[][] lags, double[] data, double errorAr)
        {
            double[] arFits;
            // The function (lags[time][lagged X]) |---> ΣᵢφᵢXₜ₋ᵢ 
            arFits = Fit.MultiDim(lags, data.Skip(_arOrder).ToArray(),
                method: DirectRegressionMethod.NormalEquations);
            var fittedVec = Vector.Build.Dense(arFits);

            for (var i = 0; i < data.Length; i++) // Calculate the error assoc. with model.
            {
                if (i < _arOrder)
                {
                    _residuals.Add(0); // 0-padding
                    continue;
                }

                var residual = data[i] - Vector.Build.Dense(lags[i - _arOrder]).DotProduct(fittedVec);
                errorAr += Math.Pow(residual, 2);
                _residuals.Add(residual);
            }

            ArResidualError = errorAr / (data.Length - _arOrder - 1);
            if (_maOrder == 0)
            {
                ArParameters = arFits; // Will not be thrown out
            }
        }
    }
}
