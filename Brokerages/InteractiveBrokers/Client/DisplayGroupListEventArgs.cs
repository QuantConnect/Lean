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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.DisplayGroupList"/> event
    /// </summary>
    public sealed class DisplayGroupListEventArgs : EventArgs
    {
        /// <summary>
        /// The requestId specified in queryDisplayGroups().
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// A list of integers representing visible group ID separated by the “|” character, and sorted by most used group first. 
        /// This list will not change during TWS session (in other words, user cannot add a new group; sorting can change though). 
        /// Example: "3|1|2"
        /// </summary>
        public string Groups { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayGroupListEventArgs"/> class
        /// </summary>
        public DisplayGroupListEventArgs(int reqId, string groups)
        {
            RequestId = reqId;
            Groups = groups;
        }
    }
}