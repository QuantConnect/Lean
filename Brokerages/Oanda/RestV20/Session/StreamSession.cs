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
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Oanda.RestV20.Session
{
    /// <summary>
    /// StreamSession abstract class used to model streaming sessions
    /// </summary>
    public abstract class StreamSession
    {
        private WebResponse _response;
        private bool _shutdown;
        private Task _runningTask;

        /// <summary>
        /// The delegate for the DataReceived event handler
        /// </summary>
        public delegate void DataHandler(string data);

        /// <summary>
        /// The event fired when a new message is received
        /// </summary>
        public event DataHandler DataReceived;

        /// <summary>
        /// Returns the started session
        /// </summary>
        protected abstract WebResponse GetSession();

        /// <summary>
        /// Starts the session
        /// </summary>
        public void StartSession()
        {
            _shutdown = false;

            _response = GetSession();

            _runningTask = Task.Run(() =>
            {
                using (var reader = new StreamReader(_response.GetResponseStream()))
                {
                    while (!_shutdown)
                    {
                        var line = reader.ReadLine();

                        var handler = DataReceived;
                        if (handler != null) handler(line);
                    }
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
                // close and dispose of previous session
                var httpResponse = _response as HttpWebResponse;
                if (httpResponse != null)
                {
                    httpResponse.Close();
                    httpResponse.Dispose();
                }
            }
            catch (Exception)
            {
                // we can get here if the socket has been closed (i.e. after a long disconnection)
            }
        }
    }
}