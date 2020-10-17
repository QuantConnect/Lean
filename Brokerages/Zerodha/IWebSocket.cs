using System;

namespace QuantConnect.Brokerages.Zerodha
{

    /// <summary>
    /// Wrapper for WebSocket to enhance testability
    /// </summary>
    public interface IWebSocket
    {

        /// <summary>
        /// Wraps constructor
        /// </summary>
        /// <param name="url"></param>
        void Initialize(string url);

        /// <summary>
        /// Wraps send method
        /// </summary>
        /// <param name="data"></param>
        void Send(string data);

        /// <summary>
        /// Wraps Connect method
        /// </summary>
        void Connect();

        /// <summary>
        /// Wraps Close method
        /// </summary>
        void Close();

        /// <summary>
        /// Wraps IsOpen
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// on message event
        /// </summary>
        event EventHandler<MessageData> Message;

        /// <summary>
        /// On error event
        /// </summary>
        event EventHandler<WebSocketError> Error;

        /// <summary>
        /// On Open event
        /// </summary>
        event EventHandler Open;

        /// <summary>
        /// On Close event
        /// </summary>
        event EventHandler<WebSocketCloseData> Closed;

    }
}
