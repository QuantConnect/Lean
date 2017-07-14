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
    /// Event arguments class for the <see cref="InteractiveBrokersClient.NewsArticle"/> event
    /// </summary>
    public class NewsArticleEventArgs : EventArgs
    {
        /// <summary>
        /// The request id.
        /// </summary>
        public int RequestId { get; set; }

        /// <summary>
        /// The type of news article (0 - plain text or html, 1 - binary data / pdf).
        /// </summary>
        public int ArticleType { get; set; }

        /// <summary>
        /// The body of article (if articleType == 1, the binary data is encoded using the Base64 scheme).
        /// </summary>
        public string ArticleText { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NewsArticleEventArgs"/> class
        /// </summary>
        public NewsArticleEventArgs(int requestId, int articleType, string articleText)
        {
            RequestId = requestId;
            ArticleType = articleType;
            ArticleText = articleText;
        }
    }
}
