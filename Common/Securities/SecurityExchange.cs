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

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;

namespace QuantConnect.Securities
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Base exchange class providing information and helper tools for reading the current exchange situation
    /// </summary>
    public class SecurityExchange 
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        private DateTime _frontier;
        private TimeSpan _marketOpen = TimeSpan.FromHours(0);
        private TimeSpan _marketClose = TimeSpan.FromHours(23.999999);

        /******************************************************** 
        * CLASS CONSTRUCTION
        *********************************************************/
        /// <summary>
        /// Initialise the exchange for this vehicle.
        /// </summary>
        public SecurityExchange() 
        { }
        
        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Timezone for the exchange
        /// </summary>
        public string TimeZone 
        {
            get;
            set;
        }

        /// <summary>
        /// Default market open time 00:00
        /// </summary>
        public virtual TimeSpan MarketOpen
        {
            get { return _marketOpen; }
            set { _marketOpen = value; }
        }

        /// <summary>
        /// Default market closing time 24:00
        /// </summary>
        public virtual TimeSpan MarketClose
        {
            get { return _marketClose; }
            set { _marketClose = value; }
        }


        /// <summary>
        /// Number of trading days per year for this security. By default the market is open 365 days per year.
        /// </summary>
        /// <remarks>Used for performance statistics to calculate sharpe ratio accurately</remarks>
        public virtual int TradingDaysPerYear 
        {
            get 
            {
                return 365;
            }
        }


        /// <summary>
        /// Time from the most recent data
        /// </summary>
        public DateTime Time 
        {
            get 
            {
                return _frontier;
            }
        }


        /// <summary>
        /// Boolean property for quickly testing if the exchange is open.
        /// </summary>
        public virtual bool ExchangeOpen 
        {
            get 
            { 
                return DateTimeIsOpen(Time); 
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Check if we are past a certain time: simple method for wrapping datetime.
        /// </summary>
        public bool TimeIsPast(int iHour, int iMin, int iSec = 0) {

            if (Time.Hour > iHour) {
                return true;
            
            } else if (Time.Hour < iHour) {
                return false;
            
            } else if (Time.Hour == iHour) {
                if (Time.Minute > iMin) {
                    return true;

                } else if (Time.Minute < iMin) {
                    return false;

                } else if (Time.Minute == iMin) {
                    //Minute Equal, Check Seconds.
                    if (Time.Second >= iSec) {
                        return true;
                    } else if (Time.Second < iSec) {
                        return false;
                    }
                }
            }
            return false;
        }



        /// <summary>
        /// Set the current datetime:
        /// </summary>
        /// <param name="newTime">Most recent data tick</param>
        public void SetDateTimeFrontier(DateTime newTime) 
        {
            _frontier = newTime;
        }

        /// <summary>
        /// Check if the *date* is open.
        /// </summary>
        /// <remarks>This is useful for first checking the date list, and then the market hours to save CPU cycles</remarks>
        /// <param name="dateToCheck">Date to check</param>
        /// <returns>Return true if the exchange is open for this date</returns>
        public virtual bool DateIsOpen(DateTime dateToCheck)
        {
            return true;
        }

        /// <summary>
        /// Time of day the market opens.
        /// </summary>
        /// <param name="time">DateTime object for this date</param>
        /// <returns>DateTime the market is considered open</returns>
        public virtual DateTime TimeOfDayOpen(DateTime time) 
        {
            //Default to midnight, start of day.
            return time.Date;
        }

        
        /// <summary>
        /// Time of day the market closes.
        /// </summary>
        /// <param name="time">DateTime object for this date</param>
        /// <returns>DateTime the market day is considered closed</returns>
        public virtual DateTime TimeOfDayClosed(DateTime time)
        {
            //Default to midnight, start of *next* day.
            return time.Date.AddDays(1);
        }

        /// <summary>
        /// Check if this DateTime is open.
        /// </summary>
        /// <param name="dateTime">DateTime to check</param>
        /// <returns>Boolean true if the market is open</returns>
        public virtual bool DateTimeIsOpen(DateTime dateTime)
        {
            return DateIsOpen(dateTime);
        }


        /// <summary>
        /// Check if the object is open including the *Extended* market hours
        /// </summary>
        /// <param name="time">Current time of day</param>
        /// <returns>True if we are in extended or primary marketing hours.</returns>
        public virtual bool DateTimeIsExtendedOpen(DateTime time)
        {
            return DateIsOpen(time);
        }
    } //End of MarketExchange


} //End Namespace