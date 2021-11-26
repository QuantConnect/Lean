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

using QuantConnect.Interfaces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Event arguments class for the <see cref="IBrokerage.DelistingNotification"/> event
    /// </summary>
    public class DelistingNotificationEventArgs
    {
        /// <summary>
        /// Gets the option symbol which has received a notification
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelistingNotificationEventArgs"/> class
        /// </summary>
        /// <param name="symbol">The symbol</param>
        public DelistingNotificationEventArgs(Symbol symbol)
        {
            Symbol = symbol;
        }
    }
}
