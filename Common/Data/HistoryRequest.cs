using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;

namespace QuantConnect.Data
{
    public class HistoryRequest
    {
        public Symbol Symbol;
        public SecurityExchangeHours ExchangeHours;
        public Resolution Resolution;
        public Resolution? FillForwardResolution;
        public bool IncludeExtendedMarketHours;

        public HistoryRequest(Symbol symbol, SecurityExchangeHours exchangeHours, Resolution resolution, Resolution? fillForwardResolution, bool includeExtendedMarketHours)
        {
            Symbol = symbol;
            ExchangeHours = exchangeHours;
            Resolution = resolution;
            FillForwardResolution = fillForwardResolution;
            IncludeExtendedMarketHours = includeExtendedMarketHours;
        }
    }
}
