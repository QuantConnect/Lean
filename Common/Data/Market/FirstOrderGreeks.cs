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
    /// Defines the first-order greeks
    /// </summary>
    /// <remarks>
    /// We can later add second and third order greeks as sub-classes
    /// </remarks>
    public class FirstOrderGreeks
    {
        /// <summary>
        /// Gets the delta.
        /// <para>
        /// Delta measures the rate of change of the option value with respect to changes in
        /// the underlying asset'sprice. (∂V/∂S)
        /// </para>
        /// </summary>
        public decimal Delta
        {
            get; private set;
        }

        /// <summary>
        /// Gets the vega.
        /// <para>
        /// Vega measures the rate of change of the option value with respect to changes in
        /// the underlying's volatility. (∂V/∂σ)
        /// </para>
        /// </summary>
        public decimal Vega
        {
            get; private set;
        }

        /// <summary>
        /// Gets the theta.
        /// <para>
        /// Theta measures the rate of change of the option value with respect to changes in
        /// time. This is commonly known as the 'time decay.' (∂V/∂τ)
        /// </para>
        /// </summary>
        public decimal Theta
        {
            get; private set;
        }

        /// <summary>
        /// Gets the rho.
        /// <para>
        /// Rho measures the rate of change of the option value with respect to changes in
        /// the risk free interest rate. (∂V/∂r)
        /// </para>
        /// </summary>
        public decimal Rho
        {
            get; private set;
        }

        /// <summary>
        /// Gets the lambda.
        /// <para>
        /// Lambda is the percentage change in option value per percentage change in the
        /// underlying's price, a measure of leverage. Sometimes referred to as gearing.
        /// (∂V/∂S ✕ S/V)
        /// </para>
        /// </summary>
        public decimal Lambda
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new default instance of the <see cref="FirstOrderGreeks"/> class
        /// </summary>
        public FirstOrderGreeks()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstOrderGreeks"/> class
        /// </summary>
        public FirstOrderGreeks(decimal delta, decimal vega, decimal theta, decimal rho, decimal lambda)
        {
            Delta = delta;
            Vega = vega;
            Theta = theta;
            Rho = rho;
            Lambda = lambda;
        }
    }
}