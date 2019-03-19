using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Future
{
    public static class FuturesExpiryCalendar
    {
        public static readonly HashSet<DateTime> BrazilDates = new HashSet<DateTime>
        {
            // New year's eve
            new DateTime(2016,12,31),
            new DateTime(2017,12,31),
            new DateTime(2018,12,31),
            new DateTime(2019,12,31),
            new DateTime(2020,12,31),
            new DateTime(2021,12,31),
            new DateTime(2022,12,31),
            new DateTime(2023,12,31),
            new DateTime(2024,12,31),
            new DateTime(2025,12,31),
            new DateTime(2026,12,31),
            new DateTime(2027,12,31),
            new DateTime(2028,12,31),
            new DateTime(2029,12,31),

            // Exceptions to the rules
            // February
            new DateTime(2022,02,28),
            // March
            new DateTime(2024,03,29),
            // December
            new DateTime(2022,12,30),
            new DateTime(2023,12,29),
        };

        public static readonly HashSet<DateTime> MexicoDates = new HashSet<DateTime>
        {
            // Day of the races is always on October 12th. Sometimes, markets are closed
            // in observance of this holiday (optional holiday).
            new DateTime(2012,10,12),
            new DateTime(2013,10,14),
            new DateTime(2014,10,13),
            new DateTime(2015,10,12),
            new DateTime(2016,10,12),
            new DateTime(2017,10,12),
            new DateTime(2018,10,12),
            new DateTime(2019,10,14),
            new DateTime(2020,10,12),
            new DateTime(2021,10,12),
            new DateTime(2022,10,12),
            new DateTime(2023,10,12),
            new DateTime(2024,10,14),
            new DateTime(2025,10,13),
        };
    }
}
