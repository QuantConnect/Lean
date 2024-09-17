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
using QuantConnect.Interfaces;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Algorithm callback command type
    /// </summary>
    public class CallbackCommand : BaseCommand
    {
        /// <summary>
        /// The target command type to run, if empty or null will be the generic untyped command handler
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The command payload
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// Runs this command against the specified algorithm instance
        /// </summary>
        /// <param name="algorithm">The algorithm to run this command against</param>
        public override CommandResultPacket Run(IAlgorithm algorithm)
        {
            if (string.IsNullOrEmpty(Type))
            {
                // target is the untyped algorithm handler
                var result = algorithm.OnCommand(JsonConvert.DeserializeObject<Command>(Payload));
                return new CommandResultPacket(this, result);
            }
            return algorithm.RunCommand(this);
        }
    }
}
