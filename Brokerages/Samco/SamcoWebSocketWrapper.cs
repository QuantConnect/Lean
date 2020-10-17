using System;
namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// Wrapper class for a Samco websocket connection
    /// </summary>
    public class SamcoWebSocketWrapper : SamcoWebSocketClientWrapper
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
        /// Initializes a new instance of the <see cref="SamcoWebSocketWrapper"/> class.
        /// </summary>
        public SamcoWebSocketWrapper(IConnectionHandler connectionHandler)
        {
            ConnectionId = Guid.NewGuid().ToString();
            ConnectionHandler = connectionHandler;
        }
    }
}
