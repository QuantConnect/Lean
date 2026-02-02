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

using System.Globalization;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Lean.DataSource.CascadeThetaData
{
    public static class ThetaDataExtensions
    {
        /// <summary>
        /// Indicates whether the exchange message error has been fired.
        /// </summary>
        private static bool _isExchangeMessageFired;

        /// <summary>
        /// Converts a date string from Theta data format (yyyyMMdd) to a DateTime object.
        /// </summary>
        /// <param name="date">The date string in Theta data format (e.g., "20240303" for March 3, 2024).</param>
        /// <returns>The equivalent DateTime object.</returns>
        public static DateTime ConvertFromThetaDataDateFormat(this string date) => DateTime.ParseExact(date, DateFormat.EightCharacter, CultureInfo.InvariantCulture);

        /// <summary>
        /// Converts a DateTime object to Theta data format (yyyyMMdd) to a string.
        /// </summary>
        /// <param name="date">The DateTime object (e.g., new DateTime(2024, 03, 03))</param>
        /// <returns>The equivalent Theta Date string date.</returns>
        public static string ConvertToThetaDataDateFormat(this DateTime date) => date.ToStringInvariant(DateFormat.EightCharacter);

        /// <summary>
        /// Generates date ranges with each range covering a specified number of days from the specified <paramref name="startDate"/> to <paramref name="endDate"/>.
        /// </summary>
        /// <param name="startDate">The starting date (inclusive) of the date range.</param>
        /// <param name="endDate">The ending date (exclusive) of the date range.</param>
        /// <param name="intervalDays">The number of days each date range should cover.</param>
        /// <returns>An enumerable sequence of tuples where each tuple contains a start date (inclusive) and end date (exclusive) covering the specified number of days.</returns>
        /// <remarks>
        /// <see href="https://http-docs.thetadata.us/Articles/Performance-And-Tuning/Request-Sizing"/>
        /// <para>
        /// Best Practices for request interval size and duration:
        /// Making large requests is often times inefficient and can lead to out of memory errors.
        /// Recommended date range limit for each type of resolution / asset class is under 1 million ticks.
        /// Going over the recommended date range may cause client or server side out of memory errors.
        /// </para>
        /// </remarks>
        public static IEnumerable<(DateTime startDate, DateTime endDate)> GenerateDateRangesWithInterval(DateTime startDate, DateTime endDate, int intervalDays = 1)
        {
            DateTime currentDate = startDate;

            while (currentDate <= endDate)
            {
                DateTime nextDate = currentDate.AddDays(intervalDays);

                if (nextDate > endDate)
                    nextDate = endDate;

                yield return (currentDate, nextDate);

                // Move to the next interval
                currentDate = nextDate.AddDays(1);
            }
        }

        /// <summary>
        /// Retrieves the time zone of the exchange for the given symbol.
        /// </summary>
        /// <param name="symbol">The symbol for which to get the exchange time zone.</param>
        /// <returns>
        /// The <see cref="NodaTime.DateTimeZone"/> representing the time zone of the exchange
        /// where the given symbol is traded.
        /// </returns>
        /// <remarks>
        /// This method uses the <see cref="MarketHoursDatabase"/> to fetch the exchange hours
        /// and extract the time zone information for the provided symbol.
        /// </remarks>
        public static NodaTime.DateTimeZone GetSymbolExchangeTimeZone(this Symbol symbol)
            => MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;

        /// <summary>
        /// Attempts to get the exchange name corresponding to the given exchange number.
        /// </summary>
        /// <param name="exchangeNumber">The numerical code of the exchange.</param>
        /// <returns>
        /// The name of the exchange if found; otherwise, an empty string.
        /// </returns>
        /// <remarks>
        /// Logs an error if the exchange number is not found in the dictionary.
        /// </remarks>
        public static string TryGetExchangeOrDefault(this byte exchangeNumber)
        {
            if (Exchanges.TryGetValue(exchangeNumber, out var exchange))
            {
                return exchange;
            }
            if (!_isExchangeMessageFired)
            {
                _isExchangeMessageFired = true;
                Log.Error($"{nameof(ThetaDataExtensions)}.{nameof(TryGetExchangeOrDefault)}: Exchange number {exchangeNumber} not found.");
            }
            return string.Empty;
        }

        /// <summary>
        /// Represents a collection of Exchanges with their corresponding numerical codes.
        /// </summary>
        private static Dictionary<byte, string> Exchanges = new()
        {
            { 1, "NQEX" },
            { 2, "NQAD" },
            { 3, "NYSE" },
            { 4, "AMEX" },
            { 5, "CBOE" },
            { 6, "ISEX" },
            { 7, "PACF" },
            { 8, "CINC" },
            { 9, "PHIL" },
            { 10, "OPRA" },
            { 11, "BOST" },
            { 12, "NQNM" },
            { 13, "NQSC" },
            { 14, "NQBB" },
            { 15, "NQPK" },
            { 16, "NQIX" },
            { 17, "CHIC" },
            { 18, "TSE" },
            { 19, "CDNX" },
            { 20, "CME" },
            { 21, "NYBT" },
            { 22, "MRCY" },
            { 23, "COMX" },
            { 24, "CBOT" },
            { 25, "NYMX" },
            { 26, "KCBT" },
            { 27, "MGEX" },
            { 28, "NYBO" },
            { 29, "NQBS" },
            { 30, "DOWJ" },
            { 31, "GEMI" },
            { 32, "SIMX" },
            { 33, "FTSE" },
            { 34, "EURX" },
            { 35, "IMPL" },
            { 36, "DTN" },
            { 37, "LMT" },
            { 38, "LME" },
            { 39, "IPEX" },
            { 40, "NQMF" },
            { 41, "FCEC" },
            { 42, "C2" },
            { 43, "MIAX" },
            { 44, "CLRP" },
            { 45, "BARK" },
            { 46, "EMLD" },
            { 47, "NQBX" },
            { 48, "HOTS" },
            { 49, "EUUS" },
            { 50, "EUEU" },
            { 51, "ENCM" },
            { 52, "ENID" },
            { 53, "ENIR" },
            { 54, "CFE" },
            { 55, "PBOT" },
            { 56, "CMEFloor" },
            { 57, "NQNX" },
            { 58, "BTRF" },
            { 59, "NTRF" },
            { 60, "BATS" },
            { 61, "FCBT" },
            { 62, "PINK" },
            { 63, "BATY" },
            { 64, "EDGE" },
            { 65, "EDGX" },
            { 66, "RUSL" },
            { 67, "CMEX" },
            { 68, "IEX" },
            { 69, "PERL" },
            { 70, "LSE" },
            { 71, "GIF" },
            { 72, "TSIX" },
            { 73, "MEMX" },
            { 74, "EMPT" },
            { 75, "LTSE" },
            { 76, "EMPT" },
            { 77, "EMPT" },
        };
    }
}
