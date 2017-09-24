/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2012-2013 OANDA Corporation
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
 * documentation files (the "Software"), to deal in the Software without restriction, including without 
 * limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
 * Software, and to permit persons to whom the Software is furnished  to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
 * the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.RestV1.DataType;

namespace QuantConnect.Brokerages.Oanda.RestV1.Session
{
#pragma warning disable 1591
    /// <summary>
    /// StreamSession abstract class used to model the Oanda Events Sessions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class StreamSession<T> where T : IHeartbeat
    {
        public delegate void DataHandler(T data);

        protected readonly string _accountId;
        private WebResponse _response;
        private bool _shutdown;
        private Task _runningTask;

        protected StreamSession(string accountId)
        {
            _accountId = accountId;
        }

        public event DataHandler DataReceived;

        public void OnDataReceived(T data)
        {
            var handler = DataReceived;
            if (handler != null) handler(data);
        }

        protected abstract WebResponse GetSession();

        public void StartSession()
        {
            _shutdown = false;
            _response = GetSession();

            _runningTask = Task.Run(() =>
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                var reader = new StreamReader(_response.GetResponseStream());
                while (!_shutdown)
                {
                    var memStream = new MemoryStream();

                    var line = reader.ReadLine();
                    memStream.Write(Encoding.UTF8.GetBytes(line), 0, Encoding.UTF8.GetByteCount(line));
                    memStream.Position = 0;

                    var data = (T) serializer.ReadObject(memStream);

                    OnDataReceived(data);
                }
            });
        }

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
#pragma warning restore 1591
}