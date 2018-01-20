using System;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Server;
using QuantConnect.Packets;

namespace QuantConnect.Views
{
    /// <summary>
    /// Desktop implementation of the ILeanManager interface for use with a REST endpoint
    /// </summary>          
    public class DesktopLeanManager : ILeanManager
    {

        public DesktopLeanManager()
        {
        }

        /// <summary>
        /// Initialize the DesktopLeanManager
        /// </summary>
        /// <param name="systemHandlers">Exposes lean engine system handlers running LEAN</param>
        /// <param name="algorithmHandlers">Exposes the lean algorithm handlers running lean</param>
        /// <param name="job">The job packet representing either a live or backtest Lean instance</param>
        /// <param name="algorithmManager">The Algorithm manager</param>
        public void Initialize(LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job, AlgorithmManager algorithmManager)
        {
            Console.WriteLine("Initializing Desktop Lean Manager");
        }

        /// <summary>
        /// Sets the IAlgorithm instance in the ILeanManager
        /// </summary>
        /// <param name="algorithm">The IAlgorithm instance being run</param>
        public void SetAlgorithm(IAlgorithm algorithm)
        {
            Console.WriteLine("Set Algorithm Desktop Lean Manager");
        }

        /// <summary>
        /// Update ILeanManager with the IAlgorithm instance
        /// </summary>
        public void Update()
        {
            Console.WriteLine("Update the Desktop Lean Manager");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine("Dispose the Desktop Lean Manager");
        }
    }
}
