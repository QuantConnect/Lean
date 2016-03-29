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

using QuantConnect.Commands;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using System.Collections.Generic;
using System.IO;

namespace QuantConnect.Queues
{
    /// <summary>
    /// Represents a command queue handler that sources it's commands from
    /// a file on the local disk
    /// </summary>
    public abstract class FileCommandQueueHandler : ICommandQueueHandler
    {
        private readonly string _commandFilePath;
        private readonly QueuedCommands _commands = new QueuedCommands();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCommandQueueHandler"/> class
        /// </summary>
        /// <param name="commandFilePath">The file path to the commands file</param>
        public FileCommandQueueHandler(string commandFilePath)
        {
            _commandFilePath = commandFilePath;
        }

        /// <summary>
        /// The file path to the commands file
        /// </summary>
        protected string CommandFilePath
        {
            get
            {
                return _commandFilePath;
            }
        }

        /// <summary>
        /// The queue of commands
        /// </summary>
        protected QueuedCommands Commands
        {
            get
            {
                return _commands;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the next command in the queue
        /// </summary>
        /// <returns>The next command in the queue, if present, null if no commands present</returns>
        public IEnumerable<ICommand> GetCommands()
        {
            if (File.Exists(_commandFilePath))
            {
                // load commands from file
                LoadCommands();

                // remove the file when we're done reading it
                File.Delete(_commandFilePath);
            }

            while (_commands.Count != 0)
            {
                yield return _commands.Dequeue();
            }
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
        /// Reads the command file on disk and deserialize to object
        /// </summary>
        protected abstract object ReadCommandFile();

        /// <summary>
        /// Populates the queue with the deserialized commands from file
        /// </summary>
        private void LoadCommands()
        {
            // update the queue by reading the command file
            object deserialized = ReadCommandFile();

            // try it as an enumerable
            var enumerable = deserialized as IEnumerable<ICommand>;
            if (enumerable != null)
            {
                foreach (var command in enumerable)
                {
                    _commands.Enqueue(command);
                }
            }
            else
            {
                // try it as a single command
                var item = deserialized as ICommand;
                if (item != null)
                {
                    _commands.Enqueue(item);
                }
            }
        }
    }
}