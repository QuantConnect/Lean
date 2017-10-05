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

using System.Collections.Generic;
using System.Xml.Serialization;

namespace QuantConnect.Brokerages.InteractiveBrokers.FinancialAdvisor
{
    /// <summary>
    /// Represents an allocation profile
    /// </summary>
    public class AllocationProfile
    {
        /// <summary>
        /// The name of the profile
        /// </summary>
        [XmlElement("name")]
        public string Name { get; set; }

        /// <summary>
        /// The type of the profile
        /// </summary>
        [XmlElement("type")]
        public string Type { get; set; }

        /// <summary>
        /// The list of allocations in the profile
        /// </summary>
        [XmlArray("ListOfAllocations")]
        public List<Allocation> Allocations { get; set; }
    }
}
