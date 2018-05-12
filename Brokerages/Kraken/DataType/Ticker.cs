using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Kraken.DataType {

    public class Ticker {

        /// <summary>
        /// Ask array(<price>, <whole lot volume>, <lot volume>).
        /// </summary>
        [JsonProperty(PropertyName = "a")]
        public decimal[] Ask;

        /// <summary>
        /// Bid array(<price>, <whole lot volume>, <lot volume>).
        /// </summary>
        [JsonProperty(PropertyName = "b")]
        public decimal[] Bid;

        /// <summary>
        /// Last trade closed array(<price>, <lot volume>).
        /// </summary>
        [JsonProperty(PropertyName = "c")]
        public decimal[] Closed;
        
        /// <summary>
        /// Volume array(<today>, <last 24 hours>).
        /// </summary>
        [JsonProperty(PropertyName = "v")]
        public decimal[] Volume;

        /// <summary>
        /// Volume weighted average price array(<today>, <last 24 hours>).
        /// </summary>
        [JsonProperty(PropertyName = "p")]
        public decimal[] VWAP;

        /// <summary>
        /// Number of trades array(<today>, <last 24 hours>).
        /// </summary>
        [JsonProperty(PropertyName = "t")]
        public int[] Trades;

        /// <summary>
        /// Low array(<today>, <last 24 hours>).
        /// </summary>
        [JsonProperty(PropertyName = "l")]
        public decimal[] Low;

        /// <summary>
        /// High array(<today>, <last 24 hours>).
        /// </summary>
        [JsonProperty(PropertyName = "h")]
        public decimal[] High;

        /// <summary>
        /// Today's opening price.
        /// </summary>
        [JsonProperty(PropertyName = "o")]
        public decimal Open;
    }

}
