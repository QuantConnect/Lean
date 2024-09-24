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
    public class ModeledGreeks : Greeks
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
        /// Initializes a new default instance of the <see cref="ModeledGreeks"/> class
        /// </summary>
        public ModeledGreeks()
            : this(0m, 0m, 0m, 0m, 0m, 0m)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModeledGreeks"/> class
        /// </summary>
        public ModeledGreeks(decimal delta, decimal gamma, decimal vega, decimal theta, decimal rho, decimal lambda)
        {
            Delta = delta;
            Gamma = gamma;
            Vega = vega;
            Theta = theta;
            Rho = rho;
            Lambda = lambda;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ModeledGreeks"/> class
        /// </summary>
        public ModeledGreeks(Func<decimal> delta, Func<decimal> gamma, Func<decimal> vega, Func<decimal> theta, Func<decimal> rho, Func<decimal> lambda)
        {
            _delta = new Lazy<decimal>(delta);
            _gamma = new Lazy<decimal>(gamma);
            _vega = new Lazy<decimal>(vega);
            _theta = new Lazy<decimal>(theta);
            _rho = new Lazy<decimal>(rho);
            _lambda = new Lazy<decimal>(lambda);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ModeledGreeks"/> class
        /// </summary>
        public ModeledGreeks(Func<Tuple<decimal, decimal>> deltaGamma, Func<decimal> vega, Func<decimal> theta, Func<decimal> rho, Func<decimal> lambda)
        {
            _deltaGamma = new Lazy<Tuple<decimal, decimal>>(deltaGamma);
            _vega = new Lazy<decimal>(vega);
            _theta = new Lazy<decimal>(theta);
            _rho = new Lazy<decimal>(rho);
            _lambda = new Lazy<decimal>(lambda);
        }
    }
}
