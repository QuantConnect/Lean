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
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Commands;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Queues
{
    /// <summary>
    /// Represents a command queue handler that sources it's commands from
    /// a file on the local disk
    /// </summary>
    public class FileCommandQueueHandler : ICommandQueueHandler
    {
        private readonly string _commandJsonFilePath;
        private readonly Queue<ICommand> _commands = new Queue<ICommand>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCommandQueueHandler"/> class
        /// using the 'command-json-file' configuration value for the command json file
        /// </summary>
        public FileCommandQueueHandler()
            : this(Config.Get("command-json-file", "command.json"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCommandQueueHandler"/> class
        /// </summary>
        /// <param name="commandJsonFilePath">The file path to the commands json file</param>
        public FileCommandQueueHandler(string commandJsonFilePath)
        {
            _commandJsonFilePath = commandJsonFilePath;
        }

        /// <summary>
        /// Initializes this command queue for the specified job
        /// </summary>
        /// <param name="job">The job that defines what queue to bind to</param>
        /// <param name="algorithm">The algorithm instance</param>
        public void Initialize(AlgorithmNodePacket job, IAlgorithm algorithm)
        {
        }

        /// <summary>
        /// Gets the next command in the queue
        /// </summary>
        /// <returns>The next command in the queue, if present, null if no commands present</returns>
        public IEnumerable<ICommand> GetCommands()
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }
    }
}
