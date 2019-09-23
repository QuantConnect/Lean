using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.PolygonDownloader
{
    public static class HelperPolygon
    {
        public static readonly SecurityExchangeHours ExchangeHoursEquity = new Func<SecurityExchangeHours>(
                () =>
                {
                    var sunday = LocalMarketHours.ClosedAllDay(DayOfWeek.Sunday);
                    var monday = new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
                    var tuesday = new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(9, 30, 0),
                        new TimeSpan(16, 0, 0));
                    var wednesday = new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(9, 30, 0),
                        new TimeSpan(16, 0, 0));
                    var thursday = new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(9, 30, 0),
                        new TimeSpan(16, 0, 0));
                    var friday = new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
                    var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

                    var earlyCloses = new Dictionary<DateTime, TimeSpan>
                        {{new DateTime(2016, 11, 25), new TimeSpan(13, 0, 0)}};
                    var exchangeHours = new SecurityExchangeHours(TimeZones.NewYork,
                        USHoliday.Dates.Select(x => x.Date), new[]
                        {
                            sunday, monday, tuesday, wednesday, thursday, friday, saturday
                        }.ToDictionary(x => x.DayOfWeek), earlyCloses);
                    return exchangeHours;
                })
            ();
        // How to initialize a static readonly variable using an anonymous method?
        // https://stackoverflow.com/questions/13570268/how-to-initialize-a-static-readonly-variable-using-an-anonymous-method

    }
}
