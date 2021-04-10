using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.AlphaVantageDownloader
{
    class AlphaVantageAuthenticator : IAuthenticator
    {
        private readonly string _apiKey;

        public AlphaVantageAuthenticator(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _apiKey = apiKey;
        }

        public void Authenticate(IRestClient client, IRestRequest request)
            => request.AddParameter("apikey", _apiKey);
    }
}
