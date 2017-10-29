using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Api;

namespace QuantConnect.API
{
    /// <summary>
    /// Logs from a live algorithm
    /// </summary>
    public class LiveLog : RestResponse
    {
        /// <summary>
        /// List of logs from the live algorithm
        /// </summary>
        [JsonProperty(PropertyName = "LiveLogs")]
        public List<string> Logs { get; set; }
    }
}
