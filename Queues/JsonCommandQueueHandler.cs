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
    public class JsonCommandQueueHandler : FileCommandQueueHandler
    {
        private readonly Queue<ICommand> _commands = new Queue<ICommand>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCommandQueueHandler"/> class
        /// using the 'command-json-file' configuration value for the command json file
        /// </summary>
        public JsonCommandQueueHandler()
            : this(Config.Get("command-json-file", "command.json"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCommandQueueHandler"/> class
        /// </summary>
        /// <param name="commandJsonFilePath">The file path to the commands json file</param>
        public JsonCommandQueueHandler(string commandJsonFilePath):
            base(commandJsonFilePath)
        {

            Commands.Enqueue(new CustomCommand());
            Commands.Enqueue(new TotoCommand());
            File.WriteAllText(commandJsonFilePath, JsonConvert.SerializeObject(new List<ICommand>
            {
                new CustomCommand(),
                new TotoCommand()
            }, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }));
        }

        /// <summary>
        /// Reads the commnd file on disk and populates the queue with the commands
        /// </summary>
        protected override object ReadCommandFile()
        {
            object deserialized;
            try
            {
                if (!File.Exists(CommandFilePath)) return null;
                var contents = File.ReadAllText(CommandFilePath);
                deserialized = JsonConvert.DeserializeObject(contents, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            }
            catch (Exception err)
            {
                Log.Error(err);
                deserialized = null;
            }

            return deserialized;
        }
    }
}
