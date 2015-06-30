namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Represents the server instance that we will be performing the RESTful call.
    /// </summary>
    public enum Server
    {
        /// <summary>
        /// The account
        /// </summary>
        Account,

        /// <summary>
        /// The rates
        /// </summary>
        Rates,

        /// <summary>
        /// The streaming rates
        /// </summary>
        StreamingRates,

        /// <summary>
        /// The streaming events
        /// </summary>
        StreamingEvents
    }
}
