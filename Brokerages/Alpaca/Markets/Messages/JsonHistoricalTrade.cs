/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Updates from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    // TODO: OlegRa - remove `V1` class and flatten hierarchy after removing Polygon Historical API v1 support

    [SuppressMessage(
        "Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
    internal sealed class JsonHistoricalTrade : IHistoricalTrade
    {
        [JsonIgnore]
        public String Exchange { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public Int64 TimeOffset { get { throw new InvalidOperationException(); } }

        [JsonProperty(PropertyName = "p", Required = Required.Default)]
        public Decimal Price { get; set; }

        [JsonProperty(PropertyName = "s", Required = Required.Default)]
        public Int64 Size { get; set; }

        [JsonProperty(PropertyName = "t", Required = Required.Default)]
        public Int64 TimestampInNanoseconds { get; set; }

        [JsonProperty(PropertyName = "y", Required = Required.Default)]
        public Int64 ParticipantTimestampInNanoseconds { get; set; }

        [JsonProperty(PropertyName = "f", Required = Required.Default)]
        public Int64 TradeReportingFacilityTimestampInNanoseconds { get; set; }

        [JsonProperty(PropertyName = "x", Required = Required.Default)]
        public Int64 ExchangeId { get; set; }

        [JsonProperty(PropertyName = "r", Required = Required.Default)]
        public Int64 TradeReportingFacilityId { get; set; }

        [JsonProperty(PropertyName = "z", Required = Required.Default)]
        public Int64 Tape { get; set; }

        [JsonProperty(PropertyName = "q", Required = Required.Default)]
        public Int64 SequenceNumber { get; set; }

        [JsonProperty(PropertyName = "i", Required = Required.Default)]
        public String TradeId { get; set; }

        [JsonProperty(PropertyName = "I", Required = Required.Default)]
        public String OriginalTradeId { get; set; }

        [JsonProperty(PropertyName = "c", Required = Required.Default)]
        public List<Int64> ConditionsList { get; set; }

        [JsonIgnore]
        public DateTime Timestamp =>
            DateTimeHelper.FromUnixTimeNanoseconds(TimestampInNanoseconds);

        [JsonIgnore]
        public DateTime ParticipantTimestamp =>
            DateTimeHelper.FromUnixTimeNanoseconds(ParticipantTimestampInNanoseconds);

        [JsonIgnore]
        public DateTime TradeReportingFacilityTimestamp =>
            DateTimeHelper.FromUnixTimeNanoseconds(TradeReportingFacilityTimestampInNanoseconds);

        [JsonIgnore]
        public IReadOnlyList<Int64> Conditions => ConditionsList.EmptyIfNull();
    }

    [SuppressMessage(
        "Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
    internal sealed class JsonHistoricalTradeV1 : IHistoricalTrade
    {
        [JsonProperty(PropertyName = "e", Required = Required.Default)]
        public String Exchange { get; set; }

        [JsonProperty(PropertyName = "t", Required = Required.Default)]
        public Int64 TimeOffset { get; set; }

        [JsonProperty(PropertyName = "p", Required = Required.Default)]
        public Decimal Price { get; set; }

        [JsonProperty(PropertyName = "s", Required = Required.Default)]
        public Int64 Size { get; set; }

        [JsonIgnore]
        public DateTime Timestamp { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public DateTime ParticipantTimestamp { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public DateTime TradeReportingFacilityTimestamp { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public Int64 ExchangeId { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public Int64 TradeReportingFacilityId { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public Int64 Tape { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public Int64 SequenceNumber { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public String TradeId { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public String OriginalTradeId { get { throw new InvalidOperationException(); } }

        [JsonIgnore]
        public IReadOnlyList<Int64> Conditions { get { throw new InvalidOperationException(); } }
    }
}