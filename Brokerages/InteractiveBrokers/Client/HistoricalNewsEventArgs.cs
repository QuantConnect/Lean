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
    /// Event arguments class for the <see cref="InteractiveBrokersClient.HistoricalNews"/> event
    /// </summary>
    public class HistoricalNewsEventArgs : EventArgs
    {
        /// <summary>
        /// The request id.
        /// </summary>
        public int RequestId { get; set; }

        /// <summary>
        /// The time.
        /// </summary>
        public string Time { get; set; }

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
        /// Initializes a new instance of the <see cref="HistoricalNewsEventArgs"/> class
        /// </summary>
        public HistoricalNewsEventArgs(int requestId, string time, string providerCode, string articleId, string headline)
        {
            RequestId = requestId;
            Time = time;
            ProviderCode = providerCode;
            ArticleId = articleId;
            Headline = headline;
        }
    }
}
