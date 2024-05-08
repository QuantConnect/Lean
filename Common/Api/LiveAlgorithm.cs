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
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Api
{
    public class BaseLiveAlgorithm : RestResponse
    {
        /// <summary>
        /// Project id for the live instance
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// Unique live algorithm deployment identifier (similar to a backtest id).
        /// </summary>
        public string DeployId { get; set; }
    }

    public class CreateLiveAlgorithmResponse : BaseLiveAlgorithm
    {
        /// <summary>
        /// The version of the Lean used to run the algorithm
        /// </summary>
        public int VersionId { get; set; }

        /// <summary>
        /// Id of the node that will run the algorithm
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// HTTP status response code
        /// </summary>
        public string ResponseCode { get; set; }
    }

    /// <summary>
    /// Response from List Live Algorithms request to QuantConnect Rest API.
    /// </summary>
    public class LiveAlgorithmSummary : BaseLiveAlgorithm
    {
        /// <summary>
        /// Algorithm status: running, stopped or runtime error.
        /// </summary>
        public AlgorithmStatus Status { get; set; }

        /// <summary>
        /// Datetime the algorithm was launched in UTC.
        /// </summary>
        public DateTime Launched { get; set; }

        /// <summary>
        /// Datetime the algorithm was stopped in UTC, null if its still running.
        /// </summary>
        public DateTime? Stopped { get; set; }

        /// <summary>
        /// Brokerage
        /// </summary>
        public string Brokerage { get; set; }

        /// <summary>
        /// Chart we're subscribed to
        /// </summary>
        /// <remarks>
        /// Data limitations mean we can only stream one chart at a time to the consumer. See which chart you're watching here.
        /// </remarks>
        public string Subscription { get; set; }

        /// <summary>
        /// Live algorithm error message from a crash or algorithm runtime error.
        /// </summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// List of the live algorithms running which match the requested status
    /// </summary>
    public class LiveList : RestResponse
    {
        /// <summary>
        /// Algorithm list matching the requested status.
        /// </summary>
        [JsonProperty(PropertyName = "live")]
        public List<LiveAlgorithmSummary> Algorithms { get; set; }
    }
}
