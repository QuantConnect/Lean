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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca
{
    internal class PricingStreamSession
    {
        private AlpacaApiBase _api;
        private List<string> _instruments;

        private bool _shutdown;
        private Task _runningTask;
        private Markets.NatsClient _client;

        /// <summary>
        /// The event fired when a new quote message is received
        /// </summary>
        public event Action<Markets.IStreamQuote> QuoteReceived;

        /// <summary>
        /// The event fired when a new quote message is received
        /// </summary>
        public event Action<Markets.IStreamTrade> TradeReceived;

        public PricingStreamSession(AlpacaApiBase alpacaApiBase, List<string> instruments)
        {
            this._api = alpacaApiBase;
            this._instruments = instruments;
        }


        public void StartSession()
        {
            _shutdown = false;

            _client = _api.GetNatsClient();

            _runningTask = Task.Run(() =>
            {
                _client.Open();
                _client.QuoteReceived += QuoteReceived;
                _client.TradeReceived += TradeReceived;
                foreach (var instrument in _instruments)
                {
                    _client.SubscribeQuote(instrument);
                    _client.SubscribeTrade(instrument);
                }
                while (!_shutdown)
                {
                    Thread.Sleep(1);
                }
            });
        }

        /// <summary>
        /// Stops the session
        /// </summary>
        public void StopSession()
        {
            _shutdown = true;

            try
            {
                // wait for task to finish
                if (_runningTask != null)
                {
                    _runningTask.Wait();
                }
            }
            catch (Exception)
            {
                // we can get here if the socket has been closed (i.e. after a long disconnection)
            }

            try
            {
                _client.Close();
                _client.Dispose();
            }
            catch (Exception)
            {
                // we can get here if the socket has been closed (i.e. after a long disconnection)
            }
        }
    }
}