using QuantConnect.Packets;

namespace QuantConnect.Views
{
    /// <summary>
    /// Messaging System Plugin Interface for the UI. 
    /// Provides a common messaging pattern between the desktop client and the UI.
    /// </summary>
    public interface IDesktopMessageHandler
    {
        /// <summary>
        /// This method should be called first when a new job is recieved.
        /// </summary>
        /// <param name="job">The job that is being executed</param>
        void Initialize(AlgorithmNodePacket job);

        /// <summary>
        /// Display the Handled error packet
        /// </summary>
        /// <param name="packet">Handled error packet to be displayed</param>
        void DisplayHandledErrorPacket(HandledErrorPacket packet);
        /// <summary>
        /// Method to display the runtime error packet
        /// </summary>
        /// <param name="packet">Runtime error packet to be displayed</param>
        void DisplayRuntimeErrorPacket(RuntimeErrorPacket packet);
        /// <summary>
        /// Method to display the Log packets
        /// </summary>
        /// <param name="packet">Log packet to be displayed</param>
        void DisplayLogPacket(LogPacket packet);
        /// <summary>
        /// Method to display the debug packet
        /// </summary>
        /// <param name="packet">Debug packet to be displayed</param>
        void DisplayDebugPacket(DebugPacket packet);
        /// <summary>
        /// Displays the Backtest results packet
        /// </summary>
        /// <param name="packet">Backtest results</param>
        void DisplayBacktestResultsPacket(BacktestResultPacket packet);
    }
}