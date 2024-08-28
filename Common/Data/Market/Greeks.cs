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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Defines the greeks
    /// </summary>
    public class BaseGreeks
    {
        /// <summary>
        /// Gets the delta.
        /// <para>
        /// Delta measures the rate of change of the option value with respect to changes in
        /// the underlying asset'sprice. (∂V/∂S)
        /// </para>
        /// </summary>
        public virtual decimal Delta { get; protected set; }

        /// <summary>
        /// Gets the gamma.
        /// <para>
        /// Gamma measures the rate of change of Delta with respect to changes in
        /// the underlying asset'sprice. (∂²V/∂S²)
        /// </para>
        /// </summary>
        public virtual decimal Gamma { get; protected set; }

        /// <summary>
        /// Gets the vega.
        /// <para>
        /// Vega measures the rate of change of the option value with respect to changes in
        /// the underlying's volatility. (∂V/∂σ)
        /// </para>
        /// </summary>
        public virtual decimal Vega { get; protected set; }

        /// <summary>
        /// Gets the theta.
        /// <para>
        /// Theta measures the rate of change of the option value with respect to changes in
        /// time. This is commonly known as the 'time decay.' (∂V/∂τ)
        /// </para>
        /// </summary>
        public virtual decimal Theta { get; protected set; }

        /// <summary>
        /// Gets the rho.
        /// <para>
        /// Rho measures the rate of change of the option value with respect to changes in
        /// the risk free interest rate. (∂V/∂r)
        /// </para>
        /// </summary>
        public virtual decimal Rho { get; protected set; }

        /// <summary>
        /// Gets the lambda.
        /// <para>
        /// Lambda is the percentage change in option value per percentage change in the
        /// underlying's price, a measure of leverage. Sometimes referred to as gearing.
        /// (∂V/∂S ✕ S/V)
        /// </para>
        /// </summary>
        public virtual decimal Lambda { get; protected set; }

        /// <summary>
        /// Gets the lambda.
        /// <para>
        /// Lambda is the percentage change in option value per percentage change in the
        /// underlying's price, a measure of leverage. Sometimes referred to as gearing.
        /// (∂V/∂S ✕ S/V)
        /// </para>
        /// </summary>
        /// <remarks>
        /// Alias for <see cref="Lambda"/> required for compatibility with Python when
        /// PEP8 API is used (lambda is a reserved keyword in Python).
        /// </remarks>
        public decimal Lambda_ => Lambda;

        /// <summary>
        /// Gets the theta per day.
        /// <para>
        /// Theta measures the rate of change of the option value with respect to changes in
        /// time. This is commonly known as the 'time decay.' (∂V/∂τ)
        /// </para>
        /// </summary>
        public decimal ThetaPerDay => Theta / 365m;

        /// <summary>
        /// Initializes a new default instance of the <see cref="BaseGreeks"/> class
        /// </summary>
        public BaseGreeks()
            : this(0m, 0m, 0m, 0m, 0m, 0m)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGreeks"/> class
        /// </summary>
        public BaseGreeks(decimal delta, decimal gamma, decimal vega, decimal theta, decimal rho, decimal lambda)
        {
            Delta = delta;
            Gamma = gamma;
            Vega = vega;
            Theta = theta;
            Rho = rho;
            Lambda = lambda;
        }
    }

    /// <summary>
    /// Defines the greeks
    /// </summary>
    public class Greeks : BaseGreeks
    {
        private Lazy<decimal> _delta;
        private Lazy<decimal> _gamma;
        private Lazy<decimal> _vega;
        private Lazy<decimal> _theta;
        private Lazy<decimal> _rho;
        private Lazy<decimal> _lambda;

        // _deltagamma stores gamma and delta combined and is done
        // for optimization purposes (approximation of delta and gamma is very similar)
        private Lazy<Tuple<decimal, decimal>> _deltaGamma;

        /// <inheritdoc />
        public override decimal Delta
        {
            get
            {
                return _delta != null ? _delta.Value : _deltaGamma.Value.Item1;
            }
            protected set
            {
                _delta = new Lazy<decimal>(() => value);
            }
        }

        /// <inheritdoc />
        public override decimal Gamma
        {
            get
            {
                return _gamma != null ? _gamma.Value : _deltaGamma.Value.Item2;
            }
            protected set
            {
                _gamma = new Lazy<decimal>(() => value);
            }
        }

        /// <inheritdoc />
        public override decimal Vega
        {
            get
            {
                return _vega.Value;
            }
            protected set
            {
                _vega = new Lazy<decimal>(() => value);
            }
        }

        /// <inheritdoc />
        public override decimal Theta
        {
            get
            {
                return _theta.Value;
            }
            protected set
            {
                _theta = new Lazy<decimal>(() => value);
            }
        }

        /// <inheritdoc />
        public override decimal Rho
        {
            get
            {
                return _rho.Value;
            }
            protected set
            {
                _rho = new Lazy<decimal>(() => value);
            }
        }

        /// <inheritdoc />
        public override decimal Lambda
        {
            get
            {
                return _lambda.Value;
            }
            protected set
            {
                _lambda = new Lazy<decimal>(() => value);
            }
        }

        /// <summary>
        /// Initializes a new default instance of the <see cref="Greeks"/> class
        /// </summary>
        public Greeks() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Greeks"/> class
        /// </summary>
        public Greeks(decimal delta, decimal gamma, decimal vega, decimal theta, decimal rho, decimal lambda)
        {
            Delta = delta;
            Gamma = gamma;
            Vega = vega;
            Theta = theta;
            Rho = rho;
            Lambda = lambda;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Greeks"/> class
        /// </summary>
        public Greeks(Func<decimal> delta, Func<decimal> gamma, Func<decimal> vega, Func<decimal> theta, Func<decimal> rho, Func<decimal> lambda)
        {
            _delta = new Lazy<decimal>(delta);
            _gamma = new Lazy<decimal>(gamma);
            _vega = new Lazy<decimal>(vega);
            _theta = new Lazy<decimal>(theta);
            _rho = new Lazy<decimal>(rho);
            _lambda = new Lazy<decimal>(lambda);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Greeks"/> class
        /// </summary>
        public Greeks(Func<Tuple<decimal, decimal>> deltaGamma, Func<decimal> vega, Func<decimal> theta, Func<decimal> rho, Func<decimal> lambda)
        {
            _deltaGamma = new Lazy<Tuple<decimal, decimal>>(deltaGamma);
            _vega = new Lazy<decimal>(vega);
            _theta = new Lazy<decimal>(theta);
            _rho = new Lazy<decimal>(rho);
            _lambda = new Lazy<decimal>(lambda);
        }
    }
}
