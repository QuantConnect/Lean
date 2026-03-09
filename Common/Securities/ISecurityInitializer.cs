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
    /// Represents a type capable of initializing a new security
    /// </summary>
    public interface ISecurityInitializer
    {
        /// <summary>
        /// Initializes the specified security
        /// </summary>
        /// <param name="security">The security to be initialized</param>
        void Initialize(Security security);
    }

    /// <summary>
    /// Provides static access to the <see cref="Null"/> security initializer
    /// </summary>
    public static class SecurityInitializer
    {
        /// <summary>
        /// Gets an implementation of <see cref="ISecurityInitializer"/> that is a no-op
        /// </summary>
        public static readonly ISecurityInitializer Null = new NullSecurityInitializer();

        private sealed class NullSecurityInitializer : ISecurityInitializer
        {
            public void Initialize(Security security) { }
        }
    }
}