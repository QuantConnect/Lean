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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Api
{
    /// <summary>
    /// File sent to the AI assistance tools
    /// </summary>
    public class AIFile
    {
        /// <summary>
        /// Name of the file
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Contents of the file
        /// </summary>
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }
    }

    /// <summary>
    /// Criteria for an AI assistance search
    /// </summary>
    public class SearchCriteria
    {
        /// <summary>
        /// Input for the search
        /// </summary>
        [JsonProperty(PropertyName = "input")]
        public string Input { get; set; }

        /// <summary>
        /// Type of the search criteria: Stubs, Forum, Docs or Examples
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Number of results to return
        /// </summary>
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Single result of an AI assistance search
    /// </summary>
    public class SearchRetrieval
    {
        /// <summary>
        /// Url of the search result
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        /// <summary>
        /// Relevance score of the search result
        /// </summary>
        [JsonProperty(PropertyName = "score")]
        public decimal Score { get; set; }

        /// <summary>
        /// Content of the search result
        /// </summary>
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }

        /// <summary>
        /// Type of the search result: 0=Stubs, 1=Forum, 2=Docs, 3=Examples
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public int Type { get; set; }
    }

    /// <summary>
    /// Base response of the AI assistance tools
    /// </summary>
    public class AIToolResponse : RestResponse
    {
        /// <summary>
        /// State of the tool run, for example "End" or "Error"
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        /// <summary>
        /// Version of the response
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public decimal Version { get; set; }
    }

    /// <summary>
    /// Response of an AI assistance tool that carries a payload
    /// </summary>
    /// <typeparam name="T">Type the payload deserializes into</typeparam>
    public class AIToolPayloadResponse<T> : AIToolResponse
    {
        /// <summary>
        /// Result of the tool run
        /// </summary>
        [JsonProperty(PropertyName = "payload")]
        public T Payload { get; set; }

        /// <summary>
        /// Type of the payload, for example "String" or "StringArray"
        /// </summary>
        [JsonProperty(PropertyName = "payloadType")]
        public string PayloadType { get; set; }
    }

    /// <summary>
    /// Response to a backtest initialization request
    /// </summary>
    public class BacktestInitResponse : AIToolPayloadResponse<string>
    {
    }

    /// <summary>
    /// Response to a code completion request
    /// </summary>
    public class CodeCompletionResponse : AIToolPayloadResponse<List<string>>
    {
    }

    /// <summary>
    /// Response to an error enhancement request
    /// </summary>
    public class ErrorEnhanceResponse : AIToolPayloadResponse<string>
    {
    }

    /// <summary>
    /// Response to a PEP8 conversion request, the payload maps each file name to its converted code
    /// </summary>
    public class PEP8ConvertResponse : AIToolPayloadResponse<Dictionary<string, string>>
    {
    }

    /// <summary>
    /// Response to a syntax check request
    /// </summary>
    public class SyntaxCheckResponse : AIToolPayloadResponse<List<string>>
    {
    }

    /// <summary>
    /// Response to a search request
    /// </summary>
    public class SearchResponse : AIToolResponse
    {
        /// <summary>
        /// List of search results
        /// </summary>
        [JsonProperty(PropertyName = "retrivals")]
        public List<SearchRetrieval> Retrievals { get; set; }

        /// <summary>
        /// Id of the message
        /// </summary>
        [JsonProperty(PropertyName = "messageId")]
        public int MessageId { get; set; }
    }
}
