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
    /// Calculate the date of an futures expiry given an expiry month and year
    /// </summary>
    public class FuturesExpiryFunctions
    {

        /// <summary>
        /// Method to retrieve the Func for a specific future symbol
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
        /// Dictorionary of the Func that calculates the expiry for a given year and month.
        /// It does not matter what the day and time of day are passed into the Func.
        /// The Func is reposible for calulating the day and time of day given a year and month
        /// </summary>
        public static Dictionary<string, Func<DateTime, DateTime>> FuturesExpiryDictionary = new Dictionary<string, Func<DateTime, DateTime>>()
        {
            // Gold (EC): http://www.cmegroup.com/trading/metals/precious/gold_contract_specifications.html
            {Futures.Metals.Gold, (time =>
                {
                    // Trading terminates on the third last business day of the delivery month.
                    var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
                    var lastDayOfMonth = new DateTime(time.Year, time.Month, daysInMonth);

                    // Count the number of days in the month after the third to last business day
                    var businessDays = 3;
                    var totalDays = 0;
                    do
                    {
                        var previousDay = lastDayOfMonth.AddDays(-totalDays);
                        if (previousDay.IsCommonBusinessDay() && !USHoliday.Dates.Contains(previousDay))
                        {
                            businessDays--;
                        }
                        if (businessDays > 0) totalDays++;
                    } while (businessDays > 0);

                    return lastDayOfMonth.AddDays(-totalDays);
                })
            },

            // SP500EMini (ES): http://www.cmegroup.com/trading/equity-index/us-index/e-mini-sandp500_contract_specifications.html
            {Futures.Indices.SP500EMini, (time =>
                {
                    // Trading can occur up to 9:30 a.m. Eastern Time (ET) on the 3rd Friday of the contract month
                    var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
                    return (from day in Enumerable.Range(1, daysInMonth)
                        where new DateTime(time.Year, time.Month, day).DayOfWeek == DayOfWeek.Friday
                        select new DateTime(time.Year, time.Month, day, 9, 30, 0)).Reverse().ElementAt(2);
                })
            }
        };
    }
}
