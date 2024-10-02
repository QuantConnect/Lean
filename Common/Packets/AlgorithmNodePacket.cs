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
using System.Collections.Generic;
using Newtonsoft.Json;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Algorithm Node Packet is a work task for the Lean Engine
    /// </summary>
    public class AlgorithmNodePacket : PythonEnvironmentPacket
    {
        /// <summary>
        /// Default constructor for the algorithm node:
        /// </summary>
        /// <param name="type"></param>
        public AlgorithmNodePacket(PacketType type)
            : base(type)
        { }

        /// <summary>
        /// The host name to use if any
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// User Id placing request
        /// </summary>
        public int UserId { get; set; }

        /// User API Token
        public string UserToken { get; set; } = string.Empty;

        /// User Organization Id
        public string OrganizationId { get; set; } = string.Empty;

        /// <summary>
        /// Project Id of the request
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// Project name of the request
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Algorithm Id - BacktestId or DeployId - Common Id property between packets.
        /// </summary>
        public string AlgorithmId
        {
            get
            {
                if (Type == PacketType.LiveNode || Type == PacketType.AlphaNode)
                {
                    return ((LiveNodePacket)this).DeployId;
                }
                else if (Type == PacketType.ResearchNode)
                {
                    return ((ResearchNodePacket)this).ResearchId;
                }
                return ((BacktestNodePacket)this).BacktestId;
            }
        }

        /// <summary>
        /// User session Id for authentication
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Language flag: Currently represents IL code or Dynamic Scripted Types.
        /// </summary>
        public Language Language { get; set; } = Language.CSharp;

        /// <summary>
        /// Server type for the deployment (512, 1024, 2048)
        /// </summary>
        public ServerType ServerType { get; set; } = ServerType.Server512;

        /// <summary>
        /// Unique compile id of this backtest
        /// </summary>
        public string CompileId { get; set; } = string.Empty;

        /// <summary>
        /// Version number identifier for the lean engine.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// An algorithm packet which has already been run and is being redelivered on this node.
        /// In this event we don't want to relaunch the task as it may result in unexpected behaviour for user.
        /// </summary>
        public bool Redelivered { get; set; }

        /// <summary>
        /// Algorithm binary with zip of contents
        /// </summary>
        public byte[] Algorithm { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Request source - Web IDE or API - for controling result handler behaviour
        /// </summary>
        public string RequestSource { get; set; } = "WebIDE";

        /// <summary>
        /// The maximum amount of RAM (in MB) this algorithm is allowed to utilize
        /// </summary>
        public int RamAllocation {
            get { return Controls.RamAllocation; }
        }

        /// <summary>
        /// Specifies values to control algorithm limits
        /// </summary>
        public Controls Controls { get; set; }

        /// <summary>
        /// The parameter values used to set algorithm parameters
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// String name of the HistoryProvider we're running with
        /// </summary>
        public string HistoryProvider { get; set; } = string.Empty;

        /// <summary>
        /// Algorithm running mode.
        /// </summary>
        [JsonIgnore]
        public virtual AlgorithmMode AlgorithmMode { get; } = AlgorithmMode.Backtesting;

        /// <summary>
        /// Deployment target, either local or cloud.
        /// </summary>
        [JsonIgnore]
        public DeploymentTarget DeploymentTarget { get; set; }

        /// <summary>
        /// Gets a unique name for the algorithm defined by this packet
        /// </summary>
        public string GetAlgorithmName()
        {
            return Invariant($"{UserId}-{ProjectId}-{AlgorithmId}");
        }
    }
}
