using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Commands
{
    public sealed class NotifyCommand : ICommand
    {
        public CommandResultPacket Run(IAlgorithm algorithm)
        {
            algorithm.Notify.Email("francis.gauthier.2@gmail.com","MGL843: You are broke!","Nah just kidding you are rich!");
            algorithm.Notify.Sms("438-868-4188", "You are broke!");
            return new CommandResultPacket(this, true);
        }
    }
}
