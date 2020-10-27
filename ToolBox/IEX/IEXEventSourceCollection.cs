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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LaunchDarkly.EventSource;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.IEX
{
    /// <summary>
    /// Class wraps a collection of clients for getting data on SSE.
    /// SSE endpoints are limited to 50 symbols per connection. To consume more than 50 symbols we need multiple connections .
    /// </summary>
    public class IEXEventSourceCollection : IDisposable
    {
        private static readonly TimeSpan TimeoutToUpdate = TimeSpan.FromSeconds(10);
        private const int SymbolsPerConnectionLimit = 50;
        private readonly string _apiKey;
        private readonly EventHandler<MessageReceivedEventArgs> _messageAction;
        private static readonly ConcurrentDictionary<EventSource, string[]> ClientSymbolsDictionary = new ConcurrentDictionary<EventSource, string[]>();
        private static readonly CountdownEvent Counter = new CountdownEvent(1);
        private static readonly ManualResetEvent UpdateInProgressEvent = new ManualResetEvent(true);

        // IEX API documentation says:
        // "We limit requests to 100 per second per IP measured in milliseconds, so no more than 1 request per 10 milliseconds."
        private readonly RateGate _rateGate = new RateGate(1, TimeSpan.FromMilliseconds(10));

        /// <summary>
        /// Indicates whether a client is connected - i.e delivers any data.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="IEXEventSourceCollection"/>
        /// </summary>
        public IEXEventSourceCollection(EventHandler<MessageReceivedEventArgs> messageAction, string apiKey)
        {
            _messageAction = messageAction;
            _apiKey = apiKey;
        }

        /// <summary>
        /// Updates the data subscription to reflect the current user-subscribed symbols set.
        /// </summary>
        /// <param name="symbols">Symbols that user is currently subscribed to</param>
        /// <returns></returns>
        public void UpdateSubscription(string[] symbols)
        {
            if (!UpdateInProgressEvent.WaitOne(TimeoutToUpdate))
            {
                UpdateInProgressEvent.Set();
                throw new Exception("IEXEventSourceCollection.UpdateSubscription(): " +
                                    "The last UpdateSubscription was not successful, counter was not signaled on time.");
            }

            // Block the event until operation completes so if during current execution
            // the method will be run again, then it waited for the end of the current update
            UpdateInProgressEvent.Reset();
            
            var remainingSymbols = new List<string>(symbols);
            var clientsToRemove = new List<EventSource>();

            foreach (var kvp in ClientSymbolsDictionary)
            {
                var clientSymbols = kvp.Value;

                // Need to perform no changes if all client symbols are relevant
                if (clientSymbols.All(symbols.Contains))
                {
                    Log.Trace($"IEXEventSourceCollection.UpdateSubscription(): Leave unchanged subscription for: {string.Join(",", clientSymbols)}");

                    // Just remove symbols from remaining collection
                    remainingSymbols.RemoveAll(i => clientSymbols.Contains(i));

                    continue;
                }

                // Client has to be replaced
                clientsToRemove.Add(kvp.Key);
            }

            if (!remainingSymbols.Any())
            {
                throw new Exception("IEXEventSourceCollection.UpdateSubscription(): Invalid logic, remaining symbols can't be an empty set.");
            }

            // Group all remaining symbols in a smaller packages to comply with per-connection-limits
            var packagedSymbolsList = new List<string[]>();
            do
            {
                Counter.AddCount();

                if (remainingSymbols.Count > SymbolsPerConnectionLimit)
                {
                    var firstFifty = remainingSymbols.Take(SymbolsPerConnectionLimit).ToArray();
                    remainingSymbols.RemoveAll(i => firstFifty.Contains(i));
                    packagedSymbolsList.Add(firstFifty);
                }
                else
                {
                    // Add all remaining symbols as a last package
                    packagedSymbolsList.Add(remainingSymbols.ToArray());
                    break;
                }
            }
            while (remainingSymbols.Any());

            // Create new client for every package (make sure that we do not exceed the rate-gate-limit while creating)
            packagedSymbolsList.DoForEach(package =>
            {
                _rateGate.WaitToProceed();
                var client = CreateNewSubscription(package);
                // This message handler should be called once only
                client.MessageReceived += MessageHandler;
            });

            // Called for example when UpdateSubscription is called for the first time
            if (!clientsToRemove.Any())
            {
                UpdateInProgressEvent.Set();
                return;
            }

            // If there are clients to remove - wait for the counter to reset to zero and remove
            Task.Run(() =>
            {
                Counter.Signal();
                if (!Counter.Wait(TimeoutToUpdate))
                {
                    throw new Exception("IEXEventSourceCollection.UpdateSubscription(): Could not update subscription within a timeout");
                }

                clientsToRemove.DoForEach(i =>
                {
                    Log.Trace($"IEXEventSourceCollection.UpdateSubscription(): Remove subscription for: {string.Join(",", ClientSymbolsDictionary[i])}");

                    string[] stub;
                    ClientSymbolsDictionary.TryRemove(i, out stub);

                    i.Close();
                    i.Dispose();
                });

                // Reset synchronization objects
                Counter.Reset(1);
                UpdateInProgressEvent.Set();

                IsConnected = true;

            }).ContinueWith(t =>
            {
                if (t.Exception != null)
                    throw t.Exception;

            },TaskContinuationOptions.OnlyOnFaulted);
        }

        // Once we received the first message, decrement the counter
        // We will remove old clients once the counter is set to 'zero' -
        // i.e. when we can make sure that all new streams are receiving data and can replace the old ones
        private static void MessageHandler(object sender, MessageReceivedEventArgs args)
        {
            var tmpClient = sender as EventSource;
            if (tmpClient == null)
            {
                throw new InvalidCastException("Invalid cast in IEXEventSourceCollection.MessageHandler()");
            }

            Counter.Signal();
            Log.Trace($"IEXEventSourceCollection.MessageHandler(): CountdownEvent count: {Counter.CurrentCount}");
            tmpClient.MessageReceived -= MessageHandler;  // Remove the handler
        }

        private EventSource CreateNewSubscription(string[] symbols)
        {
            // Build an Uri, create a client
            var url = BuildUrlString(symbols);
            var client = new EventSource(LaunchDarkly.EventSource.Configuration.Builder(new Uri(url)).Build());

            // Add to the dictionary
            ClientSymbolsDictionary.TryAdd(client, symbols);

            Log.Trace($"IEXEventSourceCollection.CreateNewSubscription(): Creating subscription for: {string.Join(",", symbols)}");

            // Set up the handlers
            client.Opened += (sender, args) => { };

            client.MessageReceived += _messageAction;

            client.Error += (sender, args) =>
            {
                var exception = args.Exception;
                Log.Trace($"ClientOnError(): EventSource Error Occurred. Details: {exception.Message} " +
                          $"ErrorType: {exception.GetType().FullName}", true);
            };

            client.Closed += (sender, args) =>
            {
                Log.Trace("ClientOnClosed(): Closing a client", true);
            };

            // Client start call will block until Stop() is called (!) - runs continuously in a background
            Task.Run(async () => await client.StartAsync());

            return client;
        }

        private string BuildUrlString(IEnumerable<string> symbols)
        {
            var url = "https://cloud-sse.iexapis.com/stable/stocksUSNoUTP?token=" + _apiKey;
            url += "&symbols=" + string.Join(",", symbols);
            return url;
        }

        public void Dispose()
        {
            foreach (var client in ClientSymbolsDictionary.Keys)
            {
                client.Close();
                client.Dispose();
            }

            IsConnected = false;
        }
    }
}
