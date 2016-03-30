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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Commands;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Queues;

namespace QuantConnect.Tests.Queues
{
    [TestFixture]
    public class FileCommandQueueHandlerTests
    {
        private const string SingleCommandJsonFilePath = "command.json";
        private const string MultiCommandJsonFilePath = "commands.json";
        private const string MultiCommandXmlFilePath = "commands.xml";

        [Test]
        public void ReadsSingleJsonCommandFromFile()
        {
            if (File.Exists(SingleCommandJsonFilePath)) File.Delete(SingleCommandJsonFilePath);
            var queue = new JsonCommandQueueHandler(SingleCommandJsonFilePath);
            Assert.IsEmpty(queue.GetCommands());
            File.WriteAllText(SingleCommandJsonFilePath, JsonConvert.SerializeObject(new LiquidateCommand(), new JsonSerializerSettings{TypeNameHandling = TypeNameHandling.All}));
            Assert.IsInstanceOf(typeof (LiquidateCommand), queue.GetCommands().Single());
        }

        [Test]
        public void ReadsMultipleCommandsFromJsonFile()
        {
            if (File.Exists(MultiCommandJsonFilePath)) File.Delete(MultiCommandJsonFilePath);
            var queue = new JsonCommandQueueHandler(MultiCommandJsonFilePath);
            Assert.IsEmpty(queue.GetCommands());
            File.WriteAllText(MultiCommandJsonFilePath, JsonConvert.SerializeObject(new List<ICommand>
            {
                new LiquidateCommand(),
                new SpecialCommand()
            }, new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All}));
            var list = queue.GetCommands().ToList();
            Assert.IsInstanceOf(typeof (LiquidateCommand), list[0]);
            Assert.IsInstanceOf(typeof (SpecialCommand), list[1]);
            Assert.IsEmpty(queue.GetCommands());
        }

        [Test]
        public void ReadsMultipleCommandsFromXmlFile()
        {
            if (File.Exists(MultiCommandXmlFilePath)) File.Delete(MultiCommandXmlFilePath);
            var queue = new XmlCommandQueueHandler(MultiCommandXmlFilePath);
            Assert.IsEmpty(queue.GetCommands());
            CommandQueue commands = new CommandQueue();
            commands.Enqueue(new LiquidateCommand());
            commands.Enqueue(new AlgorithmStatusCommand());

            using (FileStream stream = new FileStream(MultiCommandXmlFilePath, FileMode.OpenOrCreate))
            {
                XmlSerializer serializer = new XmlSerializer(typeof (CommandQueue));
                serializer.Serialize(stream, commands);
            }

            var list = queue.GetCommands().ToList();
            Assert.IsInstanceOf(typeof(LiquidateCommand), list[0]);
            Assert.IsInstanceOf(typeof(AlgorithmStatusCommand), list[1]);
            Assert.IsEmpty(queue.GetCommands());
        }

        [Test]
        public void thingus()
        {
            var color = Color.FromArgb(123, 231, 067);
            var serialzied = JsonConvert.SerializeObject(color);
        }

        private sealed class SpecialCommand : ICommand
        {
            public CommandResultPacket Run(IAlgorithm algorithm)
            {
                Console.WriteLine("This is a special command!");
                return new CommandResultPacket(this, true);
            }
        }
    }
}
