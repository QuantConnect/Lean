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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Historical Bar Size Requests
    /// </summary>
    public static class BarSize
    {
        /// <summary>
        /// 1 second bars
        /// </summary>
        public const string OneSecond = "1 secs";

        /// <summary>
        /// 5 second bars
        /// </summary>
        public const string FiveSeconds = "5 secs";

        /// <summary>
        /// 15 second bars
        /// </summary>
        public const string FifteenSeconds = "15 secs";

        /// <summary>
        /// 30 second bars
        /// </summary>
        public const string ThirtySeconds = "30 secs";

        /// <summary>
        /// 1 minute bars
        /// </summary>
        public const string OneMinute = "1 min";

        /// <summary>
        /// 2 minute bars
        /// </summary>
        public const string TwoMinutes = "2 mins";

        /// <summary>
        /// 5 minute bars
        /// </summary>
        public const string FiveMinutes = "5 mins";

        /// <summary>
        /// 15 minute bars
        /// </summary>
        public const string FifteenMinutes = "15 mins";

        /// <summary>
        /// 30 minute bars
        /// </summary>
        public const string ThirtyMinutes = "30 mins";

        /// <summary>
        /// 1 hour bars
        /// </summary>
        public const string OneHour = "1 hour";

        /// <summary>
        /// 1 day bars
        /// </summary>
        public const string OneDay = "1 day";

        /// <summary>
        /// 1 week bars
        /// </summary>
        public const string OneWeek = "1 week";

        /// <summary>
        /// 1 month bars
        /// </summary>
        public const string OneMonth = "1 month";

        /// <summary>
        /// 1 year bars
        /// </summary>
        public const string OneYear = "1 year";
    }
}
