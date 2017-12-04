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

using NodaTime;

namespace QuantConnect
{
    /// <summary>
    /// Provides access to common time zones
    /// </summary>
    public static class TimeZones
    {
        /// <summary>
        /// Gets the Universal Coordinated time zone.
        /// </summary>
        public static readonly DateTimeZone Utc = DateTimeZone.Utc;

        /// <summary>
        /// Gets the time zone for New York City, USA. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone NewYork = DateTimeZoneProviders.Tzdb["America/New_York"];

        /// <summary>
        /// Get the Eastern Standard Time (EST) WITHOUT daylight savings, this is a constant -5 hour offset
        /// </summary>
        public static readonly DateTimeZone EasternStandard = DateTimeZoneProviders.Tzdb["UTC-05"];

        /// <summary>
        /// Gets the time zone for London, England. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone London = DateTimeZoneProviders.Tzdb["Europe/London"];

        /// <summary>
        /// Gets the time zone for Hong Kong, China.
        /// </summary>
        public static readonly DateTimeZone HongKong = DateTimeZoneProviders.Tzdb["Asia/Hong_Kong"];

        /// <summary>
        /// Gets the time zone for Tokyo, Japan.
        /// </summary>
        public static readonly DateTimeZone Tokyo = DateTimeZoneProviders.Tzdb["Asia/Tokyo"];

        /// <summary>
        /// Gets the time zone for Rome, Italy. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Rome = DateTimeZoneProviders.Tzdb["Europe/Rome"];

        /// <summary>
        /// Gets the time zone for Sydney, Australia. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Sydney = DateTimeZoneProviders.Tzdb["Australia/Sydney"];

        /// <summary>
        /// Gets the time zone for Vancouver, Canada.
        /// </summary>
        public static readonly DateTimeZone Vancouver = DateTimeZoneProviders.Tzdb["America/Vancouver"];

        /// <summary>
        /// Gets the time zone for Toronto, Canada. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Toronto = DateTimeZoneProviders.Tzdb["America/Toronto"];

        /// <summary>
        /// Gets the time zone for Chicago, USA. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Chicago = DateTimeZoneProviders.Tzdb["America/Chicago"];

        /// <summary>
        /// Gets the time zone for Los Angeles, USA. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone LosAngeles = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];

        /// <summary>
        /// Gets the time zone for Phoenix, USA. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Phoenix = DateTimeZoneProviders.Tzdb["America/Phoenix"];

        /// <summary>
        /// Gets the time zone for Auckland, New Zealand. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Auckland = DateTimeZoneProviders.Tzdb["Pacific/Auckland"];

        /// <summary>
        /// Gets the time zone for Moscow, Russia.
        /// </summary>
        public static readonly DateTimeZone Moscow = DateTimeZoneProviders.Tzdb["Europe/Moscow"];

        /// <summary>
        /// Gets the time zone for Madrid, Span. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Madrid = DateTimeZoneProviders.Tzdb["Europe/Madrid"];

        /// <summary>
        /// Gets the time zone for Buenos Aires, Argentia.
        /// </summary>
        public static readonly DateTimeZone BuenosAires = DateTimeZoneProviders.Tzdb["America/Argentina/Buenos_Aires"];

        /// <summary>
        /// Gets the time zone for Brisbane, Australia.
        /// </summary>
        public static readonly DateTimeZone Brisbane = DateTimeZoneProviders.Tzdb["Australia/Brisbane"];

        /// <summary>
        /// Gets the time zone for Sao Paulo, Brazil. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone SaoPaulo = DateTimeZoneProviders.Tzdb["America/Sao_Paulo"];

        /// <summary>
        /// Gets the time zone for Cairo, Egypt.
        /// </summary>
        public static readonly DateTimeZone Cairo = DateTimeZoneProviders.Tzdb["Africa/Cairo"];

        /// <summary>
        /// Gets the time zone for Johannesburg, South Africa.
        /// </summary>
        public static readonly DateTimeZone Johannesburg = DateTimeZoneProviders.Tzdb["Africa/Johannesburg"];

        /// <summary>
        /// Gets the time zone for Anchorage, USA. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Anchorage = DateTimeZoneProviders.Tzdb["America/Anchorage"];

        /// <summary>
        /// Gets the time zone for Denver, USA. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Denver = DateTimeZoneProviders.Tzdb["America/Denver"];

        /// <summary>
        /// Gets the time zone for Detroit, USA. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Detroit = DateTimeZoneProviders.Tzdb["America/Detroit"];

        /// <summary>
        /// Gets the time zone for Mexico City, Mexico. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone MexicoCity = DateTimeZoneProviders.Tzdb["America/Mexico_City"];

        /// <summary>
        /// Gets the time zone for Jerusalem, Israel. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Jerusalem = DateTimeZoneProviders.Tzdb["Asia/Jerusalem"];

        /// <summary>
        /// Gets the time zone for Shanghai, China.
        /// </summary>
        public static readonly DateTimeZone Shanghai = DateTimeZoneProviders.Tzdb["Asia/Shanghai"];

        /// <summary>
        /// Gets the time zone for Melbourne, Australia. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Melbourne = DateTimeZoneProviders.Tzdb["Australia/Melbourne"];

        /// <summary>
        /// Gets the time zone for Amsterdam, Netherlands. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Amsterdam = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];

        /// <summary>
        /// Gets the time zone for Athens, Greece. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Athens = DateTimeZoneProviders.Tzdb["Europe/Athens"];

        /// <summary>
        /// Gets the time zone for Berlin, Germany. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Berlin = DateTimeZoneProviders.Tzdb["Europe/Berlin"];

        /// <summary>
        /// Gets the time zone for Bucharest, Romania. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Bucharest = DateTimeZoneProviders.Tzdb["Europe/Bucharest"];

        /// <summary>
        /// Gets the time zone for Dublin, Ireland. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Dublin = DateTimeZoneProviders.Tzdb["Europe/Dublin"];

        /// <summary>
        /// Gets the time zone for Helsinki, Finland. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Helsinki = DateTimeZoneProviders.Tzdb["Europe/Helsinki"];

        /// <summary>
        /// Gets the time zone for Istanbul, Turkey. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Istanbul = DateTimeZoneProviders.Tzdb["Europe/Istanbul"];

        /// <summary>
        /// Gets the time zone for Minsk, Belarus.
        /// </summary>
        public static readonly DateTimeZone Minsk = DateTimeZoneProviders.Tzdb["Europe/Minsk"];

        /// <summary>
        /// Gets the time zone for Paris, France. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Paris = DateTimeZoneProviders.Tzdb["Europe/Paris"];

        /// <summary>
        /// Gets the time zone for Zurich, Switzerland. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Zurich = DateTimeZoneProviders.Tzdb["Europe/Zurich"];

        /// <summary>
        /// Gets the time zone for Honolulu, USA. This is a daylight savings time zone.
        /// </summary>
        public static readonly DateTimeZone Honolulu = DateTimeZoneProviders.Tzdb["Pacific/Honolulu"];
    }
}
