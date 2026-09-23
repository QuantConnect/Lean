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
 *
*/

using System.Collections.Generic;
using Common.Util;
using QuantConnect.Interfaces;

namespace QuantConnect.Orders
{
    /// <summary>
    /// The terminal link order properties
    /// </summary>
    public class TerminalLinkOrderProperties : OrderProperties
    {
        /// <summary>
        /// Custom EMSX fields to send with the order. The key is the EMSX element name
        /// and the value is the element value, e.g. AdditionalProperties["EMSX_CFD_FLAG"] = "1"
        /// </summary>
        /// <remarks>Starts empty. Python cannot assign a plain dict to it, since pythonnet has no
        /// conversion for it; add the entries one by one, bulk load them from a dict with update(),
        /// and reset with clear()</remarks>
        public BaseExtendedDictionary<string, string> AdditionalProperties { get; set; } = [];

        /// <summary>
        /// The EMSX Instructions is the free form instructions that may be sent to the broker
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// The EMSX Handling Instruction is the instructions for handling the order or route.The values can be
        /// preconfigured or a value customized by the broker.
        /// </summary>
        public string HandlingInstruction { get; set; }

        /// <summary>
        /// The execution instruction field
        /// </summary>
        public string ExecutionInstruction { get; set; }

        /// <summary>
        /// Custom user order notes 1
        /// </summary>
        public string CustomNotes1 { get; set; }

        /// <summary>
        /// Custom user order notes 2
        /// </summary>
        public string CustomNotes2 { get; set; }

        /// <summary>
        /// Custom user order notes 3
        /// </summary>
        public string CustomNotes3 { get; set; }

        /// <summary>
        /// Custom user order notes 4
        /// </summary>
        public string CustomNotes4 { get; set; }

        /// <summary>
        /// Custom user order notes 5
        /// </summary>
        public string CustomNotes5 { get; set; }

        /// <summary>
        /// The EMSX account
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// The EMSX broker code
        /// </summary>
        public string Broker { get; set; }

        /// <summary>
        /// The EMSX locate broker code identifying the counterparty the shares are being borrowed
        /// from for a short sale (EMSX_LOCATE_BROKER, e.g. "BMTB"). Maps to the LocBrkr field on
        /// the EMSX trading ticket. Setting this (or <see cref="LocateId"/>) on a short equity sale
        /// causes the brokerage to emit EMSX_LOCATE_REQ = "Y" alongside.
        /// </summary>
        public string LocateBroker { get; set; }

        /// <summary>
        /// The EMSX locate confirmation/ticket id returned by the lending broker (EMSX_LOCATE_ID).
        /// Maps to the LocId field on the EMSX trading ticket.
        /// </summary>
        public string LocateId { get; set; }

        /// <summary>
        /// Indicates if the order is a contract for differences (CFD) trade (EMSX_CFD_FLAG).
        /// This field is applicable to trades on an order level, and does not populate on a per
        /// security basis.
        /// </summary>
        public bool IsCfdTrade
        {
            get { return AdditionalProperties != null && AdditionalProperties.TryGetValue("EMSX_CFD_FLAG", out var flag) && flag == "1"; }
            set { SetTag("EMSX_CFD_FLAG", value ? "1" : null); }
        }

        /// <summary>
        /// The EMSX order strategy details.
        /// Strategy parameters must be appended in the correct order as expected by EMSX.
        /// </summary>
        public StrategyParameters Strategy { get; set; }

        /// <summary>
        /// Whether to automatically include the position side in the order direction (buy-to-open, sell-to-close, etc.) instead of the default (buy, sell)
        /// </summary>
        public bool AutomaticPositionSides { get; set; }

        /// <summary>
        /// Can optionally specify the position side in the order direction (buy-to-open, sell-to-close, etc.) instead of the default (buy, sell)
        /// </summary>
        /// <remarks>Has precedence over <see cref="AutomaticPositionSides"/></remarks>
        public OrderPosition? PositionSide { get; set; }

        /// <summary>
        /// Returns a new instance clone of this object
        /// </summary>
        /// <remarks>Deep copies <see cref="AdditionalProperties"/> so edits on the clone, e.g. the
        /// locate cleanup in BrokerageExtensions.RemoveLocateFromNonShortOrder, never reach the
        /// instance the algorithm holds on to</remarks>
        public override IOrderProperties Clone()
        {
            var clone = (TerminalLinkOrderProperties)MemberwiseClone();
            clone.AdditionalProperties = new BaseExtendedDictionary<string, string>(AdditionalProperties);
            return clone;
        }

        private void SetTag(string tag, string value)
        {
            if (value == null)
            {
                AdditionalProperties?.Remove(tag);
            }
            else
            {
                AdditionalProperties[tag] = value;
            }
        }

        /// <summary>
        /// Models an EMSX order strategy parameter
        /// </summary>
        public class StrategyParameters
        {
            /// <summary>
            /// The strategy name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The strategy fields
            /// </summary>
            public List<StrategyField> Fields { get; set; }

            /// <summary>
            /// Creates a new TerminalLink order strategy instance
            /// </summary>
            /// <param name="name">The strategy name</param>
            /// <param name="fields">The strategy fields</param>
            public StrategyParameters(string name, List<StrategyField> fields)
            {
                Name = name;
                Fields = fields;
            }
        }

        /// <summary>
        /// Models an EMSX order strategy field
        /// </summary>
        public class StrategyField
        {
            /// <summary>
            /// The strategy field value
            /// </summary>
            public string Value { get; set; }

            /// <summary>
            /// Whether the strategy field carries a value
            /// </summary>
            public bool HasValue { get; set; }

            /// <summary>
            /// Creates a new TerminalLink order strategy field carrying a value.
            /// </summary>
            /// <param name="value">The strategy field value</param>
            public StrategyField(string value)
            {
                Value = value;
                HasValue = true;
            }

            /// <summary>
            /// Creates a new TerminalLink order strategy field without a value.
            /// </summary>
            public StrategyField()
            {
                HasValue = false;
            }
        }
    }
}
