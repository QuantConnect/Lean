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
        private object _listenKeyLocker = new object();
        private string userDataStreamEndpoint = "/api/v1/userDataStream";

        /// <summary>
        /// Represents UserData Session listen key
        /// </summary>
        public string SessionId { get; private set; }

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