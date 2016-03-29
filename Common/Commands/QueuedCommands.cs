using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Represent a queue of ICommand that can also be serialized or deserialized to xml
    /// http://www.codeproject.com/Articles/738100/XmlSerializer-Serializing-list-of-interfaces
    /// </summary>
    public class QueuedCommands : Queue<ICommand>, IXmlSerializable
    {
        /// <summary>
        /// Returns the XML schema
        /// </summary>
        /// <returns></returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Reads the xml elements, deserialize and enqueue the commands
        /// </summary>
        /// <param name="reader">the xml reader</param>
        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement("QueuedCommands");
            while (reader.IsStartElement("ICommand"))
            {
                Type type = Type.GetType(reader.GetAttribute("AssemblyQualifiedName"));
                XmlSerializer serial = new XmlSerializer(type);

                reader.ReadStartElement("ICommand");
                this.Enqueue((ICommand) serial.Deserialize(reader));
                reader.ReadEndElement();
            }
            reader.ReadEndElement();
        }

        /// <summary>
        /// Serialize all the queued commands
        /// </summary>
        /// <param name="writer">the xml writer</param>
        public void WriteXml(XmlWriter writer)
        {
            foreach (ICommand command in this)
            {
                writer.WriteStartElement("ICommand");
                writer.WriteAttributeString("AssemblyQualifiedName", command.GetType().AssemblyQualifiedName);
                XmlSerializer xmlSerializer = new XmlSerializer(command.GetType());
                xmlSerializer.Serialize(writer, command);
                writer.WriteEndElement();
            }
        }
    }
}