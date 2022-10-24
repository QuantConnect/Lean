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
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Event args for when a new data point is ready to be emitted
    /// </summary>
    public class NewDataAvailableEventArgs : EventArgs
    {
        /// <summary>
        /// The new data point
        /// </summary>
        public IBaseData DataPoint { get; set; }
    }
}
