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

namespace QuantConnect.Data.Custom.Tiingo
{
    /// <summary>
    /// Helper class for Tiingo configuration
    /// </summary>
    public static class Tiingo
    {
        /// <summary>
        /// Gets the Tiingo API token.
        /// </summary>
        public static string AuthCode { get; private set; } = string.Empty;

        /// <summary>
        /// Returns true if the Tiingo API token has been set.
        /// </summary>
        public static bool IsAuthCodeSet { get; private set; }

        /// <summary>
        /// Sets the Tiingo API token.
        /// </summary>
        /// <param name="authCode">The Tiingo API token</param>
        public static void SetAuthCode(string authCode)
        {
            if (string.IsNullOrWhiteSpace(authCode)) return;

            AuthCode = authCode;
            IsAuthCodeSet = true;
        }
    }
}
