using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.BinanceDownloader.Models
{
#pragma warning disable 1591
    /// <summary>
    /// Represents Binance exchange info for symbol
    /// </summary>
    public class Symbol
    {
        [JsonProperty(PropertyName = "symbol")]
        public string Name { get; set; }

        public string QuoteAsset { get; set; }

        /// <summary>
        /// Exchange info filter defines trading rules on a symbol or an exchange
        /// https://github.com/binance-exchange/binance-official-api-docs/blob/master/rest-api.md#filters
        /// </summary>
        public JObject[] Filters { get; set; }
    }

#pragma warning restore 1591
}
