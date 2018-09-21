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
            var request = new RestRequest(userDataStreamEndpoint, Method.POST);
            request.AddHeader(KeyHeader, ApiKey);

            var response = ExecuteRestRequest(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.StartSession: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var content = JObject.Parse(response.Content);
            SessionId = content.Value<string>("listenKey");

            _userDataCancellationTokenSource = new CancellationTokenSource();
            _userDataMonitorThread = new Thread(() =>
            {
                UserDataLastHeartbeatUtcTime = DateTime.UtcNow;
                double recommendedPingIntervalInMin = 30;
                var ping = new RestRequest(userDataStreamEndpoint, Method.PUT);
                ping.AddHeader(KeyHeader, ApiKey);
                ping.AddParameter(
                    "application/x-www-form-urlencoded",
                    Encoding.UTF8.GetBytes($"listenKey={SessionId}"),
                    ParameterType.RequestBody
                );

                try
                {
                    while (!_userDataCancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            TimeSpan elapsed = DateTime.UtcNow - UserDataLastHeartbeatUtcTime;
                            if (elapsed > TimeSpan.FromMinutes(recommendedPingIntervalInMin))
                            {
                                var pong = ExecuteRestRequest(ping);

                                if (pong.StatusCode != HttpStatusCode.OK)
                                {
                                    throw new Exception($"BinanceBrokerage.KeepAlive: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
                                }
                                UserDataLastHeartbeatUtcTime = DateTime.UtcNow;
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
        /// Stops the session
        /// </summary>
        public void StopSession()
        {
            _userDataCancellationTokenSource?.Cancel();
            _userDataMonitorThread?.Join();

            var ping = new RestRequest(userDataStreamEndpoint, Method.DELETE);
            ping.AddHeader(KeyHeader, ApiKey);
            ping.AddParameter(
                "application/x-www-form-urlencoded",
                Encoding.UTF8.GetBytes($"listenKey={SessionId}"),
                ParameterType.RequestBody
            );
        }
    }
}