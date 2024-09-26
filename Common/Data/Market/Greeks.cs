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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Defines the greeks
    /// </summary>
    public abstract class Greeks
    {
        /// <summary>
        /// Gets the delta.
        /// <para>
        /// Delta measures the rate of change of the option value with respect to changes in
        /// the underlying asset'sprice. (∂V/∂S)
        /// </para>
        /// </summary>
        public abstract decimal Delta { get; }

        /// <summary>
        /// Gets the gamma.
        /// <para>
        /// Gamma measures the rate of change of Delta with respect to changes in
        /// the underlying asset'sprice. (∂²V/∂S²)
        /// </para>
        /// </summary>
        public abstract decimal Gamma { get; }

        /// <summary>
        /// Gets the vega.
        /// <para>
        /// Vega measures the rate of change of the option value with respect to changes in
        /// the underlying's volatility. (∂V/∂σ)
        /// </para>
        /// </summary>
        public abstract decimal Vega { get; }

        /// <summary>
        /// Gets the theta.
        /// <para>
        /// Theta measures the rate of change of the option value with respect to changes in
        /// time. This is commonly known as the 'time decay.' (∂V/∂τ)
        /// </para>
        /// </summary>
        public abstract decimal Theta { get; }

        /// <summary>
        /// Gets the rho.
        /// <para>
        /// Rho measures the rate of change of the option value with respect to changes in
        /// the risk free interest rate. (∂V/∂r)
        /// </para>
        /// </summary>
        public abstract decimal Rho { get; }

        /// <summary>
        /// Gets the lambda.
        /// <para>
        /// Lambda is the percentage change in option value per percentage change in the
        /// underlying's price, a measure of leverage. Sometimes referred to as gearing.
        /// (∂V/∂S ✕ S/V)
        /// </para>
        /// </summary>
        public abstract decimal Lambda { get; }

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
        public virtual decimal Lambda_ => Lambda;

        /// <summary>
        /// Gets the theta per day.
        /// <para>
        /// Theta measures the rate of change of the option value with respect to changes in
        /// time. This is commonly known as the 'time decay.' (∂V/∂τ)
        /// </para>
        /// </summary>
        public virtual decimal ThetaPerDay => Theta / 365m;
    }
}
