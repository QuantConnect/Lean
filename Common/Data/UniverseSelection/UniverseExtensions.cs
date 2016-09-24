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

using System.Linq;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Provides extension methods for the <see cref="Universe"/> class
    /// </summary>
    public static class UniverseExtensions
    {
        /// <summary>
        /// Creates a new universe that logically is the result of wiring the two universes together such that
        /// the first will produce subscriptions for the second and the second will only select on data that has
        /// passed the first.
        /// 
        /// NOTE: The <paramref name="first"/> and <paramref name="second"/> universe instances provided
        /// to this method should not be manually added to the algorithm.
        /// </summary>
        /// <param name="first">The first universe in this 'chain'</param>
        /// <param name="second">The second universe in this 'chain'</param>
        /// <param name="configurationPerSymbol">True if each symbol as its own configuration, false otherwise</param>
        /// <returns>A new universe that can be added to the algorithm that represents invoking the first universe
        /// and then the second universe using the outputs of the first. </returns>
        public static Universe ChainedTo(this Universe first, Universe second, bool configurationPerSymbol)
        {
            var prefilteredSecond = second.PrefilterUsing(first);
            return new GetSubscriptionRequestsUniverseDecorator(first, (security, currentTimeUtc, maximumEndTimeUtc) =>
            {
                return first.GetSubscriptionRequests(security, currentTimeUtc, maximumEndTimeUtc).Select(request => new SubscriptionRequest(
                    template: request,
                    isUniverseSubscription: true,
                    universe: prefilteredSecond,
                    security: security,
                    configuration: configurationPerSymbol ? new SubscriptionDataConfig(prefilteredSecond.Configuration, symbol: security.Symbol) : prefilteredSecond.Configuration,
                    startTimeUtc: currentTimeUtc - prefilteredSecond.Configuration.Resolution.ToTimeSpan(),
                    endTimeUtc: currentTimeUtc.AddSeconds(-1)
                    ));
            });
        }

        /// <summary>
        /// Creates a new universe that restricts the universe selection data to symbols that passed the
        /// first universe's selection critera
        /// 
        /// NOTE: The <paramref name="second"/> universe instance provided to this method should not be manually
        /// added to the algorithm. The <paramref name="first"/> should still be manually (assuming no other changes).
        /// </summary>
        /// <param name="second">The universe to be filtere</param>
        /// <param name="first">The universe providing the set of symbols used for filtered</param>
        /// <returns>A new universe that can be added to the algorithm that represents invoking the second
        /// using the selections from the first as a filter.</returns>
        public static Universe PrefilterUsing(this Universe second, Universe first)
        {
            return new SelectSymbolsUniverseDecorator(second, (utcTime, data) =>
            {
                var clone = (BaseDataCollection)data.Clone();
                clone.Data = clone.Data.Where(d => first.ContainsMember(d.Symbol)).ToList();
                return second.SelectSymbols(utcTime, clone);
            });
        }
    }
}