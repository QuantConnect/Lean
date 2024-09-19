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

using QuantConnect.Packets;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Contains data held as the result of executing a command
    /// </summary>
    public class CommandResultPacket : Packet
    {
        /// <summary>
        /// Gets or sets the command that produced this packet
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// Gets or sets whether or not the
        /// </summary>
        public bool? Success { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandResultPacket"/> class
        /// </summary>
        public CommandResultPacket(ICommand command, bool? success)
            : base(PacketType.CommandResult)
        {
            Success = success;
            CommandName = command.GetType().Name;
        }
    }
}
