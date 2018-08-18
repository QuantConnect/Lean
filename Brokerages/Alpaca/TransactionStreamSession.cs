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
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca
{
	internal class TransactionStreamSession
	{
		private AlpacaApiBase _api;
		private bool _shutdown;
		private Task _runningTask;
		private Markets.SockClient _client;

		/// <summary>
		/// The event fired when a new message is received
		/// </summary>
		public event Action<Markets.ITradeUpdate> TradeReceived;

		public TransactionStreamSession(AlpacaApiBase alpacaApiBase)
		{
			this._api = alpacaApiBase;
		}

		public void StartSession()
		{
			_shutdown = false;

			_client = _api.GetSockClient();

			_runningTask = Task.Run(() =>
			{
				_client.ConnectAsync();
				_client.OnTradeUpdate += TradeReceived;
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
				_client.DisconnectAsync().Wait();
				_client.Dispose();
			}
			catch (Exception)
			{
				// we can get here if the socket has been closed (i.e. after a long disconnection)
			}
		}
	}
}