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
using QuantConnect.Commands;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuantConnect.Queues
{
    /// <summary>
    /// Represents a command queue handler that sources it's commands from
    /// a json file on the local disk
    /// </summary>
    public class JsonCommandQueueHandler : FileCommandQueueHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonCommandQueueHandler"/> class
        /// using the 'command-json-file' configuration value for the command json file
        /// </summary>
        public JsonCommandQueueHandler()
            : this(Config.Get("command-json-file", "command.json"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonCommandQueueHandler"/> class
        /// </summary>
        /// <param name="commandJsonFilePath">The file path to the commands json file</param>
        public JsonCommandQueueHandler(string commandJsonFilePath) :
            base(commandJsonFilePath)
        {
        }

        /// <summary>
        /// Reads the json command file on disk and deserialize to object
        /// </summary>
        protected override IEnumerable<ICommand> ReadCommandFile()
        {
            try
            {
                if (!File.Exists(CommandFilePath)) return null;
                var contents = File.ReadAllText(CommandFilePath);
                var deserialized = JsonConvert.DeserializeObject(contents,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                return deserialized as IEnumerable<ICommand> ?? new List<ICommand> { deserialized as ICommand };
            }
            catch (Exception err)
            {
                Log.Error(err);
                return null;
            }
        }
    }
}