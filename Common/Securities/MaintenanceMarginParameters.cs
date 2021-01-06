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
    /// Parameters for <see cref="IBuyingPowerModel.GetMaintenanceMargin"/>
    /// </summary>
    public class MaintenanceMarginParameters
    {
        /// <summary>
        /// Gets the security
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaintenanceMarginParameters"/> class
        /// </summary>
        /// <param name="security">The security</param>
        public MaintenanceMarginParameters(Security security)
        {
            Security = security;
        }

        /// <summary>
        /// Creates a new instance of <see cref="MaintenanceMarginParameters"/> for the security's underlying
        /// </summary>
        public MaintenanceMarginParameters ForUnderlying()
        {
            var derivative = Security as IDerivativeSecurity;
            if (derivative == null)
            {
                throw new InvalidOperationException("ForUnderlying is only invokable for IDerivativeSecurity (Option|Future)");
            }

            return new MaintenanceMarginParameters(derivative.Underlying);
        }
    }
}
