using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    public class CrunchDAOSignalExport : ISignalExportTarget
    {
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly string _submissionName;
        private readonly string _comment;
        private const string _destination = "https://api.tournament.crunchdao.com/v3/alpha-submissions";
        private static HttpClient _client;

        public CrunchDAOSignalExport(string apiKey, string modelName, string submissionName = "", string comment = "")
        {
            _apiKey = apiKey;
            _modelName = modelName;
            _submissionName = submissionName;
            _comment = comment;
            _client = new HttpClient();
        }


        public string Send(List<PortfolioTarget> holdings)
        {
            
        }
    }
}
