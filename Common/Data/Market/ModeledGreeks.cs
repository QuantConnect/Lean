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
    internal class ModeledGreeks : Greeks
    {
        private Lazy<decimal> _delta;
        private Lazy<decimal> _gamma;
        private Lazy<decimal> _vega;
        private Lazy<decimal> _theta;
        private Lazy<decimal> _rho;
        private Lazy<decimal> _lambda;

        /// <summary>
        /// Gets the delta
        /// </summary>
        public override decimal Delta => _delta.Value;

        /// <summary>
        /// Gets the gamma
        /// </summary>
        public override decimal Gamma => _gamma.Value;

        /// <summary>
        /// Gets the vega
        /// </summary>
        public override decimal Vega => _vega.Value;

        /// <summary>
        /// Gets the theta
        /// </summary>
        public override decimal Theta => _theta.Value;

        /// <summary>
        /// Gets the rho
        /// </summary>
        public override decimal Rho => _rho.Value;

        /// <summary>
        /// Gets the lambda
        /// </summary>
        public override decimal Lambda => _lambda.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModeledGreeks"/> class
        /// </summary>
        public ModeledGreeks(Func<decimal> delta, Func<decimal> gamma, Func<decimal> vega, Func<decimal> theta, Func<decimal> rho, Func<decimal> lambda)
        {
            _delta = new Lazy<decimal>(delta, isThreadSafe: false);
            _gamma = new Lazy<decimal>(gamma, isThreadSafe: false);
            _vega = new Lazy<decimal>(vega, isThreadSafe: false);
            _theta = new Lazy<decimal>(theta, isThreadSafe: false);
            _rho = new Lazy<decimal>(rho, isThreadSafe: false);
            _lambda = new Lazy<decimal>(lambda, isThreadSafe: false);
        }
    }
}
