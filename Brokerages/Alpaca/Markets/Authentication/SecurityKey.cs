/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 * Changes:
 *   * constructor to reuse existing null check code
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Base class for 'security key' abstraction.
    /// </summary>
    public abstract class SecurityKey
    {
        /// <summary>
        /// Creates new instance of <see cref="SecurityKey"/> object.
        /// </summary>
        /// <param name="value">Security key value.</param>
        protected SecurityKey(String value)
        {
            Value = value.EnsureNotNull(nameof(value));
        }

        internal String Value { get; }

        internal abstract IEnumerable<KeyValuePair<String, String>> GetAuthenticationHeaders();

        internal abstract JsonAuthRequest.JsonData GetAuthenticationData();
    }
}
