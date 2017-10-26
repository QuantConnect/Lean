using System;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Defines data returned from a web socket error
    /// </summary>
    public class WebSocketError
    {
        /// <summary>
        /// Gets the message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the exception raised
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketError"/> class
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="exception">The error</param>
        public WebSocketError(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}