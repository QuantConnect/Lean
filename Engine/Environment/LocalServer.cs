using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Environment
{
    public class LocalServer : IServer
    {
        public void Run(AlgorithmManager algorithmManager, LeanEngineSystemHandlers systemHandlers,
            LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job)
        {
            // NOP
        }
    }
}
