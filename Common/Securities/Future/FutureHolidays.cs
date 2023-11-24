using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;

namespace QuantConnect.Securities.Future
{
    public class FutureHolidays
    {
        public List<DateTime> Holidays { private set; get; }
        public FutureHolidays(string market, string symbol)
        {
            Holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry(market, symbol, SecurityType.Future)
                        .ExchangeHours
                        .Holidays.ToList();
        }

        public bool Contains(DateTime datetime)
        {
            return Holidays.Contains(datetime);
        }
    }
}
