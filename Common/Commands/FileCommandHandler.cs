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
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using System.Collections.Generic;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Represents a command handler that sources it's commands from a file on the local disk
    /// </summary>
    public class FileCommandHandler : BaseCommandHandler
    {
        private readonly string _commandJsonFilePath;
        private readonly Queue<ICommand> _commands = new Queue<ICommand>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCommandHandler"/> class
        /// using the 'command-json-file' configuration value for the command json file
        /// </summary>
        public FileCommandHandler()
            : this(Config.Get("command-json-file", "command.json"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCommandHandler"/> class
        /// </summary>
        /// <param name="commandJsonFilePath">The file path to the commands json file</param>
        public FileCommandHandler(string commandJsonFilePath)
        {
            _commandJsonFilePath = commandJsonFilePath;
        }

        /// <summary>
        /// Gets the next command in the queue
        /// </summary>
        /// <returns>The next command in the queue, if present, null if no commands present</returns>
        protected override IEnumerable<ICommand> GetCommands()
        {
            if (File.Exists(_commandJsonFilePath))
            {
                // update the queue by reading the command file
                ReadCommandFile();
            }

            while (_commands.Count != 0)
            {
                yield return _commands.Dequeue();
            }
        }

        /// <summary>
        /// Acknowledge a command that has been executed
        /// </summary>
        /// <param name="command">The command that was executed</param>
        /// <param name="commandResultPacket">The result</param>
        protected override void Acknowledge(ICommand command, CommandResultPacket commandResultPacket)
        {
            if (string.IsNullOrEmpty(command.Id))
            {
                Log.Error($"FileCommandHandler.Acknowledge(): command Id is null or empty, will skip writting result file");
                return;
            }
            File.WriteAllText($"command-result-{command.Id}.json", JsonConvert.SerializeObject(commandResultPacket));
        }

        /// <summary>
        /// Reads the commnd file on disk and populates the queue with the commands
        /// </summary>
        private void ReadCommandFile()
        {
            object deserialized;
            try
            {
                if (!File.Exists(_commandJsonFilePath)) return;
                var contents = File.ReadAllText(_commandJsonFilePath);
                deserialized = JsonConvert.DeserializeObject(contents, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            }
            catch (Exception err)
            {
                Log.Error(err);
                deserialized = null;
            }

            // remove the file when we're done reading it
            File.Delete(_commandJsonFilePath);

            // try it as an enumerable
            var enumerable = deserialized as IEnumerable<ICommand>;
            if (enumerable != null)
            {
                foreach (var command in enumerable)
                {
                    _commands.Enqueue(command);
                }
                return;
            }

            // try it as a single command
            var item = deserialized as ICommand;
            if (item != null)
            {
                _commands.Enqueue(item);
            }
        }
    }
}
