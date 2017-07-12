using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Lean.Engine.Environment
{
    public class ConsoleEnvironment : IEnvironment
    {
        public void Create(AlgorithmManager algorithmManager, LeanEngineSystemHandlers systemHandlers,
            LeanEngineAlgorithmHandlers algorithmHandlers)
        {
            // NOP
        }
    }
}
