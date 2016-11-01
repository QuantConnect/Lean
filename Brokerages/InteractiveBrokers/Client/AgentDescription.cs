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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Used for Rule 80A describes the type of trader.
    /// </summary>
    public static class AgentDescription
    {
        /// <summary>
        /// An individual
        /// </summary>
        public const string Individual = "I";

        /// <summary>
        /// An Agency
        /// </summary>
        public const string Agency = "A";

        /// <summary>
        /// An Agent or Other Member
        /// </summary>
        public const string AgentOtherMember = "W";

        /// <summary>
        /// Individual PTIA
        /// </summary>
        public const string IndividualPtia = "J";

        /// <summary>
        /// Agency PTIA
        /// </summary>
        public const string AgencyPtia = "U";

        /// <summary>
        /// Agether or Other Member PTIA
        /// </summary>
        public const string AgentOtherMemberPtia = "M";

        /// <summary>
        /// Individual PT
        /// </summary>
        public const string IndividualPt = "K";

        /// <summary>
        /// Agency PT
        /// </summary>
        public const string AgencyPt = "Y";

        /// <summary>
        /// Agent Other Member PT
        /// </summary>
        public const string AgentOtherMemberPt = "N";

        /// <summary>
        /// No Description Provided
        /// </summary>
        public const string None = "";
    }
}
