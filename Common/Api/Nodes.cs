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

namespace QuantConnect.Api
{
    /// <summary>
    /// Node class built for API endpoints nodes/read and nodes/create.
    /// Converts JSON properties from API response into data members for the class.
    /// Contains all relevant information on a Node to interact through API endpoints.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// The nodes cpu clock speed in GHz.
        /// </summary>
        [JsonProperty(PropertyName = "speed")]
        public decimal Speed { get; set; }

        /// <summary>
        /// The monthly and yearly prices of the node in US dollars,
        /// see <see cref="NodePrices"/> for type.
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public NodePrices Prices { get; set; }

        /// <summary>
        /// CPU core count of node.
        /// </summary>
        [JsonProperty(PropertyName = "cpu")]
        public int CpuCount { get; set; }

        /// <summary>
        /// Indicate if the node has GPU (1) or not (0)
        /// </summary>
        [JsonProperty(PropertyName = "hasGpu")]
        public int HasGPU { get; set; }

        /// <summary>
        /// Size of RAM in Gigabytes.
        /// </summary>
        [JsonProperty(PropertyName = "ram")]
        public decimal Ram { get; set; }

        /// <summary>
        /// Name of the node.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Node type identifier for configuration.
        /// </summary>
        [JsonProperty(PropertyName = "sku")]
        public string SKU { get; set; }

        /// <summary>
        /// Description of the node.
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// User currently using the node.
        /// </summary>
        [JsonProperty(PropertyName = "usedBy")]
        public string UsedBy { get; set; }

        /// <summary>
        /// URL of the user using the node
        /// </summary>
        [JsonProperty(PropertyName = "userProfile")]
        public string UserProfile { get; set; }

        /// <summary>
        /// Project the node is being used for.
        /// </summary>
        [JsonProperty(PropertyName = "projectName")]
        public string ProjectName { get; set; }

        /// <summary>
        /// Id of the project the node is being used for.
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int? ProjectId { get; set; }

        /// <summary>
        /// Indicates if the node is currently busy.
        /// </summary>
        [JsonProperty(PropertyName = "busy")]
        public bool Busy { get; set; }

        /// <summary>
        /// Full ID of node.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Maximum number of assets recommended for this node.
        /// </summary>
        [JsonProperty(PropertyName = "assets")]
        public int Assets { get; set; }

        /// <summary>
        /// Node host.
        /// </summary>
        [JsonProperty(PropertyName = "host")]
        public string Host { get; set; }

        /// <summary>
        /// Indicate if this is the active node. The project will use this node if it's not busy.
        /// </summary>
        [JsonProperty(PropertyName = "active")]
        public bool Active { get; set; }
    }

    /// <summary>
    /// Collection of <see cref="Node"/> objects for each target environment.
    /// </summary>
    public class NodeList : RestResponse
    {
        /// <summary>
        /// Collection of backtest nodes
        /// </summary>
        [JsonProperty(PropertyName = "backtest")]
        public List<Node> BacktestNodes { get; set; }

        /// <summary>
        /// Collection of research nodes
        /// </summary>
        [JsonProperty(PropertyName = "research")]
        public List<Node> ResearchNodes { get; set; }

        /// <summary>
        /// Collection of live nodes
        /// </summary>
        [JsonProperty(PropertyName = "live")]
        public List<Node> LiveNodes { get; set; }
    }

    /// <summary>
    /// Rest api response wrapper for node/create, reads in the nodes information into a
    /// node object
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
    /// Every SKU is made up of 3 variables:
    /// - Target environment (L for live, B for Backtest, R for Research)
    /// - CPU core count
    /// - Dedicated RAM (GB)
    /// </summary>
    public class SKU
    {
        /// <summary>
        /// The number of CPU cores in the node
        /// </summary>
        public int Cores { get; set; }

        /// <summary>
        /// Size of RAM in GB of the Node
        /// </summary>
        public int Memory { get; set; }

        /// <summary>
        /// Target environment for the node
        /// </summary>
        public NodeType Target { get; set; }

        /// <summary>
        /// Constructs a SKU object out of the provided node configuration
        /// </summary>
        /// <param name="cores">Number of cores</param>
        /// <param name="memory">Size of RAM in GBs</param>
        /// <param name="target">Target Environment Live/Backtest/Research</param>
        public SKU(int cores, int memory, NodeType target)
        {
            Cores = cores;
            Memory = memory;
            Target = target;
        }

        /// <summary>
        /// Generates the SKU string for API calls based on the specifications of the node
        /// </summary>
        /// <returns>String representation of the SKU</returns>
        public override string ToString()
        {
            string result = "";

            switch (Target)
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

            if (Cores == 0)
            {
                result += "-MICRO";
            }
            else
            {
                result += Cores + "-" + Memory;
            }

            return result;
        }
    }

    /// <summary>
    /// NodeTypes enum for all possible options of target environments
    /// Used in conjuction with SKU class as a NodeType is a required parameter for SKU
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

    /// <summary>
    /// Class for deserializing node prices from node object
    /// </summary>
    public class NodePrices
    {
        /// <summary>
        /// The monthly price of the node in US dollars
        /// </summary>
        [JsonProperty(PropertyName = "monthly")]
        public int Monthly { get; set; }

        /// <summary>
        /// The yearly prices of the node in US dollars
        /// </summary>
        [JsonProperty(PropertyName = "yearly")]
        public int Yearly { get; set; }
    }

    /// <summary>
    /// Supported optimization nodes
    /// </summary>
    public static class OptimizationNodes
    {
        /// <summary>
        /// 2 CPUs 8 GB ram
        /// </summary>
        public static string O2_8 => "O2-8";

        /// <summary>
        /// 4 CPUs 12 GB ram
        /// </summary>
        public static string O4_12 => "O4-12";

        /// <summary>
        /// 8 CPUs 16 GB ram
        /// </summary>
        public static string O8_16 => "O8-16";
    }
}
