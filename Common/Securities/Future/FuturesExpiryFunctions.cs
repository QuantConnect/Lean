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
        public static Func<DateTime, DateTime> FuturesExpiryFunction(string symbol)
        {
            if (FuturesExpiryDictionary.ContainsKey(symbol.ToUpper()))
            {
                return FuturesExpiryDictionary[symbol.ToUpper()];
            }

            // If function for expiry cannot be found pass the date through
            return (date) => date;
        }

        /// <summary>
        /// The USDA publishes a report containing contract prices for the contract month - https://usda.library.cornell.edu/concern/publications/zs25x847n
        /// These dates are erratic and requires maintenance of a separate list instead of using holiday entries in MHDB.
        /// </summary>
        /// <remarks>We only report the publication date of the report. In order to get accurate last trade dates, subtract one (plus holidays) from the value's date</remarks>
        public static Dictionary<DateTime, DateTime> DairyReportDates = new Dictionary<DateTime, DateTime>()
        {
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
        public static Dictionary<string, Func<DateTime, DateTime>> FuturesExpiryDictionary = new Dictionary<string, Func<DateTime, DateTime>>()
        {
            // Metals
            // Gold (GC): http://www.cmegroup.com/trading/metals/precious/gold_contract_specifications.html
            {Futures.Metals.Gold, (time =>
                    // Trading terminates on the third last business day of the delivery month.
                    FuturesExpiryUtilityFunctions.NthLastBusinessDay(time,3)
                )
            },
            // Silver (SI): http://www.cmegroup.com/trading/metals/precious/silver_contract_specifications.html
            {Futures.Metals.Silver, (time =>
                    // Trading terminates on the third last business day of the delivery month.
                    FuturesExpiryUtilityFunctions.NthLastBusinessDay(time,3)
                )
            },
            // Platinum (PL): http://www.cmegroup.com/trading/metals/precious/platinum_contract_specifications.html
            {Futures.Metals.Platinum, (time =>
                    // Trading terminates on the third last business day of the delivery month.
                    FuturesExpiryUtilityFunctions.NthLastBusinessDay(time,3)
                )
            },
            // Palladium (PA): http://www.cmegroup.com/trading/metals/precious/palladium_contract_specifications.html
            {Futures.Metals.Palladium, (time =>
                    // Trading terminates on the third last business day of the delivery month.
                    FuturesExpiryUtilityFunctions.NthLastBusinessDay(time,3)
                )
            },
            // Aluminum MW U.S. Transaction Premium Platts (25MT) (AUP): https://www.cmegroup.com/trading/metals/base/aluminum-mw-us-transaction-premium-platts-swap-futures_contract_specifications.html
            {Futures.Metals.AluminumMWUSTransactionPremiumPlatts25MT, (time =>
                {
                    // Trading terminates on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Metals.AluminumMWUSTransactionPremiumPlatts25MT, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Aluminium European Premium Duty-Paid (Metal Bulletin) (EDP): https://www.cmegroup.com/trading/metals/base/aluminium-european-premium-duty-paid-metal-bulletin_contract_specifications.html
            {Futures.Metals.AluminiumEuropeanPremiumDutyPaidMetalBulletin, (time =>
                {
                    // Trading terminates on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Metals.AluminiumEuropeanPremiumDutyPaidMetalBulletin, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Indices
            // SP500EMini (ES): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-sandp500_contract_specifications.html
            {Futures.Indices.SP500EMini, (time =>
                {
                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // NASDAQ100EMini (NQ): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-nasdaq-100_contract_specifications.html
            {Futures.Indices.NASDAQ100EMini, (time =>
                {
                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // Dow30EMini (YM): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-dow_contract_specifications.html
            {Futures.Indices.Dow30EMini, (time =>
                {
                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan(13,30,0));
                })
            },
            // Russell2000EMini (RTY): https://www.cmegroup.com/trading/equity-index/us-index/e-mini-russell-2000_contract_specifications.html
            {Futures.Indices.Russell2000EMini, (time =>
                {
                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    return thirdFriday.Add(new TimeSpan (13,30,0));
                })
            },
            // Nikkei225Dollar (NKD): https://www.cmegroup.com/trading/equity-index/international-index/nikkei-225-dollar_contract_specifications.html
            {Futures.Indices.Nikkei225Dollar, (time =>
                {
                    // Trading terminates at 5:00 p.m. Eastern Time (ET) on Business Day prior to 2nd Friday of the contract month.
                    var secondFriday = FuturesExpiryUtilityFunctions.SecondFriday(time);
                    var priorBusinessDay = secondFriday.AddDays(-1);
                    while (!FuturesExpiryUtilityFunctions.NotHoliday(priorBusinessDay))
                    {
                        priorBusinessDay = priorBusinessDay.AddDays(-1);
                    }
                    return priorBusinessDay.Add(TimeSpan.FromHours(21));
                })
            },
            // CBOE Volatility Index Futures (VIX): https://cfe.cboe.com/cfe-products/vx-cboe-volatility-index-vix-futures/contract-specifications
            {Futures.Indices.VIX, (time =>
                {
                    // Trading can occur up to 9:00 a.m. Eastern Time (ET) on the "Wednesday that is 30 days prior to
                    // the third Friday of the calendar month immediately following the month in which the contract expires".
                    var nextThirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time.AddMonths(1));
                    var expiryDate = nextThirdFriday.AddDays(-30);
                    // If the next third Friday or the Wednesday are holidays, then it is moved to the previous day.
                    if (USHoliday.Dates.Contains(expiryDate) || USHoliday.Dates.Contains(nextThirdFriday))
                    {
                        expiryDate = expiryDate.AddDays(-1);
                    }
                    // Trading hours for expiring VX futures contracts end at 8:00 a.m. Chicago time on the final settlement date.
                    return expiryDate.Add(new TimeSpan(13, 0, 0));
                })
            },
            // Bloomberg Commodity Index (AW): https://www.cmegroup.com/trading/agricultural/commodity-index/bloomberg-commodity-index_contract_specifications.html
            {Futures.Indices.BloombergCommodityIndex, (time =>
                {
                    // 3rd Wednesday of the contract month/ 1:30pm
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Indices.BloombergCommodityIndex, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(thirdWednesday))
                    {
                        thirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -1);
                    }

                    return thirdWednesday.Add(new TimeSpan(18, 30, 0));
                })
            },
            // E-mini Nasdaq-100 Biotechnology Index (BIO): https://www.cmegroup.com/trading/equity-index/us-index/e-mini-nasdaq-biotechnology_contract_specifications.html
            {Futures.Indices.NASDAQ100BiotechnologyEMini, (time =>
                {
                    // Trading can occur up to 9:30 a.m. ET on the 3rd Friday of the contract month
                    var thirdFriday = FuturesExpiryUtilityFunctions.ThirdFriday(time);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Indices.NASDAQ100BiotechnologyEMini, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(thirdFriday))
                    {
                        thirdFriday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdFriday, -1);
                    }

                    return thirdFriday.Add(new TimeSpan(13, 30, 0));
                })
            },
            // Grains And OilSeeds Group
            // Wheat (ZW): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/wheat_contract_specifications.html
            {Futures.Grains.Wheat, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1);
                })
            },
            // Corn (ZC): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/corn_contract_specifications.html
            {Futures.Grains.Corn, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1);
                })
            },
            // Soybeans (ZS): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/soybean_contract_specifications.html
            {Futures.Grains.Soybeans, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1);
                })
            },
            // SoybeanMeal (ZM): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/soybean-meal_contract_specifications.html
            {Futures.Grains.SoybeanMeal, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1);
                })
            },
            // SoybeanOil (ZL): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/soybean-oil_contract_specifications.html
            {Futures.Grains.SoybeanOil, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1);
                })
            },
            // Oats (ZO): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/oats_contract_specifications.html
            {Futures.Grains.Oats, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    var fifteenth = new DateTime(time.Year,time.Month,15);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth,-1);
                })
            },
            // Black Sea Corn Financially Settled (Platts) (BCF): https://www.cmegroup.com/trading/agricultural/grain-and-oilseed/black-sea-corn-financially-settled-platts_contract_specifications.html
            {Futures.Grains.BlackSeaCornFinanciallySettledPlatts, (time =>
                {
                    // Trading terminates on the last business day of the contract month which is also a Platts publication date for the price assessment.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Black Sea Wheat Financially Settled (Platts) (BWF): https://www.cmegroup.com/trading/agricultural/grain-and-oilseed/black-sea-wheat-financially-settled-platts_contract_specifications.html
            {Futures.Grains.BlackSeaWheatFinanciallySettledPlatts, (time =>
                {
                    // Trading terminates on the last business day of the contract month which is also a Platts publication date for the price assessment.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.AUDNZD, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Currencies group
            // U.S. Dollar Index Futures is not found on cmegroup will discuss and update
            //  GBP (6B): http://www.cmegroup.com/trading/fx/g10/british-pound_contract_specifications.html
            {Futures.Currencies.GBP, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-2);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // CAD (6C): http://www.cmegroup.com/trading/fx/g10/canadian-dollar_contract_specifications.html
            {Futures.Currencies.CAD, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the business day immediately preceding the third Wednesday of the contract month (usually Tuesday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var businessDayPrecedingThridWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-1);
                    return businessDayPrecedingThridWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // JPY (6J): http://www.cmegroup.com/trading/fx/g10/japanese-yen_contract_specifications.html
            {Futures.Currencies.JPY, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-2);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // CHF (6S): http://www.cmegroup.com/trading/fx/g10/swiss-franc_contract_specifications.html
            {Futures.Currencies.CHF, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-2);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // EUR (6E): http://www.cmegroup.com/trading/fx/g10/euro-fx_contract_specifications.html
            {Futures.Currencies.EUR, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-2);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // AUD (6A): http://www.cmegroup.com/trading/fx/g10/australian-dollar_contract_specifications.html
            {Futures.Currencies.AUD, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-2);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // NZD (6N): http://www.cmegroup.com/trading/fx/g10/new-zealand-dollar_contract_specifications.html
            {Futures.Currencies.NZD, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-2);
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // RUB (6R): https://www.cmegroup.com/trading/fx/emerging-market/russian-ruble_contract_specifications.html
            {Futures.Currencies.RUB, (time =>
                {
                    // 11:00 a.m. Mosccow time on the fifteenth day of the month, or, if not a business day, on the next business day for the Moscow interbank foreign exchange market.
                    var fifteenth = new DateTime(time.Year, time.Month, 15);

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(fifteenth))
                    {
                        fifteenth = FuturesExpiryUtilityFunctions.AddBusinessDays(fifteenth, 1);
                    }
                    return fifteenth.Add(new TimeSpan(08,0,0));
                })
            },
            // BRL (6L): https://www.cmegroup.com/trading/fx/emerging-market/brazilian-real_contract_specifications.html
            {Futures.Currencies.BRL, (time =>
                {
                    // On the last business day of the month, at 9:15 a.m. CT, immediately preceding the contract month, on which the Central Bank of Brazil is scheduled to publish its final end-of-month (EOM), "Commercial exchange rate for Brazilian reais per U.S. dollar for cash delivery" (PTAX rate).
                    var lastPrecedingBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(time, -1);
                    var symbolHolidays = MarketHoursDatabase.FromDataFolder().GetEntry("usa", Futures.Currencies.BRL, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (symbolHolidays.Contains(lastPrecedingBusinessDay))
                    {
                        lastPrecedingBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastPrecedingBusinessDay, -1);
                    }

                    return lastPrecedingBusinessDay.Add(new TimeSpan(14,15,0));
                })
            },
            // MXN (6M): https://www.cmegroup.com/trading/fx/emerging-market/mexican-peso_contract_specifications.html
            {Futures.Currencies.MXN, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday). 
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday,-2);

                    var symbolHolidays = MarketHoursDatabase.FromDataFolder().GetEntry("usa", Futures.Currencies.MXN, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    // Columbus Day as per SIFMA holiday schedule
                    if (symbolHolidays.Contains(secondBusinessDayPrecedingThirdWednesday))
                    {
                        secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(secondBusinessDayPrecedingThirdWednesday, -1);
                    }
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // ZAR (6Z): https://www.cmegroup.com/trading/fx/emerging-market/south-african-rand_contract_specifications.html
            {Futures.Currencies.ZAR, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday)
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2);

                    var symbolHolidays = MarketHoursDatabase.FromDataFolder().GetEntry("usa", Futures.Currencies.ZAR, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (symbolHolidays.Contains(secondBusinessDayPrecedingThirdWednesday))
                    {
                        secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(secondBusinessDayPrecedingThirdWednesday, -1);
                    }
                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // AUD/CAD (ACD): https://www.cmegroup.com/trading/fx/g10/australian-dollar-canadian-dollar_contract_specifications.html
            {Futures.Currencies.AUDCAD, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday)
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2);

                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.AUDCAD, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(secondBusinessDayPrecedingThirdWednesday))
                    {
                        secondBusinessDayPrecedingThirdWednesday =  FuturesExpiryUtilityFunctions.AddBusinessDays(secondBusinessDayPrecedingThirdWednesday, -1);
                    }

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14,16,0));
                })
            },
            // Australian Dollar/Japanese Yen (AJY): https://www.cmegroup.com/trading/fx/g10/australian-dollar-japanese-yen_contract_specifications.html
            {Futures.Currencies.AUDJPY, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.AUDJPY, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(secondBusinessDayPrecedingThirdWednesday))
                    {
                        secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(secondBusinessDayPrecedingThirdWednesday, -1);
                    }

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Australian Dollar/New Zealand Dollar (ANE): https://www.cmegroup.com/trading/fx/g10/australian-dollar-new-zealand-dollar_contract_specifications.html
            {Futures.Currencies.AUDNZD, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.AUDNZD, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(secondBusinessDayPrecedingThirdWednesday))
                    {
                        secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(secondBusinessDayPrecedingThirdWednesday, -1);
                    }

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Bitcoin (BTC): https://www.cmegroup.com/trading/equity-index/us-index/bitcoin_contract_specifications.html
            {Futures.Currencies.BTC, (time =>
                {
                    // Trading terminates at 4:00 p.m. London time on the last Friday of the contract month. If that day is not a business day in both the U.K. and the US, trading terminates on the preceding day that is a business day for both the U.K. and the U.S..
                    var lastFriday = (from day in Enumerable.Range(1, DateTime.DaysInMonth(time.Year, time.Month))
                                      where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Friday
                                      select new DateTime(time.Year, time.Month, day)).Last();

                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.AUDNZD, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastFriday))
                    {
                        lastFriday = FuturesExpiryUtilityFunctions.AddBusinessDays(lastFriday, -1);
                    }

                    return lastFriday.Add(new TimeSpan(15, 0, 0));
                })
            },
            // Canadian Dollar/Japanese Yen (CJY): https://www.cmegroup.com/trading/fx/g10/canadian-dollar-japanese-yen_contract_specifications.html
            {Futures.Currencies.CADJPY, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.CADJPY, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(secondBusinessDayPrecedingThirdWednesday))
                    {
                        secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(secondBusinessDayPrecedingThirdWednesday, -1);
                    }

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Standard-Size USD/Offshore RMB (CNH) (CNH): https://www.cmegroup.com/trading/fx/emerging-market/usd-cnh_contract_specifications.html
            {Futures.Currencies.StandardSizeUSDOffshoreRMBCNH, (time =>
                {
                    // Trading terminates on the second Hong Kong business day prior to the third Wednesday of the contract month at 11:00 a.m. Hong Kong local time.
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = thirdWednesday.AddDays(-2);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.StandardSizeUSDOffshoreRMBCNH, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(secondBusinessDayPrecedingThirdWednesday) || !secondBusinessDayPrecedingThirdWednesday.IsCommonBusinessDay())
                    {
                        secondBusinessDayPrecedingThirdWednesday = secondBusinessDayPrecedingThirdWednesday.AddDays(-1);
                    }

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(3,0,0));
                })
            },
            // E-mini Euro FX (E7): https://www.cmegroup.com/trading/fx/g10/e-mini-euro-fx_contract_specifications.html
            {Futures.Currencies.EuroFXEmini, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday). 
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.EuroFXEmini, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(secondBusinessDayPrecedingThirdWednesday))
                    {
                        secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(secondBusinessDayPrecedingThirdWednesday, -1);
                    }

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Euro/Australian Dollar (EAD): https://www.cmegroup.com/trading/fx/g10/euro-fx-australian-dollar_contract_specifications.html
            {Futures.Currencies.EURAUD, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday). 
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.EURAUD, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(secondBusinessDayPrecedingThirdWednesday))
                    {
                        secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(secondBusinessDayPrecedingThirdWednesday, -1);
                    }

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Euro/Canadian Dollar (ECD): https://www.cmegroup.com/trading/fx/g10/euro-fx-canadian-dollar_contract_specifications.html
            {Futures.Currencies.EURCAD, (time =>
                {
                    // Trading terminates at 9:16 a.m. CT on the second business day prior to the third Wednesday of the contract month. 
                    var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(time);
                    var secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(thirdWednesday, -2);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Currencies.EURCAD, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(secondBusinessDayPrecedingThirdWednesday))
                    {
                        secondBusinessDayPrecedingThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(secondBusinessDayPrecedingThirdWednesday, -1);
                    }

                    return secondBusinessDayPrecedingThirdWednesday.Add(new TimeSpan(14, 16, 0));
                })
            },
            // Financials group
            // Y30TreasuryBond (ZB): http://www.cmegroup.com/trading/interest-rates/us-treasury/30-year-us-treasury-bond_contract_specifications.html
            {Futures.Financials.Y30TreasuryBond, (time =>
                {
                    //  Seventh business day preceding the last business day of the delivery month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var seventhBusinessDayPrecedingLastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay,-7);
                    return seventhBusinessDayPrecedingLastBusinessDay.Add(new TimeSpan(12,01,0));
                })
            },
            // Y10TreasuryNote (ZN): http://www.cmegroup.com/trading/interest-rates/us-treasury/10-year-us-treasury-note_contract_specifications.html
            {Futures.Financials.Y10TreasuryNote, (time =>
                {
                    //  Seventh business day preceding the last business day of the delivery month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var seventhBusinessDayPrecedingLastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay,-7);
                    return seventhBusinessDayPrecedingLastBusinessDay.Add(new TimeSpan(12,01,0));
                })
            },
            // Y5TreasuryNote (ZF): http://www.cmegroup.com/trading/interest-rates/us-treasury/5-year-us-treasury-note_contract_specifications.html
            {Futures.Financials.Y5TreasuryNote, (time =>
                {
                    // Last business day of the calendar month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    return lastBusinessDay.Add(new TimeSpan(12,01,0));
                })
            },
            // Y2TreasuryNote (ZT): http://www.cmegroup.com/trading/interest-rates/us-treasury/2-year-us-treasury-note_contract_specifications.html
            {Futures.Financials.Y2TreasuryNote, (time =>
                {
                    // Last business day of the calendar month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    return lastBusinessDay.Add(new TimeSpan(12,01,0));
                })
            },
            // EuroDollar Futures : TODO London bank calendar

            // Energies group
            // Propane Non LDH Mont Belvieu (1S): https://www.cmegroup.com/trading/energy/petrochemicals/propane-non-ldh-mt-belvieu-opis-balmo-swap_contract_specifications.html
            {Futures.Energies.PropaneNonLDHMontBelvieu, (time =>
                {
                    // Trading shall cease on the last business day of the contract month (no time specified)
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Argus Propane Far East Index BALMO (22): https://www.cmegroup.com/trading/energy/petrochemicals/argus-propane-far-east-index-balmo-swap-futures_contract_specifications.html
            {Futures.Energies.ArgusPropaneFarEastIndexBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month. Business days are based on the Singapore Public Holiday calendar.
                    // TODO: Might need singapore calendar
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Mini European 3.5% Fuel Oil Barges FOB Rdam (Platts) (A0D): https://www.cmegroup.com/trading/energy/refined-products/mini-european-35pct-fuel-oil-platts-barges-fob-rdam-swap-futures_contract_specifications.html
            {Futures.Energies.MiniEuropeanThreePointPercentFiveFuelOilBargesPlatts, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.MiniEuropeanThreePointPercentFiveFuelOilBargesPlatts, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Mini Singapore Fuel Oil 180 cst (Platts) (A0F): https://www.cmegroup.com/trading/energy/refined-products/mini-singapore-fuel-oil-180-cst-platts-swap-futures_contract_specifications.html
            {Futures.Energies.MiniSingaporeFuelOil180CstPlatts, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    // Special case exists where the last trade occurs on US holiday, but not an exchange holiday (markets closed)
                    // In order to fix that case, we will start from the last day of the month and go backwards checking if it's a weekday and a holiday
                    var lastDay = new DateTime(time.Year, time.Month, DateTime.DaysInMonth(time.Year, time.Month));
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.MiniSingaporeFuelOil180CstPlatts, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastDay) || !lastDay.IsCommonBusinessDay())
                    {
                        lastDay = lastDay.AddDays(-1);
                    }

                    return lastDay;
                })
            },
            // Gulf Coast ULSD (Platts) Up-Down BALMO Futures (A1L): https://www.cmegroup.com/trading/energy/refined-products/ulsd-up-down-balmo-calendar-swap-futures_contract_specifications.html
            {Futures.Energies.GulfCoastULSDPlattsUpDownBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Gulf Coast Jet (Platts) Up-Down BALMO Futures (A1M): https://www.cmegroup.com/trading/energy/refined-products/jet-fuel-up-down-balmo-calendar-swap_contract_specifications.html
            {Futures.Energies.GulfCoastJetPlattsUpDownBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Propane Non-LDH Mont Belvieu (OPIS) Futures (A1R): https://www.cmegroup.com/trading/energy/petrochemicals/propane-non-ldh-mt-belvieu-opis-swap_contract_specifications.html
            {Futures.Energies.PropaneNonLDHMontBelvieuOPIS, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // European Propane CIF ARA (Argus) BALMO Futures (A32): https://www.cmegroup.com/trading/energy/petrochemicals/european-propane-cif-ara-argus-balmo-swap-futures_contract_specifications.html
            {Futures.Energies.EuropeanPropaneCIFARAArgusBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Premium Unleaded Gasoline 10 ppm FOB MED (Platts) Futures (A3G): https://www.cmegroup.com/trading/energy/refined-products/premium-unleaded-10-ppm-platts-fob-med-swap_contract_specifications.html
            {Futures.Energies.PremiumUnleadedGasoline10ppmFOBMEDPlatts, (time =>
                {
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.PremiumUnleadedGasoline10ppmFOBMEDPlatts, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Argus Propane Far East Index Futures (A7E): 
            {Futures.Energies.ArgusPropaneFarEastIndex, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Gasoline Euro-bob Oxy NWE Barges (Argus) Crack Spread BALMO Futures (A7I): https://www.cmegroup.com/trading/energy/refined-products/gasoline-euro-bob-oxy-new-barges-crack-spread-balmo-swap-futures_contract_specifications.html
            {Futures.Energies.GasolineEurobobOxyNWEBargesArgusCrackSpreadBALMO, (time =>
                {
                    // Trading ceases on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Mont Belvieu Natural Gasoline (OPIS) Futures (A7Q): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-natural-gasoline-5-decimal-opis-swap_contract_specifications.html
            {Futures.Energies.MontBelvieuNaturalGasolineOPIS, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Mont Belvieu Normal Butane (OPIS) BALMO Futures (A8J): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-normal-butane-opis-balmo-swap_contract_specifications.html
            {Futures.Energies.MontBelvieuNormalButaneOPISBALMO, (time =>
                {
                    // Trading terminates on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Conway Propane (OPIS) Futures (A8K): https://www.cmegroup.com/trading/energy/petrochemicals/conway-propane-opis-swap_contract_specifications.html
            {Futures.Energies.ConwayPropaneOPIS, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Mont Belvieu LDH Propane (OPIS) BALMO Futures (A8O): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-ldh-propane-opis-balmo-swap-futures_contract_specifications.html
            {Futures.Energies.MontBelvieuLDHPropaneOPISBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Argus Propane Far East Index vs. European Propane CIF ARA (Argus) Futures (A91): https://www.cmegroup.com/trading/energy/petrochemicals/argus-propane-far-east-index-vs-european-propane-cif-ara-argus-swap-futures_contract_specifications.html
            {Futures.Energies.ArgusPropaneFarEastIndexVsEuropeanPropaneCIFARAArgus, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.ArgusPropaneFarEastIndexVsEuropeanPropaneCIFARAArgus, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Argus Propane (Saudi Aramco) Futures (A9N): https://www.cmegroup.com/trading/energy/petrochemicals/argus-propane-saudi-aramco-swap-futures_contract_specifications.html
            {Futures.Energies.ArgusPropaneSaudiAramco, (time =>
                {
                    // Trading shall terminate on the last business day of the month prior to the contract month.
                    // Business days are based on the Singapore Public Holiday Calendar.
                    // Special case in 2021 where last traded date falls on US Holiday, but not exchange holiday
                    var previousMonth = time.AddMonths(-1);
                    var lastDay = new DateTime(previousMonth.Year, previousMonth.Month, DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month));
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.ArgusPropaneSaudiAramco, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (!lastDay.IsCommonBusinessDay() || holidays.Contains(lastDay))
                    {
                        lastDay = lastDay.AddDays(-1);
                    }

                    return lastDay;
                })
            },
            // Group Three ULSD (Platts) vs. NY Harbor ULSD Futures (AA6): https://www.cmegroup.com/trading/energy/refined-products/group-three-ultra-low-sulfur-diesel-ulsd-platts-vs-heating-oil-spread-swap_contract_specifications.html
            {Futures.Energies.GroupThreeULSDPlattsVsNYHarborULSD, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Group Three Sub-octane Gasoline (Platts) vs. RBOB Futures (AA8): https://www.cmegroup.com/trading/energy/refined-products/group-three-unleaded-gasoline-platts-vs-rbob-spread-swap_contract_specifications.html
            {Futures.Energies.GroupThreeSuboctaneGasolinePlattsVsRBOB, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Singapore Fuel Oil 180 cst (Platts) BALMO Futures (ABS): https://www.cmegroup.com/trading/energy/refined-products/singapore-180cst-fuel-oil-balmo-swap_contract_specifications.html
            {Futures.Energies.SingaporeFuelOil180cstPlattsBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Singapore Fuel Oil 380 cst (Platts) BALMO Futures (ABT): https://www.cmegroup.com/trading/energy/refined-products/singapore-380cst-fuel-oil-balmo-swap_contract_specifications.html
            {Futures.Energies.SingaporeFuelOil380cstPlattsBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Mont Belvieu Ethane (OPIS) Futures (AC0): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-ethane-opis-5-decimals-swap_contract_specifications.html
            {Futures.Energies.MontBelvieuEthaneOPIS, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Mont Belvieu Normal Butane (OPIS) Futures (AD0): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-normal-butane-5-decimals-swap_contract_specifications.html
            {Futures.Energies.MontBelvieuNormalButaneOPIS, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Brent Crude Oil vs. Dubai Crude Oil (Platts) Futures (ADB): https://www.cmegroup.com/trading/energy/crude-oil/brent-dubai-swap-futures_contract_specifications.html
            {Futures.Energies.BrentCrudeOilVsDubaiCrudeOilPlatts, (time =>
                {
                    // Trading shall cease on the last London and Singapore business day of the contract month.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.BrentCrudeOilVsDubaiCrudeOilPlatts, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Argus LLS vs. WTI (Argus) Trade Month Futures (AE5): https://www.cmegroup.com/trading/energy/crude-oil/argus-lls-vs-wti-argus-trade-month-swap-futures_contract_specifications.html
            {Futures.Energies.ArgusLLSvsWTIArgusTradeMonth, (time =>
                {
                    // Trading shall cease at the close of trading on the last business day that falls on or before the 25th calendar day of the month prior to the contract month. If the 25th calendar day is a weekend or holiday, trading shall cease on the first business day prior to the 25th calendar day. 
                    var previousMonth = time.AddMonths(-1);
                    var twentyFifthDay = new DateTime(previousMonth.Year, previousMonth.Month, 25);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.ArgusLLSvsWTIArgusTradeMonth, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (!twentyFifthDay.IsCommonBusinessDay() || holidays.Contains(twentyFifthDay))
                    {
                        twentyFifthDay = FuturesExpiryUtilityFunctions.AddBusinessDays(twentyFifthDay, -1);
                    }

                    return twentyFifthDay;
                })
            },
            // Singapore Gasoil (Platts) vs. Low Sulphur Gasoil (AGA): https://www.cmegroup.com/trading/energy/refined-products/gasoil-arb-singapore-gasoil-platts-vs-ice-rdam-gasoil-swap_contract_specifications.html
            {Futures.Energies.SingaporeGasoilPlattsVsLowSulphurGasoilFutures, (time =>
                {
                    // Trading ceases on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.SingaporeGasoilPlattsVsLowSulphurGasoilFutures, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Los Angeles CARBOB Gasoline (OPIS) vs. RBOB Gasoline (AJL): https://www.cmegroup.com/trading/energy/refined-products/los-angeles-carbob-gasoline-opis-spread-swap_contract_specifications.html
            {Futures.Energies.LosAngelesCARBOBGasolineOPISvsRBOBGasoline, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Los Angeles Jet (OPIS) vs. NY Harbor ULSD (AJS): https://www.cmegroup.com/trading/energy/refined-products/los-angeles-carbob-gasoline-opis-spread-swap_contract_specifications.html
            {Futures.Energies.LosAngelesJetOPISvsNYHarborULSD, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Los Angeles CARB Diesel (OPIS) vs. NY Harbor ULSD (AKL): https://www.cmegroup.com/trading/energy/refined-products/los-angeles-carbob-diesel-opis-spread-swap_contract_specifications.html 
            {Futures.Energies.LosAngelesCARBDieselOPISvsNYHarborULSD, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // European Naphtha (Platts) BALMO (AKZ): https://www.cmegroup.com/trading/energy/refined-products/european-naphtha-balmo-swap_contract_specifications.html
            {Futures.Energies.EuropeanNaphthaPlattsBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // European Propane CIF ARA (Argus) (APS): https://www.cmegroup.com/trading/energy/petrochemicals/european-propane-cif-ara-argus-swap_contract_specifications.html
            {Futures.Energies.EuropeanPropaneCIFARAArgus, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.EuropeanPropaneCIFARAArgus, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Mont Belvieu Natural Gasoline (OPIS) BALMO (AR0): https://www.cmegroup.com/trading/energy/petrochemicals/mt-belvieu-natural-gasoline-balmo-swap_contract_specifications.html
            {Futures.Energies.MontBelvieuNaturalGasolineOPISBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // RBOB Gasoline Crack Spread (ARE): https://www.cmegroup.com/trading/energy/refined-products/rbob-crack-spread-swap-futures_contract_specifications.html
            {Futures.Energies.RBOBGasolineCrackSpread, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Gulf Coast HSFO (Platts) BALMO (AVZ): https://www.cmegroup.com/trading/energy/refined-products/gulf-coast-3pct-fuel-oil-balmo-swap_contract_specifications.html
            {Futures.Energies.GulfCoastHSFOPlattsBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Mars (Argus) vs. WTI Trade Month (AYV): https://www.cmegroup.com/trading/energy/crude-oil/mars-crude-oil-argus-vs-wti-trade-month-spread-swap-futures_contract_specifications.html
            {Futures.Energies.MarsArgusVsWTITradeMonth, (time =>
                {
                    // Trading shall cease at the close of trading on the last business day that falls on or before the 25th calendar day of the 
                    // month prior to the contract month. If the 25th calendar day is a weekend or holiday, trading shall cease on the 
                    // first business day prior to the 25th calendar day.
                    var twentyFifthDayPriorMonth = new DateTime(time.Year, time.Month, 25).AddMonths(-1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.MarsArgusVsWTITradeMonth, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (!FuturesExpiryUtilityFunctions.NotHoliday(twentyFifthDayPriorMonth) || holidays.Contains(twentyFifthDayPriorMonth))
                    {
                        twentyFifthDayPriorMonth = FuturesExpiryUtilityFunctions.AddBusinessDays(twentyFifthDayPriorMonth, -1);
                    }

                    return twentyFifthDayPriorMonth;
                })
            },
            // Mars (Argus) vs. WTI Financial (AYX): https://www.cmegroup.com/trading/energy/crude-oil/mars-crude-oil-argus-vs-wti-calendar-spread-swap-futures_contract_specifications.html
            {Futures.Energies.MarsArgusVsWTIFinancial, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.MarsArgusVsWTIFinancial, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Ethanol T2 FOB Rdam Including Duty (Platts) (AZ1): https://www.cmegroup.com/trading/energy/ethanol/ethanol-platts-t2-fob-rotterdam-including-duty-swap-futures_contract_specifications.html
            {Futures.Energies.EthanolT2FOBRdamIncludingDutyPlatts, (time =>
                {
                    // Trading terminates on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.EthanolT2FOBRdamIncludingDutyPlatts, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Mont Belvieu LDH Propane (OPIS) (B0): https://www.cmegroup.com/trading/energy/petrochemicals/mont-belvieu-propane-5-decimals-swap_contract_specifications.html
            {Futures.Energies.MontBelvieuLDHPropaneOPIS, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Gasoline Euro-bob Oxy NWE Barges (Argus) (B7H): https://www.cmegroup.com/trading/energy/refined-products/gasoline-euro-bob-oxy-new-barges-swap-futures_contract_specifications.html
            {Futures.Energies.GasolineEurobobOxyNWEBargesArgus, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.GasolineEurobobOxyNWEBargesArgus, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // WTI-Brent Financial (BK): https://www.cmegroup.com/trading/energy/crude-oil/wti-brent-ice-calendar-swap-futures_contract_specifications.html
            {Futures.Energies.WTIBrentFinancial, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.WTIBrentFinancial, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // 3.5% Fuel Oil Barges FOB Rdam (Platts) Crack Spread (1000mt) (BOO): https://www.cmegroup.com/trading/energy/refined-products/35pct-fuel-oil-platts-barges-fob-rdam-crack-spread-1000mt-swap-futures_contract_specifications.html
            {Futures.Energies.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread1000mt, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.ThreePointFivePercentFuelOilBargesFOBRdamPlattsCrackSpread1000mt, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Gasoline Euro-bob Oxy NWE Barges (Argus) BALMO (BR7): https://www.cmegroup.com/trading/energy/refined-products/gasoline-euro-bob-oxy-new-barges-balmo-swap-futures_contract_specifications.html
            {Futures.Energies.GasolineEurobobOxyNWEBargesArgusBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Brent Last Day Financial (BZ): https://www.cmegroup.com/trading/energy/crude-oil/brent-crude-oil-last-day_contract_specifications.html
            {Futures.Energies.BrentLastDayFinancial, (time =>
                {
                    // Trading terminates the last London business day of the month, 2 months prior to the contract month except for the February contract month which terminates the 2nd last London business day of the month, 2 months prior to the contract month.
                    var twoMonthsPriorToContractMonth = time.AddMonths(-2);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.BrentLastDayFinancial, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    DateTime lastBusinessDay;

                    if (twoMonthsPriorToContractMonth.Month == 2)
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(twoMonthsPriorToContractMonth, 1);
                    }
                    else
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(twoMonthsPriorToContractMonth, 1);
                    }

                    while (holidays.Contains(lastBusinessDay))
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // CrudeOilWTI (CL): http://www.cmegroup.com/trading/energy/crude-oil/light-sweet-crude_contract_specifications.html
            {Futures.Energies.CrudeOilWTI, (time =>
                {
                    // Trading in the current delivery month shall cease on the third business day prior to the twenty-fifth calendar day of the month preceding the delivery month. If the twenty-fifth calendar day of the month is a non-business day, trading shall cease on the third business day prior to the last business day preceding the twenty-fifth calendar day. In the event that the official Exchange holiday schedule changes subsequent to the listing of a Crude Oil futures, the originally listed expiration date shall remain in effect.In the event that the originally listed expiration day is declared a holiday, expiration will move to the business day immediately prior.
                    var twentyFifth = new DateTime(time.Year,time.Month,25);
                    twentyFifth = twentyFifth.AddMonths(-1);
                    if(FuturesExpiryUtilityFunctions.NotHoliday(twentyFifth))
                    {
                        return FuturesExpiryUtilityFunctions.AddBusinessDays(twentyFifth,-3);
                    }
                    else
                    {
                        var lastBuisnessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(twentyFifth,-1);
                        return FuturesExpiryUtilityFunctions.AddBusinessDays(lastBuisnessDay,-3);
                    }
                })
            },
            // Gulf Coast CBOB Gasoline A2 (Platts) vs. RBOB Gasoline (CRB): https://www.cmegroup.com/trading/energy/refined-products/gulf-coast-cbob-gasoline-a2-platts-vs-rbob-spread-swap_contract_specifications.html
            {Futures.Energies.GulfCoastCBOBGasolineA2PlattsVsRBOBGasoline, (time =>
                {
                    // Trading shall cease on the last business day of the contract month.
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Clearbrook Bakken Sweet Crude Oil Monthly Index (Net Energy) (CSW): https://www.cmegroup.com/trading/energy/crude-oil/clearbrook-bakken-crude-oil-index-net-energy_contract_specifications.html
            {Futures.Energies.ClearbrookBakkenSweetCrudeOilMonthlyIndexNetEnergy, (time =>
                {
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
            {Futures.Energies.WTIFinancial, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.WTIFinancial, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Chicago Ethanol (Platts) (CU): https://www.cmegroup.com/trading/energy/ethanol/chicago-ethanol-platts-swap_contract_specifications.html a
            {Futures.Energies.ChicagoEthanolPlatts, (time =>
                {
                    // Trading terminates on the last business day of the contract month
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Singapore Mogas 92 Unleaded (Platts) Brent Crack Spread (D1N): https://www.cmegroup.com/trading/energy/refined-products/singapore-mogas-92-unleaded-platts-brent-crack-spread-swap-futures_contract_specifications.html
            {Futures.Energies.SingaporeMogas92UnleadedPlattsBrentCrackSpread, (time =>
                {
                    // Trading shall cease on the last business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.SingaporeMogas92UnleadedPlattsBrentCrackSpread, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // Dubai Crude Oil (Platts) Financial (DCB): https://www.cmegroup.com/trading/energy/crude-oil/dubai-crude-oil-calendar-swap-futures_contract_specifications.html
            {Futures.Energies.DubaiCrudeOilPlattsFinancial, (time =>
                {
                    // Trading shall cease on the last London and Singapore business day of the contract month
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.DubaiCrudeOilPlattsFinancial, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;

                    while (holidays.Contains(lastBusinessDay) || !lastBusinessDay.IsCommonBusinessDay())
                    {
                        lastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(lastBusinessDay, -1);
                    }

                    return lastBusinessDay;
                })
            },
            // JJapan C&F Naphtha (Platts) BALMO (E6): https://www.cmegroup.com/trading/energy/refined-products/japan-naphtha-balmo-swap-futures_contract_specifications.html
            {Futures.Energies.JapanCnFNaphthaPlattsBALMO, (time =>
                {
                    // Trading shall cease on the last business day of the contract month. 
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                })
            },
            // Ethanol (EH): https://www.cmegroup.com/trading/energy/ethanol/cbot-ethanol_contract_specifications.html
            {Futures.Energies.Ethanol, (time =>
                {
                    // Trading terminates on 3rd business day of the contract month in "ctm"
                    var currentDay = time;
                    var daysCounted = time.IsCommonBusinessDay() ? 1 : 0;
                    var i = 0;
                    var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry("usa", Futures.Energies.Ethanol, SecurityType.Future)
                        .ExchangeHours
                        .Holidays;
                    
                    while (daysCounted < 3)
                    {
                        if (holidays.Contains(currentDay) || USHoliday.Dates.Contains(currentDay))
                        {
                            // Catches edge case where first day is on a friday
                            if (i == 0 && currentDay.DayOfWeek == DayOfWeek.Friday)
                            {
                                daysCounted = 0;
                            }

                            currentDay = currentDay.AddDays(1);

                            if (i != 0 && currentDay.IsCommonBusinessDay())
                            {
                                daysCounted++;
                            }
                            i++;
                            continue;
                        }

                        currentDay = currentDay.AddDays(1);

                        if (!holidays.Contains(currentDay) && FuturesExpiryUtilityFunctions.NotHoliday(currentDay))
                        {
                            daysCounted++;
                        }
                        i++;
                    }

                    return currentDay;
                })
            },
            // HeatingOil (HO): http://www.cmegroup.com/trading/energy/refined-products/heating-oil_contract_specifications.html
            {Futures.Energies.HeatingOil, (time =>
                {
                    // Trading in a current month shall cease on the last business day of the month preceding the delivery month.
                    var precedingMonth = time.AddMonths(-1);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(precedingMonth, 1);
                })
            },
            // Gasoline (RB): http://www.cmegroup.com/trading/energy/refined-products/rbob-gasoline_contract_specifications.html
            {Futures.Energies.Gasoline, (time =>
                {
                    // Trading in a current delivery month shall cease on the last business day of the month preceding the delivery month.
                    var precedingMonth = time.AddMonths(-1);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(precedingMonth, 1);
                })
            },
            // Natural Gas (NG) : http://www.cmegroup.com/trading/energy/natural-gas/natural-gas_contract_specifications.html
            {Futures.Energies.NaturalGas, (time =>
                {
                    //Trading of any delivery month shall cease three (3) business days prior to the first day of the delivery month. In the event that the official Exchange holiday schedule changes subsequent to the listing of a Natural Gas futures, the originally listed expiration date shall remain in effect.In the event that the originally listed expiration day is declared a holiday, expiration will move to the business day immediately prior.
                    var firstDay = new DateTime(time.Year,time.Month,1);
                    return FuturesExpiryUtilityFunctions.AddBusinessDays(firstDay,-3);
                })
            },

            // Meats group
            // LiveCattle (LE): http://www.cmegroup.com/trading/agricultural/livestock/live-cattle_contract_specifications.html
            {Futures.Meats.LiveCattle, (time =>
                {
                    //Last business day of the contract month, 12:00 p.m.
                    var lastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(time, 1);
                    return lastBusinessDay.Add(new TimeSpan(12,0,0));
                })
            },
            // LeanHogs (HE): http://www.cmegroup.com/trading/agricultural/livestock/lean-hogs_contract_specifications.html
            {Futures.Meats.LeanHogs, (time =>
                {
                    // 10th business day of the contract month, 12:00 p.m.
                    var lastday = new DateTime(time.Year,time.Month,1);
                    lastday = lastday.AddDays(-1);
                    var tenthday = FuturesExpiryUtilityFunctions.AddBusinessDays(lastday,10);
                    return tenthday.Add(new TimeSpan(12,0,0));
                })
            },
            // FeederCattle (GF): http://www.cmegroup.com/trading/agricultural/livestock/feeder-cattle_contract_specifications.html
            {Futures.Meats.FeederCattle, (time =>
                {
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
                        while (!FuturesExpiryUtilityFunctions.NotHoliday(priorThursday) || !FuturesExpiryUtilityFunctions.NotPrecededByHoliday(priorThursday))
                        {
                            priorThursday = priorThursday.AddDays(-7);
                        }
                        return priorThursday;
                    }
                    // Checking Condition 2
                    var lastThursday = (from day in Enumerable.Range(1, daysInMonth)
                                  where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Thursday
                                  select new DateTime(time.Year, time.Month, day)).Reverse().ElementAt(0);
                    while (!FuturesExpiryUtilityFunctions.NotHoliday(lastThursday) || !FuturesExpiryUtilityFunctions.NotPrecededByHoliday(lastThursday))
                    {
                        lastThursday = lastThursday.AddDays(-7);
                    }
                    return lastThursday;
                })
            },
            // Sugar #11 CME (YO): https://www.cmegroup.com/trading/agricultural/softs/sugar-no11_contract_specifications.html
            {Futures.Softs.Sugar11CME, (time =>
                {
                    // Trading terminates on the day immediately preceding the first notice day of the corresponding trading month of Sugar No. 11 futures at ICE Futures U.S.
                    var precedingMonth = time.AddMonths(-1);
                    return FuturesExpiryUtilityFunctions.NthLastBusinessDay(precedingMonth, 1);
                })
            },
            // Dairy Group
            // Cash-settled Butter (CB): https://www.cmegroup.com/trading/agricultural/dairy/cash-settled-butter_contract_specifications.html
            {Futures.Dairy.CashSettledButter, (time =>
                {
                    // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the Butter price for that contract month. (LTD 12:10 p.m.)
                    DateTime publicationDate;
                    if (DairyReportDates.TryGetValue(time, out publicationDate))
                    {
                        do
                        {
                            publicationDate = publicationDate.AddDays(-1);
                        }
                        while (!FuturesExpiryUtilityFunctions.NotHoliday(publicationDate));
                    }
                    else
                    {
                        publicationDate = time;
                    }

                    // The USDA price announcements are erratic in their publication date. You can view the calendar the USDA announces prices here: https://www.ers.usda.gov/calendar/
                    // More specifically, the report you should be looking for has the name "National Dairy Products Sales Report"

                    return publicationDate.Add(new TimeSpan(17, 10, 0));
                })
            },
            // Cash-Settled Cheese (CSC): https://www.cmegroup.com/trading/agricultural/dairy/cheese_contract_specifications.html
            {Futures.Dairy.CashSettledCheese, (time =>
                {
                    // Trading shall terminate on the business day immediately preceding the release date for the USDA monthly weighted average price in the U.S. for cheese. LTD close is at 12:10 p.m. Central Time 
                    DateTime publicationDate;
                    if (DairyReportDates.TryGetValue(time, out publicationDate))
                    {
                        do
                        {
                            publicationDate = publicationDate.AddDays(-1);
                        }
                        while (!FuturesExpiryUtilityFunctions.NotHoliday(publicationDate));
                    }
                    else
                    {
                        publicationDate = time;
                    }

                    // The USDA price announcements are erratic in their publication date. You can view the calendar the USDA announces prices here: https://www.ers.usda.gov/calendar/
                    // More specifically, the report you should be looking for has the name "National Dairy Products Sales Report"

                    return publicationDate.Add(new TimeSpan(17, 10, 0));
                })
            },
            // Class III Milk (DC): https://www.cmegroup.com/trading/agricultural/dairy/class-iii-milk_contract_specifications.html
            {Futures.Dairy.ClassIIIMilk, (time =>
                {
                    // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the Class III price for that contract month (LTD 12:10 p.m.)
                    DateTime publicationDate;
                    if (DairyReportDates.TryGetValue(time, out publicationDate))
                    {
                        do
                        {
                            publicationDate = publicationDate.AddDays(-1);
                        }
                        while (!FuturesExpiryUtilityFunctions.NotHoliday(publicationDate));
                    }
                    else
                    {
                        publicationDate = time;
                    }

                    // The USDA price announcements are erratic in their publication date. You can view the calendar the USDA announces prices here: https://www.ers.usda.gov/calendar/
                    // More specifically, the report you should be looking for has the name "National Dairy Products Sales Report"

                    return publicationDate.Add(new TimeSpan(17, 10, 0));
                })
            },
            // Dry Whey (DY): https://www.cmegroup.com/trading/agricultural/dairy/dry-whey_contract_specifications.html
            {Futures.Dairy.DryWhey, (time =>
                {
                    // Trading shall terminate on the business day immediately preceding the day on which the USDA announces the Dry Whey price for that contract month. (LTD 12:10 p.m.) 
                    DateTime publicationDate;
                    if (DairyReportDates.TryGetValue(time, out publicationDate))
                    {
                        do
                        {
                            publicationDate = publicationDate.AddDays(-1);
                        }
                        while (!FuturesExpiryUtilityFunctions.NotHoliday(publicationDate));
                    }
                    else
                    {
                        publicationDate = time;
                    }

                    // The USDA price announcements are erratic in their publication date. You can view the calendar the USDA announces prices here: https://www.ers.usda.gov/calendar/
                    // More specifically, the report you should be looking for has the name "National Dairy Products Sales Report"

                    return publicationDate.Add(new TimeSpan(17, 10, 0));
                })
            },
        };
    }
}
