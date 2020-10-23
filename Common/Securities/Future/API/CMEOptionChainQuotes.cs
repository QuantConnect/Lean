using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Securities.Future
{
    public class CMEOptionChainQuotes
    {
        [JsonProperty("optionContractQuotes")]
        public List<CMEOptionChainQuoteEntry> OptionContractQuotes { get; private set; }
    }

    public class CMEOptionChainQuoteEntry
    {
        [JsonProperty("call")]
        public CMEOptionChainQuote Call { get; private set; }

        [JsonProperty("put")]
        public CMEOptionChainQuote Put { get; private set; }
    }

    public class CMEOptionChainQuote
    {
        private decimal _strikePrice;

        [JsonProperty("code")]
        public string Code { get; private set; }

        [JsonIgnore]
        public decimal StrikePrice
        {
            get
            {
                if (_strikePrice != default(decimal))
                {
                    return _strikePrice;
                }

                _strikePrice = decimal.Parse(Code.Split(' ')[1].Substring(1), CultureInfo.InvariantCulture);
                return _strikePrice;
            }
        }
    }
}
