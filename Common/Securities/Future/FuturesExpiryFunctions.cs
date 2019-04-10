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
            //Propane Non LDH Mont Belvieu (1S): https://www.cmegroup.com/trading/energy/petrochemicals/propane-non-ldh-mt-belvieu-opis-balmo-swap_contract_specifications.html
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
            }
        };
    }
}
