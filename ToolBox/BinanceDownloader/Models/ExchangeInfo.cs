using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.BinanceDownloader.Models
{
#pragma warning disable 1591

    /// <summary>
    /// Represents Binance exchange info response
    /// https://github.com/binance-exchange/binance-official-api-docs/blob/master/rest-api.md#exchange-information
    /// </summary>
    public class ExchangeInfo
    {
        public Symbol[] Symbols { get; set; }
    }

#pragma warning restore 1591
}
