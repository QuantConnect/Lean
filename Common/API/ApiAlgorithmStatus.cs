namespace QuantConnect.API
{
    /// <summary>
    /// An enum representing the possible status of a live running algorithm on QC
    /// </summary>
    public enum ApiAlgorithmStatus
    {
        /// <summary>
        /// Represents all possible statuses
        /// </summary>
        All,
        /// <summary>
        /// The algorithm is currently running without error
        /// </summary>
        Running,
        /// <summary>
        /// The algorithm is not running and has crashed
        /// </summary>
        RuntimeError,
        /// <summary>
        /// The algorithm has been stopped
        /// </summary>
        Stopped,
        /// <summary>
        /// The algorithm's positions have been liquidated
        /// </summary>
        Liquidated
    }
}
