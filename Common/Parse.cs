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

namespace QuantConnect
{
    /// <summary>
    /// Provides methods for parsing strings using <see cref="CultureInfo.InvariantCulture"/>
    /// </summary>
    public static class Parse
    {
        /// <summary>
        /// Parses the provided value as a <see cref="System.TimeSpan"/> using <see cref="System.TimeSpan.Parse(string,IFormatProvider)"/>
        /// with <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static TimeSpan TimeSpan(string value)
        {
            return System.TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses the provided value as a <see cref="System.DateTime"/> using <see cref="System.DateTime.Parse(string,IFormatProvider)"/>
        /// with <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static DateTime DateTime(string value)
        {
            return System.DateTime.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses the provided value as a <see cref="System.DateTime"/> using <see cref="System.DateTime.ParseExact(string,string,IFormatProvider)"/>
        /// with the specified <paramref name="format"/> and <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static DateTime DateTimeExact(string value, string format)
        {
            return System.DateTime.ParseExact(value, format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses the provided value as a <see cref="System.DateTime"/> using <see cref="System.DateTime.ParseExact(string,string,IFormatProvider)"/>
        /// with the specified <paramref name="format"/>, <paramref name="dateTimeStyles"/> and <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static DateTime DateTimeExact(string value, string format, DateTimeStyles dateTimeStyles)
        {
            return System.DateTime.ParseExact(value, format, CultureInfo.InvariantCulture, dateTimeStyles);
        }

        /// <summary>
        /// Parses the provided value as a <see cref="double"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static double Double(string value)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses the provided value as a <see cref="decimal"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static decimal Decimal(string value)
        {
            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses the provided value as a <see cref="decimal"/> using the specified <paramref name="numberStyles"/>
        /// and <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static decimal Decimal(string value, NumberStyles numberStyles)
        {
            return decimal.Parse(value, numberStyles, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses the provided value as a <see cref="int"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static int Int(string value)
        {
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses the provided value as a <see cref="long"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static long Long(string value)
        {
            return long.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses the provided value as a <see cref="long"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// and the specified <paramref name="numberStyles"/>
        /// </summary>
        public static long Long(string value, NumberStyles numberStyles)
        {
            return long.Parse(value, numberStyles, CultureInfo.InvariantCulture);
        }
    }
}