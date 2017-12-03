using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public class CancelPost : PostBase
   {
      [JsonProperty("order_id")]
      public long OrderId { get; set; }
   }
#pragma warning restore 1591
}
