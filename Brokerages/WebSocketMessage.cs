namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Defines a message received at a web socket
    /// </summary>
    public class WebSocketMessage
    {
        /// <summary>
        /// Gets the raw message data as text
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketMessage"/> class
        /// </summary>
        /// <param name="message">The message</param>
        public WebSocketMessage(string message)
        {
            Message = message;
        }
    }
}