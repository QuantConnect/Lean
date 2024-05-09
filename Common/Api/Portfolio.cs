using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Securities;

namespace QuantConnect.Api
{
    public class Portfolio
    {
        /// <summary>
        /// Dictionary of algorithm holdings information
        /// </summary>
        [JsonProperty(PropertyName = "Holdings")]
        public Dictionary<string, Holding> Holdings { get; set; }

        /// <summary>
        /// Dictionary where each value represents a holding of a currency in cash, which is the key
        /// </summary>
        [JsonProperty(PropertyName = "Cash")]
        public Dictionary<string, Cash> Cash { get; set; }
    }

    public class PortfolioResponse : RestResponse
    {
        [JsonProperty(PropertyName = "portfolio")]
        public Portfolio Portfolio { get; set; }
    }
}
