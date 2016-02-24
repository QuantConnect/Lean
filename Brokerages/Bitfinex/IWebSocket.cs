using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Wrapper for WebSocketSharp to enhance testability
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
        /// Wraps message event handler setter
        /// </summary>
        /// <param name="handler"></param>
        void OnMessage(EventHandler<WebSocketSharp.MessageEventArgs> handler);

        /// <summary>
        /// Wraps Close method
        /// </summary>
        void Close();

        /// <summary>
        /// Wraps IsAlive
        /// </summary>
        bool IsAlive { get; }

    }
}
