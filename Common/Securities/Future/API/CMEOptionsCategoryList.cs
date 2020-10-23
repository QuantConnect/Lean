using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Securities.Future
{
    public class CMEOptionsCategoryListEntry
    {
        [JsonProperty("label")]
        public string Label { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("optionType")]
        public string OptionType { get; private set; }

        [JsonProperty("daily")]
        public bool Daily { get; private set; }

        [JsonProperty("sto")]
        public bool Sto { get; private set; }

        [JsonProperty("weekly")]
        public bool Weekly { get; private set; }

        [JsonProperty("expirations")]
        public Dictionary<string, CMEOptionsCategoryExpiration> Expirations { get; private set; }
    }

    public class CMEOptionsCategoryExpiration
    {
        [JsonProperty("label")]
        public string Label { get; private set; }

        [JsonProperty("productId")]
        public int ProductId { get; private set; }

        [JsonProperty("expiration")]
        public string Expiration { get; private set; }
    }
}
