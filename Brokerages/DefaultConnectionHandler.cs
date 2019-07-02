/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Threading;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// A default implementation of <see cref="IConnectionHandler"/>
    /// which signals disconnection if no data is received for a given time span
    /// and attempts to reconnect automatically.
    /// </summary>
    public class DefaultConnectionHandler : IConnectionHandler
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Thread _connectionMonitorThread;

        private bool _isEnabled;
        private readonly object _lockerConnectionMonitor = new object();
        private volatile bool _connectionLost;
        private DateTime _lastDataReceivedTime;

        /// <summary>
        /// Event that fires when a connection loss is detected
        /// </summary>
        public event EventHandler ConnectionLost;

        /// <summary>
        /// Event that fires when a lost connection is restored
        /// </summary>
        public event EventHandler ConnectionRestored;

        /// <summary>
        /// Event that fires when a reconnection attempt is required
        /// </summary>
        public event EventHandler ReconnectRequested;

        /// <summary>
        /// The elapsed time with no received data after which a connection loss is reported
        /// </summary>
        public TimeSpan MaximumIdleTimeSpan { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The minimum time in seconds to wait before attempting to reconnect
        /// </summary>
        public int MinimumSecondsForNextReconnectionAttempt { get; set; } = 1;

        /// <summary>
        /// The maximum time in seconds to wait before attempting to reconnect
        /// </summary>
        public int MaximumSecondsForNextReconnectionAttempt { get; set; } = 60;

        /// <summary>
        /// The unique Id for the connection
        /// </summary>
        public string ConnectionId { get; private set; }

        /// <summary>
        /// Initializes the connection handler
        /// </summary>
        /// <param name="connectionId">The connection id</param>
        public void Initialize(string connectionId)
        {
            ConnectionId = connectionId;

            var waitHandle = new ManualResetEvent(false);

            _cancellationTokenSource = new CancellationTokenSource();

            _connectionMonitorThread = new Thread(() =>
            {
                waitHandle.Set();

                var nextReconnectionAttemptUtcTime = DateTime.UtcNow;
                var nextReconnectionAttemptSeconds = MinimumSecondsForNextReconnectionAttempt;

                lock (_lockerConnectionMonitor)
                {
                    _lastDataReceivedTime = DateTime.UtcNow;
                }

                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        Thread.Sleep(1000);

                        if (!_isEnabled) continue;

                        try
                        {
                            TimeSpan elapsed;
                            lock (_lockerConnectionMonitor)
                            {
                                elapsed = DateTime.UtcNow - _lastDataReceivedTime;
                            }

                            if (!_connectionLost && elapsed > MaximumIdleTimeSpan)
                            {
                                _connectionLost = true;
                                nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);

                                OnConnectionLost();
                            }
                            else if (_connectionLost)
                            {
                                if (elapsed <= MaximumIdleTimeSpan)
                                {
                                    _connectionLost = false;
                                    nextReconnectionAttemptSeconds = MinimumSecondsForNextReconnectionAttempt;

                                    OnConnectionRestored();
                                }
                                else
                                {
                                    if (DateTime.UtcNow > nextReconnectionAttemptUtcTime)
                                    {
                                        // double the interval between attempts (capped to 1 minute)
                                        nextReconnectionAttemptSeconds = Math.Min(nextReconnectionAttemptSeconds * 2, MaximumSecondsForNextReconnectionAttempt);
                                        nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);

                                        OnReconnectRequested();
                                    }
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Log.Error($"Error in DefaultConnectionHandler: {exception}");
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                }
            }) { IsBackground = true };

            _connectionMonitorThread.Start();

            waitHandle.WaitOne();
        }

        /// <summary>
        /// Enables/disables monitoring of the connection
        /// </summary>
        /// <param name="isEnabled">True to enable monitoring, false otherwise</param>
        public void EnableMonitoring(bool isEnabled)
        {
            // if we are switching to enabled, initialize the last data received time
            if (!_isEnabled && isEnabled)
            {
                KeepAlive(DateTime.UtcNow);
            }

            _isEnabled = isEnabled;
        }

        /// <summary>
        /// Notifies the connection handler that new data was received
        /// </summary>
        /// <param name="lastDataReceivedTime">The UTC timestamp of the last data point received</param>
        public void KeepAlive(DateTime lastDataReceivedTime)
        {
            lock (_lockerConnectionMonitor)
            {
                _lastDataReceivedTime = lastDataReceivedTime;
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="ConnectionLost"/> event
        /// </summary>
        protected virtual void OnConnectionLost()
        {
            ConnectionLost?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event invocator for the <see cref="ConnectionRestored"/> event
        /// </summary>
        protected virtual void OnConnectionRestored()
        {
            ConnectionRestored?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event invocator for the <see cref="ReconnectRequested"/> event
        /// </summary>
        protected virtual void OnReconnectRequested()
        {
            ReconnectRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _isEnabled = false;

            // request and wait for thread to stop
            _cancellationTokenSource?.Cancel();
            _connectionMonitorThread?.Join();
        }
    }
}
