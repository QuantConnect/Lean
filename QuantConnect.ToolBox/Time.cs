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
using System.Globalization;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Time helper class collection for working with trading dates
    /// </summary>
    /// <remarks>
    /// Copied from: https://github.com/QuantConnect/Lean/blob/844a040be52353273278d21c89d69e10b8a8a24e/Common/Time.cs
    /// </remarks>
    public static class Time
    {
        /// <summary>
        /// Provides a value far enough in the future the current computer hardware will have decayed :)
        /// </summary>
        public static readonly DateTime EndOfTime = new DateTime(2050, 12, 31);

        /// <summary>
        /// One Day TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

        /// <summary>
        /// One Hour TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

        /// <summary>
        /// One Minute TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        /// <summary>
        /// One Second TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        /// <summary>
        /// One Millisecond TimeSpan Period Constant
        /// </summary>
        public static readonly TimeSpan OneMillisecond = TimeSpan.FromMilliseconds(1);

        /// <summary>
        /// Create a C# DateTime from a UnixTimestamp
        /// </summary>
        /// <param name="unixTimeStamp">Double unix timestamp (Time since Midnight Jan 1 1970)</param>
        /// <returns>C# date timeobject</returns>
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            var time = DateTime.Now;
            try
            {
                // Unix timestamp is seconds past epoch
                time = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                time = time.AddSeconds(unixTimeStamp);
            }
            catch (Exception err)
            {
                Log.Error("Time.UnixTimeStampToDateTime(): " + unixTimeStamp + err.Message);
            }
            return time;
        }

        /// <summary>
        /// Convert a Datetime to Unix Timestamp
        /// </summary>
        /// <param name="time">C# datetime object</param>
        /// <returns>Double unix timestamp</returns>
        public static double DateTimeToUnixTimeStamp(DateTime time)
        {
            double timestamp = 0;
            try
            {
                timestamp = (time - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
            }
            catch (Exception err)
            {
                Log.Error("Time.DateTimeToUnixTimeStamp(): " + time.ToOADate() + err.Message);
            }
            return timestamp;
        }

        /// <summary>
        /// Get the current time as a unix timestamp
        /// </summary>
        /// <returns>Double value of the unix as UTC timestamp</returns>
        public static double TimeStamp()
        {
            return DateTimeToUnixTimeStamp(DateTime.UtcNow);
        }

        /// <summary>
        /// Parse a standard YY MM DD date into a DateTime. Attempt common date formats 
        /// </summary>
        /// <param name="dateToParse">String date time to parse</param>
        /// <returns>Date time</returns>
        public static DateTime ParseDate(string dateToParse)
        {
            try
            {
                //First try the exact options:
                DateTime date;
                if (DateTime.TryParseExact(dateToParse, DateFormat.SixCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse, DateFormat.EightCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse.Substring(0, 19), DateFormat.JsonFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParseExact(dateToParse, DateFormat.US, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date;
                }
                if (DateTime.TryParse(dateToParse, out date))
                {
                    return date;
                }
            }
            catch (Exception err)
            {
                Log.Error("Time.ParseDate(): " + err.Message);
            }

            return DateTime.Now;
        }
    }
}