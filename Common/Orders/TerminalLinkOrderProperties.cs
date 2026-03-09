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

namespace QuantConnect.Orders
{
    /// <summary>
    /// The terminal link order properties
    /// </summary>
    public class TerminalLinkOrderProperties : OrderProperties
    {
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
