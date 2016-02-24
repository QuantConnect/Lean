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
    public class WebSocketWrapper : IWebSocket
    {

        WebSocket wrapped;

        /// <summary>
        /// Wraps constructor
        /// </summary>
        /// <param name="url"></param>
        public void Initialize(string url)
        {
            wrapped = new WebSocket(url);
        }

        /// <summary>
        /// Wraps send method
        /// </summary>
        /// <param name="data"></param>
        public void Send(string data)
        {
            wrapped.Send(data);
        }

        /// <summary>
        /// Wraps Connect method
        /// </summary>
        public void Connect()
        {
            wrapped.Connect();
        }

        /// <summary>
        /// Wraps message event handler setter
        /// </summary>
        /// <param name="handler"></param>
        public void OnMessage(EventHandler<WebSocketSharp.MessageEventArgs> handler)
        {
            wrapped.OnMessage += handler;
        }

        /// <summary>
        /// Wraps Close method
        /// </summary>
        public void Close()
        {
            wrapped.Close();
        }

        /// <summary>
        /// Wraps IsAlive
        /// </summary>
        public bool IsAlive
        {
            get { return wrapped.IsAlive; }
        }
    }
}
