using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using RestSharp;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// UserData Session class used to manage listen key
    /// </summary>
    public partial class BinanceBrokerage
    {
        private Task _runningTask;
        private CancellationTokenSource _userDataCancellationTokenSource;
        private Thread _userDataMonitorThread;
        private object _listenKeyLocker = new object();
        private DateTime UserDataLastHeartbeatUtcTime;
        private string userDataStreamEndpoint = "/api/v1/userDataStream";

        /// <summary>
        /// Represents UserData Session listen key
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>
        /// Starts the session
        /// </summary>
        public void StartSession()
        {
            CreateListenKey();

            _userDataCancellationTokenSource = new CancellationTokenSource();
            _userDataMonitorThread = new Thread(() =>
            {
                var nextReconnectionAttemptUtcTime = DateTime.UtcNow;
                double nextReconnectionAttemptSeconds = 1;
                UserDataLastHeartbeatUtcTime = DateTime.UtcNow;
                double recommendedPingIntervalInSec = 15;
                bool authorized = true;

                try
                {
                    while (!_userDataCancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            TimeSpan elapsed = DateTime.UtcNow - UserDataLastHeartbeatUtcTime;
                            if (authorized && elapsed > TimeSpan.FromSeconds(recommendedPingIntervalInSec))
                            {
                                if (!SessionKeepAlive())
                                {
                                    authorized = false;
                                    nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);

                                    OnMessage(BrokerageMessageEvent.Disconnected("UserData Stream listen key has been expired or deleted."));
                                }
                                UserDataLastHeartbeatUtcTime = DateTime.UtcNow;
                            }
                            else if (!authorized)
                            {
                                try
                                {
                                    if (DateTime.UtcNow > nextReconnectionAttemptUtcTime)
                                    {
                                        try
                                        {
                                            CreateListenKey();
                                            ConnectStream();
                                            authorized = true;
                                        }
                                        catch (Exception err)
                                        {
                                            // double the interval between attempts (capped to 1 minute)
                                            nextReconnectionAttemptSeconds = Math.Min(nextReconnectionAttemptSeconds * 2, 60);
                                            nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);
                                            Log.Error(err);
                                        }
                                    }
                                }
                                catch (Exception exception)
                                {
                                    Log.Error(exception);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Log.Error(exception);
                        }

                        Thread.Sleep(10000);
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                }
            })
            { IsBackground = true };
            _userDataMonitorThread.Start();
            while (!_userDataMonitorThread.IsAlive)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Check User Data stream listen key is alive
        /// </summary>
        /// <returns></returns>
        private bool SessionKeepAlive()
        {
            if (string.IsNullOrEmpty(SessionId))
            {
                throw new Exception("BinanceBrokerage:UserStream. listenKey wasn't allocated or has been refused.");
            }

            var ping = new RestRequest(userDataStreamEndpoint, Method.PUT);
            ping.AddHeader(KeyHeader, ApiKey);
            ping.AddParameter(
                "application/x-www-form-urlencoded",
                Encoding.UTF8.GetBytes($"listenKey={SessionId}"),
                ParameterType.RequestBody
            );

            var pong = ExecuteRestRequest(ping);
            return pong.StatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// Stops the session
        /// </summary>
        public void StopSession()
        {
            _userDataCancellationTokenSource?.Cancel();
            _userDataMonitorThread?.Join();

            if (!string.IsNullOrEmpty(SessionId))
            {
                var request = new RestRequest(userDataStreamEndpoint, Method.DELETE);
                request.AddHeader(KeyHeader, ApiKey);
                request.AddParameter(
                    "application/x-www-form-urlencoded",
                    Encoding.UTF8.GetBytes($"listenKey={SessionId}"),
                    ParameterType.RequestBody
                );
                ExecuteRestRequest(request);
            }
        }


        private void CreateListenKey()
        {
            var request = new RestRequest(userDataStreamEndpoint, Method.POST);
            request.AddHeader(KeyHeader, ApiKey);

            var response = ExecuteRestRequest(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.StartSession: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var content = JObject.Parse(response.Content);
            lock (_listenKeyLocker)
            {
                SessionId = content.Value<string>("listenKey");
            }
        }
    }
}