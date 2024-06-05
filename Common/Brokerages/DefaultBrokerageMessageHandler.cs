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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides a default implementation o <see cref="IBrokerageMessageHandler"/> that will forward
    /// messages as follows:
    /// Information -> IResultHandler.Debug
    /// Warning     -> IResultHandler.Error &amp;&amp; IApi.SendUserEmail
    /// Error       -> IResultHandler.Error &amp;&amp; IAlgorithm.RunTimeError
    /// </summary>
    public class DefaultBrokerageMessageHandler : IBrokerageMessageHandler
    {
        private static readonly TimeSpan DefaultOpenThreshold = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DefaultInitialDelay = TimeSpan.FromMinutes(15);

        private volatile bool _connected;

        private readonly IAlgorithm _algorithm;
        private readonly TimeSpan _openThreshold;
        private readonly TimeSpan _initialDelay;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageMessageHandler"/> class
        /// </summary>
        /// <param name="algorithm">The running algorithm</param>
        /// <param name="job">The job that produced the algorithm</param>
        /// <param name="api">The api for the algorithm</param>
        /// <param name="initialDelay"></param>
        /// <param name="openThreshold">Defines how long before market open to re-check for brokerage reconnect message</param>
        public DefaultBrokerageMessageHandler(IAlgorithm algorithm, TimeSpan? initialDelay = null, TimeSpan? openThreshold = null)
            : this(algorithm, null, null, initialDelay, openThreshold)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBrokerageMessageHandler"/> class
        /// </summary>
        /// <param name="algorithm">The running algorithm</param>
        /// <param name="job">The job that produced the algorithm</param>
        /// <param name="api">The api for the algorithm</param>
        /// <param name="initialDelay"></param>
        /// <param name="openThreshold">Defines how long before market open to re-check for brokerage reconnect message</param>
        public DefaultBrokerageMessageHandler(IAlgorithm algorithm, AlgorithmNodePacket job, IApi api, TimeSpan? initialDelay = null, TimeSpan? openThreshold = null)
        {
            _algorithm = algorithm;
            _connected = true;
            _openThreshold = openThreshold ?? DefaultOpenThreshold;
            _initialDelay = initialDelay ?? DefaultInitialDelay;
        }

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="message">The message to be handled</param>
        public void HandleMessage(BrokerageMessageEvent message)
        {
            // based on message type dispatch to result handler
            switch (message.Type)
            {
                case BrokerageMessageType.Information:
                    _algorithm.Debug(Messages.DefaultBrokerageMessageHandler.BrokerageInfo(message));
                    break;

                case BrokerageMessageType.Warning:
                    _algorithm.Error(Messages.DefaultBrokerageMessageHandler.BrokerageWarning(message));
                    break;

                case BrokerageMessageType.Error:
                    // unexpected error, we need to close down shop
                    _algorithm.SetRuntimeError(new Exception(message.Message),
                        Messages.DefaultBrokerageMessageHandler.BrokerageErrorContext);
                    break;

                case BrokerageMessageType.Disconnect:
                    _connected = false;
                    Log.Trace(Messages.DefaultBrokerageMessageHandler.Disconnected);

                    // check to see if any non-custom security exchanges are open within the next x minutes
                    var open = (from kvp in _algorithm.Securities
                                let security = kvp.Value
                                where security.Type != SecurityType.Base
                                let exchange = security.Exchange
                                let localTime = _algorithm.UtcTime.ConvertFromUtc(exchange.TimeZone)
                                where exchange.IsOpenDuringBar(
                                    localTime,
                                    localTime + _openThreshold,
                                    _algorithm.SubscriptionManager.SubscriptionDataConfigService
                                        .GetSubscriptionDataConfigs(security.Symbol)
                                        .IsExtendedMarketHours())
                                select security).Any();

                    // if any are open then we need to kill the algorithm
                    if (open)
                    {
                        Log.Trace(Messages.DefaultBrokerageMessageHandler.DisconnectedWhenExchangesAreOpen(_initialDelay));

                        // wait 15 minutes before killing algorithm
                        StartCheckReconnected(_initialDelay, message);
                    }
                    else
                    {
                        Log.Trace(Messages.DefaultBrokerageMessageHandler.DisconnectedWhenExchangesAreClosed);

                        // if they aren't open, we'll need to check again a little bit before markets open
                        DateTime nextMarketOpenUtc;
                        if (_algorithm.Securities.Count != 0)
                        {
                            nextMarketOpenUtc = (from kvp in _algorithm.Securities
                                                 let security = kvp.Value
                                                 where security.Type != SecurityType.Base
                                                 let exchange = security.Exchange
                                                 let localTime = _algorithm.UtcTime.ConvertFromUtc(exchange.TimeZone)
                                                 let marketOpen = exchange.Hours.GetNextMarketOpen(localTime,
                                                     _algorithm.SubscriptionManager.SubscriptionDataConfigService
                                                         .GetSubscriptionDataConfigs(security.Symbol)
                                                         .IsExtendedMarketHours())
                                                 let marketOpenUtc = marketOpen.ConvertToUtc(exchange.TimeZone)
                                                 select marketOpenUtc).Min();
                        }
                        else
                        {
                            // if we have no securities just make next market open an hour from now
                            nextMarketOpenUtc = DateTime.UtcNow.AddHours(1);
                        }

                        var timeUntilNextMarketOpen = nextMarketOpenUtc - DateTime.UtcNow - _openThreshold;
                        Log.Trace(Messages.DefaultBrokerageMessageHandler.TimeUntilNextMarketOpen(timeUntilNextMarketOpen));

                        // wake up 5 minutes before market open and check if we've reconnected
                        StartCheckReconnected(timeUntilNextMarketOpen, message);
                    }
                    break;

                case BrokerageMessageType.Reconnect:
                    _connected = true;
                    Log.Trace(Messages.DefaultBrokerageMessageHandler.Reconnected);

                    if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Cancel();
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles a new order placed manually in the brokerage side
        /// </summary>
        /// <param name="eventArgs">The new order event</param>
        /// <returns>Whether the order should be added to the transaction handler</returns>
        public bool HandleOrder(NewBrokerageOrderNotificationEventArgs eventArgs)
        {
            return false;
        }

        private void StartCheckReconnected(TimeSpan delay, BrokerageMessageEvent message)
        {
            _cancellationTokenSource.DisposeSafely();
            _cancellationTokenSource = new CancellationTokenSource(delay);

            Task.Run(() =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }

                CheckReconnected(message);

            }, _cancellationTokenSource.Token);
        }

        private void CheckReconnected(BrokerageMessageEvent message)
        {
            if (!_connected)
            {
                Log.Error(Messages.DefaultBrokerageMessageHandler.StillDisconnected);
                _algorithm.SetRuntimeError(new Exception(message.Message),
                    Messages.DefaultBrokerageMessageHandler.BrokerageDisconnectedShutDownContext);
            }
        }
    }
}
