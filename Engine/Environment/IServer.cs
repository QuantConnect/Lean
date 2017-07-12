using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Environment
{
    public interface IServer
    {
        void Run(AlgorithmManager algorithmManager, LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job);
    }
}
