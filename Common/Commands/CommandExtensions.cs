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
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using System.Collections.Concurrent;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Helper methods associated with commands
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// Will consumer and execute any command in the queue
        /// </summary>
        /// <param name="commandQueue">The command queue to fetch commands from</param>
        /// <param name="algorithm">The target algorithm instance</param>
        /// <param name="resultQueue">The target result queue for the commands result packet</param>
        public static void Consume(this ICommandQueueHandler commandQueue, IAlgorithm algorithm, ConcurrentQueue<Packet> resultQueue)
        {
            try
            {
                foreach (var command in commandQueue.GetCommands().Where(c => c != null))
                {
                    Log.Trace($"CommandExtensions.Consume(): Executing {command}");
                    CommandResultPacket result;
                    try
                    {
                        result = command.Run(algorithm);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        algorithm.Error($"{command.GetType().Name} Error: {err.Message}");
                        result = new CommandResultPacket(command, false);
                    }

                    // send the result of the command off to the result handler
                    resultQueue.Enqueue(result);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}
