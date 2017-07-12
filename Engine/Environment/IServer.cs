using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Environment
{
    /// <summary>
    /// Provides an outer scope to Lean and Lean.Engine that is convenient
    /// for specializing logic around the server hosting Lean
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// Run the IServer implementation
        /// </summary>
        /// <param name="algorithmManager">The Algorithm manager</param>
        /// <param name="systemHandlers">Exposes lean engine system handlers running LEAN</param>
        /// <param name="algorithmHandlers">Exposes the lean algorithm handlers running lean</param>
        /// <param name="job">The job packet representing either a live or backtest Lean instance</param>
        void Run(AlgorithmManager algorithmManager, LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job);
    }
}
