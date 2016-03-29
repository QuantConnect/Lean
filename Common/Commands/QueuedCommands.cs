using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace QuantConnect.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class QueuedCommands : Queue<ICommand>, IXmlSerializable
    {
        //http://www.codeproject.com/Articles/738100/XmlSerializer-Serializing-list-of-interfaces
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public XmlSchema GetSchema()
        {
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement("QueuedCommands");
            while (reader.IsStartElement("ICommand"))
            {
                Type type = Type.GetType(reader.GetAttribute("AssemblyQualifiedName"));
                XmlSerializer serial = new XmlSerializer(type);

                reader.ReadStartElement("ICommand");
                this.Enqueue((ICommand)serial.Deserialize(reader));
                reader.ReadEndElement();
            }
            reader.ReadEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
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
