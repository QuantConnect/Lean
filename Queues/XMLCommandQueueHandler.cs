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
using System.Xml.Serialization;

namespace QuantConnect.Queues
{
    /// <summary>
    /// Represents a command queue handler that sources it's commands from
    /// an xml file on the local disk
    /// </summary>
    public class XmlCommandQueueHandler : FileCommandQueueHandler
    {
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(QueuedCommands));
        /// <summary>
        /// Initializes a new instance of the <see cref="FileCommandQueueHandler"/> class
        /// using the 'command-xml-file' configuration value for the command xml file
        /// </summary>
        public XmlCommandQueueHandler()
            : this(Config.Get("command-xml-file", "command.xml"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlCommandQueueHandler"/> class
        /// </summary>
        /// <param name="commandFilePath">The file path to the commands xml file</param>
        public XmlCommandQueueHandler(string commandFilePath)
            :base(commandFilePath)
        {

            QueuedCommands commands = new QueuedCommands();
            commands.Enqueue(new TotoCommand());
            commands.Enqueue(new CustomCommand());

            using (FileStream stream = new FileStream(CommandFilePath, FileMode.OpenOrCreate))
                serializer.Serialize(stream, commands);
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override object ReadCommandFile()
        {
            object deserialized;
           
            try
            {
                using (FileStream stream = new FileStream(CommandFilePath, FileMode.Open))
                    deserialized = serializer.Deserialize(stream);
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
