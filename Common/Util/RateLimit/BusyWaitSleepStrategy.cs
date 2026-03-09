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

using System.Threading;

namespace QuantConnect.Util.RateLimit
{
    /// <summary>
    /// Provides a CPU intensive means of waiting for more tokens to be available in <see cref="ITokenBucket"/>.
    /// This strategy is only viable when the requested number of tokens is expected to become available in an
    /// extremely short period of time. This implementation aims to keep the current thread executing to prevent
    /// potential content switches arising from a thread yielding or sleeping strategy.
    /// </summary>
    public class BusyWaitSleepStrategy : ISleepStrategy
    {
        /// <summary>
        /// Provides a CPU intensive sleep by executing <see cref="Thread.SpinWait"/> for a single spin.
        /// </summary>
        public void Sleep()
        {
            Thread.SpinWait(1);
        }
    }
}