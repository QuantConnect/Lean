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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Contains additional properties and settings for an order submitted to Fix Bloomberg
    /// </summary>
    public class BloombergFixOrderProperties : FixOrderProperties
    {
        /// <summary>
        /// The broker the shares are borrowed from for a short sale.
        /// Reads and writes fix tag LocateBroker 5700 in <see cref="FixOrderProperties.AdditionalProperties"/>.
        /// </summary>
        public string LocateBroker
        {
            get { return GetTag("5700"); }
            set { SetTag("5700", value); }
        }

        /// <summary>
        /// Whether a locate is required for the short sale, "Y" or "N".
        /// Reads and writes fix tag LocateReqd 114 in <see cref="FixOrderProperties.AdditionalProperties"/>.
        /// </summary>
        public string LocateReqd
        {
            get { return GetTag("114"); }
            set { SetTag("114", value); }
        }

        private string GetTag(string tag)
        {
            return AdditionalProperties != null && AdditionalProperties.TryGetValue(tag, out var value) ? value : null;
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
    }
}
