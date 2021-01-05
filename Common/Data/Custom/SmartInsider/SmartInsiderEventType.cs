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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace QuantConnect.Data.Custom.SmartInsider
{
    /// <summary>
    /// Describes what will or has taken place in an execution
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SmartInsiderEventType
    {
        /// <summary>
        /// Notification that the board has gained the authority to repurchase
        /// </summary>
        [EnumMember(Value = "Authorisation")]
        Authorization,

        /// <summary>
        /// Notification of the board that shares will be repurchased.
        /// </summary>
        [EnumMember(Value = "New Intention")]
        Intention,

        /// <summary>
        /// Repurchase transactions that have been actioned.
        /// </summary>
        [EnumMember(Value = "Transaction")]
        Transaction,

        /// <summary>
        /// Increase in the scope of the existing plan (extended date, increased value, etc.)
        /// </summary>
        [EnumMember(Value = "Upwards Revision")]
        UpwardsRevision,

        /// <summary>
        /// Decrease in the scope of the existing plan (shortened date, reduced value, etc.)
        /// </summary>
        [EnumMember(Value = "Downwards Revision")]
        DownwardsRevision,

        /// <summary>
        /// General change of details of the plan (max/min price alteration, etc.)
        /// </summary>
        [EnumMember(Value = "Revised Details")]
        RevisedDetails,

        /// <summary>
        /// Total cancellation of the plan
        /// </summary>
        [EnumMember(Value = "Programme Cancellation")]
        Cancellation,

        /// <summary>
        /// Announcement by a company that the board of directors or management will be seeking to obtain authorisation for a repurchase plan.
        /// </summary>
        [EnumMember(Value = "Seek Authorisation")]
        SeekAuthorization,

        /// <summary>
        /// Announcement by a company that a plan of repurchase has been suspended. Further details of the suspension are included in the note.
        /// </summary>
        [EnumMember(Value = "Plan Suspension")]
        PlanSuspension,
        
        /// <summary>
        /// Announcement by a company that a suspended plan has been re-started. Further details of the suspension are included in the note.
        /// </summary>
        [EnumMember(Value = "Plan Re-started")]
        PlanReStarted,
        
        /// <summary>
        /// Announcement by a company not specified and/or not documented in the other categories. Further details are included in the note.
        /// </summary>
        NotSpecified
    }
}
