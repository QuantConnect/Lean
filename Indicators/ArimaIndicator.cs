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
    /// An ARIMA is a time series model which can be used to describe a set of data. In particular,with Xₜ
    /// representing the series, the model assumes the data are of form (after differencing <see cref="_d" /> times):
    /// <para>
    ///     Xₜ = c + εₜ + ΣᵢφᵢXₜ₋ᵢ +  Σᵢθᵢεₜ₋ᵢ
    /// </para>
    /// where the first sum has an upper limit of <see cref="_p" /> and the second <see cref="_q" />.
    /// </summary>
    public class ArimaIndicator : TimeSeriesIndicator, IIndicatorWarmUpPeriodProvider

    {
        /// <summary>
        /// Differencing coefficient. Determines how many times the series should be differenced before fitting the
        /// model.
        /// </summary>
        private readonly int _d;

        private readonly bool _intercept;

        /// <summary>
        /// AR coefficient.
        /// </summary>
        private readonly int _p;

        /// <summary>
        /// MA Coefficient.
        /// </summary>
        private readonly int _q;

        private readonly RollingWindow<double> _rollingData;

        private List<double> _residuals;

        /// <summary>
        /// A dictionary, indexed by "AR" and "MA", containing their respective, fitted parameters.
        /// </summary>
        public Dictionary<string, double[]> Parameters;

        /// <summary>
        /// Fits an ARIMA(p,d,q) model of form (after differencing it <see cref="_d" /> times):
        /// <para>
        ///     Xₜ = c + εₜ + ΣᵢφᵢXₜ₋ᵢ +  Σᵢθᵢεₜ₋ᵢ
        /// </para>
        /// where the first sum has an upper limit of <see cref="_p" /> and the second <see cref="_q" />.
        /// This particular constructor fits the model by means of <see cref="TwoStepFit" /> for a specified name.
        /// </summary>
        /// <param name="name">The name of the indicator</param>
        /// <param name="p">AR order</param>
        /// <param name="d">Difference order</param>
        /// <param name="q">MA order</param>
        /// <param name="period">Size of the rolling series to fit onto</param>
        /// <param name="intercept">Whether ot not to include the intercept term</param>
        public ArimaIndicator(
            string name,
            int p,
            int d,
            int q,
            int period,
            bool intercept = true
            )
            : base(name)
        {
            if (period >= Math.Max(p, q))
            {
                _p = p;
                _q = q;
                _d = d;
                WarmUpPeriod = period;
                _rollingData = new RollingWindow<double>(period);
                _intercept = intercept;
            }
            else
            {
                throw new ArgumentException("Period must exceed both p and q");
            }
        }

        /// <summary>
        /// Fits an ARIMA(p,d,q) model of form (after differencing it <see cref="_d" /> times):
        /// <para>
        ///     Xₜ = c + εₜ + ΣᵢφᵢXₜ₋ᵢ +  Σᵢθᵢεₜ₋ᵢ
        /// </para>
        /// where the first sum has an upper limit of <see cref="_p" /> and the second <see cref="_q" />.
        /// This particular constructor fits the model by means of <see cref="TwoStepFit" /> by means of OLS.
        /// </summary>
        /// <param name="p">AR order</param>
        /// <param name="d">Difference order</param>
        /// <param name="q">MA order</param>
        /// <param name="period">Size of the rolling series to fit onto</param>
        public ArimaIndicator(int p, int d, int q, int period)
            : this($"ARIMA(({p},{d},{q}), {period})", p, d, q, period)
        {
            if (period >= Math.Max(p, q))
            {
                _p = p;
                _q = q;
                _d = d;
                WarmUpPeriod = period;
                _rollingData = new RollingWindow<double>(period);
            }
            else
            {
                throw new ArgumentException("Period must exceed both p and q");
            }
        }

        /// <summary>
        /// The variance of the residuals (Var(ε)) from the first step of <see cref="TwoStepFit" />.
        /// </summary>
        public double ArResidualError { get; private set; }

        /// <summary>
        /// The variance of the residuals (Var(ε)) from the second step of <see cref="TwoStepFit" />.
        /// </summary>
        public double MaResidualError { get; private set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _rollingData.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public new int WarmUpPeriod { get; }

        /// <summary>
        /// Fits the model by means of implementing the following pseudo-code algorithm (in the form of "if{then}"):
        /// <code>
        /// if d > 0 {Difference data D times}
        /// if p > 0 {Fit the AR model Xₜ = ΣᵢφᵢXₜ; ε's are set to residuals from fitting this.}
        /// if q > 0 {Fit the MA parameters left over  Xₜ = c + εₜ + ΣᵢφᵢXₜ₋ᵢ +  Σᵢθᵢεₜ₋ᵢ}
        /// Return: φ and θ estimates.
        /// </code>
        /// http://mbhauser.com/informal-notes/two-step-arma-estimation.pdf
        /// </summary>
        protected void TwoStepFit(double[] series) // Protected for any future inheritors (e.g., SARIMA)
        {
            _residuals = new List<double>();
            Parameters = new Dictionary<string, double[]>();
            var data = _d > 0 ? DifferenceSeries(_d, series) : series; // Difference the series
            double errAr = 0;
            double errMa = 0;
            double[] arFits;
            var lags = _p > 0 ? LaggedSeries(_p, data) : new[] {data};
            if (_p > 0)
            {
                // The function (lags[time][lagged X]) |---> ΣᵢφᵢXₜ₋ᵢ 
                arFits = Fit.MultiDim(lags, data.Skip(_p).ToArray(), method: DirectRegressionMethod.NormalEquations);
                var fittedVec = Vector.Build.Dense(arFits);

                for (var i = 0; i < data.Length; i++) // Calculate the error assoc. with model.
                {
                    if (i < _p)
                    {
                        _residuals.Add(0); // 0-padding
                        continue;
                    }

                    var residual = data[i] - Vector.Build.Dense(lags[i - _p]).DotProduct(fittedVec);
                    errAr += Math.Pow(residual, 2);
                    _residuals.Add(residual);
                }

                ArResidualError = errAr / (data.Length - _p - 1);
                if (_q == 0)
                {
                    Parameters["AR"] = arFits; // Will not be thrown out
                }
            }

            else // Xₜ is a sum of mean-zero terms.
            {
                _residuals = series.ToList();
            }

            if (_q > 0) // MA part as in (4) of mbhauser notes.
            {
                var size = Math.Max(_q, _p);
                var appendedData = new List<double[]>();
                var laggedErrors = LaggedSeries(size, _residuals.ToArray());
                for (var i = 0; i < laggedErrors.Length; i++)
                {
                    var doubles = lags[i].ToList();
                    doubles.AddRange(laggedErrors[i]);
                    appendedData.Add(doubles.ToArray());
                }

                var maFits = Fit.MultiDim(appendedData.ToArray(), data.Skip(_p).ToArray(),
                    method: DirectRegressionMethod.NormalEquations, intercept: _intercept);
                for (var i = size; i < data.Length; i++) // Calculate the error assoc. with model.
                {
                    var paramVector = _intercept
                        ? Vector.Build.Dense(maFits.Skip(1).ToArray())
                        : Vector.Build.Dense(maFits);
                    var residual = data[i] - Vector.Build.Dense(appendedData[i - size]).DotProduct(paramVector);
                    errMa += Math.Pow(residual, 2);
                }

                if (_intercept)
                {
                    MaResidualError = errMa / (data.Length - Math.Max(_p, _q) - 1);
                    Parameters["MA"] = maFits.Skip(1 + _q).ToArray();
                    Parameters["AR"] = maFits.Skip(1).Take(_q).ToArray();
                    Parameters["Intercept"] = new[] {maFits[0]};
                }
                else
                {
                    MaResidualError = errMa / (data.Length - Math.Max(_p, _q) - 1);
                    Parameters["MA"] = maFits.Skip(_q).ToArray();
                    Parameters["AR"] = maFits.Take(_q).ToArray();
                }
            }
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
            _rollingData.Add((double) input.Value);
            if (_rollingData.IsReady)
            {
                var arrayData = _rollingData.ToArray();
                TwoStepFit(arrayData);
                arrayData = _d > 0 ? DifferenceSeries(_d, arrayData) : arrayData;
                double summants = 0;
                if (_p > 0)
                {
                    for (var i = 0; i < _p; i++) // AR Parameters
                    {
                        summants += Parameters["AR"][i] * arrayData[i];
                    }
                }

                if (_q > 0)
                {
                    for (var i = 0; i < _q; i++) // MA Parameters
                    {
                        summants += Parameters["MA"][i] * _residuals[_q + i];
                    }

                    summants += Parameters.ContainsKey("Intercept") ? Parameters["Intercept"][0] : 0; // Intercept term
                }

                if (_d > 0)
                {
                    arrayData.ToList().Insert(0, summants); // Prepends
                    summants = InverseDifferencedSeries(arrayData).First(); // Returns disintegrated series
                }

                return (decimal) summants;
            }

            return 0m;
        }
    }
}
