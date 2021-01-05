/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Order status in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderStatus
    {
        /// <summary>
        /// Order accepted by server.
        /// </summary>
        [EnumMember(Value = "accepted")]
        Accepted,

        /// <summary>
        /// New working order.
        /// </summary>
        [EnumMember(Value = "new")]
        New,

        /// <summary>
        /// Order partially filled.
        /// </summary>
        [EnumMember(Value = "partially_filled")]
        PartiallyFilled,

        /// <summary>
        /// Order completely filled.
        /// </summary>
        [EnumMember(Value = "filled")]
        Filled,

        /// <summary>
        /// Order processing done.
        /// </summary>
        [EnumMember(Value = "done_for_day")]
        DoneForDay,

        /// <summary>
        /// Order cancelled.
        /// </summary>
        [EnumMember(Value = "canceled")]
        Canceled,

        /// <summary>
        /// Order replaced (modified).
        /// </summary>
        [EnumMember(Value = "replaced")]
        Replaced,

        /// <summary>
        /// Order cancellation request pending.
        /// </summary>
        [EnumMember(Value = "pending_cancel")]
        PendingCancel,

        /// <summary>
        /// Order processing stopped by server.
        /// </summary>
        [EnumMember(Value = "stopped")]
        Stopped,

        /// <summary>
        /// Order rejected by server side.
        /// </summary>
        [EnumMember(Value = "rejected")]
        Rejected,

        /// <summary>
        /// Order processing suspended by server.
        /// </summary>
        [EnumMember(Value = "suspended")]
        Suspended,

        /// <summary>
        /// Initial new order request pending.
        /// </summary>
        [EnumMember(Value = "pending_new")]
        PendingNew,

        /// <summary>
        /// Order information calculated by server.
        /// </summary>
        [EnumMember(Value = "calculated")]
        Calculated,

        /// <summary>
        /// Order expired.
        /// </summary>
        [EnumMember(Value = "expired")]
        Expired,

        /// <summary>
        /// Order accepted for bidding by server.
        /// </summary>
        [EnumMember(Value = "accepted_for_bidding")]
        AcceptedForBidding,

        /// <summary>
        /// Order replacement request pending.
        /// </summary>
        [EnumMember(Value = "pending_replace")]
        PendingReplace,

        /// <summary>
        /// Order completely filled.
        /// </summary>
        [EnumMember(Value = "fill")]
        Fill
    }
}
