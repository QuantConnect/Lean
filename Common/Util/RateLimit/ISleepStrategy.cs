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

namespace QuantConnect.Util.RateLimit
{
    /// <summary>
    /// Defines a strategy for sleeping the current thread of execution. This is currently used via the
    /// <see cref="ITokenBucket.Consume"/> in order to wait for new tokens to become available for consumption.
    /// </summary>
    public interface ISleepStrategy
    {
        /// <summary>
        /// Sleeps the current thread in an implementation specific way
        /// and for an implementation specific amount of time
        /// </summary>
        void Sleep();
    }
}