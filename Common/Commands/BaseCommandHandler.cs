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
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Packets;
using Newtonsoft.Json.Linq;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Base algorithm command handler
    /// </summary>
    public abstract class BaseCommandHandler : ICommandHandler
    {
        protected static readonly JsonSerializerSettings Settings = new() { TypeNameHandling = TypeNameHandling.All };

        /// <summary>
        /// The algorithm instance
        /// </summary>
        protected IAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Initializes this command queue for the specified job
        /// </summary>
        /// <param name="job">The job that defines what queue to bind to</param>
        /// <param name="algorithm">The algorithm instance</param>
        public virtual void Initialize(AlgorithmNodePacket job, IAlgorithm algorithm)
        {
            Algorithm = algorithm;
        }

        /// <summary>
        /// Get the commands to run
        /// </summary>
        protected abstract IEnumerable<ICommand> GetCommands();

        /// <summary>
        /// Acknowledge a command that has been executed
        /// </summary>
        /// <param name="command">The command that was executed</param>
        /// <param name="commandResultPacket">The result</param>
        protected virtual void Acknowledge(ICommand command, CommandResultPacket commandResultPacket)
        {
            // nop
        }

        /// <summary>
        /// Will consumer and execute any command in the queue
        /// </summary>
        public IEnumerable<CommandResultPacket> ProcessCommands()
        {
            List<CommandResultPacket> resultPackets = null;
            try
            {
                foreach (var command in GetCommands().Where(c => c != null))
                {
                    Log.Trace($"BaseCommandHandler.ProcessCommands(): {Messages.BaseCommandHandler.ExecutingCommand(command)}");
                    CommandResultPacket result;
                    try
                    {
                        result = command.Run(Algorithm);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        Algorithm.Error($"{command.GetType().Name} Error: {err.Message}");
                        result = new CommandResultPacket(command, false);
                    }

                    Acknowledge(command, result);

                    if(resultPackets == null)
                    {
                        resultPackets = new List<CommandResultPacket>();
                    }
                    resultPackets.Add(result);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return resultPackets ?? Enumerable.Empty<CommandResultPacket>();
        }

        /// <summary>
        /// Disposes of this instance
        /// </summary>
        public virtual void Dispose()
        {
            // nop
        }

        /// <summary>
        /// Helper method to create a callback command
        /// </summary>
        protected ICommand TryGetCallbackCommand(string payload)
        {
            Dictionary<string, JToken> deserialized = new(StringComparer.InvariantCultureIgnoreCase);
            try
            {
                if (!string.IsNullOrEmpty(payload))
                {
                    var jobject = JObject.Parse(payload);
                    foreach (var kv in jobject)
                    {
                        deserialized[kv.Key] = kv.Value;
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err, $"Payload: '{payload}'");
                return null;
            }

            if (!deserialized.TryGetValue("id", out var id) || id == null)
            {
                id = string.Empty;
            }

            if (!deserialized.TryGetValue("$type", out var type) || type == null)
            {
                type = string.Empty;
            }

            return new CallbackCommand { Id = id.ToString(), Type = type.ToString(), Payload = payload };
        }
    }
}
