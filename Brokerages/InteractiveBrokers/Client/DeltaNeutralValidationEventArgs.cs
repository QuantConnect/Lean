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
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.DeltaNeutralValidation"/> event
    /// </summary>
    public sealed class DeltaNeutralValidationEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the data request.
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// Underlying component.
        /// </summary>
        public UnderComp UnderComp { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeltaNeutralValidationEventArgs"/> class
        /// </summary>
        public DeltaNeutralValidationEventArgs(int requestId, UnderComp underComp)
        {
            RequestId = requestId;
            UnderComp = underComp;
        }
    }
}