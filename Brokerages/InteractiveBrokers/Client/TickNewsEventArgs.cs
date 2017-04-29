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
    /// Event arguments class for the <see cref="InteractiveBrokersClient.TickNews"/> event
    /// </summary>
    public class TickNewsEventArgs : EventArgs
    {
        /// <summary>
        /// The ticker id.
        /// </summary>
        public int TickerId { get; set; }

        /// <summary>
        /// The time stamp.
        /// </summary>
        public long TimeStamp { get; set; }

        /// <summary>
        /// The provider code.
        /// </summary>
        public string ProviderCode { get; set; }

        /// <summary>
        /// The article id.
        /// </summary>
        public string ArticleId { get; set; }

        /// <summary>
        /// The headline.
        /// </summary>
        public string Headline { get; set; }

        /// <summary>
        /// The extra data.
        /// </summary>
        public string ExtraData { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickNewsEventArgs"/> class
        /// </summary>
        public TickNewsEventArgs(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
        {
            TickerId = tickerId;
            TimeStamp = timeStamp;
            ProviderCode = providerCode;
            ArticleId = articleId;
            Headline = headline;
            ExtraData = extraData;
        }
    }
}
