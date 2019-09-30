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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Period constants for multi-period fields
    /// </summary>
    public static class Period
    {
        /// <summary>
        /// Period constant for one month
        /// </summary>
        public const string OneMonth = "1M";

        /// <summary>
        /// Period constant for two months
        /// </summary>
        public const string TwoMonths = "2M";

        /// <summary>
        /// Period constant for three months
        /// </summary>
        public const string ThreeMonths = "3M";

        /// <summary>
        /// Period constant for six months
        /// </summary>
        public const string SixMonths = "6M";

        /// <summary>
        /// Period constant for nine months
        /// </summary>
        public const string NineMonths = "9M";

        /// <summary>
        /// Period constant for twelve months
        /// </summary>
        public const string TwelveMonths = "12M";

        /// <summary>
        /// Period constant for one year
        /// </summary>
        public const string OneYear = "1Y";

        /// <summary>
        /// Period constant for two years
        /// </summary>
        public const string TwoYears = "2Y";

        /// <summary>
        /// Period constant for three years
        /// </summary>
        public const string ThreeYears = "3Y";

        /// <summary>
        /// Period constant for five years
        /// </summary>
        public const string FiveYears = "5Y";

        /// <summary>
        /// Period constant for ten years
        /// </summary>
        public const string TenYears = "10Y";
    }

    /// <summary>
    /// Period constants for multi-period fields as bytes
    /// </summary>
    /// <remarks>For performance speed and memory using bytes versus strings.
    /// This is the period we are going to store in memory</remarks>
    internal static class PeriodAsByte
    {
        /// <summary>
        /// Converts a byte period to its string equivalent
        /// </summary>
        public static string Convert(byte period)
        {
            switch (period)
            {
                case 0:
                    // no period case
                    return "";
                case 1:
                    return Period.OneMonth;
                case 2:
                    return Period.TwoMonths;
                case 3:
                    return Period.ThreeMonths;
                case 6:
                    return Period.SixMonths;
                case 9:
                    return Period.NineMonths;
                case 12:
                    return Period.TwelveMonths;
                case 121:
                    return Period.OneYear;
                case 24:
                    return Period.TwoYears;
                case 36:
                    return Period.ThreeYears;
                case 60:
                    return Period.FiveYears;
                case 120:
                    return Period.TenYears;
                default:
                    throw new InvalidOperationException(Invariant($"{period} is not a valid period value"));
            }
        }

        /// <summary>
        /// Converts a string period to its byte equivalent
        /// </summary>
        public static byte Convert(string period)
        {
            switch (period)
            {
                case "":
                    // no period case
                    return NoPeriod;
                case Period.OneMonth:
                    return OneMonth;
                case Period.TwoMonths:
                    return TwoMonths;
                case Period.ThreeMonths:
                    return ThreeMonths;
                case Period.SixMonths:
                    return SixMonths;
                case Period.NineMonths:
                    return NineMonths;
                case Period.TwelveMonths:
                    return TwelveMonths;
                case Period.OneYear:
                    return OneYear;
                case Period.TwoYears:
                    return TwoYears;
                case Period.ThreeYears:
                    return ThreeYears;
                case Period.FiveYears:
                    return FiveYears;
                case Period.TenYears:
                    return TenYears;
                default:
                    throw new InvalidOperationException($"{period} is not a valid period value");
            }
        }

        /// <summary>
        /// No Period constant
        /// </summary>
        public const byte NoPeriod = 0;

        /// <summary>
        /// Period constant for one month
        /// </summary>
        public const byte OneMonth = 1;

        /// <summary>
        /// Period constant for two months
        /// </summary>
        public const byte TwoMonths = 2;

        /// <summary>
        /// Period constant for three months
        /// </summary>
        public const byte ThreeMonths = 3;

        /// <summary>
        /// Period constant for six months
        /// </summary>
        public const byte SixMonths = 6;

        /// <summary>
        /// Period constant for nine months
        /// </summary>
        public const byte NineMonths = 9;

        /// <summary>
        /// Period constant for twelve months
        /// </summary>
        public const byte TwelveMonths = 12;

        /// <summary>
        /// Period constant for one year
        /// </summary>
        public const byte OneYear = 121;

        /// <summary>
        /// Period constant for two years
        /// </summary>
        public const byte TwoYears = 24;

        /// <summary>
        /// Period constant for three years
        /// </summary>
        public const byte ThreeYears = 36;

        /// <summary>
        /// Period constant for five years
        /// </summary>
        public const byte FiveYears = 60;

        /// <summary>
        /// Period constant for ten years
        /// </summary>
        public const byte TenYears = 120;
    }
}
