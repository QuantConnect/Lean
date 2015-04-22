using System;
using QuantConnect.Logging;
using RestSharp;

namespace QuantConnect.Lean.Engine
{
    public class RestSubscriptionStreamReader : IStreamReader
    {
        private readonly RestClient _client;
        private readonly RestRequest _request;

        public RestSubscriptionStreamReader(string source)
        {
            _client = new RestClient(source);
            _request = new RestRequest(Method.GET);
        }

        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.Rest; }
        }

        public bool EndOfStream
        {
            get { return false; }
        }

        public string ReadLine()
        {
            try
            {
                var response = _client.Execute(_request);
                if (response != null)
                {
                    return response.Content;
                }
            }
            catch (Exception err)
            {
                Log.Error("RestSubscriptionStreamReader.ReadLine(): " + err.Message);
            }

            return string.Empty;
        }

        public void Close()
        {
        }

        public void Dispose()
        {
        }
    }
}