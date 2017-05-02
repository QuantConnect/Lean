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

            // If func for expiry cannot be found pass the date through
            return (date) => date;
        }

        /// <summary>
        /// Method to retrieve n^th succeeding/preceding business day for a given day
        /// </summary>
        private static DateTime NthBusinessDay(DateTime time, int n)
        {
            if (n < 0)
            {
                var businessDays = (-1) * n;
                var totalDays = 1;
                do
                {
                    var previousDay = time.AddDays(-totalDays);
                    if (notHoliday(previousDay))
                    {
                        businessDays--;
                    }
                    if (businessDays > 0) totalDays++;
                } while (businessDays > 0);

                return time.AddDays(-totalDays);
            }
            else
            {
                var businessDays = n;
                var totalDays = 1;
                do
                {
                    var previousDay = time.AddDays(totalDays);
                    if (notHoliday(previousDay))
                    {
                        businessDays--;
                    }
                    if (businessDays > 0) totalDays++;
                } while (businessDays > 0);

                return time.AddDays(totalDays);
            }
        }

        /// <summary>
        /// Method to retrieve the third last business day of the delivery month.
        /// </summary>
        private static DateTime ThirdLastBusinessDay(DateTime time)
        {
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            var lastDayOfMonth = new DateTime(time.Year, time.Month, daysInMonth);

            // Count the number of days in the month after the third to last business day
            var businessDays = 3;
            var totalDays = 0;
            do
            {
                var previousDay = lastDayOfMonth.AddDays(-totalDays);
                if (notHoliday(previousDay))
                {
                    businessDays--;
                }
                if (businessDays > 0) totalDays++;
            } while (businessDays > 0);

            return lastDayOfMonth.AddDays(-totalDays);
        }

        /// <summary>
        /// Method to retrieve 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
        /// </summary>
        private static DateTime ThirdFridayAtNineThirty(DateTime time)
        {
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            return (from day in Enumerable.Range(1, daysInMonth)
                    where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Friday
                    select new DateTime(time.Year, time.Month, day, 13, 30, 0)).ElementAt(2);
        }

        ///<summary>
        /// Method to retrieve the business day prior to the 15th calendar day of the contract month.
        /// </summary>
        private static DateTime BusinessDayBeforeFifteenth(DateTime time)
        {
            var fifteenthDayOfMonth = new DateTime(time.Year, time.Month, 15);
            // Get the previous businessday recursively
            var previousDay = fifteenthDayOfMonth.AddDays(-1);
            while (!previousDay.IsCommonBusinessDay() || USHoliday.Dates.Contains(previousDay))
            {
                previousDay = previousDay.AddDays(-1);
            }
            return previousDay;
        }

        /// <summary>
        /// Method to retrieve 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
        /// </summary>
        private static DateTime SecondBusinessDayPrecedingThirdWednesdayAtNineSixteen(DateTime time)
        {
            // Get Third Wednesday
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            var ThirdWednesday = (from day in Enumerable.Range(1, daysInMonth)
                                  where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Wednesday
                                  select new DateTime(time.Year, time.Month, day)).ElementAt(2);

            // Now get the second business day preceding third Wednesday
            var previousDay = NthBusinessDay(ThirdWednesday, -2);
            return new DateTime(previousDay.Year, previousDay.Month, previousDay.Day, 14, 16, 0);
        }

        /// <summary>
        /// Method to retrieve 9:16 a.m. Central Time (CT) on the business day immediately preceding the third Wednesday of the contract month (usually Tuesday).
        /// </summary>
        private static DateTime BusinessDayPrecedingThirdWednesdayAtNineSixteen(DateTime time)
        {
            // Get Third Wednesday
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            var ThirdWednesday = (from day in Enumerable.Range(1, daysInMonth)
                                  where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Wednesday
                                  select new DateTime(time.Year, time.Month, day)).ElementAt(2);

            // Now get the second business day preceding third Wednesday
            var previousDay = NthBusinessDay(ThirdWednesday, -1);
            return new DateTime(previousDay.Year, previousDay.Month, previousDay.Day, 14, 16, 0);
        }

        /// <summary>
        /// Mehtod to retrieve 12:01 on last business day of the calendar month.
        /// </summary>
        private static DateTime LastBusinessDay(DateTime time, TimeSpan t)
        {
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            return (from day in Enumerable.Range(1, daysInMonth)
                    let _day = new DateTime(time.Year, time.Month, day, t.Hours, t.Minutes, t.Seconds)
                    where notHoliday(_day)
                    select _day).Reverse().ElementAt(0);
        }

        /// <summary>
        /// Method to retrieve 12:01 pm on the seventh business day preceding the last business day of the delivery month.
        /// </summary>
        private static DateTime SeventhBusinessDayPreceedingLastBusinessDay(DateTime time)
        {
            var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
            var lastBusinessDay = (from day in Enumerable.Range(1, daysInMonth)
                                   let _day = new DateTime(time.Year, time.Month, day)
                                   where notHoliday(_day)
                                   select _day).Reverse().ElementAt(0);
            var seventhPreceding = NthBusinessDay(lastBusinessDay, -7);
            return new DateTime(seventhPreceding.Year, seventhPreceding.Month, seventhPreceding.Day, 12, 01, 0);
        }

        /// <summary>
        /// Method to retrieve last business day of the month preceding the delivery month. 
        /// </summary>
        private static DateTime LastBusinessDayPrecedingMonth(DateTime time)
        {
            var precedingMonth = time.AddMonths(-1);
            var daysInMonth = DateTime.DaysInMonth(precedingMonth.Year, precedingMonth.Month);
            var lastBusinessDay = (from day in Enumerable.Range(1, daysInMonth)
                                   let _day = new DateTime(precedingMonth.Year, precedingMonth.Month, day)
                                   where notHoliday(_day)
                                   select _day).Reverse().ElementAt(0);
            return lastBusinessDay;
        }

        /// <summary>
        /// Method to check whether a given time is holiday or not
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private static bool notHoliday(DateTime time)
        {
            return time.IsCommonBusinessDay() && !USHoliday.Dates.Contains(time);
        }

        /// <summary>
        /// This function takes Thursday as input and returns true if four weekdays preceding it are not Holidays
        /// </summary>
        /// <param name="Thursday"></param>
        /// <returns></returns>
        private static bool notPrecededByHoliday(DateTime Thursday)
        {
            if (Thursday.DayOfWeek != DayOfWeek.Thursday)
            {
                return false;
            }
            var result = true;
            // for Monday, Tuesday and Wednesday
            for (int i = 1; i <= 3; i++)
            {
                if (!notHoliday(Thursday.AddDays(-i)))
                {
                    result = false;
                }
            }
            // for Friday
            if (!notHoliday(Thursday.AddDays(-6)))
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Dictorionary of the Func that calculates the expiry for a given year and month.
        /// It does not matter what the day and time of day are passed into the Func.
        /// The Func is reposible for calulating the day and time of day given a year and month
        /// </summary>
        public static Dictionary<string, Func<DateTime, DateTime>> FuturesExpiryDictionary = new Dictionary<string, Func<DateTime, DateTime>>()
        {
            // Metals
            // Gold (GC): http://www.cmegroup.com/trading/metals/precious/gold_contract_specifications.html
            {Futures.Metals.Gold, (time =>
                {
                    // Trading terminates on the third last business day of the delivery month.
                    return ThirdLastBusinessDay(time);
                })
            },
            // Silver (SI): http://www.cmegroup.com/trading/metals/precious/silver_contract_specifications.html
            {Futures.Metals.Silver, (time =>
                {
                    // Trading terminates on the third last business day of the delivery month.
                    return ThirdLastBusinessDay(time);
                })
            },
            // Platinum (PL): http://www.cmegroup.com/trading/metals/precious/platinum_contract_specifications.html
            {Futures.Metals.Platinum, (time =>
                {
                    // Trading terminates on the third last business day of the delivery month.
                    return ThirdLastBusinessDay(time);
                })
            },
            // Palladium (PA): http://www.cmegroup.com/trading/metals/precious/palladium_contract_specifications.html
            {Futures.Metals.Palladium, (time =>
                {
                    // Trading terminates on the third last business day of the delivery month.
                    return ThirdLastBusinessDay(time);
                })
            },
            // Indices
            // SP500EMini (ES): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-sandp500_contract_specifications.html
            {Futures.Indices.SP500EMini, (time =>
                {
                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    return ThirdFridayAtNineThirty(time);
                })
            },
            // NASDAQ100EMini (NQ): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-nasdaq-100_contract_specifications.html
            {Futures.Indices.NASDAQ100EMini, (time =>
                {
                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    return ThirdFridayAtNineThirty(time);
                })
            },
            // Dow30EMini (YM): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-dow_contract_specifications.html
            {Futures.Indices.Dow30EMini, (time =>
                {
                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    return ThirdFridayAtNineThirty(time);
                })
            },
            // CBOE Volatility Index Futures (VIX)  is not found on cmegroup will discuss and update
            // Grains And OilSeeds Group
            // Wheat (ZW): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/wheat_contract_specifications.html
            {Futures.Grains.Wheat, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    return BusinessDayBeforeFifteenth(time);
                })
            },
            // Corn (ZC): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/corn_contract_specifications.html
            {Futures.Grains.Corn, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    return BusinessDayBeforeFifteenth(time);
                })
            },
            // Soybeans (ZS): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/soybean_contract_specifications.html
            {Futures.Grains.Soybeans, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    return BusinessDayBeforeFifteenth(time);
                })
            },
            // SoybeanMeal (ZM): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/soybean-meal_contract_specifications.html
            {Futures.Grains.SoybeanMeal, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    return BusinessDayBeforeFifteenth(time);
                })
            },
            // SoybeanOil (ZL): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/soybean-oil_contract_specifications.html
            {Futures.Grains.SoybeanOil, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    return BusinessDayBeforeFifteenth(time);
                })
            },
            // Oats (ZO): http://www.cmegroup.com/trading/agricultural/grain-and-oilseed/oats_contract_specifications.html
            {Futures.Grains.Oats, (time =>
                {
                    // The business day prior to the 15th calendar day of the contract month.
                    return BusinessDayBeforeFifteenth(time);
                })
            },

            // Currencies group
            // U.S. Dollar Index Futures is not found on cmegroup will discuss and update
            //  GBP (6B): http://www.cmegroup.com/trading/fx/g10/british-pound_contract_specifications.html
            {Futures.Currencies.GBP, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    return SecondBusinessDayPrecedingThirdWednesdayAtNineSixteen(time);
                })
            },
            // CAD (6C): http://www.cmegroup.com/trading/fx/g10/canadian-dollar_contract_specifications.html
            {Futures.Currencies.CAD, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the business day immediately preceding the third Wednesday of the contract month (usually Tuesday).
                    return BusinessDayPrecedingThirdWednesdayAtNineSixteen(time);
                })
            },
            // JPY (6J): http://www.cmegroup.com/trading/fx/g10/japanese-yen_contract_specifications.html
            {Futures.Currencies.JPY, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    return SecondBusinessDayPrecedingThirdWednesdayAtNineSixteen(time);
                })
            },
            // CHF (6S): http://www.cmegroup.com/trading/fx/g10/swiss-franc_contract_specifications.html
            {Futures.Currencies.CHF, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    return SecondBusinessDayPrecedingThirdWednesdayAtNineSixteen(time);
                })
            },
            // EUR (6E): http://www.cmegroup.com/trading/fx/g10/euro-fx_contract_specifications.html
            {Futures.Currencies.EUR, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    return SecondBusinessDayPrecedingThirdWednesdayAtNineSixteen(time);
                })
            },
            // AUD (6A): http://www.cmegroup.com/trading/fx/g10/australian-dollar_contract_specifications.html
            {Futures.Currencies.AUD, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    return SecondBusinessDayPrecedingThirdWednesdayAtNineSixteen(time);
                })
            },
            // NZD (6N): http://www.cmegroup.com/trading/fx/g10/new-zealand-dollar_contract_specifications.html
            {Futures.Currencies.NZD, (time =>
                {
                    // 9:16 a.m. Central Time (CT) on the second business day immediately preceding the third Wednesday of the contract month (usually Monday).
                    return SecondBusinessDayPrecedingThirdWednesdayAtNineSixteen(time);
                })
            },

            // Financials group
            // Y30TreasuryBond (ZB): http://www.cmegroup.com/trading/interest-rates/us-treasury/30-year-us-treasury-bond_contract_specifications.html
            {Futures.Financials.Y30TreasuryBond, (time =>
                {
                    //  Seventh business day preceding the last business day of the delivery month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    return SeventhBusinessDayPreceedingLastBusinessDay(time);
                })
            },
            // Y10TreasuryNote (ZN): http://www.cmegroup.com/trading/interest-rates/us-treasury/10-year-us-treasury-note_contract_specifications.html
            {Futures.Financials.Y10TreasuryNote, (time =>
                {
                    //  Seventh business day preceding the last business day of the delivery month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    return SeventhBusinessDayPreceedingLastBusinessDay(time);
                })
            },
            // Y5TreasuryNote (ZF): http://www.cmegroup.com/trading/interest-rates/us-treasury/5-year-us-treasury-note_contract_specifications.html
            {Futures.Financials.Y5TreasuryNote, (time =>
                {
                    // Last business day of the calendar month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    return LastBusinessDay(time, new TimeSpan(12,1,0));
                })
            },
            // Y2TreasuryNote (ZT): http://www.cmegroup.com/trading/interest-rates/us-treasury/2-year-us-treasury-note_contract_specifications.html
            {Futures.Financials.Y2TreasuryNote, (time =>
                {
                    // Last business day of the calendar month. Trading in expiring contracts closes at 12:01 p.m. on the last trading day.
                    return LastBusinessDay(time, new TimeSpan(12,1,0));
                })
            },
            // EuroDollar Futures : TODO London bank calendar

            // Energies group
            // CrudeOilWTI (CL): http://www.cmegroup.com/trading/energy/crude-oil/light-sweet-crude_contract_specifications.html
            {Futures.Energies.CrudeOilWTI, (time =>
                {
                    // Trading in the current delivery month shall cease on the third business day prior to the twenty-fifth calendar day of the month preceding the delivery month. If the twenty-fifth calendar day of the month is a non-business day, trading shall cease on the third business day prior to the last business day preceding the twenty-fifth calendar day. In the event that the official Exchange holiday schedule changes subsequent to the listing of a Crude Oil futures, the originally listed expiration date shall remain in effect.In the event that the originally listed expiration day is declared a holiday, expiration will move to the business day immediately prior.
                    var twenty_fifth = new DateTime(time.Year,time.Month,25);
                    twenty_fifth = twenty_fifth.AddMonths(-1);
                    if(notHoliday(twenty_fifth))
                    {
                        return NthBusinessDay(twenty_fifth,-3);
                    }
                    else
                    {
                        var lastBuisnessDay = NthBusinessDay(twenty_fifth,-1);
                        return NthBusinessDay(lastBuisnessDay,-3);
                    }
                })
            },
            // HeatingOil (HO): http://www.cmegroup.com/trading/energy/refined-products/heating-oil_contract_specifications.html
            {Futures.Energies.HeatingOil, (time =>
                {
                    // Trading in a current month shall cease on the last business day of the month preceding the delivery month.
                    return LastBusinessDayPrecedingMonth(time);
                })
            },
            // Gasoline (RB): http://www.cmegroup.com/trading/energy/refined-products/rbob-gasoline_contract_specifications.html
            {Futures.Energies.Gasoline, (time =>
                {
                    // Trading in a current delivery month shall cease on the last business day of the month preceding the delivery month.
                    return LastBusinessDayPrecedingMonth(time);
                })
            },
            // Natural Gas (NG) : http://www.cmegroup.com/trading/energy/natural-gas/natural-gas_contract_specifications.html
            {Futures.Energies.NaturalGas, (time =>
                {
                    //Trading of any delivery month shall cease three (3) business days prior to the first day of the delivery month. In the event that the official Exchange holiday schedule changes subsequent to the listing of a Natural Gas futures, the originally listed expiration date shall remain in effect.In the event that the originally listed expiration day is declared a holiday, expiration will move to the business day immediately prior.
                    var firstDay = new DateTime(time.Year,time.Month,1);
                    return NthBusinessDay(firstDay,-3);
                })
            },

            // Meats group
            // LiveCattle (LE): http://www.cmegroup.com/trading/agricultural/livestock/live-cattle_contract_specifications.html
            {Futures.Meats.LiveCattle, (time =>
                {
                    //Last business day of the contract month, 12:00 p.m.
                    return LastBusinessDay(time, new TimeSpan(12,0,0));
                })
            },
            // LeanHogs (HE): http://www.cmegroup.com/trading/agricultural/livestock/lean-hogs_contract_specifications.html
            {Futures.Meats.LeanHogs, (time =>
                {
                    // 10th business day of the contract month, 12:00 p.m.
                    var lastday = new DateTime(time.Year,time.Month,1);
                    lastday = lastday.AddDays(-1);
                    var tenthday = NthBusinessDay(lastday,10);
                    return new DateTime(tenthday.Year,tenthday.Month,tenthday.Day,12,0,0);
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
                        var PriorThursday = (from day in Enumerable.Range(1, daysInMonth)
                                  where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Thursday
                                  select new DateTime(time.Year, time.Month, day)).Reverse().ElementAt(1);
                        while (!notHoliday(PriorThursday) || !notPrecededByHoliday(PriorThursday))
                        {
                            PriorThursday = PriorThursday.AddDays(-7);
                        }
                        return PriorThursday;
                    }
                    // Checking Condition 2
                    var lastThursday = (from day in Enumerable.Range(1, daysInMonth)
                                  where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Thursday
                                  select new DateTime(time.Year, time.Month, day)).Reverse().ElementAt(0);
                    while (!notHoliday(lastThursday) || !notPrecededByHoliday(lastThursday))
                    {
                        lastThursday = lastThursday.AddDays(-7);
                    }
                    return lastThursday;
                })
            }
        };
    }
}
