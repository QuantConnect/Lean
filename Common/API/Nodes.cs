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
using QuantConnect.Api;
using QuantConnect.Util;

namespace QuantConnect.API
{
    /// <summary>
    /// Node obj for API response
    /// </summary>
    public class Node
    {

        /// <summary>
        /// The nodes cpu speed
        /// </summary>
        [JsonProperty(PropertyName = "speed")]
        public decimal Speed { get; set; }

        /// <summary>
        /// The price of the node
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public int Price { get; set; }

        /// <summary>
        /// CPU Count of node
        /// </summary>
        [JsonProperty(PropertyName = "cpu")]
        public int CpuCount { get; set; }

        /// <summary>
        /// Size of RAM
        /// </summary>
        [JsonProperty(PropertyName = "ram")]
        public int Ram { get; set; }

        /// <summary>
        /// Name of node
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Node type (Sku)
        /// </summary>
        [JsonProperty(PropertyName = "sku")]
        public string Sku { get; set; }

        /// <summary>
        /// String description of Node
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Used by 
        /// </summary>
        [JsonProperty(PropertyName = "usedBy")]
        public string UsedBy { get; set; }

        /// <summary>
        /// Project it is being used for
        /// </summary>
        [JsonProperty(PropertyName = "projectName")]
        public string ProjectName { get; set; }

        /// <summary>
        /// Bool if the node is currently busy
        /// </summary>
        [JsonProperty(PropertyName = "busy")]
        public bool Busy { get; set; }

        /// <summary>
        /// Full ID of node
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }


        /// <summary>
        /// Determine the appropriate SKU for a node by params
        /// </summary>
        /// <param name="cores">Number of cores</param>
        /// <param name="memory">Size of RAM</param>
        /// <param name="target">Target Environment Live/Backtest/Research</param>
        /// <returns>Returns the SKU as a string for the node desired</returns>
        public string GetSKU(int cores, int memory, string target)
        {
            string result = "";
            result += target[0];

            if(cores == 0)
            {
                result += "-micro";
            } else
            {
                result += cores + "-" + memory;
            }

            return result;
        }

    }

    /// <summary>
    /// node/read rest api response wrapper
    /// </summary>
    public class NodeList : RestResponse
    {

        /// <summary>
        /// Collection of backtest nodes
        /// </summary>
        [JsonProperty(PropertyName = "backtest")]
        public List<Node> BacktestNodes;

        /// <summary>
        /// Collection of research nodes
        /// </summary>
        [JsonProperty(PropertyName = "research")]
        public List<Node> ResearchNodes;

        /// <summary>
        /// Collection of live nodes
        /// </summary>
        [JsonProperty(PropertyName = "live")]
        public List<Node> LiveNodes;
    }

    /// <summary>
    /// node/create rest api response wrapper
    /// </summary>
    public class CreatedNode : RestResponse
    {
        /// <summary>
        /// Contains the node created
        /// </summary>
        [JsonProperty(PropertyName = "node")]
        public Node Node;
    }



}
