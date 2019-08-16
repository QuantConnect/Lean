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
    /// Provides extension methods for properly parsing and serializing values while properly using
    /// an IFormatProvider/CultureInfo when applicable
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Parses the specified string as <typeparamref name="T"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static T ConvertInvariant<T>(this object convertible)
        {
            return (T)convertible.ConvertInvariant(typeof(T));
        }

        /// <summary>
        /// Parses the specified string as <paramref name="conversionType"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static object ConvertInvariant(this object convertible, Type conversionType)
        {
            return Convert.ChangeType(convertible, conversionType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Non-extension method alias for <see cref="FormattableString.Invariant"/>
        /// This supports the <code>using static QuantConnect.StringExtensions</code> syntax
        /// and is aimed at ensuring all formatting is piped through this class instead of
        /// alternatively piping through directly to <see cref="FormattableString.Invariant"/>
        /// </summary>
        public static string Invariant(FormattableString formattable)
        {
            return FormattableString.Invariant(formattable);
        }

        /// <summary>
        /// Converts the provided value to a string using <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static string ToStringInvariant(this IConvertible convertible)
        {
            if (convertible == null)
            {
                return string.Empty;
            }

            return convertible.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats the provided value using the specified <paramref name="format"/> and
        /// <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        public static string ToStringInvariant(this IFormattable formattable, string format)
        {
            if (formattable == null)
            {
                return string.Empty;
            }

            // if we have a colon, this implies there's a width parameter in the format it seems this isn't handled
            // as one would expect. For example, specifying something like $"{value,10:0.00}" would force the string
            // to be at least 10 characters wide with extra padding in the front, but passing the string '10:0.00' or
            // ',10:0.00' doesn't work. If we are able to detect a colon in the format and the values preceding the colon,
            // are numeric, then we know it starts with a width parameter and we can pipe it into a custom-formed
            // string.format call to get the correct output
            if (format != null)
            {
                var indexOfColon = format.IndexOfInvariant(":");
                if (indexOfColon != -1)
                {
                    int padding;
                    var beforeColon = format.Substring(0, indexOfColon);
                    if (int.TryParse(beforeColon, out padding))
                    {
                        return string.Format(CultureInfo.InvariantCulture, $"{{0,{format}}}", formattable);
                    }
                }
            }

            return formattable.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Provides a convenience methods for converting a <see cref="DateTime"/> to an invariant ISO-8601 string
        /// </summary>
        public static string ToIso8601Invariant(this DateTime dateTime)
        {
            return dateTime.ToStringInvariant("O");
        }

        /// <summary>
        /// Checks if the string starts with the provided <paramref name="beginning"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// while optionally ignoring case.
        /// </summary>
        public static bool StartsWithInvariant(this string value, string beginning, bool ignoreCase = false)
        {
            return value.StartsWith(beginning, ignoreCase, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Checks if the string ends with the provided <paramref name="ending"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// while optionally ignoring case.
        /// </summary>
        public static bool EndsWithInvariant(this string value, string ending, bool ignoreCase = false)
        {
            return value.EndsWith(ending, ignoreCase, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Gets the index of the specified <paramref name="character"/> using <see cref="StringComparison.InvariantCulture"/>
        /// </summary>
        public static int IndexOfInvariant(this string value, char character)
        {
            return value.IndexOf(character);
        }

        /// <summary>
        /// Gets the index of the specified <paramref name="substring"/> using <see cref="StringComparison.InvariantCulture"/>
        /// </summary>
        public static int IndexOfInvariant(this string value, string substring, bool ignoreCase = false)
        {
            return value.IndexOf(substring, ignoreCase
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture
            );
        }

        /// <summary>
        /// Gets the index of the specified <paramref name="substring"/> using <see cref="StringComparison.InvariantCulture"/>
        /// </summary>
        public static int LastIndexOfInvariant(this string value, string substring, bool ignoreCase = false)
        {
            return value.LastIndexOf(substring, ignoreCase
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture
            );
        }

        /// <summary>
        /// Provides a shorthand for avoiding the more verbose ternary equivalent.
        /// Consider the following:
        /// <code>
        /// string.IsNullOrEmpty(str) ? (decimal?)null : Convert.ToDecimal(str, CultureInfo.InvariantCulture)
        /// </code>
        /// Can be expressed as:
        /// <code>
        /// str.IfNotNullOrEmpty&lt;decimal?&gt;(s => Convert.ToDecimal(str, CultureInfo.InvariantCulture))
        /// </code>
        /// When combined with additional methods from this class, reducing further to a declarative:
        /// <code>
        /// str.IfNotNullOrEmpty&lt;decimal?&gt;(s => s.ParseDecimalInvariant())
        /// str.IfNotNullOrEmpty&lt;decimal?&gt;(s => s.ConvertInvariant&lt;decimal&gt;())
        /// </code>
        /// </summary>
        /// <paramref name="value">The string value to check for null or empty</paramref>
        /// <paramref name="defaultValue">The default value to use if null or empty</paramref>
        /// <paramref name="func">Function run on non-null string w/ length > 0</paramref>
        public static T IfNotNullOrEmpty<T>(this string value, T defaultValue, Func<string, T> func)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return func(value);
        }

        /// <summary>
        /// Provides a shorthand for avoiding the more verbose ternary equivalent.
        /// Consider the following:
        /// <code>
        /// string.IsNullOrEmpty(str) ? (decimal?)null : Convert.ToDecimal(str, CultureInfo.InvariantCulture)
        /// </code>
        /// Can be expressed as:
        /// <code>
        /// str.IfNotNullOrEmpty&lt;decimal?&gt;(s => Convert.ToDecimal(str, CultureInfo.InvariantCulture))
        /// </code>
        /// When combined with additional methods from this class, reducing further to a declarative:
        /// <code>
        /// str.IfNotNullOrEmpty&lt;decimal?&gt;(s => s.ParseDecimalInvariant())
        /// str.IfNotNullOrEmpty&lt;decimal?&gt;(s => s.ConvertInvariant&lt;decimal&gt;())
        /// </code>
        /// </summary>
        /// <paramref name="value">The string value to check for null or empty</paramref>
        /// <paramref name="func">Function run on non-null string w/ length > 0</paramref>
        public static T IfNotNullOrEmpty<T>(this string value, Func<string, T> func)
        {
            return value.IfNotNullOrEmpty(default(T), func);
        }
    }
}