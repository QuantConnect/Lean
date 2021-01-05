/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/
using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonAccountUpdate : IAccountUpdate
    {
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public Guid AccountId { get; set; }

        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        public AccountStatus Status { get; set; }

        [JsonProperty(PropertyName = "currency", Required = Required.Default)]
        public String Currency { get; set; }

        [JsonProperty(PropertyName = "cash", Required = Required.Always)]
        public Decimal TradableCash { get; set; }

        [JsonProperty(PropertyName = "cash_withdrawable", Required = Required.Default)]
        public Decimal WithdrawableCash { get; set; }

        [JsonProperty(PropertyName = "created_at", Required = Required.Always)]
        public DateTime CreatedAt { get; set; }

        [JsonProperty(PropertyName = "updated_at", Required = Required.Always)]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty(PropertyName = "deleted_at", Required = Required.AllowNull)]
        public DateTime? DeletedAt { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(
            StreamingContext context)
        {
            if (String.IsNullOrEmpty(Currency))
            {
                Currency = Currencies.USD;
            }
        }
    }
}
