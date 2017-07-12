using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Environment
{
    /// <summary>
    /// NOP implementation of the IServer interface
    /// </summary>
    public class LocalServer : IServer
    {
        /// <summary>
        /// Empty implementation of the IServer interface
        /// </summary>
        /// <param name="algorithmManager">The Algorithm manager</param>
        /// <param name="systemHandlers">Exposes lean engine system handlers running LEAN</param>
        /// <param name="algorithmHandlers">Exposes the lean algorithm handlers running lean</param>
        /// <param name="job">The job packet representing either a live or backtest Lean instance</param>
        public void Run(AlgorithmManager algorithmManager, LeanEngineSystemHandlers systemHandlers,
            LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job)
        {
            // NOP
        }
    }
}
