using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Commands
{
    public sealed class CustomCommand : ICommand
    {
        public CommandResultPacket Run(IAlgorithm algorithm)
        {
            Console.WriteLine("Toto");
            return new CommandResultPacket(this,true);
        }
    }
}
