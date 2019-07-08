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
using System.Text.RegularExpressions;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for validating strings following a certain format, such as an email address
    /// </summary>
    public static class Validate
    {
        /// <summary>
        /// Validates the provided email address
        /// </summary>
        /// <remarks>
        /// Implementation taken from msdn (with slight refactoring for readability and C#6 compliance):
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
        /// </remarks>
        /// <param name="emailAddress">The email address to be validated</param>
        /// <returns>True if the provided email address is valid</returns>
        public static bool EmailAddress(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return false;
            }

            emailAddress = NormalizeEmailAddressDomainName(emailAddress);
            if (emailAddress == null)
            {
                // an error occurred during domain name normalization
                return false;
            }

            try
            {
                return RegularExpression.Email.IsMatch(emailAddress);
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeEmailAddressDomainName(string emailAddress)
        {
            try
            {
                // Normalize the domain
                emailAddress = RegularExpression.EmailDomainName.Replace(emailAddress, match =>
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    var domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                });
            }
            catch
            {
                return null;
            }

            return emailAddress;
        }

        /// <summary>
        /// Provides static storage of compiled regular expressions to preclude parsing on each invocation
        /// </summary>
        public static class RegularExpression
        {
            private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

            /// <summary>
            /// Matches the domain name in an email address ignored@[domain.com]
            /// Pattern sourced via msdn:
            /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
            /// </summary>
            public static readonly Regex EmailDomainName = new Regex(@"(@)(.+)$", RegexOptions.Compiled, MatchTimeout);

            /// <summary>
            /// Matches a valid email address address@sub.domain.com
            /// Pattern sourced via msdn:
            /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
            /// </summary>
            public static readonly Regex Email = new Regex(@"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$", RegexOptions.IgnoreCase | RegexOptions.Compiled, MatchTimeout);
        }
    }
}
