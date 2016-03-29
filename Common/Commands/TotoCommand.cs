using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Logging;

namespace QuantConnect.Commands
{
    public sealed class TotoCommand : ICommand
    {
        public CommandResultPacket Run(IAlgorithm algorithm)
        {
            Log.Trace("Toto");
            return new CommandResultPacket(this, true);
        }

    }
}
