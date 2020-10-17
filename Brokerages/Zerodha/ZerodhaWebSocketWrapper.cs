using System;
namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// Wrapper class for a Zerodha websocket connection
    /// </summary>
    public class ZerodhaWebSocketWrapper : ZerodhaWebSocketClientWrapper
    {
        /// <summary>
        /// The unique Id for the connection
        /// </summary>
        public string ConnectionId { get; }

        /// <summary>
        /// The handler for the connection
        /// </summary>
        public IConnectionHandler ConnectionHandler { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerodhaWebSocketWrapper"/> class.
        /// </summary>
        public ZerodhaWebSocketWrapper(IConnectionHandler connectionHandler)
        {
            ConnectionId = Guid.NewGuid().ToString();
            ConnectionHandler = connectionHandler;
        }
    }
}
