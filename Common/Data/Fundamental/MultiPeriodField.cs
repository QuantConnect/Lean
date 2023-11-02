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

using System.Linq;
using Python.Runtime;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Abstract base class for multi-period fields
    /// </summary>
    public abstract class MultiPeriodField<T> : ReusuableCLRObject
    {
        /// <summary>
        /// No Value
        /// </summary>
        public static T NoValue { get; } = BaseFundamentalDataProvider.GetDefault<T>();

        /// <summary>
        /// The time provider instance to use
        /// </summary>
        protected ITimeProvider TimeProvider { get; }

        /// <summary>
        /// The default period
        /// </summary>
        protected abstract string DefaultPeriod { get; }

        /// <summary>
        /// The target security identifier
        /// </summary>
        protected SecurityIdentifier SecurityIdentifier { get; set; }

        /// <summary>
        /// Returns true if the field contains a value for the default period
        /// </summary>
        public abstract bool HasValue { get; }

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        public virtual T Value => GetPeriodValues().Select(x => x.Value).DefaultIfEmpty(NoValue).FirstOrDefault();

        /// <summary>
        /// Creates an empty instance
        /// </summary>
        protected MultiPeriodField()
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="timeProvider"></param>
        /// <param name="securityIdentifier"></param>
        protected MultiPeriodField(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier)
        {
            TimeProvider = timeProvider;
            SecurityIdentifier = securityIdentifier;
        }

        /// <summary>
        /// Gets a dictionary of period names and values for the field
        /// </summary>
        public abstract IReadOnlyDictionary<string, T> GetPeriodValues();

        /// <summary>
        /// Returns true if the field contains a value for the requested period
        /// </summary>
        /// <returns>True if the field contains a value for the requested period</returns>
        public virtual bool HasPeriodValue(string period) => !BaseFundamentalDataProvider.IsNone(typeof(T), GetPeriodValue(period));

        /// <summary>
        /// Gets the value of the field for the requested period
        /// </summary>
        /// <param name="period">The requested period</param>
        /// <returns>The value for the period</returns>
        public abstract T GetPeriodValue(string period);

        /// <summary>
        /// Gets the list of available period names for the field
        /// </summary>
        public IEnumerable<string> GetPeriodNames()
        {
            return GetPeriodValues().Select(x => x.Key);
        }

        /// <summary>
        /// Returns true if the field has at least one value for one period
        /// </summary>
        public bool HasValues()
        {
            return GetPeriodValues().Any();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return string.Join(";", GetPeriodValues().Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        protected string ConvertPeriod(string period)
        {
            if (string.IsNullOrEmpty(period))
            {
                return DefaultPeriod;
            }

            switch (period)
            {
                case Period.OneMonth:
                    return "OneMonth";
                case Period.TwoMonths:
                    return "TwoMonths";
                case Period.ThreeMonths:
                    return "ThreeMonths";
                case Period.SixMonths:
                    return "SixMonths";
                case Period.NineMonths:
                    return "NineMonths";
                case Period.TwelveMonths:
                    return "TwelveMonths";
                case Period.OneYear:
                    return "OneYear";
                case Period.TwoYears:
                    return "TwoYears";
                case Period.ThreeYears:
                    return "ThreeYears";
                case Period.FiveYears:
                    return "FiveYears";
                case Period.TenYears:
                    return "TenYears";
                default:
                    return period;
            }
        }

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        public static implicit operator T(MultiPeriodField<T> instance)
        {
            return instance.Value;
        }
    }

    public abstract class MultiPeriodField : MultiPeriodField<double>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        protected MultiPeriodField()
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="timeProvider"></param>
        /// <param name="securityIdentifier"></param>
        protected MultiPeriodField(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier) : base(timeProvider, securityIdentifier)
        {
        }

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        public static implicit operator decimal(MultiPeriodField instance)
        {
            return (decimal)instance.Value;
        }
    }

    public abstract class MultiPeriodFieldLong : MultiPeriodField<long>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        protected MultiPeriodFieldLong()
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="timeProvider"></param>
        /// <param name="securityIdentifier"></param>
        protected MultiPeriodFieldLong(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier) : base(timeProvider, securityIdentifier)
        {
        }

        /// <summary>
        /// Returns the default value for the field
        /// </summary>
        public static implicit operator decimal(MultiPeriodFieldLong instance)
        {
            return (decimal)instance.Value;
        }
    }
}
