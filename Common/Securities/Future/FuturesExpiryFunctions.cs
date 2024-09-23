/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Calculate the date of a futures expiry given an expiry month and year
    /// </summary>
    public class FuturesExpiryFunctions
    {
        /// <summary>
        /// Method to retrieve the Function for a specific future symbol
        /// </summary>
        public static Func<DateTime, DateTime> FuturesExpiryFunction(Symbol symbol)
        {
            Func<DateTime, DateTime> result;
            if (FuturesExpiryDictionary.TryGetValue(symbol.Canonical, out result))
            {
                return result;
            }

            // If the function cannot be found, throw an exception as it hasn't yet been implemented
            throw new ArgumentException($"Expiry function not implemented for {symbol} in FuturesExpiryFunctions.FuturesExpiryDictionary");
        }

        /// <summary>
        /// The USDA publishes a report containing contract prices for the contract month.
        /// You can see future publication dates at https://www.ams.usda.gov/rules-regulations/mmr/dmr (Advanced and Class Price Release Dates)
        /// These dates are erratic and requires maintenance of a separate list instead of using holiday entries in MHDB.
        /// </summary>
        /// <remarks>We only report the publication date of the report. In order to get accurate last trade dates, subtract one (plus holidays) from the value's date</remarks>
        public static Dictionary<DateTime, DateTime> DairyReportDates = new Dictionary<DateTime, DateTime>()
        {
            {new DateTime(2012, 3, 1), new DateTime(2012, 4, 2) },
            {new DateTime(2012, 4, 1), new DateTime(2012, 5, 2) },
            {new DateTime(2012, 5, 1), new DateTime(2012, 5, 31) },
            {new DateTime(2012, 6, 1), new DateTime(2012, 7, 5) },
            {new DateTime(2012, 7, 1), new DateTime(2012, 8, 1) },
            {new DateTime(2012, 8, 1), new DateTime(2012, 8, 29) },
            {new DateTime(2012, 9, 1), new DateTime(2012, 10, 3) },
            {new DateTime(2012, 10, 1), new DateTime(2012, 10, 31) },
            {new DateTime(2012, 11, 1), new DateTime(2012, 12, 5) },
            {new DateTime(2012, 12, 1), new DateTime(2013, 1, 3) },
            {new DateTime(2013, 1, 1), new DateTime(2013, 1, 30) },
            {new DateTime(2013, 2, 1), new DateTime(2013, 2, 27) },
            {new DateTime(2013, 3, 1), new DateTime(2013, 4, 3) },
            {new DateTime(2013, 4, 1), new DateTime(2013, 5, 1) },
            {new DateTime(2013, 5, 1), new DateTime(2013, 6, 5) },
            {new DateTime(2013, 6, 1), new DateTime(2013, 7, 3) },
            {new DateTime(2013, 7, 1), new DateTime(2013, 7, 31) },
            {new DateTime(2013, 8, 1), new DateTime(2013, 9, 5) },
            {new DateTime(2013, 9, 1), new DateTime(2013, 10, 18) },
            {new DateTime(2013, 10, 1), new DateTime(2013, 10, 30) },
            {new DateTime(2013, 11, 1), new DateTime(2013, 12, 4) },
            {new DateTime(2013, 12, 1), new DateTime(2014, 1, 2) },
            {new DateTime(2014, 1, 1), new DateTime(2014, 2, 5) },
            {new DateTime(2014, 2, 1), new DateTime(2014, 3, 5) },
            {new DateTime(2014, 3, 1), new DateTime(2014, 4, 2) },
            {new DateTime(2014, 4, 1), new DateTime(2014, 4, 30) },
            {new DateTime(2014, 5, 1), new DateTime(2014, 6, 4) },
            {new DateTime(2014, 6, 1), new DateTime(2014, 7, 2) },
            {new DateTime(2014, 7, 1), new DateTime(2014, 7, 30) },
            {new DateTime(2014, 8, 1), new DateTime(2014, 9, 4) },
            {new DateTime(2014, 9, 1), new DateTime(2014, 10, 1) },
            {new DateTime(2014, 10, 1), new DateTime(2014, 11, 5) },
            {new DateTime(2014, 11, 1), new DateTime(2014, 12, 3) },
            {new DateTime(2014, 12, 1), new DateTime(2014, 12, 31) },
            {new DateTime(2015, 1, 1), new DateTime(2015, 2, 4) },
            {new DateTime(2015, 2, 1), new DateTime(2015, 3, 4) },
            {new DateTime(2015, 3, 1), new DateTime(2015, 4, 1) },
            {new DateTime(2015, 4, 1), new DateTime(2015, 4, 29) },
            {new DateTime(2015, 5, 1), new DateTime(2015, 6, 3) },
            {new DateTime(2015, 6, 1), new DateTime(2015, 7, 1) },
            {new DateTime(2015, 7, 1), new DateTime(2015, 8, 5) },
            {new DateTime(2015, 8, 1), new DateTime(2015, 9, 2) },
            {new DateTime(2015, 9, 1), new DateTime(2015, 9, 30) },
            {new DateTime(2015, 10, 1), new DateTime(2015, 11, 4) },
            {new DateTime(2015, 11, 1), new DateTime(2015, 12, 2) },
            {new DateTime(2015, 12, 1), new DateTime(2015, 12, 30) },
            {new DateTime(2016, 1, 1), new DateTime(2016, 2, 3) },
            {new DateTime(2016, 2, 1), new DateTime(2016, 3, 2) },
            {new DateTime(2016, 3, 1), new DateTime(2016, 3, 30) },
            {new DateTime(2016, 4, 1), new DateTime(2016, 5, 4) },
            {new DateTime(2016, 5, 1), new DateTime(2016, 6, 2) },
            {new DateTime(2016, 6, 1), new DateTime(2016, 6, 29) },
            {new DateTime(2016, 7, 1), new DateTime(2016, 8, 3) },
            {new DateTime(2016, 8, 1), new DateTime(2016, 8, 31) },
            {new DateTime(2016, 9, 1), new DateTime(2016, 10, 5) },
            {new DateTime(2016, 10, 1), new DateTime(2016, 11, 2) },
            {new DateTime(2016, 11, 1), new DateTime(2016, 11, 30) },
            {new DateTime(2016, 12, 1), new DateTime(2017, 1, 5) },
            {new DateTime(2017, 1, 1), new DateTime(2017, 2, 1) },
            {new DateTime(2017, 2, 1), new DateTime(2017, 3, 1) },
            {new DateTime(2017, 3, 1), new DateTime(2017, 4, 5) },
            {new DateTime(2017, 4, 1), new DateTime(2017, 5, 3) },
            {new DateTime(2017, 5, 1), new DateTime(2017, 6, 1) },
            {new DateTime(2017, 6, 1), new DateTime(2017, 6, 28) },
            {new DateTime(2017, 7, 1), new DateTime(2017, 8, 2) },
            {new DateTime(2017, 8, 1), new DateTime(2017, 8, 30) },
            {new DateTime(2017, 9, 1), new DateTime(2017, 10, 4) },
            {new DateTime(2017, 10, 1), new DateTime(2017, 11, 1) },
            {new DateTime(2017, 11, 1), new DateTime(2017, 11, 29) },
            {new DateTime(2017, 12, 1), new DateTime(2018, 1, 4) },
            {new DateTime(2018, 1, 1), new DateTime(2018, 1, 31) },
            {new DateTime(2018, 2, 1), new DateTime(2018, 2, 28) },
            {new DateTime(2018, 3, 1), new DateTime(2018, 4, 4) },
            {new DateTime(2018, 4, 1), new DateTime(2018, 5, 2) },
            {new DateTime(2018, 5, 1), new DateTime(2018, 5, 31) },
            {new DateTime(2018, 6, 1), new DateTime(2018, 7, 5) },
            {new DateTime(2018, 7, 1), new DateTime(2018, 8, 1) },
            {new DateTime(2018, 8, 1), new DateTime(2018, 8, 29) },
            {new DateTime(2018, 9, 1), new DateTime(2018, 10, 3) },
            {new DateTime(2018, 10, 1), new DateTime(2018, 10, 31) },
            {new DateTime(2018, 11, 1), new DateTime(2018, 12, 5) },
            {new DateTime(2018, 12, 1), new DateTime(2019, 1, 3) },
            {new DateTime(2019, 1, 1), new DateTime(2019, 1, 30) },
            {new DateTime(2019, 2, 1), new DateTime(2019, 2, 27) },
            {new DateTime(2019, 3, 1), new DateTime(2019, 4, 3) },
            {new DateTime(2019, 4, 1), new DateTime(2019, 5, 1) },
            {new DateTime(2019, 5, 1), new DateTime(2019, 6, 5) },
            {new DateTime(2019, 6, 1), new DateTime(2019, 7, 3) },
            {new DateTime(2019, 7, 1), new DateTime(2019, 7, 31) },
            {new DateTime(2019, 8, 1), new DateTime(2019, 9, 5) },
            {new DateTime(2019, 9, 1), new DateTime(2019, 10, 2) },
            {new DateTime(2019, 10, 1), new DateTime(2019, 10, 30) },
            {new DateTime(2019, 11, 1), new DateTime(2019, 12, 4) },
            {new DateTime(2019, 12, 1), new DateTime(2020, 1, 2) },
            {new DateTime(2020, 1, 1), new DateTime(2020, 2, 5) },
            {new DateTime(2020, 2, 1), new DateTime(2020, 3, 4) },
            {new DateTime(2020, 3, 1), new DateTime(2020, 4, 1) },
            {new DateTime(2020, 4, 1), new DateTime(2020, 4, 29) },
            {new DateTime(2020, 5, 1), new DateTime(2020, 6, 3) },
            {new DateTime(2020, 6, 1), new DateTime(2020, 7, 1) },
            {new DateTime(2020, 7, 1), new DateTime(2020, 8, 5) },
            {new DateTime(2020, 8, 1), new DateTime(2020, 9, 2) },
            {new DateTime(2020, 9, 1), new DateTime(2020, 9, 30) },
            {new DateTime(2020, 10, 1), new DateTime(2020, 11, 4) },
            {new DateTime(2020, 11, 1), new DateTime(2020, 12, 2) },
            {new DateTime(2020, 12, 1), new DateTime(2020, 12, 30) },
            {new DateTime(2021, 1, 1), new DateTime(2021, 2, 3) },
            {new DateTime(2021, 2, 1), new DateTime(2021, 3, 3) },
            {new DateTime(2021, 3, 1), new DateTime(2021, 3, 31) },
            {new DateTime(2021, 4, 1), new DateTime(2021, 5, 5) },
            {new DateTime(2021, 5, 1), new DateTime(2021, 6, 3) },
            {new DateTime(2021, 6, 1), new DateTime(2021, 6, 30) },
            {new DateTime(2021, 7, 1), new DateTime(2021, 8, 4) },
            {new DateTime(2021, 8, 1), new DateTime(2021, 9, 1) },
            {new DateTime(2021, 9, 1), new DateTime(2021, 9, 29) },
            {new DateTime(2021, 10, 1), new DateTime(2021, 11, 3) },
            {new DateTime(2021, 11, 1), new DateTime(2021, 12, 1) },
            {new DateTime(2021, 12, 1), new DateTime(2022, 1, 5) },
            {new DateTime(2022, 1, 1), new DateTime(2022, 2, 2) },
            {new DateTime(2022, 2, 1), new DateTime(2022, 3, 2) },
            {new DateTime(2022, 3, 1), new DateTime(2022, 3, 30) },
            {new DateTime(2022, 4, 1), new DateTime(2022, 5, 4) },
            {new DateTime(2022, 5, 1), new DateTime(2022, 6, 2) },
            {new DateTime(2022, 6, 1), new DateTime(2022, 6, 29) },
            {new DateTime(2022, 7, 1), new DateTime(2022, 8, 3) },
            {new DateTime(2022, 8, 1), new DateTime(2022, 8, 31) },
            {new DateTime(2022, 9, 1), new DateTime(2022, 10, 5) },
            {new DateTime(2022, 10, 1), new DateTime(2022, 11, 2) },
            {new DateTime(2022, 11, 1), new DateTime(2022, 11, 30) },
            {new DateTime(2022, 12, 1), new DateTime(2023, 1, 5) },
        };

        /// <summary>
        /// Enbridge's Notice of Shipment report dates. Used to calculate the last trade date for CSW
        /// </summary>
        /// <remarks>Subtract a day from the value's date in order to get the last trade date</remarks>
        public static Dictionary<DateTime, DateTime> EnbridgeNoticeOfShipmentDates = new Dictionary<DateTime, DateTime>()
        {
            {new DateTime(2019, 6, 1), new DateTime(2019, 5, 17) },
            {new DateTime(2019, 7, 1), new DateTime(2019, 6, 15) },
            {new DateTime(2019, 8, 1), new DateTime(2019, 7, 17) },
            {new DateTime(2019, 9, 1), new DateTime(2019, 8, 16) },
            {new DateTime(2019, 10, 1), new DateTime(2019, 9, 14) },
            {new DateTime(2019, 11, 1), new DateTime(2019, 10, 17) },
            {new DateTime(2019, 12, 1), new DateTime(2019, 11, 15) },
            {new DateTime(2020, 1, 1), new DateTime(2019, 12, 14) },
            {new DateTime(2020, 2, 1), new DateTime(2020, 1, 22) },
            {new DateTime(2020, 3, 1), new DateTime(2020, 2, 21) },
            {new DateTime(2020, 4, 1), new DateTime(2020, 3, 21) },
            {new DateTime(2020, 5, 1), new DateTime(2020, 4, 21) },
            {new DateTime(2020, 6, 1), new DateTime(2020, 5, 21) },
            {new DateTime(2020, 7, 1), new DateTime(2020, 6, 23) },
            {new DateTime(2020, 8, 1), new DateTime(2020, 7, 21) },
            {new DateTime(2020, 9, 1), new DateTime(2020, 8, 21) },
            {new DateTime(2020, 10, 1), new DateTime(2020, 9, 22) },
            {new DateTime(2020, 11, 1), new DateTime(2020, 10, 21) },
            {new DateTime(2020, 12, 1), new DateTime(2020, 11, 21) },
            {new DateTime(2021, 1, 1), new DateTime(2020, 12, 22) },
            {new DateTime(2021, 2, 1), new DateTime(2021, 1, 21) },
            {new DateTime(2021, 3, 1), new DateTime(2021, 2, 23) },
            {new DateTime(2021, 4, 1), new DateTime(2021, 3, 23) },
            {new DateTime(2021, 5, 1), new DateTime(2021, 4, 21) },
            {new DateTime(2021, 6, 1), new DateTime(2021, 5, 21) },
            {new DateTime(2021, 7, 1), new DateTime(2021, 6, 22) },
            {new DateTime(2021, 8, 1), new DateTime(2021, 7, 21) },
            {new DateTime(2021, 9, 1), new DateTime(2021, 8, 21) },
            {new DateTime(2021, 10, 1), new DateTime(2021, 9, 21) },
            {new DateTime(2021, 11, 1), new DateTime(2021, 10, 21) },
            {new DateTime(2021, 12, 1), new DateTime(2021, 11, 23) },
            {new DateTime(2022, 1, 1), new DateTime(2021, 12, 21) },
            {new DateTime(2022, 2, 1), new DateTime(2022, 1, 21) },
            {new DateTime(2022, 3, 1), new DateTime(2022, 2, 23) },
            {new DateTime(2022, 4, 1), new DateTime(2022, 3, 22) },
            {new DateTime(2022, 5, 1), new DateTime(2022, 4, 21) },
            {new DateTime(2022, 6, 1), new DateTime(2022, 5, 21) },
            {new DateTime(2022, 7, 1), new DateTime(2022, 6, 21) },
            {new DateTime(2022, 8, 1), new DateTime(2022, 7, 21) },
            {new DateTime(2022, 9, 1), new DateTime(2022, 8, 23) },
            {new DateTime(2022, 10, 1), new DateTime(2022, 9, 21) },
            {new DateTime(2022, 11, 1), new DateTime(2022, 10, 21) },
            {new DateTime(2022, 12, 1), new DateTime(2022, 11, 22) },
        };

        /// <summary>
        /// Dictionary of the Functions that calculates the expiry for a given year and month.
        /// It does not matter what the day and time of day are passed into the Functions.
        /// The Functions is responsible for calculating the day and time of day given a year and month
        /// </summary>
        public static readonly Dictionary<Symbol, Func<DateTime, DateTime>> FuturesExpiryDictionary = new Dictionary<Symbol, Func<DateTime, DateTime>>()
        {
            // Metals
            // Gold (GC): http://www.cmegroup.com/trading/metals/precious/gold_contract_specifications.html
            {Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX), (time =>
                {
                    // Monthly contracts
                    // Trading terminates on the third last business day of the delivery month.
                    var market = Market.COMEX;
                    var symbol = Futures.Metals.Gold;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time,3, holidays);
                })
            },
            // Silver (SI): http://www.cmegroup.com/trading/metals/precious/silver_contract_specifications.html
            {Symbol.Create(Futures.Metals.Silver, SecurityType.Future, Market.COMEX), (time =>
                {
                    // Monthly contracts
                    // Trading terminates on the third last business day of the delivery month.
                    var market = Market.COMEX;
                    var symbol = Futures.Metals.Silver;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time,3, holidays);
                })
            },
            // Platinum (PL): http://www.cmegroup.com/trading/metals/precious/platinum_contract_specifications.html
            {Symbol.Create(Futures.Metals.Platinum, SecurityType.Future, Market.NYMEX), (time =>
                {
                    // Monthly contracts
                    // Trading terminates on the third last business day of the delivery month.
                    var market = Market.NYMEX;
                    var symbol = Futures.Metals.Platinum;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time,3, holidays);
                })
            },
            // Palladium (PA): http://www.cmegroup.com/trading/metals/precious/palladium_contract_specifications.html
            {Symbol.Create(Futures.Metals.Palladium, SecurityType.Future, Market.NYMEX), (time =>
                {
                    // Monthly contracts
                    // Trading terminates on the third last business day of the delivery month.
                    var market = Market.NYMEX;
                    var symbol = Futures.Metals.Palladium;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time,3, holidays);
                })
            },
            // Aluminum MW U.S. Transaction Premium Platts (25MT) (AUP): https://www.cmegroup.com/trading/metals/base/aluminum-mw-us-transaction-premium-platts-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Metals.AluminumMWUSTransactionPremiumPlatts25MT, SecurityType.Future, Market.COMEX), (time =>
                {
                    var market = Market.COMEX;
                    var symbol = Futures.Metals.AluminumMWUSTransactionPremiumPlatts25MT;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);

                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Aluminium European Premium Duty-Paid (Metal Bulletin) (EDP): https://www.cmegroup.com/trading/metals/base/aluminium-european-premium-duty-paid-metal-bulletin_contract_specifications.html
            {Symbol.Create(Futures.Metals.AluminiumEuropeanPremiumDutyPaidMetalBulletin, SecurityType.Future, Market.COMEX), (time =>
                {
                    var market = Market.COMEX;
                    var symbol = Futures.Metals.AluminiumEuropeanPremiumDutyPaidMetalBulletin;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Copper (HG): https://www.cmegroup.com/trading/metals/base/copper_contract_specifications.html
            {Symbol.Create(Futures.Metals.Copper, SecurityType.Future, Market.COMEX), (time =>
                {
                    var market = Market.COMEX;
                    var symbol = Futures.Metals.Copper;
                    // Monthly contracts
                    // Trading terminates at 12:00 Noon CT on the third last business day of the contract month.
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 3, holidays).Add(new TimeSpan(17, 0, 0));
                })
            },
            // U.S. Midwest Domestic Hot-Rolled Coil Steel (CRU) Index (HRC): https://www.cmegroup.com/trading/metals/ferrous/hrc-steel_contract_specifications.html
            {Symbol.Create(Futures.Metals.USMidwestDomesticHotRolledCoilSteelCRUIndex, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Metals.USMidwestDomesticHotRolledCoilSteelCRUIndex;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the business day prior to the last Wednesday of the named contract month.
                    var lastWednesday = (from dateRange in Enumerable.Range(1, DateTime.DaysInMonth(time.Year, time.Month))
                                         where new DateTime(time.Year, time.Month, dateRange).DayOfWeek == DayOfWeek.Wednesday
                                         select new DateTime(time.Year, time.Month, dateRange)).Last();

                    return FuturesExpiryUtilityFunctions.AddBusinessDays(lastWednesday, -1, holidays);
                })
            },
            // Indices
            // SP500EMini (ES): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-sandp500_contract_specifications.html
            {Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME), (time =>
                {
                    // Quarterly contracts (Mar/3, Jun/6 , Sep/9 , Dec/12) listed for 9 consecutive quarters and 3 additional December contract months.
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // EuroStoxx50 (FESX): https://www.xetra.com/resource/blob/63488/437afcd347fb020377873dd1ceac10ba/EURO-STOXX-50-Factsheet-data.pdf
            {Symbol.Create(Futures.Indices.EuroStoxx50, SecurityType.Future, Market.EUREX), (time =>
                {
                    // Quarterly contracts (Mar/3, Jun/6 , Sep/9 , Dec/12) listed for 9 consecutive quarters and 3 additional December contract months.
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // NASDAQ100EMini (NQ): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-nasdaq-100_contract_specifications.html
            {Symbol.Create(Futures.Indices.NASDAQ100EMini, SecurityType.Future, Market.CME), (time =>
                {
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 5 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // Dow30EMini (YM): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-dow_contract_specifications.html
            {Symbol.Create(Futures.Indices.Dow30EMini, SecurityType.Future, Market.CBOT), (time =>
                {
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 4 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // Russell2000EMini (RTY): https://www.cmegroup.com/trading/equity-index/us-index/e-mini-russell-2000_contract_specifications.html
            {Symbol.Create(Futures.Indices.Russell2000EMini, SecurityType.Future, Market.CME), (time =>
                {
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 5 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan (13,30,0));
                })
            },
            // Nikkei225Dollar (NKD): https://www.cmegroup.com/trading/equity-index/international-index/nikkei-225-dollar_contract_specifications.html
            {Symbol.Create(Futures.Indices.Nikkei225Dollar, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.Nikkei225Dollar;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 12 quarters, and 3 additional Dec contract months
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading terminates at 5:00 p.m. Eastern Time (ET) on Business Day prior to 2nd Friday of the contract month.
                    var secondFriday = FuturesExpiryUtilityFunctions.SecondFriday(time);
                    var priorBusinessDay = secondFriday.AddDays(-1);
                    while (!FuturesExpiryUtilityFunctions.NotHoliday(priorBusinessDay, holidays))
                    {
                        priorBusinessDay = priorBusinessDay.AddDays(-1);
                    }
                    return priorBusinessDay.Add(TimeSpan.FromHours(21));
                })
            },
            // Nikkei225YenCME (NIY): https://www.cmegroup.com/markets/equities/international-indices/nikkei-225-yen.contractSpecs.html
            {Symbol.Create(Futures.Indices.Nikkei225YenCME, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.Nikkei225YenCME;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 12 quarters, serial contract listed for 3 months, and 3 additional Dec contract months
                    // Trading terminates at 5:00 p.m. Eastern Time (ET) on Business Day prior to 2nd Friday of the contract month.
                    var secondFriday = FuturesExpiryUtilityFunctions.SecondFriday(time);
                    var priorBusinessDay = secondFriday.AddDays(-1);
                    while (!FuturesExpiryUtilityFunctions.NotHoliday(priorBusinessDay, holidays))
                    {
                        priorBusinessDay = priorBusinessDay.AddDays(-1);
                    }
                    return priorBusinessDay.Add(TimeSpan.FromHours(21));
                })
            },
            // Nikkei225YenEMini (ENY): https://www.cmegroup.com/markets/equities/international-indices/emini-nikkei-225-yen.contractSpecs.html
            {Symbol.Create(Futures.Indices.Nikkei225YenEMini, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.Nikkei225YenEMini;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Four months in the March Quarterly Cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading terminates at 5:00 p.m. Eastern Time (ET) on Business Day prior to 2nd Friday of the contract month.
                    var secondFriday = FuturesExpiryUtilityFunctions.SecondFriday(time);
                    var priorBusinessDay = secondFriday.AddDays(-1);
                    while (!FuturesExpiryUtilityFunctions.NotHoliday(priorBusinessDay, holidays))
                    {
                        priorBusinessDay = priorBusinessDay.AddDays(-1);
                    }
                    return priorBusinessDay.Add(TimeSpan.FromHours(21));
                })
            },
            // FTSEChina50EMini (FT5): https://www.cmegroup.com/markets/equities/international-indices/e-mini-ftse-china-50-index.contractSpecs.html
            {Symbol.Create(Futures.Indices.FTSEChina50EMini, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.FTSEChina50EMini;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Contracts listed for the  2 nearest serial and 4 quarterly months.
                    //Trading terminates on the second to last business day of the contract month at the end of trading on the Hong Kong Exchange Securities Market
                    var secondLastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time,2, holidays);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(secondLastBusinessDay, holidays))
                    {
                        secondLastBusinessDay = secondLastBusinessDay.AddDays(-1);
                    }
                    return secondLastBusinessDay.Add(TimeSpan.FromHours(6));
                })
            },
            // FTSE100EMini (FT1): https://www.cmegroup.com/markets/equities/international-indices/e-mini-ftse-100-index.contractSpecs.html
            {Symbol.Create(Futures.Indices.FTSE100EMini, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.FTSE100EMini;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Contracts listed for five months in the March Quarterly Cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    //Trading terminates on the third Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.NthFriday(time,3);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(thirdFriday, holidays))
                    {
                        thirdFriday = thirdFriday.AddDays(-1);
                    }
                    return thirdFriday;
                })
            },
            // SPEurop350ESGEMini (E3G): https://www.cmegroup.com/markets/equities/international-indices/e-mini-sp-europe-350-esg-index.contractSpecs.html
            {Symbol.Create(Futures.Indices.SPEurop350ESGEMini, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.SPEurop350ESGEMini;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Contracts listed for 5 months in the March Quarterly Cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    //Trading terminates on the 3rd Friday of contract delivery month.
                    var thirdFriday = FuturesExpiryUtilityFunctions.NthFriday(time,3);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(thirdFriday, holidays))
                    {
                        thirdFriday = thirdFriday.AddDays(-1);
                    }
                    return thirdFriday;
                })
            },
            // FTSE100USDEMini (FTU): https://www.cmegroup.com/markets/equities/international-indices/e-mini-sp-europe-350-esg-index.contractSpecs.html
            {Symbol.Create(Futures.Indices.FTSE100USDEMini, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.FTSE100USDEMini;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Contracts listed for five months in the March Quarterly Cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    //Trading terminates on the third Friday of the contract month.
                    var thirdFriday = FuturesExpiryUtilityFunctions.NthFriday(time,3);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(thirdFriday, holidays))
                    {
                        thirdFriday = thirdFriday.AddDays(-1);
                    }
                    return thirdFriday;
                })
            },
            // TOPIXUSD (TPD): https://www.cmegroup.com/markets/equities/international-indices/usd-denominated-topix-index.contractSpecs.html
            {Symbol.Create(Futures.Indices.TOPIXUSD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.TOPIXUSD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Quarterly Contracts listed for (Mar, Jun, Sep, Dec) for 5 months
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    //Trading terminates at 5:00 p.m. ET on the Thursday prior to the second Friday of the contract month.
                    var secondFriday = FuturesExpiryUtilityFunctions.NthFriday(time,2);
                    var thursdaypriorsecondFriday = secondFriday.AddDays(-1);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(thursdaypriorsecondFriday, holidays))
                    {
                        thursdaypriorsecondFriday = thursdaypriorsecondFriday.AddDays(-1);
                    }
                    return thursdaypriorsecondFriday.Add(TimeSpan.FromHours(21));
                })
            },
            // TOPIXYEN (TPY): https://www.cmegroup.com/markets/equities/international-indices/usd-denominated-topix-index.contractSpecs.html
            {Symbol.Create(Futures.Indices.TOPIXYEN, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.TOPIXYEN;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Quarterly Contracts listed for (Mar, Jun, Sep, Dec) for 5 months
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    //Trading terminates at 5:00 p.m. ET on the Thursday prior to the second Friday of the contract month.
                    var secondFriday = FuturesExpiryUtilityFunctions.NthFriday(time,2);
                    var thursdaypriorsecondFriday = secondFriday.AddDays(-1);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(thursdaypriorsecondFriday, holidays))
                    {
                        thursdaypriorsecondFriday = thursdaypriorsecondFriday.AddDays(-1);
                    }
                    return thursdaypriorsecondFriday.Add(TimeSpan.FromHours(21));
                })
            },
            // DowJonesRealEstate (RX): https://www.cmegroup.com/markets/equities/dow-jones/dow-jones-rei.contractSpecs.html
            {Symbol.Create(Futures.Indices.DowJonesRealEstate, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.DowJonesRealEstate;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Quarterly contracts (Mar, Jun, Sep, Dec) listed for 4 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }
                    //Trading can occur up to 9:30 a.m. ET on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.NthFriday(time,3);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(thirdFriday, holidays))
                    {
                        thirdFriday = thirdFriday.AddDays(-1);
                    }
                    return thirdFriday.Add(new TimeSpan(13, 30, 0));
                })
            },
            // SP500EMiniESG (ESG): https://www.cmegroup.com/markets/equities/sp/e-mini-sandp-500-esg-index.contractSpecs.html
            {Symbol.Create(Futures.Indices.SP500EMiniESG, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.SP500EMiniESG;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Quarterly contracts (Mar, Jun, Sep, Dec) listed for 5 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }
                    //Trading terminates at 9:30 a.m. ET on the 3rd Friday of the contract month.
                    var thirdFriday = FuturesExpiryUtilityFunctions.NthFriday(time,3);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(thirdFriday, holidays))
                    {
                        thirdFriday = thirdFriday.AddDays(-1);
                    }
                    return thirdFriday.Add(new TimeSpan(13, 30, 0));
                })
            },
            // Russell1000EMini (RS1): https://www.cmegroup.com/markets/equities/russell/e-mini-russell-1000-index.contractSpecs.html
            {Symbol.Create(Futures.Indices.Russell1000EMini, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.Russell1000EMini;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Quarterly contracts (Mar, Jun, Sep, Dec) lisrted for 5 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }
                    //Trading terminates at 9:30 a.m. ET on the 3rd Friday of the contract month.
                    var thirdFriday = FuturesExpiryUtilityFunctions.NthFriday(time,3);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(thirdFriday, holidays))
                    {
                        thirdFriday = thirdFriday.AddDays(-1);
                    }
                    return thirdFriday.Add(new TimeSpan(13, 30, 0));
                })
            },
            // SP500AnnualDividendIndex (SDA): https://www.cmegroup.com/markets/equities/sp/sp-500-annual-dividend-index.contractSpecs.html
            {Symbol.Create(Futures.Indices.SP500AnnualDividendIndex, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.SP500AnnualDividendIndex;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    //Annual contracts (December) listed for 11 consecutive years
                    while (!FutureExpirationCycles.December.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }
                    //Trading terminates at 9:30 a.m. ET on the 3rd Friday of the contract month.
                    var thirdFriday = FuturesExpiryUtilityFunctions.NthFriday(time,3);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(thirdFriday, holidays))
                    {
                        thirdFriday = thirdFriday.AddDays(-1);
                    }
                    return thirdFriday.Add(new TimeSpan(13, 30, 0));
                })
            },
            // CBOE Volatility Index Futures (VIX): https://www.cboe.com/tradable_products/vix/vix_futures/specifications/
            {Symbol.Create(Futures.Indices.VIX, SecurityType.Future, Market.CFE), (time =>
                {
                    // Trading can occur up to 9:00 a.m. Eastern Time (ET) on the "Wednesday that is 30 days prior to
                    // the third Friday of the calendar month immediately following the month in which the contract expires".
                    var market = Market.CFE;
                    var symbol = Futures.Indices.VIX;
                    var nextThirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time.AddMonths(1));
                    var expiryDate = nextThirdFriday.AddDays(-30);
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // If the next third Friday or the Wednesday are holidays, then it is moved to the previous day.
                    if (holidays.Contains(expiryDate) || holidays.Contains(nextThirdFriday))
                    {
                        expiryDate = expiryDate.AddDays(-1);
                    }
                    // Trading hours for expiring VX futures contracts end at 8:00 a.m. Chicago time on the final settlement date.
                    return expiryDate.Add(new TimeSpan(13, 0, 0));
                })
            },
            // Bloomberg Commodity Index (AW): https://www.cmegroup.com/trading/agricultural/commodity-index/bloomberg-commodity-index_contract_specifications.html
            {Symbol.Create(Futures.Indices.BloombergCommodityIndex, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Indices.BloombergCommodityIndex;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 4 consecutive quarters and 4 additional Dec contract months
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 3rd Wednesday of the contract month/ 1:30pm
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    thirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(thirdWednesday, -1, holidays);
                    return thirdWednesday.Add(new TimeSpan(18, 30, 0));
                })
            },
            // E-mini Nasdaq-100 Biotechnology Index (BIO): https://www.cmegroup.com/trading/equity-index/us-index/e-mini-nasdaq-biotechnology_contract_specifications.html
            {Symbol.Create(Futures.Indices.NASDAQ100BiotechnologyEMini, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.NASDAQ100BiotechnologyEMini;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 5 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading can occur up to 9:30 a.m. ET on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    thirdFriday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(thirdFriday, -1, holidays);

                    return thirdFriday.Add(new TimeSpan(13, 30, 0));
                })
            },
            // E-mini FTSE Emerging Index (EI): https://www.cmegroup.com/trading/equity-index/international-index/e-mini-ftse-emerging-index_contract_specifications.html
            {Symbol.Create(Futures.Indices.FTSEEmergingEmini, SecurityType.Future, Market.CME), (time =>
                {
                    // Five months in the March Quarterly Cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading can occur up to 4:00 p.m. ET on the 3rd Friday of contract month
                    return FuturesExpiryUtilityFunctions.NthFriday(time, 3).Add(new TimeSpan(20, 0, 0));
                })
            },
            // E-mini S&amp;P MidCap 400 Futures (EMD): https://www.cmegroup.com/trading/equity-index/us-index/e-mini-sandp-midcap-400_contract_specifications.html
            {Symbol.Create(Futures.Indices.SP400MidCapEmini, SecurityType.Future, Market.CME), (time =>
                {
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 5 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading can occur up until 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    return FuturesExpiryUtilityFunctions.NthFriday(time, 3).Add(new TimeSpan(13, 30, 0));
                })
            },
            // S&amp;P-GSCI Commodity Index (GD): https://www.cmegroup.com/trading/agricultural/commodity-index/gsci_contract_specifications.html
            {Symbol.Create(Futures.Indices.SPGSCICommodity, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.SPGSCICommodity;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the11th business day of the contract month, 1:40pm.

                    return FuturesExpiryUtilityFunctions.NthBusinessDay(time, 11, holidays).Add(new TimeSpan(18, 40, 0));
                })
            },
            // USD-Denominated Ibovespa Index (IBV): https://www.cmegroup.com/trading/equity-index/international-index/usd-denominated-ibovespa_contract_specifications.html
            {Symbol.Create(Futures.Indices.USDDenominatedIbovespa, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Indices.USDDenominatedIbovespa;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Four bi-monthly contracts (Feb/2, Apr/4, Jun/6, Aug/8, Oct/10, Dec/12 cycle)
                    while (!FutureExpirationCycles.GJMQVZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 5:00 p.m. Sao Paulo Time on the Wednesday closest to the 15th calendar day of the contract month. If it is a non-trading day at BM&amp;F Bovespa, trading shall terminate on the next trading day.
                    var wednesdays = (from dateRange in Enumerable.Range(1, DateTime.DaysInMonth(time.Year, time.Month))
                                                               where new DateTime(time.Year, time.Month, dateRange).DayOfWeek == DayOfWeek.Wednesday
                                                               select new DateTime(time.Year, time.Month, dateRange));

                    var distanceFromFifteenthDay = wednesdays.Select(x => Math.Abs(15 - x.Day)).ToList();
                    var wednesdayIndex = distanceFromFifteenthDay.IndexOf(distanceFromFifteenthDay.Min());
                    var closestWednesday = wednesdays.ElementAt(wednesdayIndex);
                    if (holidays.Contains(closestWednesday) || !FuturesExpiryUtilityFunctions.NotHoliday(closestWednesday, holidays))
                    {
                        closestWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(closestWednesday, 1, holidays);
                    }

                    return closestWednesday.Add(new TimeSpan(20, 0, 0));
                })
            },

            // JPY-Denominated Nikkie 225 Index Futures: https://www2.sgx.com/derivatives/products/nikkei225futuresoptions?cc=NK#Contract%20Specifications
            {Symbol.Create(Futures.Indices.Nikkei225Yen, SecurityType.Future, Market.SGX), (time =>
                {
                    // 6 nearest serial months & 32 nearest quarterly months
                    // The day before the second Friday of the contract month. Trading Hours on Last Day is normal Trading Hours (session T)
                    // T Session
                    // 7.15 am - 2.30 pm
                    var market = Market.SGX;
                    var symbol = Futures.Indices.Nikkei225Yen;
                    var secondFriday = FuturesExpiryUtilityFunctions.SecondFriday(time);
                    var priorBusinessDay = secondFriday.AddDays(-1);

                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    while (holidays.Contains(priorBusinessDay) || !priorBusinessDay.IsCommonBusinessDay())
                    {
                        priorBusinessDay = priorBusinessDay.AddDays(-1);
                    }
                    return priorBusinessDay.Add(new TimeSpan(14, 30, 0));
                })
            },

            // MSCI Taiwan Index Futures: https://www2.sgx.com/derivatives/products/timsci?cc=TW
            {Symbol.Create(Futures.Indices.MSCITaiwanIndex, SecurityType.Future, Market.SGX), (time =>
                {
                    // 2 nearest serial months and 12 quarterly months on March, June, September and December cycle.
                    // Second last business day of the contract month. Same as T Session trading hours
                    var market = Market.SGX;
                    var symbol = Futures.Indices.MSCITaiwanIndex;
                    var lastDay = new DateTime(time.Year, time.Month, DateTime.DaysInMonth(time.Year, time.Month));
                    var priorBusinessDay = lastDay.AddDays(-1);

                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    while (holidays.Contains(priorBusinessDay) || !priorBusinessDay.IsCommonBusinessDay())
                    {
                        priorBusinessDay = priorBusinessDay.AddDays(-1);
                    }
                    return priorBusinessDay.Add(new TimeSpan(13, 45, 0));
                })
            },

            // Nifty 50 Index Futures: https://www1.nseindia.com/products/content/derivatives/equities/contract_specifitns.htm
            {Symbol.Create(Futures.Indices.Nifty50, SecurityType.Future, Market.India), (time =>
                {
                    // 3 consecutive months trading cycle – Near-Month, Mid-Month and Far-Month.
                    // Last Thursday of the expiring contract month. If this falls on an NSE non-business day, the last trading day shall be the preceding business day.
                    // The expiring contract shall close on its last trading day at 3.30 pm.
                    var market = Market.India;
                    var symbol = Futures.Indices.Nifty50;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    var expiryday = FuturesExpiryUtilityFunctions.LastThursday(time);

                    while (holidays.Contains(expiryday) || !expiryday.IsCommonBusinessDay())
                    {
                        expiryday = expiryday.AddDays(-1);
                    }
                    return expiryday.Add(new TimeSpan(15, 30, 0));
                })
            },

            // BankNifty Index Futures: https://www1.nseindia.com/products/content/derivatives/equities/bank_nifty_new.htm
            {Symbol.Create(Futures.Indices.BankNifty, SecurityType.Future, Market.India), (time =>
                {
                    // have a maximum of 3-month trading cycle - the near month , the next month and the far month.
                    // Last Thursday of the expiring contract month. If this falls on an NSE non-business day, the last trading day shall be the preceding business day.
                    // The expiring contract shall close on its last trading day at 3.30 pm.
                    var market = Market.India;
                    var symbol = Futures.Indices.BankNifty;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    var expiryday = FuturesExpiryUtilityFunctions.LastThursday(time);

                    while (holidays.Contains(expiryday) || !expiryday.IsCommonBusinessDay())
                    {
                        expiryday = expiryday.AddDays(-1);
                    }
                    return expiryday.Add(new TimeSpan(15, 30, 0));
                })
            },


            // BSE S&P Sensex Index Futures: https://www.bseindia.com/static/markets/Derivatives/DeriReports/market_information.html#!#ach6
            {Symbol.Create(Futures.Indices.BseSensex, SecurityType.Future, Market.India), (time =>
                {
                    // Last Thursday of the expiring contract month. If this falls on an BSE non-business day, the last trading day shall be the preceding business day.
                    // The expiring contract shall close on its last trading day at 3.30 pm.
                    var market = Market.India;
                    var symbol = Futures.Indices.BseSensex;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    var expiryday = FuturesExpiryUtilityFunctions.LastThursday(time);

                    while (holidays.Contains(expiryday) || !expiryday.IsCommonBusinessDay())
                    {
                        expiryday = expiryday.AddDays(-1);
                    }
                    return expiryday.Add(new TimeSpan(15, 30, 0));
                })
            },

            // MSCI Europe Net Total Return (USD) Futures: https://www.theice.com/products/71512951/MSCI-Europe-NTR-Index-Future-USD & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Indices.MSCIEuropeNTR, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Indices.MSCIEuropeNTR;
                    // Trading terminates on the third Friday of the contract month @16:15.
                    var lastTradingDay = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    lastTradingDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastTradingDay, -1, holidays);

                    return lastTradingDay.Add(new TimeSpan(16, 15, 0));
                })
            },
            // MSCI Japan Net Total Return Futures: https://www.theice.com/products/75392111/MSCI-Japan-NTR-Index-Future & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Indices.MSCIJapanNTR, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Indices.MSCIJapanNTR;
                    // Trading terminates on the third Friday of the contract month @16:15.
                    var lastTradingDay = FuturesExpiryUtilityFunctions.ThirdFriday(time);

                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    lastTradingDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastTradingDay, -1, holidays);

                    return lastTradingDay.Add(new TimeSpan(16, 15, 0));
                })
            },
            // MSCI Emerging Markets Asia Net Total Return Futures: https://www.theice.com/products/32375861/MSCI-Emerging-Markets-Asia-NTR-Index-Future & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Indices.MSCIEmergingMarketsAsiaNTR, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Indices.MSCIEmergingMarketsAsiaNTR;
                    // Trading terminates on the third Friday of the contract month @16:15.
                    var lastTradingDay = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    lastTradingDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastTradingDay, -1, holidays);

                    return lastTradingDay.Add(new TimeSpan(16, 15, 0));
                })
            },
            // MSCI EAFE Index Futures: https://www.theice.com/products/31196848/MSCI-EAFE-Index-Future & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Indices.MSCIEafeIndex, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Indices.MSCIEafeIndex;
                    // Trading terminates on the third Friday of the contract month @16:15.
                    var lastTradingDay = FuturesExpiryUtilityFunctions.ThirdFriday(time);

                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    lastTradingDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastTradingDay, -1, holidays);

                    return lastTradingDay.Add(new TimeSpan(16, 15, 0));
                })
            },
            // MSCI Emerging Markets Index Futures: https://www.theice.com/products/31196851/MSCI-Emerging-Markets-Index-Future & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Indices.MSCIEmergingMarketsIndex, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Indices.MSCIEmergingMarketsIndex;
                    // Trading terminates on the third Friday of the contract month @16:15.
                    var lastTradingDay = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    lastTradingDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastTradingDay, -1, holidays);

                    return lastTradingDay.Add(new TimeSpan(16, 15, 0));
                })
            },
            // MSCI USA Index Futures: https://www.theice.com/products/32375866/MSCI-USA-Index-Future & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Indices.MSCIUsaIndex, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Indices.MSCIUsaIndex;
                    // Trading terminates on the third Friday of the contract month @16:15.
                    var lastTradingDay = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    lastTradingDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastTradingDay, -1, holidays);

                    return lastTradingDay.Add(new TimeSpan(16, 15, 0));
                })
            },
            // Forestry Group
            // Random Length Lumber (LBS): https://www.cmegroup.com/trading/agricultural/lumber-and-pulp/random-length-lumber_contract_specifications.html
            {Symbol.Create(Futures.Forestry.RandomLengthLumber, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Forestry.RandomLengthLumber;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts (Jan, Mar, May, Jul, Sep, Nov) listed for 7 months
                    while (!FutureExpirationCycles.FHKNUX.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // The business day prior to the 16th calendar day of the contract month at 12:05pm CT
                    var sixteenth = new DateTime(time.Year,time.Month,16);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(sixteenth, -1, holidays).Add(new TimeSpan(17, 5, 0));
                })
            },
            // Lumber and Softs
            // Lumber Futures (LBR): https://www.cmegroup.com/markets/agriculture/lumber-and-softs/lumber.contractSpecs.html
            {Symbol.Create(Futures.Forestry.Lumber, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Forestry.Lumber;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts (Jan, Mar, May, Jul, Sep, Nov) listed for 7 months
                    while (!FutureExpirationCycles.FHKNUX.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // The business day prior to the 16th calendar day of the contract month at 12:05pm CT
                    var sixteenth = new DateTime(time.Year,time.Month, 16);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(sixteenth, -1, holidays).Add(new TimeSpan(17, 5, 0));
                })
            },
            // Grains And OilSeeds Group
            // Chicago SRW Wheat (ZW): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/wheat_contract_specifications.html
            {Symbol.Create(Futures.Grains.SRWWheat, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Grains.SRWWheat;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // 15 monthly contracts of Mar, May, Jul, Sep, Dec listed annually following the termination of trading in the July contract of the current year.
                    while (!FutureExpirationCycles.HKNUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1, holidays);
                })
            },
            // HRW Wheat (KE): https://www.cmegroup.com/trading/agricultural/grain-and-oilseed/kc-wheat_contract_specifications.html
            {Symbol.Create(Futures.Grains.HRWWheat, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Grains.HRWWheat;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts (Mar, May, Jul, Sep, Dec) listed for  15 months
                    while (!FutureExpirationCycles.HKNUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1, holidays);
                })
            },
            // Corn (ZC): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/corn_contract_specifications.html
            {Symbol.Create(Futures.Grains.Corn, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Grains.Corn;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // 9 monthly contracts of Mar/3, May/5, Sep/9 and 8 monthly contracts of Jul/7 and Dec/12 listed annually after the termination of trading in the December contract of the current year.
                    while (!FutureExpirationCycles.HKNUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1, holidays);
                })
            },
            // Soybeans (ZS): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/soybean_contract_specifications.html
            {Symbol.Create(Futures.Grains.Soybeans, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Grains.Soybeans;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // 15 monthly contracts of Jan/1, Mar/3, May/5, Aug/8, Sep/9 and 8 monthly contracts of Jul/7 and Nov/11 listed annually after the termination of trading in the November contract of the current year.
                    while (!FutureExpirationCycles.FHKNQUX.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1, holidays);
                })
            },
            // SoybeanMeal (ZM): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/soybean-meal_contract_specifications.html
            {Symbol.Create(Futures.Grains.SoybeanMeal, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Grains.SoybeanMeal;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // 	15 monthly contracts of Jan/1, Mar/3, May/5, Aug/8, Sep/9 and 12 monthly contracts of Jul/7, Oct/10, Dec/12 listed annually after the termination of trading in the December contract of the current year.
                    while (!FutureExpirationCycles.FHKNQUVZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1, holidays);
                })
            },
            // SoybeanOil (ZL): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/soybean-oil_contract_specifications.html
            {Symbol.Create(Futures.Grains.SoybeanOil, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Grains.SoybeanOil;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // 	15 monthly contracts of Jan/1, Mar/3, May/5, Aug/8, Sep/9 and 12 monthly contracts of Jul/7, Oct/10, Dec/12 listed annually after the termination of trading in the December contract of the current year.
                    while (!FutureExpirationCycles.FHKNQUVZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1, holidays);
                })
            },
            // Oats (ZO): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/oats_contract_specifications.html
            {Symbol.Create(Futures.Grains.Oats, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Grains.Oats;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts (Mar, May, Jul, Sep, Dec) listed for 10 months and 1 additional Jul and 1 additional Sep contract listed in September
                    while (!FutureExpirationCycles.HKNUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1, holidays);
                })
            },
            // Black Sea Corn Financially Settled (Platts) (BCF): https://www.cmegroup.com/trading/agricultural/grain-and-oilseed/black-sea-corn-financially-settled-platts_contract_specifications.html
            {Symbol.Create(Futures.Grains.BlackSeaCornFinanciallySettledPlatts, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Grains.BlackSeaCornFinanciallySettledPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for 15 consecutive months.
                    // Trading terminates on the last business day of the contract month which is also a Platts publication date for the price assessment.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Black Sea Wheat Financially Settled (Platts) (BWF): https://www.cmegroup.com/trading/agricultural/grain-and-oilseed/black-sea-wheat-financially-settled-platts_contract_specifications.html
            {Symbol.Create(Futures.Grains.BlackSeaWheatFinanciallySettledPlatts, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Grains.BlackSeaWheatFinanciallySettledPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 15 consecutive months
                    // Trading terminates on the last business day of the contract month which is also a Platts publication date for the price assessment.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Currencies group
            // U.S. Dollar Index(R) Futures (DX): https://www.theice.com/products/194/US-Dollar-Index-Futures
            {Symbol.Create(Futures.Currencies.USD, SecurityType.Future, Market.ICE), (time =>
                {
                    // Four months in the March/June/September/December quarterly expiration cycle
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Last Trading Day:
                    // Trading ceases at 10:16 Eastern time two days prior to settlement
                    //
                    // Final Settlement:
                    // The US Dollar Index is physically settled on the third Wednesday of the expiration month
                    // against six component currencies (euro, Japanese yen, British pound, Canadian dollar, Swedish
                    // krona and Swiss franc) in their respective percentage weights in the Index.
                    // Settlement rates may be quoted to three decimal places.

                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var twoDaysPrior = thirdWednesday.AddDays(-2);

                    return twoDaysPrior.Add(new TimeSpan(10, 16, 0));
                })
            },
            //  GBP (6B): http://www.cmegroup.com/trading/fx/g10/british-pound_contract_specifications.html
            {Symbol.Create(Futures.Currencies.GBP, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.GBP;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 20 consecutive quarters and serial contracts listed for 3 months

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(
                        thirdWednesday,
                        -2, holidays);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // CAD (6C): http://www.cmegroup.com/trading/fx/g10/canadian-dollar_contract_specifications.html
            {Symbol.Create(Futures.Currencies.CAD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.CAD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 20 consecutive quarters and serial contracts listed for 3 months

                    // 9:16 a.m. Central Time (CT) on the business day immediately preceding the third Wednesday of the contract month (usually Tuesday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var businessDayPrecedingThridWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -1, holidays);
                    return businessDayPrecedingThridWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // JPY (6J): http://www.cmegroup.com/trading/fx/g10/japanese-yen_contract_specifications.html
            {Symbol.Create(Futures.Currencies.JPY, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.JPY;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 20 consecutive quarters and serial contracts listed for 3 months

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(
                        thirdWednesday,
                        -2, holidays);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // CHF (6S): http://www.cmegroup.com/trading/fx/g10/swiss-franc_contract_specifications.html
            {Symbol.Create(Futures.Currencies.CHF, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.CHF;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 20 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(
                        thirdWednesday,
                        -2, holidays);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // EUR (6E): http://www.cmegroup.com/trading/fx/g10/euro-fx_contract_specifications.html
            {Symbol.Create(Futures.Currencies.EUR, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.EUR;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 20 consecutive quarters and serial contracts listed for 3 months

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(
                        thirdWednesday,
                        -2, holidays);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // AUD (6A): http://www.cmegroup.com/trading/fx/g10/australian-dollar_contract_specifications.html
            {Symbol.Create(Futures.Currencies.AUD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.AUD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 20 consecutive quarters and serial contracts listed for 3 months

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(
                        thirdWednesday,
                        -2, holidays);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // NZD (6N): http://www.cmegroup.com/trading/fx/g10/new-zealand-dollar_contract_specifications.html
            {Symbol.Create(Futures.Currencies.NZD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.NZD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 6 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(
                        thirdWednesday,
                        -2, holidays);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // RUB (6R): https://www.cmegroup.com/trading/fx/emerging-market/russian-ruble_contract_specifications.html
            {Symbol.Create(Futures.Currencies.RUB, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.RUB;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contacts listed for 12 consecutive months and quarterly contracts (Mar, Jun, Sep, Dec) listed for16 additional quarters
                    // 11:00 a.m. Mosccow time on the fifteenth day of the month, or, if not a business day, on the next business day for the Moscow interbank foreign exchange market.
                    var fifteenth = new DateTime(time.Year, time.Month, 15);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(fifteenth, holidays))
                    {
                        fifteenth = FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth, 1, holidays);
                    }
                    return fifteenth.Add(new TimeSpan(08,0,0));
                })
            },
            // BRL (6L): https://www.cmegroup.com/trading/fx/emerging-market/brazilian-real_contract_specifications.html
            {Symbol.Create(Futures.Currencies.BRL, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.BRL;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 60 consecutive months
                    // On the last business day of the month, at 9:15 a.m. CT, immediately preceding the contract month, on which the Central Bank of Brazil is scheduled to publish its final end-of-month (EOM), "Commercial exchange rate for Brazilian reais per U.S. dollar for cash delivery" (PTAX rate).
                    var lastPrecedingBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(time, -1, holidays);
                    lastPrecedingBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastPrecedingBusinessDay, -1, holidays);

                    return lastPrecedingBusinessDay.Add(new TimeSpan(14,15,0));
                })
            },
            // MXN (6M): https://www.cmegroup.com/trading/fx/emerging-market/mexican-peso_contract_specifications.html
            {Symbol.Create(Futures.Currencies.MXN, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MXN;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 13 consecutive  months and 2 additional quarterly contracts (Mar, Jun, Sep, Dec)
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // ZAR (6Z): https://www.cmegroup.com/trading/fx/emerging-market/south-african-rand_contract_specifications.html
            {Symbol.Create(Futures.Currencies.ZAR, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.ZAR;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 13 consecutive months and quarterly contracts (Mar, Jun, Sep, Dec) listed for 4 consecutive quarters
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday)
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // AUD/CAD (ACD): https://www.cmegroup.com/trading/fx/g10/australian-dollar-canadian-dollar_contract_specifications.html
            {Symbol.Create(Futures.Currencies.AUDCAD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.AUDCAD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Six months in the March quarterly cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday)
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday =  FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // Australian Dollar/Japanese Yen (AJY): https://www.cmegroup.com/trading/fx/g10/australian-dollar-japanese-yen_contract_specifications.html
            {Symbol.Create(Futures.Currencies.AUDJPY, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.AUDJPY;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Six months in the March quarterly cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Australian Dollar/New Zealand Dollar (ANE): https://www.cmegroup.com/trading/fx/g10/australian-dollar-new-zealand-dollar_contract_specifications.html
            {Symbol.Create(Futures.Currencies.AUDNZD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.AUDNZD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Six months in the March quarterly cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Bitcoin (BTC): https://www.cmegroup.com/trading/equity-index/us-index/bitcoin_contract_specifications.html
            {Symbol.Create(Futures.Currencies.BTC, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.BTC;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 6 consecutive months and 2 additional Dec contract months. If the 6 consecutive months includes Dec, list only 1 additional Dec contract month.
                    // Trading terminates at 4:00 p.m. London time on the last Friday of the contract month. If that day is not a business day in both the U.K. and the US, trading terminates on the preceding day that is a business day for both the U.K. and the U.S..
                    var lastFriday =FuturesExpiryUtilityFunctions.LastFriday(time);
                    lastFriday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastFriday, -1, holidays);

                    return lastFriday.Add(new TimeSpan(15, 0, 0));
                })
            },
            // Ether (ETH): https://www.cmegroup.com/markets/cryptocurrencies/ether/ether.contractSpecs.html
            {Symbol.Create(Futures.Currencies.ETH, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.ETH;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 6 consecutive months, quarterly contracts (Mar, Jun, Sep, Dec) listed for 4 additional quarters and a second Dec contract if only one is listed.
                    // Trading terminates at 4:00 p.m. London time on the last Friday of the contract month that is either a London or U.S. business day. If the last Friday of the contract month day is not a business day in both London and the U.S., trading terminates on the prior London or U.S. business day.
                    var lastFriday = FuturesExpiryUtilityFunctions.LastFriday(time);
                    lastFriday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastFriday, -1, holidays);

                    return lastFriday.Add(new TimeSpan(15, 0, 0));
                })
            },
            // Canadian Dollar/Japanese Yen (CJY): https://www.cmegroup.com/trading/fx/g10/canadian-dollar-japanese-yen_contract_specifications.html
            {Symbol.Create(Futures.Currencies.CADJPY, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.CADJPY;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Six months in the March quarterly cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Standard-Size USD/Offshore RMB (CNH): https://www.cmegroup.com/trading/fx/emerging-market/usd-cnh_contract_specifications.html
            {Symbol.Create(Futures.Currencies.StandardSizeUSDOffshoreRMBCNH, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.StandardSizeUSDOffshoreRMBCNH;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for 13 consecutive months and quarterly contracts (Mar, Jun, Sep, Dec) listed for the next 8  quarters.
                    // Trading terminates on the second Hong Kong business day prior to the third Wednesday of the contract month at 11:00 a.m. Hong Kong local time.
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = thirdWednesday.AddDays(-2);
                    while (holidays.Contains(secondBusinessDayPrecedingThirdWednesday) || !secondBusinessDayPrecedingThirdWednesday.IsCommonBusinessDay())
                    {
                        secondBusinessDayPrecedingThirdWednesday = secondBusinessDayPrecedingThirdWednesday.AddDays(-1);
                    }

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(3,0,0));
                })
            },
            // E-mini Euro FX (E7): https://www.cmegroup.com/trading/fx/g10/e-mini-euro-fx_contract_specifications.html
            {Symbol.Create(Futures.Currencies.EuroFXEmini, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.EuroFXEmini;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 2 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Euro/Australian Dollar (EAD): https://www.cmegroup.com/trading/fx/g10/euro-fx-australian-dollar_contract_specifications.html
            {Symbol.Create(Futures.Currencies.EURAUD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.EURAUD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 6 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Euro/Canadian Dollar (ECD): https://www.cmegroup.com/trading/fx/g10/euro-fx-canadian-dollar_contract_specifications.html
            {Symbol.Create(Futures.Currencies.EURCAD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.EURCAD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 6 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading terminates at 9:16 a.m. CT on the second business day prior to the third Wednesday of the contract month.
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Euro/Swedish Krona (ESK): https://www.cmegroup.com/trading/fx/g10/euro-fx-swedish-krona_contract_specifications.html
            {Symbol.Create(Futures.Currencies.EURSEK, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.EURSEK;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Six months in the March quarterly cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // E-mini Japanese Yen (J7): https://www.cmegroup.com/trading/fx/g10/e-mini-japanese-yen_contract_specifications.html
            {Symbol.Create(Futures.Currencies.JapaneseYenEmini, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.JapaneseYenEmini;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Two months in the March quarterly cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                   // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Financials group
            // Y30TreasuryBond (ZB): http://www.cmegroup.com/trading/interest-rates/us-treasury/30-year-us-treasury-bond_contract_specifications.html
            {Symbol.Create(Futures.Financials.Y30TreasuryBond, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.Y30TreasuryBond;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 3 quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    //  Seventh business day preceding the last business day of the delivery month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    var seventhBusinessDayPrecedingLastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay,-7, holidays);
                    return seventhBusinessDayPrecedingLastBusinessDay.Add(new TimeSpan(12,01,0));
                })
            },
            // Y10TreasuryNote (ZN): http://www.cmegroup.com/trading/interest-rates/us-treasury/10-year-us-treasury-note_contract_specifications.html
            {Symbol.Create(Futures.Financials.Y10TreasuryNote, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.Y10TreasuryNote;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 3 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    //  Seventh business day preceding the last business day of the delivery month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    var seventhBusinessDayPrecedingLastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay,-7, holidays);
                    return seventhBusinessDayPrecedingLastBusinessDay.Add(new TimeSpan(12,01,0));
                })
            },
            // Y5TreasuryNote (ZF): http://www.cmegroup.com/trading/interest-rates/us-treasury/5-year-us-treasury-note_contract_specifications.html
            {Symbol.Create(Futures.Financials.Y5TreasuryNote, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.Y5TreasuryNote;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 3 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Last business day of the calendar month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    return lastBusinessDay.Add(new TimeSpan(12,01,0));
                })
            },
            // Y2TreasuryNote (ZT): http://www.cmegroup.com/trading/interest-rates/us-treasury/2-year-us-treasury-note_contract_specifications.html
            {Symbol.Create(Futures.Financials.Y2TreasuryNote, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.Y2TreasuryNote;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 3 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Last business day of the calendar month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    return lastBusinessDay.Add(new TimeSpan(12,01,0));
                })
            },
            // Eurodollar (GE): https://www.cmegroup.com/trading/interest-rates/stir/eurodollar_contract_specifications.html
            {Symbol.Create(Futures.Financials.EuroDollar, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Financials.EuroDollar;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 40 consecutive quarters and the nearest 4 serial contract months.
                    // List a new quarterly contract for trading on the last trading day of the nearby expiry.

                    // Termination of trading:
                    // Second London bank business day before 3rd Wednesday of the contract month. Trading
                    // in expiring contracts terminates at 11:00 a.m. London time on the last trading day.

                    return FuturesExpiryUtilityFunctions.AddBusinessDays(FuturesExpiryUtilityFunctions.ThirdWednesday(time), -2, holidays)
                        .Add(TimeSpan.FromHours(11));
                })
            },
            // 5-Year USD MAC Swap (F1U): https://www.cmegroup.com/trading/interest-rates/swap-futures/5-year-usd-mac-swap_contract_specifications.html
            {Symbol.Create(Futures.Financials.FiveYearUSDMACSwap, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.FiveYearUSDMACSwap;

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 2 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Second London business day before 3rd Wednesday of futures Delivery Month. Trading in expiring contracts closes at 2:00 p.m. on the last trading day.
                    var secondBusinessDayBeforeThirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time).AddDays(-2);
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Because we're using a London calendar, we need to put entries in MHDB and not use `USHolidays.Dates`
                    while (holidays.Contains(secondBusinessDayBeforeThirdWednesday) || !secondBusinessDayBeforeThirdWednesday.IsCommonBusinessDay())
                    {
                        secondBusinessDayBeforeThirdWednesday = secondBusinessDayBeforeThirdWednesday.AddDays(-1);
                    }

                    return secondBusinessDayBeforeThirdWednesday.Add(new TimeSpan(19, 0, 0));
                })
            },
            // Ultra U.S. Treasury Bond (UB): https://www.cmegroup.com/trading/interest-rates/us-treasury/ultra-t-bond_contract_specifications.html
            {Symbol.Create(Futures.Financials.UltraUSTreasuryBond, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.UltraUSTreasuryBond;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 3 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Seventh business day preceding the last business day of the delivery month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    var sevenBusinessDaysBeforeLastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 8, holidays);

                    return sevenBusinessDaysBeforeLastBusinessDay.Add(new TimeSpan(12, 1, 0));
                })
            },
            // Ultra 10-Year U.S. Treasury Note (TN): https://www.cmegroup.com/trading/interest-rates/us-treasury/ultra-10-year-us-treasury-note_contract_specifications.html
            {Symbol.Create(Futures.Financials.UltraTenYearUSTreasuryNote, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.UltraTenYearUSTreasuryNote;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 3 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                     // Trading terminates on the 7th business day before the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 8, holidays);
                })
            },
            // Energy group
            // Propane Non LDH Mont Belvieu (1S): https://www.cmegroup.com/trading/energy/petrochemicals/propane-non-ldh-mt-belvieu-opis-balmo-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.PropaneNonLDHMontBelvieu, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.PropaneNonLDHMontBelvieu;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly BALMO contracts listed for the current month and the following month listed 10 business days prior to the start of the contract month
                    // Trading shall cease on the last business day of the contract month (no time specified)
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Argus Propane Far East Index BALMO (22): https://www.cmegroup.com/trading/energy/petrochemicals/argus-propane-far-east-index-balmo-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.ArgusPropaneFarEastIndexBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.ArgusPropaneFarEastIndexBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly BALMO contracts listed for three cconsecutive months
                    // Trading shall cease on the last business day of the contract month. Business days are based on the Singapore Public Holiday calendar.
                    // TODO: Might need singapore calendar
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Mini European 3.5% Fuel Oil Barges FOB Rdam (Platts) (A0D): https://www.cmegroup.com/trading/energy/refined-products/mini-european-35pct-fuel-oil-platts-barges-fob-rdam-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.MiniEuropeanThreePointPercentFiveFuelOilBargesPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MiniEuropeanThreePointPercentFiveFuelOilBargesPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for the current year and the next 4 calendar years.
                    // Trading shall cease on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Mini Singapore Fuel Oil 180 cst (Platts) (A0F): https://www.cmegroup.com/trading/energy/refined-products/mini-singapore-fuel-oil-180-cst-platts-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.MiniSingaporeFuelOil180CstPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MiniSingaporeFuelOil180CstPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for the current year and the next 5 calendar years.
                    // Trading shall cease on the last business day of the contract month.
                    // Special case exists where the last trade occurs on US holiday, but not an exchange holiday (markets closed)
                    // In order to fix that case, we will start from the last day of the month and go backwards checking if it's a weekday and a holiday
                    var lastDay = new DateTime(time.Year, time.Month, DateTime.DaysInMonth(time.Year, time.Month));

                    while (holidays.Contains(lastDay) || !lastDay.IsCommonBusinessDay())
                    {
                        lastDay = lastDay.AddDays(-1);
                    }

                    return lastDay;
                })
            },
            // Gulf Coast ULSD (Platts) Up-Down BALMO Futures (A1L): https://www.cmegroup.com/trading/energy/refined-products/ulsd-up-down-balmo-calendar-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.GulfCoastULSDPlattsUpDownBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GulfCoastULSDPlattsUpDownBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly BALMO contracts listed for the current month and the following month listed 10 business days prior to the start of the contract month
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Gulf Coast Jet (Platts) Up-Down BALMO Futures (A1M): https://www.cmegroup.com/trading/energy/refined-products/jet-fuel-up-down-balmo-calendar-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.GulfCoastJetPlattsUpDownBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GulfCoastJetPlattsUpDownBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly BALMO contracts listed for the current month and the following month listed 10 business days prior to the start of the contract month
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Propane Non-LDH Mont Belvieu (OPIS) Futures (A1R): https://www.cmegroup.com/trading/energy/petrochemicals/propane-non-ldh-mt-belvieu-opis-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.PropaneNonLDHMontBelvieuOPIS, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.PropaneNonLDHMontBelvieuOPIS;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for 48 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // European Propane CIF ARA (Argus) BALMO Futures (A32): https://www.cmegroup.com/trading/energy/petrochemicals/european-propane-cif-ara-argus-balmo-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.EuropeanPropaneCIFARAArgusBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.EuropeanPropaneCIFARAArgusBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly BALMO contracts listed for 3 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Premium Unleaded Gasoline 10 ppm FOB MED (Platts) Futures (A3G): https://www.cmegroup.com/trading/energy/refined-products/premium-unleaded-10-ppm-platts-fob-med-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.PremiumUnleadedGasoline10ppmFOBMEDPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.PremiumUnleadedGasoline10ppmFOBMEDPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // 48 consecutive months
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Argus Propane Far East Index Futures (A7E): https://www.cmegroup.com/trading/energy/petrochemicals/argus-propane-far-east-index-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.ArgusPropaneFarEastIndex, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.ArgusPropaneFarEastIndex;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for 48 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Gasoline Euro-bob Oxy NWE Barges (Argus) Crack Spread BALMO Futures (A7I): https://www.cmegroup.com/trading/energy/refined-products/gasoline-euro-bob-oxy-new-barges-crack-spread-balmo-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.GasolineEurobobOxyNWEBargesArgusCrackSpreadBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GasolineEurobobOxyNWEBargesArgusCrackSpreadBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly BALMO contracts listed for 3 consecutive months
                    // Trading ceases on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Mont Belvieu Natural Gasoline (OPIS) Futures (A7Q): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-natural-gasoline-5-decimal-opis-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.MontBelvieuNaturalGasolineOPIS, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MontBelvieuNaturalGasolineOPIS;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for 56 consecutive months
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Mont Belvieu Normal Butane (OPIS) BALMO Futures (A8J): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-normal-butane-opis-balmo-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.MontBelvieuNormalButaneOPISBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MontBelvieuNormalButaneOPISBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly BALMO contracts listed for the current month and the following month listed 10 business days prior to the start of the contract month
                    // Trading terminates on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Conway Propane (OPIS) Futures (A8K): https://www.cmegroup.com/trading/energy/petrochemicals/conway-propane-opis-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.ConwayPropaneOPIS, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.ConwayPropaneOPIS;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for the current year and the next 4 calendar years.
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Mont Belvieu LDH Propane (OPIS) BALMO Futures (A8O): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-ldh-propane-opis-balmo-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.MontBelvieuLDHPropaneOPISBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MontBelvieuLDHPropaneOPISBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly BALMO contracts listed for the current month and the following month listed 10 business days prior to the start of the contract month
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Argus Propane Far East Index vs. European Propane CIF ARA (Argus) Futures (A91): https://www.cmegroup.com/trading/energy/petrochemicals/argus-propane-far-east-index-vs-european-propane-cif-ara-argus-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.ArgusPropaneFarEastIndexVsEuropeanPropaneCIFARAArgus, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.ArgusPropaneFarEastIndexVsEuropeanPropaneCIFARAArgus;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for 36 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Argus Propane (Saudi Aramco) Futures (A9N): https://www.cmegroup.com/trading/energy/petrochemicals/argus-propane-saudi-aramco-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.ArgusPropaneSaudiAramco, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.ArgusPropaneSaudiAramco;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for 48 consecutive months
                    // Trading shall terminate on the last business day of the month prior to the contract month.
                    // Business days are based on the Singapore Public Holiday Calendar.
                    // Special case in 2021 where last traded date falls on US Holiday, but not exchange holiday
                    var previousMonth = time.AddMonths(-1);
                    var lastDay = new DateTime(previousMonth.Year, previousMonth.Month, DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month));

                    while (!lastDay.IsCommonBusinessDay() || holidays.Contains(lastDay))
                    {
                        lastDay = lastDay.AddDays(-1);
                    }

                    return lastDay;
                })
            },
            // Group Three ULSD (Platts) vs. NY Harbor ULSD Futures (AA6): https://www.cmegroup.com/trading/energy/refined-products/group-three-ultra-low-sulfur-diesel-ulsd-platts-vs-heating-oil-spread-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.GroupThreeULSDPlattsVsNYHarborULSD, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GroupThreeULSDPlattsVsNYHarborULSD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Group Three Sub-octane Gasoline (Platts) vs. RBOB Futures (AA8): https://www.cmegroup.com/trading/energy/refined-products/group-three-unleaded-gasoline-platts-vs-rbob-spread-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.GroupThreeSuboctaneGasolinePlattsVsRBOB, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GroupThreeSuboctaneGasolinePlattsVsRBOB;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // 36 consecutive months
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Singapore Fuel Oil 180 cst (Platts) BALMO Futures (ABS): https://www.cmegroup.com/trading/energy/refined-products/singapore-180cst-fuel-oil-balmo-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.SingaporeFuelOil180cstPlattsBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.SingaporeFuelOil180cstPlattsBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly BALMO contracts listed for 3 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Singapore Fuel Oil 380 cst (Platts) BALMO Futures (ABT): https://www.cmegroup.com/trading/energy/refined-products/singapore-380cst-fuel-oil-balmo-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.SingaporeFuelOil380cstPlattsBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.SingaporeFuelOil380cstPlattsBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly BALMO contracts listed for 3 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Mont Belvieu Ethane (OPIS) Futures (AC0): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-ethane-opis-5-decimals-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.MontBelvieuEthaneOPIS, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MontBelvieuEthaneOPIS;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for the current year and the next 4 calendar years.
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Mont Belvieu Normal Butane (OPIS) Futures (AD0): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-normal-butane-5-decimals-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.MontBelvieuNormalButaneOPIS, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MontBelvieuNormalButaneOPIS;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for the current year and next 4 calendar years.
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Brent Crude Oil vs. Dubai Crude Oil (Platts) Futures (ADB): https://www.cmegroup.com/trading/energy/crude-oil/brent-dubai-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.BrentCrudeOilVsDubaiCrudeOilPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.BrentCrudeOilVsDubaiCrudeOilPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Trading shall cease on the last London and Singapore business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Argus LLS vs. WTI (Argus) Trade Month Futures (AE5): https://www.cmegroup.com/trading/energy/crude-oil/argus-lls-vs-wti-argus-trade-month-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.ArgusLLSvsWTIArgusTradeMonth, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.ArgusLLSvsWTIArgusTradeMonth;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Trading shall cease at the close of trading on the last business day that falls on or before the 25th calendar day of the month prior to the contract month. If the 25th calendar day is a weekend or holiday, trading shall cease on the first business day prior to the 25th calendar day.
                    var previousMonth = time.AddMonths(-1);
                    var twentyFifthDay = new DateTime(previousMonth.Year, previousMonth.Month, 25);
                    while (!twentyFifthDay.IsCommonBusinessDay() || holidays.Contains(twentyFifthDay))
                    {
                        twentyFifthDay = FuturesExpiryUtilityFunctions.AddBusinessDays(twentyFifthDay, -1, holidays);
                    }

                    return twentyFifthDay;
                })
            },
            // Singapore Gasoil (Platts) vs. Low Sulphur Gasoil (AGA): https://www.cmegroup.com/trading/energy/refined-products/gasoil-arb-singapore-gasoil-platts-vs-ice-rdam-gasoil-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.SingaporeGasoilPlattsVsLowSulphurGasoilFutures, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.SingaporeGasoilPlattsVsLowSulphurGasoilFutures;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // Monthly contracts listed for the current year and the next 2 calendar years.
                    // Trading ceases on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Los Angeles CARBOB Gasoline (OPIS) vs. RBOB Gasoline (AJL): https://www.cmegroup.com/trading/energy/refined-products/los-angeles-carbob-gasoline-opis-spread-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.LosAngelesCARBOBGasolineOPISvsRBOBGasoline, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.LosAngelesCARBOBGasolineOPISvsRBOBGasoline;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // 36 consecutive months
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Los Angeles Jet (OPIS) vs. NY Harbor ULSD (AJS): https://www.cmegroup.com/trading/energy/refined-products/los-angeles-carbob-gasoline-opis-spread-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.LosAngelesJetOPISvsNYHarborULSD, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.LosAngelesJetOPISvsNYHarborULSD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // 36 consecutive months
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Los Angeles CARB Diesel (OPIS) vs. NY Harbor ULSD (AKL): https://www.cmegroup.com/trading/energy/refined-products/los-angeles-carbob-diesel-opis-spread-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.LosAngelesCARBDieselOPISvsNYHarborULSD, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.LosAngelesCARBDieselOPISvsNYHarborULSD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // 3 consecutive years
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // European Naphtha (Platts) BALMO (AKZ): https://www.cmegroup.com/trading/energy/refined-products/european-naphtha-balmo-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.EuropeanNaphthaPlattsBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.EuropeanNaphthaPlattsBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly BALMO contracts listed for 3 consecutive months
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // European Propane CIF ARA (Argus) (APS): https://www.cmegroup.com/trading/energy/petrochemicals/european-propane-cif-ara-argus-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.EuropeanPropaneCIFARAArgus, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.EuropeanPropaneCIFARAArgus;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 3 calendar years.
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Mont Belvieu Natural Gasoline (OPIS) BALMO (AR0): https://www.cmegroup.com/trading/energy/petrochemicals/mt-belvieu-natural-gasoline-balmo-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.MontBelvieuNaturalGasolineOPISBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MontBelvieuNaturalGasolineOPISBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly BALMO contracts listed for the current month and the following month listed 10 business days prior to the start of the contract month
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // RBOB Gasoline Crack Spread (ARE): https://www.cmegroup.com/trading/energy/refined-products/rbob-crack-spread-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.RBOBGasolineCrackSpread, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.RBOBGasolineCrackSpread;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // The current year plus the next three calendar years
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Gulf Coast HSFO (Platts) BALMO (AVZ): https://www.cmegroup.com/trading/energy/refined-products/gulf-coast-3pct-fuel-oil-balmo-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.GulfCoastHSFOPlattsBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GulfCoastHSFOPlattsBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly BALMO contracts listed for the current month and the following month listed 10 business days prior to the start of the contract month
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Mars (Argus) vs. WTI Trade Month (AYV): https://www.cmegroup.com/trading/energy/crude-oil/mars-crude-oil-argus-vs-wti-trade-month-spread-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.MarsArgusVsWTITradeMonth, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MarsArgusVsWTITradeMonth;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 5 calendar years.
                    // Trading shall cease at the close of trading on the last business day that falls on or before the 25th calendar day of the
                    // month prior to the contract month. If the 25th calendar day is a weekend or holiday, trading shall cease on the
                    // first business day prior to the 25th calendar day.
                    var twentyFifthDayPriorMonth = new DateTime(time.Year, time.Month, 25).AddMonths(-1);
                    while (!FuturesExpiryUtilityFunctions.NotHoliday(twentyFifthDayPriorMonth, holidays) || holidays.Contains(twentyFifthDayPriorMonth))
                    {
                        twentyFifthDayPriorMonth = FuturesExpiryUtilityFunctions.AddBusinessDays(twentyFifthDayPriorMonth, -1, holidays);
                    }

                    return twentyFifthDayPriorMonth;
                })
            },
            // Mars (Argus) vs. WTI Financial (AYX): https://www.cmegroup.com/trading/energy/crude-oil/mars-crude-oil-argus-vs-wti-calendar-spread-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.MarsArgusVsWTIFinancial, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MarsArgusVsWTIFinancial;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // The current year and the next five (5) consecutive calendar years.
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Ethanol T2 FOB Rdam Including Duty (Platts) (AZ1): https://www.cmegroup.com/trading/energy/ethanol/ethanol-platts-t2-fob-rotterdam-including-duty-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.EthanolT2FOBRdamIncludingDutyPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.EthanolT2FOBRdamIncludingDutyPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 36 consecutive months
                    // Trading terminates on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Mont Belvieu LDH Propane (OPIS) (B0): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-propane-5-decimals-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.MontBelvieuLDHPropaneOPIS, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MontBelvieuLDHPropaneOPIS;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 4 calendar years.
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Gasoline Euro-bob Oxy NWE Barges (Argus) (B7H): https://www.cmegroup.com/trading/energy/refined-products/gasoline-euro-bob-oxy-new-barges-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.GasolineEurobobOxyNWEBargesArgus, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GasolineEurobobOxyNWEBargesArgus;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 36 consecutive months
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // WTI-Brent Financial (BK): https://www.cmegroup.com/trading/energy/crude-oil/wti-brent-ice-calendar-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.WTIBrentFinancial, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.WTIBrentFinancial;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 8 calendar years.
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // 3.5% Fuel Oil Barges FOB Rdam (Platts) Crack Spread (1000mt) (BOO): https://www.cmegroup.com/trading/energy/refined-products/35pct-fuel-oil-platts-barges-fob-rdam-crack-spread-1000mt-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread1000mt, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread1000mt;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 4 calendar years.
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Gasoline Euro-bob Oxy NWE Barges (Argus) BALMO (BR7): https://www.cmegroup.com/trading/energy/refined-products/gasoline-euro-bob-oxy-new-barges-balmo-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.GasolineEurobobOxyNWEBargesArgusBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GasolineEurobobOxyNWEBargesArgusBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly BALMO contracts listed for 3 consecutive months
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Brent Last Day Financial (BZ): https://www.cmegroup.com/trading/energy/crude-oil/brent-crude-oil-last-day_contract_specifications.html
            {Symbol.Create(Futures.Energy.BrentLastDayFinancial, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.BrentLastDayFinancial;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 7 calendar years and 3 additional contract months.
                    // Trading terminates the last London business day of the month, 2 months prior to the contract month except for the February contract month which terminates the 2nd last London business day of the month, 2 months prior to the contract month.
                    var twoMonthsPriorToContractMonth = time.AddMonths(-2);

                    DateTime lastBusinessDay;

                    if (twoMonthsPriorToContractMonth.Month == 2)
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(twoMonthsPriorToContractMonth, 1, holidays);
                    }
                    else
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(twoMonthsPriorToContractMonth, 1, holidays);
                    }

                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // CrudeOilWTI (CL): http://www.cmegroup.com/trading/energy/crude-oil/light-sweet-crude_contract_specifications.html
            {Symbol.Create(Futures.Energy.CrudeOilWTI, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.CrudeOilWTI;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 10 calendar years and 2 additional contract months.
                    // Trading in the current delivery month shall cease on the third business day prior to the twenty-fifth calendar day of the month preceding the delivery month. If the twenty-fifth calendar day of the month is a non-business day, trading shall cease on the third business day prior to the last business day preceding the twenty-fifth calendar day. In the event that the official Exchange holiday schedule changes subsequent to the listing of a Crude Oil futures, the originally listed expiration date shall remain in effect.In the event that the originally listed expiration day is declared a holiday, expiration will move to the business day immediately prior.
                    var twentyFifth = new DateTime(time.Year,time.Month,25);
                    twentyFifth = twentyFifth.AddMonths(-1);

                    var businessDays = -3;
                    if(!FuturesExpiryUtilityFunctions.NotHoliday(twentyFifth, holidays))
                    {
                        // if the 25th is a holiday we substract 1 extra bussiness day
                        businessDays -= 1;
                    }
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(twentyFifth, businessDays, holidays);
                })
            },
            // Gulf Coast CBOB Gasoline A2 (Platts) vs. RBOB Gasoline (CRB): https://www.cmegroup.com/trading/energy/refined-products/gulf-coast-cbob-gasoline-a2-platts-vs-rbob-spread-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.GulfCoastCBOBGasolineA2PlattsVsRBOBGasoline, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GulfCoastCBOBGasolineA2PlattsVsRBOBGasoline;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // 36 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Clearbrook Bakken Sweet Crude Oil Monthly Index (Net Energy) (CSW): https://www.cmegroup.com/trading/energy/crude-oil/clearbrook-bakken-crude-oil-index-net-energy_contract_specifications.html
            {Symbol.Create(Futures.Energy.ClearbrookBakkenSweetCrudeOilMonthlyIndexNetEnergy, SecurityType.Future, Market.NYMEX), (time =>
                {
                    // Monthly contracts listed for the current year and the next 3 calendar years.
                    // Trading terminates one Canadian business day prior to the Notice of Shipments (NOS) date on the Enbridge Pipeline. The NOS date occurs on or about the 20th calendar day of the month, subject to confirmation by Enbridge Pipeline. The official schedule for the NOS dates will be made publicly available by Enbridge.
                    // This report is behind a portal that requires registration (privately). As such, we cannot access the notice of shipment dates, but we can keep track
                    // of the CME group's website in order to discover the NOS dates
                    // Publication dates are also erratic. We must maintain a separate list from MHDB in order to keep track of these days
                    DateTime publicationDate;

                    if (!EnbridgeNoticeOfShipmentDates.TryGetValue(time, out publicationDate))
                    {
                        publicationDate = new DateTime(time.Year, time.Month, 21).AddMonths(-1);
                    }
                    do
                    {
                        publicationDate = publicationDate.AddDays(-1);
                    }
                    while (!publicationDate.IsCommonBusinessDay());

                    return publicationDate;
                })
            },
            // WTI Financial (CSX): https://www.cmegroup.com/trading/energy/crude-oil/west-texas-intermediate-wti-crude-oil-calendar-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.WTIFinancial, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.WTIFinancial;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 8 calendar years.
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Chicago Ethanol (Platts) (CU): https://www.cmegroup.com/trading/energy/ethanol/chicago-ethanol-platts-swap_contract_specifications.html a
            {Symbol.Create(Futures.Energy.ChicagoEthanolPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.ChicagoEthanolPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 36 consecutive months
                    // Trading terminates on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Singapore Mogas 92 Unleaded (Platts) Brent Crack Spread (D1N): https://www.cmegroup.com/trading/energy/refined-products/singapore-mogas-92-unleaded-platts-brent-crack-spread-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.SingaporeMogas92UnleadedPlattsBrentCrackSpread, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.SingaporeMogas92UnleadedPlattsBrentCrackSpread;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next calendar year.
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Dubai Crude Oil (Platts) Financial (DCB): https://www.cmegroup.com/trading/energy/crude-oil/dubai-crude-oil-calendar-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.DubaiCrudeOilPlattsFinancial, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.DubaiCrudeOilPlattsFinancial;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next five calendar years.
                    // Trading shall cease on the last London and Singapore business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Japan C&amp;F Naphtha (Platts) BALMO (E6): https://www.cmegroup.com/trading/energy/refined-products/japan-naphtha-balmo-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.JapanCnFNaphthaPlattsBALMO, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.JapanCnFNaphthaPlattsBALMO;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly BALMO contracts listed for 3 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // Ethanol (EH): https://www.cmegroup.com/trading/energy/ethanol/cbot-ethanol_contract_specifications.html
            {Symbol.Create(Futures.Energy.Ethanol, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Energy.Ethanol;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 36 consecutive months
                    // Trading terminates on 3rd business day of the contract month in "ctm"

                    return FuturesExpiryUtilityFunctions.NthBusinessDay(time, 3, holidays);
                })
            },
            // European Naphtha (Platts) Crack Spread (EN): https://www.cmegroup.com/trading/energy/refined-products/european-naphtha-crack-spread-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.EuropeanNaphthaPlattsCrackSpread, SecurityType.Future, Market.NYMEX), (time =>
                {
                    // Monthly contracts listed for the current year and the next 3 calendar years
                    // Trading ceases on the last business day of the contract month.
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.EuropeanNaphthaPlattsCrackSpread;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = lastBusinessDay.AddDays(-1);
                    }

                    return lastBusinessDay;
                })
            },
            // European Propane CIF ARA (Argus) vs. Naphtha Cargoes CIF NWE (Platts) (EPN): https://www.cmegroup.com/trading/energy/refined-products/european-propane-cif-ara-argus-vs-naphtha-cif-nwe-platts-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.EuropeanPropaneCIFARAArgusVsNaphthaCargoesCIFNWEPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    // Monthly contracts listed for the current year and the next 3 calendar years.
                    // Trading shall cease on the last business day of the contract month.
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.EuropeanPropaneCIFARAArgusVsNaphthaCargoesCIFNWEPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = lastBusinessDay.AddDays(-1);
                    }

                    return lastBusinessDay;
                })
            },
            // Singapore Fuel Oil 380 cst (Platts) vs. European 3.5% Fuel Oil Barges FOB Rdam (Platts) (EVC): https://www.cmegroup.com/trading/energy/refined-products/singapore-fuel-oil-380-cst-platts-vs-european-35-fuel-oil-barges-fob-rdam-platts_contract_specifications.html
            {Symbol.Create(Futures.Energy.SingaporeFuelOil380cstPlattsVsEuropeanThreePointFivePercentFuelOilBargesFOBRdamPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.SingaporeFuelOil380cstPlattsVsEuropeanThreePointFivePercentFuelOilBargesFOBRdamPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 5 calendar years.
                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = lastBusinessDay.AddDays(-1);
                    }

                    return lastBusinessDay;
                })
            },
            // East-West Gasoline Spread (Platts-Argus) (EWG): https://www.cmegroup.com/trading/energy/refined-products/east-west-gasoline-spread-platts-argus-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.EastWestGasolineSpreadPlattsArgus, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.EastWestGasolineSpreadPlattsArgus;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 12 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // East-West Naphtha: Japan C&amp;F vs. Cargoes CIF NWE Spread (Platts) (EWN): https://www.cmegroup.com/trading/energy/refined-products/east-west-naphtha-japan-cf-vs-cargoes-cif-nwe-spread-platts-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.EastWestNaphthaJapanCFvsCargoesCIFNWESpreadPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    // Monthly contracts listed for 36 consecutive months
                    // Trading terminates on the last business day of the contract month.
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.EastWestNaphthaJapanCFvsCargoesCIFNWESpreadPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = lastBusinessDay.AddDays(-1);
                    }

                    return lastBusinessDay;
                })
            },
            // RBOB Gasoline vs. Euro-bob Oxy NWE Barges (Argus) (350,000 gallons) (EXR): https://www.cmegroup.com/trading/energy/refined-products/rbob-gasoline-vs-euro-bob-oxy-argus-nwe-barges-1000mt-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.RBOBGasolineVsEurobobOxyNWEBargesArgusThreeHundredFiftyThousandGallons, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.RBOBGasolineVsEurobobOxyNWEBargesArgusThreeHundredFiftyThousandGallons;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 36 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                })
            },
            // 3.5% Fuel Oil Barges FOB Rdam (Platts) Crack Spread Futures (FO): https://www.cmegroup.com/trading/energy/refined-products/northwest-europe-nwe-35pct-fuel-oil-rottderdam-crack-spread-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 4 calendar years.
                    // Trading ceases on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = lastBusinessDay.AddDays(-1);
                    }

                    return lastBusinessDay;
                })
            },
            // Freight Route TC14 (Baltic) (FRC): https://www.cmegroup.com/trading/energy/freight/freight-route-tc14-baltic-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.FreightRouteTC14Baltic, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.FreightRouteTC14Baltic;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 5 consecutive years.
                    // Trading terminates on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = lastBusinessDay.AddDays(-1);
                    }

                    return lastBusinessDay;
                })
            },
            // 1% Fuel Oil Cargoes FOB NWE (Platts) vs. 3.5% Fuel Oil Barges FOB Rdam (Platts) (FSS):  https://www.cmegroup.com/trading/energy/refined-products/fuel-oil-diff-1pct-nwe-cargoes-vs-35pct-barges-swap_contract_specifications.html
            {Symbol.Create(Futures.Energy.OnePercentFuelOilCargoesFOBNWEPlattsVsThreePointFivePercentFuelOilBargesFOBRdamPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    // Monthly contracts listed for 52 consecutive months
                    // Trading ceases on the last business day of the contract month.
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.OnePercentFuelOilCargoesFOBNWEPlattsVsThreePointFivePercentFuelOilBargesFOBRdamPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = lastBusinessDay.AddDays(-1);
                    }

                    return lastBusinessDay;
                })
            },
            // Gulf Coast HSFO (Platts) vs. European 3.5% Fuel Oil Barges FOB Rdam (Platts) (GCU): https://www.cmegroup.com/trading/energy/refined-products/gulf-coast-no6-fuel-oil-3pct-vs-european-3point5pct-fuel-oil-barges-fob-rdam-platts-swap-futures_contract_specifications.html
            {Symbol.Create(Futures.Energy.GulfCoastHSFOPlattsVsEuropeanThreePointFivePercentFuelOilBargesFOBRdamPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.GulfCoastHSFOPlattsVsEuropeanThreePointFivePercentFuelOilBargesFOBRdamPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 36 consecutive months
                    // Trading shall cease on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = lastBusinessDay.AddDays(-1);
                    }

                    return lastBusinessDay;
                })
            },
            // WTI Houston Crude Oil (HCL): https://www.cmegroup.com/trading/energy/crude-oil/wti-houston-crude-oil_contract_specifications.html
            {Symbol.Create(Futures.Energy.WTIHoustonCrudeOil, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.WTIHoustonCrudeOil;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed through and including Dec-21
                    // Trading terminates 3 business days prior to the twenty-fifth calendar day of the month prior to the contract month.  If the twenty-fifth calendar day is not a business day, trading terminates 3 business days prior to the business day preceding the twenty-fifth calendar day of the month prior to the contract month.
                    var twentyFifthDayInPriorMonth = new DateTime(time.Year, time.Month, 25).AddMonths(-1);
                    var i = 0;

                    while (i < 3 || !twentyFifthDayInPriorMonth.IsCommonBusinessDay() || holidays.Contains(twentyFifthDayInPriorMonth))
                    {
                        if (twentyFifthDayInPriorMonth.IsCommonBusinessDay() &&
                            !holidays.Contains(twentyFifthDayInPriorMonth))
                        {
                            i++;
                        }
                        twentyFifthDayInPriorMonth = twentyFifthDayInPriorMonth.AddDays(-1);
                    }

                    return twentyFifthDayInPriorMonth;
                })
            },
            // Natural Gas (Henry Hub) Last-day Financial (HH): https://www.cmegroup.com/trading/energy/natural-gas/natural-gas-last-day_contract_specifications.html
            {Symbol.Create(Futures.Energy.NaturalGasHenryHubLastDayFinancial, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.NaturalGasHenryHubLastDayFinancial;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 12 calendar years.
                    // Trading terminates on the third last business day of the month prior to the contract month.
                    var previousMonth = time.AddMonths(-1);
                    previousMonth = new DateTime(previousMonth.Year, previousMonth.Month, DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month));

                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(previousMonth, 3, holidays);
                })
            },
            // HeatingOil (HO): http://www.cmegroup.com/trading/energy/refined-products/heating-oil_contract_specifications.html
            {Symbol.Create(Futures.Energy.HeatingOil, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.HeatingOil;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 3 calendar years and 1 additional month.
                    // Trading in a current month shall cease on the last business day of the month preceding the delivery month.
                    var precedingMonth = time.AddMonths(-1);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(precedingMonth, 1, holidays);
                })
            },
            // Natural Gas (Henry Hub) Penultimate Financial (HP): https://www.cmegroup.com/trading/energy/natural-gas/natural-gas-penultimate_contract_specifications.html
            {Symbol.Create(Futures.Energy.NaturalGasHenryHubPenultimateFinancial, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.NaturalGasHenryHubPenultimateFinancial;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 5 calendar years.
                    // Trading terminates on the 4th last business day of the month prior to the contract month.
                    var previousMonth = time.AddMonths(-1);

                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(previousMonth, 4, holidays);
                })
            },
            // WTI Houston (Argus) vs. WTI Trade Month (HTT): https://www.cmegroup.com/trading/energy/crude-oil/wti-houston-argus-vs-wti-trade-month_contract_specifications.html
            {Symbol.Create(Futures.Energy.WTIHoustonArgusVsWTITradeMonth, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.WTIHoustonArgusVsWTITradeMonth;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 3 calendar years.
                    // Trading terminates on the last business day that falls on or before the 25th calendar day of the month prior to the contract month. If the 25th calendar day is a weekend or holiday, trading shall cease on the first business day prior to the 25th calendar day.
                    var twentyFifthPreviousMonth = new DateTime(time.Year, time.Month, 25).AddMonths(-1);
                    while (holidays.Contains(twentyFifthPreviousMonth) || !FuturesExpiryUtilityFunctions.NotHoliday(twentyFifthPreviousMonth, holidays))
                    {
                        twentyFifthPreviousMonth = FuturesExpiryUtilityFunctions.AddBusinessDays(twentyFifthPreviousMonth, -1, holidays);
                    }

                    return twentyFifthPreviousMonth;
                })
            },
            // Gasoline (RB): http://www.cmegroup.com/trading/energy/refined-products/rbob-gasoline_contract_specifications.html
            {Symbol.Create(Futures.Energy.Gasoline, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.Gasoline;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 3 calendar years and 1 additional month.
                    // Trading in a current delivery month shall cease on the last business day of the month preceding the delivery month.
                    var precedingMonth = time.AddMonths(-1);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(precedingMonth, 1, holidays);
                })
            },
            // Natural Gas (NG) : http://www.cmegroup.com/trading/energy/natural-gas/natural-gas_contract_specifications.html
            {Symbol.Create(Futures.Energy.NaturalGas, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.NaturalGas;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next 12 calendar years.
                    //Trading of any delivery month shall cease three (3) business days prior to the first day of the delivery month. In the event that the official Exchange holiday schedule changes subsequent to the listing of a Natural Gas futures, the originally listed expiration date shall remain in effect.In the event that the originally listed expiration day is declared a holiday, expiration will move to the business day immediately prior.
                    var firstDay = new DateTime(time.Year,time.Month,1);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(firstDay, -3, holidays);
                })
            },
            // Brent Crude (B) : https://www.theice.com/products/219/Brent-Crude-Futures
            {Symbol.Create(Futures.Energy.BrentCrude, SecurityType.Future, Market.ICE), (time =>
                {
                    var market = Market.ICE;
                    var symbol = Futures.Energy.BrentCrude;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Up to 96 consecutive months
                    //Trading shall cease at the end of the designated settlement period on the last Business Day of the second month
                    //preceding the relevant contract month (e.g. the March contract month will expire on the last Business Day of January).
                    //If the day on which trading is due to cease would be either: (i) the Business Day preceding Christmas Day, or
                    //(ii) the Business Day preceding New Year’s Day, then trading shall cease on the next preceding Business Day
                    var secondPrecedingMonth = time.AddMonths(-2);
                    var nthLastBusinessDay = secondPrecedingMonth.Month == 12 ? 2 : 1;
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(secondPrecedingMonth, nthLastBusinessDay, holidays);
                })
            },
            // Low Sulphur Gasoil Futures (G): https://www.theice.com/products/34361119/Low-Sulphur-Gasoil-Futures
            {Symbol.Create(Futures.Energy.LowSulfurGasoil, SecurityType.Future, Market.ICE), (time =>
                {
                    var market = Market.ICE;
                    var symbol = Futures.Energy.LowSulfurGasoil;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Up to 96 consecutive months
                    //Trading shall cease at 12:00 hours London Time, 2 business days prior to the 14th calendar day of the delivery month.
                    var fourteenthDay = new DateTime(time.Year,time.Month,14);
                    var twelfthDay = FuturesExpiryUtilityFunctions.AddBusinessDays(fourteenthDay, -2, holidays);
                    return twelfthDay.Add(new TimeSpan(12,0,0));
                })
            },
            // Meats group
            // LiveCattle (LE): http://www.cmegroup.com/trading/agricultural/livestock/live-cattle_contract_specifications.html
            {Symbol.Create(Futures.Meats.LiveCattle, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Meats.LiveCattle;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts of (Feb, Apr, Jun, Aug, Oct, Dec) listed for 9 months
                    while (!FutureExpirationCycles.GJMQVZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    //Last business day of the contract month, 12:00 p.m.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    return lastBusinessDay.Add(new TimeSpan(12,0,0));
                })
            },
            // LeanHogs (HE): http://www.cmegroup.com/trading/agricultural/livestock/lean-hogs_contract_specifications.html
            {Symbol.Create(Futures.Meats.LeanHogs, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Meats.LeanHogs;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    /*
                     2 monthly contracts of:
                    Feb listed in August
                    Apr listed in October
                    May listed in December
                    Jun listed in December
                    Jul listed in February
                    Aug listed in April
                    Oct listed in May
                    Dec listed in June
                     */
                    while (!FutureExpirationCycles.GJKMNQVZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 10th business day of the contract month, 12:00 p.m.
                    var lastday = new DateTime(time.Year,time.Month,1);
                    lastday = lastday.AddDays(-1);
                    var tenthday = FuturesExpiryUtilityFunctions.AddBusinessDays(lastday, 10, holidays);
                    return tenthday.Add(new TimeSpan(12,0,0));
                })
            },
            // FeederCattle (GF): http://www.cmegroup.com/trading/agricultural/livestock/feeder-cattle_contract_specifications.html
            {Symbol.Create(Futures.Meats.FeederCattle, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Meats.FeederCattle;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts of (Jan, Mar, Apr, May, Aug, Sep, Oct, Nov) listed for 8 months
                    while (!FutureExpirationCycles.FHJKQUVX.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    /* Trading shall terminate on the last Thursday of the contract month, except:
                     * 1. The November contract shall terminate on the Thursday
                     * prior to Thanksgiving Day, unless a holiday falls on
                     * that Thursday or on any of the four weekdays prior to
                     * that Thursday, in which case trading shall terminate on
                     * the first prior Thursday that is not a holiday and is
                     * not so preceded by a holiday. Weekdays shall be defined
                     * as Monday, Tuesday, Wednesday, Thursday and Friday.
                     * 2. Any contract month in which a holiday falls on the last
                     * Thursday of the month or on any of the four weekdays
                     * prior to that Thursday shall terminate on the first
                     * prior Thursday that is not a holiday and is not so
                     * preceded by a holiday.*/
                    var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
                    // Checking condition 1
                    if(time.Month == 11)
                    {
                        var priorThursday = (from day in Enumerable.Range(1, daysInMonth)
                                  where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Thursday
                                  select new DateTime(time.Year, time.Month, day)).Reverse().ElementAt(1);
                        while (!FuturesExpiryUtilityFunctions.NotHoliday(priorThursday, holidays) || !FuturesExpiryUtilityFunctions.NotPrecededByHoliday(priorThursday, holidays))
                        {
                            priorThursday = priorThursday.AddDays(-7);
                        }
                        return priorThursday;
                    }
                    // Checking Condition 2
                    var lastThursday = (from day in Enumerable.Range(1, daysInMonth)
                                  where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Thursday
                                  select new DateTime(time.Year, time.Month, day)).Reverse().ElementAt(0);
                    while (!FuturesExpiryUtilityFunctions.NotHoliday(lastThursday, holidays) || !FuturesExpiryUtilityFunctions.NotPrecededByHoliday(lastThursday, holidays))
                    {
                        lastThursday = lastThursday.AddDays(-7);
                    }
                    return lastThursday;
                })
            },
            // Softs group
            // Cotton #2 (CT): https://www.theice.com/products/254/Cotton-No-2-Futures
            {Symbol.Create(Futures.Softs.Cotton2, SecurityType.Future, Market.ICE), (time =>
                {
                    var market = Market.ICE;
                    var symbol = Futures.Softs.Cotton2;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // March, May, July, October, December
                    while (!FutureExpirationCycles.HKNVZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Last Trading Day:
                    // Seventeen business days from end of spot month.

                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 17, holidays);
                })
            },
            // Orange Juice (OJ): https://www.theice.com/products/30/FCOJ-A-Futures
            {Symbol.Create(Futures.Softs.OrangeJuice, SecurityType.Future, Market.ICE), (time =>
                {
                    var market = Market.ICE;
                    var symbol = Futures.Softs.OrangeJuice;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);

                    // January, March, May, July, September, November.
                    while (!FutureExpirationCycles.FHKNUX.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 15, holidays);
                })
            },
            // Coffee (KC): https://www.theice.com/products/15/Coffee-C-Futures
            {Symbol.Create(Futures.Softs.Coffee, SecurityType.Future, Market.ICE), (time =>
                {
                    var market = Market.ICE;
                    var symbol = Futures.Softs.Coffee;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // March, May, July, September, December.
                    while (!FutureExpirationCycles.HKNUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Last Trading Day:
                    // One business day prior to last notice day
                    //
                    // Last Notice Day:
                    // Seven business days prior to the last business day off the delivery month

                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 9, holidays);
                })
            },
            // Sugar #11 ICE (SB): https://www.theice.com/products/23/Sugar-No-11-Futures
            {Symbol.Create(Futures.Softs.Sugar11, SecurityType.Future, Market.ICE), (time =>
                {
                    var market = Market.ICE;
                    var symbol = Futures.Softs.Sugar11;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // March, May, July and October
                    while (!FutureExpirationCycles.HKNV.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Last Trading Day:
                    // Last business day of the month preceding the delivery month

                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time.AddMonths(-1), 1, holidays);
                })
            },
            // Sugar #11 CME (YO): https://www.cmegroup.com/trading/agricultural/softs/sugar-no11_contract_specifications.html
            {Symbol.Create(Futures.Softs.Sugar11CME, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Softs.Sugar11CME;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Trading is conducted in the March, May, July, and October cycle for the next 24 months.
                    while (!FutureExpirationCycles.HKNV.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading terminates on the day immediately preceding the first notice day of the corresponding trading month of Sugar No. 11 futures at ICE Futures U.S.
                    var precedingMonth = time.AddMonths(-1);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(precedingMonth, 1, holidays);
                })
            },
            // Cocoa (CC): https://www.theice.com/products/7/Cocoa-Futures
            {Symbol.Create(Futures.Softs.Cocoa, SecurityType.Future, Market.ICE), (time =>
                {
                    var market = Market.ICE;
                    var symbol = Futures.Softs.Cocoa;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // March, May, July, September, December
                    while (!FutureExpirationCycles.HKNUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Last Trading Day:
                    // One business day prior to last notice day
                    //
                    // Last Notice Day:
                    // Ten business days prior to last business day of delivery month

                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 12, holidays);
                })
            },
            // Dairy Group
            // Cash-settled Butter (CB): https://www.cmegroup.com/trading/agricultural/dairy/cash-settled-butter_contract_specifications.html
            {Symbol.Create(Futures.Dairy.CashSettledButter, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Dairy.CashSettledButter;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 24 consecutive months
                    // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the Butter price for that contract month. (LTD 12:10 p.m.)
                    return FuturesExpiryUtilityFunctions.DairyLastTradeDate(time, holidays);
                })
            },
            // Cash-Settled Cheese (CSC): https://www.cmegroup.com/trading/agricultural/dairy/cheese_contract_specifications.html
            {Symbol.Create(Futures.Dairy.CashSettledCheese, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Dairy.CashSettledCheese;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 24 consecutive months
                    // Trading shall terminate on the business day immediately preceding the release date for the USDA monthly weighted average price in the U.S. for cheese. LTD close is at 12:10 p.m. Central Time
                    return FuturesExpiryUtilityFunctions.DairyLastTradeDate(time, holidays);
                })
            },
            // Class III Milk (DC): https://www.cmegroup.com/trading/agricultural/dairy/class-iii-milk_contract_specifications.html
            {Symbol.Create(Futures.Dairy.ClassIIIMilk, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Dairy.ClassIIIMilk;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 24 consecutive months
                    // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the Class III price for that contract month (LTD 12:10 p.m.)
                    return FuturesExpiryUtilityFunctions.DairyLastTradeDate(time, holidays);
                })
            },
            // Dry Whey (DY): https://www.cmegroup.com/trading/agricultural/dairy/dry-whey_contract_specifications.html
            {Symbol.Create(Futures.Dairy.DryWhey, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Dairy.DryWhey;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 24 consecutive months
                    // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the Dry Whey price for that contract month. (LTD 12:10 p.m.)
                    return FuturesExpiryUtilityFunctions.DairyLastTradeDate(time, holidays);
                })
            },
            // Class IV Milk (GDK): https://www.cmegroup.com/trading/agricultural/dairy/class-iv-milk_contract_specifications.html
            {Symbol.Create(Futures.Dairy.ClassIVMilk, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Dairy.ClassIVMilk;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 24 consecutive months
                    // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the Class IV price for that contract month. (LTD 12:10 p.m.)
                    return FuturesExpiryUtilityFunctions.DairyLastTradeDate(time, holidays);
                })
            },
            // Non-fat Dry Milk (GNF): https://www.cmegroup.com/trading/agricultural/dairy/nonfat-dry-milk_contract_specifications.html
            {Symbol.Create(Futures.Dairy.NonfatDryMilk, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Dairy.NonfatDryMilk;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 24 consecutive months
                    // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the Nonfat Dry Milk price for that contract month. (LTD 12:10 p.m.)
                    return FuturesExpiryUtilityFunctions.DairyLastTradeDate(time, holidays);
                })
            },
            // Micro Gold Futures (MGC): https://www.cmegroup.com/markets/metals/precious/e-micro-gold.contractSpecs.html
            {Symbol.Create(Futures.Metals.MicroGold, SecurityType.Future, Market.COMEX), (time =>
                {
                    var market = Market.COMEX;
                    var symbol = Futures.Metals.MicroGold;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Four bi-monthly contracts (Feb/2, Apr/4, Jun/6, Aug/8, Oct/10, Dec/12 cycle)
                    while (!FutureExpirationCycles.GJMQVZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Monthly contracts
                    // Trading terminates on the third last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 3, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro Silver Futures (SIL): https://www.cmegroup.com/markets/metals/precious/1000-oz-silver.contractSpecs.html
            {Symbol.Create(Futures.Metals.MicroSilver, SecurityType.Future, Market.COMEX), (time =>
                {
                    var market = Market.COMEX;
                    var symbol = Futures.Metals.MicroSilver;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the third last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 3, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro Gold TAS Futures (MGT): https://www.cmegroup.com/markets/metals/precious/e-micro-gold.contractSpecs.html
            {Symbol.Create(Futures.Metals.MicroGoldTAS, SecurityType.Future, Market.COMEX), (time =>
                {
                    var market = Market.COMEX;
                    var symbol = Futures.Metals.MicroGoldTAS;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the third last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 3, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro Palladium Futures (PAM): https://www.cmegroup.com/markets/metals/precious/e-micro-palladium.contractSpecs.html
            {Symbol.Create(Futures.Metals.MicroPalladium, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Metals.MicroPalladium;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the third last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 3, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Mini Sized NY Gold Futures: https://www.theice.com/products/31500921/Mini-Gold-Future & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Metals.MiniNYGold, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Metals.MiniNYGold;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Trading terminates on the third last business day of the contract month @13:30

                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 3, holidays);

                    return lastBusinessDay.Add(new TimeSpan(13, 30, 0));
                })
            },
            // Mini Sized NY Silver Futures: https://www.theice.com/products/31500921/Mini-Silver-Future & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Metals.MiniNYSilver, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Metals.MiniNYSilver;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Trading terminates on the third last business day of the contract month @13:25

                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 3, holidays);

                    return lastBusinessDay.Add(new TimeSpan(13, 25, 0));
                })
            },
            // Gold 100 Oz Futures: https://www.theice.com/products/31499002/100-oz-Gold-Future & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Metals.Gold100Oz, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Metals.Gold100Oz;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Trading terminates on the third last business day of the contract month @13:30

                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 3, holidays);

                    return lastBusinessDay.Add(new TimeSpan(13, 30, 0));
                })
            },
            // Silver 5000 Oz Futures: https://www.theice.com/products/31500922/5000-oz-Silver-Future & https://www.theice.com/publicdocs/futures_us/exchange_notices/ICE_Futures_US_2022_TRADING_HOLIDAY_CALENDAR_20211118.pdf
            {Symbol.Create(Futures.Metals.Silver5000Oz, SecurityType.Future, Market.NYSELIFFE), (time =>
                {
                    var market = Market.NYSELIFFE;
                    var symbol = Futures.Metals.Silver5000Oz;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Trading terminates on the third last business day of the contract month @13:25

                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 3, holidays);

                    return lastBusinessDay.Add(new TimeSpan(13, 25, 0));
                })
            },
            // Micro 10-Year Yield Futures (10Y): https://www.cmegroup.com/markets/interest-rates/us-treasury/micro-10-year-yield.contractSpecs.html
            {Symbol.Create(Futures.Financials.MicroY10TreasuryNote, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.MicroY10TreasuryNote;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro 30-Year Yield Futures (30Y): https://www.cmegroup.com/markets/interest-rates/us-treasury/micro-30-year-yield_contract_specifications.html
            {Symbol.Create(Futures.Financials.MicroY30TreasuryBond, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.MicroY30TreasuryBond;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro 2-Year Yield Futures (2YY): https://www.cmegroup.com/markets/interest-rates/us-treasury/micro-2-year-yield.contractSpecs.html
            {Symbol.Create(Futures.Financials.MicroY2TreasuryBond, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.MicroY2TreasuryBond;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro 5-Year Yield Futures (5YY): https://www.cmegroup.com/markets/interest-rates/us-treasury/micro-5-year-yield.contractSpecs.html
            {Symbol.Create(Futures.Financials.MicroY5TreasuryBond, SecurityType.Future, Market.CBOT), (time =>
                {
                    var market = Market.CBOT;
                    var symbol = Futures.Financials.MicroY5TreasuryBond;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts
                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro EUR/USD Futures (M6E): https://www.cmegroup.com/markets/fx/g10/e-micro-euro.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroEUR, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroEUR;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 2 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading terminates at 9:16 a.m. CT 2 business day prior to the 3rd Wednesday of the contract quqrter.
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Micro AUD/USD Futures (M6A): https://www.cmegroup.com/markets/fx/g10/e-micro-australian-dollar.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroAUD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroAUD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 2 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // On the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday;
                })
            },
            // Micro GBP/USD Futures (M6B): https://www.cmegroup.com/markets/fx/g10/e-micro-british-pound.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroGBP, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroGBP;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 2 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Micro CAD/USD Futures (MCD): https://www.cmegroup.com/markets/fx/g10/e-micro-canadian-dollar-us-dollar.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroCADUSD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroCADUSD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 2 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading terminates 1 business day prior to the 3rd Wednesday of the contract quarter.
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var firstBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -1, holidays);
                    firstBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(firstBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return firstBusinessDayPrecedingThirdWednesday;
                })
            },
            // Micro JPY/USD Futures (MJY): https://www.cmegroup.com/markets/fx/g10/e-micro-japanese-yen-us-dollar.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroJPY, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroJPY;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 2 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Micro CHF/USD Futures (MSF): https://www.cmegroup.com/markets/fx/g10/e-micro-swiss-franc-us-dollar.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroCHF, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroCHF;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 2 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Micro USD/JPY Futures (M6J): https://www.cmegroup.com/markets/fx/g10/micro-usd-jpy.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroUSDJPY, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroUSDJPY;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 2 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Micro INR/USD Futures (MIR): https://www.cmegroup.com/markets/fx/g10/e-micro-indian-rupee.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroINRUSD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroINRUSD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 12 consecutive months.

                    // Trading terminates at 12:00 noon Mumbai time two Indian business days immediately preceding the last Indian
                    // business day of the contract month.

                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    var secondBusinessDayPrecedingLastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay,-2, holidays);
                    return secondBusinessDayPrecedingLastBusinessDay.Add(new TimeSpan(6,30,0));
                })
            },
            // Micro USD/CAD Futures (M6C): https://www.cmegroup.com/markets/fx/g10/micro-usd-cad.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroCAD, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroCAD;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Two months in the March quarterly cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                   // Trading terminates at 9:16 a.m. CT, 1 business day prior to the third Wednesday of the contract month.
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var firstBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -1, holidays);
                    firstBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(firstBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return firstBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Micro USD/CHF Futures (M6S): https://www.cmegroup.com/markets/fx/g10/micro-usd-chf.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroUSDCHF, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroUSDCHF;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Two months in the March quarterly cycle (Mar, Jun, Sep, Dec)
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                   // Trading terminates at 9:16 a.m. CT, 2 business days prior to the third Wednesday of the contract month.
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2, holidays);
                    secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(secondBusinessDayPrecedingThirdWednesday, -1, holidays);

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Micro USD/CNH Futures (MNH): https://www.cmegroup.com/markets/fx/g10/e-micro-cnh.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroUSDCNH, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroUSDCNH;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 12 consecutive months.

                    // Trading terminates at 11:00 a.m. Hong Kong time on the second Hong Kong business day prior
                    // to the third Wednesday of the contract month.

                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-2, holidays);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(3,0,0));
                })
            },
            // Micro E-mini S&P 500 Index Futures (MES): https://www.cmegroup.com/markets/equities/sp/micro-e-mini-sandp-500.contractSpecs.html
            {Symbol.Create(Futures.Indices.MicroSP500EMini, SecurityType.Future, Market.CME), (time =>
                {
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 5 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading terminates at 9:30 a.m. ET on the 3rd Friday of the contract month.
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // Micro E-mini Nasdaq-100 Index Futures (MNQ): https://www.cmegroup.com/markets/equities/nasdaq/micro-e-mini-nasdaq-100.contractSpecs.html
            {Symbol.Create(Futures.Indices.MicroNASDAQ100EMini, SecurityType.Future, Market.CME), (time =>
                {
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 5 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading terminates at 9:30 a.m. ET on the 3rd Friday of the contract month.
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // Micro E-mini Russell 2000 Index Futures (M2K): https://www.cmegroup.com/markets/equities/russell/micro-e-mini-russell-2000.contractSpecs.html
            {Symbol.Create(Futures.Indices.MicroRussell2000EMini, SecurityType.Future, Market.CME), (time =>
                {
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 5 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading terminates at 9:30 a.m. ET on the 3rd Friday of the contract month.
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // Micro E-mini Dow Jones Industrial Average Index Futures (MYM): https://www.cmegroup.com/markets/equities/dow-jones/micro-e-mini-dow.contractSpecs.html
            {Symbol.Create(Futures.Indices.MicroDow30EMini, SecurityType.Future, Market.CBOT), (time =>
                {
                    // Quarterly contracts (Mar, Jun, Sep, Dec) listed for 4 consecutive quarters
                    while (!FutureExpirationCycles.HMUZ.Contains(time.Month))
                    {
                        time = time.AddMonths(1);
                    }

                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // Micro WTI Crude Oil Futures (MCL): https://www.cmegroup.com/markets/energy/crude-oil/micro-wti-crude-oil.contractSpecs.html
            {Symbol.Create(Futures.Energy.MicroCrudeOilWTI, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MicroCrudeOilWTI;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 12 consecutive months and additional Jun and Dec contract months

                    // Trading terminates 4 business days prior to the 25th calendar day of the month prior to the
                    // contract month (1 business day prior to CL LTD)
                    // If the 25th calendar day is not a business day, trading terminates 5 business days before the 25th calendar day of the month prior to the contract month.

                    var previousMonth = time.AddMonths(-1);
                    var twentyFifthDay = new DateTime(previousMonth.Year, previousMonth.Month, 25);

                    var businessDays = -4;
                    if(!FuturesExpiryUtilityFunctions.NotHoliday(twentyFifthDay, holidays))
                    {
                        // if the 25th is a holiday we substract 1 extra bussiness day
                        businessDays -= 1;
                    }
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(twentyFifthDay, businessDays, holidays);
                })
            },
            // Micro Singapore FOB Marine Fuel 0.5% (Platts) Futures (S50): https://www.cmegroup.com/markets/energy/refined-products/micro-singapore-fob-marine-fuel-05-platts.contractSpecs.html
            {Symbol.Create(Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and next 3 calendar years
                    // Add monthly contracts for a new calendar year following the termination of trading in the
                    // December contract of the current year.

                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro Gasoil 0.1% Barges FOB ARA (Platts) Futures (M1B): https://www.cmegroup.com/markets/energy/refined-products/micro-gasoil-01-barges-fob-rdam-platts.contractSpecs.html
            {Symbol.Create(Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 36 consecutive months

                    // Trading terminates on the last London business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro European FOB Rdam Marine Fuel 0.5% Barges (Platts) Futures (R50): https://www.cmegroup.com/markets/energy/refined-products/micro-european-fob-rdam-marine-fuel-05-barges-platts.contractSpecs.html
            {Symbol.Create(Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and next 3 calendar years.
                    // Add monthly contracts for a new calendar year following the termination of trading
                    // in the December contract of the current year.

                    // Trading terminates on the last London business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro European 3.5% Fuel Oil Barges FOB Rdam (Platts) Futures (MEF): https://www.cmegroup.com/markets/energy/refined-products/micro-european-35-fuel-oil-barges-fob-rdam-platts.contractSpecs.html
            {Symbol.Create(Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and 5 calendar years.Monthly contracts for a new calendar
                    // year will be added following the termination of trading in  the December contract of the current year.

                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro Singapore Fuel Oil 380CST (Platts) Futures (MAF): https://www.cmegroup.com/markets/energy/refined-products/micro-singapore-fuel-oil-380cst-platts.contractSpecs.html
            {Symbol.Create(Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and 5 calendar years.Monthly contracts for a new calendar
                    // year will be added following the termination of trading in  the December contract of the current year.

                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro Coal (API 5) fob Newcastle (Argus/McCloskey) Futures (M5F): https://www.cmegroup.com/markets/energy/coal/micro-coal-api-5-fob-newcastle-argus-mccloskey.contractSpecs.html
            {Symbol.Create(Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for the current year and the next calendar year. Monthly contracts
                    // for a new calendar year will be added following the termination of trading in the December
                    // contract of the current year.

                    // Trading terminates on the last Friday of the contract month. If such Friday is a UK holiday,
                    // trading terminates on the UK business day immediately prior to the last Friday of the contract
                    // month unless such day is not an Exchange business day, in which case trading terminates on the
                    // Exchange business day immediately prior.

                    var lastFriday = FuturesExpiryUtilityFunctions.LastFriday(time);

                    while (holidays.Contains(lastFriday))
                    {
                        lastFriday = FuturesExpiryUtilityFunctions.AddBusinessDays(lastFriday, -1, holidays);
                        while (holidays.Contains(lastFriday))
                        {
                            lastFriday = FuturesExpiryUtilityFunctions.AddBusinessDays(lastFriday, -1, holidays);
                        }
                    }

                    return lastFriday;
                })
            },
            // Micro European 3.5% Fuel Oil Cargoes FOB Med (Platts) Futures (M35): https://www.cmegroup.com/markets/energy/refined-products/micro-european-35-fuel-oil-cargoes-fob-med-platts.contractSpecs.html
            {Symbol.Create(Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, SecurityType.Future, Market.NYMEX), (time =>
                {
                    var market = Market.NYMEX;
                    var symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 36 consecutive months

                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1, holidays);
                    lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastBusinessDay, -1, holidays);

                    return lastBusinessDay;
                })
            },
            // Micro Ether Futures (MET): https://www.cmegroup.com/markets/cryptocurrencies/ether/micro-ether.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroEther, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroEther;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 6 consecutive months and 2 additional Dec contract months.

                    // Trading terminates at 4:00 p.m. London time on the last Friday of the contract month that
                    // is either a London or U.S. business day. If the last Friday of the contract month day is
                    // not a business day in both London and the U.S., trading terminates on the prior London or
                    // U.S. business day.

                    // BTIC: Trading terminates at 4:00 p.m. London time on the last Thursday of the contract month
                    // that is either a London or U.S. business day. If the last Thursday of the contract month day
                    // is not a business day in both London and the U.S., trading terminates on the prior London or U.S.
                    // business day.

                    var lastFriday = FuturesExpiryUtilityFunctions.LastFriday(time);
                    lastFriday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastFriday, -1, holidays);

                    return lastFriday.Add(new TimeSpan(15, 0, 0));
                })
            },
            // Micro Bitcoin Futures (MBT): https://www.cmegroup.com/markets/cryptocurrencies/bitcoin/micro-bitcoin.contractSpecs.html
            {Symbol.Create(Futures.Currencies.MicroBTC, SecurityType.Future, Market.CME), (time =>
                {
                    var market = Market.CME;
                    var symbol = Futures.Currencies.MicroBTC;
                    var holidays = FuturesExpiryUtilityFunctions.GetHolidays(market, symbol);
                    // Monthly contracts listed for 6 consecutive months and 2 additional Dec contract months.
                    // If the 6 consecutive months includes Dec, list only 1 additional Dec contract month.

                    // Trading terminates at 4:00 p.m. London time on the last Friday of the contract month.
                    // If this is not both a London and U.S. business day, trading terminates on the prior
                    // London and the U.S. business day.

                    // BTIC: Trading terminates at 4:00 p.m. London time on the last Thursday of the contract
                    // month.If this is not both a London and U.S. business day, trading terminates on the prior
                    // London and the U.S. business day.

                    var lastFriday = FuturesExpiryUtilityFunctions.LastFriday(time);
                    lastFriday = FuturesExpiryUtilityFunctions.AddBusinessDaysIfHoliday(lastFriday, -1, holidays);

                    return lastFriday.Add(new TimeSpan(15, 0, 0));
                })
            }
        };
    }
}
