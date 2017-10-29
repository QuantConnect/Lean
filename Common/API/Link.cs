using Newtonsoft.Json;

namespace QuantConnect.Api
{
    /// <summary>
    /// Response from reading purchased data
    /// </summary>
    public class Link : RestResponse
    {
        /// <summary>
        /// Link to the data
        /// </summary>
        [JsonProperty(PropertyName = "link")]
        public string DataLink { get; set; }
    }
}
