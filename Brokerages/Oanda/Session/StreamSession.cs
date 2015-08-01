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
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.DataType;

namespace QuantConnect.Brokerages.Oanda.Session
{
    /// <summary>
    /// StreamSession abstract class used to model the Oanda Events Sessions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class StreamSession<T> where T : IHeartbeat
    {
        public delegate void DataHandler(T data);

        protected readonly int _accountId;
        private WebResponse _response;
        private bool _shutdown;

        protected StreamSession(int accountId)
        {
            _accountId = accountId;
        }

        public event DataHandler DataReceived;

        public void OnDataReceived(T data)
        {
            var handler = DataReceived;
            if (handler != null) handler(data);
        }

        protected abstract Task<WebResponse> GetSession();

        public async void StartSession()
        {
            _shutdown = false;
            _response = await GetSession();


            Task.Run(() =>
            {
                var serializer = new DataContractJsonSerializer(typeof (T));
                var reader = new StreamReader(_response.GetResponseStream());
                while (!_shutdown)
                {
                    var memStream = new MemoryStream();

                    var line = reader.ReadLine();
                    memStream.Write(Encoding.UTF8.GetBytes(line), 0, Encoding.UTF8.GetByteCount(line));
                    memStream.Position = 0;

                    var data = (T) serializer.ReadObject(memStream);

                    // Don't send heartbeats
                    if (!data.IsHeartbeat())
                    {
                        OnDataReceived(data);
                    }
                }
            }
                );
        }

        public void StopSession()
        {
            _shutdown = true;
        }
    }
}