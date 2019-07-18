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

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Reduced interface which exposes required <see cref="ITimeProvider"/> for <see cref="IDataFeed"/> implementations
    /// </summary>
    public interface IDataFeedTimeProvider
    {
        /// <summary>
        /// Continuous UTC time provider
        /// </summary>
        ITimeProvider TimeProvider { get; }

        /// <summary>
        /// Time provider which returns current UTC frontier time
        /// </summary>
        ITimeProvider FrontierTimeProvider { get; }
    }
}
