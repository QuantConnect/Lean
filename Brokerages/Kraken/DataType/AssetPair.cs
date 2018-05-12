using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Kraken.DataType {

    public class AssetPair {
        /// <summary>
        /// Alternate pair name.
        /// </summary>
        public string Altname;

        /// <summary>
        /// Asset private class of base component.
        /// </summary>
        [JsonProperty(PropertyName = "aclass_base")]
        public string AclassBase;

        /// <summary>
        /// Asset id of base component
        /// </summary>
        public string Base;

        /// <summary>
        /// Asset class of quote component.
        /// </summary>
        [JsonProperty(PropertyName = "aclass_quote")]
        public string AclassQuote;

        /// <summary>
        /// Asset id of quote component.
        /// </summary>
        public string Quote;

        /// <summary>
        /// Volume lot size.
        /// </summary>
        public string Lot;

        /// <summary>
        /// Scaling decimal places for pair.
        /// </summary>
        [JsonProperty(PropertyName = "pair_decimals")]
        public int PairDecimals;

        /// <summary>
        /// Scaling decimal places for volume.
        /// </summary>
        [JsonProperty(PropertyName = "lot_decimals")]
        public int LotDecimals;

        /// <summary>
        /// Amount to multiply lot volume by to get currency volume.
        /// </summary>
        [JsonProperty(PropertyName = "lot_multiplier")]
        public int LotMultiplier;

        /// <summary>
        /// Array of leverage amounts available when buying.
        /// </summary>
        [JsonProperty(PropertyName = "leverage_buy")]
        public decimal[] LeverageBuy;

        /// <summary>
        /// Array of leverage amounts available when selling.
        /// </summary>
        [JsonProperty(PropertyName = "leverage_sell")]
        public decimal[] LeverageSell;

        /// <summary>
        /// Fee schedule array in [volume, percent fee].
        /// </summary>
        public decimal[][] Fees;

        /// <summary>
        /// Maker fee schedule array in [volume, percent fee] tuples(if on maker/taker).
        /// </summary>
        [JsonProperty(PropertyName = "fees_maker")]
        public decimal[][] FeesMaker;

        /// <summary>
        /// Volume discount currency
        /// </summary>
        [JsonProperty(PropertyName = "fee_volume_currency")]
        public string FeeVolumeCurrency;

        /// <summary>
        /// Margin call level.
        /// </summary>
        [JsonProperty(PropertyName = "margin_call")]
        public decimal MarginCall;

        /// <summary>
        /// Stop-out/liquidation margin level.
        /// </summary>
        [JsonProperty(PropertyName = "margin_stop")]
        public decimal MarginStop;
    }

}
