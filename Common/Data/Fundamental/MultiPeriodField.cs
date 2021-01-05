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

using System;
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
        /// Helper, and internal struct use to hold the values for a period.
        /// </summary>
        /// <remarks>For performance using a struct versus a class, this allows us to save
        /// on references</remarks>
        protected struct PeriodField
        {
            /// <summary>
            /// Creates a new period field instance
            /// </summary>
            public PeriodField(byte period, decimal value)
            {
                Period = period;
                Value = value;
            }

            /// <summary>
            /// The period associated with this value <see cref="PeriodAsByte"/>
            /// </summary>
            public byte Period { get; set; }

            /// <summary>
            /// The value for this period
            /// </summary>
            public decimal Value { get; set; }
        }

        private bool StoreIsEmpty => Store == null || Store.Length == 0;

        /// <summary>
        /// The dictionary store containing all values for the multi-period field
        /// </summary>
        /// <remarks>For performance using and array versus any other collection
        /// this allows us to save in memory footprint and speed. We expect few amount of
        /// elements in each collection but multiple collections <see cref="FineFundamental"/></remarks>
        protected PeriodField[] Store;

        /// <summary>
        /// Gets the default period for the field
        /// </summary>
        protected virtual byte DefaultPeriod => StoreIsEmpty ? PeriodAsByte.NoPeriod : Store.FirstOrDefault().Period;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        protected MultiPeriodField(IDictionary<string, decimal> store = null)
        {
            if (store != null)
            {
                Store = store.Select(kvp => new PeriodField(PeriodAsByte.Convert(kvp.Key), kvp.Value)).ToArray();
            }
        }

        /// <summary>
        /// Gets the value of the field for the requested period
        /// </summary>
        /// <param name="period">The requested period</param>
        /// <returns>The value for the period</returns>
        public decimal GetPeriodValue(string period)
        {
            return GetPeriodValue(PeriodAsByte.Convert(period));
        }

        /// <summary>
        /// Internal implementation which gets the value of the field for the requested period
        /// </summary>
        protected decimal GetPeriodValue(byte period)
        {
            return StoreIsEmpty ? 0 : Store.FirstOrDefault(field => field.Period == period).Value;
        }

        /// <summary>
        /// Returns true if the field contains a value for the requested period
        /// </summary>
        /// <param name="period">The requested period</param>
        public bool HasPeriodValue(string period) => !StoreIsEmpty && Store.Any(field => field.Period == PeriodAsByte.Convert(period));

        /// <summary>
        /// Returns true if the field contains a value for the default period
        /// </summary>
        public bool HasValue => HasPeriodValue(PeriodAsByte.Convert(DefaultPeriod));

        /// <summary>
        /// Gets the list of available period names for the field
        /// </summary>
        /// <returns>The list of periods</returns>
        public IEnumerable<string> GetPeriodNames()
        {
            return StoreIsEmpty
                ? Enumerable.Empty<string>() : Store.Select(field => PeriodAsByte.Convert(field.Period));
        }

        /// <summary>
        /// Gets a dictionary of period names and values for the field
        /// </summary>
        /// <returns>The dictionary of period names and values</returns>
        public IReadOnlyDictionary<string, decimal> GetPeriodValues()
        {
            return StoreIsEmpty
                ? new Dictionary<string, decimal>()
                : Store.ToDictionary(field => PeriodAsByte.Convert(field.Period), field => field.Value);
        }

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        [JsonIgnore]
        public decimal Value
        {
            get
            {
                if (StoreIsEmpty)
                {
                    return 0;
                }

                return HasValue ? GetPeriodValue(DefaultPeriod) : Store.First().Value;
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
            SetPeriodValue(PeriodAsByte.Convert(period), value);
        }

        /// <summary>
        /// Internal implementation which sets the value of the field for the specified period
        /// </summary>
        protected void SetPeriodValue(byte period, decimal value)
        {
            if (!StoreIsEmpty)
            {
                for (var i = 0; i < Store.Length; i++)
                {
                    if (Store[i].Period == period)
                    {
                        Store[i].Value = value;
                        return;
                    }
                }
            }

            // if we are here it means the array does not have the value
            // we have to create a new array and add the value
            var index = 0;
            if (Store == null)
            {
                Store = new PeriodField[1];
            }
            else
            {
                var newSize = Store.Length + 1;
                Array.Resize(ref Store, newSize);
                index = newSize - 1;
            }
            Store[index] = new PeriodField(period, value);
        }

        /// <summary>
        /// Returns true if the field has at least one value for one period
        /// </summary>
        public bool HasValues()
        {
            return !StoreIsEmpty;
        }

        /// <summary>
        /// Applies updated values from <paramref name="update"/> to this instance
        /// </summary>
        /// <remarks>Used to apply data updates to the current instance. This WILL overwrite existing values.</remarks>
        /// <param name="update">The next data update for this instance</param>
        public void UpdateValues(MultiPeriodField update)
        {
            if (update?.Store == null)
                return;

            foreach (var kvp in update.Store)
            {
                SetPeriodValue(kvp.Period, kvp.Value);
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
            return StoreIsEmpty
                ? "" : string.Join(";", Store.Select(x => $"{PeriodAsByte.Convert(x.Period)}:{x.Value}"));
        }
    }
}
