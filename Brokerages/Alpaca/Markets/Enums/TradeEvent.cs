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
    /// Trade event in Alpaca trade update stream
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TradeEvent
    {
        /// <summary>
        /// New working order.
        /// </summary>
        [EnumMember(Value = "new")]
        New,

        /// <summary>
        /// Order partially filled.
        /// </summary>
        [EnumMember(Value = "partial_fill")]
        PartialFill,

        /// <summary>
        /// Order completely filled.
        /// </summary>
        [EnumMember(Value = "fill")]
        Fill,

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
        /// Order cancellation was rejected.
        /// </summary>
        [EnumMember(Value = "order_cancel_rejected")]
        OrderCancelRejected
    }
}
