/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 * Changes:
 *   * Made constructor C# 6 compatible by removing => definition
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Secret API key for Alpaca/Polygon APIs authentication.
    /// </summary>
    public sealed class SecretKey : SecurityKey
    {
        /// <summary>
        /// Creates new instance of <see cref="SecretKey"/> object.
        /// </summary>
        /// <param name="keyId">Secret API key identifier.</param>
        /// <param name="value">Secret API key value.</param>
        public SecretKey(
            String keyId,
            String value)
            : base(value)
        {
            KeyId = keyId;
        }

        internal String KeyId { get; }

        internal override IEnumerable<KeyValuePair<String, String>> GetAuthenticationHeaders()
        {
            yield return new KeyValuePair<String, String>(
                "APCA-API-KEY-ID", KeyId);
            yield return new KeyValuePair<String, String>(
                "APCA-API-SECRET-KEY", Value);
        }

        internal override JsonAuthRequest.JsonData GetAuthenticationData() =>
            new JsonAuthRequest.JsonData
            {
                KeyId = KeyId,
                SecretKey = Value
            };
    }
}
