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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Parameters for <see cref="IBuyingPowerModel.GetInitialMarginRequirement"/>
    /// </summary>
    public class InitialMarginParameters
    {
        /// <summary>
        /// Gets the security
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Gets the quantity
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitialMarginParameters"/> class
        /// </summary>
        /// <param name="security">The security</param>
        /// <param name="quantity">The quantity</param>
        public InitialMarginParameters(Security security, decimal quantity)
        {
            Security = security;
            Quantity = quantity;
        }

        /// <summary>
        /// Creates a new instance of <see cref="InitialMarginParameters"/> for the security's underlying
        /// </summary>
        public InitialMarginParameters ForUnderlying()
        {
            var derivative = Security as IDerivativeSecurity;
            if (derivative == null)
            {
                throw new InvalidOperationException(
                    Messages
                        .InitialMarginParameters
                        .ForUnderlyingOnlyInvokableForIDerivativeSecurity
                );
            }

            return new InitialMarginParameters(derivative.Underlying, Quantity);
        }
    }
}
