/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2023 QuantConnect Corporation.
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
 *
*/

using Python.Runtime;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Simple base class shared by top layer fundamental properties which depend on a time provider
    /// </summary>
    public abstract class FundamentalTimeDependentProperty : ReusuableCLRObject
    {
        /// <summary>
        /// The time provider instance to use
        /// </summary>
        protected ITimeProvider _timeProvider { get; }

        /// <summary>
        /// The SID instance to use
        /// </summary>
        protected SecurityIdentifier _securityIdentifier { get; }

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public FundamentalTimeDependentProperty(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier)
        {
            _timeProvider = timeProvider;
            _securityIdentifier = securityIdentifier;
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        public abstract FundamentalTimeDependentProperty Clone(ITimeProvider timeProvider);
    }
}
