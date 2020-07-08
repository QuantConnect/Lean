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

using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Api;

namespace QuantConnect.API
{
    /// <summary>
    /// Node obj for API response, contains all relevant data members for a node.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// The nodes cpu clock speed in GHz
        /// </summary>
        [JsonProperty(PropertyName = "speed")]
        public decimal Speed { get; set; }

        /// <summary>
        /// The monthly price of the node in US dollars
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public int Price { get; set; }

        /// <summary>
        /// CPU core count of node
        /// </summary>
        [JsonProperty(PropertyName = "cpu")]
        public int CpuCount { get; set; }

        /// <summary>
        /// Size of RAM in Gigabytes
        /// </summary>
        [JsonProperty(PropertyName = "ram")]
        public decimal Ram { get; set; }

        /// <summary>
        /// Name of the node
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Node type identifier for configuration
        /// </summary>
        [JsonProperty(PropertyName = "sku")]
        public string SKU { get; set; }

        /// <summary>
        /// String description of the node
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// User currently using the node
        /// </summary>
        [JsonProperty(PropertyName = "usedBy")]
        public string UsedBy { get; set; }

        /// <summary>
        /// Project the node is being used for
        /// </summary>
        [JsonProperty(PropertyName = "projectName")]
        public string ProjectName { get; set; }

        /// <summary>
        /// Boolean if the node is currently busy
        /// </summary>
        [JsonProperty(PropertyName = "busy")]
        public bool Busy { get; set; }

        /// <summary>
        /// Full ID of node
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Rest api response wrapper for node/read, contains the set of node lists for each
    /// target environment.
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
    /// Rest api response wrapper for node/create, contains the new node object created
    /// </summary>
    public class CreatedNode : RestResponse
    {
        /// <summary>
        /// The created node from node/create
        /// </summary>
        [JsonProperty("node")]
        public Node Node { get; set; }
    }

    /// <summary>
    /// Class for generating a SKU for a node with a given configuration
    /// </summary>
    public class SKU
    {
        /// <summary>
        /// The number of CPU cores in the node
        /// </summary>
        public int cores;

        /// <summary>
        /// Size of RAM in GB of the Node
        /// </summary>
        public int memory;

        /// <summary>
        /// Target environment for the node
        /// </summary>
        public NodeType target;

        /// <summary>
        /// Constructs a SKU object out of the provided node configuration
        /// </summary>
        /// <param name="cores">Number of cores</param>
        /// <param name="memory">Size of RAM in GBs</param>
        /// <param name="target">Target Environment Live/Backtest/Research</param>
        public SKU(int cores, int memory, NodeType target)
        {
            this.cores = cores;
            this.memory = memory;
            this.target = target;
        }

        /// <summary>
        /// Generates the SKU string for API calls based on the specifications of the node
        /// </summary>
        /// <returns>String representation of the SKU</returns>
        public override string ToString()
        {
            string result = "";

            switch (target)
            {
                case NodeType.Backtest: 
                    result += "B";
                    break;
                case NodeType.Research:
                    result += "R";
                    break;
                case NodeType.Live:
                    result += "L";
                    break;
            }

            if (cores == 0)
            {
                result += "-Micro";
            }
            else
            {
                result += cores + "-" + memory;
            }

            return result;
        }
    }

    /// <summary>
    /// The possible target environments for Nodes
    /// </summary>
    public enum NodeType
    {
        /// A node for running backtests
        Backtest,   //0
        /// A node for running research
        Research,   //1
        /// A node for live trading
        Live        //2
    }
}
