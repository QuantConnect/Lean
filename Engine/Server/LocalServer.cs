using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Server
{
    /// <summary>
    /// NOP implementation of the IServer interface
    /// </summary>
    public class LocalServer : IServer
    {
        /// <summary>
        /// Empty implementation of the IServer interface
        /// </summary>
        /// <param name="systemHandlers">Exposes lean engine system handlers running LEAN</param>
        /// <param name="algorithmHandlers">Exposes the lean algorithm handlers running lean</param>
        /// <param name="job">The job packet representing either a live or backtest Lean instance</param>
        /// <param name="algorithmManager">The Algorithm manager</param>
        public void Initialize(LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job, AlgorithmManager algorithmManager)
        {
            // NOP
        }

        /// <summary>
        /// Sets the IAlgorithm instance in the IServer
        /// </summary>
        /// <param name="algorithm">The IAlgorithm instance being run</param>
        public void SetAlgorithm(IAlgorithm algorithm)
        {
            // NOP
        }

        /// <summary>
        /// Update IServer with the IAlgorithm instance
        /// </summary>
        public void Update()
        {
            // NOP
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            // NOP
        }
    }
}
