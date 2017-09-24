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
using System.Linq;
using Newtonsoft.Json;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Abstract base class for multi-period fields
    /// </summary>
    public abstract class MultiPeriodField
    {
        /// <summary>
        /// The dictionary store containing all values for the multi-period field
        /// </summary>
        protected IDictionary<string, decimal> Store;

        /// <summary>
        /// Gets the default period for the field
        /// </summary>
        protected virtual string DefaultPeriod
        {
            get { return Store.Keys.FirstOrDefault() ?? string.Empty; }
        }

        /// <summary>
        /// Gets the value of the field for the requested period
        /// </summary>
        /// <param name="period">The requested period</param>
        /// <returns>The value for the period</returns>
        protected decimal GetPeriodValue(string period)
        {
            decimal value;
            return Store.TryGetValue(period, out value) ? value : 0m;
        }

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        [JsonIgnore]
        public decimal Value
        {
            get
            {
                if (Store.Count == 0) return 0;

                decimal value;
                return Store.TryGetValue(DefaultPeriod, out value) ? value : Store.First().Value;
            }
        }

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        /// <param name="field"></param>
        public static implicit operator decimal(MultiPeriodField field)
        {
            return field.Value;
        }

        /// <summary>
        /// Sets the value of the field for the specified period
        /// </summary>
        /// <param name="period">The period</param>
        /// <param name="value">The value to be set</param>
        public void SetPeriodValue(string period, decimal value)
        {
            Store[period] = value;
        }

        /// <summary>
        /// Returns true if the field has at least one value for one period
        /// </summary>
        public bool HasValues()
        {
            return Store.Count > 0;
        }

        /// <summary>
        /// Sets period values for non existing periods from a previous instance
        /// </summary>
        /// <remarks>Used to fill-forward values from previous dates</remarks>
		/// <param name="previous">The previous instance</param>
        public void UpdateValues(MultiPeriodField previous)
        {
            if (previous == null)
                return;

            foreach (var kvp in previous.Store.Where(kvp => !Store.ContainsKey(kvp.Key)))
            {
                SetPeriodValue(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Join(";", Store.Select(x => x.Key + ":" + x.Value));
        }
    }
}
