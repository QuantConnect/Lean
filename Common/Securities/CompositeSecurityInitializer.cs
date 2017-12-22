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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="ISecurityInitializer"/> that executes
    /// each initializer in order
    /// </summary>
    public class CompositeSecurityInitializer : ISecurityInitializer
    {
        private readonly ISecurityInitializer[] _initializers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeSecurityInitializer"/> class
        /// </summary>
        /// <param name="initializers">The initializers to execute in order</param>
        public CompositeSecurityInitializer(params ISecurityInitializer[] initializers)
        {
            _initializers = initializers;
        }

        /// <summary>
        /// Execute each of the internally held initializers in sequence
        /// </summary>
        /// <param name="security">The security to be initialized</param>
        public void Initialize(Security security)
        {
            foreach (var initializer in _initializers)
            {
                initializer.Initialize(security);
            }
        }
    }
}