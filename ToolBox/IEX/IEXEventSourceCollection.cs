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
        private static readonly TimeSpan TimeoutToUpdate = TimeSpan.FromSeconds(30);
        private const int SymbolsPerConnectionLimit = 50;
        private readonly string _apiKey;
        private readonly EventHandler<MessageReceivedEventArgs> _messageAction;
        private readonly ConcurrentDictionary<EventSource, string[]> _clientSymbolsDictionary = new ConcurrentDictionary<EventSource, string[]>();
        private readonly CountdownEvent _counter = new CountdownEvent(1);

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
            Log.Debug("IEXEventSourceCollection.UpdateSubscription(): Subscription update started");

            var remainingSymbols = new List<string>(symbols);
            var clientsToRemove = new List<EventSource>();

            foreach (var kvp in _clientSymbolsDictionary)
            {
                var clientSymbols = kvp.Value;

                // Need to perform no changes if all client symbols are relevant
                if (clientSymbols.All(symbols.Contains))
                {
                    Log.Debug($"IEXEventSourceCollection.UpdateSubscription(): Leave unchanged subscription for: {string.Join(",", clientSymbols)}");

                    // Just remove symbols from collection of remaining
                    remainingSymbols.RemoveAll(i => clientSymbols.Contains(i));
                    continue;
                }

                clientsToRemove.Add(kvp.Key);
            }

            Log.Debug($"IEXEventSourceCollection.UpdateSubscription(): {clientsToRemove.Count} old clients to remove");

            if (!remainingSymbols.Any())
            {
                throw new Exception("IEXEventSourceCollection.UpdateSubscription(): Invalid logic, remaining symbols can't be an empty set.");
            }

            // Group all remaining symbols in a smaller packages to comply with per-connection-limits
            var packagedSymbolsList = new List<string[]>();
            do
            {
                _counter.AddCount();  // Increment

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

                Log.Debug($"IEXEventSourceCollection.CreateNewSubscription(): Creating new subscription for: {string.Join(",", package)}");
                CreateNewSubscription(package);
            });

            _counter.Signal();
            if (!_counter.Wait(TimeoutToUpdate))
            {
                throw new Exception("IEXEventSourceCollection.UpdateSubscription(): Could not update subscription within a timeout");
            }

            clientsToRemove.DoForEach(i =>
            {
                Log.Debug($"IEXEventSourceCollection.UpdateSubscription(): Remove subscription for: {string.Join(",", _clientSymbolsDictionary[i])}");

                string[] stub;
                _clientSymbolsDictionary.TryRemove(i, out stub);

                i.Close();
                i.Dispose();
            });

            // Reset counter
            _counter.Reset(1);

            IsConnected = true;
        }

        private EventSource CreateNewSubscription(string[] symbols)
        {
            // Build an Uri, create a client
            var url = BuildUrlString(symbols);
            var client = new EventSource(LaunchDarkly.EventSource.Configuration.Builder(new Uri(url)).Build());

            // Add to the dictionary
            _clientSymbolsDictionary.TryAdd(client, symbols);

            // Set up the handlers
            client.Opened += (sender, args) =>
            {
                _counter.Signal();   // Decrement
                Log.Debug($"ClientOnOpened(): Counter count after decrement: {_counter.CurrentCount}");
            };

            client.MessageReceived += _messageAction;

            client.Error += (sender, args) =>
            {
                var exception = args.Exception;
                Log.Debug($"ClientOnError(): EventSource Error Occurred. Details: {exception.Message} " +
                          $"ErrorType: {exception.GetType().FullName}");
            };

            client.Closed += (sender, args) =>
            {
                Log.Debug("ClientOnClosed(): Closing a client");
            };

            // Client start call will block until Stop() is called (!) - runs continuously in a background
            Task.Run(async () => await client.StartAsync().ConfigureAwait(false));

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
            foreach (var client in _clientSymbolsDictionary.Keys)
            {
                client.Close();
                client.Dispose();
            }

            IsConnected = false;
        }
    }
}
