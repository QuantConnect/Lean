using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using QuantConnect.Api;
using QuantConnect.Orders;
using QuantConnect.Packets;

namespace QuantConnect.API
{
    /// <summary>
    /// Details a live algorithm from the "live/read" Api endpoint
    /// </summary>
    public class LiveAlgorithmResults : RestResponse
    {
        /// <summary>
        /// Represents data about the live running algorithm returned from the server
        /// </summary>
        public LiveResultsData LiveResults { get; set; }
    }

    /// <summary>
    /// Holds information about the state and operation of the live running algorithm
    /// </summary>
    public class LiveResultsData
    {
        /// <summary>
        /// Results version
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        /// <summary>
        /// Temporal resolution of the results returned from the Api
        /// </summary>
        [JsonProperty(PropertyName = "resolution"), JsonConverter(typeof(StringEnumConverter))]
        public Resolution Resolution { get; set; }

        /// <summary>
        /// Class to represent the data groups results return from the Api
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public LiveResult Results { get; set; }
    }
}
