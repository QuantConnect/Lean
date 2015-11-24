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

using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Represents a command that will change the algorithm's status
    /// </summary>
    public class AlgorithmStatusCommand : ICommand
    {
        /// <summary>
        /// Gets or sets the algorithm status
        /// </summary>
        public AlgorithmStatus Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmStatusCommand"/>
        /// </summary>
        public AlgorithmStatusCommand()
        {
            Status = AlgorithmStatus.Running;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmStatusCommand"/> with
        /// the specified status
        /// </summary>
        public AlgorithmStatusCommand(AlgorithmStatus status)
        {
            Status = status;
        }

        /// <summary>
        /// Sets the algoritm's status to <see cref="Status"/>
        /// </summary>
        /// <param name="algorithm">The algorithm to run this command against</param>
        public CommandResultPacket Run(IAlgorithm algorithm)
        {
            algorithm.Status = Status;
            return new CommandResultPacket(this, true);
        }
    }
}