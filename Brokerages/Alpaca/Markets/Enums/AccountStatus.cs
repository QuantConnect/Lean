﻿﻿/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// User account status in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AccountStatus
    {
        /// <summary>
        /// Account opened but not verified.
        /// </summary>
        [EnumMember(Value = "ONBOARDING")]
        Onboarding,

        /// <summary>
        /// Missed important information.
        /// </summary>
        [EnumMember(Value = "SUBMISSION_FAILED")]
        SubmissionFailed,

        /// <summary>
        /// Additional information added.
        /// </summary>
        [EnumMember(Value = "SUBMITTED")]
        Submitted,

        /// <summary>
        /// Accouunt data updated.
        /// </summary>
        [EnumMember(Value = "ACCOUNT_UPDATED")]
        AccountUpdated,

        /// <summary>
        /// Approval request sent.
        /// </summary>
        [EnumMember(Value = "APPROVAL_PENDING")]
        ApprovalPending,

        /// <summary>
        /// Account approved and working.
        /// </summary>
        [EnumMember(Value = "ACTIVE")]
        Active,

        /// <summary>
        /// Account approval rejected.
        /// </summary>
        [EnumMember(Value = "REJECTED")]
        Rejected
    }
}
