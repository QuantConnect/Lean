using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Kraken.DataType {

    public class AssetInfo {
        /// <summary>
        /// Alternate name.
        /// </summary>
        public string Altname;

        /// <summary>
        /// Asset class.
        /// </summary>
        public string Aclass;

        /// <summary>
        /// Scaling decimal places for record keeping.
        /// </summary>
        public int Decimals;

        /// <summary>
        /// Scaling decimal places for output display.
        /// </summary>
        [JsonProperty(PropertyName = "display_decimals ")]
        public int DisplayDecimals;
    }

}
