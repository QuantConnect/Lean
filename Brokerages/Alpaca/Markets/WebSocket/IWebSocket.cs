/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made from original:
 *   - Removed Nullable reference type definitions for compatibility with C# 6
*/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulate logic required for communicating with web socket server from API.
    /// </summary>
    public interface IWebSocket : IDisposable
    {
        /// <summary>
        /// Opens web socket communication channel. Connection state changes will be reported
        /// using <see cref="Opened"/> event and errors - using <see cref="Error"/> event.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Connection opening task for awaiting (if needed).</returns>
        Task OpenAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Closes web socket communication channel. Connection state changes will be reported
        /// using <see cref="Closed"/> event and errors - using <see cref="Error"/> event.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Connection closing task for awaiting (if needed).</returns>
        Task CloseAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Sends text message into opened web socket connection.
        /// </summary>
        /// <param name="message"></param>
        void Send(
            String message);

        /// <summary>
        /// Occurred after successful web socket connection (at protocol level).
        /// </summary>
        event Action Opened;

        /// <summary>
        /// Occurred after successful web socket disconnection (at protocol level).
        /// </summary>
        event Action Closed;

        /// <summary>
        /// Occurred on each new completed web socket message receiving data or text.
        /// </summary>
        event Action<Byte[]> DataReceived;

        /// <summary>
        /// Occurred on each new completed web socket message receiving text.
        /// </summary>
        event Action<String> MessageReceived;

        /// <summary>
        /// Occurred in case of any communication errors (on opening/close/listening/send).
        /// </summary>
        [SuppressMessage(
            "Naming", "CA1716:Identifiers should not match keywords",
            Justification = "Already used by clients and creates conflict only in VB.NET")]
        event Action<Exception> Error;
    }
}
