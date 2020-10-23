using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Securities.Future
{
    public class CMEProductSlateV2ListResponse
    {
        [JsonProperty("products")]
        public List<CMEProductSlateV2ListEntry> Products { get; private set; }
    }

    public class CMEProductSlateV2ListEntry
    {
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("clearing")]
        public string Clearing { get; private set; }

        [JsonProperty("globex")]
        public string Globex { get; private set; }

        [JsonProperty("globexTraded")]
        public bool GlobexTraded { get; private set; }

        [JsonProperty("cpc")]
        public string CPC { get; private set; }

        [JsonProperty("venues")]
        public string Venues { get; private set; }

        [JsonProperty("cleared")]
        public string Cleared { get; private set; }

        [JsonProperty("exch")]
        public string Exchange { get; private set; }

        [JsonProperty("groupId")]
        public int GroupId { get; private set; }

        [JsonProperty("subGroupId")]
        public int subGroupId { get; private set; }
    }
}
