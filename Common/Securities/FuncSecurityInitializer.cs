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
 *
*/

using System;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides a functional implementation of <see cref="ISecurityInitializer"/>
    /// </summary>
    public class FuncSecurityInitializer : ISecurityInitializer
    {
        private readonly Action<Security> _initializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncSecurityInitializer"/> class
        /// </summary>
        /// <param name="initializer">The functional implementation of <see cref="ISecurityInitializer.Initialize"/></param>
        public FuncSecurityInitializer(Action<Security> initializer)
        {
            _initializer = initializer;
        }

        /// <summary>
        /// Initializes the specified security
        /// </summary>
        /// <param name="security">The security to be initialized</param>
        public void Initialize(Security security)
        {
            _initializer(security);
        }
    }
}
