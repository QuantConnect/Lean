namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Represents different environments available for the REST API.
    /// </summary>
    public enum Environment
    {
        /// <summary>
        /// An environment purely for testing; it is not as fast, stable and reliable as the other environments (i.e. it can go down once in a while). 
        /// Market data returned from this environment is simulated (not real market data).
        /// </summary>
        Sandbox,

        /// <summary>
        /// A stable environment; recommended for testing with your fxTrade Practice account and your personal access token.
        /// </summary>
        Practice,

        /// <summary>
        /// A stable environment; recommended for production-ready code to execute with your fxTrade account and your personal access token.
        /// </summary>
        Trade
    }
}
